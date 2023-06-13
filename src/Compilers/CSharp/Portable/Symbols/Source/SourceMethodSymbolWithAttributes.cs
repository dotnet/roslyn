// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A source method that can have attributes, including a member method, accessor, or local function.
    /// </summary>
    internal abstract class SourceMethodSymbolWithAttributes : SourceMethodSymbol, IAttributeTargetSymbol
    {
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
        private CustomAttributesBag<CSharpAttributeData> _lazyReturnTypeCustomAttributesBag;

        // some symbols may not have a syntax (e.g. lambdas, synthesized event accessors)
        protected readonly SyntaxReference syntaxReferenceOpt;
        protected SourceMethodSymbolWithAttributes(SyntaxReference syntaxReferenceOpt)
        {
            this.syntaxReferenceOpt = syntaxReferenceOpt;
        }

#nullable enable
        /// <summary>
        /// Gets the syntax node used for the in-method binder.
        /// </summary>
        protected CSharpSyntaxNode? GetInMethodSyntaxNode()
        {
            switch (SyntaxNode)
            {
                case ConstructorDeclarationSyntax constructor:
                    return constructor.Initializer ?? (CSharpSyntaxNode?)constructor.Body ?? constructor.ExpressionBody;
                case BaseMethodDeclarationSyntax method:
                    return (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody;
                case AccessorDeclarationSyntax accessor:
                    return (CSharpSyntaxNode?)accessor.Body ?? accessor.ExpressionBody;
                case ArrowExpressionClauseSyntax arrowExpression:
                    Debug.Assert(arrowExpression.Parent!.Kind() == SyntaxKind.PropertyDeclaration ||
                                 arrowExpression.Parent.Kind() == SyntaxKind.IndexerDeclaration);
                    return arrowExpression;
                case LocalFunctionStatementSyntax localFunction:
                    return (CSharpSyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody;
                case CompilationUnitSyntax _ when this is SynthesizedSimpleProgramEntryPointSymbol entryPoint:
                    return (CSharpSyntaxNode)entryPoint.ReturnTypeSyntax;
                case RecordDeclarationSyntax recordDecl:
                    Debug.Assert(recordDecl.IsKind(SyntaxKind.RecordDeclaration));
                    return recordDecl;
                case ClassDeclarationSyntax classDecl:
                    return classDecl;
                default:
                    return null;
            }
        }

        internal virtual Binder? OuterBinder => null;

        internal virtual Binder? WithTypeParametersBinder => null;

#nullable disable

        internal SyntaxReference SyntaxRef
        {
            get
            {
                return this.syntaxReferenceOpt;
            }
        }

        internal virtual CSharpSyntaxNode SyntaxNode
        {
            get
            {
                return (this.syntaxReferenceOpt == null) ? null : (CSharpSyntaxNode)this.syntaxReferenceOpt.GetSyntax();
            }
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return this.syntaxReferenceOpt == null ? null : this.syntaxReferenceOpt.SyntaxTree;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return (this.syntaxReferenceOpt == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(this.syntaxReferenceOpt);
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations =>
            DecodeReturnTypeAnnotationAttributes(GetDecodedReturnTypeWellKnownAttributeData());

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull
            => GetDecodedReturnTypeWellKnownAttributeData()?.NotNullIfParameterNotNull ?? ImmutableHashSet<string>.Empty;

        /// <summary>
        /// Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source method symbols.
        /// </summary>
        /// <remarks>
        /// Used for example for event accessors. The "remove" method delegates attribute binding to the "add" method.
        /// The bound attribute data are then applied to both accessors.
        /// </remarks>
        protected virtual SourceMemberMethodSymbol BoundAttributesSource
        {
            get
            {
                return null;
            }
        }

        protected virtual IAttributeTargetSymbol AttributeOwner
        {
            get { return this; }
        }

        protected virtual AttributeLocation AttributeLocationForLoadAndValidateAttributes
        {
            get { return AttributeLocation.None; }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this.AttributeOwner; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Method; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                switch (MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.Destructor:
                    case MethodKind.StaticConstructor:
                        return AttributeLocation.Method;

                    case MethodKind.PropertySet:
                    case MethodKind.EventRemove:
                    case MethodKind.EventAdd:
                        return AttributeLocation.Method | AttributeLocation.Return | AttributeLocation.Parameter;

                    default:
                        return AttributeLocation.Method | AttributeLocation.Return;
                }
            }
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares attributes for this method symbol.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
        }

        /// <summary>
        /// Gets the syntax list of custom attributes that declares attributes for return type of this method.
        /// </summary>
        internal virtual OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
        {
            // Usually the same list as other attributes applied on the method, but e.g.
            // constructors and destructors do not allow return-type attributes, so this is empty.
            return GetAttributeDeclarations();
        }

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal MethodEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (MethodEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        protected MethodWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

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
            var attributesBag = _lazyReturnTypeCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetReturnTypeAttributesBag();
            }

            return (ReturnTypeWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            var bag = _lazyCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            return GetAttributesBag(ref _lazyCustomAttributesBag, forReturnType: false);
        }

        /// <summary>
        /// Returns a bag of custom attributes applied on the method return value and data decoded from well-known attributes. Returns null if there are no attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetReturnTypeAttributesBag()
        {
            var bag = _lazyReturnTypeCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            return GetAttributesBag(ref _lazyReturnTypeCustomAttributesBag, forReturnType: true);
        }

        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag(ref CustomAttributesBag<CSharpAttributeData> lazyCustomAttributesBag, bool forReturnType)
        {
            var copyFrom = this.BoundAttributesSource;

            // prevent infinite recursion:
            Debug.Assert(!ReferenceEquals(copyFrom, this));

            bool bagCreatedOnThisThread;
            if ((object)copyFrom != null)
            {
                var attributesBag = forReturnType ? copyFrom.GetReturnTypeAttributesBag() : copyFrom.GetAttributesBag();
                bagCreatedOnThisThread = Interlocked.CompareExchange(ref lazyCustomAttributesBag, attributesBag, null) == null;
            }
            else
            {
                var (declarations, symbolPart) = forReturnType
                    ? (GetReturnTypeAttributeDeclarations(), AttributeLocation.Return)
                    : (GetAttributeDeclarations(), AttributeLocationForLoadAndValidateAttributes);
                bagCreatedOnThisThread = LoadAndValidateAttributes(
                    declarations,
                    ref lazyCustomAttributesBag,
                    symbolPart,
                    binderOpt: OuterBinder);
            }

            if (bagCreatedOnThisThread)
            {
                NoteAttributesComplete(forReturnType);
            }

            return lazyCustomAttributesBag;
        }

        /// <summary>
        /// Called when this thread loaded the method's attributes. For method symbols with completion state.
        /// </summary>
        protected abstract void NoteAttributesComplete(bool forReturnType);

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

#nullable enable
        internal override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None || arguments.SymbolPart == AttributeLocation.Return);

            bool hasAnyDiagnostics;

            if (arguments.SymbolPart == AttributeLocation.None)
            {
                if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute))
                {
                    var (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                    if (!attributeData.HasErrors)
                    {
                        string? name = attributeData.GetConstructorArgument<string>(0, SpecialType.System_String);
                        arguments.GetOrCreateData<MethodEarlyWellKnownAttributeData>().AddConditionalSymbol(name);
                        if (!hasAnyDiagnostics)
                        {
                            return (attributeData, boundAttribute);
                        }
                    }

                    return (null, null);
                }
                else if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out CSharpAttributeData? attributeData, out BoundAttribute? boundAttribute, out ObsoleteAttributeData? obsoleteData))
                {
                    if (obsoleteData != null)
                    {
                        arguments.GetOrCreateData<MethodEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                    }

                    return (attributeData, boundAttribute);
                }
                else if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.UnmanagedCallersOnlyAttribute))
                {
                    arguments.GetOrCreateData<MethodEarlyWellKnownAttributeData>().UnmanagedCallersOnlyAttributePresent = true;
                    // We can't actually decode this attribute yet: CallConvs is an array, and it cannot be bound yet or we could hit a cycle
                    // in error cases. We only detect whether or not the attribute is present for use in ensuring that we create as few lazily-computed
                    // diagnostics that might later get thrown away as possible when binding method calls.
                    return (null, null);
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        /// <summary>
        /// Binds attributes applied to this method.
        /// </summary>
        public ImmutableArray<(CSharpAttributeData, BoundAttribute)> BindMethodAttributes()
        {
            return BindAttributes(GetAttributeDeclarations(), OuterBinder);
        }
#nullable disable

        public override bool AreLocalsZeroed
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data?.HasSkipLocalsInitAttribute != true && AreContainingSymbolLocalsZeroed;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                if (ContainingSymbol is SourceMemberContainerTypeSymbol { AnyMemberHasAttributes: false })
                {
                    return null;
                }

                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (MethodEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data != null ? data.ObsoleteAttributeData : null;
                }

                if (syntaxReferenceOpt is null)
                {
                    // no references -> no attributes
                    return null;
                }

                return ObsoleteAttributeData.Uninitialized;
            }
        }

