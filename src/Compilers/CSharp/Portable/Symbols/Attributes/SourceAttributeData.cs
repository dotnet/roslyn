// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly NamedTypeSymbol _attributeClass;
        private readonly MethodSymbol _attributeConstructor;
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<int> _constructorArgumentsSourceIndices;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;
        private readonly bool _isConditionallyOmitted;
        private readonly bool _hasErrors;
        private readonly SyntaxReference _applicationNode;

        internal SourceAttributeData(
            SyntaxReference applicationNode,
            NamedTypeSymbol attributeClass,
            MethodSymbol attributeConstructor,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<int> constructorArgumentsSourceIndices,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments,
            bool hasErrors,
            bool isConditionallyOmitted)
        {
            Debug.Assert(!isConditionallyOmitted || (object)attributeClass != null && attributeClass.IsConditional);
            Debug.Assert(!constructorArguments.IsDefault);
            Debug.Assert(!namedArguments.IsDefault);
            Debug.Assert(constructorArgumentsSourceIndices.IsDefault ||
                constructorArgumentsSourceIndices.Any() && constructorArgumentsSourceIndices.Length == constructorArguments.Length);

            _attributeClass = attributeClass;
            _attributeConstructor = attributeConstructor;
            _constructorArguments = constructorArguments;
            _constructorArgumentsSourceIndices = constructorArgumentsSourceIndices;
            _namedArguments = namedArguments;
            _isConditionallyOmitted = isConditionallyOmitted;
            _hasErrors = hasErrors;
            _applicationNode = applicationNode;
        }

        internal SourceAttributeData(SyntaxReference applicationNode, NamedTypeSymbol attributeClass, MethodSymbol attributeConstructor, bool hasErrors)
            : this(
            applicationNode,
            attributeClass,
            attributeConstructor,
            constructorArguments: ImmutableArray<TypedConstant>.Empty,
            constructorArgumentsSourceIndices: default(ImmutableArray<int>),
            namedArguments: ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty,
            hasErrors: hasErrors,
            isConditionallyOmitted: false)
        {
        }

        public override NamedTypeSymbol AttributeClass
        {
            get
            {
                return _attributeClass;
            }
        }

        public override MethodSymbol AttributeConstructor
        {
            get
            {
                return _attributeConstructor;
            }
        }

        public override SyntaxReference ApplicationSyntaxReference
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
            Debug.Assert((object)this.AttributeConstructor != null);
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
                    Debug.Assert(sourceArgIndex < attributeSyntax.ArgumentList.Arguments.Count);
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

        internal protected sealed override ImmutableArray<TypedConstant> CommonConstructorArguments
        {
            get { return _constructorArguments; }
        }

        internal protected sealed override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments
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
            if ((object)ctor == null)
            {
                return -1;
            }

            // Lazily loaded System.Type type symbol
            TypeSymbol lazySystemType = null;

            ImmutableArray<ParameterSymbol> parameters = ctor.Parameters;
            bool foundMatch = false;

            for (int i = 0; i < description.Signatures.Length; i++)
            {
                byte[] targetSignature = description.Signatures[i];
                if (targetSignature[0] != (byte)SignatureAttributes.Instance)
                {
                    continue;
                }

                byte parameterCount = targetSignature[1];
                if (parameterCount != parameters.Length)
                {
                    continue;
                }

                if ((SignatureTypeCode)targetSignature[2] != SignatureTypeCode.Void)
                {
                    continue;
                }

                foundMatch = (targetSignature.Length == 3);
                int k = 0;
                for (int j = 3; j < targetSignature.Length; j++)
                {
                    if (k >= parameters.Length)
                    {
                        break;
                    }

                    TypeSymbol parameterType = parameters[k].Type.TypeSymbol;
                    SpecialType specType = parameterType.SpecialType;
                    byte targetType = targetSignature[j];

                    if (targetType == (byte)SignatureTypeCode.TypeHandle)
                    {
                        j++;

                        if (parameterType.Kind != SymbolKind.NamedType && parameterType.Kind != SymbolKind.ErrorType)
                        {
                            foundMatch = false;
                            break;
                        }

                        var namedType = (NamedTypeSymbol)parameterType;
                        AttributeDescription.TypeHandleTargetInfo targetInfo = AttributeDescription.TypeHandleTargets[targetSignature[j]];

                        // Compare name and containing symbol name. Uses HasNameQualifier
                        // extension method to avoid string allocations.
                        if (!string.Equals(namedType.MetadataName, targetInfo.Name, System.StringComparison.Ordinal) ||
                            !namedType.HasNameQualifier(targetInfo.Namespace))
                        {
                            foundMatch = false;
                            break;
                        }

                        targetType = (byte)targetInfo.Underlying;

                        if (parameterType.IsEnumType())
                        {
                            specType = parameterType.GetEnumUnderlyingType().SpecialType;
                        }
                    }
                    else if (parameterType.IsArray())
                    {
                        specType = ((ArrayTypeSymbol)parameterType).ElementType.SpecialType;
                    }

                    switch (targetType)
                    {
                        case (byte)SignatureTypeCode.Boolean:
                            foundMatch = specType == SpecialType.System_Boolean;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Char:
                            foundMatch = specType == SpecialType.System_Char;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.SByte:
                            foundMatch = specType == SpecialType.System_SByte;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Byte:
                            foundMatch = specType == SpecialType.System_Byte;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Int16:
                            foundMatch = specType == SpecialType.System_Int16;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt16:
                            foundMatch = specType == SpecialType.System_UInt16;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Int32:
                            foundMatch = specType == SpecialType.System_Int32;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt32:
                            foundMatch = specType == SpecialType.System_UInt32;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Int64:
                            foundMatch = specType == SpecialType.System_Int64;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.UInt64:
                            foundMatch = specType == SpecialType.System_UInt64;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Single:
                            foundMatch = specType == SpecialType.System_Single;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Double:
                            foundMatch = specType == SpecialType.System_Double;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.String:
                            foundMatch = specType == SpecialType.System_String;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.Object:
                            foundMatch = specType == SpecialType.System_Object;
                            k += 1;
                            break;

                        case (byte)SerializationTypeCode.Type:
                            if ((object)lazySystemType == null)
                            {
                                lazySystemType = GetSystemType(targetSymbol);
                            }

                            foundMatch = parameterType == lazySystemType;
                            k += 1;
                            break;

                        case (byte)SignatureTypeCode.SZArray:
                            // Skip over and check the next byte
                            foundMatch = parameterType.IsArray();
                            break;

                        default:
                            return -1;
                    }

                    if (!foundMatch)
                    {
                        break;
                    }
                }

                if (foundMatch)
                {
                    return i;
                }
            }

            Debug.Assert(!foundMatch);
            return -1;
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
