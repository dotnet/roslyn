// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
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
            TypeWithAnnotations parameterType,
            RefKind refKind,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
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

            _lazyDefaultSyntaxValue = ConstantValue.Unset;
        }

        private Binder WithTypeParametersBinderOpt => (ContainingSymbol as SourceMethodSymbolWithAttributes)?.WithTypeParametersBinder;

        internal sealed override SyntaxReference SyntaxReference => _syntaxRef;

        private ParameterSyntax CSharpSyntaxNode => (ParameterSyntax)_syntaxRef?.GetSyntax();

        public override bool IsDiscard => false;

        internal sealed override ConstantValue ExplicitDefaultConstantValue
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

        internal sealed override ConstantValue DefaultValueFromAttributes
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return (data != null && data.DefaultParameterValue != ConstantValue.Unset) ? data.DefaultParameterValue : ConstantValue.NotAvailable;
            }
        }

        internal sealed override bool IsIDispatchConstant
            => GetDecodedWellKnownAttributeData()?.HasIDispatchConstantAttribute == true;

        internal override bool IsIUnknownConstant
            => GetDecodedWellKnownAttributeData()?.HasIUnknownConstantAttribute == true;

        internal override bool IsCallerLineNumber => GetEarlyDecodedWellKnownAttributeData()?.HasCallerLineNumberAttribute == true;

        internal override bool IsCallerFilePath => GetEarlyDecodedWellKnownAttributeData()?.HasCallerFilePathAttribute == true;

        internal override bool IsCallerMemberName => GetEarlyDecodedWellKnownAttributeData()?.HasCallerMemberNameAttribute == true;

        internal override int CallerArgumentExpressionParameterIndex
        {
            get
            {
                return GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex ?? -1;
            }
        }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes
            => (GetDecodedWellKnownAttributeData()?.InterpolatedStringHandlerArguments).NullToEmpty();

        internal override bool HasInterpolatedStringHandlerArgumentError
            => GetDecodedWellKnownAttributeData()?.InterpolatedStringHandlerArguments.IsDefault ?? false;

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                return DecodeFlowAnalysisAttributes(GetDecodedWellKnownAttributeData());
            }
        }

        public override bool IsNullChecked
            => this.CSharpSyntaxNode?.ExclamationExclamationToken.Kind() == SyntaxKind.ExclamationExclamationToken;

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(ParameterWellKnownAttributeData attributeData)
        {
            if (attributeData == null)
            {
                return FlowAnalysisAnnotations.None;
            }
            FlowAnalysisAnnotations annotations = FlowAnalysisAnnotations.None;
            if (attributeData.HasAllowNullAttribute) annotations |= FlowAnalysisAnnotations.AllowNull;
            if (attributeData.HasDisallowNullAttribute) annotations |= FlowAnalysisAnnotations.DisallowNull;

            if (attributeData.HasMaybeNullAttribute)
            {
                annotations |= FlowAnalysisAnnotations.MaybeNull;
            }
            else
            {
                if (attributeData.MaybeNullWhenAttribute is bool when)
                {
                    annotations |= (when ? FlowAnalysisAnnotations.MaybeNullWhenTrue : FlowAnalysisAnnotations.MaybeNullWhenFalse);
                }
            }

            if (attributeData.HasNotNullAttribute)
            {
                annotations |= FlowAnalysisAnnotations.NotNull;
            }
            else
            {
                if (attributeData.NotNullWhenAttribute is bool when)
                {
                    annotations |= (when ? FlowAnalysisAnnotations.NotNullWhenTrue : FlowAnalysisAnnotations.NotNullWhenFalse);
                }
            }

            if (attributeData.DoesNotReturnIfAttribute is bool condition)
            {
                annotations |= (condition ? FlowAnalysisAnnotations.DoesNotReturnIfTrue : FlowAnalysisAnnotations.DoesNotReturnIfFalse);
            }

            return annotations;
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
            => GetDecodedWellKnownAttributeData()?.NotNullIfParameterNotNull ?? ImmutableHashSet<string>.Empty;

        internal bool HasEnumeratorCancellationAttribute
        {
            get
            {
                ParameterWellKnownAttributeData attributeData = GetDecodedWellKnownAttributeData();
                return attributeData?.HasEnumeratorCancellationAttribute == true;
            }
        }

#nullable enable

        internal static SyntaxNode? GetDefaultValueSyntaxForIsNullableAnalysisEnabled(ParameterSyntax? parameterSyntax) =>
            parameterSyntax?.Default?.Value;

        private ConstantValue DefaultSyntaxValue
        {
            get
            {
                if (state.NotePartComplete(CompletionPart.StartDefaultSyntaxValue))
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    Debug.Assert(diagnostics.DiagnosticBag != null);
                    var previousValue = Interlocked.CompareExchange(
                        ref _lazyDefaultSyntaxValue,
                        MakeDefaultExpression(diagnostics, out var binder, out var parameterEqualsValue),
                        ConstantValue.Unset);
                    Debug.Assert(previousValue == ConstantValue.Unset);

                    var completedOnThisThread = state.NotePartComplete(CompletionPart.EndDefaultSyntaxValue);
                    Debug.Assert(completedOnThisThread);

                    if (parameterEqualsValue is not null)
                    {
                        if (binder is not null &&
                            GetDefaultValueSyntaxForIsNullableAnalysisEnabled(CSharpSyntaxNode) is { } valueSyntax)
                        {
                            NullableWalker.AnalyzeIfNeeded(binder, parameterEqualsValue, valueSyntax, diagnostics.DiagnosticBag);
                        }
                        if (!_lazyDefaultSyntaxValue.IsBad)
                        {
                            VerifyParamDefaultValueMatchesAttributeIfAny(_lazyDefaultSyntaxValue, parameterEqualsValue.Value.Syntax, diagnostics);
                        }
                    }

                    AddDeclarationDiagnostics(diagnostics);
                    diagnostics.Free();

                    completedOnThisThread = state.NotePartComplete(CompletionPart.EndDefaultSyntaxValueDiagnostics);
                    Debug.Assert(completedOnThisThread);
                }

                state.SpinWaitComplete(CompletionPart.EndDefaultSyntaxValue, default(CancellationToken));
                return _lazyDefaultSyntaxValue;
            }
        }

        private Binder GetDefaultParameterValueBinder(SyntaxNode syntax)
        {
            var binder = WithTypeParametersBinderOpt;

            // If binder is null, then get it from the compilation. Otherwise use the provided binder.
            // Don't always get it from the compilation because we might be in a speculative context (local function parameter),
            // in which case the declaring compilation is the wrong one.
            if (binder == null)
            {
                var compilation = this.DeclaringCompilation;
                var binderFactory = compilation.GetBinderFactory(syntax.SyntaxTree);
                binder = binderFactory.GetBinder(syntax);
            }
            Debug.Assert(binder.GetBinder(syntax) == null);
            return binder;
        }

        private void NullableAnalyzeParameterDefaultValueFromAttributes()
        {
            var parameterSyntax = this.CSharpSyntaxNode;
            if (parameterSyntax == null)
            {
                // If there is no syntax at all for the parameter, it means we are in a situation like
                // a property setter whose 'value' parameter has a default value from attributes.
                // There isn't a sensible use for this in the language, so we just bail in such scenarios.
                return;
            }

            // The syntax span used to determine whether the attribute value is in a nullable-enabled
            // context is larger than necessary - it includes the entire attribute list rather than the specific
            // default value attribute which is used in AttributeSemanticModel.IsNullableAnalysisEnabled().
            var attributes = parameterSyntax.AttributeLists.Node;
            if (attributes is null || !NullableWalker.NeedsAnalysis(DeclaringCompilation, attributes))
            {
                return;
            }

            var defaultValue = DefaultValueFromAttributes;
            if (defaultValue == null || defaultValue.IsBad)
            {
                return;
            }

            var binder = GetDefaultParameterValueBinder(parameterSyntax);

            // Nullable warnings *within* the attribute argument (such as a W-warning for `(string)null`)
            // are reported when we nullable-analyze attribute arguments separately from here.
            // However, this analysis of the constant value's compatibility with the parameter
            // needs to wait until the attributes are populated on the parameter symbol.
            var parameterEqualsValue = new BoundParameterEqualsValue(
                parameterSyntax,
                this,
                ImmutableArray<LocalSymbol>.Empty,
                // note that if the parameter type conflicts with the default value from attributes,
                // we will just get a bad constant value above and return early.
                new BoundLiteral(parameterSyntax, defaultValue, Type));

            var diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(diagnostics.DiagnosticBag != null);
            NullableWalker.AnalyzeIfNeeded(binder, parameterEqualsValue, parameterSyntax, diagnostics.DiagnosticBag);
            AddDeclarationDiagnostics(diagnostics);
            diagnostics.Free();
        }

        // This method *must not* depend on attributes on the parameter symbol.
        // Otherwise we will have cycles when binding usage of attributes whose constructors have optional parameters
        private ConstantValue MakeDefaultExpression(BindingDiagnosticBag diagnostics, out Binder? binder, out BoundParameterEqualsValue? parameterEqualsValue)
        {
            binder = null;
            parameterEqualsValue = null;

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

            binder = GetDefaultParameterValueBinder(defaultSyntax);
            Binder binderForDefault = binder.CreateBinderForParameterDefaultValue(this, defaultSyntax);
            Debug.Assert(binderForDefault.InParameterDefaultValue);
            Debug.Assert(binderForDefault.ContainingMemberOrLambda == ContainingSymbol);

            parameterEqualsValue = binderForDefault.BindParameterDefaultValue(defaultSyntax, this, diagnostics, out var valueBeforeConversion);
            if (valueBeforeConversion.HasErrors)
            {
                return ConstantValue.Bad;
            }

            BoundExpression convertedExpression = parameterEqualsValue.Value;
            bool hasErrors = ParameterHelpers.ReportDefaultParameterErrors(binder, ContainingSymbol, parameterSyntax, this, valueBeforeConversion, convertedExpression, diagnostics);
            if (hasErrors)
            {
                return ConstantValue.Bad;
            }

            // If we have something like M(double? x = 1) then the expression we'll get is (double?)1, which
            // does not have a constant value. The constant value we want is (double)1.
            // The default literal conversion is an exception: (double)default would give the wrong value for M(double? x = default).
            if (convertedExpression.ConstantValue == null && convertedExpression.Kind == BoundKind.Conversion &&
                ((BoundConversion)convertedExpression).ConversionKind != ConversionKind.DefaultLiteral)
            {
                if (parameterType.Type.IsNullableType())
                {
                    convertedExpression = binder.GenerateConversionForAssignment(parameterType.Type.GetNullableUnderlyingType(),
                        valueBeforeConversion, diagnostics, Binder.ConversionForAssignmentFlags.DefaultParameter);
                }
            }

            if (this.IsNullChecked && convertedExpression.ConstantValue?.IsNull == true)
            {
                diagnostics.Add(ErrorCode.WRN_NullCheckedHasDefaultNull, Locations.FirstOrNone(), this.Name);
            }

            // represent default(struct) by a Null constant:
            var value = convertedExpression.ConstantValue ?? ConstantValue.Null;
            return value;
        }

