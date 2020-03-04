﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private readonly struct ParameterTypeInfo
        {
            /// <summary>
            /// This is the type name of the parameter when <see cref="IsComplexType"/> is false.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Similar to <see cref="SyntaxTreeIndex.ExtensionMethodInfo"/>, we divide extension methods into simple 
            /// and complex categories for filtering purpose. Whether a method is simple is determined based on if we 
            /// can determine it's target type easily with a pure text matching. For complex methods, we will need to
            /// rely on symbol to decide if it's feasible.
            /// 
            /// Simple types include:
            /// - Primitive types
            /// - Types which is not a generic method parameter
            /// - By reference type of any types above
            /// </summary>
            public readonly bool IsComplexType;

            public ParameterTypeInfo(string name, bool isComplex)
            {
                Name = name;
                IsComplexType = isComplex;
            }
        }

        public readonly struct ExtensionMethodInfo
        {
            /// <summary>
            /// Name of the extension method. 
            /// This can be used to retrive corresponding symbols via <see cref="INamespaceOrTypeSymbol.GetMembers(string)"/>
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Fully qualified name for the type that contains this extension method.
            /// </summary>
            public readonly string FullyQualifiedContainerName;

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

            public ParameterTypeInfo GetGenericInstantiation(ParameterTypeInfo genericType, ImmutableArray<ParameterTypeInfo> typeArguments)
                => genericType.IsComplexType
                    ? ComplexInfo
                    : new ParameterTypeInfo(genericType.Name, isComplex: false);

            public ParameterTypeInfo GetByReferenceType(ParameterTypeInfo elementType)
                => elementType;

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

            public ParameterTypeInfo GetArrayType(ParameterTypeInfo elementType, ArrayShape shape) => ComplexInfo;

            public ParameterTypeInfo GetSZArrayType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetFunctionPointerType(MethodSignature<ParameterTypeInfo> signature) => ComplexInfo;

            public ParameterTypeInfo GetGenericMethodParameter(object genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetGenericTypeParameter(object genericContext, int index) => ComplexInfo;

            public ParameterTypeInfo GetModifiedType(ParameterTypeInfo modifier, ParameterTypeInfo unmodifiedType, bool isRequired) => ComplexInfo;

            public ParameterTypeInfo GetPinnedType(ParameterTypeInfo elementType) => ComplexInfo;

            public ParameterTypeInfo GetPointerType(ParameterTypeInfo elementType) => ComplexInfo;
        }
    }
}
