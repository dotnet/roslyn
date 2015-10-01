using Microsoft.VisualStudio.Language.Intellisense;
using System;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class CompletionListSelectedEventArgs
    {
        public readonly int? newValue;
        public readonly int? oldValue;

        public CompletionListSelectedEventArgs(int? oldValue, int? newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }
    }
}