// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private struct DeclarationInfo
        {
            public ImmutableArray<DeclaredSymbolInfo> DeclaredSymbolInfos { get; }

            public DeclarationInfo(ImmutableArray<DeclaredSymbolInfo> declaredSymbolInfos)
            {
                DeclaredSymbolInfos = declaredSymbolInfos;
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(DeclaredSymbolInfos.Length);
                foreach (var declaredSymbolInfo in DeclaredSymbolInfos)
                {
                    declaredSymbolInfo.WriteTo(writer);
                }
            }

            public static DeclarationInfo? TryReadFrom(StringTable stringTable, ObjectReader reader)
            {
                try
                {
                    var declaredSymbolCount = reader.ReadInt32();
                    var builder = ImmutableArray.CreateBuilder<DeclaredSymbolInfo>(declaredSymbolCount);
                    for (var i = 0; i < declaredSymbolCount; i++)
                    {
                        builder.Add(DeclaredSymbolInfo.ReadFrom_ThrowsOnFailure(stringTable, reader));
                    }

                    return new DeclarationInfo(builder.MoveToImmutable());
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
