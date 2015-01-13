// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class EventSymbolKey : AbstractSymbolKey<EventSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;

            internal EventSymbolKey(IEventSymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingType, visitor);
                this.metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var containerInfo = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var events = GetAllSymbols<INamedTypeSymbol>(containerInfo).SelectMany(t => t.GetMembers(this.metadataName)).OfType<IEventSymbol>();
                return CreateSymbolInfo(events);
            }

            internal override bool Equals(EventSymbolKey other, ComparisonOptions options)
            {
                return
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    other.containerKey.Equals(this.containerKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    GetHashCode(options.IgnoreCase, this.metadataName),
                    this.containerKey.GetHashCode(options));
            }
        }
    }
}