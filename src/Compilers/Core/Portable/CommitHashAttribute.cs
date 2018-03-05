// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    internal sealed class CommitHashAttribute : Attribute
    {
        internal readonly string Hash;
        public CommitHashAttribute(string hash)
        {
            Hash = hash;
        }
    }
}
