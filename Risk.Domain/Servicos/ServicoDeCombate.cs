namespace Risk.Domain.Servicos
{
    public class ResultadoBatalha
    {
        public List<int> DadosAtaque { get; set; } = new();
        public List<int> DadosDefesa { get; set; } = new();
    }

    public class ServicoDeCombate
    {
        private readonly IDado _dado;

        public ServicoDeCombate(IDado dado)
        {
            _dado = dado ?? throw new ArgumentNullException(nameof(dado));
        }

        public ResultadoBatalha ResolverBatalha (Pais atacante, Pais defensor, int tropasAtacantes, int tropasDefensoras)
        {
            if (!atacante.JogadorDonoId.HasValue)
                throw new InvalidOperationException("Um país neutro/sem dono não pode declarar ataque.");
            if (atacante.JogadorDonoId == defensor.JogadorDonoId)
                throw new InvalidOperationException("Você não pode atacar seu próprio país.");
            if (atacante.SaldoDeTropas <= tropasAtacantes)
                throw new InvalidOperationException("O país atacante precisa manter pelo menos 1 tropa na retaguarda.");
            if (tropasAtacantes < 1 || tropasAtacantes > 3)
                throw new InvalidOperationException("O ataque deve ser feito com 1 a 3 tropas.");
            if (tropasDefensoras < 1 || tropasDefensoras > 2)
                throw new InvalidOperationException("A defesa deve ser feita com 1 ou 2 tropas.");

            var dadosAtaque = RolarDados(tropasAtacantes);
            var dadosDefesa = RolarDados(tropasDefensoras);
            var resultado = new ResultadoBatalha
            {
                DadosAtaque = dadosAtaque,
                DadosDefesa = dadosDefesa
            };

            int batalhasParaComparar = Math.Min(dadosAtaque.Count, dadosDefesa.Count);

            for (int i = 0; i < batalhasParaComparar; i++)
            {
                if (dadosAtaque[i] > dadosDefesa[i])
                {
                    defensor.RemoverTropas(1, permitirZero: true);
                }
                else
                {
                    atacante.RemoverTropas(1, permitirZero: false);
                }
                
            }
            if (defensor.SaldoDeTropas == 0)
            {
                defensor.Conquistar(atacante.JogadorDonoId.Value, tropasAtacantes);
                atacante.RemoverTropas(tropasAtacantes);

            }

            return resultado;
        }

        private List<int> RolarDados(int quantidade)
        {
            var resultados = new List<int>();
            for (int i = 0; i < quantidade; i++)
            {
                resultados.Add(_dado.Rolar());
            }
            
            return resultados.OrderByDescending(x => x).ToList();

        }

    }
}
