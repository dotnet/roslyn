// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Text;
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
        /// these to <see cref="Node"/>s.  Those nodes will not point to individual 
        /// strings, but will instead point at <see cref="_concatenatedNames"/>.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct BuilderNode
        {
            public static readonly BuilderNode RootNode = new BuilderNode("", RootNodeParentIndex, default);

            public readonly string Name;
            public readonly int ParentIndex;
            public readonly MultiDictionary<MetadataNode, ParameterTypeInfo>.ValueSet ParameterTypeInfos;

            public BuilderNode(string name, int parentIndex, MultiDictionary<MetadataNode, ParameterTypeInfo>.ValueSet parameterTypeInfos = default)
            {
                Name = name;
                ParentIndex = parentIndex;
                ParameterTypeInfos = parameterTypeInfos;
            }

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            private string GetDebuggerDisplay()
            {
                return Name + ", " + ParentIndex;
            }
        }

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct Node
        {
            /// <summary>
            /// Span in <see cref="_concatenatedNames"/> of the Name of this Node.
            /// </summary>
            public readonly TextSpan NameSpan;

            /// <summary>
            /// Index in <see cref="_nodes"/> of the parent Node of this Node.
            /// Value will be <see cref="RootNodeParentIndex"/> if this is the 
            /// Node corresponding to the root symbol.
            /// </summary>
            public readonly int ParentIndex;

            public Node(TextSpan wordSpan, int parentIndex)
            {
                NameSpan = wordSpan;
                ParentIndex = parentIndex;
            }

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            public void AssertEquivalentTo(Node node)
            {
                Debug.Assert(node.NameSpan == this.NameSpan);
                Debug.Assert(node.ParentIndex == this.ParentIndex);
            }

            private string GetDebuggerDisplay()
            {
                return NameSpan + ", " + ParentIndex;
            }
        }

        public readonly struct ParameterTypeInfo
        {
            public string Name { get; }
            public bool IsComplexType { get; }

            public ParameterTypeInfo(string name, bool isComplex)
            {
                Name = name;
                IsComplexType = isComplex;
            }

            public override string ToString()
                => $"{Name}, {IsComplexType}";
        }

        public readonly struct ExtensionMethodInfo
        {
            public string FullyQualifiedContainerName { get; }

            public string Name { get; }

            public ExtensionMethodInfo(string fullyQualifiedContainerName, string name)
            {
                FullyQualifiedContainerName = fullyQualifiedContainerName;
                Name = name;
            }
        }

        private sealed class ParameterTypeInfoProvider : ISignatureTypeProvider<ParameterTypeInfo, object>
        {
            public static readonly ParameterTypeInfoProvider Instance = new ParameterTypeInfoProvider();

            private static ParameterTypeInfo ComplexInfo
                => new ParameterTypeInfo(string.Empty, isComplex: true);

            public ParameterTypeInfo GetPrimitiveType(PrimitiveTypeCode typeCode)
                => new ParameterTypeInfo(typeCode.ToString(), isComplex: false);

            public ParameterTypeInfo GetArrayType(ParameterTypeInfo elementType, ArrayShape shape) => ComplexInfo;

            public ParameterTypeInfo GetSZArrayType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetGenericInstantiation(ParameterTypeInfo genericType, ImmutableArray<ParameterTypeInfo> typeArguments)
                => genericType.IsComplexType
                    ? ComplexInfo
                    : new ParameterTypeInfo(genericType.Name, isComplex: false);

            public ParameterTypeInfo GetByReferenceType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetFunctionPointerType(MethodSignature<ParameterTypeInfo> signature) => ComplexInfo;

            public ParameterTypeInfo GetGenericMethodParameter(object genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetGenericTypeParameter(object genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetModifiedType(ParameterTypeInfo modifier, ParameterTypeInfo unmodifiedType, bool isRequired) => ComplexInfo;

            public ParameterTypeInfo GetPinnedType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetPointerType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var type = reader.GetTypeDefinition(handle);
                var name = reader.GetString(type.Name);
                return new ParameterTypeInfo(name, isComplex: false);
            }

            public ParameterTypeInfo GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var type = reader.GetTypeReference(handle);
                var name = reader.GetString(type.Name);
                return new ParameterTypeInfo(name, isComplex: false);
            }

            public ParameterTypeInfo GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<ParameterTypeInfo, object>(Instance, reader, genericContext).DecodeType(ref sigReader);
            }
        }
    }
}
