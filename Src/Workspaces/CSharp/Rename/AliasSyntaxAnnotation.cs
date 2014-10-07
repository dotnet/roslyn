using System;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    [Serializable]
    internal class AliasSyntaxAnnotation : SyntaxAnnotation
    {
        public readonly string AliasName;

        public AliasSyntaxAnnotation(string aliasName)
        {
            this.AliasName = aliasName;
        }
    }
}
