// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal class CommitHashAttribute : Attribute
    {
        internal string _hash;
        public CommitHashAttribute(string hash)
        {
            _hash = hash;
        }
    }
}