#nullable enable
        internal sealed override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
        {
            if (syntaxReferenceOpt is null)
            {
                // no references -> no attributes
                return null;
            }

            if (forceComplete)
            {
                _ = this.GetAttributes();
            }

            var lazyCustomAttributesBag = _lazyCustomAttributesBag;
            if (lazyCustomAttributesBag is null || !lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                Debug.Assert(!forceComplete);
                return UnmanagedCallersOnlyAttributeData.Uninitialized;
            }

            if (lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                var lateData = (MethodWellKnownAttributeData?)lazyCustomAttributesBag.DecodedWellKnownAttributeData;

#if DEBUG
                verifyDataConsistent((MethodEarlyWellKnownAttributeData?)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, lateData);
#endif

                return lateData?.UnmanagedCallersOnlyAttributeData;
            }

            var earlyData = (MethodEarlyWellKnownAttributeData?)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
            Debug.Assert(!forceComplete);
            return earlyData?.UnmanagedCallersOnlyAttributePresent == true
                ? UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound
                : null;

#if DEBUG // Can remove ifdefs and replace with Conditional after https://github.com/dotnet/roslyn/issues/47463 is fixed
            static void verifyDataConsistent(MethodEarlyWellKnownAttributeData? earlyData, MethodWellKnownAttributeData? lateData)
            {
                if (lateData is { UnmanagedCallersOnlyAttributeData: not null })
                {
                    // We can't verify the symmetric case here. Error conditions (such as if a bad expression was provided to the array initializer)
                    // can cause the attribute to be skipped during regular attribute binding. Early binding doesn't know that though, so
                    // it still gets marked as present.
                    Debug.Assert(earlyData is { UnmanagedCallersOnlyAttributePresent: true });
                }
                else if (earlyData is null or { UnmanagedCallersOnlyAttributePresent: false })
                {
                    Debug.Assert(lateData is null or { UnmanagedCallersOnlyAttributeData: null });
                }
            }
#endif
        }
