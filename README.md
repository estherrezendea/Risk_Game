# Risk Distribuído (.NET + gRPC)

Projeto acadêmico da disciplina de **Sistemas Distribuídos** que implementa um MVP de Risk usando **C#/.NET** e **gRPC** para simular invocação remota de métodos (equivalente conceitual ao Java RMI).

## Objetivo

Demonstrar, de forma simples e robusta:

- arquitetura em camadas (Domain, Shared, Server, Client);
- comunicação remota cliente-servidor com gRPC;
- coordenação de turnos entre dois clientes independentes;
- regras mínimas de jogo (territórios, tropas, combate com dados, cartas e reforço);
- segurança de concorrência no servidor para evitar inconsistências.

## Arquitetura do projeto

```text
RiskDistributed.sln
├── Risk.Domain   -> Entidades e regras de negócio (núcleo)
├── Risk.Shared   -> Contratos gRPC (.proto) e tipos compartilhados
├── Risk.Server   -> Serviço gRPC e orquestração do jogo
└── Risk.Client   -> Cliente Console interativo do jogador
```

### Responsabilidades por camada

- `Risk.Domain`
  - Entidades ricas: `Partida`, `Jogador`, `Pais`, `Continente`, `Carta`.
  - Regras de validação (fail fast) e integridade do jogo.
  - Serviço de combate com rolagem de dados.
- `Risk.Shared`
  - Contrato `risk.proto` com mensagens e RPCs.
  - Enum compartilhado `TipoTropa`.
- `Risk.Server`
  - Implementa o serviço `JogoRiskService`.
  - Mantém estado global da partida.
  - Controla concorrência com `lock` nas operações críticas.
- `Risk.Client`
  - Interface textual para conectar, visualizar estado e executar ações.

## Pré-requisitos

- .NET SDK **9.0** (ou compatível com `net9.0`)
- Windows, Linux ou macOS com terminal

Verificar instalação:

```bash
dotnet --version
```

## Como executar (passo a passo)

Use **3 terminais**: 1 para servidor e 2 para clientes.

### 1. Compilar a solução

Na raiz do projeto:

```bash
dotnet build RiskDistributed.sln
```

### 2. Iniciar o servidor gRPC

Terminal 1:

```bash
dotnet run --project Risk.Server
```

Servidor sobe em `http://localhost:5000` (gRPC sobre HTTP/2).

### 3. Iniciar o Cliente 1

Terminal 2:

```bash
dotnet run --project Risk.Client
```

Digite o nome do jogador quando solicitado.

### 4. Iniciar o Cliente 2

Terminal 3:

```bash
dotnet run --project Risk.Client
```

Digite o nome do segundo jogador.

Quando o segundo jogador conecta, a partida inicia automaticamente, os territórios são distribuídos e o turno é definido.

## Fluxo do jogo (como funciona)

## 1. Conexão

- RPC: `Conectar`
- Cada cliente recebe um `jogador_id` único.

## 2. Início automático com 2 jogadores

- Estado sai de `AguardandoJogadores`.
- Mapa é inicializado com 2 continentes de exemplo:
  - América do Sul: Brasil, Argentina, Peru
  - América do Norte: México, EUA, Canadá
- Territórios são distribuídos em round-robin.
- Cada país começa com **3 tropas** (permite ataques já no início).

## 3. Consulta de estado

- RPC: `ObterEstado`
- Cliente visualiza:
  - fase atual;
  - jogador do turno;
  - países e donos;
  - tropas por país em formato simplificado:
    - `Infantarias = Saldo % 5`
    - `Cavalarias = Saldo / 5`
  - cartas do jogador;
  - tropas livres disponíveis.

## 4. Ataque

- RPC: `Atacar`
- Regras principais:
  - só no próprio turno;
  - país atacante deve ser do jogador;
  - não pode atacar país próprio;
  - ataque com 1 a 3 tropas;
  - defensor rola até 2 dados;
  - comparação por ordem decrescente dos dados.
- Resultado inclui:
  - dados de ataque;
  - dados de defesa;
  - mensagem de resultado;
  - indicador de conquista.

## 5. Conquista e cartas

- Se defensor ficar com 0 tropas, território é conquistado.
- Ao conquistar, atacante recebe 1 carta aleatória (`Infantaria` ou `Cavalaria`).
- Após conquista, servidor verifica condição de vitória global.

## 6. Troca de cartas

