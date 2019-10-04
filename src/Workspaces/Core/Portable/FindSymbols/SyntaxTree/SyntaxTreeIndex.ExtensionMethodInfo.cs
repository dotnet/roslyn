// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIndex
    {
        private readonly struct ExtensionMethodInfo
        {
            // Target type name to index of DeclaredSymbolInfo
            // All type predefind types are converted to its metadata form. e.g. int => Int32
            public readonly ImmutableDictionary<string, ImmutableArray<int>> SimpleExtensionMethodInfo { get; }
            public readonly ImmutableArray<int> ComplexExtensionMethodInfo { get; }

            public ExtensionMethodInfo(ImmutableDictionary<string, ImmutableArray<int>> simpleExtensionMethodInfo, ImmutableArray<int> complexExtensionMethodInfo)
            {
                SimpleExtensionMethodInfo = simpleExtensionMethodInfo;
                ComplexExtensionMethodInfo = complexExtensionMethodInfo;
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(SimpleExtensionMethodInfo.Count);

                foreach (var kvp in SimpleExtensionMethodInfo)
                {
                    writer.WriteString(kvp.Key);
                    writer.WriteInt32(kvp.Value.Length);

                    foreach (var declaredSymbolInfoIndex in kvp.Value)
                    {
                        writer.WriteInt32(declaredSymbolInfoIndex);
                    }
                }

                writer.WriteInt32(ComplexExtensionMethodInfo.Length);
                foreach (var declaredSymbolInfoIndex in ComplexExtensionMethodInfo)
                {
                    writer.WriteInt32(declaredSymbolInfoIndex);
                }
            }

            public static ExtensionMethodInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var simpleExtensionMethodInfo = ImmutableDictionary.CreateBuilder<string, ImmutableArray<int>>();
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; ++i)
                    {
                        var typeName = reader.ReadString();
                        var arrayLength = reader.ReadInt32();
                        var arrayBuilder = ArrayBuilder<int>.GetInstance(arrayLength);

                        for (var j = 0; j < arrayLength; ++j)
                        {
                            var declaredSymbolInfoIndex = reader.ReadInt32();
                            arrayBuilder.Add(declaredSymbolInfoIndex);
                        }

                        simpleExtensionMethodInfo[typeName] = arrayBuilder.ToImmutableAndFree();
                    }

                    count = reader.ReadInt32();
                    var complexExtensionMethodInfo = ArrayBuilder<int>.GetInstance(count);
                    for (var i = 0; i < count; ++i)
                    {
                        var declaredSymbolInfoIndex = reader.ReadInt32();
                        complexExtensionMethodInfo.Add(declaredSymbolInfoIndex);
                    }

                    return new ExtensionMethodInfo(simpleExtensionMethodInfo.ToImmutable(), complexExtensionMethodInfo.ToImmutableAndFree());
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
