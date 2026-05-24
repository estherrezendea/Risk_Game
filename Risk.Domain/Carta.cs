using Risk.Shared;

namespace Risk.Domain
{
    public class Carta
    {
        public string PaisNome { get; private set; }
        public TipoTropa Tipo { get; private set; }

        public Carta(string paisNome, TipoTropa tipo)
        {
            PaisNome = paisNome;
            Tipo = tipo;
        }
    }
}