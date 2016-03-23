using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class CompletionItemFilter
    {
        public readonly Glyph Glyph;
        public readonly char AccessKey;

        public CompletionItemFilter(Glyph glyph, char accessKey)
        {
            this.Glyph = glyph;
            this.AccessKey = accessKey;
        }
    }
}
