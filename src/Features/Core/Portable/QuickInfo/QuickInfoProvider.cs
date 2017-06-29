using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// A provider that produces <see cref="QuickInfoItem"/>'s. 
    /// Providers are used with some <see cref="QuickInfoService"/> implementations.
    /// </summary>
    internal abstract class QuickInfoProvider
    {
        /// <summary>
        /// Gets the <see cref="QuickInfoItem"/> for the position.
        /// </summary>
        /// <returns>The <see cref="QuickInfoItem"/> or null if no item is available.</returns>
        public abstract Task<QuickInfoItem> GetQuickInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}