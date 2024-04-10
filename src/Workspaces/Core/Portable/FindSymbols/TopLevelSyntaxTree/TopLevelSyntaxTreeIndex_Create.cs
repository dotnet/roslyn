// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class TopLevelSyntaxTreeIndex
{
    private static TopLevelSyntaxTreeIndex CreateIndex(
        ProjectState project, SyntaxNode root, Checksum checksum, CancellationToken cancellationToken)
    {
        var infoFactory = project.LanguageServices.GetRequiredService<IDeclaredSymbolInfoFactoryService>();

        using var _1 = ArrayBuilder<DeclaredSymbolInfo>.GetInstance(out var declaredSymbolInfos);
        using var _2 = PooledDictionary<string, ArrayBuilder<int>>.GetInstance(out var extensionMethodInfo);
        try
        {
            infoFactory.AddDeclaredSymbolInfos(
                project, root, declaredSymbolInfos, extensionMethodInfo, cancellationToken);

            return new TopLevelSyntaxTreeIndex(
                checksum,
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
