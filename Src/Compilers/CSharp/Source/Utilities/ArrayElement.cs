namespace Roslyn.Compilers.CSharp
{
    internal struct ArrayElement<T>
    {
        internal T Value;

        internal ArrayElement(T value)
        {
            this.Value = value;
        }

        public static implicit operator ArrayElement<T>(T value)
        {
            return new ArrayElement<T>(value);
        }
    }
}