// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class to represent all source method-like symbols. This includes
    /// things like ordinary methods and constructors, and functions
    /// like lambdas and local functions.
    /// </summary>
    internal abstract class SourceMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of clauses, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// If a type parameter does not have constraints, the corresponding entry in the array is null.
        /// </summary>
        public abstract ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses();

        protected static void ReportBadRefToken(TypeSyntax returnTypeSyntax, DiagnosticBag diagnostics)
        {
            if (!returnTypeSyntax.HasErrors)
            {
                var refKeyword = returnTypeSyntax.GetFirstToken();
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, refKeyword.GetLocation(), refKeyword.ToString());
            }
        }

        protected virtual CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            return CustomAttributesBag<CSharpAttributeData>.Empty;
        }

        protected virtual CustomAttributesBag<CSharpAttributeData> GetReturnTypeAttributesBag()
        {
            return CustomAttributesBag<CSharpAttributeData>.Empty;
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations =>
            DecodeReturnTypeAnnotationAttributes(GetDecodedReturnTypeWellKnownAttributeData());

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull
            => GetDecodedReturnTypeWellKnownAttributeData()?.NotNullIfParameterNotNull ?? ImmutableHashSet<string>.Empty;

        internal void GenerateExternalMethodWarnings(DiagnosticBag diagnostics)
        {
            if (this.GetAttributes().IsEmpty && !this.ContainingType.IsComImport)
            {
                // external method with no attributes
                var errorCode = (this.MethodKind == MethodKind.Constructor || this.MethodKind == MethodKind.StaticConstructor) ?
                    ErrorCode.WRN_ExternCtorNoImplementation :
                    ErrorCode.WRN_ExternMethodNoImplementation;
                diagnostics.Add(errorCode, this.Locations[0], this);
            }
        }

#nullable enable
        public override DllImportData? GetDllImportData() => ((MethodWellKnownAttributeData?)GetAttributesBag().DecodedWellKnownAttributeData)?.DllImportPlatformInvokeData;
#nullable restore

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonMethodEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = GetAttributesBag();
            return (CommonMethodEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        protected MethodWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = GetAttributesBag();
            return (MethodWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns information retrieved from custom attributes on return type in source, or null if the symbol is not source symbol or there are none.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal ReturnTypeWellKnownAttributeData GetDecodedReturnTypeWellKnownAttributeData()
        {
            var attributesBag = GetReturnTypeAttributesBag();
            return (ReturnTypeWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// Gets the attributes applied on the return value of this method symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.GetReturnTypeAttributesBag().Attributes;
        }

        internal sealed override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None || arguments.SymbolPart == AttributeLocation.Return);

            bool hasAnyDiagnostics;

            if (arguments.SymbolPart == AttributeLocation.None)
            {
                if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute))
                {
                    var boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                    if (!boundAttribute.HasErrors)
                    {
                        string name = boundAttribute.GetConstructorArgument<string>(0, SpecialType.System_String);
                        arguments.GetOrCreateData<CommonMethodEarlyWellKnownAttributeData>().AddConditionalSymbol(name);
                        if (!hasAnyDiagnostics)
                        {
                            return boundAttribute;
                        }
                    }

                    return null;
                }
                else
                {
                    CSharpAttributeData boundAttribute;
                    ObsoleteAttributeData obsoleteData;

                    if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
                    {
                        if (obsoleteData != null)
                        {
                            arguments.GetOrCreateData<CommonMethodEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                        }

                        return boundAttribute;
                    }
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            CommonMethodEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            return data != null ? data.ConditionalSymbols : ImmutableArray<string>.Empty;
        }

        internal override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(!arguments.Attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None || arguments.SymbolPart == AttributeLocation.Return);

            if (arguments.SymbolPart == AttributeLocation.None)
            {
                DecodeWellKnownAttributeAppliedToMethod(ref arguments);
            }
            else
            {
                DecodeWellKnownAttributeAppliedToReturnValue(ref arguments);
            }
        }

        private void DecodeWellKnownAttributeAppliedToMethod(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.PreserveSigAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().SetPreserveSignature(arguments.Index);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MethodImplAttribute))
            {
                AttributeData.DecodeMethodImplAttribute<MethodWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>(ref arguments, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DllImportAttribute))
            {
                DecodeDllImportAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ConditionalAttribute))
            {
                ValidateConditionalAttribute(attribute, arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SuppressUnmanagedCodeSecurityAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasSuppressUnmanagedCodeSecurityAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicSecurityMethodAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasDynamicSecurityMethodAttribute = true;
            }
            else if (VerifyObsoleteAttributeAppliedToMethod(ref arguments, AttributeDescription.ObsoleteAttribute))
            {
            }
            else if (VerifyObsoleteAttributeAppliedToMethod(ref arguments, AttributeDescription.DeprecatedAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsReadOnlyAttribute))
            {
                // IsReadOnlyAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsReadOnlyAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsUnmanagedAttribute))
            {
                // IsUnmanagedAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsUnmanagedAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsByRefLikeAttribute))
            {
                // IsByRefLikeAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsByRefLikeAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CaseSensitiveExtensionAttribute))
            {
                // [Extension] attribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitExtension, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableContextAttribute))
            {
                ReportExplicitUseOfNullabilityAttribute(in arguments, AttributeDescription.NullableContextAttribute);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(this, AttributeDescription.SecuritySafeCriticalAttribute))
            {
                if (IsAsync)
                {
                    arguments.Diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, arguments.AttributeSyntaxOpt.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DoesNotReturnAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasDoesNotReturnAttribute = true;
            }
            else
            {
                var compilation = this.DeclaringCompilation;
                if (attribute.IsSecurityAttribute(compilation))
                {
                    attribute.DecodeSecurityAttribute<MethodWellKnownAttributeData>(this, compilation, ref arguments);
                }
            }
        }

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                return DecodeFlowAnalysisAttributes(GetDecodedWellKnownAttributeData());
            }
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(MethodWellKnownAttributeData attributeData)
            => attributeData?.HasDoesNotReturnAttribute == true ? FlowAnalysisAnnotations.DoesNotReturn : FlowAnalysisAnnotations.None;

        private bool VerifyObsoleteAttributeAppliedToMethod(
            ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments,
            AttributeDescription description)
        {
            if (arguments.Attribute.IsTargetAttribute(this, description))
            {
                if (this.IsAccessor())
                {
                    if (this is SourceEventAccessorSymbol)
                    {
                        // CS1667: Attribute '{0}' is not valid on event accessors. It is only valid on '{1}' declarations.
                        AttributeUsageInfo attributeUsage = arguments.Attribute.AttributeClass.GetAttributeUsageInfo();
                        arguments.Diagnostics.Add(ErrorCode.ERR_AttributeNotOnEventAccessor, arguments.AttributeSyntaxOpt.Name.Location, description.FullName, attributeUsage.GetValidTargetsErrorArgument());
                    }
                    else
                    {
                        MessageID.IDS_FeatureObsoleteOnPropertyAccessor.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                    }
                }
                else if (this.MethodKind is MethodKind.LocalFunction)
                {
                    // CS8760: Attribute '{0}' is not valid on local functions.
                    arguments.Diagnostics.Add(ErrorCode.ERR_AttributeNotOnLocalFunction, arguments.AttributeSyntaxOpt.Name.Location, description.FullName);
                }

                return true;
            }

            return false;
        }

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, DiagnosticBag diagnostics)
        {
            if (this.IsAccessor())
            {
                // CS1667: Attribute '{0}' is not valid on property or event accessors. It is only valid on '{1}' declarations.
                AttributeUsageInfo attributeUsage = attribute.AttributeClass.GetAttributeUsageInfo();
                diagnostics.Add(ErrorCode.ERR_AttributeNotOnAccessor, node.Name.Location, node.GetErrorDisplayName(), attributeUsage.GetValidTargetsErrorArgument());
            }
            else if (this.ContainingType.IsInterfaceType())
            {
                // CS0582: The Conditional attribute is not valid on interface members
                diagnostics.Add(ErrorCode.ERR_ConditionalOnInterfaceMethod, node.Location);
            }
            else if (this.IsOverride)
            {
                // CS0243: The Conditional attribute is not valid on '{0}' because it is an override method
                diagnostics.Add(ErrorCode.ERR_ConditionalOnOverride, node.Location, this);
            }
            else if (!this.CanBeReferencedByName || this.MethodKind == MethodKind.Destructor)
            {
                // CS0577: The Conditional attribute is not valid on '{0}' because it is a constructor, destructor, operator, or explicit interface implementation
                diagnostics.Add(ErrorCode.ERR_ConditionalOnSpecialMethod, node.Location, this);
            }
            else if (!this.ReturnsVoid)
            {
                // CS0578: The Conditional attribute is not valid on '{0}' because its return type is not void
                diagnostics.Add(ErrorCode.ERR_ConditionalMustReturnVoid, node.Location, this);
            }
            else if (this.HasAnyOutParameter())
            {
                // CS0685: Conditional member '{0}' cannot have an out parameter
                diagnostics.Add(ErrorCode.ERR_ConditionalWithOutParam, node.Location, this);
            }
            else
            {
                string name = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);

                if (name == null || !SyntaxFacts.IsValidIdentifier(name))
                {
                    // CS0633: The argument to the '{0}' attribute must be a valid identifier
                    CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, node);
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
                }
            }
        }

        private bool HasAnyOutParameter()
        {
            foreach (var param in this.Parameters)
            {
                if (param.RefKind == RefKind.Out)
                {
                    return true;
                }
            }

            return false;
        }

        private void DecodeWellKnownAttributeAppliedToReturnValue(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                // MarshalAs applied to the return value:
                MarshalAsAttributeDecoder<ReturnTypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.ReturnValue, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsUnmanagedAttribute))
            {
                // IsUnmanagedAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsUnmanagedAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsReadOnlyAttribute))
            {
                // IsReadOnlyAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsReadOnlyAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsByRefLikeAttribute))
            {
                // IsByRefLikeAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsByRefLikeAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.TupleElementNamesAttribute))
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableAttribute))
            {
                // NullableAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitNullableAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MaybeNullAttribute))
            {
                arguments.GetOrCreateData<ReturnTypeWellKnownAttributeData>().HasMaybeNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullAttribute))
            {
                arguments.GetOrCreateData<ReturnTypeWellKnownAttributeData>().HasNotNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullIfNotNullAttribute))
            {
                arguments.GetOrCreateData<ReturnTypeWellKnownAttributeData>().AddNotNullIfParameterNotNull(attribute.DecodeNotNullIfNotNullAttribute());
            }
        }

        private void DecodeDllImportAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            bool hasErrors = false;

            if (!this.IsExtern || !this.IsStatic)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnInvalidMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            if (this.IsGenericMethod || ContainingType?.IsGenericType == true)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnGenericMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            string moduleName = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);
            if (!MetadataHelpers.IsValidMetadataIdentifier(moduleName))
            {
                // Dev10 reports CS0647: "Error emitting attribute ..."
                CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, arguments.AttributeSyntaxOpt);
                arguments.Diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                hasErrors = true;
                moduleName = null;
            }

            // Default value of charset is inherited from the module (only if specified).
            // This might be different from ContainingType.DefaultMarshallingCharSet. If the charset is not specified on module
            // ContainingType.DefaultMarshallingCharSet would be Ansi (the class is emitted with "Ansi" charset metadata flag)
            // while the charset in P/Invoke metadata should be "None".
            CharSet charSet = this.GetEffectiveDefaultMarshallingCharSet() ?? Cci.Constants.CharSet_None;

            string importName = null;
            bool preserveSig = true;
            CallingConvention callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
            bool setLastError = false;
            bool exactSpelling = false;  // C#: ExactSpelling=false for any charset
            bool? bestFitMapping = null;
            bool? throwOnUnmappable = null;

            int position = 1;
            foreach (var namedArg in attribute.CommonNamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "EntryPoint":
                        importName = namedArg.Value.ValueInternal as string;
                        if (!MetadataHelpers.IsValidMetadataIdentifier(importName))
                        {
                            // Dev10 reports CS0647: "Error emitting attribute ..."
                            arguments.Diagnostics.Add(ErrorCode.ERR_InvalidNamedArgument, arguments.AttributeSyntaxOpt.ArgumentList.Arguments[position].Location, namedArg.Key);
                            hasErrors = true;
                            importName = null;
                        }

                        break;

                    case "CharSet":
                        // invalid values will be ignored
                        charSet = namedArg.Value.DecodeValue<CharSet>(SpecialType.System_Enum);
                        break;

                    case "SetLastError":
                        // invalid values will be ignored
                        setLastError = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "ExactSpelling":
                        // invalid values will be ignored
                        exactSpelling = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "PreserveSig":
                        preserveSig = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "CallingConvention":
                        // invalid values will be ignored
                        callingConvention = namedArg.Value.DecodeValue<CallingConvention>(SpecialType.System_Enum);
                        break;

                    case "BestFitMapping":
                        bestFitMapping = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;

                    case "ThrowOnUnmappableChar":
                        throwOnUnmappable = namedArg.Value.DecodeValue<bool>(SpecialType.System_Boolean);
                        break;
                }

                position++;
            }

            if (!hasErrors)
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().SetDllImport(
                    arguments.Index,
                    moduleName,
                    importName,
                    DllImportData.MakeFlags(
                        exactSpelling,
                        charSet,
                        setLastError,
                        callingConvention,
                        bestFitMapping,
                        throwOnUnmappable),
                    preserveSig);
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(symbolPart == AttributeLocation.None || symbolPart == AttributeLocation.Return);

            if (symbolPart != AttributeLocation.Return)
            {
                if (ContainingType.IsComImport && ContainingType.TypeKind == TypeKind.Class)
                {
                    switch (this.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            if (!this.IsImplicitlyDeclared)
                            {
                                // CS0669: A class with the ComImport attribute cannot have a user-defined constructor
                                diagnostics.Add(ErrorCode.ERR_ComImportWithUserCtor, this.Locations[0]);
                            }

                            break;

                        default:
                            if (!this.IsAbstract && !this.IsExtern)
                            {
                                // CS0423: Since '{1}' has the ComImport attribute, '{0}' must be extern or abstract
                                diagnostics.Add(ErrorCode.ERR_ComImportWithImpl, this.Locations[0], this, ContainingType);
                            }

                            break;
                    }
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        private static FlowAnalysisAnnotations DecodeReturnTypeAnnotationAttributes(ReturnTypeWellKnownAttributeData attributeData)
        {
            FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None;
            if (attributeData != null)
            {
                if (attributeData.HasMaybeNullAttribute)
                {
                    annotations |= FlowAnalysisAnnotations.MaybeNull;
                }
                if (attributeData.HasNotNullAttribute)
                {
                    annotations |= FlowAnalysisAnnotations.NotNull;
                }
            }
            return annotations;
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                var data = this.GetDecodedReturnTypeWellKnownAttributeData();
                return data != null ? data.MarshallingInformation : null;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                var result = (data != null) ? data.MethodImplAttributes : default(System.Reflection.MethodImplAttributes);

                if (this.ContainingType.IsComImport && this.MethodKind == MethodKind.Constructor)
                {
                    // Synthesized constructor of ComImport types is marked as Runtime implemented and InternalCall
                    result |= (System.Reflection.MethodImplAttributes.Runtime | System.Reflection.MethodImplAttributes.InternalCall);
                }

                return result;
            }
        }
    }
}
