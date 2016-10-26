using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Information about all the declarations defined within a document.  Each declaration in the
    /// document get a single item in <see cref="IDeclarationInfo.DeclaredSymbolInfos"/>.
    /// </summary>
    internal interface IDeclarationInfo
    {
        IReadOnlyList<DeclaredSymbolInfo> DeclaredSymbolInfos { get; }
    }
}