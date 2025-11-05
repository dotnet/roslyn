// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A source parameter, potentially with a default value, attributes, etc.
    /// </summary>
    internal abstract class SourceComplexParameterSymbolBase : SourceParameterSymbol, IAttributeTargetSymbol
    {
        [Flags]
        private enum ParameterFlags : byte
        {
            None = 0,
            HasParamsModifier = 0x1,
            ParamsParameter = 0x02, // Value of this flag is either derived from HasParamsModifier, or inherited from overridden member 
            ExtensionThisParameter = 0x04,
            DefaultParameter = 0x08,
        }

        private readonly SyntaxReference _syntaxRef;
        private readonly ParameterFlags _parameterSyntaxKind;

        private ThreeState _lazyHasOptionalAttribute;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
#nullable enable
        protected ConstantValue? _lazyDefaultSyntaxValue;
#nullable disable

        protected SourceComplexParameterSymbolBase(
            Symbol owner,
            int ordinal,
            RefKind refKind,
            string name,
            Location location,
            SyntaxReference syntaxRef,
            bool hasParamsModifier,
            bool isParams,
            bool isExtensionMethodThis,
            ScopedKind scope)
            : base(owner, ordinal, refKind, scope, name, location)
        {
            Debug.Assert((syntaxRef == null) || (syntaxRef.GetSyntax().IsKind(SyntaxKind.Parameter)));

            _lazyHasOptionalAttribute = ThreeState.Unknown;
            _syntaxRef = syntaxRef;

            if (hasParamsModifier)
            {
                _parameterSyntaxKind |= ParameterFlags.HasParamsModifier;
            }

            if (isParams)
            {
                _parameterSyntaxKind |= ParameterFlags.ParamsParameter;
            }

            if (isExtensionMethodThis)
            {
                _parameterSyntaxKind |= ParameterFlags.ExtensionThisParameter;
            }

            var parameterSyntax = this.ParameterSyntax;
            if (parameterSyntax != null && parameterSyntax.Default != null)
            {
                _parameterSyntaxKind |= ParameterFlags.DefaultParameter;
            }

            _lazyDefaultSyntaxValue = ConstantValue.Unset;
        }

        private Binder WithTypeParametersBinderOpt => (ContainingSymbol as SourceMethodSymbol)?.WithTypeParametersBinder;

        internal sealed override SyntaxReference SyntaxReference => _syntaxRef;

        private ParameterSyntax ParameterSyntax => (ParameterSyntax)_syntaxRef?.GetSyntax();

        public override bool IsDiscard => false;

#nullable enable
        internal sealed override ConstantValue? ExplicitDefaultConstantValue
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

        internal sealed override ConstantValue? DefaultValueFromAttributes
        {
            get
            {
                ParameterEarlyWellKnownAttributeData data = GetEarlyDecodedWellKnownAttributeData();
                return (data != null && data.DefaultParameterValue != ConstantValue.Unset) ? data.DefaultParameterValue : null;
            }
        }
#nullable disable

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

        internal override bool HasEnumeratorCancellationAttribute
        {
            get
            {
                ParameterWellKnownAttributeData attributeData = GetDecodedWellKnownAttributeData();
                return attributeData?.HasEnumeratorCancellationAttribute == true;
            }
        }

#nullable enable

        internal sealed override ScopedKind EffectiveScope
        {
            get
            {
                var scope = CalculateEffectiveScopeIgnoringAttributes();
                if (scope != ScopedKind.None &&
                    HasUnscopedRefAttribute &&
                    UseUpdatedEscapeRules)
                {
                    return ScopedKind.None;
                }
                return scope;
            }
        }

        internal override bool HasUnscopedRefAttribute => GetEarlyDecodedWellKnownAttributeData()?.HasUnscopedRefAttribute == true;

        internal static SyntaxNode? GetDefaultValueSyntaxForIsNullableAnalysisEnabled(ParameterSyntax? parameterSyntax) =>
            parameterSyntax?.Default?.Value;

        /// <summary>
        /// Returns the bound default value syntax from the parameter, if it exists.
        /// Note that this method will only return a non-null value if the
        /// default value was supplied in syntax. If the value is supplied through the DefaultParameterValue
        /// attribute, then ExplicitDefaultValue will be non-null but this method will return null.
        /// However, if ExplicitDefaultValue is null, this method should always return null.
        /// </summary>
        public BoundParameterEqualsValue? BindParameterEqualsValue()
        {
            // Rebind default value expression, ignoring any diagnostics, in order to produce
            // a bound node that can be used for passes such as definite assignment.
            MakeDefaultExpression(BindingDiagnosticBag.Discarded, out var _, out var parameterEqualsValue);
            return parameterEqualsValue;
        }

        private ConstantValue? DefaultSyntaxValue
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
                            GetDefaultValueSyntaxForIsNullableAnalysisEnabled(ParameterSyntax) is { } valueSyntax)
                        {
                            NullableWalker.AnalyzeIfNeeded(binder, parameterEqualsValue, valueSyntax, diagnostics.DiagnosticBag);
                        }

                        Debug.Assert(_lazyDefaultSyntaxValue is not null);
                        if (!_lazyDefaultSyntaxValue.IsBad)
                        {
                            VerifyParamDefaultValueMatchesAttributeIfAny(_lazyDefaultSyntaxValue, parameterEqualsValue.Value.Syntax, diagnostics);

                            // Ensure availability of `DecimalConstantAttribute`.
                            if (_lazyDefaultSyntaxValue.IsDecimal &&
                                DefaultValueFromAttributes == ConstantValue.NotAvailable)
                            {
                                Binder.ReportUseSiteDiagnosticForSynthesizedAttribute(DeclaringCompilation,
                                    WellKnownMember.System_Runtime_CompilerServices_DecimalConstantAttribute__ctor,
                                    diagnostics,
                                    parameterEqualsValue.Value.Syntax.Location);
                            }
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
            var parameterSyntax = this.ParameterSyntax;
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
        private ConstantValue? MakeDefaultExpression(BindingDiagnosticBag diagnostics, out Binder? binder, out BoundParameterEqualsValue? parameterEqualsValue)
        {
            binder = null;
            parameterEqualsValue = null;

            var parameterSyntax = this.ParameterSyntax;
            if (parameterSyntax == null)
            {
                return null;
            }

            var defaultSyntax = parameterSyntax.Default;
            if (defaultSyntax == null)
            {
                return null;
            }

            MessageID.IDS_FeatureOptionalParameter.CheckFeatureAvailability(diagnostics, defaultSyntax.EqualsToken);

            binder = GetDefaultParameterValueBinder(defaultSyntax);
            binder = binder.CreateBinderForParameterDefaultValue(this, defaultSyntax);
            Debug.Assert(binder.InParameterDefaultValue);
            Debug.Assert(binder.ContainingMemberOrLambda == ContainingSymbol);

            parameterEqualsValue = binder.BindParameterDefaultValue(defaultSyntax, this, diagnostics, out var valueBeforeConversion);
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
            if (convertedExpression.ConstantValueOpt == null && convertedExpression.Kind == BoundKind.Conversion &&
                ((BoundConversion)convertedExpression).ConversionKind != ConversionKind.DefaultLiteral)
            {
                if (Type.IsNullableType())
                {
                    convertedExpression = binder.GenerateConversionForAssignment(Type.GetNullableUnderlyingType(),
                        valueBeforeConversion, diagnostics, Binder.ConversionForAssignmentFlags.DefaultParameter);
                }
            }

            // represent default(struct) by a Null constant:
            var value = convertedExpression.ConstantValueOpt ?? ConstantValue.Null;
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

#nullable enable
        /// <summary>
        /// Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source parameter symbols.
        /// </summary>
        /// <remarks>
        /// This is inconsistent with analogous 'BoundAttributesSource' on other symbols.
        /// Usually the definition part is the source, but for parameters the implementation part is the source.
        /// This affects the location of diagnostics among other things.
        /// </remarks>
        private SourceParameterSymbol? BoundAttributesSource
            => PartialImplementationPart;

        protected SourceParameterSymbol? PartialImplementationPart
        {
            get
            {
                ImmutableArray<ParameterSymbol> implParameters = this.ContainingSymbol.GetPartialImplementationPart()?.GetParameters() ?? default;

                if (implParameters.IsDefault)
                {
                    return null;
                }

                Debug.Assert(!this.ContainingSymbol.IsPartialImplementation());
                return (SourceParameterSymbol)implParameters[this.Ordinal];
            }
        }

        protected SourceParameterSymbol? PartialDefinitionPart
        {
            get
            {
                ImmutableArray<ParameterSymbol> defParameters = this.ContainingSymbol.GetPartialDefinitionPart()?.GetParameters() ?? default;

                if (defParameters.IsDefault)
                {
                    return null;
                }

                Debug.Assert(!this.ContainingSymbol.IsPartialDefinition());
                return (SourceParameterSymbol)defParameters[this.Ordinal];
            }
        }

        internal sealed override SyntaxList<AttributeListSyntax> AttributeDeclarationList
        {
            get
            {
                var syntax = this.ParameterSyntax;
                return (syntax != null) ? syntax.AttributeLists : default(SyntaxList<AttributeListSyntax>);
            }
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares attributes for this parameter symbol.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Attributes on parameters in partial members are owned by the parameter in the implementation part.
            // If this symbol has a non-null PartialImplementationPart, we should have accessed this method through that implementation symbol.
            Debug.Assert(PartialImplementationPart is null);

            if (PartialDefinitionPart is { } definitionPart)
            {
                return OneOrMany.Create(
                    AttributeDeclarationList,
                    definitionPart.AttributeDeclarationList);
            }
            else
            {
                return OneOrMany.Create(AttributeDeclarationList);
            }
        }
#nullable disable

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

        /// <summary>
        /// Binds attributes applied to this parameter.
        /// </summary>
        public ImmutableArray<(CSharpAttributeData, BoundAttribute)> BindParameterAttributes()
        {
            return BindAttributes(GetAttributeDeclarations(), WithTypeParametersBinderOpt);
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
            else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.UnscopedRefAttribute))
            {
                // We can't bind the attribute here because that might lead to a cycle.
                // Instead, simply record that the attribute exists and bind later.
                arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().HasUnscopedRefAttribute = true;
                return (null, null);
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
                        if (constructorArguments[0].TryDecodeValue(SpecialType.System_String, out string? parameterName)
                            && parameterName is not null)
                        {
                            index = GetCallerArgumentExpressionParameterIndex(this, parameterName);
                        }
                    }

                    arguments.GetOrCreateData<ParameterEarlyWellKnownAttributeData>().CallerArgumentExpressionParameterIndex = index;
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        /// <summary>
        /// Given a parameter (marked with CallerArgumentExpression attribute) and the parameter name specified in the attribute,
        /// returns the index of the parameter the attribute refers to, or -1 if no such parameter exists.
        /// For new non-static extension members, the extension parameter is considered to be at index 0.
        /// </summary>
        internal static int GetCallerArgumentExpressionParameterIndex(ParameterSymbol parameter, string parameterName)
        {
            int offset = 0;
            Symbol containingSymbol = parameter.ContainingSymbol;
            if (containingSymbol.IsExtensionBlockMember() && !containingSymbol.IsStatic)
            {
                if (parameter.ContainingType.ExtensionParameter is { } extensionParameter
                    && extensionParameter.Name.Equals(parameterName, StringComparison.Ordinal))
                {
                    return 0;
                }

                offset = 1;
            }

            var parameters = containingSymbol.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name.Equals(parameterName, StringComparison.Ordinal))
                {
                    return i + offset;
                }
            }

            return -1;
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

        protected override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);
            Debug.Assert(AttributeDescription.InterpolatedStringHandlerArgumentAttribute.Signatures.Length == 2);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            if (attribute.IsTargetAttribute(AttributeDescription.DefaultParameterValueAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DefaultParameterValueAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DecimalConstantAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DecimalConstantAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DateTimeConstantAttribute))
            {
                // Attribute decoded and constant value stored during EarlyDecodeWellKnownAttribute.
                DecodeDefaultParameterValueAttribute(AttributeDescription.DateTimeConstantAttribute, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.OptionalAttribute))
            {
                Debug.Assert(_lazyHasOptionalAttribute == ThreeState.True);

                if (HasDefaultArgumentSyntax)
                {
                    // error CS1745: Cannot specify default parameter value in conjunction with DefaultParameterAttribute or OptionalAttribute
                    diagnostics.Add(ErrorCode.ERR_DefaultValueUsedWithAttributes, arguments.AttributeSyntaxOpt.Name.Location);
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ParamArrayAttribute) || attribute.IsTargetAttribute(AttributeDescription.ParamCollectionAttribute))
            {
                // error CS0674: Do not use 'System.ParamArrayAttribute'/'System.Runtime.CompilerServices.ParamCollectionAttribute'. Use the 'params' keyword instead.
                diagnostics.Add(ErrorCode.ERR_ExplicitParamArrayOrCollection, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.InAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasInAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.OutAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasOutAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.MarshalAsAttribute))
            {
                MarshalAsAttributeDecoder<ParameterWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.Parameter, MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.IDispatchConstantAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasIDispatchConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.IUnknownConstantAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasIUnknownConstantAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CallerLineNumberAttribute))
            {
                ValidateCallerLineNumberAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CallerFilePathAttribute))
            {
                ValidateCallerFilePathAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CallerMemberNameAttribute))
            {
                ValidateCallerMemberNameAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CallerArgumentExpressionAttribute))
            {
                ValidateCallerArgumentExpressionAttribute(arguments.AttributeSyntaxOpt, attribute, diagnostics);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.DynamicAttribute |
                ReservedAttributes.IsReadOnlyAttribute |
                ReservedAttributes.RequiresLocationAttribute |
                ReservedAttributes.IsUnmanagedAttribute |
                ReservedAttributes.IsByRefLikeAttribute |
                ReservedAttributes.TupleElementNamesAttribute |
                ReservedAttributes.NullableAttribute |
                ReservedAttributes.NativeIntegerAttribute |
                ReservedAttributes.ScopedRefAttribute |
                ReservedAttributes.ExtensionMarkerAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.AllowNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasAllowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DisallowNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasDisallowNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.MaybeNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasMaybeNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.MaybeNullWhenAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().MaybeNullWhenAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(attribute);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.NotNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasNotNullAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.NotNullWhenAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().NotNullWhenAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(attribute);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DoesNotReturnIfAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().DoesNotReturnIfAttribute = DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(attribute);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.NotNullIfNotNullAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().AddNotNullIfParameterNotNull(attribute.DecodeNotNullIfNotNullAttribute());
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.EnumeratorCancellationAttribute))
            {
                arguments.GetOrCreateData<ParameterWellKnownAttributeData>().HasEnumeratorCancellationAttribute = true;
                ValidateCancellationTokenAttribute(arguments.AttributeSyntaxOpt, (BindingDiagnosticBag)arguments.Diagnostics);
            }
            else if (attribute.GetTargetAttributeSignatureIndex(AttributeDescription.InterpolatedStringHandlerArgumentAttribute) is (0 or 1) and var index)
            {
                DecodeInterpolatedStringHandlerArgumentAttribute(ref arguments, diagnostics, index);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.UnscopedRefAttribute))
            {
                if (!this.UseUpdatedEscapeRules)
                {
                    diagnostics.Add(ErrorCode.WRN_UnscopedRefAttributeOldRules, arguments.AttributeSyntaxOpt.Location);
                }

                if (!this.IsValidUnscopedRefAttributeTarget())
                {
                    diagnostics.Add(ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget, arguments.AttributeSyntaxOpt.Location);
                }
                else if (DeclaredScope != ScopedKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_UnscopedScoped, arguments.AttributeSyntaxOpt.Location);
                }
            }
        }

        private bool IsValidUnscopedRefAttributeTarget()
        {
            return RefKind != RefKind.None || (HasParamsModifier && Type.IsRefLikeOrAllowsRefLikeType());
        }

        private static bool? DecodeMaybeNullWhenOrNotNullWhenOrDoesNotReturnIfAttribute(CSharpAttributeData attribute)
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

                if (this.RefKind == RefKind.RefReadOnlyParameter && this.IsOptional && this.ParameterSyntax.Default is null)
                {
                    // A default value is specified for 'ref readonly' parameter '{0}', but 'ref readonly' should be used only for references. Consider declaring the parameter as 'in'.
                    diagnostics.Add(ErrorCode.WRN_RefReadonlyParameterDefaultValue, syntax, this.Name);
                }
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
                    diagnosticsOpt.Add(node.Name, useSiteInfo);
                }
                return ConstantValue.Bad;
            }

            if (diagnose)
            {
                diagnosticsOpt.Add(node.Name, useSiteInfo);
            }

            return ConstantValue.Create(arg.ValueInternal, constantValueDiscriminator);
        }

        private bool IsValidCallerInfoContext(AttributeSyntax node) => !ContainingSymbol.IsExplicitInterfaceImplementation()
                                                                    && !ContainingSymbol.IsOperator()
                                                                    && !IsOnPartialImplementation(node);

