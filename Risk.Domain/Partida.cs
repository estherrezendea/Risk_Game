using System.Linq;

namespace Risk.Domain
{
    public enum EstadoPartida
    {
        AguardandoJogadores,
        DistribuindoTropas,
        EmAndamento,
        Finalizada
    }

    public class Partida
    {
        public Guid Id { get; set; }
        public EstadoPartida EstadoAtual {get; private set;}

        private readonly List<Continente> _tabuleiro;
        public IReadOnlyList<Continente> Tabuleiro => _tabuleiro.AsReadOnly();
        private readonly List<Jogador> _jogadores;
        public IReadOnlyList<Jogador> Jogadores => _jogadores.AsReadOnly();
        public Jogador? Vencedor { get; private set; }
        public int TotalTrocasRealizadas { get; private set; }
        public int IndiceJogadorAtual { get; private set; }

        public Partida()
        {
            Id = Guid.NewGuid();
            EstadoAtual = EstadoPartida.AguardandoJogadores;
            _tabuleiro = new List<Continente>();
            _jogadores = new List<Jogador>();
            IndiceJogadorAtual = 0;
        }

        public void AdicionarContinente(Continente continente)
        {
            if (EstadoAtual != EstadoPartida.AguardandoJogadores)
                throw new InvalidOperationException("O tabuleiro só pode ser montado antes do jogo começar.");
            _tabuleiro.Add(continente);
        }

        public void AdicionarJogador(Jogador jogador)
        {
            if (EstadoAtual != EstadoPartida.AguardandoJogadores)
                throw new InvalidOperationException("A partida já começou. Não é possível entrar agora.");
            if (_jogadores.Count >= 6)
                throw new InvalidOperationException("A partida já está cheia (máximo de 6 jogadores).");
            _jogadores.Add(jogador);
        }

        public void IniciarPartida()
        {
            if (_jogadores.Count < 2)
                throw new InvalidOperationException("É necessário pelo menos 2 jogadores para iniciar a partida.");
            if (_tabuleiro.Count == 0)
                throw new InvalidOperationException("O tabuleiro deve ser montado antes de iniciar a partida.");

            EstadoAtual = EstadoPartida.DistribuindoTropas;
            IndiceJogadorAtual = 0;
        }

        public Jogador ObterJogadorAtual()
        {
            if (EstadoAtual == EstadoPartida.AguardandoJogadores)
                throw new InvalidOperationException("A partida ainda não começou.");
            return _jogadores[IndiceJogadorAtual];
        }

        public void PassarTurno()
        {
            if (EstadoAtual != EstadoPartida.EmAndamento && EstadoAtual != EstadoPartida.DistribuindoTropas)
                throw new InvalidOperationException("A partida não está em andamento.");

            if (_jogadores.Count == 0)
                throw new InvalidOperationException("Não há jogadores cadastrados na partida.");

            int tentativas = 0;
            do
            {
                IndiceJogadorAtual++;
                if (IndiceJogadorAtual >= _jogadores.Count)
                    IndiceJogadorAtual = 0;

                tentativas++;
            }
            while (!_jogadores[IndiceJogadorAtual].EstaAtivo && tentativas <= _jogadores.Count);

            if (!_jogadores[IndiceJogadorAtual].EstaAtivo)
                throw new InvalidOperationException("Não há jogadores ativos para receber o próximo turno.");

            if (EstadoAtual == EstadoPartida.EmAndamento)
                DistribuirReforcos(_jogadores[IndiceJogadorAtual]);
        }

        public void IniciarCombate()
        {
            if (EstadoAtual != EstadoPartida.DistribuindoTropas)
                throw new InvalidOperationException("O combate só pode ser iniciado após a distribuição de tropas.");

            EstadoAtual = EstadoPartida.EmAndamento;

            if (_jogadores.Count == 0)
                throw new InvalidOperationException("Não há jogadores para iniciar o combate.");

            DistribuirReforcos(_jogadores[IndiceJogadorAtual]);
        }

