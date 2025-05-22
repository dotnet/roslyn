// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch.CSharp;

[ExportLanguageService(typeof(ISemanticSearchService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSemanticSearchService(IAsynchronousOperationListenerProvider listenerProvider)
    : AbstractSemanticSearchService(listenerProvider)
{
    protected override Compilation CreateCompilation(
        SourceText query,
        IEnumerable<MetadataReference> references,
        SolutionServices services,
        out SyntaxTree queryTree,
        CancellationToken cancellationToken)
    {
        var syntaxTreeFactory = services.GetRequiredLanguageService<ISyntaxTreeFactoryService>(LanguageNames.CSharp);

        var globalUsingsAndToolsTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: null,
            CSharpSemanticSearchUtilities.ParseOptions,
            SemanticSearchUtilities.CreateSourceText(CSharpSemanticSearchUtilities.Configuration.GlobalUsingsAndTools),
            cancellationToken);

        queryTree = syntaxTreeFactory.ParseSyntaxTree(
            filePath: SemanticSearchUtilities.QueryDocumentName,
            CSharpSemanticSearchUtilities.ParseOptions,
            query,
            cancellationToken);

        return CSharpCompilation.Create(
            assemblyName: SemanticSearchUtilities.QueryProjectName,
            [queryTree, globalUsingsAndToolsTree],
            references,
            CSharpSemanticSearchUtilities.CompilationOptions);
    }

    protected override ObjectFormatter ObjectFormatter
        => CSharpObjectFormatter.Instance;
}

#endif
