namespace Risk.Domain
{
    public class Jogador
    {
        public Guid Id {get; private set;}
        public string Nome {get; private set;}
        public bool EstaAtivo {get; private set;}
        public int TropasLivres { get; private set; }
        private readonly List<Carta> _cartas = new List<Carta>();
        public IReadOnlyCollection<Carta> Cartas => _cartas.AsReadOnly();


        public Jogador(string nome)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            EstaAtivo = true;
        }

        public void Eliminar()
        {
            EstaAtivo = false;
        }

        public void ReceberCarta(Carta carta) => _cartas.Add(carta);

        public void RemoverCartas(IEnumerable<string> nomesPaises)
        {
            _cartas.RemoveAll(c => nomesPaises.Contains(c.PaisNome));
        }

        public void AdicionarTropasLivres(int quantidade)
        {
            if (quantidade > 0) TropasLivres += quantidade;
        }

        public void ConsumirTropasLivres(int quantidade)
        {
            if (quantidade > TropasLivres)
                throw new InvalidOperationException("Você não tem tropas livres suficientes.");
            TropasLivres -= quantidade;
        }
    }

}