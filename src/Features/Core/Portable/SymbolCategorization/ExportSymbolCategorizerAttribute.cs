using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SymbolCategorization
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportSymbolCategorizerAttribute : ExportAttribute
    {
        public ExportSymbolCategorizerAttribute() : base(typeof(ISymbolCategorizer))
        {
        }
    }
}