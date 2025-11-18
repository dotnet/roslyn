// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class TopLevelSyntaxTreeIndex
{
    private static TopLevelSyntaxTreeIndex CreateIndex(
        ProjectState project, SyntaxTree tree, Checksum checksum, CancellationToken cancellationToken)
    {
        var languageServices = project.LanguageServices;
        var infoFactory = languageServices.GetRequiredService<IDeclaredSymbolInfoFactoryService>();
        var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();

        using var _1 = ArrayBuilder<DeclaredSymbolInfo>.GetInstance(out var declaredSymbolInfos);
        using var _2 = PooledDictionary<string, ArrayBuilder<int>>.GetInstance(out var extensionMethodInfo);
        try
        {
            var root = tree.GetRoot(cancellationToken);
            var isGeneratedCode = tree.IsGeneratedCode(analyzerOptions: null, syntaxFacts, cancellationToken);

            infoFactory.AddDeclaredSymbolInfos(
                project, root, declaredSymbolInfos, extensionMethodInfo, cancellationToken);

            return new TopLevelSyntaxTreeIndex(
                checksum,
                isGeneratedCode,
                new DeclarationInfo(declaredSymbolInfos.ToImmutable()),
                new ExtensionMethodInfo(
                    extensionMethodInfo.ToImmutableDictionary(
                        static kvp => kvp.Key,
                        static kvp => kvp.Value.ToImmutable())));
        }
        finally
        {
            foreach (var (_, builder) in extensionMethodInfo)
                builder.Free();
        }
    }
}
