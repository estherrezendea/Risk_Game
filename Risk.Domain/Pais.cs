namespace Risk.Domain
{
    public class Pais
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public Guid? JogadorDonoId { get; private set; }
        public int SaldoDeTropas { get; private set; }
        private readonly HashSet<Guid> _vizinhosIds = new HashSet<Guid>();
        public IReadOnlyCollection<Guid> VizinhosIds => _vizinhosIds;

        public Pais(string nome)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            SaldoDeTropas = 0;
        }

        public void Conquistar (Guid novoDonoId, int tropasIniciais)
        {
            if (tropasIniciais <= 0)
                throw new ArgumentException("É necessário pelo menos uma tropa para ocupar o país.");

            JogadorDonoId = novoDonoId;
            SaldoDeTropas = tropasIniciais;
        }

        public void AdicionarTropas (int quantidade)
        {
            if (quantidade <= 0) return;
            SaldoDeTropas += quantidade;
        }

        public void RemoverTropas (int quantidade, bool permitirZero = false)
        {
            if (quantidade <= 0)
                throw new ArgumentException("A quantidade de tropas para remoção deve ser maior que zero.");

            if (quantidade > SaldoDeTropas)
                throw new InvalidOperationException($"O país {Nome} não tem tropas suficientes para remover.");

            if (!permitirZero && SaldoDeTropas - quantidade < 1)
                throw new InvalidOperationException($"O país {Nome} deve manter pelo menos 1 tropa na defesa.");

            SaldoDeTropas -= quantidade;
        }

        public void AdicionarFronteira(Guid vizinhoId)
        {
            if (vizinhoId == Guid.Empty)
                throw new ArgumentException("ID de vizinho inválido.");
            if (vizinhoId == Id)
                throw new ArgumentException("Um país não pode fazer fronteira com ele mesmo.");

            _vizinhosIds.Add(vizinhoId);
        }

        public bool FazFronteiraCom(Guid outroPaisId)
        {
            return _vizinhosIds.Contains(outroPaisId);
        }
    }
}
