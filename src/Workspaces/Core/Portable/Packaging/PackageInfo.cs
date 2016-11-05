// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Packaging
{
    internal class PackageInfo
    {
        public readonly PackageSource Source;
        public readonly string PackageName;

        public PackageInfo(PackageSource source, string packageName)
        {
            Source = source;
            PackageName = packageName;
        }
    }
}