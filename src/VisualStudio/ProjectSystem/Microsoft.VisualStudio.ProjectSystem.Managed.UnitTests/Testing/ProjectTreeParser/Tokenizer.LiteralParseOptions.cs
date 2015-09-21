using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Testing
{
    internal partial class Tokenizer
    {
        public enum LiteralParseOptions
        {
            None,
            AllowWhiteSpace,
            Required,
        }
    }
}