        public void InicializarMapaExemplo()
        {
            // Criando o Continente 1: América do Sul (Bônus de 2 tropas)
            var américaDoSul = new Continente("América do Sul", 2);
            var brasil = new Pais("Brasil");
            var argentina = new Pais("Argentina");
            var peru = new Pais("Peru");

            américaDoSul.AdicionarPais(brasil);
            américaDoSul.AdicionarPais(argentina);
            américaDoSul.AdicionarPais(peru);

            // Criando o Continente 2: América do Norte (Bônus de 5 tropas)
            var américaDoNorte = new Continente("América do Norte", 5);
            var mexico = new Pais("México");
            var usa = new Pais("EUA");
            var canada = new Pais("Canadá");

            américaDoNorte.AdicionarPais(mexico);
            américaDoNorte.AdicionarPais(usa);
            américaDoNorte.AdicionarPais(canada);

            // Conexões de fronteira
            ConectarPaises(brasil, argentina);
            ConectarPaises(brasil, peru);
            ConectarPaises(peru, mexico); // Ponte simplificada entre os continentes
            ConectarPaises(mexico, usa);
            ConectarPaises(usa, canada);

            // Adicionando os continentes ao tabuleiro da partida
            AdicionarContinente(américaDoSul);
            AdicionarContinente(américaDoNorte);
        }

        public void DistribuirTerritoriosETropas()
        {
            if (EstadoAtual != EstadoPartida.DistribuindoTropas)
                throw new InvalidOperationException("A partida não está na fase de distribuição.");

            // 1. Achata a lista: Pega todos os países de dentro de todos os continentes
            var todosOsPaises = _tabuleiro.SelectMany(continente => continente.Paises).ToList();

            // 2. Embaralha os países de forma aleatória
            var random = new Random();
            var paisesEmbaralhados = todosOsPaises.OrderBy(x => random.Next()).ToList();

            // 3. Distribuição Round-Robin (um para você, um para mim...)
            int indexJogador = 0;
            foreach (var pais in paisesEmbaralhados)
            {
                var idDono = _jogadores[indexJogador].Id;

                // Regra: começa com 3 para permitir ataques já na primeira rodada
                pais.Conquistar(idDono, 3);

                // Avança para o próximo jogador
                indexJogador++;
                if (indexJogador >= _jogadores.Count)
                {
                    indexJogador = 0; // Volta pro primeiro se chegou no fim da fila
                }
            }

            // logo após distribuir, já mudamos o status para começar a guerra!
            IniciarCombate();
        }

        public void RegistrarTrocaDeCartas()
        {
            TotalTrocasRealizadas++;
        }

        public void VerificarCondicaoDeVitoria()
        {
            if (EstadoAtual != EstadoPartida.EmAndamento) return;

            // Pega todos os países do tabuleiro
            var todosOsPaises = _tabuleiro.SelectMany(c => c.Paises).ToList();

            // Pega o ID do dono do primeiro país da lista
            var primeiroDonoId = todosOsPaises.First().JogadorDonoId;

            // Se o primeiro país não tiver dono, o jogo obviamente não acabou
            if (!primeiroDonoId.HasValue) return;

            //  Verifica se TODOS os outros países têm exatamente o mesmo dono do primeiro
            bool todosDoMesmoDono = todosOsPaises.All(p => p.JogadorDonoId == primeiroDonoId);

            if (todosDoMesmoDono)
            {
                EstadoAtual = EstadoPartida.Finalizada;
                Vencedor = _jogadores.FirstOrDefault(j => j.Id == primeiroDonoId);
            }
        }

        private void DistribuirReforcos(Jogador jogador)
        {
            if (jogador == null)
                throw new ArgumentNullException(nameof(jogador));

            int qtdPaises = _tabuleiro
                .SelectMany(c => c.Paises)
                .Count(p => p.JogadorDonoId == jogador.Id);

            int novosSoldados = Math.Max(3, qtdPaises / 3);

            foreach (var continente in _tabuleiro)
            {
                if (continente.JogadorDominaContinente(jogador.Id))
                    novosSoldados += continente.BonusDeTropas;
            }

            jogador.AdicionarTropasLivres(novosSoldados);
        }

        private static void ConectarPaises(Pais paisA, Pais paisB)
        {
            if (paisA == null)
                throw new ArgumentNullException(nameof(paisA));
            if (paisB == null)
                throw new ArgumentNullException(nameof(paisB));

            paisA.AdicionarFronteira(paisB.Id);
            paisB.AdicionarFronteira(paisA.Id);
        }

    }
}