#nullable disable

        public override string MetadataName
        {
            get
            {
                // The metadata parameter name should be the name used in the partial definition.

                var sourceMethod = this.ContainingSymbol as SourceOrdinaryMethodSymbol;
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

        protected virtual IAttributeTargetSymbol AttributeOwner => this;

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner => AttributeOwner;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation => AttributeLocation.Parameter;

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                if (SynthesizedRecordPropertySymbol.HaveCorrespondingSynthesizedRecordPropertySymbol(this))
                {
                    return AttributeLocation.Parameter | AttributeLocation.Property | AttributeLocation.Field;
                }

                return AttributeLocation.Parameter;
            }
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
                var sourceMethod = this.ContainingSymbol as SourceOrdinaryMethodSymbol;
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

            var sourceMethod = this.ContainingSymbol as SourceOrdinaryMethodSymbol;
            if ((object)sourceMethod == null)
            {
                return OneOrMany.Create(attributes);
            }

            SyntaxList<AttributeListSyntax> otherAttributes;

            // if this is a definition get the implementation and vice versa
            SourceOrdinaryMethodSymbol otherPart = sourceMethod.OtherPartOfPartial;
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
        internal ParameterWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (ParameterWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal ParameterEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (ParameterEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal sealed override CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            if (_lazyCustomAttributesBag == null || !_lazyCustomAttributesBag.IsSealed)
            {
                SourceParameterSymbol copyFrom = this.BoundAttributesSource;

                // prevent infinite recursion:
                Debug.Assert(!ReferenceEquals(copyFrom, this));

                bool bagCreatedOnThisThread;
                if ((object)copyFrom != null)
                {
                    var attributesBag = copyFrom.GetAttributesBag();
                    bagCreatedOnThisThread = Interlocked.CompareExchange(ref _lazyCustomAttributesBag, attributesBag, null) == null;
                }
                else
                {
                    var attributeSyntax = this.GetAttributeDeclarations();
                    bagCreatedOnThisThread = LoadAndValidateAttributes(attributeSyntax, ref _lazyCustomAttributesBag, binderOpt: WithTypeParametersBinderOpt);
                }

                if (bagCreatedOnThisThread)
                {
                    NullableAnalyzeParameterDefaultValueFromAttributes();
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

#nullable enable
        internal override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
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
                else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CallerArgumentExpressionAttribute))
                {
                    var index = -1;
                    var (attributeData, _) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out _);
                    if (!attributeData.HasErrors)
                    {
                        var constructorArguments = attributeData.CommonConstructorArguments;
                        Debug.Assert(constructorArguments.Length == 1);
                        if (constructorArguments[0].TryDecodeValue(SpecialType.System_String, out string? parameterName))
                        {
                            var parameters = ContainingSymbol.GetParameters();
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                if (parameters[i].Name.Equals(parameterName, StringComparison.Ordinal))
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                    }

                    arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().CallerArgumentExpressionParameterIndex = index;
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        private (CSharpAttributeData?, BoundAttribute?) EarlyDecodeAttributeForDefaultParameterValue(AttributeDescription description, ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            Debug.Assert(description.Equals(AttributeDescription.DefaultParameterValueAttribute) ||
                description.Equals(AttributeDescription.DecimalConstantAttribute) ||
                description.Equals(AttributeDescription.DateTimeConstantAttribute));

            bool hasAnyDiagnostics;
            var (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
            ConstantValue value;
            if (attributeData.HasErrors)
            {
                value = ConstantValue.Bad;
                hasAnyDiagnostics = true;
            }
            else
            {
                value = DecodeDefaultParameterValueAttribute(description, attributeData, arguments.AttributeSyntax, diagnose: false, diagnosticsOpt: null);
            }

            var paramData = arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>();
            if (paramData.DefaultParameterValue == ConstantValue.Unset)
            {
                paramData.DefaultParameterValue = value;
            }

            return !hasAnyDiagnostics ? (attributeData, boundAttribute) : (null, null);
        }
#nullable disable

        internal override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);
            Debug.Assert(AttributeDescription.InterpolatedStringHandlerArgumentAttribute.Signatures.Length == 2);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

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
                    diagnostics.Add(ErrorCode.ERR_DefaultValueUsedWithAttributes, arguments.AttributeSyntaxOpt.Name.Location);
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ParamArrayAttribute))
            {
                // error CS0674: Do not use 'System.ParamArrayAttribute'. Use the 'params' keyword instead.
                diagnostics.Add(ErrorCode.ERR_ExplicitParamArray, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.InAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasInAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.OutAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasOutAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                MarshalAsAttributeDecoder<ParameterWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.Parameter, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IDispatchConstantAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasIDispatchConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IUnknownConstantAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasIUnknownConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerLineNumberAttribute))
            {
                ValidateCallerLineNumberAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerFilePathAttribute))
            {
                ValidateCallerFilePathAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerMemberNameAttribute))
            {
                ValidateCallerMemberNameAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CallerArgumentExpressionAttribute))
            {
                ValidateCallerArgumentExpressionAttribute(arguments.AttributeSyntaxOpt, attribute, diagnostics);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.DynamicAttribute | ReservedAttributes.IsReadOnlyAttribute | ReservedAttributes.IsUnmanagedAttribute | ReservedAttributes.IsByRefLikeAttribute | ReservedAttributes.TupleElementNamesAttribute | ReservedAttributes.NullableAttribute | ReservedAttributes.NativeIntegerAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.AllowNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasAllowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DisallowNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasDisallowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MaybeNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasMaybeNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MaybeNullWhenAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().MaybeNullWhenAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(AttributeDescription.MaybeNullWhenAttribute, attribute);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasNotNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullWhenAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().NotNullWhenAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(AttributeDescription.NotNullWhenAttribute, attribute);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DoesNotReturnIfAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().DoesNotReturnIfAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(AttributeDescription.DoesNotReturnIfAttribute, attribute);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NotNullIfNotNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().AddNotNullIfParameterNotNull(attribute.DecodeNotNullIfNotNullAttribute());
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.EnumeratorCancellationAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasEnumeratorCancellationAttribute = true;
                ValidateCancellationTokenAttribute(arguments.AttributeSyntaxOpt, (BindingDiagnosticBag)arguments.Diagnostics);
            }
            else if (attribute.GetTargetAttributeSignatureIndex(this, AttributeDescription.InterpolatedStringHandlerArgumentAttribute) is (0 or 1) and var index)
            {
                DecodeInterpolatedStringHandlerArgumentAttribute(ref arguments, diagnostics, index);
            }
        }

        private static bool? DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(AttributeDescription description, CSharpAttributeData attribute)
        {
            var arguments = attribute.CommonConstructorArguments;
            return arguments.Length == 1 && arguments[0].TryDecodeValue(SpecialType.System_Boolean, out bool value) ?
                (bool?)value :
                null;
        }

        private void DecodeDefaultParameterValueAttribute(AttributeDescription description, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            var attribute = arguments.Attribute;
            var syntax = arguments.AttributeSyntaxOpt;
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

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
        private void VerifyParamDefaultValueMatchesAttributeIfAny(ConstantValue value, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            var data = GetEarlyDecodedWellKnownAttributeData();
            if (data != null)
            {
                var attrValue = data.DefaultParameterValue;
                if ((attrValue != ConstantValue.Unset) &&
                    (value != attrValue))
                {
                    // CS8017: The parameter has multiple distinct default values.
                    diagnostics.Add(ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute, syntax.Location);
                }
            }
        }

        private ConstantValue DecodeDefaultParameterValueAttribute(AttributeDescription description, CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, BindingDiagnosticBag diagnosticsOpt)
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

        private ConstantValue DecodeDefaultParameterValueAttribute(CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, BindingDiagnosticBag diagnosticsOpt)
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
                ((NamedTypeSymbol)arg.TypeInternal).EnumUnderlyingType.SpecialType :
                arg.TypeInternal.SpecialType;

            var compilation = this.DeclaringCompilation;
            var constantValueDiscriminator = ConstantValue.GetDiscriminator(specialType);
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnosticsOpt, ContainingAssembly);
            if (constantValueDiscriminator == ConstantValueTypeDiscriminator.Bad)
            {
                if (arg.Kind != TypedConstantKind.Array && arg.ValueInternal == null)
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
                        diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueBadValueType, node.Name.Location, arg.TypeInternal);
                    }
                    return ConstantValue.Bad;
                }
            }
            else if (!compilation.Conversions.ClassifyConversionFromType((TypeSymbol)arg.TypeInternal, this.Type, isChecked: false, ref useSiteInfo).Kind.IsImplicitConversion())
            {
                // error CS1908: The type of the argument to the DefaultParameterValue attribute must match the parameter type
                if (diagnose)
                {
                    diagnosticsOpt.Add(ErrorCode.ERR_DefaultValueTypeMustMatch, node.Name.Location);
                    diagnosticsOpt.Add(node.Name.Location, useSiteInfo);
                }
                return ConstantValue.Bad;
            }

            if (diagnose)
            {
                diagnosticsOpt.Add(node.Name.Location, useSiteInfo);
            }

            return ConstantValue.Create(arg.ValueInternal, constantValueDiscriminator);
        }

        private bool IsValidCallerInfoContext(AttributeSyntax node) => !ContainingSymbol.IsExplicitInterfaceImplementation()
                                                                    && !ContainingSymbol.IsOperator()
                                                                    && !IsOnPartialImplementation(node);

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

        private void ValidateCallerLineNumberAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4024: The CallerLineNumberAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerLineNumberConversion(TypeWithAnnotations.Type, ref useSiteInfo))
            {
                // CS4017: CallerLineNumberAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol intType = compilation.GetSpecialType(SpecialType.System_Int32);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerLineNumberParam, node.Name.Location, intType, TypeWithAnnotations.Type);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4020: The CallerLineNumberAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue, node.Name.Location);
            }

            diagnostics.Add(node.Name.Location, useSiteInfo);
        }

        private void ValidateCallerFilePathAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4025: The CallerFilePathAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerInfoStringConversion(TypeWithAnnotations.Type, ref useSiteInfo))
            {
                // CS4018: CallerFilePathAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerFilePathParam, node.Name.Location, stringType, TypeWithAnnotations.Type);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4021: The CallerFilePathAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue, node.Name.Location);
            }
            else if (IsCallerLineNumber)
            {
                // CS7082: The CallerFilePathAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerLineNumberAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            diagnostics.Add(node.Name.Location, useSiteInfo);
        }

        private void ValidateCallerMemberNameAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4026: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerInfoStringConversion(TypeWithAnnotations.Type, ref useSiteInfo))
            {
                // CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerMemberNameParam, node.Name.Location, stringType, TypeWithAnnotations.Type);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS4022: The CallerMemberNameAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue, node.Name.Location);
            }
            else if (IsCallerLineNumber)
            {
                // CS7081: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerLineNumberAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (IsCallerFilePath)
            {
                // CS7080: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerFilePathAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            diagnostics.Add(node.Name.Location, useSiteInfo);
        }

        private void ValidateCallerArgumentExpressionAttribute(AttributeSyntax node, CSharpAttributeData attribute, BindingDiagnosticBag diagnostics)
        {
            // We intentionally don't report an error for earlier language versions here. The attribute already existed
            // before the feature was developed. The error is only reported when the binder supplies a value
            // based on the attribute.
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS8966: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (!compilation.Conversions.HasCallerInfoStringConversion(TypeWithAnnotations.Type, ref useSiteInfo))
            {
                // CS8959: CallerArgumentExpressionAttribute cannot be applied because there are no standard conversions from type '{0}' to type '{1}'
                TypeSymbol stringType = compilation.GetSpecialType(SpecialType.System_String);
                diagnostics.Add(ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam, node.Name.Location, stringType, TypeWithAnnotations.Type);
            }
            else if (!HasExplicitDefaultValue && !ContainingSymbol.IsPartialImplementation()) // attribute applied to parameter without default
            {
                // Unconsumed location checks happen first, so we require a default value.

                // CS8964: The CallerArgumentExpressionAttribute may only be applied to parameters with default values
                diagnostics.Add(ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue, node.Name.Location);
            }
            else if (IsCallerLineNumber)
            {
                // CS8960: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerLineNumberAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (IsCallerFilePath)
            {
                // CS8961: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerFilePathAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (IsCallerMemberName)
            {
                // CS8962: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerMemberNameAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (attribute.CommonConstructorArguments.Length == 1 &&
                GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex == -1)
            {
                // CS8963: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is applied with an invalid parameter name.
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }
            else if (GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex == Ordinal)
            {
                // CS8965: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect because it's self-referential.
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            diagnostics.Add(node.Name.Location, useSiteInfo);
        }

        private void ValidateCancellationTokenAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            if (needsReporting())
            {
                diagnostics.Add(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, node.Name.Location, CSharpSyntaxNode.Identifier.ValueText);
            }

            bool needsReporting()
            {
                if (!Type.Equals(this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Threading_CancellationToken)))
                {
                    return true;
                }
                else if (this.ContainingSymbol is MethodSymbol method &&
                    method.IsAsync &&
                    method.ReturnType.OriginalDefinition.Equals(this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T)))
                {
                    // Note: async methods that return this type must be iterators. This is enforced elsewhere
                    return false;
                }

                return true;
            }
        }

#nullable enable
        private void DecodeInterpolatedStringHandlerArgumentAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments, BindingDiagnosticBag diagnostics, int attributeIndex)
        {
            Debug.Assert(attributeIndex is 0 or 1);
            Debug.Assert(arguments.Attribute.IsTargetAttribute(this, AttributeDescription.InterpolatedStringHandlerArgumentAttribute) && arguments.Attribute.CommonConstructorArguments.Length == 1);
            Debug.Assert(arguments.AttributeSyntaxOpt is not null);

            var errorLocation = arguments.AttributeSyntaxOpt.Location;

            if (Type is not NamedTypeSymbol { IsInterpolatedStringHandlerType: true } handlerType)
            {
                // '{0}' is not an interpolated string handler type.
                diagnostics.Add(ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType, errorLocation, Type);
                setInterpolatedStringHandlerAttributeError(ref arguments);
                return;
            }

            if (this is LambdaParameterSymbol)
            {
                // Lambda parameters will ignore this attribute at usage
                diagnostics.Add(ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters, errorLocation);
            }

            TypedConstant constructorArgument = arguments.Attribute.CommonConstructorArguments[0];

            ImmutableArray<ParameterSymbol> containingSymbolParameters = ContainingSymbol.GetParameters();

            ImmutableArray<int> parameterOrdinals;
            ArrayBuilder<ParameterSymbol?> parameters;
            if (attributeIndex == 0)
            {
                if (decodeName(constructorArgument, ref arguments) is not (int ordinal, var parameter))
                {
                    // If an error needs to be reported, it will already have been reported by another step.
                    setInterpolatedStringHandlerAttributeError(ref arguments);
                    return;
                }

                parameterOrdinals = ImmutableArray.Create(ordinal);
                parameters = ArrayBuilder<ParameterSymbol?>.GetInstance(1);
                parameters.Add(parameter);
            }
            else if (attributeIndex == 1)
            {
                if (constructorArgument.IsNull)
                {
                    setInterpolatedStringHandlerAttributeError(ref arguments);
                    // null is not a valid parameter name. To get access to the receiver of an instance method, use the empty string as the parameter name.
                    diagnostics.Add(ErrorCode.ERR_NullInvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt!.Location);
                    return;
                }

                bool hadError = false;
                parameters = ArrayBuilder<ParameterSymbol?>.GetInstance(constructorArgument.Values.Length);
                var ordinalsBuilder = ArrayBuilder<int>.GetInstance(constructorArgument.Values.Length);
                foreach (var nestedArgument in constructorArgument.Values)
                {
                    if (decodeName(nestedArgument, ref arguments) is (int ordinal, var parameter) && !hadError)
                    {
                        parameters.Add(parameter);
                        ordinalsBuilder.Add(ordinal);
                    }
                    else
                    {
                        hadError = true;
                    }
                }

                if (hadError)
                {
                    parameters.Free();
                    ordinalsBuilder.Free();
                    setInterpolatedStringHandlerAttributeError(ref arguments);
                    return;
                }

                parameterOrdinals = ordinalsBuilder.ToImmutableAndFree();
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            var parameterWellKnownAttributeData = arguments.GetOrCreateData<ParameterWellKnownAttributeData>();
            parameterWellKnownAttributeData.InterpolatedStringHandlerArguments = parameterOrdinals;

            (int Ordinal, ParameterSymbol? Parameter)? decodeName(TypedConstant constant, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            {
                Debug.Assert(arguments.AttributeSyntaxOpt is not null);
                if (constant.IsNull)
                {
                    // null is not a valid parameter name. To get access to the receiver of an instance method, use the empty string as the parameter name.
                    diagnostics.Add(ErrorCode.ERR_NullInvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt.Location);
                    return null;
                }

                if (constant.TypeInternal is not { SpecialType: SpecialType.System_String })
                {
                    // There has already been an error reported. Just return null.
                    return null;
                }

                var name = constant.DecodeValue<string>(SpecialType.System_String);
                Debug.Assert(name != null);
                if (name == "")
                {
                    // Name refers to the "this" instance parameter.
                    if (!ContainingSymbol.RequiresInstanceReceiver() || ContainingSymbol is MethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.DelegateInvoke or MethodKind.LambdaMethod })
                    {
                        // '{0}' is not an instance method, the receiver cannot be an interpolated string handler argument.
                        diagnostics.Add(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt.Location, ContainingSymbol);
                        return null;
                    }

                    return (BoundInterpolatedStringArgumentPlaceholder.InstanceParameter, null);
                }

                var parameter = containingSymbolParameters.FirstOrDefault(static (param, name) => string.Equals(param.Name, name, StringComparison.Ordinal), name);
                if (parameter is null)
                {
                    // '{0}' is not a valid parameter name from '{1}'.
                    diagnostics.Add(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt.Location, name, ContainingSymbol);
                    return null;
                }

                if ((object)parameter == this)
                {
                    // InterpolatedStringHandlerArgumentAttribute arguments cannot refer to the parameter the attribute is used on.
                    diagnostics.Add(ErrorCode.ERR_CannotUseSelfAsInterpolatedStringHandlerArgument, errorLocation);
                    return null;
                }

                if (parameter.Ordinal > Ordinal)
                {
                    // Parameter '{0}' occurs after '{1}' in the parameter list, but is used as an argument for interpolated string handler conversions.
                    // This will require the caller to reorder parameters with named arguments at the call site. Consider putting the interpolated
                    // string handler parameter after all arguments involved.
                    diagnostics.Add(ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter, errorLocation, parameter.Name, this.Name);
                }

                return (parameter.Ordinal, parameter);
            }

            static void setInterpolatedStringHandlerAttributeError(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().InterpolatedStringHandlerArguments = default;
            }
        }
#nullable disable

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            var data = (ParameterWellKnownAttributeData)decodedData;
            if (data != null)
            {
                switch (RefKind)
                {
                    case RefKind.Ref:
                        if (data.HasOutAttribute && !data.HasInAttribute)
                        {
                            // error CS0662: Cannot specify the Out attribute on a ref parameter without also specifying the In attribute.
                            diagnostics.Add(ErrorCode.ERR_OutAttrOnRefParam, this.Locations[0]);
                        }
                        break;
                    case RefKind.Out:
                        if (data.HasInAttribute)
                        {
                            // error CS0036: An out parameter cannot have the In attribute.
                            diagnostics.Add(ErrorCode.ERR_InAttrOnOutParam, this.Locations[0]);
                        }
                        break;
                    case RefKind.In:
                        if (data.HasOutAttribute)
                        {
                            // error CS8355: An in parameter cannot have the Out attribute.
                            diagnostics.Add(ErrorCode.ERR_OutAttrOnInParam, this.Locations[0]);
                        }
                        break;
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
            => base.IsMetadataIn || GetDecodedWellKnownAttributeData()?.HasInAttribute == true;

        internal sealed override bool IsMetadataOut
            => base.IsMetadataOut || GetDecodedWellKnownAttributeData()?.HasOutAttribute == true;

        internal sealed override MarshalPseudoCustomAttributeData MarshallingInformation
            => GetDecodedWellKnownAttributeData()?.MarshallingInformation;

        public override bool IsParams => (_parameterSyntaxKind & ParameterSyntaxKind.ParamsParameter) != 0;

        internal override bool IsExtensionMethodThis => (_parameterSyntaxKind & ParameterSyntaxKind.ExtensionThisParameter) != 0;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            _ = this.GetAttributes();
            _ = this.ExplicitDefaultConstantValue;
            state.SpinWaitComplete(CompletionPart.ComplexParameterSymbolAll, cancellationToken);
        }
    }

    internal sealed class SourceComplexParameterSymbolWithCustomModifiersPrecedingByRef : SourceComplexParameterSymbol
    {
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

        internal SourceComplexParameterSymbolWithCustomModifiersPrecedingByRef(
            Symbol owner,
            int ordinal,
            TypeWithAnnotations parameterType,
            RefKind refKind,
            ImmutableArray<CustomModifier> refCustomModifiers,
            string name,
            ImmutableArray<Location> locations,
            SyntaxReference syntaxRef,
            bool isParams,
            bool isExtensionMethodThis)
            : base(owner, ordinal, parameterType, refKind, name, locations, syntaxRef, isParams, isExtensionMethodThis)
        {
            Debug.Assert(!refCustomModifiers.IsEmpty);

            _refCustomModifiers = refCustomModifiers;

            Debug.Assert(refKind != RefKind.None || _refCustomModifiers.IsEmpty);
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _refCustomModifiers;
    }
}
