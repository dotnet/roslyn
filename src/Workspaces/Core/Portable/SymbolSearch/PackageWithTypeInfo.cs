// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal class PackageWithTypeInfo : PackageInfo
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string TypeName;
        public readonly string Version;

        public PackageWithTypeInfo(
            PackageSource source,
            string packageName,
            string typeName,
            string version,
            int rank,
            IReadOnlyList<string> containingNamespaceNames)
            : base(source, packageName, rank)
        {
            TypeName = typeName;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }
}