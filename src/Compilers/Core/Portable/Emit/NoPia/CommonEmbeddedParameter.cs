// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal abstract partial class EmbeddedTypesManager<
        TPEModuleBuilder,
        TModuleCompilationState,
        TEmbeddedTypesManager,
        TSyntaxNode,
        TAttributeData,
        TSymbol,
        TAssemblySymbol,
        TNamedTypeSymbol,
        TFieldSymbol,
        TMethodSymbol,
        TEventSymbol,
        TPropertySymbol,
        TParameterSymbol,
        TTypeParameterSymbol,
        TEmbeddedType,
        TEmbeddedField,
        TEmbeddedMethod,
        TEmbeddedEvent,
        TEmbeddedProperty,
        TEmbeddedParameter,
        TEmbeddedTypeParameter>
    {
        internal abstract class CommonEmbeddedParameter : Cci.IParameterDefinition
        {
            public readonly CommonEmbeddedMember ContainingPropertyOrMethod;
            public readonly TParameterSymbol UnderlyingParameter;
            private ImmutableArray<TAttributeData> _lazyAttributes;

            protected CommonEmbeddedParameter(CommonEmbeddedMember containingPropertyOrMethod, TParameterSymbol underlyingParameter)
            {
                this.ContainingPropertyOrMethod = containingPropertyOrMethod;
                this.UnderlyingParameter = underlyingParameter;
            }

            protected TEmbeddedTypesManager TypeManager
            {
                get
                {
                    return ContainingPropertyOrMethod.TypeManager;
                }
            }

            protected abstract bool HasDefaultValue { get; }
            protected abstract Cci.IMetadataConstant GetDefaultValue(EmitContext context);
            protected abstract bool IsIn { get; }
            protected abstract bool IsOut { get; }
            protected abstract bool IsOptional { get; }
            protected abstract bool IsMarshalledExplicitly { get; }
            protected abstract Cci.IMarshallingInformation MarshallingInformation { get; }
            protected abstract ImmutableArray<byte> MarshallingDescriptor { get; }
            protected abstract string Name { get; }
            protected abstract Cci.IParameterTypeInformation UnderlyingParameterTypeInformation { get; }
            protected abstract ushort Index { get; }
            protected abstract IEnumerable<TAttributeData> GetCustomAttributesToEmit(TModuleCompilationState compilationState);

            private bool IsTargetAttribute(TAttributeData attrData, AttributeDescription description)
            {
                return TypeManager.IsTargetAttribute(UnderlyingParameter, attrData, description);
            }

            private ImmutableArray<TAttributeData> GetAttributes(TModuleCompilationState compilationState, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                var builder = ArrayBuilder<TAttributeData>.GetInstance();

                // Copy some of the attributes.

                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                foreach (var attrData in GetCustomAttributesToEmit(compilationState))
                {
                    if (IsTargetAttribute(attrData, AttributeDescription.ParamArrayAttribute))
                    {
                        if (attrData.CommonConstructorArguments.Length == 0)
                        {
                            builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_ParamArrayAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else if (IsTargetAttribute(attrData, AttributeDescription.DateTimeConstantAttribute))
                    {
                        if (attrData.CommonConstructorArguments.Length == 1)
                        {
                            builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_DateTimeConstantAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                        }
                    }
                    else
                    {
                        int signatureIndex = TypeManager.GetTargetAttributeSignatureIndex(UnderlyingParameter, attrData, AttributeDescription.DecimalConstantAttribute);
                        if (signatureIndex != -1)
                        {
                            Debug.Assert(signatureIndex == 0 || signatureIndex == 1);

                            if (attrData.CommonConstructorArguments.Length == 5)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(
                                    signatureIndex == 0 ? WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor :
                                        WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctorByteByteInt32Int32Int32,
                                    attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                        else if (IsTargetAttribute(attrData, AttributeDescription.DefaultParameterValueAttribute))
                        {
                            if (attrData.CommonConstructorArguments.Length == 1)
                            {
                                builder.AddOptional(TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_DefaultParameterValueAttribute__ctor, attrData, syntaxNodeOpt, diagnostics));
                            }
                        }
                    }
                }

                return builder.ToImmutableAndFree();
            }

            bool Cci.IParameterDefinition.HasDefaultValue
            {
                get
                {
                    return HasDefaultValue;
                }
            }

            Cci.IMetadataConstant Cci.IParameterDefinition.GetDefaultValue(EmitContext context)
            {
                return GetDefaultValue(context);
            }

            bool Cci.IParameterDefinition.IsIn
            {
                get
                {
                    return IsIn;
                }
            }

            bool Cci.IParameterDefinition.IsOut
            {
                get
                {
                    return IsOut;
                }
            }

            bool Cci.IParameterDefinition.IsOptional
            {
                get
                {
                    return IsOptional;
                }
            }

            bool Cci.IParameterDefinition.IsMarshalledExplicitly
            {
                get
                {
                    return IsMarshalledExplicitly;
                }
            }

            Cci.IMarshallingInformation Cci.IParameterDefinition.MarshallingInformation
            {
                get
                {
                    return MarshallingInformation;
                }
            }

            ImmutableArray<byte> Cci.IParameterDefinition.MarshallingDescriptor
            {
                get
                {
                    return MarshallingDescriptor;
                }
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                if (_lazyAttributes.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var attributes = GetAttributes((TModuleCompilationState)context.ModuleBuilder.CommonModuleCompilationState, (TSyntaxNode)context.SyntaxNodeOpt, diagnostics);

                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyAttributes, attributes))
                    {
                        // Save any diagnostics that we encountered.
                        context.Diagnostics.AddRange(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyAttributes;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                throw ExceptionUtilities.Unreachable;
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            string Cci.INamedEntity.Name
            {
                get { return Name; }
            }

            ImmutableArray<Cci.ICustomModifier> Cci.IParameterTypeInformation.CustomModifiers
            {
                get
                {
                    return UnderlyingParameterTypeInformation.CustomModifiers;
                }
            }

            bool Cci.IParameterTypeInformation.IsByReference
            {
                get
                {
                    return UnderlyingParameterTypeInformation.IsByReference;
                }
            }

            ushort Cci.IParameterTypeInformation.CountOfCustomModifiersPrecedingByRef
            {
                get
                {
                    return UnderlyingParameterTypeInformation.CountOfCustomModifiersPrecedingByRef;
                }
            }

            Cci.ITypeReference Cci.IParameterTypeInformation.GetType(EmitContext context)
            {
                return UnderlyingParameterTypeInformation.GetType(context);
            }

            ushort Cci.IParameterListEntry.Index
            {
                get
                {
                    return Index;
                }
            }

            /// <remarks>
            /// This is only used for testing.
            /// </remarks>
            public override string ToString()
            {
                return ((ISymbol)UnderlyingParameter).ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
            }
        }
    }
}
