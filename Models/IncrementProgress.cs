namespace DioramaEngine.Models
{
    public class IncrementProgress : Progress<double>
    {
        public int MaximumValue { get; set; }
        public int CurrentValue { get; set; }
        public IncrementProgress(int maximumValue) : base()
        {
            MaximumValue = maximumValue;
        }

        public IncrementProgress(Action<double> handler, int maximumValue) : base(handler)
        {
            MaximumValue = maximumValue;
        }

        public void Increment()
        {
            OnReport((double)(1.0d / (MaximumValue - 1) * CurrentValue++));
        }

        public void IncreaseBy(int delta)
        {
            CurrentValue += delta;
            OnReport((double)(1.0d / (MaximumValue - 1) * CurrentValue));
        }
    }
}
