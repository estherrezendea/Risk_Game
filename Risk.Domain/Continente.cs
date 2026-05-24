namespace Risk.Domain
{
    public class Continente
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public int BonusDeTropas { get; private set; }
        private readonly List<Pais> _paises;
        public IReadOnlyCollection<Pais> Paises => _paises.AsReadOnly();

        public Continente(string nome, int bonusDeTropas)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("O continente precisa de um nome válido.");

            Id = Guid.NewGuid();
            Nome = nome;
            BonusDeTropas = bonusDeTropas;
            _paises = new List<Pais>();
        }

        public void AdicionarPais(Pais pais)
        {
            if (pais == null) throw new ArgumentNullException(nameof(pais));
            _paises.Add(pais);
        }

        public bool JogadorDominaContinente(Guid jogadorId)
        {
            if (_paises.Count == 0) return false;
            foreach (var pais in _paises)
            {
                if (pais.JogadorDonoId != jogadorId)
                    return false;
            }
            return true;

        }
    }
}