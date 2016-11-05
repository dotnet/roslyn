// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal abstract class PackageInfoResult : PackageInfo
    {
        public readonly int Rank;

        protected PackageInfoResult(PackageSource source, string packageName, int rank)
            : base(source, packageName)
        {
            Rank = rank;
        }
    }
}