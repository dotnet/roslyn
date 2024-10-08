// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationNamespaceInfo
{
    private static readonly ConditionalWeakTable<INamespaceSymbol, CodeGenerationNamespaceInfo> s_namespaceToInfoMap = new();

    private readonly IList<ISymbol> _imports;

    private CodeGenerationNamespaceInfo(IList<ISymbol> imports)
        => _imports = imports;

    public static void Attach(
        INamespaceSymbol @namespace,
        IList<ISymbol> imports)
    {
        var info = new CodeGenerationNamespaceInfo(imports ?? SpecializedCollections.EmptyList<ISymbol>());
        s_namespaceToInfoMap.Add(@namespace, info);
    }

    private static CodeGenerationNamespaceInfo GetInfo(INamespaceSymbol @namespace)
    {
        s_namespaceToInfoMap.TryGetValue(@namespace, out var info);
        return info;
    }

    public static IList<ISymbol> GetImports(INamespaceSymbol @namespace)
        => GetImports(GetInfo(@namespace));

    private static IList<ISymbol> GetImports(CodeGenerationNamespaceInfo info)
    {
        return info == null ? [] : info._imports;
    }
}
