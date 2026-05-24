namespace Risk.Domain
{
    public class Dado : IDado
    {
        public int Rolar()
        {
            return Random.Shared.Next(1, 7);
        }
    }
}