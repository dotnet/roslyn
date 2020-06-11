// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
