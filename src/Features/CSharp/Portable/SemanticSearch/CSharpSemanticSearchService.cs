// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch.CSharp;

[ExportLanguageService(typeof(ISemanticSearchService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSemanticSearchService() : AbstractSemanticSearchService()
{
    protected override Compilation CreateCompilation(
        SourceText query,
        IEnumerable<MetadataReference> references,
        SolutionServices services,
        out SyntaxTree queryTree,
        CancellationToken cancellationToken)
    {
        var syntaxTreeFactory = services.GetRequiredLanguageService<ISyntaxTreeFactoryService>(LanguageNames.CSharp);

        var globalUsingsTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: null,
            CSharpSemanticSearchUtilities.ParseOptions,
            SemanticSearchUtilities.CreateSourceText(CSharpSemanticSearchUtilities.Configuration.GlobalUsings),
            cancellationToken);

        queryTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: SemanticSearchUtilities.QueryDocumentName,
            CSharpSemanticSearchUtilities.ParseOptions,
            query,
            cancellationToken);

        return CSharpCompilation.Create(
            assemblyName: SemanticSearchUtilities.QueryProjectName,
            [queryTree, globalUsingsTree],
            references,
            CSharpSemanticSearchUtilities.CompilationOptions);
    }
}

#endif
