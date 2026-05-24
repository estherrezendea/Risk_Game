using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Risk.Shared.Protos;
using Risk.Domain;
using Risk.Domain.Servicos;

namespace Risk.Server.Services
{
    public class JogoRiskService : JogoRisk.JogoRiskBase
    {
        private static readonly Partida PartidaAtual = new Partida();
        private static readonly ServicoDeCombate ServicoCombate = new ServicoDeCombate(new Dado());
        private static readonly object _lock = new object();

        static JogoRiskService()
        {
            Console.WriteLine("[SERVIDOR] Gerando mapa inicial do jogo...");
            PartidaAtual.InicializarMapaExemplo();
        }

        public override Task<ConectarResponse> Conectar(ConectarRequest request, ServerCallContext context)
        {
            Console.WriteLine($"[SERVIDOR] Recebendo pedido de conexão de: {request.NomeJogador}");
            try
            {
                lock (_lock)
                {
                    var novoJogador = new Jogador(request.NomeJogador);
                    PartidaAtual.AdicionarJogador(novoJogador);
                    Console.WriteLine($"[SERVIDOR] Jogador {novoJogador.Nome} conectado.");

                    if (PartidaAtual.Jogadores.Count == 2 && PartidaAtual.EstadoAtual == EstadoPartida.AguardandoJogadores)
                    {
                        Console.WriteLine("\n[SERVIDOR] 2 Jogadores detectados! Iniciando partida...");
                        PartidaAtual.IniciarPartida(); // Muda status para Distribuindo
                        PartidaAtual.DistribuirTerritoriosETropas(); // Entrega os países e vai para EmAndamento
                        Console.WriteLine($"[SERVIDOR] Mapa distribuído. É a vez de: {PartidaAtual.ObterJogadorAtual().Nome}\n");
                    }

                    return Task.FromResult(new ConectarResponse
                    {
                        JogadorId = novoJogador.Id.ToString(),
                        Sucesso = true,
                        Mensagem = "Bem-vindo ao Quartel General!"
                    });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ConectarResponse { Sucesso = false, Mensagem = ex.Message });
            }
        }

        public override Task<ObterEstadoResponse> ObterEstado(ObterEstadoRequest request, ServerCallContext context)
        {
            lock (_lock)
            {
                var response = new ObterEstadoResponse
                {
                    FaseAtual = PartidaAtual.EstadoAtual.ToString()
                };

                var jogadorSolicitante = PartidaAtual.Jogadores.FirstOrDefault(j => j.Id.ToString() == request.JogadorId);
                Jogador? jogadorAtual = null;

                if (PartidaAtual.EstadoAtual != EstadoPartida.AguardandoJogadores && PartidaAtual.Jogadores.Count > 0)
                {
                    jogadorAtual = PartidaAtual.ObterJogadorAtual();
                    response.IdJogadorAtual = jogadorAtual.Id.ToString();
                    response.NomeJogadorAtual = jogadorAtual.Nome;
                    response.TropasLivres = jogadorAtual.TropasLivres;
                }

                if (jogadorSolicitante != null)
                {
                    foreach (var carta in jogadorSolicitante.Cartas)
                    {
                        response.SuasCartas.Add(new CartaDto
                        {
                            PaisNome = carta.PaisNome,
                            TipoTropa = carta.Tipo.ToString()
                        });
                    }
                }

                foreach (var continente in PartidaAtual.Tabuleiro)
                {
                    foreach (var pais in continente.Paises)
                    {
                        string nomeDono = "Sem Dono";
                        if (pais.JogadorDonoId.HasValue)
                        {
                            var dono = PartidaAtual.Jogadores.FirstOrDefault(j => j.Id == pais.JogadorDonoId.Value);
                            if (dono != null) nomeDono = dono.Nome;
                        }

                        response.Paises.Add(new PaisDto
                        {
                            Nome = pais.Nome,
                            NomeDono = nomeDono,
                            Cavalarias = pais.SaldoDeTropas / 5,
                            Infantarias = pais.SaldoDeTropas % 5
                        });
                    }
                }

                return Task.FromResult(response);
            }
        }

