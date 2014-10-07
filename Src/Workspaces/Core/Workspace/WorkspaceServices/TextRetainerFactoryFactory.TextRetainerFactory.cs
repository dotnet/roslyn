using Roslyn.Compilers;

namespace Roslyn.Services.Host
{
    internal partial class TextRetainerFactoryFactory
    {
        private class TextRetainerFactory : CostBasedRetainerFactory<IText>
        {
            // 4M chars * 2bytes/char = 8 MB
            private const long DefaultSize = 1 << 22;
            private const int DefaultTextCount = 8;

            public TextRetainerFactory()
                : this(DefaultSize, DefaultTextCount)
            {
            }

            public TextRetainerFactory(
                long maxTextSize = DefaultSize,
                int minTextCount = DefaultTextCount)
                : base(itemCost: t => t.Length, maxCost: maxTextSize, minCount: minTextCount)
            {
            }
        }
    }
}