#nullable enable
        /// <summary>
        /// Is the attribute syntax appearing on a parameter of a partial method implementation part?
        /// Since attributes are merged between the parts of a partial, we need to look at the syntax where the
        /// attribute appeared in the source to see if it corresponds to a partial method implementation part.
        /// </summary>
        private bool IsOnPartialImplementation(AttributeSyntax node)
        {
            // If we are asking this, the candidate attribute had better be contained in *some* attribute associated with this parameter syntactically
            Debug.Assert(this.GetAttributeDeclarations().Any(attrLists => attrLists.Any(attrList => attrList.Contains(node))));

            var implParameter = this.ContainingSymbol.IsPartialImplementation() ? this : PartialImplementationPart;
            if (implParameter?.AttributeDeclarationList is not { } implParameterAttributeList)
            {
                return false;
            }

            return implParameterAttributeList.Any(attrList => attrList.Attributes.Contains(node));
        }
#nullable disable

        private void ValidateCallerLineNumberAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4024: The CallerLineNumberAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation, node.Name.Location, ParameterSyntax.Identifier.ValueText);
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

            diagnostics.Add(node.Name, useSiteInfo);
        }

        private void ValidateCallerFilePathAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4025: The CallerFilePathAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation, node.Name.Location, ParameterSyntax.Identifier.ValueText);
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
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }

            diagnostics.Add(node.Name, useSiteInfo);
        }

        private void ValidateCallerMemberNameAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            CSharpCompilation compilation = this.DeclaringCompilation;
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);

            if (!IsValidCallerInfoContext(node))
            {
                // CS4026: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect because it applies to a
                //         member that is used in contexts that do not allow optional arguments
                diagnostics.Add(ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation, node.Name.Location, ParameterSyntax.Identifier.ValueText);
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
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }
            else if (IsCallerFilePath)
            {
                // CS7080: The CallerMemberNameAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerFilePathAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }

            diagnostics.Add(node.Name, useSiteInfo);
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
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation, node.Name.Location, ParameterSyntax.Identifier.ValueText);
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
                diagnostics.Add(ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }
            else if (IsCallerFilePath)
            {
                // CS8961: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerFilePathAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }
            else if (IsCallerMemberName)
            {
                // CS8962: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is overridden by the CallerMemberNameAttribute.
                diagnostics.Add(ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }
            else if (attribute.CommonConstructorArguments.Length == 1 &&
                GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex == -1)
            {
                // CS8963: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect. It is applied with an invalid parameter name.
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }
            else if (GetEarlyDecodedWellKnownAttributeData()?.CallerArgumentExpressionParameterIndex == getOrdinalIncludingExtensionParameter())
            {
                // CS8965: The CallerArgumentExpressionAttribute applied to parameter '{0}' will have no effect because it's self-referential.
                diagnostics.Add(ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential, node.Name.Location, ParameterSyntax.Identifier.ValueText);
            }

            diagnostics.Add(node.Name, useSiteInfo);
            return;

            int getOrdinalIncludingExtensionParameter()
            {
                int offset = 0;
                Symbol containingSymbol = this.ContainingSymbol;
                if (containingSymbol.IsExtensionBlockMember()
                    && !containingSymbol.IsStatic)
                {
                    // Note: the offset applies to all non-static extension methods,
                    // even in error scenarios where there is no extension parameter
                    offset = 1;
                }

                return this.Ordinal + offset;
            }
        }

        private void ValidateCancellationTokenAttribute(AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            if (needsReporting())
            {
                diagnostics.Add(ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage, node.Name.Location, ParameterSyntax.Identifier.ValueText);
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
            Debug.Assert(arguments.Attribute.IsTargetAttribute(AttributeDescription.InterpolatedStringHandlerArgumentAttribute) && arguments.Attribute.CommonConstructorArguments.Length == 1);
            Debug.Assert(arguments.AttributeSyntaxOpt is not null);

            if (Type is not NamedTypeSymbol { IsInterpolatedStringHandlerType: true } handlerType)
            {
                // '{0}' is not an interpolated string handler type.
                diagnostics.Add(ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType, arguments.AttributeSyntaxOpt.Location, Type);
                setInterpolatedStringHandlerAttributeError(ref arguments);
                return;
            }

            if (this is LambdaParameterSymbol)
            {
                // Lambda parameters will ignore this attribute at usage
                diagnostics.Add(ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters, arguments.AttributeSyntaxOpt.Location);
            }

            if (ContainingSymbol is SynthesizedExtensionMarker)
            {
                // Interpolated string handler arguments are not allowed in this context.
                diagnostics.Add(ErrorCode.ERR_InterpolatedStringHandlerArgumentDisallowed, arguments.AttributeSyntaxOpt.Location);
                setInterpolatedStringHandlerAttributeError(ref arguments);
                return;
            }

            TypedConstant constructorArgument = arguments.Attribute.CommonConstructorArguments[0];

            ImmutableArray<ParameterSymbol> containingSymbolParameters = ContainingSymbol.GetParameters();
            ParameterSymbol? extensionParameter = ContainingType.ExtensionParameter;

            ImmutableArray<int> parameterOrdinals;
            if (attributeIndex == 0)
            {
                if (decodeName(constructorArgument, ref arguments) is not int ordinal)
                {
                    // If an error needs to be reported, it will already have been reported by another step.
                    setInterpolatedStringHandlerAttributeError(ref arguments);
                    return;
                }

                parameterOrdinals = ImmutableArray.Create(ordinal);
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
                var ordinalsBuilder = ArrayBuilder<int>.GetInstance(constructorArgument.Values.Length);
                foreach (var nestedArgument in constructorArgument.Values)
                {
                    if (decodeName(nestedArgument, ref arguments) is int ordinal && !hadError)
                    {
                        ordinalsBuilder.Add(ordinal);
                    }
                    else
                    {
                        hadError = true;
                    }
                }

                if (hadError)
                {
                    ordinalsBuilder.Free();
                    setInterpolatedStringHandlerAttributeError(ref arguments);
                    return;
                }

                parameterOrdinals = ordinalsBuilder.ToImmutableAndFree();
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }

            var parameterWellKnownAttributeData = arguments.GetOrCreateData<ParameterWellKnownAttributeData>();
            parameterWellKnownAttributeData.InterpolatedStringHandlerArguments = parameterOrdinals;

            int? decodeName(TypedConstant constant, ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
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
                    if (!ContainingSymbol.RequiresInstanceReceiver()
                        || ContainingSymbol is MethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.DelegateInvoke or MethodKind.LambdaMethod }
                        || ContainingSymbol.IsExtensionBlockMember())
                    {
                        // '{0}' is not an instance method, the receiver or extension receiver parameter cannot be an interpolated string handler argument.
                        diagnostics.Add(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt.Location, ContainingSymbol);
                        return null;
                    }

                    return BoundInterpolatedStringArgumentPlaceholder.InstanceParameter;
                }

                if (string.Equals(extensionParameter?.Name, name, StringComparison.Ordinal))
                {
                    if (!ContainingSymbol.RequiresInstanceReceiver())
                    {
                        // '{0}' is not an instance method, the receiver or extension receiver parameter cannot be an interpolated string handler argument.
                        diagnostics.Add(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, arguments.AttributeSyntaxOpt.Location, ContainingSymbol);
                        return null;
                    }

                    return BoundInterpolatedStringArgumentPlaceholder.ExtensionReceiver;
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
                    diagnostics.Add(ErrorCode.ERR_CannotUseSelfAsInterpolatedStringHandlerArgument, arguments.AttributeSyntaxOpt.Location);
                    return null;
                }

                if (parameter.Ordinal > Ordinal)
                {
                    // Parameter '{0}' occurs after '{1}' in the parameter list, but is used as an argument for interpolated string handler conversions.
                    // This will require the caller to reorder parameters with named arguments at the call site. Consider putting the interpolated
                    // string handler parameter after all arguments involved.
                    diagnostics.Add(ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter, arguments.AttributeSyntaxOpt.Location, parameter.Name, this.Name);
                }

                return parameter.Ordinal;
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
                            diagnostics.Add(ErrorCode.ERR_OutAttrOnRefParam, this.GetFirstLocation());
                        }
                        break;
                    case RefKind.Out:
                        if (data.HasInAttribute)
                        {
                            // error CS0036: An out parameter cannot have the In attribute.
                            diagnostics.Add(ErrorCode.ERR_InAttrOnOutParam, this.GetFirstLocation());
                        }
                        break;
                    case RefKind.In:
                        if (data.HasOutAttribute)
                        {
                            // error CS8355: An in parameter cannot have the Out attribute.
                            diagnostics.Add(ErrorCode.ERR_OutAttrOnInParam, this.GetFirstLocation());
                        }
                        break;
                    case RefKind.RefReadOnlyParameter:
                        if (data.HasOutAttribute)
                        {
                            // error: A ref readonly parameter cannot have the Out attribute.
                            diagnostics.Add(ErrorCode.ERR_OutAttrOnRefReadonlyParam, this.GetFirstLocation());
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
                return (_parameterSyntaxKind & ParameterFlags.DefaultParameter) != 0;
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

        protected sealed override bool HasParamsModifier => (_parameterSyntaxKind & ParameterFlags.HasParamsModifier) != 0;

        public sealed override bool IsParamsArray => (_parameterSyntaxKind & ParameterFlags.ParamsParameter) != 0 && this.Type.IsSZArray();

        public sealed override bool IsParamsCollection => (_parameterSyntaxKind & ParameterFlags.ParamsParameter) != 0 && !this.Type.IsSZArray();

        internal override bool IsExtensionMethodThis => (_parameterSyntaxKind & ParameterFlags.ExtensionThisParameter) != 0;

        public abstract override ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        internal override void ForceComplete(SourceLocation locationOpt, Predicate<Symbol> filter, CancellationToken cancellationToken)
        {
            Debug.Assert(filter == null);
            _ = this.GetAttributes();
            _ = this.ExplicitDefaultConstantValue;
            DoMiscValidation();
            state.SpinWaitComplete(CompletionPart.ComplexParameterSymbolAll, cancellationToken);
        }

#nullable enable

        private void DoMiscValidation()
        {
            if (state.NotePartComplete(CompletionPart.StartMiscValidation))
            {
                var diagnostics = BindingDiagnosticBag.GetInstance();

                if (IsParams && ParameterSyntax?.Modifiers.Any(SyntaxKind.ParamsKeyword) == true)
                {
                    validateParamsType(diagnostics);
                }

                if (DeclaredScope == ScopedKind.ScopedValue && !Type.IsErrorOrRefLikeOrAllowsRefLikeType())
                {
                    Debug.Assert(ParameterSyntax is not null);
                    diagnostics.Add(ErrorCode.ERR_ScopedRefAndRefStructOnly, ParameterSyntax);
                }

                AddDeclarationDiagnostics(diagnostics);
                diagnostics.Free();

                bool completedOnThisThread = state.NotePartComplete(CompletionPart.EndMiscValidation);
                Debug.Assert(completedOnThisThread);
            }

            state.SpinWaitComplete(CompletionPart.EndMiscValidation, default(CancellationToken));

            void validateParamsType(BindingDiagnosticBag diagnostics)
            {
                var collectionTypeKind = ConversionsBase.GetCollectionExpressionTypeKind(DeclaringCompilation, Type, out TypeWithAnnotations elementTypeWithAnnotations);

                var elementType = elementTypeWithAnnotations.Type;
                switch (collectionTypeKind)
                {
                    case CollectionExpressionTypeKind.None:
                        reportInvalidParams(diagnostics);
                        return;

                    case CollectionExpressionTypeKind.ImplementsIEnumerable:
                        {
                            var syntax = ParameterSyntax;
                            var binder = GetDefaultParameterValueBinder(syntax).WithContainingMemberOrLambda(ContainingSymbol); // this binder is good for our purpose

                            binder.TryGetCollectionIterationType(syntax, Type, out elementTypeWithAnnotations);
                            elementType = elementTypeWithAnnotations.Type;
                            if (elementType is null)
                            {
                                reportInvalidParams(diagnostics);
                                return;
                            }

                            if (!binder.HasCollectionExpressionApplicableConstructor(
                                    hasWithElement: false, syntax, Type, out MethodSymbol? constructor, isExpanded: out _, diagnostics, isParamsModifierValidation: true))
                            {
                                return;
                            }

                            if (constructor is not null)
                            {
                                checkIsAtLeastAsVisible(syntax, binder, constructor, diagnostics);
                            }

                            if (!binder.HasCollectionExpressionApplicableAddMethod(syntax, Type, out ImmutableArray<MethodSymbol> addMethods, diagnostics))
                            {
                                return;
                            }

                            Debug.Assert(!addMethods.IsDefaultOrEmpty);

                            if (addMethods[0].IsExtensionMethod || addMethods[0].IsExtensionBlockMember()) // No need to check other methods, extensions are never mixed with instance methods
                            {
                                diagnostics.Add(ErrorCode.ERR_ParamsCollectionExtensionAddMethod, syntax, Type);
                                return;
                            }

                            MethodSymbol? reportAsLessVisible = null;

                            foreach (var addMethod in addMethods)
                            {
                                if (isAtLeastAsVisible(syntax, binder, addMethod, diagnostics))
                                {
                                    reportAsLessVisible = null;
                                    break;
                                }
                                else
                                {
                                    reportAsLessVisible ??= addMethod;
                                }
                            }

                            if (reportAsLessVisible is not null)
                            {
                                diagnostics.Add(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, syntax, reportAsLessVisible, ContainingSymbol);
                            }
                        }
                        break;

                    case CollectionExpressionTypeKind.CollectionBuilder:
                        {
                            var syntax = ParameterSyntax;
                            var binder = GetDefaultParameterValueBinder(syntax).WithContainingMemberOrLambda(ContainingSymbol); // this binder is good for our purpose

                            binder.TryGetCollectionIterationType(syntax, Type, out elementTypeWithAnnotations);
                            elementType = elementTypeWithAnnotations.Type;
                            if (elementType is null)
                            {
                                reportInvalidParams(diagnostics);
                                return;
                            }

                            var collectionBuilderMethods = binder.GetCollectionBuilderMethods(
                                syntax, (NamedTypeSymbol)Type, diagnostics, forParams: true);
                            Debug.Assert(collectionBuilderMethods.Length <= 1);

                            if (collectionBuilderMethods is not [var collectionBuilderMethod])
                            {
                                Debug.Assert(diagnostics.HasAnyErrors(), $"{nameof(binder.GetCollectionBuilderMethods)} should have reported an error in this case");
                                return;
                            }

                            binder.CheckCollectionBuilderMethod(syntax, collectionBuilderMethod, diagnostics);

                            if (ContainingSymbol.ContainingSymbol is NamedTypeSymbol) // No need to check for lambdas or local function
                            {
                                checkIsAtLeastAsVisible(syntax, binder, collectionBuilderMethod, diagnostics);
                            }
                        }
                        break;
                }

                Debug.Assert(elementType is { });

                if (collectionTypeKind != CollectionExpressionTypeKind.Array)
                {
                    MessageID.IDS_FeatureParamsCollections.CheckFeatureAvailability(diagnostics, ParameterSyntax);
                }
            }

            bool isAtLeastAsVisible(ParameterSyntax syntax, Binder binder, MethodSymbol method, BindingDiagnosticBag diagnostics)
            {
                var useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);

                bool result = method.IsAsRestrictive(ContainingSymbol, ref useSiteInfo) &&
                              method.ContainingType.IsAtLeastAsVisibleAs(ContainingSymbol, ref useSiteInfo);

                diagnostics.Add(syntax.Location, useSiteInfo);
                return result;
            }

            void checkIsAtLeastAsVisible(ParameterSyntax syntax, Binder binder, MethodSymbol method, BindingDiagnosticBag diagnostics)
            {
                if (!isAtLeastAsVisible(syntax, binder, method, diagnostics))
                {
                    diagnostics.Add(ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember, syntax, method, ContainingSymbol);
                }
            }

            void reportInvalidParams(BindingDiagnosticBag diagnostics)
            {
                diagnostics.Add(ErrorCode.ERR_ParamsMustBeCollection, ParameterSyntax.Modifiers.First(static m => m.IsKind(SyntaxKind.ParamsKeyword)).GetLocation());
            }
        }

#nullable disable
    }

    internal sealed class SourceComplexParameterSymbol : SourceComplexParameterSymbolBase
    {
        private readonly TypeWithAnnotations _parameterType;

        internal SourceComplexParameterSymbol(
            Symbol owner,
            int ordinal,
            TypeWithAnnotations parameterType,
            RefKind refKind,
            string name,
            Location location,
            SyntaxReference syntaxRef,
            bool hasParamsModifier,
            bool isParams,
            bool isExtensionMethodThis,
            ScopedKind scope)
            : base(owner, ordinal, refKind, name, location, syntaxRef, hasParamsModifier: hasParamsModifier, isParams: isParams, isExtensionMethodThis, scope)
        {
            _parameterType = parameterType;
        }

        public override TypeWithAnnotations TypeWithAnnotations => _parameterType;
        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;
    }

    internal sealed class SourceComplexParameterSymbolWithCustomModifiersPrecedingRef : SourceComplexParameterSymbolBase
    {
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly TypeWithAnnotations _parameterType;

        internal SourceComplexParameterSymbolWithCustomModifiersPrecedingRef(
            Symbol owner,
            int ordinal,
            TypeWithAnnotations parameterType,
            RefKind refKind,
            ImmutableArray<CustomModifier> refCustomModifiers,
            string name,
            Location location,
            SyntaxReference syntaxRef,
            bool hasParamsModifier,
            bool isParams,
            bool isExtensionMethodThis,
            ScopedKind scope)
            : base(owner, ordinal, refKind, name, location, syntaxRef, hasParamsModifier: hasParamsModifier, isParams: isParams, isExtensionMethodThis, scope)
        {
            Debug.Assert(!refCustomModifiers.IsEmpty);

            _parameterType = parameterType;
            _refCustomModifiers = refCustomModifiers;

            Debug.Assert(refKind != RefKind.None || _refCustomModifiers.IsEmpty);
        }

        public override TypeWithAnnotations TypeWithAnnotations => _parameterType;
        public override ImmutableArray<CustomModifier> RefCustomModifiers => _refCustomModifiers;
    }
}
