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
        private class NullSymbolKey : AbstractSymbolKey<NullSymbolKey>
        {
            public static readonly NullSymbolKey Instance = new NullSymbolKey();

            [JsonConstructor]
            internal NullSymbolKey()
            {
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                return default(SymbolKeyResolution);
            }

            internal override bool Equals(NullSymbolKey other, ComparisonOptions options)
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