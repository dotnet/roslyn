// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Packaging
{
    internal abstract class PackageInfo
    {
        public readonly PackageSource Source;
        public readonly string PackageName;
        internal readonly int Rank;

        protected PackageInfo(PackageSource source, string packageName, int rank)
        {
            Source = source;
            PackageName = packageName;
            Rank = rank;
        }
    }
}