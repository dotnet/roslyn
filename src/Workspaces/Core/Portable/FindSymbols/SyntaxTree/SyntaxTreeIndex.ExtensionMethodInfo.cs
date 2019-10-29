// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            // We divide extension methods into two categories, simple and complex, for filtering purpose.
            // Whether a method is simple is determined based on if we can determine it's target type easily
            // with a pure text matching. For complex methods, we will need to rely on symbol to decide if it's 
            // feasible.
            //
            // Complex methods include:
            // - Method declared in the document which includes using alias directive
            // - Generic method where the target type is a type-paramter (e.g. List<T> would be considered simple, not complex)
            // - If the target type name is one of the following (i.e. name of the type for the first parameter) 
            //      1. Array type
            //      2. ValueTuple type
            //      3. Pointer type
            //
            // The rest of methods are considered simple.

            /// <summary>
            /// Name of the simple method's target type name to the index of its DeclaredSymbolInfo in `_declarationInfo`.
            /// All predefined types are converted to its metadata form. e.g. int => Int32. For generic types, type parameters are ignored.
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<int>> SimpleExtensionMethodInfo { get; }

            /// <summary>
            /// Indices of to all complex methods' DeclaredSymbolInfo in `_declarationInfo`.
            /// </summary>
            public readonly ImmutableArray<int> ComplexExtensionMethodInfo { get; }

            public ExtensionMethodInfo(
                ImmutableDictionary<string, ImmutableArray<int>> simpleExtensionMethodInfo,
                ImmutableArray<int> complexExtensionMethodInfo)
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
