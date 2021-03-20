namespace RG_PSI_PZ3.Model
{
    public class PowerEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public override string ToString()
        {
            return $"Id={Id}, Name={Name}, Type={GetType()}";
        }
    }
}