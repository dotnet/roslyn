using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract class QuickInfoElementProvider
    {
        public abstract Task<QuickInfoData> GetQuickInfoElementAsync(Document document, int position, CancellationToken cancellationToken);
    }
}