#nullable disable

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            MethodEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            return data != null ? data.ConditionalSymbols : ImmutableArray<string>.Empty;
        }

        protected override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
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
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;
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
                ValidateConditionalAttribute(attribute, arguments.AttributeSyntaxOpt, diagnostics);
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
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.IsReadOnlyAttribute |
                ReservedAttributes.IsUnmanagedAttribute |
                ReservedAttributes.IsByRefLikeAttribute |
                ReservedAttributes.NullableContextAttribute |
                ReservedAttributes.CaseSensitiveExtensionAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(this, AttributeDescription.SecuritySafeCriticalAttribute))
            {
                if (IsAsync)
                {
                    diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, arguments.AttributeSyntaxOpt.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SkipLocalsInitAttribute))
            {
                CSharpAttributeData.DecodeSkipLocalsInitAttribute<MethodWellKnownAttributeData>(DeclaringCompilation, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DoesNotReturnAttribute))
            {
                arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasDoesNotReturnAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MemberNotNullAttribute))
            {
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullAttribute<MethodWellKnownAttributeData>(ContainingType, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MemberNotNullWhenAttribute))
            {
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullWhenAttribute<MethodWellKnownAttributeData>(ContainingType, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ModuleInitializerAttribute))
            {
                MessageID.IDS_FeatureModuleInitializers.CheckFeatureAvailability(diagnostics, arguments.AttributeSyntaxOpt);
                DecodeModuleInitializerAttribute(arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.UnmanagedCallersOnlyAttribute))
            {
                DecodeUnmanagedCallersOnlyAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.UnscopedRefAttribute))
            {
                if (this.IsValidUnscopedRefAttributeTarget())
                {
                    arguments.GetOrCreateData<MethodWellKnownAttributeData>().HasUnscopedRefAttribute = true;
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget, arguments.AttributeSyntaxOpt.Location);
                }
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.InterceptsLocationAttribute))
            {
                DecodeInterceptsLocationAttribute(arguments);
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

        internal override ImmutableArray<string> NotNullMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullMembers ?? ImmutableArray<string>.Empty;

        internal override ImmutableArray<string> NotNullWhenTrueMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullWhenTrueMembers ?? ImmutableArray<string>.Empty;

        internal override ImmutableArray<string> NotNullWhenFalseMembers =>
            GetDecodedWellKnownAttributeData()?.NotNullWhenFalseMembers ?? ImmutableArray<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get
            {
                return DecodeFlowAnalysisAttributes(GetDecodedWellKnownAttributeData());
            }
        }

        private static FlowAnalysisAnnotations DecodeFlowAnalysisAttributes(MethodWellKnownAttributeData attributeData)
            => attributeData?.HasDoesNotReturnAttribute == true ? FlowAnalysisAnnotations.DoesNotReturn : FlowAnalysisAnnotations.None;

        internal sealed override bool HasUnscopedRefAttribute => GetDecodedWellKnownAttributeData()?.HasUnscopedRefAttribute == true;

        private bool VerifyObsoleteAttributeAppliedToMethod(
            ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments,
            AttributeDescription description)
        {
            if (arguments.Attribute.IsTargetAttribute(this, description))
            {
                if (this.IsAccessor())
                {
                    var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

                    if (this is SourceEventAccessorSymbol)
                    {
                        // CS1667: Attribute '{0}' is not valid on event accessors. It is only valid on '{1}' declarations.
                        AttributeUsageInfo attributeUsage = arguments.Attribute.AttributeClass.GetAttributeUsageInfo();
                        diagnostics.Add(ErrorCode.ERR_AttributeNotOnEventAccessor, arguments.AttributeSyntaxOpt.Name.Location, description.FullName, attributeUsage.GetValidTargetsErrorArgument());
                    }
                    else
                    {
                        MessageID.IDS_FeatureObsoleteOnPropertyAccessor.CheckFeatureAvailability(diagnostics, arguments.AttributeSyntaxOpt);
                    }
                }

                return true;
            }

            return false;
        }

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsConditional);

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
                // CS0577: The Conditional attribute is not valid on '{0}' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
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
            else if (this is { MethodKind: MethodKind.LocalFunction, IsStatic: false })
            {
                diagnostics.Add(ErrorCode.ERR_ConditionalOnLocalFunction, node.Location, this);
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
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                // MarshalAs applied to the return value:
                MarshalAsAttributeDecoder<ReturnTypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.ReturnValue, MessageProvider.Instance);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.DynamicAttribute |
                ReservedAttributes.IsUnmanagedAttribute |
                ReservedAttributes.IsReadOnlyAttribute |
                ReservedAttributes.IsByRefLikeAttribute |
                ReservedAttributes.TupleElementNamesAttribute |
                ReservedAttributes.NullableAttribute |
                ReservedAttributes.NativeIntegerAttribute))
            {
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

#nullable enable
        private void DecodeDllImportAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            RoslynDebug.Assert(arguments.AttributeSyntaxOpt?.ArgumentList is object);

            var attribute = arguments.Attribute;
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;
            Debug.Assert(!attribute.HasErrors);
            bool hasErrors = false;

            var implementationPart = this.PartialImplementationPart ?? this;
            if (!implementationPart.IsExtern || !implementationPart.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_DllImportOnInvalidMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            var isAnyNestedMethodGeneric = false;
            for (MethodSymbol? current = this; current is object; current = current.ContainingSymbol as MethodSymbol)
            {
                if (current.IsGenericMethod)
                {
                    isAnyNestedMethodGeneric = true;
                    break;
                }
            }

            if (isAnyNestedMethodGeneric || ContainingType?.IsGenericType == true)
            {
                diagnostics.Add(ErrorCode.ERR_DllImportOnGenericMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            string? moduleName = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);
            if (!MetadataHelpers.IsValidMetadataIdentifier(moduleName))
            {
                // Dev10 reports CS0647: "Error emitting attribute ..."
                CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, arguments.AttributeSyntaxOpt);
                diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
                hasErrors = true;
                moduleName = null;
            }

            // Default value of charset is inherited from the module (only if specified).
            // This might be different from ContainingType.DefaultMarshallingCharSet. If the charset is not specified on module
            // ContainingType.DefaultMarshallingCharSet would be Ansi (the class is emitted with "Ansi" charset metadata flag) 
            // while the charset in P/Invoke metadata should be "None".
            CharSet charSet = this.GetEffectiveDefaultMarshallingCharSet() ?? Cci.Constants.CharSet_None;

            string? importName = null;
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
                            diagnostics.Add(ErrorCode.ERR_InvalidNamedArgument, arguments.AttributeSyntaxOpt.ArgumentList.Arguments[position].Location, namedArg.Key);
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
                    importName ?? Name,
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

        private void DecodeModuleInitializerAttribute(DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.AttributeSyntaxOpt is object);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            if (MethodKind != MethodKind.Ordinary)
            {
                diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, arguments.AttributeSyntaxOpt.Location);
                return;
            }

            Debug.Assert(ContainingType is object);
            var hasError = false;

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
            if (!AccessCheck.IsSymbolAccessible(this, ContainingAssembly, ref useSiteInfo))
            {
                diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            diagnostics.Add(arguments.AttributeSyntaxOpt, useSiteInfo);

            if (!IsStatic || ParameterCount > 0 || !ReturnsVoid || IsAbstract || IsVirtual)
            {
                diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            if (IsGenericMethod || ContainingType.IsGenericType)
            {
                diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            // If this is an UnmanagedCallersOnly method, it means that this cannot be called by managed code, including the attempt by the CLR
            // to run the module initializer.
            if (_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData is MethodEarlyWellKnownAttributeData { UnmanagedCallersOnlyAttributePresent: true })
            {
                diagnostics.Add(ErrorCode.ERR_ModuleInitializerCannotBeUnmanagedCallersOnly, arguments.AttributeSyntaxOpt.Location);
                hasError = true;
            }

            if (!hasError && !CallsAreOmitted(arguments.AttributeSyntaxOpt.SyntaxTree))
            {
                DeclaringCompilation.AddModuleInitializerMethod(this);
            }
        }

        private void DecodeInterceptsLocationAttribute(DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.AttributeSyntaxOpt is object);
            Debug.Assert(!arguments.Attribute.HasErrors);
            var attributeData = arguments.Attribute;
            var attributeArguments = attributeData.CommonConstructorArguments;
            if (attributeArguments is not [
                { Type.SpecialType: SpecialType.System_String },
                { Kind: not TypedConstantKind.Array, Value: int lineNumberOneBased },
                { Kind: not TypedConstantKind.Array, Value: int characterNumberOneBased }])
            {
                // Since the attribute does not have errors (asserted above), it should be guaranteed that we have the above arguments.
                throw ExceptionUtilities.Unreachable();
            }

            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;
            var attributeSyntax = arguments.AttributeSyntaxOpt;
            var attributeLocation = attributeSyntax.Location;
            const int filePathParameterIndex = 0;
            const int lineNumberParameterIndex = 1;
            const int characterNumberParameterIndex = 2;

            if (!attributeSyntax.SyntaxTree.Options.Features.ContainsKey("InterceptorsPreview"))
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorsFeatureNotEnabled, attributeSyntax);
                return;
            }

            var attributeFilePath = (string?)attributeArguments[0].Value;
            if (attributeFilePath is null)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorFilePathCannotBeNull, attributeData.GetAttributeArgumentSyntaxLocation(filePathParameterIndex, attributeSyntax));
                return;
            }

            if (ContainingType.IsGenericType)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorContainingTypeCannotBeGeneric, attributeLocation, this);
                return;
            }

            if (MethodKind != MethodKind.Ordinary)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorMethodMustBeOrdinary, attributeLocation);
                return;
            }

            Debug.Assert(_lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed);
            var unmanagedCallersOnly = this.GetUnmanagedCallersOnlyAttributeData(forceComplete: false);
            if (unmanagedCallersOnly != null)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorCannotUseUnmanagedCallersOnly, attributeLocation);
                return;
            }

            var syntaxTrees = DeclaringCompilation.SyntaxTrees;
            var matchingTrees = DeclaringCompilation.GetSyntaxTreesByMappedPath(attributeFilePath);
            if (matchingTrees.Count == 0)
            {
                var referenceResolver = DeclaringCompilation.Options.SourceReferenceResolver;
                // if we expect '/_/Program.cs':

                // we might get: 'C:\Project\Program.cs' <-- path not mapped
                var unmappedMatch = syntaxTrees.FirstOrDefault(static (tree, filePath) => tree.FilePath == filePath, attributeFilePath);
                if (unmappedMatch != null)
                {
                    diagnostics.Add(
                        ErrorCode.ERR_InterceptorPathNotInCompilationWithUnmappedCandidate,
                        attributeData.GetAttributeArgumentSyntaxLocation(filePathParameterIndex, attributeSyntax),
                        attributeFilePath,
                        mapPath(referenceResolver, unmappedMatch));
                    return;
                }

                // we might get: '\_\Program.cs' <-- slashes not normalized
                // we might get: '\_/Program.cs' <-- slashes don't match
                // we might get: 'Program.cs' <-- suffix match
                // Force normalization of all '\' to '/', but when we recommend a path in the diagnostic message, ensure it will match what we expect if the user decides to use it.
                var suffixMatch = syntaxTrees.FirstOrDefault(static (tree, pair)
                    => mapPath(pair.referenceResolver, tree)
                        .Replace('\\', '/')
                        .EndsWith(pair.attributeFilePath),
                    (referenceResolver, attributeFilePath: attributeFilePath.Replace('\\', '/')));
                if (suffixMatch != null)
                {
                    diagnostics.Add(
                        ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate,
                        attributeData.GetAttributeArgumentSyntaxLocation(filePathParameterIndex, attributeSyntax),
                        attributeFilePath,
                        mapPath(referenceResolver, suffixMatch));
                    return;
                }

                diagnostics.Add(ErrorCode.ERR_InterceptorPathNotInCompilation, attributeData.GetAttributeArgumentSyntaxLocation(filePathParameterIndex, attributeSyntax), attributeFilePath);

                return;
            }
            else if (matchingTrees.Count > 1)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorNonUniquePath, attributeData.GetAttributeArgumentSyntaxLocation(filePathParameterIndex, attributeSyntax), attributeFilePath);
                return;
            }

            SyntaxTree? matchingTree = matchingTrees[0];
            // Internally, line and character numbers are 0-indexed, but when they appear in code or diagnostic messages, they are 1-indexed.
            int lineNumberZeroBased = lineNumberOneBased - 1;
            int characterNumberZeroBased = characterNumberOneBased - 1;

            if (lineNumberZeroBased < 0 || characterNumberZeroBased < 0)
            {
                var location = attributeData.GetAttributeArgumentSyntaxLocation(lineNumberZeroBased < 0 ? lineNumberParameterIndex : characterNumberParameterIndex, attributeSyntax);
                diagnostics.Add(ErrorCode.ERR_InterceptorLineCharacterMustBePositive, location);
                return;
            }

            var referencedLines = matchingTree.GetText().Lines;
            var referencedLineCount = referencedLines.Count;

            if (lineNumberZeroBased >= referencedLineCount)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorLineOutOfRange, attributeData.GetAttributeArgumentSyntaxLocation(lineNumberParameterIndex, attributeSyntax), referencedLineCount, lineNumberOneBased);
                return;
            }

            var line = referencedLines[lineNumberZeroBased];
            var lineLength = line.End - line.Start;
            if (characterNumberZeroBased >= lineLength)
            {
                diagnostics.Add(ErrorCode.ERR_InterceptorCharacterOutOfRange, attributeData.GetAttributeArgumentSyntaxLocation(characterNumberParameterIndex, attributeSyntax), lineLength, characterNumberOneBased);
                return;
            }

            var referencedPosition = line.Start + characterNumberZeroBased;
            var root = matchingTree.GetRoot();
            var referencedToken = root.FindToken(referencedPosition);
            switch (referencedToken)
            {
                case { Parent: SimpleNameSyntax { Parent: MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } memberAccess } rhs } when memberAccess.Name == rhs:
                case { Parent: SimpleNameSyntax { Parent: InvocationExpressionSyntax invocation } simpleName } when invocation.Expression == simpleName:
                    // happy case
                    break;
                case { Parent: SimpleNameSyntax { Parent: not MemberAccessExpressionSyntax } }:
                case { Parent: SimpleNameSyntax { Parent: MemberAccessExpressionSyntax memberAccess } rhs } when memberAccess.Name == rhs:
                    // NB: there are all sorts of places "simple names" can appear in syntax. With these checks we are trying to
                    // minimize confusion about why the name being used is not *interceptable*, but it's done on a best-effort basis.
                    diagnostics.Add(ErrorCode.ERR_InterceptorNameNotInvoked, attributeLocation, referencedToken.Text);
                    return;
                default:
                    diagnostics.Add(ErrorCode.ERR_InterceptorPositionBadToken, attributeLocation, referencedToken.Text);
                    return;
            }

            // Did they actually refer to the start of the token, not the middle, or in trivia?
            if (referencedPosition != referencedToken.Span.Start)
            {
                var linePositionZeroBased = referencedToken.GetLocation().GetLineSpan().StartLinePosition;
                diagnostics.Add(ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition, attributeLocation, referencedToken.Text, linePositionZeroBased.Line + 1, linePositionZeroBased.Character + 1);
                return;
            }

            DeclaringCompilation.AddInterception(matchingTree.FilePath, lineNumberZeroBased, characterNumberZeroBased, attributeLocation, this);

            static string mapPath(SourceReferenceResolver? referenceResolver, SyntaxTree tree)
            {
                return referenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
            }
        }

        private void DecodeUnmanagedCallersOnlyAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.AttributeSyntaxOpt != null);
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            arguments.GetOrCreateData<MethodWellKnownAttributeData>().UnmanagedCallersOnlyAttributeData =
                DecodeUnmanagedCallersOnlyAttributeData(this, arguments.Attribute, arguments.AttributeSyntaxOpt.Location, diagnostics);

            bool reportedError = CheckAndReportValidUnmanagedCallersOnlyTarget(arguments.AttributeSyntaxOpt.Name.Location, diagnostics);

            var returnTypeSyntax = this.ExtractReturnTypeSyntax();

            // If there is no return type (such as a property definition), Dummy.GetRoot() is returned.
            if (ReferenceEquals(returnTypeSyntax, CSharpSyntaxTree.Dummy.GetRoot()))
            {
                // If there's no syntax for the return type, then we already errored because this isn't a valid
                // unmanagedcallersonly target (it's a property getter/setter or some other non-regular-method).
                // Any more errors would just be noise.
                Debug.Assert(reportedError);
                return;
            }

            checkAndReportManagedTypes(ReturnType, this.RefKind, returnTypeSyntax, isParam: false, diagnostics);
            foreach (var param in Parameters)
            {
                checkAndReportManagedTypes(param.Type, param.RefKind, param.GetNonNullSyntaxNode(), isParam: true, diagnostics);
            }

            static void checkAndReportManagedTypes(TypeSymbol type, RefKind refKind, SyntaxNode syntax, bool isParam, BindingDiagnosticBag diagnostics)
            {
                if (refKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly, syntax.Location);
                }

                // use-site diagnostics will be reported at actual parameter declaration site, we're only interested
                // in reporting managed types being used
                switch (type.ManagedKindNoUseSiteDiagnostics)
                {
                    case ManagedKind.Unmanaged:
                    case ManagedKind.UnmanagedWithGenerics:
                        // Note that this will let through some things that are technically unmanaged, but not
                        // actually blittable. However, we don't have a formal concept of blittable in C#
                        // itself, so checking for purely unmanaged types is the best we can do here.
                        return;

                    case ManagedKind.Managed:
                        // Cannot use '{0}' as a {1} type on a method attributed with 'UnmanagedCallersOnly.
                        diagnostics.Add(ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly, syntax.Location, type, (isParam ? MessageID.IDS_Parameter : MessageID.IDS_Return).Localize());
                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.ManagedKindNoUseSiteDiagnostics);
                }
            }

            static UnmanagedCallersOnlyAttributeData DecodeUnmanagedCallersOnlyAttributeData(SourceMethodSymbolWithAttributes @this, CSharpAttributeData attribute, Location location, BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(attribute.AttributeClass is not null);
                ImmutableHashSet<CodeAnalysis.Symbols.INamedTypeSymbolInternal>? callingConventionTypes = null;
                if (attribute.CommonNamedArguments is { IsDefaultOrEmpty: false } namedArgs)
                {
                    var systemType = @this.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type);

                    foreach (var (key, value) in attribute.CommonNamedArguments)
                    {
                        // Technically, CIL can define a field and a property with the same name. However, such a
                        // member results in an Ambiguous Member error, and we never get to this piece of code at all.
                        // See UnmanagedCallersOnly_PropertyAndFieldNamedCallConvs for an example
                        bool isField = attribute.AttributeClass.GetMembers(key).Any(
                            static (m, systemType) => m is FieldSymbol { Type: ArrayTypeSymbol { ElementType: NamedTypeSymbol elementType } } && elementType.Equals(systemType, TypeCompareKind.ConsiderEverything),
                            systemType);

                        var namedArgumentDecoded = TryDecodeUnmanagedCallersOnlyCallConvsField(key, value, isField, location, diagnostics);

                        if (namedArgumentDecoded.IsCallConvs)
                        {
                            callingConventionTypes = namedArgumentDecoded.CallConvs;
                        }
                    }
                }

                return UnmanagedCallersOnlyAttributeData.Create(callingConventionTypes);
            }
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(symbolPart == AttributeLocation.None || symbolPart == AttributeLocation.Return);

            if (symbolPart != AttributeLocation.Return)
            {
                Debug.Assert(_lazyCustomAttributesBag != null);
                Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);

                if (ContainingSymbol is NamedTypeSymbol { IsComImport: true, TypeKind: TypeKind.Class or TypeKind.Interface })
                {
                    switch (this.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            if (!this.IsImplicitlyDeclared)
                            {
                                // CS0669: A class with the ComImport attribute cannot have a user-defined constructor
                                diagnostics.Add(ErrorCode.ERR_ComImportWithUserCtor, this.GetFirstLocation());
                            }

                            break;

                        default:
                            if (!this.IsAbstract && !this.IsExtern)
                            {
                                // CS0423: Since '{1}' has the ComImport attribute, '{0}' must be extern or abstract
                                diagnostics.Add(ErrorCode.ERR_ComImportWithImpl, this.GetFirstLocation(), this, ContainingType);
                            }

                            break;
                    }
                }

                if (IsExtern
                    && !IsAbstract
                    && !this.IsPartialMethod()
                    && GetInMethodSyntaxNode() is null
                    && boundAttributes.IsEmpty
                    && !this.ContainingType.IsComImport)
                {
                    var errorCode = (this.MethodKind == MethodKind.Constructor || this.MethodKind == MethodKind.StaticConstructor) ?
                        ErrorCode.WRN_ExternCtorNoImplementation :
                        ErrorCode.WRN_ExternMethodNoImplementation;
                    diagnostics.Add(errorCode, this.GetFirstLocation(), this);
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        protected void AsyncMethodChecks(BindingDiagnosticBag diagnostics)
        {
            AsyncMethodChecks(verifyReturnType: true, this.GetFirstLocation(), diagnostics);
        }

        protected void AsyncMethodChecks(bool verifyReturnType, Location errorLocation, BindingDiagnosticBag diagnostics)
        {
            if (IsAsync)
            {
                bool hasErrors = false;

                if (verifyReturnType)
                {
                    if (this.RefKind != RefKind.None)
                    {
                        var returnTypeSyntax = this.SyntaxNode switch
                        {
                            MethodDeclarationSyntax { ReturnType: var methodReturnType } => methodReturnType,
                            LocalFunctionStatementSyntax { ReturnType: var localReturnType } => localReturnType,
                            ParenthesizedLambdaExpressionSyntax { ReturnType: { } lambdaReturnType } => lambdaReturnType,
                            var unexpected => throw ExceptionUtilities.UnexpectedValue(unexpected)
                        };

                        ReportBadRefToken(returnTypeSyntax, diagnostics);
                        hasErrors = true;
                    }
                    else if (isBadAsyncReturn(this))
                    {
                        diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, errorLocation);
                        hasErrors = true;
                    }
                }

                if (this.HasAsyncMethodBuilderAttribute(out _))
                {
                    MessageID.IDS_AsyncMethodBuilderOverride.CheckFeatureAvailability(diagnostics, this.DeclaringCompilation, errorLocation);
                }

                // Avoid checking attributes on containing types to avoid a potential cycle when a lambda
                // is used in an attribute argument - see https://github.com/dotnet/roslyn/issues/54074.
                // (ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct was never reported
                // for lambda expressions and is not a .NET Core scenario so it's not necessary to handle.)
                if (this.MethodKind != MethodKind.LambdaMethod)
                {
                    for (NamedTypeSymbol curr = this.ContainingType; (object)curr != null; curr = curr.ContainingType)
                    {
                        if (curr is SourceNamedTypeSymbol { HasSecurityCriticalAttributes: true })
                        {
                            diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, errorLocation);
                            hasErrors = true;
                            break;
                        }
                    }
                }

                if ((this.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) != 0)
                {
                    diagnostics.Add(ErrorCode.ERR_SynchronizedAsyncMethod, errorLocation);
                    hasErrors = true;
                }

                if (!hasErrors)
                {
                    ReportAsyncParameterErrors(diagnostics, errorLocation);
                }

                var iAsyncEnumerableType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T);
                if (ReturnType.OriginalDefinition.Equals(iAsyncEnumerableType) &&
                    GetInMethodSyntaxNode() is object)
                {
                    var cancellationTokenType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Threading_CancellationToken);
                    var enumeratorCancellationCount = Parameters.Count(p => p.IsSourceParameterWithEnumeratorCancellationAttribute());
                    if (enumeratorCancellationCount == 0 &&
                        ParameterTypesWithAnnotations.Any(static (p, cancellationTokenType) => p.Type.Equals(cancellationTokenType), cancellationTokenType))
                    {
                        // Warn for CancellationToken parameters in async-iterators with no parameter decorated with [EnumeratorCancellation]
                        // There could be more than one parameter that could be decorated with [EnumeratorCancellation] so we warn on the method instead
                        diagnostics.Add(ErrorCode.WRN_UndecoratedCancellationTokenParameter, errorLocation, this);
                    }

                    if (enumeratorCancellationCount > 1)
                    {
                        // The [EnumeratorCancellation] attribute can only be used on one parameter
                        diagnostics.Add(ErrorCode.ERR_MultipleEnumeratorCancellationAttributes, errorLocation);
                    }
                }
            }

            static bool isBadAsyncReturn(MethodSymbol methodSymbol)
            {
                var returnType = methodSymbol.ReturnType;
                var declaringCompilation = methodSymbol.DeclaringCompilation;
                return !returnType.IsErrorType() &&
                    !returnType.IsVoidType() &&
                    !returnType.IsIAsyncEnumerableType(declaringCompilation) &&
                    !returnType.IsIAsyncEnumeratorType(declaringCompilation) &&
                    !methodSymbol.IsAsyncEffectivelyReturningTask(declaringCompilation) &&
                    !methodSymbol.IsAsyncEffectivelyReturningGenericTask(declaringCompilation);
            }
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

        public sealed override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        internal sealed override bool HasRuntimeSpecialName
        {
            get
            {
                return base.HasRuntimeSpecialName || IsVtableGapInterfaceMethod();
            }
        }

        private bool IsVtableGapInterfaceMethod()
        {
            return this.ContainingType.IsInterface &&
                   ModuleExtensions.GetVTableGapSize(this.MetadataName) > 0;
        }

        internal override bool HasSpecialName
        {
            get
            {
                switch (this.MethodKind)
                {
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                    case MethodKind.UserDefinedOperator:
                    case MethodKind.Conversion:
                        return true;
                }

                if (IsVtableGapInterfaceMethod())
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal sealed override bool IsDirectlyExcludedFromCodeCoverage =>
            GetDecodedWellKnownAttributeData()?.HasExcludeFromCodeCoverageAttribute == true;

        internal override bool RequiresSecurityObject
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasDynamicSecurityMethodAttribute;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasDeclarativeSecurity;
            }
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            var attributesBag = this.GetAttributesBag();
            var wellKnownData = (MethodWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
            if (wellKnownData != null)
            {
                SecurityWellKnownAttributeData? securityData = wellKnownData.SecurityInformation;
                if (securityData != null)
                {
                    return securityData.GetSecurityAttributes(attributesBag.Attributes);
                }
            }

            return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
        }

        public override DllImportData? GetDllImportData()
        {
            var data = this.GetDecodedWellKnownAttributeData();
            return data != null ? data.DllImportPlatformInvokeData : null;
        }

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation
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
