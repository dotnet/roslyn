// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                    return recordDecl;
                default:
                    return null;
            }
        }
#nullable restore

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
        internal CommonMethodEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

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
                    : (GetAttributeDeclarations(), AttributeLocation.None);
                bagCreatedOnThisThread = LoadAndValidateAttributes(
                    declarations,
                    ref lazyCustomAttributesBag,
                    symbolPart,
                    binderOpt: (this as LocalFunctionSymbol)?.SignatureBinder);
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
                    var data = (CommonMethodEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
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

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            CommonMethodEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            return data != null ? data.ConditionalSymbols : ImmutableArray<string>.Empty;
        }

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
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
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.IsReadOnlyAttribute | ReservedAttributes.IsUnmanagedAttribute | ReservedAttributes.IsByRefLikeAttribute | ReservedAttributes.NullableContextAttribute | ReservedAttributes.CaseSensitiveExtensionAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(this, AttributeDescription.SecuritySafeCriticalAttribute))
            {
                if (IsAsync)
                {
                    arguments.Diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, arguments.AttributeSyntaxOpt.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName());
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
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullAttribute<MethodWellKnownAttributeData>(ContainingType, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.MemberNotNullWhenAttribute))
            {
                MessageID.IDS_FeatureMemberNotNull.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                CSharpAttributeData.DecodeMemberNotNullWhenAttribute<MethodWellKnownAttributeData>(ContainingType, ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ModuleInitializerAttribute))
            {
                MessageID.IDS_FeatureModuleInitializers.CheckFeatureAvailability(arguments.Diagnostics, arguments.AttributeSyntaxOpt);
                DecodeModuleInitializerAttribute(arguments);
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

                return true;
            }

            return false;
        }

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, DiagnosticBag diagnostics)
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
            Debug.Assert(!attribute.HasErrors);

            if (attribute.IsTargetAttribute(this, AttributeDescription.MarshalAsAttribute))
            {
                // MarshalAs applied to the return value:
                MarshalAsAttributeDecoder<ReturnTypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>.Decode(ref arguments, AttributeTargets.ReturnValue, MessageProvider.Instance);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                ReservedAttributes.DynamicAttribute | ReservedAttributes.IsUnmanagedAttribute | ReservedAttributes.IsReadOnlyAttribute | ReservedAttributes.IsByRefLikeAttribute | ReservedAttributes.TupleElementNamesAttribute | ReservedAttributes.NullableAttribute | ReservedAttributes.NativeIntegerAttribute))
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
            Debug.Assert(!attribute.HasErrors);
            bool hasErrors = false;

            var implementationPart = this.PartialImplementationPart ?? this;
            if (!implementationPart.IsExtern || !implementationPart.IsStatic)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnInvalidMethod, arguments.AttributeSyntaxOpt.Name.Location);
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
                arguments.Diagnostics.Add(ErrorCode.ERR_DllImportOnGenericMethod, arguments.AttributeSyntaxOpt.Name.Location);
                hasErrors = true;
            }

            string? moduleName = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);
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

            if (MethodKind != MethodKind.Ordinary)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary, arguments.AttributeSyntaxOpt.Location);
                return;
            }

            Debug.Assert(ContainingType is object);
            var hasError = false;

            HashSet<DiagnosticInfo>? useSiteDiagnostics = null;
            if (!AccessCheck.IsSymbolAccessible(this, ContainingAssembly, ref useSiteDiagnostics))
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            arguments.Diagnostics.Add(arguments.AttributeSyntaxOpt, useSiteDiagnostics);

            if (!IsStatic || ParameterCount > 0 || !ReturnsVoid)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            if (IsGenericMethod || ContainingType.IsGenericType)
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric, arguments.AttributeSyntaxOpt.Location, Name);
                hasError = true;
            }

            if (!hasError && !CallsAreOmitted(arguments.AttributeSyntaxOpt.SyntaxTree))
            {
                DeclaringCompilation.AddModuleInitializerMethod(this);
            }
        }
#nullable restore

        internal sealed override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(symbolPart == AttributeLocation.None || symbolPart == AttributeLocation.Return);

            if (symbolPart != AttributeLocation.Return)
            {
                Debug.Assert(_lazyCustomAttributesBag != null);
                Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);

                if (ContainingSymbol is NamedTypeSymbol { IsComImport: true, TypeKind: TypeKind.Class })
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
                    diagnostics.Add(errorCode, this.Locations[0], this);
                }
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        protected void AsyncMethodChecks(DiagnosticBag diagnostics)
        {
            if (IsAsync)
            {
                var errorLocation = this.Locations[0];

                if (this.RefKind != RefKind.None)
                {
                    var returnTypeSyntax = this.SyntaxNode switch
                    {
                        MethodDeclarationSyntax { ReturnType: var methodReturnType } => methodReturnType,
                        LocalFunctionStatementSyntax { ReturnType: var localReturnType } => localReturnType,
                        var unexpected => throw ExceptionUtilities.UnexpectedValue(unexpected)
                    };

                    ReportBadRefToken(returnTypeSyntax, diagnostics);
                }
                else if (ReturnType.IsBadAsyncReturn(this.DeclaringCompilation))
                {
                    diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, errorLocation);
                }

                for (NamedTypeSymbol curr = this.ContainingType; (object)curr != null; curr = curr.ContainingType)
                {
                    var sourceNamedTypeSymbol = curr as SourceNamedTypeSymbol;
                    if ((object)sourceNamedTypeSymbol != null && sourceNamedTypeSymbol.HasSecurityCriticalAttributes)
                    {
                        diagnostics.Add(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct, errorLocation);
                        break;
                    }
                }

                if ((this.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) != 0)
                {
                    diagnostics.Add(ErrorCode.ERR_SynchronizedAsyncMethod, errorLocation);
                }

                if (!diagnostics.HasAnyResolvedErrors())
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
                        ParameterTypesWithAnnotations.Any(p => p.Type.Equals(cancellationTokenType)))
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
                SecurityWellKnownAttributeData securityData = wellKnownData.SecurityInformation;
                if (securityData != null)
                {
                    return securityData.GetSecurityAttributes(attributesBag.Attributes);
                }
            }

            return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
        }

        public override DllImportData GetDllImportData()
        {
            var data = this.GetDecodedWellKnownAttributeData();
            return data != null ? data.DllImportPlatformInvokeData : null;
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
