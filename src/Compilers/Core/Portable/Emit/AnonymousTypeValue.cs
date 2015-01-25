// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    [DebuggerDisplay("{Name, nq}")]
    internal struct AnonymousTypeValue
    {
        public readonly string Name;
        public readonly int UniqueIndex;
        public readonly ITypeDefinition Type;

        public AnonymousTypeValue(string name, int uniqueIndex, ITypeDefinition type)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(uniqueIndex >= 0);

            this.Name = name;
            this.UniqueIndex = uniqueIndex;
            this.Type = type;
        }
    }
}
