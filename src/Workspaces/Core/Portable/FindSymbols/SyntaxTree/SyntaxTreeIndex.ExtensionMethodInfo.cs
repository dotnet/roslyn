﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            // Whether a method is simple is determined based on if we can determine it's receiver type easily
            // with a pure text matching. For complex methods, we will need to rely on symbol to decide if it's 
            // feasible.
            //
            // Complex methods include:
            // - Method declared in the document which includes using alias directive
            // - Generic method where the receiver type is a type-paramter (e.g. List<T> would be considered simple, not complex)
            // - If the receiver type name is Pointer type (i.e. name of the type for the first parameter) 
            //
            // The rest of methods are considered simple.

            /// <summary>
            /// Name of the extension method's receiver type to the index of its DeclaredSymbolInfo in `_declarationInfo`.
            /// 
            /// For simple types, the receiver type name is it's metadata name. All predefined types are converted to its metadata form.
            /// e.g. int => Int32. For generic types, type parameters are ignored.
            /// 
            /// For complex types, the receiver type name is "".
            /// 
            /// For any kind of array types, it's "{element's receiver type name}[]".
            /// e.g. 
            /// int[][,] => "Int32[]"
            /// T (where T is a type parameter) => ""
            /// T[,] (where T is a type parameter) => "T[]"
            /// </summary>
            public readonly ImmutableDictionary<string, ImmutableArray<int>> ReceiverTypeNameToExtensionMethodMap { get; }

            public bool ContainsExtensionMethod => !ReceiverTypeNameToExtensionMethodMap.IsEmpty;

            public ExtensionMethodInfo(ImmutableDictionary<string, ImmutableArray<int>> receiverTypeNameToExtensionMethodMap)
            {
                ReceiverTypeNameToExtensionMethodMap = receiverTypeNameToExtensionMethodMap;
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(ReceiverTypeNameToExtensionMethodMap.Count);

                foreach (var (name, indices) in ReceiverTypeNameToExtensionMethodMap)
                {
                    writer.WriteString(name);
                    writer.WriteInt32(indices.Length);

                    foreach (var declaredSymbolInfoIndex in indices)
                        writer.WriteInt32(declaredSymbolInfoIndex);
                }
            }

            public static ExtensionMethodInfo? TryReadFrom(ObjectReader reader)
            {
                try
                {
                    var receiverTypeNameToExtensionMethodMapBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<int>>();
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

                        receiverTypeNameToExtensionMethodMapBuilder[typeName] = arrayBuilder.ToImmutableAndFree();
                    }

                    return new ExtensionMethodInfo(receiverTypeNameToExtensionMethodMapBuilder.ToImmutable());
                }
                catch (Exception)
                {
                }

                return null;
            }
        }
    }
}
