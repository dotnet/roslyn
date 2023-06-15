// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class TopLevelSyntaxTreeIndex
    {
        private readonly struct DeclarationInfo(ImmutableArray<DeclaredSymbolInfo> declaredSymbolInfos)
        {
            public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos { get; } = declaredSymbolInfos;

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(DeclaredSymbolInfos.Length);
                foreach (var declaredSymbolInfo in DeclaredSymbolInfos)
                    declaredSymbolInfo.WriteTo(writer);
            }

            public static DeclarationInfo? TryReadFrom(StringTable stringTable, ObjectReader reader)
            {
                try
                {
                    var declaredSymbolCount = reader.ReadInt32();
                    using var _ = ArrayBuilder<DeclaredSymbolInfo>.GetInstance(declaredSymbolCount, out var builder);
                    for (var i = 0; i < declaredSymbolCount; i++)
                        builder.Add(DeclaredSymbolInfo.ReadFrom_ThrowsOnFailure(stringTable, reader));

                    return new DeclarationInfo(builder.ToImmutableAndClear());
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
