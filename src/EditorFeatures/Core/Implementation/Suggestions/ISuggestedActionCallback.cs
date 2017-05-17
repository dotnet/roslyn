using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal interface ISuggestedActionCallback
    {
        void OnSuggestedActionExecuted(SuggestedAction action);
    }
}
