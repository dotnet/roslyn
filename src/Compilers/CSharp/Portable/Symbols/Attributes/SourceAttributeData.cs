﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a Source custom attribute specification
    /// </summary>
    internal class SourceAttributeData : CSharpAttributeData
    {
        private readonly NamedTypeSymbol? _attributeClass;
        private readonly MethodSymbol? _attributeConstructor; // Only null when error. Use MemberNotNull when available https://github.com/dotnet/roslyn/issues/41964
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<int> _constructorArgumentsSourceIndices;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;
        private readonly bool _isConditionallyOmitted;
        private readonly bool _hasErrors;
        private readonly SyntaxReference? _applicationNode;

        internal SourceAttributeData(
            SyntaxReference? applicationNode,
            NamedTypeSymbol? attributeClass,
            MethodSymbol? attributeConstructor,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<int> constructorArgumentsSourceIndices,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments,
            bool hasErrors,
            bool isConditionallyOmitted)
        {
            Debug.Assert(!isConditionallyOmitted || attributeClass is object && attributeClass.IsConditional);
            Debug.Assert(!constructorArguments.IsDefault);
            Debug.Assert(!namedArguments.IsDefault);
            Debug.Assert(constructorArgumentsSourceIndices.IsDefault ||
                constructorArgumentsSourceIndices.Any() && constructorArgumentsSourceIndices.Length == constructorArguments.Length);
            Debug.Assert(attributeConstructor is object || hasErrors);

            _attributeClass = attributeClass;
            _attributeConstructor = attributeConstructor;
            _constructorArguments = constructorArguments;
            _constructorArgumentsSourceIndices = constructorArgumentsSourceIndices;
            _namedArguments = namedArguments;
            _isConditionallyOmitted = isConditionallyOmitted;
            _hasErrors = hasErrors;
            _applicationNode = applicationNode;
        }

        internal SourceAttributeData(SyntaxReference applicationNode, NamedTypeSymbol attributeClass, MethodSymbol? attributeConstructor, bool hasErrors)
            : this(
            applicationNode,
            attributeClass,
            attributeConstructor,
            constructorArguments: ImmutableArray<TypedConstant>.Empty,
            constructorArgumentsSourceIndices: default,
            namedArguments: ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty,
            hasErrors: hasErrors,
            isConditionallyOmitted: false)
        {
        }

        public override NamedTypeSymbol? AttributeClass
        {
            get
            {
                return _attributeClass;
            }
        }

        public override MethodSymbol? AttributeConstructor
        {
            get
            {
                return _attributeConstructor;
            }
        }

        public override SyntaxReference? ApplicationSyntaxReference
        {
            get
            {
                return _applicationNode;
            }
        }

        /// <summary>
        /// If the <see cref="CSharpAttributeData.ConstructorArguments"/> contains any named constructor arguments or default value arguments,
        /// it returns an array representing each argument's source argument index. A value of -1 indicates default value argument.
        /// Otherwise, returns null.
        /// </summary>
        internal ImmutableArray<int> ConstructorArgumentsSourceIndices
        {
            get
            {
                return _constructorArgumentsSourceIndices;
            }
        }

        internal CSharpSyntaxNode GetAttributeArgumentSyntax(int parameterIndex, AttributeSyntax attributeSyntax)
        {
            // This method is only called when decoding (non-erroneous) well-known attributes.
            Debug.Assert(!this.HasErrors);
            Debug.Assert(this.AttributeConstructor is object);
            Debug.Assert(parameterIndex >= 0);
            Debug.Assert(parameterIndex < this.AttributeConstructor.ParameterCount);
            Debug.Assert(attributeSyntax != null);

            if (_constructorArgumentsSourceIndices.IsDefault)
            {
                // We have no named ctor arguments AND no default arguments.
                Debug.Assert(attributeSyntax.ArgumentList != null);
                Debug.Assert(this.AttributeConstructor.ParameterCount <= attributeSyntax.ArgumentList.Arguments.Count);

                return attributeSyntax.ArgumentList.Arguments[parameterIndex];
            }
            else
            {
                int sourceArgIndex = _constructorArgumentsSourceIndices[parameterIndex];

                if (sourceArgIndex == -1)
                {
                    // -1 signifies optional parameter whose default argument is used.
                    Debug.Assert(this.AttributeConstructor.Parameters[parameterIndex].IsOptional);
                    return attributeSyntax.Name;
                }
                else
                {
                    Debug.Assert(sourceArgIndex >= 0);
                    Debug.Assert(sourceArgIndex < attributeSyntax.ArgumentList!.Arguments.Count);
                    return attributeSyntax.ArgumentList.Arguments[sourceArgIndex];
                }
            }
        }

        internal override bool IsConditionallyOmitted
        {
            get
            {
                return _isConditionallyOmitted;
            }
        }

        internal SourceAttributeData WithOmittedCondition(bool isConditionallyOmitted)
        {
            if (this.IsConditionallyOmitted == isConditionallyOmitted)
            {
                return this;
            }
            else
            {
                return new SourceAttributeData(this.ApplicationSyntaxReference, this.AttributeClass, this.AttributeConstructor, this.CommonConstructorArguments,
                    this.ConstructorArgumentsSourceIndices, this.CommonNamedArguments, this.HasErrors, isConditionallyOmitted);
            }
        }

        internal override bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
        }

        protected internal sealed override ImmutableArray<TypedConstant> CommonConstructorArguments
        {
            get { return _constructorArguments; }
        }

        protected internal sealed override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments
        {
            get { return _namedArguments; }
        }

        /// <summary>
        /// This method finds an attribute by metadata name and signature. The algorithm for signature matching is similar to the one
        /// in Module.GetTargetAttributeSignatureIndex. Note, the signature matching is limited to primitive types
        /// and System.Type.  It will not match an arbitrary signature but it is sufficient to match the signatures of the current set of
        /// well known attributes.
        /// </summary>
        /// <param name="targetSymbol">The symbol which is the target of the attribute</param>
        /// <param name="description">The attribute to match.</param>
        internal override int GetTargetAttributeSignatureIndex(Symbol targetSymbol, AttributeDescription description)
        {
            if (!IsTargetAttribute(description.Namespace, description.Name))
            {
                return -1;
            }

            var ctor = this.AttributeConstructor;

            // Ensure that the attribute data really has a constructor before comparing the signature.
            if (ctor is null)
            {
                return -1;
            }

            // Lazily loaded System.Type type symbol
            TypeSymbol? lazySystemType = null;

            ImmutableArray<ParameterSymbol> parameters = ctor.Parameters;

            for (int signatureIndex = 0; signatureIndex < description.Signatures.Length; signatureIndex++)
            {
                byte[] targetSignature = description.Signatures[signatureIndex];

                if (matches(targetSignature, parameters, ref lazySystemType))
                {
                    return signatureIndex;
                }
            }

            return -1;

            bool matches(byte[] targetSignature, ImmutableArray<ParameterSymbol> parameters, ref TypeSymbol? lazySystemType)
            {
                if (targetSignature[0] != (byte)SignatureAttributes.Instance)
                {
                    return false;
                }

                byte parameterCount = targetSignature[1];
                if (parameterCount != parameters.Length)
                {
                    return false;
                }

                if ((SignatureTypeCode)targetSignature[2] != SignatureTypeCode.Void)
                {
                    return false;
                }

                int parameterIndex = 0;
                for (int signatureByteIndex = 3; signatureByteIndex < targetSignature.Length; signatureByteIndex++)
                {
                    if (parameterIndex >= parameters.Length)
                    {
                        return false;
                    }

                    TypeSymbol parameterType = parameters[parameterIndex].Type;
                    SpecialType specType = parameterType.SpecialType;
                    byte targetType = targetSignature[signatureByteIndex];

                    if (targetType == (byte)SignatureTypeCode.TypeHandle)
                    {
                        signatureByteIndex++;

                        if (parameterType.Kind != SymbolKind.NamedType && parameterType.Kind != SymbolKind.ErrorType)
                        {
                            return false;
                        }

                        var namedType = (NamedTypeSymbol)parameterType;
                        AttributeDescription.TypeHandleTargetInfo targetInfo = AttributeDescription.TypeHandleTargets[targetSignature[signatureByteIndex]];

                        // Compare name and containing symbol name. Uses HasNameQualifier
                        // extension method to avoid string allocations.
                        if (!string.Equals(namedType.MetadataName, targetInfo.Name, System.StringComparison.Ordinal) ||
                            !namedType.HasNameQualifier(targetInfo.Namespace))
                        {
                            return false;
                        }

                        targetType = (byte)targetInfo.Underlying;

                        if (parameterType.IsEnumType())
                        {
                            specType = parameterType.GetEnumUnderlyingType()!.SpecialType;
                        }
                    }
                    else if (targetType != (byte)SignatureTypeCode.SZArray && parameterType.IsArray())
                    {
                        if (targetSignature[signatureByteIndex - 1] != (byte)SignatureTypeCode.SZArray)
                        {
                            return false;
                        }

                        specType = ((ArrayTypeSymbol)parameterType).ElementType.SpecialType;
                    }

                    switch (targetType)
                    {
                        case (byte)SignatureTypeCode.Boolean:
                            if (specType != SpecialType.System_Boolean)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Char:
                            if (specType != SpecialType.System_Char)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.SByte:
                            if (specType != SpecialType.System_SByte)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Byte:
                            if (specType != SpecialType.System_Byte)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Int16:
                            if (specType != SpecialType.System_Int16)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt16:
                            if (specType != SpecialType.System_UInt16)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Int32:
                            if (specType != SpecialType.System_Int32)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt32:
                            if (specType != SpecialType.System_UInt32)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Int64:
                            if (specType != SpecialType.System_Int64)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt64:
                            if (specType != SpecialType.System_UInt64)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Single:
                            if (specType != SpecialType.System_Single)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Double:
                            if (specType != SpecialType.System_Double)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.String:
                            if (specType != SpecialType.System_String)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.Object:
                            if (specType != SpecialType.System_Object)
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SerializationTypeCode.Type:
                            lazySystemType ??= GetSystemType(targetSymbol);

                            if (!TypeSymbol.Equals(parameterType, lazySystemType, TypeCompareKind.ConsiderEverything))
                            {
                                return false;
                            }
                            parameterIndex += 1;
                            break;

                        case (byte)SignatureTypeCode.SZArray:
                            // Skip over and check the next byte
                            if (!parameterType.IsArray())
                            {
                                return false;
                            }
                            break;

                        default:
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the System.Type type symbol from targetSymbol's containing assembly.
        /// </summary>
        /// <param name="targetSymbol">Target symbol on which this attribute is applied.</param>
        /// <returns>System.Type type symbol.</returns>
        internal virtual TypeSymbol GetSystemType(Symbol targetSymbol)
        {
            return targetSymbol.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type);
        }
    }
}
