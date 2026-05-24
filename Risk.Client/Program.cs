using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Risk.Shared.Protos;

Console.WriteLine("=======================================");
Console.WriteLine("      RISK DISTRIBUÍDO - CLIENTE       ");
Console.WriteLine("=======================================\n");

Console.Write("Digite o nome do seu General: ");
string nomeJogador = Console.ReadLine() ?? "Anônimo";

using var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new JogoRisk.JogoRiskClient(channel);
var request = new ConectarRequest { NomeJogador = nomeJogador };

Console.WriteLine("\n[REDE] Conectando ao Quartel General (Servidor)...");

try
{
    var response = await client.ConectarAsync(request);
    if (!response.Sucesso)
    {
        Console.WriteLine($"\n❌ [FALHA] Não foi possível entrar no jogo: {response.Mensagem}");
        return; 
    }

    string meuId = response.JogadorId;
    Console.WriteLine($"\n✅ [SUCESSO] {response.Mensagem}");
    Console.WriteLine($"🔑 Seu ID: {meuId}");
    
    bool jogando = true;
    while (jogando)
    {
        Console.WriteLine("\n--- MENU DE AÇÕES ---");
        Console.WriteLine("1 - Ver Tabuleiro (Estado do Jogo)");
        Console.WriteLine("2 - Atacar Território");
        Console.WriteLine("3 - Passar Turno");
        Console.WriteLine("4 - Trocar Cartas");
        Console.WriteLine("5 - Reforçar País");
        Console.WriteLine("0 - Sair");
        Console.Write("Escolha sua estratégia: ");
        
        string opcao = Console.ReadLine() ?? "";

        switch (opcao)
        {
            case "1":
                await VerTabuleiro(client, meuId);
                break;
            case "2":
                await ExecutarAtaque(client, meuId);
                break;
            case "3":
                await PassarTurno(client, meuId);
                break;
            case "0":
                jogando = false;
                break;
            case "4":
                await ExecutarTroca(client, meuId);
                break;
            case "5":
                await ExecutarReforco(client, meuId);
                break;
            default:
                Console.WriteLine("Opção inválida, General.");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n🚨 [ERRO DE REDE] Verifique se o Servidor está rodando.");
    Console.WriteLine($"Detalhe: {ex.Message}");
}

async Task VerTabuleiro(JogoRisk.JogoRiskClient grpcClient, string id)
{
    var resposta = await grpcClient.ObterEstadoAsync(new ObterEstadoRequest { JogadorId = id });
    
    Console.WriteLine($"\n--- ESTADO DO JOGO ({resposta.FaseAtual}) ---");
    
    if (resposta.FaseAtual != "Aguardando Jogadores")
    {
        string deQuemEAVez = (resposta.IdJogadorAtual == id) ? "SUA VEZ!" : $"Vez de {resposta.NomeJogadorAtual}";
        Console.WriteLine($"Turno Atual: {deQuemEAVez}");
    }

    Console.WriteLine("\n--- TERRITÓRIOS ---");
    foreach (var pais in resposta.Paises)
    {
        Console.WriteLine($"- {pais.Nome} | Dono: {pais.NomeDono} | Infantarias: {pais.Infantarias} | Cavalarias: {pais.Cavalarias}");
    }
    Console.WriteLine("\n--- SEUS RECURSOS ---");
    Console.WriteLine($"Cartas em mãos: {resposta.SuasCartas.Count}");
    Console.WriteLine($"Tropas livres disponíveis: {resposta.TropasLivres}");
    foreach(var carta in resposta.SuasCartas)
    {
        Console.WriteLine($"> País: {carta.PaisNome} | Tipo: {carta.TipoTropa}");
    }
}

async Task ExecutarAtaque(JogoRisk.JogoRiskClient grpcClient, string id)
{
    var estado = await grpcClient.ObterEstadoAsync(new ObterEstadoRequest { JogadorId = id });
    if (estado.IdJogadorAtual != id)
    {
        Console.WriteLine("\n❌ Não é sua vez de jogar. Aguarde o seu turno.");
        return;
    }

    Console.Write("\nDe qual país você quer atacar? ");
    string atacante = Console.ReadLine() ?? "";
    
    Console.Write("Qual país você quer invadir? ");
    string defensor = Console.ReadLine() ?? "";
    
    Console.Write("Com quantas tropas (1 a 3)? ");
    if (!int.TryParse(Console.ReadLine(), out int tropas) || tropas < 1 || tropas > 3)
    {
        Console.WriteLine("Quantidade inválida.");
        return;
    }

    var request = new AtacarRequest
    {
        JogadorId = id,
        PaisAtacante = atacante,
        PaisDefensor = defensor,
        QtdTropas = tropas
    };

    try
    {
        var resposta = await grpcClient.AtacarAsync(request);
        Console.WriteLine("\n--- RELATÓRIO DE BATALHA ---");
        
        if (resposta.DadosAtaque.Count > 0)
            Console.WriteLine($"Dados de ataque: {string.Join(", ", resposta.DadosAtaque)}");
             
        if (resposta.DadosDefesa.Count > 0)
            Console.WriteLine($"Dados de defesa: {string.Join(", ", resposta.DadosDefesa)}");

        Console.WriteLine($"Resultado: {resposta.Mensagem}");
    }
    catch (Grpc.Core.RpcException ex)
    {
        Console.WriteLine($"\n❌ Ação Negada pelo Servidor: {ex.Status.Detail}");
    }
}

async Task PassarTurno(JogoRisk.JogoRiskClient grpcClient, string id)
{
    try
    {
        var resposta = await grpcClient.PassarTurnoAsync(new PassarTurnoRequest { JogadorId = id });
        if (resposta.Sucesso)
            Console.WriteLine("\n✅ Você encerrou o seu turno.");
        else
            Console.WriteLine($"\n❌ {resposta.Mensagem}");
    }
    catch (Grpc.Core.RpcException ex)
    {
         Console.WriteLine($"\n❌ Ação Negada: {ex.Status.Detail}");
    }
}

async Task ExecutarTroca(JogoRisk.JogoRiskClient grpcClient, string id)
{
    Console.WriteLine("\n--- TROCA DE CARTAS ---");
    Console.WriteLine("Digite o nome dos 3 países das cartas que deseja trocar (separados por vírgula):");
    Console.WriteLine("Exemplo: Brasil, México, Peru");
    
    string input = Console.ReadLine() ?? "";
    var paises = input.Split(',').Select(p => p.Trim()).ToList();

    if (paises.Count != 3)
    {
        Console.WriteLine("❌ Você deve informar exatamente 3 nomes de países.");
        return;
    }

    var request = new TrocarCartasRequest { JogadorId = id };
    request.PaisesDasCartas.AddRange(paises);

    try
    {
        var resposta = await grpcClient.TrocarCartasAsync(request);
        if (resposta.Sucesso)
            Console.WriteLine($"\n✅ {resposta.Mensagem}");
        else
            Console.WriteLine($"\n❌ {resposta.Mensagem}");
    }
    catch (Grpc.Core.RpcException ex)
    {
        Console.WriteLine($"\n❌ Ação Negada pelo Servidor: {ex.Status.Detail}");
    }
}

static async Task ExecutarReforco(JogoRisk.JogoRiskClient grpcClient, string id)
{
    Console.WriteLine("\n--- REFORÇO DE PAÍS ---");
    Console.Write("Nome do país que receberá reforço: ");
    string paisNome = Console.ReadLine() ?? "";

    Console.Write("Quantidade de tropas livres para alocar: ");
    if (!int.TryParse(Console.ReadLine(), out int quantidade))
    {
        Console.WriteLine("❌ Quantidade inválida.");
        return;
    }

    var request = new ReforcarPaisRequest
    {
        JogadorId = id,
        PaisNome = paisNome,
        Quantidade = quantidade
    };

    try
    {
        var resposta = await grpcClient.ReforcarPaisAsync(request);
        if (resposta.Sucesso)
            Console.WriteLine($"\n✅ {resposta.Mensagem}");
        else
            Console.WriteLine($"\n❌ {resposta.Mensagem}");
    }
    catch (Grpc.Core.RpcException ex)
    {
        Console.WriteLine($"\n❌ Ação Negada pelo Servidor: {ex.Status.Detail}");
    }

}