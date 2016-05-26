// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class DynamicTypeSymbolKey : AbstractSymbolKey<DynamicTypeSymbolKey>
        {
            internal static readonly DynamicTypeSymbolKey Instance = new DynamicTypeSymbolKey();

            [JsonConstructor]
            internal DynamicTypeSymbolKey()
            {
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                return new SymbolKeyResolution(compilation.DynamicType);
            }

            internal override bool Equals(DynamicTypeSymbolKey other, ComparisonOptions options)
            {
                return true;
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return RuntimeHelpers.GetHashCode(Instance);
            }
        }
    }
}