        public override Task<PassarTurnoResponse> PassarTurno(PassarTurnoRequest request, ServerCallContext context)
        {
            try
            {
                lock (_lock)
                {
                    var jogadorAtual = PartidaAtual.ObterJogadorAtual();

                    if (jogadorAtual.Id.ToString() != request.JogadorId)
                    {
                        return Task.FromResult(new PassarTurnoResponse { Sucesso = false, Mensagem = "Ação negada: Não é o seu turno." });
                    }

                    PartidaAtual.PassarTurno();

                    Console.WriteLine($"[SERVIDOR] Turno passado. Agora é a vez de: {PartidaAtual.ObterJogadorAtual().Nome}");
                    return Task.FromResult(new PassarTurnoResponse { Sucesso = true, Mensagem = "Turno encerrado com sucesso." });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new PassarTurnoResponse { Sucesso = false, Mensagem = ex.Message });
            }
        }

        public override Task<AtacarResponse> Atacar(AtacarRequest request, ServerCallContext context)
        {
            try
            {
                lock (_lock)
                {
                    var jogadorAtual = PartidaAtual.ObterJogadorAtual();
                    if (jogadorAtual.Id.ToString() != request.JogadorId)
                        throw new InvalidOperationException("Não é o seu turno de atacar!");

                    Pais? atacante = null;
                    Pais? defensor = null;

                    foreach (var continente in PartidaAtual.Tabuleiro)
                    {
                        if (atacante == null)
                            atacante = continente.Paises.FirstOrDefault(p => p.Nome.Equals(request.PaisAtacante, StringComparison.OrdinalIgnoreCase));

                        if (defensor == null)
                            defensor = continente.Paises.FirstOrDefault(p => p.Nome.Equals(request.PaisDefensor, StringComparison.OrdinalIgnoreCase));
                    }

                    if (atacante == null || defensor == null)
                        throw new InvalidOperationException("País atacante ou defensor não encontrado no tabuleiro.");

                    if (atacante.JogadorDonoId != jogadorAtual.Id)
                        throw new InvalidOperationException("Você só pode atacar a partir de um país seu.");

                    if (defensor.JogadorDonoId == jogadorAtual.Id)
                        throw new InvalidOperationException("Você não pode atacar um país que já pertence a você.");

                    if (!atacante.FazFronteiraCom(defensor.Id))
                        throw new InvalidOperationException($"Ataque negado: {atacante.Nome} não faz fronteira com {defensor.Nome}.");

                    if (request.QtdTropas <= 0)
                        throw new InvalidOperationException("A quantidade de tropas atacantes deve ser maior que zero.");

                    int tropasDefesa = Math.Min(defensor.SaldoDeTropas, 2);
                    Guid donoOriginalDefensor = defensor.JogadorDonoId ?? Guid.Empty;

                    Console.WriteLine($"[Batalha] {atacante.Nome} ataca {defensor.Nome}!");

                    var resultado = ServicoCombate.ResolverBatalha(atacante, defensor, request.QtdTropas, tropasDefesa);

                    bool conquistou = donoOriginalDefensor != defensor.JogadorDonoId;

                    string msgExtra = "";
                    if (conquistou)
                    {
                        var tipoAleatorio = (Random.Shared.Next(0, 2) == 0) ? Risk.Shared.TipoTropa.Infantaria : Risk.Shared.TipoTropa.Cavalaria;
                        var novaCarta = new Carta(defensor.Nome, tipoAleatorio);
                        jogadorAtual.ReceberCarta(novaCarta);

                        msgExtra = $"\nVocê recebeu uma carta de {tipoAleatorio} do território conquistado!";

                        PartidaAtual.VerificarCondicaoDeVitoria();
                        if (PartidaAtual.EstadoAtual == EstadoPartida.Finalizada)
                        {
                            msgExtra += "\nPARABÉNS! Você conquistou o mundo e venceu a partida!";
                        }
                    }

                    var response = new AtacarResponse
                    {
                        Sucesso = true,
                        Mensagem = conquistou ? $"Vitória! Você conquistou {defensor.Nome}. {msgExtra}" : "Batalha finalizada. O território resistiu.",
                        Conquistou = conquistou
                    };
                    response.DadosAtaque.AddRange(resultado.DadosAtaque);
                    response.DadosDefesa.AddRange(resultado.DadosDefesa);

                    return Task.FromResult(response);
                }
            }
            catch (Exception ex)
            {
                 return Task.FromResult(new AtacarResponse { Sucesso = false, Mensagem = ex.Message });
            }
        }

