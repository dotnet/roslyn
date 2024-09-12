// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private const int RootNodeParentIndex = -1;

        /// <summary>
        /// <see cref="BuilderNode"/>s are produced when initially creating our indices.
        /// They store Names of symbols and the index of their parent symbol.  When we
        /// produce the final <see cref="SymbolTreeInfo"/> though we will then convert
        /// these to <see cref="Node"/>s.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private readonly struct BuilderNode(string name, int parentIndex, MultiDictionary<MetadataNode, ParameterTypeInfo>.ValueSet parameterTypeInfos = default)
        {
            public static readonly BuilderNode RootNode = new("", RootNodeParentIndex, default);

            public readonly string Name = name;
            public readonly int ParentIndex = parentIndex;
            public readonly MultiDictionary<MetadataNode, ParameterTypeInfo>.ValueSet ParameterTypeInfos = parameterTypeInfos;

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            private string GetDebuggerDisplay()
                => Name + ", " + ParentIndex;
        }

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private readonly struct Node(string name, int parentIndex)
        {
            /// <summary>
            /// The Name of this Node.
            /// </summary>
            public readonly string Name = name;

            /// <summary>
            /// Index in <see cref="_nodes"/> of the parent Node of this Node.
            /// Value will be <see cref="RootNodeParentIndex"/> if this is the 
            /// Node corresponding to the root symbol.
            /// </summary>
            public readonly int ParentIndex = parentIndex;

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            public void AssertEquivalentTo(Node node)
            {
                Debug.Assert(node.Name == this.Name);
                Debug.Assert(node.ParentIndex == this.ParentIndex);
            }

            private string GetDebuggerDisplay()
                => Name + ", " + ParentIndex;
        }

        private readonly record struct ParameterTypeInfo(string name, bool isComplex, bool isArray)
        {
            /// <summary>
            /// This is the type name of the parameter when <see cref="IsComplexType"/> is false. 
            /// For array types, this is just the element type name.
            /// e.g. `int` for `int[][,]` 
            /// </summary>
            public readonly string Name = name;

            /// <summary>
            /// Indicate if the type of parameter is any kind of array.
            /// This is relevant for both simple and complex types. For example:
            /// - array of simple type like int[], int[][], int[][,], etc. are all ultimately represented as "int[]" in index.
            /// - array of complex type like T[], T[][], etc are all represented as "[]" in index, 
            ///   in contrast to just "" for non-array types.
            /// </summary>
            public readonly bool IsArray = isArray;

            /// <summary>
            /// Similar to <see cref="TopLevelSyntaxTreeIndex.ExtensionMethodInfo"/>, we divide extension methods into
            /// simple and complex categories for filtering purpose. Whether a method is simple is determined based on
            /// if we can determine it's receiver type easily with a pure text matching. For complex methods, we will
            /// need to rely on symbol to decide if it's feasible.
            /// 
            /// Simple types include:
            /// - Primitive types
            /// - Types which is not a generic method parameter
            /// - By reference type of any types above
            /// - Array types with element of any types above
            /// </summary>
            public readonly bool IsComplexType = isComplex;
        }

        public readonly record struct ExtensionMethodInfo(string fullyQualifiedContainerName, string name)
        {
            /// <summary>
            /// Name of the extension method. 
            /// This can be used to retrieve corresponding symbols via <see cref="INamespaceOrTypeSymbol.GetMembers(string)"/>
            /// </summary>
            public readonly string Name = name;

            /// <summary>
            /// Fully qualified name for the type that contains this extension method.
            /// </summary>
            public readonly string FullyQualifiedContainerName = fullyQualifiedContainerName;
        }

        private sealed class ParameterTypeInfoProvider : ISignatureTypeProvider<ParameterTypeInfo, object?>
        {
            public static readonly ParameterTypeInfoProvider Instance = new();

            private static ParameterTypeInfo ComplexInfo
                => new(string.Empty, isComplex: true, isArray: false);

            public ParameterTypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode)
                => new(typeCode.ToString(), isComplex: false, isArray: false);

            public ParameterTypeInfo GetGenericInstantiation(ParameterTypeInfo genericType, ImmutableArray<ParameterTypeInfo> typeArguments)
                => genericType.IsComplexType
                    ? ComplexInfo
                    : new ParameterTypeInfo(genericType.Name, isComplex: false, isArray: false);

            public ParameterTypeInfo GetByReferenceType(ParameterTypeInfo elementType)
                => elementType;

            public ParameterTypeInfo GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var type = reader.GetTypeDefinition(handle);
                var name = reader.GetString(type.Name);
                return new ParameterTypeInfo(name, isComplex: false, isArray: false);
            }

            public ParameterTypeInfo GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var type = reader.GetTypeReference(handle);
                var name = reader.GetString(type.Name);
                return new ParameterTypeInfo(name, isComplex: false, isArray: false);
            }

            public ParameterTypeInfo GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<ParameterTypeInfo, object?>(Instance, reader, genericContext).DecodeType(ref sigReader);
            }

            public ParameterTypeInfo GetArrayType(ParameterTypeInfo elementType, ArrayShape shape) => GetArrayTypeInfo(elementType);

            public ParameterTypeInfo GetSZArrayType(ParameterTypeInfo elementType) => GetArrayTypeInfo(elementType);

            private static ParameterTypeInfo GetArrayTypeInfo(ParameterTypeInfo elementType)
                => elementType.IsComplexType
                    ? new ParameterTypeInfo(string.Empty, isComplex: true, isArray: true)
                    : new ParameterTypeInfo(elementType.Name, isComplex: false, isArray: true);

            public ParameterTypeInfo GetFunctionPointerType(MethodSignature<ParameterTypeInfo> signature) => ComplexInfo;

            public ParameterTypeInfo GetGenericMethodParameter(object? genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetGenericTypeParameter(object? genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetModifiedType(ParameterTypeInfo modifier, ParameterTypeInfo unmodifiedType, bool isRequired) => ComplexInfo;

            public ParameterTypeInfo GetPinnedType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetPointerType(ParameterTypeInfo elementType) => ComplexInfo;
        }
    }
}
