using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    public struct BraceMatchingResult
    {
        public TextSpan LeftSpan { get; }
        public TextSpan RightSpan { get; }

        public BraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
            : this()
        {
            this.LeftSpan = leftSpan;
            this.RightSpan = rightSpan;
        }
    }

    public interface IFSharpBraceMatcher
    {
        Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default);
    }
}
