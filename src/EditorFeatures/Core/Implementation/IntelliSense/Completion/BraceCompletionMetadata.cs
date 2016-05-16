using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class BraceCompletionMetadata
    {
        public IEnumerable<char> OpeningBraces { get; }
        public IEnumerable<char> ClosingBraces { get; }
        public IEnumerable<string> ContentTypes { get; }

        public BraceCompletionMetadata(IReadOnlyDictionary<string, object> data)
        {
            OpeningBraces = data.GetEnumerableMetadata<char>(nameof(OpeningBraces));
            ClosingBraces = data.GetEnumerableMetadata<char>(nameof(ClosingBraces));
            ContentTypes = data.GetEnumerableMetadata<string>(nameof(ContentTypes));
        }
    }
}
