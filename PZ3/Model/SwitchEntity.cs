namespace RG_PSI_PZ3.Model
{
    public class SwitchEntity : PowerEntity
    {
        public string Status { get; set; }

        public override string ToString()
        {
            return $"Id={Id}, Name={Name}, Status={Status}, Type={GetType()}";
        }
    }
}