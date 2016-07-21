// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationNamespaceInfo
    {
        private static readonly ConditionalWeakTable<INamespaceSymbol, CodeGenerationNamespaceInfo> s_namespaceToInfoMap =
            new ConditionalWeakTable<INamespaceSymbol, CodeGenerationNamespaceInfo>();

        private readonly IList<ISymbol> _imports;

        private CodeGenerationNamespaceInfo(IList<ISymbol> imports)
        {
            _imports = imports;
        }

        public static void Attach(
            INamespaceSymbol @namespace,
            IList<ISymbol> imports)
        {
            var info = new CodeGenerationNamespaceInfo(imports ?? SpecializedCollections.EmptyList<ISymbol>());
            s_namespaceToInfoMap.Add(@namespace, info);
        }

        private static CodeGenerationNamespaceInfo GetInfo(INamespaceSymbol @namespace)
        {
            CodeGenerationNamespaceInfo info;
            s_namespaceToInfoMap.TryGetValue(@namespace, out info);
            return info;
        }

        public static IList<ISymbol> GetImports(INamespaceSymbol @namespace)
        {
            return GetImports(GetInfo(@namespace));
        }

        private static IList<ISymbol> GetImports(CodeGenerationNamespaceInfo info)
        {
            return info == null
                ? SpecializedCollections.EmptyList<ISymbol>()
                : info._imports;
        }
    }
}
