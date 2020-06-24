using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineParamNameHints
{
    class InlineParamNameHintDataTag : ITag
    {
        public readonly string TagName;
        public InlineParamNameHintDataTag(string name)
        {
            TagName = name;
        }
    }
}
