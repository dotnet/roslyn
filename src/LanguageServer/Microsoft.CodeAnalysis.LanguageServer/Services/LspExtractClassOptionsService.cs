// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IExtractClassOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspExtractClassOptionsService() : IExtractClassOptionsService
{
    public ExtractClassOptions? GetExtractClassOptions(
        Document document,
        INamedTypeSymbol originalType,
        ImmutableArray<ISymbol> selectedMembers,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        var symbolsToUse = selectedMembers.IsEmpty
            ? originalType.GetMembers().Where(member => member switch
            {
                IMethodSymbol methodSymbol => methodSymbol.MethodKind == MethodKind.Ordinary,
                IFieldSymbol fieldSymbol => !fieldSymbol.IsImplicitlyDeclared,
                _ => member.Kind == SymbolKind.Property || member.Kind == SymbolKind.Event
            })
            : selectedMembers;

        var memberAnalysisResults = symbolsToUse.SelectAsArray(m => new ExtractClassMemberAnalysisResult(m, makeAbstract: false));
        const string name = "NewBaseType";
        var extension = document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb";
        return new(
            name + extension,
            name,
            true,
            memberAnalysisResults);
    }
}
