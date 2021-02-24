// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