- RPC: `TrocarCartas`
- MVP atual aceita troca de **3 cartas do mesmo tipo**.
- Bônus de tropas livres:
  - 1ª troca: 4 tropas
  - próximas: aumenta +2 a cada troca global da partida.

## 7. Reforço de país

- RPC: `ReforcarPais`
- Regras:
  - só no próprio turno;
  - país deve pertencer ao jogador;
  - quantidade deve ser > 0;
  - consome `TropasLivres` e adiciona no país.

## 8. Passar turno

- RPC: `PassarTurno`
- Avança para o próximo jogador ativo.

## Menu do cliente

No console do cliente:

- `1` Ver tabuleiro
- `2` Atacar território
- `3` Passar turno
- `4` Trocar cartas
- `5` Reforçar país
- `0` Sair

## Regras do nosso jogo vs Risk clássico

### Regras implementadas no projeto

- Partida com 2 jogadores conectados a um servidor central.
- Mapa com continentes e países; cada país tem dono e tropas.
- Tipos de tropa simplificados:
  - Infantaria = 1 ponto
  - Cavalaria = 5 pontos
- Turnos alternados com validação de autoria (somente o jogador da vez pode agir).
- Reforço automático no início do turno:
  - mínimo de 3 tropas
  - + bônus por continente totalmente dominado
- Ataque com dados:
  - atacante: até 3 dados
  - defensor: até 2 dados
  - empate favorece o defensor
- Conquista ocorre quando o defensor chega a 0 tropas.
- Cartas:
  - ao conquistar território, jogador recebe carta
  - troca de 3 cartas do mesmo tipo por tropas livres
- Reforço manual com tropas livres em países do próprio jogador.
- Vitória por domínio total do mapa (todos os países com o mesmo dono).

### Diferenças em relação ao Risk original (decisões de escopo)

- Mapa reduzido e de demonstração.
- Sem regra completa de adjacência global para ataque/fortificação.
- Sem fase formal de fortificação separada no fim do turno.
- Sistema de cartas simplificado (sem combinações completas/coringa).
- Foco em jogabilidade mínima e arquitetura distribuída estável.

### Justificativa acadêmica

Este projeto prioriza os elementos exigidos na disciplina (dados, cartas, países, continentes, tropas e disputa entre clientes), com simplificações controladas para reduzir complexidade e manter demonstrabilidade.

## Métodos gRPC disponíveis

Serviço `JogoRisk`:

- `Conectar`
- `ObterEstado`
- `Atacar`
- `PassarTurno`
- `TrocarCartas`
- `ReforcarPais`

Contrato em: `Risk.Shared/Protos/risk.proto`.

## Concorrência e consistência

O servidor usa seção crítica (`lock`) para proteger acesso ao estado compartilhado da partida em chamadas sensíveis (`ObterEstado`, `Atacar`, `PassarTurno`, `TrocarCartas`, `ReforcarPais`, `Conectar`).

Isso evita corridas quando dois clientes chamam o servidor ao mesmo tempo.

## Tratamento de erros

- Padrão fail fast no domínio e no serviço.
- Erros de regra retornam `Sucesso = false` com mensagem explicativa.
- Cliente mostra feedback claro para ação inválida.

## Limitações atuais do MVP

- Adjacência implementada de forma simplificada no mapa de exemplo (não é um mapa oficial completo do Risk).
- Sem interface gráfica (console apenas).
- Sem persistência em banco (estado em memória).
- Sem reconexão/sessão distribuída avançada.

## Possibilidades de evolução

- Adicionar grafo de adjacência e validação de fronteiras.
- Incluir mais continentes e mapa completo.
- Criar estratégia de reforço automático por continentes dominados.
- Adicionar autenticação de jogador e reconexão.
- Persistir partida em banco (ex.: PostgreSQL/Redis).
- Separar servidor de estado para múltiplas partidas simultâneas.

## Solução de problemas

- **Erro de conexão no cliente**: confirme se o servidor está rodando na porta `5000`.
- **Cliente não consegue agir**: provavelmente não é o turno dele; use `Ver Tabuleiro`.
- **Troca de cartas falha**: no MVP, as 3 cartas precisam ser do mesmo tipo.
- **Reforço falha**: verifique se há tropas livres suficientes e se o país pertence ao jogador.

## Comandos rápidos

```bash
# build
dotnet build RiskDistributed.sln

# servidor
dotnet run --project Risk.Server

# cliente (abrir em 2 terminais)
dotnet run --project Risk.Client
```

---