        public override Task<TrocarCartasResponse> TrocarCartas(TrocarCartasRequest request, ServerCallContext context)
        {
            try
            {
                lock (_lock)
                {
                    var jogadorAtual = PartidaAtual.ObterJogadorAtual();

                    // Validação 1: É a vez dele?
                    if (jogadorAtual.Id.ToString() != request.JogadorId)
                        throw new InvalidOperationException("Você só pode trocar cartas no seu próprio turno.");

                    // Validação 2: Ele mandou exatamente 3 cartas?
                    if (request.PaisesDasCartas.Count != 3)
                        throw new ArgumentException("Você deve selecionar exatamente 3 cartas para trocar.");

                    // Validação 3: Ele realmente tem essas cartas na mão?
                    var cartasDoJogador = jogadorAtual.Cartas
                        .Where(c => request.PaisesDasCartas.Contains(c.PaisNome))
                        .ToList();

                    if (cartasDoJogador.Count != 3)
                        throw new InvalidOperationException("Você tentou trocar cartas que não possui.");

                    // Validação 4: A combinação é válida? ( 3 do mesmo tipo)
                    bool todasInfantaria = cartasDoJogador.All(c => c.Tipo == Risk.Shared.TipoTropa.Infantaria);
                    bool todasCavalaria = cartasDoJogador.All(c => c.Tipo == Risk.Shared.TipoTropa.Cavalaria);

                    if (!todasInfantaria && !todasCavalaria)
                        throw new InvalidOperationException("Combinação inválida! Para este MVP, as 3 cartas devem ser do mesmo tipo.");

                    // Matemática do Bônus: Começa em 4, e aumenta de 2 em 2 a cada troca no jogo
                    int tropasGanhas = 4 + (PartidaAtual.TotalTrocasRealizadas * 2);

                    // Aplica as alterações no Domínio
                    PartidaAtual.RegistrarTrocaDeCartas();
                    jogadorAtual.RemoverCartas(request.PaisesDasCartas);
                    jogadorAtual.AdicionarTropasLivres(tropasGanhas);

                    Console.WriteLine($"[SERVIDOR] Jogador {jogadorAtual.Nome} trocou cartas e ganhou {tropasGanhas} tropas.");

                    return Task.FromResult(new TrocarCartasResponse
                    {
                        Sucesso = true,
                        Mensagem = $"Troca efetuada com sucesso! Você recebeu {tropasGanhas} tropas.",
                        TropasRecebidas = tropasGanhas
                    });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TrocarCartasResponse { Sucesso = false, Mensagem = ex.Message });
            }
        }

        public override Task<ReforcarPaisResponse> ReforcarPais(ReforcarPaisRequest request, ServerCallContext context)
        {
            try
            {
                lock (_lock)
                {
                    var jogadorAtual = PartidaAtual.ObterJogadorAtual();

                    if (jogadorAtual.Id.ToString() != request.JogadorId)
                        throw new InvalidOperationException("Você só pode reforçar países no seu próprio turno.");

                    if (request.Quantidade <= 0)
                        throw new InvalidOperationException("A quantidade de tropas para reforço deve ser maior que zero.");

                    Pais? pais = null;
                    foreach (var continente in PartidaAtual.Tabuleiro)
                    {
                        pais = continente.Paises.FirstOrDefault(p => p.Nome.Equals(request.PaisNome, StringComparison.OrdinalIgnoreCase));
                        if (pais != null)
                            break;
                    }

                    if (pais == null)
                        throw new InvalidOperationException("País não encontrado no tabuleiro.");

                    if (pais.JogadorDonoId != jogadorAtual.Id)
                        throw new InvalidOperationException("Você só pode reforçar um país que pertence a você.");

                    jogadorAtual.ConsumirTropasLivres(request.Quantidade);
                    pais.AdicionarTropas(request.Quantidade);

                    return Task.FromResult(new ReforcarPaisResponse
                    {
                        Sucesso = true,
                        Mensagem = $"Reforço aplicado com sucesso em {pais.Nome}."
                    });
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ReforcarPaisResponse { Sucesso = false, Mensagem = ex.Message });
            }
        }
    }

}
