// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A source parameter, potentially with a default value, attributes, etc.
    /// </summary>
    internal class SourceComplexParameterSymbol : SourceParameterSymbol, IAttributeTargetSymbol
    {
        [Flags]
        private enum ParameterSyntaxKind : byte
        {
            Regular = 0,
            ParamsParameter = 1,
            ExtensionThisParameter = 2,
            DefaultParameter = 4,
        }

        private readonly SyntaxReference _syntaxRef;
        private readonly ParameterSyntaxKind _parameterSyntaxKind;

        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
        private ThreeState _lazyHasOptionalAttribute;
        protected ConstantValue _lazyDefaultSyntaxValue;

        internal SourceComplexParameterSymbol(
            Symbol owner,
            int ordinal,
            TypeSymbolWithAnnotations parameterType,
            RefKind refKind,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis)
            : base(owner, parameterType, ordinal, refKind, name, locations)
        {
            Debug.Assert((syntaxRef == null) || (syntaxRef.GetSyntax().IsKind(SyntaxKind.Parameter)));

            _lazyHasOptionalAttribute = ThreeState.Unknown;
            _syntaxRef = syntaxRef;

            if (isParams)
            {
                _parameterSyntaxKind |= ParameterSyntaxKind.ParamsParameter;
            }

            if (isExtensionMethodThis)
            {
                _parameterSyntaxKind |= ParameterSyntaxKind.ExtensionThisParameter;
            }

            var parameterSyntax = this.CSharpSyntaxNode;
            if (parameterSyntax != null && parameterSyntax.Default != null)
            {
                _parameterSyntaxKind |= ParameterSyntaxKind.DefaultParameter;
            }

            _lazyDefaultSyntaxValue = defaultSyntaxValue;
        }

        protected virtual Binder ParameterBinder
        {
            get { return null; }
        }

        internal override SyntaxReference SyntaxReference
        {
            get
            {
                return _syntaxRef;
            }
        }

        internal ParameterSyntax CSharpSyntaxNode
        {
            get
            {
                return _syntaxRef == null ? null : (ParameterSyntax)_syntaxRef.GetSyntax();
            }
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxRef == null ? null : _syntaxRef.SyntaxTree;
            }
        }

        internal override ConstantValue ExplicitDefaultConstantValue
        {
            get
            {
                // Parameter has either default argument syntax or DefaultParameterValue attribute, but not both.
                // We separate these since in some scenarios (delegate Invoke methods) we need to suppress syntactic 
                // default value but use value from pseudo-custom attribute. 
                //
                // For example:
                // public delegate void D([Optional, DefaultParameterValue(1)]int a, int b = 2);
                //
                // Dev11 emits the first parameter as option with default value and the second as regular parameter.
                // The syntactic default value is suppressed since additional synthesized parameters are added at the end of the signature.

                return DefaultSyntaxValue ?? DefaultValueFromAttributes;
            }
        }

        internal override ConstantValue DefaultValueFromAttributes
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return (data != null && data.DefaultParameterValue != ConstantValue.Unset) ? data.DefaultParameterValue : ConstantValue.NotAvailable;
            }
        }

        internal override bool IsIDispatchConstant
        {
            get
            {
                CommonParameterWellKnownAttributeData data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasIDispatchConstantAttribute;
            }
        }

        internal override bool IsIUnknownConstant
        {
            get
            {
                CommonParameterWellKnownAttributeData data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasIUnknownConstantAttribute;
            }
        }

        private bool HasCallerLineNumberAttribute
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return data != null && data.HasCallerLineNumberAttribute;
            }
        }

        private bool HasCallerFilePathAttribute
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return data != null && data.HasCallerFilePathAttribute;
            }
        }

        private bool HasCallerMemberNameAttribute
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return data != null && data.HasCallerMemberNameAttribute;
            }
        }

        internal override bool IsCallerLineNumber
        {
            get
            {
                return HasCallerLineNumberAttribute;
            }
        }

        internal override bool IsCallerFilePath
        {
            get
            {
                return !HasCallerLineNumberAttribute && HasCallerFilePathAttribute;
            }
        }

        internal override bool IsCallerMemberName
        {
            get
            {
                return !HasCallerLineNumberAttribute && !HasCallerFilePathAttribute && HasCallerMemberNameAttribute;
            }
        }

        private ConstantValue DefaultSyntaxValue
        {
            get
            {
                if (_lazyDefaultSyntaxValue == ConstantValue.Unset)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    if (Interlocked.CompareExchange(ref _lazyDefaultSyntaxValue, MakeDefaultExpression(diagnostics, ParameterBinder), ConstantValue.Unset) == ConstantValue.Unset)
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyDefaultSyntaxValue;
            }
        }

        // If binder is null, then get it from the compilation. Otherwise use the provided binder.
        // Don't always get it from the compilation because we might be in a speculative context (local function parameter),
        // in which case the declaring compilation is the wrong one.
        protected ConstantValue MakeDefaultExpression(DiagnosticBag diagnostics, Binder binder)
        {
            var parameterSyntax = this.CSharpSyntaxNode;
            if (parameterSyntax == null)
            {
                return ConstantValue.NotAvailable;
            }

            var defaultSyntax = parameterSyntax.Default;
            if (defaultSyntax == null)
            {
                return ConstantValue.NotAvailable;
            }

            if (binder == null)
            {
                var syntaxTree = _syntaxRef.SyntaxTree;
                var compilation = this.DeclaringCompilation;
                var binderFactory = compilation.GetBinderFactory(syntaxTree);
                binder = binderFactory.GetBinder(defaultSyntax);
            }

            BoundExpression valueBeforeConversion;
            var convertedExpression = binder.CreateBinderForParameterDefaultValue(this, defaultSyntax).BindParameterDefaultValue(defaultSyntax, parameterType.TypeSymbol, diagnostics, out valueBeforeConversion);

            bool hasErrors = ParameterHelpers.ReportDefaultParameterErrors(binder, ContainingSymbol, parameterSyntax, this, valueBeforeConversion, diagnostics);
            if (hasErrors)
            {
                return ConstantValue.Bad;
            }

            // If we have something like M(double? x = 1) then the expression we'll get is (double?)1, which
            // does not have a constant value. The constant value we want is (double)1.

            if (convertedExpression.ConstantValue == null && convertedExpression.Kind == BoundKind.Conversion)
            {
                if (parameterType.TypeSymbol.IsNullableType())
                {
                    convertedExpression = binder.GenerateConversionForAssignment(parameterType.TypeSymbol.GetNullableUnderlyingType(),
                        valueBeforeConversion, diagnostics, isDefaultParameter: true);
                }
            }

            // represent default(struct) by a Null constant:
            var value = convertedExpression.ConstantValue ?? ConstantValue.Null;
            VerifyParamDefaultValueMatchesAttributeIfAny(value, defaultSyntax.Value, diagnostics);
            return value;
        }

        public override string MetadataName
        {
            get
            {
                // The metadata parameter name should be the name used in the partial definition.

                var sourceMethod = this.ContainingSymbol as SourceMemberMethodSymbol;
                if ((object)sourceMethod == null)
                {
                    return base.MetadataName;
                }

                var definition = sourceMethod.SourcePartialDefinition;
                if ((object)definition == null)
                {
                    return base.MetadataName;
                }

                return definition.Parameters[this.Ordinal].MetadataName;
            }
        }

        protected virtual IAttributeTargetSymbol AttributeOwner
        {
            get { return this; }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this.AttributeOwner; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Parameter; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get { return AttributeLocation.Parameter; }
        }

        /// <summary>
        /// Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source parameter symbols.
        /// </summary>
        /// <remarks>
        /// Used for parameters of partial implementation. We bind the attributes only on the definition
        /// part and copy them over to the implementation.
        /// </remarks>
        private SourceParameterSymbol BoundAttributesSource
        {
            get
            {
                var sourceMethod = this.ContainingSymbol as SourceMemberMethodSymbol;
                if ((object)sourceMethod == null)
                {
                    return null;
                }

                var impl = sourceMethod.SourcePartialImplementation;
                if ((object)impl == null)
                {
                    return null;
                }

                return (SourceParameterSymbol)impl.Parameters[this.Ordinal];
            }
        }

        internal sealed override SyntaxList<AttributeListSyntax> AttributeDeclarationList
        {
            get
            {
                var syntax = this.CSharpSyntaxNode;
                return (syntax != null) ? syntax.AttributeLists : default(SyntaxList<AttributeListSyntax>);
            }
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares attributes for this parameter symbol.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // C# spec:
            // The attributes on the parameters of the resulting method declaration
            // are the combined attributes of the corresponding parameters of the defining
            // and the implementing partial method declaration in unspecified order.
            // Duplicates are not removed.

            SyntaxList<AttributeListSyntax> attributes = AttributeDeclarationList;

            var sourceMethod = this.ContainingSymbol as SourceMemberMethodSymbol;
            if ((object)sourceMethod == null)
            {
                return OneOrMany.Create(attributes);
            }

            SyntaxList<AttributeListSyntax> otherAttributes;

            // if this is a definition get the implementation and vice versa
            SourceMemberMethodSymbol otherPart = sourceMethod.OtherPartOfPartial;
            if ((object)otherPart != null)
            {
                otherAttributes = ((SourceParameterSymbol)otherPart.Parameters[this.Ordinal]).AttributeDeclarationList;
            }
            else
            {
                otherAttributes = default(SyntaxList<AttributeListSyntax>);
            }

            if (attributes.Equals(default(SyntaxList<AttributeListSyntax>)))
            {
                return OneOrMany.Create(otherAttributes);
            }
            else if (otherAttributes.Equals(default(SyntaxList<AttributeListSyntax>)))
            {
                return OneOrMany.Create(attributes);
            }

            return OneOrMany.Create(ImmutableArray.Create(attributes, otherAttributes));
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonParameterWellKnownAttributeData GetDecodedWellKnownAttributeData(DiagnosticBag diagnosticsOpt = null)
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag(diagnosticsOpt);
            }

            return (CommonParameterWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal ParameterEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData(DiagnosticBag diagnosticsOpt = null)
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag(diagnosticsOpt);
            }

            return (ParameterEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal sealed override CustomAttributesBag<CSharpAttributeData> GetAttributesBag(DiagnosticBag diagnosticsOpt)
        {
            if (_lazyCustomAttributesBag == null || !_lazyCustomAttributesBag.IsSealed)
            {
                SourceParameterSymbol copyFrom = this.BoundAttributesSource;

                // prevent infinite recursion:
                Debug.Assert(!ReferenceEquals(copyFrom, this));

                bool bagCreatedOnThisThread;
                if ((object)copyFrom != null)
                {
                    var attributesBag = copyFrom.GetAttributesBag(diagnosticsOpt);
                    bagCreatedOnThisThread = Interlocked.CompareExchange(ref _lazyCustomAttributesBag, attributesBag, null) == null;
                }
                else
                {
                    var attributeSyntax = this.GetAttributeDeclarations();
                    bagCreatedOnThisThread = LoadAndValidateAttributes(attributeSyntax, ref _lazyCustomAttributesBag, addToDiagnostics: diagnosticsOpt, binderOpt: ParameterBinder);
                }

                if (bagCreatedOnThisThread)
                {
                    state.NotePartComplete(CompletionPart.Attributes);
                }
            }

            return _lazyCustomAttributesBag;
        }

        internal override void EarlyDecodeWellKnownAttributeType(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
            Debug.Assert(!attributeType.IsErrorType());

            // NOTE: OptionalAttribute is decoded specially before any of the other attributes and stored in the parameter
            // symbol (rather than in the EarlyWellKnownAttributeData) because it is needed during overload resolution.
            if (CSharpAttributeData.IsTargetEarlyAttribute(attributeType, attributeSyntax, AttributeDescription.OptionalAttribute))
            {
                _lazyHasOptionalAttribute = ThreeState.True;
            }
        }

        internal override void PostEarlyDecodeWellKnownAttributeTypes()
        {
            if (_lazyHasOptionalAttribute == ThreeState.Unknown)
            {
                _lazyHasOptionalAttribute = ThreeState.False;
            }

            base.PostEarlyDecodeWellKnownAttributeTypes();
        }

        internal override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DefaultParameterValueAttribute))
            {
                return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DefaultParameterValueAttribute, ref arguments);
            }
            else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DecimalConstantAttribute))
            {
                return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DecimalConstantAttribute, ref arguments);
            }
            else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.DateTimeConstantAttribute))
            {
                return EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription.DateTimeConstantAttribute, ref arguments);
            }
            else if (!IsOnPartialImplementation(arguments.AttributeSyntax))
            {
                if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerLineNumberAttribute))
                {
                    arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().HasCallerLineNumberAttribute = true;
                }
                else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerFilePathAttribute))
                {
                    arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().HasCallerFilePathAttribute = true;
                }
                else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerMemberNameAttribute))
                {
                    arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().HasCallerMemberNameAttribute = true;
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        private CSharpAttributeData EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription description, ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            Debug.Assert(description.Equals(AttributeDescription.DefaultParameterValueAttribute) ||
                description.Equals(AttributeDescription.DecimalConstantAttribute) ||
                description.Equals(AttributeDescription.DateTimeConstantAttribute));

            bool hasAnyDiagnostics;
            var attribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
            ConstantValue value;
            if (attribute.HasErrors)
            {
                value = ConstantValue.Bad;
                hasAnyDiagnostics = true;
            }
            else
            {
                value = DecodeDefaultParameterValueAttribute(description, attribute, arguments.AttributeSyntax, diagnose: false, diagnosticsOpt: null);
            }

            var paramData = arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>();
            if (paramData.DefaultParameterValue == ConstantValue.Unset)
            {
                paramData.DefaultParameterValue = value;
            }

            return !hasAnyDiagnostics ? attribute : null;
        }

        internal override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.DefaultParameterValueAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DefaultParameterValueAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DecimalConstantAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DecimalConstantAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DateTimeConstantAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DateTimeConstantAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.OptionalAttribute))
            {
                Debug.Assert(_lazyHasOptionalAttribute == ThreeState.True);

                if (HasDefaultArgumentSyntax)
                {
                    // error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                    arguments.Diagnostics.Add(ErrorCode.ERR_DefaultValueUsedWithAttributes, arguments.AttributeSyntaxOpt.Name.Location);
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ParamArrayAttribute))
            {
                // error CS0674: Do not use 'System.ParamArrayAttribute'. Use the 'params' keyword instead.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitParamArray, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.InAttribute))
            {
                if (this.RefKind == RefKind.Out)
                {
                    // error CS0036: An out parameter cannot have the In attribute
                    arguments.Diagnostics.Add(ErrorCode.ERR_InAttrOnOutParam, arguments.AttributeSyntaxOpt.Name.Location);
                }
                else
                {
                    arguments.GetOrCreateData<CommonParameterWellKnownAttributeData>().HasInAttribute = true;
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.OutAttribute))
            {
                arguments.GetOrCreateData<CommonParameterWellKnownAttributeData>().HasOutAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                MarshalAsAttributeDecoder<CommonParameterWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.Parameter, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IDispatchConstantAttribute))
            {
                arguments.GetOrCreateData<CommonParameterWellKnownAttributeData>().HasIDispatchConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IUnknownConstantAttribute))
            {
                arguments.GetOrCreateData<CommonParameterWellKnownAttributeData>().HasIUnknownConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerLineNumberAttribute))
            {
                ValidateCallerLineNumberAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerFilePathAttribute))
            {
                ValidateCallerFilePathAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerMemberNameAttribute))
            {
                ValidateCallerMemberNameAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
        }


        private void DecodeDefaultParameterValueAttribute(AttributeDescription description, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            var attribute = arguments.Attribute;
            var syntax = arguments.AttributeSyntaxOpt;
            var diagnostics = arguments.Diagnostics;

            Debug.Assert(syntax != null);
            Debug.Assert(diagnostics != null);

            var value = DecodeDefaultParameterValueAttribute(description, attribute, syntax, diagnose: true, diagnosticsOpt: diagnostics);
            if (!value.IsBad)
            {
                VerifyParamDefaultValueMatchesAttributeIfAny(value, syntax, diagnostics);
            }
        }

        /// <summary>
        /// Verify the default value matches the default value from any earlier attribute
        /// (DefaultParameterValueAttribute, DateTimeConstantAttribute or DecimalConstantAttribute).
        /// If not, report ERR_ParamDefaultValueDiffersFromAttribute.
        /// </summary>
        private void VerifyParamDefaultValueMatchesAttributeIfAny(ConstantValue value, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var data = GetEarlyDecodedWellKnownAttributeData(diagnostics);
            if (data != null)
            {
                var attrValue = data.DefaultParameterValue;
                if (!attrValue.IsBad &&
                    (attrValue != ConstantValue.Unset) &&
                    (value != attrValue))
                {
                    // CS8017: The parameter has multiple distinct default values.
                    diagnostics.Add(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, syntax.Location);
                }
            }
        }

        private ConstantValue DecodeDefaultParameterValueAttribute(AttributeDescription description, CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert(!attribute.HasErrors);

            if (description.Equals(AttributeDescription.DefaultParameterValueAttribute))
            {
                return DecodeDefaultParameterValueAttribute(attribute, node, diagnose, diagnosticsOpt);
            }
            else if (description.Equals(AttributeDescription.DecimalConstantAttribute))
            {
                return attribute.DecodeDecimalConstantValue();
            }
            else
            {
                Debug.Assert(description.Equals(AttributeDescription.DateTimeConstantAttribute));
                return attribute.DecodeDateTimeConstantValue();
            }
        }

        private ConstantValue DecodeDefaultParameterValueAttribute(CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert(!diagnose || diagnosticsOpt != null);

            if (HasDefaultArgumentSyntax)
            {
                // error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                if (diagnose)
                {
                    diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueUsedWithAttributes, node.Name.Location);
                }
                return ConstantValue.Bad;
            }

            // BREAK: In dev10, DefaultParameterValueAttribute could not be applied to System.Type or array parameters.
            // When this was attempted, dev10 produced CS1909, ERR_DefaultValueBadParamType.  Roslyn takes a different
            // approach: instead of looking at the parameter type, we look at the argument type.  There's nothing wrong
            // with providing a default value for a System.Type or array parameter, as long as the default parameter
            // is not a System.Type or an array (i.e. null is fine).  Since we are no longer interested in the type of
            // the parameter, all occurrences of CS1909 have been replaced with CS1910, ERR_DefaultValueBadValueType,
            // to indicate that the argument type, rather than the parameter type, is the source of the problem.

            Debug.Assert(attribute.CommonConstructorArguments.Length == 1);

            // the type of the value is the type of the expression in the attribute:
            var arg = attribute.CommonConstructorArguments[0];

            SpecialType specialType = arg.Kind == TypedConstantKind.Enum ?
                ((INamedTypeSymbol)arg.Type).EnumUnderlyingType.SpecialType :
                arg.Type.SpecialType;

            var compilation = this.DeclaringCompilation;
            var constantValueDiscriminator = ConstantValue.GetDiscriminator(specialType);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            if (constantValueDiscriminator == ConstantValueTypeDiscriminator.Bad)
            {
                if (arg.Kind != TypedConstantKind.Array && arg.Value == null)
                {
                    if (this.Type.IsReferenceType)
                    {
                        constantValueDiscriminator = ConstantValueTypeDiscriminator.Null;
                    }
                    else
                    {
                        // error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                        if (diagnose)
                        {
                            diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueTypeMustMatch, node.Name.Location);
                        }
                        return ConstantValue.Bad;
                    }
                }
                else
                {
                    // error CS1910: Argument of type '{0}' is not applicable for the DefaultParameterValue attribute
                    if (diagnose)
                    {
                        diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueBadValueType, node.Name.Location, arg.Type);
                    }
                    return ConstantValue.Bad;
                }
            }
            else if (!compilation.Conversions.ClassifyConversion((TypeSymbol)arg.Type, this.Type.TypeSymbol, ref useSiteDiagnostics).Kind.IsImplicitConversion())
            {
                // error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                if (diagnose)
                {
                    diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueTypeMustMatch, node.Name.Location);
                    diagnosticsOpt.Add(node.Name.Location, useSiteDiagnostics);
                }
                return ConstantValue.Bad;
            }

            if (diagnose)
            {
                diagnosticsOpt.Add(node.Name.Location, useSiteDiagnostics);
            }

            return ConstantValue.Create(arg.Value, constantValueDiscriminator);
        }

        private bool IsValidCallerInfoContext(AttributeSyntax node)
        {
            return !ContainingSymbol.IsExplicitInterfaceImplementation() && !ContainingSymbol.IsOperator() && !IsOnPartialImplementation(node);
        }

        /// <summary>
        /// Is the attribute syntax appearing on a parameter of a partial method implementation part?
        /// Since attributes are merged between the parts of a partial, we need to look at the syntax where the
        /// attribute appeared in the source to see if it corresponds to a partial method implementation part.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool IsOnPartialImplementation(AttributeSyntax node)
        {
            var method = ContainingSymbol as MethodSymbol;
            if ((object)method == null) return false;
            var impl = method.IsPartialImplementation() ? method : method.PartialImplementationPart;
            if ((object)impl == null) return false;
            var paramList =
                node     // AttributeSyntax
                .Parent  // AttributeListSyntax
                .Parent  // ParameterSyntax
                .Parent as ParameterListSyntax; // ParameterListSyntax
            if (paramList == null) return false;
            var methDecl = paramList.Parent as MethodDeclarationSyntax;
            if (methDecl == null) return false;
            foreach (var r in impl.DeclaringSyntaxReferences)
            {
                if (r.GetSyntax() == methDecl) return true;
            }
            return false;
        }

        private void ValidateCallerLineNumberAttribute(AttributeSyntax node, DiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (!IsValidCallerInfoContext(node))
            {
                // CS4024: The CallerLineNumberAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerLineNumberConversion(Type.TypeSymbol, ref useSiteDiagnostics))
            {
                // CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerLineNumberParam, node.Name.Location, intType, Type.TypeSymbol);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, node.Name.Location);
            }

            diagnostics.Add(node.Name.Location, useSiteDiagnostics);
        }

        private void ValidateCallerFilePathAttribute(AttributeSyntax node, DiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (!IsValidCallerInfoContext(node))
            {
                // CS4025: The CallerFilePathAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerInfoStringConversion(Type.TypeSymbol, ref useSiteDiagnostics))
            {
                // CS4018: CallerFilePathAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerFilePathParam, node.Name.Location, stringType, Type.TypeSymbol);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4021: The CallerFilePathAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue, node.Name.Location);
            }
            else if (HasCallerLineNumberAttribute)
            {
                // CS7082: The CallerFilePathAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerLineNumberAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            diagnostics.Add(node.Name.Location, useSiteDiagnostics);
        }

        private void ValidateCallerMemberNameAttribute(AttributeSyntax node, DiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (!IsValidCallerInfoContext(node))
            {
                // CS4026: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerInfoStringConversion(Type.TypeSymbol, ref useSiteDiagnostics))
            {
                // CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerMemberNameParam, node.Name.Location, stringType, Type.TypeSymbol);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4022: The CallerMemberNameAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue, node.Name.Location);
            }
            else if (HasCallerLineNumberAttribute)
            {
                // CS7081: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerLineNumberAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (HasCallerFilePathAttribute)
            {
                // CS7080: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerFilePathAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            diagnostics.Add(node.Name.Location, useSiteDiagnostics);
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            var data = (CommonParameterWellKnownAttributeData)decodedData;
            if (data != null)
            {
                if (this.RefKind == RefKind.Ref && data.HasOutAttribute && !data.HasInAttribute)
                {
                    // error CS0662: '...' cannot specify only Out attribute on a ref parameter. Use both In and Out attributes, or neither.
                    diagnostics.Add(ErrorCode.ERR_OutAttrOnRefParam, this.Locations[0]);
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        /// <summary>
        /// True if the parameter has default argument syntax.
        /// </summary>
        internal override bool HasDefaultArgumentSyntax
        {
            get
            {
                return (_parameterSyntaxKind & ParameterSyntaxKind.DefaultParameter) != 0;
            }
        }

        /// <summary>
        /// True if the parameter is marked by <see cref="System.Runtime.InteropServices.OptionalAttribute"/>.
        /// </summary>
        internal sealed override bool HasOptionalAttribute
        {
            get
            {
                if (_lazyHasOptionalAttribute == ThreeState.Unknown)
                {
                    SourceParameterSymbol copyFrom = this.BoundAttributesSource;

                    // prevent infinite recursion:
                    Debug.Assert(!ReferenceEquals(copyFrom, this));

                    if ((object)copyFrom != null)
                    {
                        // Parameter of partial implementation.
                        // We bind the attributes only on the definition part and copy them over to the implementation.
                        _lazyHasOptionalAttribute = copyFrom.HasOptionalAttribute.ToThreeState();
                    }
                    else
                    {
                        // lazyHasOptionalAttribute is decoded early, hence we cannot reach here when binding attributes for this symbol.
                        // So it is fine to force complete attributes here.

                        var attributes = GetAttributes();

                        if (!attributes.Any())
                        {
                            _lazyHasOptionalAttribute = ThreeState.False;
                        }
                    }
                }

                Debug.Assert(_lazyHasOptionalAttribute.HasValue());

                return _lazyHasOptionalAttribute.Value();
            }
        }

        internal override bool IsMetadataOptional
        {
            get
            {
                // NOTE: IsMetadataOptional property can be invoked during overload resolution.
                // NOTE: Optional attribute is decoded very early in attribute binding phase, see method EarlyDecodeOptionalAttribute
                // NOTE: If you update the below check to look for any more attributes, make sure that they are decoded early.
                return HasDefaultArgumentSyntax || HasOptionalAttribute;
            }
        }

        internal sealed override bool IsMetadataIn
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasInAttribute;
            }
        }

        internal sealed override bool IsMetadataOut
        {
            get
            {
                if (this.RefKind == Microsoft.CodeAnalysis.RefKind.Out)
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasOutAttribute;
            }
        }

        internal sealed override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null ? data.MarshallingInformation : null;
            }
        }

        public override bool IsParams
        {
            get
            {
                return (_parameterSyntaxKind & ParameterSyntaxKind.ParamsParameter) != 0;
            }
        }

        internal override bool IsExtensionMethodThis
        {
            get
            {
                return (_parameterSyntaxKind & ParameterSyntaxKind.ExtensionThisParameter) != 0;
            }
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            base.ForceComplete(locationOpt, cancellationToken);

            // Force binding of default value.
            var unused = this.ExplicitDefaultConstantValue;
        }
    }

    internal sealed class SourceComplexParameterSymbolWithCustomModifiersPrecedingByRef : SourceComplexParameterSymbol
    {
        private readonly ushort _countOfCustomModifiersPrecedingByRef;

        internal SourceComplexParameterSymbolWithCustomModifiersPrecedingByRef(
            Symbol owner,
            int ordinal,
            TypeSymbolWithAnnotations parameterType,
            RefKind refKind,
            ushort countOfCustomModifiersPrecedingByRef,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            ConstantValue defaultSyntaxValue,
            bool isParams,
            bool isExtensionMethodThis)
            : base(owner, ordinal, parameterType, refKind, name, locations, syntaxRef, defaultSyntaxValue, isParams, isExtensionMethodThis)
        {
            _countOfCustomModifiersPrecedingByRef = countOfCustomModifiersPrecedingByRef;

            Debug.Assert(refKind != RefKind.None);
            Debug.Assert(_countOfCustomModifiersPrecedingByRef > 0);
            Debug.Assert(_countOfCustomModifiersPrecedingByRef <= parameterType.CustomModifiers.Length);
        }

        internal override ushort CountOfCustomModifiersPrecedingByRef
        {
            get
            {
                return _countOfCustomModifiersPrecedingByRef;
            }
        }
    }
}
