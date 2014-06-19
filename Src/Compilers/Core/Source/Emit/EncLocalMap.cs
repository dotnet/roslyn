namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class EncLocalMap
    {
        public static readonly EncLocalMap Empty = new EmptyLocalMap();

        public abstract int PreviousLocalCount { get; }

        public abstract int GetPreviousLocalSlot(object identity);

        private sealed class EmptyLocalMap : EncLocalMap
        {
            public override int PreviousLocalCount
            {
                get { return 0; }
            }

            public override int GetPreviousLocalSlot(object identity)
            {
                return -1;
            }
        }
    }
}
