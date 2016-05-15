using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IDeclarationInfo
    {
        IReadOnlyList<DeclaredSymbolInfo> DeclaredSymbolInfos { get; }
    }
}