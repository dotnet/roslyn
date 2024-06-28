// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Symbol
    {
        /// <summary>
        /// Gets the attributes for this symbol. Returns an empty <see cref="ImmutableArray&lt;AttributeData&gt;"/> if
        /// there are no attributes.
        /// </summary>
        public virtual ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            Debug.Assert(!(this is IAttributeTargetSymbol)); //such types must override

            // Return an empty array by default.
            // Sub-classes that can have custom attributes must
            // override this method
            return ImmutableArray<CSharpAttributeData>.Empty;
        }

        /// <summary>
        /// Gets the attribute target kind corresponding to the symbol kind
        /// If attributes cannot be applied to this symbol kind, returns
        /// an invalid AttributeTargets value of 0
        /// </summary>
        /// <returns>AttributeTargets or 0</returns>
        internal virtual AttributeTargets GetAttributeTarget()
        {
            switch (this.Kind)
            {
                case SymbolKind.Assembly:
                    return AttributeTargets.Assembly;

                case SymbolKind.Field:
                    return AttributeTargets.Field;

                case SymbolKind.Method:
                    var method = (MethodSymbol)this;
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                        case MethodKind.StaticConstructor:
                            return AttributeTargets.Constructor;

                        default:
                            return AttributeTargets.Method;
                    }

                case SymbolKind.NamedType:
                    var namedType = (NamedTypeSymbol)this;
                    switch (namedType.TypeKind)
                    {
                        case TypeKind.Class:
                            return AttributeTargets.Class;

                        case TypeKind.Delegate:
                            return AttributeTargets.Delegate;

                        case TypeKind.Enum:
                            return AttributeTargets.Enum;

                        case TypeKind.Interface:
                            return AttributeTargets.Interface;

                        case TypeKind.Struct:
                            return AttributeTargets.Struct;

                        case TypeKind.TypeParameter:
                            return AttributeTargets.GenericParameter;

                        case TypeKind.Submission:
                            // attributes can't be applied on a submission type:
                            throw ExceptionUtilities.UnexpectedValue(namedType.TypeKind);
                    }
                    break;

                case SymbolKind.NetModule:
                    return AttributeTargets.Module;

                case SymbolKind.Parameter:
                    return AttributeTargets.Parameter;

                case SymbolKind.Property:
                    return AttributeTargets.Property;

                case SymbolKind.Event:
                    return AttributeTargets.Event;

                case SymbolKind.TypeParameter:
                    return AttributeTargets.GenericParameter;
            }

            return 0;
        }

        /// <summary>
        /// Method to early decode the type of well-known attribute which can be queried during the BindAttributeType phase.
        /// This method is called first during attribute binding so that any attributes that affect semantics of type binding
        /// can be decoded here.
        /// </summary>
        /// <remarks>
        /// NOTE: If you are early decoding any new well-known attribute, make sure to update PostEarlyDecodeWellKnownAttributeTypes
        /// to default initialize this data.
        /// </remarks>
        internal virtual void EarlyDecodeWellKnownAttributeType(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
        }

        /// <summary>
        /// This method is called during attribute binding after EarlyDecodeWellKnownAttributeTypes has been executed.
        /// Symbols should default initialize the data for early decoded well-known attributes here.
        /// </summary>
        internal virtual void PostEarlyDecodeWellKnownAttributeTypes()
        {
        }

#nullable enable
        /// <summary>
        /// Method to early decode applied well-known attribute which can be queried by the binder.
        /// This method is called during attribute binding after we have bound the attribute types for all attributes,
        /// but haven't yet bound the attribute arguments/attribute constructor.
        /// Early decoding certain well-known attributes enables the binder to use this decoded information on this symbol
        /// when binding the attribute arguments/attribute constructor without causing attribute binding cycle.
        /// </summary>
        internal virtual (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            return (null, null);
        }

        internal static bool EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(
            ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments,
            out CSharpAttributeData? attributeData,
            out BoundAttribute? boundAttribute,
            out ObsoleteAttributeData? obsoleteData)
        {
            var type = arguments.AttributeType;
            var syntax = arguments.AttributeSyntax;

            ObsoleteAttributeKind kind;
            if (CSharpAttributeData.IsTargetEarlyAttribute(type, syntax, AttributeDescription.ObsoleteAttribute))
            {
                kind = ObsoleteAttributeKind.Obsolete;
            }
            else if (CSharpAttributeData.IsTargetEarlyAttribute(type, syntax, AttributeDescription.DeprecatedAttribute))
            {
                kind = ObsoleteAttributeKind.Deprecated;
            }
            else if (CSharpAttributeData.IsTargetEarlyAttribute(type, syntax, AttributeDescription.WindowsExperimentalAttribute))
            {
                kind = ObsoleteAttributeKind.WindowsExperimental;
            }
            else if (CSharpAttributeData.IsTargetEarlyAttribute(type, syntax, AttributeDescription.ExperimentalAttribute))
            {
                kind = ObsoleteAttributeKind.Experimental;
            }
            else
            {
                obsoleteData = null;
                attributeData = null;
                boundAttribute = null;
                return false;
            }

            bool hasAnyDiagnostics;
            (attributeData, boundAttribute) = arguments.Binder.GetAttribute(syntax, type, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
            if (!attributeData.HasErrors)
            {
                obsoleteData = attributeData.DecodeObsoleteAttribute(kind);
                if (hasAnyDiagnostics)
                {
                    attributeData = null;
                    boundAttribute = null;
                }
            }
            else
            {
                obsoleteData = null;
                attributeData = null;
                boundAttribute = null;
            }
            return true;
        }

        /// <summary>
        /// This method is called by the binder when it is finished binding a set of attributes on the symbol so that
        /// the symbol can extract data from the attribute arguments and potentially perform validation specific to
        /// some well known attributes.
        /// <para>
        /// NOTE: If we are decoding a well-known attribute that could be queried by the binder, consider decoding it during early decoding pass.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Symbol types should override this if they want to handle a specific well-known attribute.
        /// If the attribute is of a type that the symbol does not wish to handle, it should delegate back to
        /// this (base) method.
        /// </para>
        /// </remarks>
        protected void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.Diagnostics.DiagnosticBag is not null);
            Debug.Assert(arguments.AttributeSyntaxOpt is not null);
            if (arguments.Attribute.IsTargetAttribute(AttributeDescription.CompilerFeatureRequiredAttribute))
            {
                // Do not use '{FullName}'. This is reserved for compiler usage.
                arguments.Diagnostics.DiagnosticBag.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.CompilerFeatureRequiredAttribute.FullName);
            }
            else if (arguments.Attribute.IsTargetAttribute(AttributeDescription.ExperimentalAttribute))
            {
                if (!SyntaxFacts.IsValidIdentifier((string?)arguments.Attribute.CommonConstructorArguments[0].ValueInternal))
                {
                    var attrArgumentLocation = arguments.Attribute.GetAttributeArgumentLocation(parameterIndex: 0);
                    arguments.Diagnostics.DiagnosticBag.Add(ErrorCode.ERR_InvalidExperimentalDiagID, attrArgumentLocation);
                }
            }

            DecodeWellKnownAttributeImpl(ref arguments);
        }
#nullable disable

        protected virtual void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
        }

        /// <summary>
        /// Called to report attribute related diagnostics after all attributes have been bound and decoded.
        /// Called even if there are no attributes.
        /// </summary>
        /// <remarks>
        /// This method is called by the binder from <see cref="LoadAndValidateAttributes"/> after it has finished binding attributes on the symbol,
        /// has executed <see cref="DecodeWellKnownAttribute"/> for attributes applied on the symbol and has stored the decoded data in the
        /// lazyCustomAttributesBag on the symbol. Bound attributes haven't been stored on the bag yet.
        ///
        /// Post-validation for attributes that is dependent on other attributes can be done here.
        ///
        /// This method should not have any side effects on the symbol, i.e. it SHOULD NOT change the symbol state.
        /// </remarks>
        /// <param name="boundAttributes">Bound attributes.</param>
        /// <param name="allAttributeSyntaxNodes">Syntax nodes of attributes in order they are specified in source, or null if there are no attributes.</param>
        /// <param name="diagnostics">Diagnostic bag.</param>
        /// <param name="symbolPart">Specific part of the symbol to which the attributes apply, or <see cref="AttributeLocation.None"/> if the attributes apply to the symbol itself.</param>
        /// <param name="decodedData">Decoded well-known attribute data, could be null.</param>
        internal virtual void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
        }

#nullable enable
        /// <summary>
        /// This method does the following set of operations in the specified order:
        /// (1) GetAttributesToBind: Merge attributes from the given attributesSyntaxLists and filter out attributes by attribute target.
        /// (2) BindAttributeTypes: Bind all the attribute types to enable early decode of certain well-known attributes by type.
        /// (3) EarlyDecodeWellKnownAttributes: Perform early decoding of certain well-known attributes that could be queried by the binder in subsequent steps.
        ///     (NOTE: This step has the side effect of updating the symbol state based on the data extracted from well known attributes).
        /// (4) GetAttributes: Bind the attributes (attribute arguments and constructor) using bound attribute types.
        /// (5) DecodeWellKnownAttributes: Decode and validate bound well known attributes.
        ///     (NOTE: This step has the side effect of updating the symbol state based on the data extracted from well known attributes).
        /// (6) StoreBoundAttributesAndDoPostValidation:
        ///     (a) Store the bound attributes in lazyCustomAttributes in a thread safe manner.
        ///     (b) Perform some additional post attribute validations, such as
        ///         1) Duplicate attributes, attribute usage target validation, etc.
        ///         2) Post validation for attributes dependent on other attributes
        ///         These validations cannot be performed prior to step 6(a) as we might need to
        ///         perform a GetAttributes() call on a symbol which can introduce a cycle in attribute binding.
        ///         We avoid this cycle by performing such validations in PostDecodeWellKnownAttributes after lazyCustomAttributes have been set.
        ///     NOTE: PostDecodeWellKnownAttributes SHOULD NOT change the symbol state.
        /// </summary>
        /// <remarks>
        /// Current design of early decoding well-known attributes doesn't permit decoding attribute arguments/constructor as this can lead to binding cycles.
        /// For well-known attributes used by the binder, where we need the decoded arguments, we must handle them specially in one of the following possible ways:
        ///   (a) Avoid decoding the attribute arguments during binding and delay the corresponding binder tasks to a separate post-pass executed after binding.
        ///   (b) As the cycles can be caused only when we are binding attribute arguments/constructor, special case the corresponding binder tasks based on the current BinderFlags.
        /// </remarks>
        /// <param name="attributesSyntaxLists"></param>
        /// <param name="lazyCustomAttributesBag"></param>
        /// <param name="symbolPart">Specific part of the symbol to which the attributes apply, or <see cref="AttributeLocation.None"/> if the attributes apply to the symbol itself.</param>
        /// <param name="earlyDecodingOnly">Indicates that only early decoding should be performed.  WARNING: the resulting bag will not be sealed.</param>
        /// <param name="binderOpt">Binder to use. If null, <see cref="DeclaringCompilation"/> GetBinderFactory will be used.</param>
        /// <param name="attributeMatchesOpt">If specified, only load attributes that match this predicate, and any diagnostics produced will be dropped.</param>
        /// <param name="beforeAttributePartBound">If specified, invoked before any part of the attribute syntax is bound.</param>
        /// <param name="afterAttributePartBound">If specified, invoked after any part of the attribute syntax is bound.</param>
        /// <returns>Flag indicating whether lazyCustomAttributes were stored on this thread. Caller should check for this flag and perform NotePartComplete if true.</returns>
        internal bool LoadAndValidateAttributes(
            OneOrMany<SyntaxList<AttributeListSyntax>> attributesSyntaxLists,
            ref CustomAttributesBag<CSharpAttributeData>? lazyCustomAttributesBag,
            AttributeLocation symbolPart = AttributeLocation.None,
            bool earlyDecodingOnly = false,
            Binder? binderOpt = null,
            Func<AttributeSyntax, bool>? attributeMatchesOpt = null,
            Action<AttributeSyntax>? beforeAttributePartBound = null,
            Action<AttributeSyntax>? afterAttributePartBound = null)
        {
            var diagnostics = BindingDiagnosticBag.GetInstance();
            var compilation = this.DeclaringCompilation;

            ImmutableArray<Binder> binders;
            BoundAttribute[]? boundAttributeArray;
            ImmutableArray<AttributeSyntax> attributesToBind = this.GetAttributesToBind(attributesSyntaxLists, symbolPart, diagnostics, compilation, attributeMatchesOpt, binderOpt, out binders);
            int totalAttributesCount = attributesToBind.Length;
            Debug.Assert(!attributesToBind.IsDefault);

            ImmutableArray<CSharpAttributeData> boundAttributes;
            WellKnownAttributeData? wellKnownAttributeData;

            if (totalAttributesCount != 0)
            {
                Debug.Assert(!binders.IsDefault);
                Debug.Assert(binders.Length == attributesToBind.Length);

                // Initialize the bag so that data decoded from early attributes can be stored onto it.
                if (lazyCustomAttributesBag == null)
                {
                    Interlocked.CompareExchange(ref lazyCustomAttributesBag, new CustomAttributesBag<CSharpAttributeData>(), null);
                }

                // Bind the attribute types and then early decode them.
                var attributeTypesBuilder = new NamedTypeSymbol[totalAttributesCount];

                Binder.BindAttributeTypes(binders, attributesToBind, this, attributeTypesBuilder, beforeAttributePartBound, afterAttributePartBound, diagnostics);

                bool interestedInDiagnostics = !earlyDecodingOnly && attributeMatchesOpt is null;
                if (interestedInDiagnostics)
                {
                    for (var i = 0; i < totalAttributesCount; i++)
                    {
                        if (attributeTypesBuilder[i].IsGenericType)
                        {
                            MessageID.IDS_FeatureGenericAttributes.CheckFeatureAvailability(diagnostics, attributesToBind[i]);
                        }
                    }
                }

                ImmutableArray<NamedTypeSymbol> boundAttributeTypes = attributeTypesBuilder.AsImmutableOrNull();

                this.EarlyDecodeWellKnownAttributeTypes(boundAttributeTypes, attributesToBind);
                this.PostEarlyDecodeWellKnownAttributeTypes();

                // Bind the attribute in two stages - early and normal.
                var attributeDataArray = new CSharpAttributeData[totalAttributesCount];
                boundAttributeArray = interestedInDiagnostics ? new BoundAttribute[totalAttributesCount] : null;

                // Early bind and decode some well-known attributes.
                EarlyWellKnownAttributeData? earlyData = this.EarlyDecodeWellKnownAttributes(binders, boundAttributeTypes, attributesToBind, symbolPart, attributeDataArray, boundAttributeArray);
                Debug.Assert(!attributeDataArray.Contains((attr) => attr != null && attr.HasErrors));

                // Store data decoded from early bound well-known attributes.
                // TODO: what if this succeeds on another thread, not ours?
                lazyCustomAttributesBag.SetEarlyDecodedWellKnownAttributeData(earlyData);

                if (earlyDecodingOnly)
                {
                    diagnostics.Free(); //NOTE: dropped.
                    return false;
                }

                // Bind attributes.
                Binder.GetAttributes(binders, attributesToBind, boundAttributeTypes, attributeDataArray, boundAttributeArray, beforeAttributePartBound, afterAttributePartBound, diagnostics);
                boundAttributes = attributeDataArray.AsImmutableOrNull();

                // All attributes must be bound by now.
                Debug.Assert(!boundAttributes.Any(static (attr) => attr == null));

                // Validate attribute usage and Decode remaining well-known attributes.
                wellKnownAttributeData = this.ValidateAttributeUsageAndDecodeWellKnownAttributes(binders, attributesToBind, boundAttributes, diagnostics, symbolPart);

                // Store data decoded from remaining well-known attributes.
                // TODO: what if this succeeds on another thread but not this thread?
                lazyCustomAttributesBag.SetDecodedWellKnownAttributeData(wellKnownAttributeData);
            }
            else if (earlyDecodingOnly)
            {
                diagnostics.Free(); //NOTE: dropped.
                return false;
            }
            else
            {
                boundAttributes = ImmutableArray<CSharpAttributeData>.Empty;
                boundAttributeArray = null;
                wellKnownAttributeData = null;
                Interlocked.CompareExchange(ref lazyCustomAttributesBag, CustomAttributesBag<CSharpAttributeData>.WithEmptyData(), null);
                this.PostEarlyDecodeWellKnownAttributeTypes();
            }

            Debug.Assert(!earlyDecodingOnly);

            // Store attributes into the bag.
            bool lazyAttributesStoredOnThisThread = false;
            if (lazyCustomAttributesBag.SetAttributes(boundAttributes))
            {
                if (attributeMatchesOpt is null)
                {
                    this.PostDecodeWellKnownAttributes(boundAttributes, attributesToBind, diagnostics, symbolPart, wellKnownAttributeData);

                    removeObsoleteDiagnosticsForForwardedTypes(boundAttributes, attributesToBind, ref diagnostics);
                    Debug.Assert(diagnostics.DiagnosticBag is not null);

                    this.RecordPresenceOfBadAttributes(boundAttributes);

                    if (totalAttributesCount != 0)
                    {
                        Debug.Assert(boundAttributeArray is not null);
                        for (var i = 0; i < totalAttributesCount; i++)
                        {
                            var boundAttribute = boundAttributeArray[i];
                            Debug.Assert(boundAttribute is not null);
                            if (boundAttribute.Constructor is { } ctor)
                            {
                                Binder.CheckRequiredMembersInObjectInitializer(ctor, ImmutableArray<BoundExpression>.CastUp(boundAttribute.NamedArguments), boundAttribute.Syntax, diagnostics);
                            }
                            NullableWalker.AnalyzeIfNeeded(binders[i], boundAttribute, boundAttribute.Syntax, diagnostics.DiagnosticBag);
                        }
                    }

                    AddDeclarationDiagnostics(diagnostics);
                }
                lazyAttributesStoredOnThisThread = true;
                if (lazyCustomAttributesBag.IsEmpty) lazyCustomAttributesBag = CustomAttributesBag<CSharpAttributeData>.Empty;
            }

            Debug.Assert(lazyCustomAttributesBag.IsSealed);
            diagnostics.Free();
            return lazyAttributesStoredOnThisThread;

            void removeObsoleteDiagnosticsForForwardedTypes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> attributesToBind, ref BindingDiagnosticBag diagnostics)
            {
                Debug.Assert(diagnostics.DiagnosticBag is not null);

                if (!boundAttributes.IsDefaultOrEmpty &&
                    this is SourceAssemblySymbol &&
                    !diagnostics.DiagnosticBag.IsEmptyWithoutResolution &&
                    diagnostics.DiagnosticBag.AsEnumerableWithoutResolution().OfType<DiagnosticWithInfo>().Where(isObsoleteDiagnostic).Any())
                {
                    // We are binding attributes for an assembly and have an obsolete diagnostic reported,
                    // or we have lazy diagnostic, that might be resolved to an obsolete diagnostic later.
                    // We would like to filter out a diagnostic like that for a forwarded type.
                    // The TypeForwardedTo attribute takes only one argument, which must be System.Type and it
                    // designates the forwarded type. The only form of System.Type value accepted
                    // as an argument for an attribute is a 'typeof' expression. The only obsolete diagnostics
                    // that can be reported for a 'typeof' expression, is diagnostics for its argument, which is
                    // the reference to a type. A forwarded type, when we are dealing with a TypeForwardedTo
                    // application.

                    // The general strategy:
                    //    1. Collect locations of the first argument of each TypeForwardedTo attribute application.
                    //    2. Collect obsolete diagnostics reported within the span of those locations.
                    //    3. Remove the collected diagnostics, if any.

                    var builder = ArrayBuilder<Location>.GetInstance();
                    int totalAttributesCount = attributesToBind.Length;

                    Debug.Assert(totalAttributesCount == boundAttributes.Length);

                    //    1. Collect locations of the first argument of each TypeForwardedTo attribute application.
                    for (int i = 0; i < totalAttributesCount; i++)
                    {
                        CSharpAttributeData boundAttribute = boundAttributes[i];

                        if (!boundAttribute.HasErrors && boundAttribute.IsTargetAttribute(AttributeDescription.TypeForwardedToAttribute) &&
                            boundAttribute.CommonConstructorArguments[0].ValueInternal is TypeSymbol &&
                            attributesToBind[i].ArgumentList?.Arguments[0].Expression.Location is { } location)
                        {
                            builder.Add(location);
                        }
                    }

                    if (builder.Count != 0)
                    {
                        var toRemove = new HashSet<Diagnostic>(ReferenceEqualityComparer.Instance);

                        //    2. Collect obsolete diagnostics reported within the span of those locations.
                        foreach (Diagnostic d in diagnostics.DiagnosticBag.AsEnumerableWithoutResolution())
                        {
                            if (d is DiagnosticWithInfo withInfo && isObsoleteDiagnostic(withInfo))
                            {
                                Location location = withInfo.Location;

                                foreach (Location argumentLocation in builder)
                                {
                                    if (location.SourceTree == argumentLocation.SourceTree &&
                                        argumentLocation.SourceSpan.Contains(location.SourceSpan))
                                    {
                                        toRemove.Add(withInfo);
                                        break;
                                    }
                                }
                            }
                        }

                        //    3. Remove the collected diagnostics, if any.
                        if (toRemove.Count != 0)
                        {
                            var filtered = BindingDiagnosticBag.GetInstance();

                            filtered.AddDependencies(diagnostics);

                            foreach (Diagnostic d in diagnostics.DiagnosticBag.AsEnumerableWithoutResolution())
                            {
                                if (!toRemove.Contains(d))
                                {
                                    filtered.Add(d);
                                }
                            }

                            diagnostics.Free();
                            diagnostics = filtered;
                        }
                    }

                    builder.Free();
                }
            }

            static bool isObsoleteDiagnostic(DiagnosticWithInfo d)
            {
                return d.HasLazyInfo ? d.LazyInfo is LazyObsoleteDiagnosticInfo : d.Info.IsObsoleteDiagnostic();
            }
        }

        /// <summary>
        /// Binds attributes applied to this symbol.
        /// </summary>
        protected ImmutableArray<(CSharpAttributeData, BoundAttribute)> BindAttributes(OneOrMany<SyntaxList<AttributeListSyntax>> attributeDeclarations, Binder? rootBinder)
        {
            var boundAttributeArrayBuilder = ArrayBuilder<(CSharpAttributeData, BoundAttribute)>.GetInstance();
            foreach (var attributeListSyntaxList in attributeDeclarations)
            {
                var binder = GetAttributeBinder(attributeListSyntaxList, DeclaringCompilation, rootBinder);
                foreach (var attributeListSyntax in attributeListSyntaxList)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var boundType = binder.BindType(attributeSyntax.Name, BindingDiagnosticBag.Discarded);
                        var boundTypeSymbol = (NamedTypeSymbol)boundType.Type;
                        var boundAttribute = binder.GetAttribute(attributeSyntax, boundTypeSymbol,
                            beforeAttributePartBound: null, afterAttributePartBound: null, BindingDiagnosticBag.Discarded);
                        boundAttributeArrayBuilder.Add(boundAttribute);
                    }
                }
            }
            return boundAttributeArrayBuilder.ToImmutableAndFree();
        }
#nullable disable

        private void RecordPresenceOfBadAttributes(ImmutableArray<CSharpAttributeData> boundAttributes)
        {
            foreach (var attribute in boundAttributes)
            {
                if (attribute.HasErrors)
                {
                    CSharpCompilation compilation = this.DeclaringCompilation;
                    Debug.Assert(compilation != null);
                    ((SourceModuleSymbol)compilation.SourceModule).RecordPresenceOfBadAttributes();
                    break;
                }
            }
        }

        /// <summary>
        /// Method to merge attributes from the given attributesSyntaxLists and filter out attributes by attribute target.
        /// This is the first step in attribute binding.
        /// </summary>
        /// <remarks>
        /// This method can generate diagnostics for few cases where we have an invalid target specifier and the parser hasn't generated the necessary diagnostics.
        /// It should not perform any bind operations as it can lead to an attribute binding cycle.
        /// </remarks>
        private ImmutableArray<AttributeSyntax> GetAttributesToBind(
            OneOrMany<SyntaxList<AttributeListSyntax>> attributeDeclarationSyntaxLists,
            AttributeLocation symbolPart,
            BindingDiagnosticBag diagnostics,
            CSharpCompilation compilation,
            Func<AttributeSyntax, bool> attributeMatchesOpt,
            Binder rootBinderOpt,
            out ImmutableArray<Binder> binders)
        {
            var attributeTarget = (IAttributeTargetSymbol)this;

            ArrayBuilder<AttributeSyntax> syntaxBuilder = null;
            ArrayBuilder<Binder> bindersBuilder = null;
            int attributesToBindCount = 0;

            for (int listIndex = 0; listIndex < attributeDeclarationSyntaxLists.Count; listIndex++)
            {
                var attributeDeclarationSyntaxList = attributeDeclarationSyntaxLists[listIndex];
                if (attributeDeclarationSyntaxList.Any())
                {
                    int prevCount = attributesToBindCount;
                    foreach (var attributeDeclarationSyntax in attributeDeclarationSyntaxList)
                    {
                        // We bind the attribute only if it has a matching target for the given ownerSymbol and attributeLocation.
                        if (MatchAttributeTarget(attributeTarget, symbolPart, attributeDeclarationSyntax.Target, diagnostics) &&
                            ShouldBindAttributes(attributeDeclarationSyntax, diagnostics))
                        {
                            if (syntaxBuilder == null)
                            {
                                syntaxBuilder = new ArrayBuilder<AttributeSyntax>();
                                bindersBuilder = new ArrayBuilder<Binder>();
                            }

                            var attributesToBind = attributeDeclarationSyntax.Attributes;
                            if (attributeMatchesOpt is null)
                            {
                                syntaxBuilder.AddRange(attributesToBind);
                                attributesToBindCount += attributesToBind.Count;
                            }
                            else
                            {
                                foreach (var attribute in attributesToBind)
                                {
                                    if (attributeMatchesOpt(attribute))
                                    {
                                        syntaxBuilder.Add(attribute);
                                        attributesToBindCount++;
                                    }
                                }
                            }
                        }
                    }

                    if (attributesToBindCount != prevCount)
                    {
                        Debug.Assert(bindersBuilder != null);

                        var binder = GetAttributeBinder(attributeDeclarationSyntaxList, compilation, rootBinderOpt);

                        for (int i = 0; i < attributesToBindCount - prevCount; i++)
                        {
                            bindersBuilder.Add(binder);
                        }
                    }
                }
            }

            if (syntaxBuilder != null)
            {
                binders = bindersBuilder.ToImmutableAndFree();
                return syntaxBuilder.ToImmutableAndFree();
            }
            else
            {
                binders = ImmutableArray<Binder>.Empty;
                return ImmutableArray<AttributeSyntax>.Empty;
            }
        }

        protected virtual bool ShouldBindAttributes(AttributeListSyntax attributeDeclarationSyntax, BindingDiagnosticBag diagnostics)
        {
            return true;
        }

#nullable enable
        private Binder GetAttributeBinder(SyntaxList<AttributeListSyntax> attributeDeclarationSyntaxList, CSharpCompilation compilation, Binder? rootBinder = null)
        {
            var binder = rootBinder ?? compilation.GetBinderFactory(attributeDeclarationSyntaxList.Node!.SyntaxTree).GetBinder(attributeDeclarationSyntaxList.Node);
            binder = new ContextualAttributeBinder(binder, this);
            Debug.Assert(!binder.InAttributeArgument || this is MethodSymbol { MethodKind: MethodKind.LambdaMethod or MethodKind.LocalFunction }, "Possible cycle in attribute binding");
            return binder;
        }
#nullable disable

        private static bool MatchAttributeTarget(IAttributeTargetSymbol attributeTarget, AttributeLocation symbolPart, AttributeTargetSpecifierSyntax targetOpt, BindingDiagnosticBag diagnostics)
        {
            IAttributeTargetSymbol attributesOwner = attributeTarget.AttributesOwner;

            // Determine if the target symbol owns the attribute declaration.
            // We need to report diagnostics only once, so do it when visiting attributes for the owner.
            bool isOwner = symbolPart == AttributeLocation.None && ReferenceEquals(attributesOwner, attributeTarget);

            if (targetOpt == null)
            {
                // only attributes with an explicit target match if the symbol doesn't own the attributes:
                return isOwner;
            }

            // Special error code for this case.
            if (isOwner &&
                targetOpt.Identifier.ToAttributeLocation() == AttributeLocation.Module)
            {
                var parseOptions = (CSharpParseOptions)targetOpt.SyntaxTree.Options;
                if (parseOptions.LanguageVersion == LanguageVersion.CSharp1)
                    diagnostics.Add(ErrorCode.WRN_NonECMAFeature, targetOpt.GetLocation(), MessageID.IDS_FeatureModuleAttrLoc);
            }

            AttributeLocation allowedTargets = attributesOwner.AllowedAttributeLocations;

            AttributeLocation explicitTarget = targetOpt.GetAttributeLocation();
            if (explicitTarget == AttributeLocation.None)
            {
                // error: unknown attribute location
                if (isOwner)
                {
                    //NOTE: ValueText so that we accept targets like "@return", to match dev10 (DevDiv #2591).
                    diagnostics.Add(ErrorCode.WRN_InvalidAttributeLocation,
                        targetOpt.Identifier.GetLocation(), targetOpt.Identifier.ValueText, allowedTargets.ToDisplayString());
                }

                return false;
            }

            if ((explicitTarget & allowedTargets) == 0)
            {
                // error: invalid target for symbol
                if (isOwner)
                {
                    if (allowedTargets == AttributeLocation.None)
                    {
                        switch (attributeTarget.DefaultAttributeLocation)
                        {
                            case AttributeLocation.Assembly:
                            case AttributeLocation.Module:
                                // global attributes are disallowed in interactive code:
                                diagnostics.Add(ErrorCode.ERR_GlobalAttributesNotAllowed, targetOpt.Identifier.GetLocation());
                                break;

                            default:
                                // currently this can't happen
                                throw ExceptionUtilities.UnexpectedValue(attributeTarget.DefaultAttributeLocation);
                        }
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.WRN_AttributeLocationOnBadDeclaration,
                            targetOpt.Identifier.GetLocation(), targetOpt.Identifier.ToString(), allowedTargets.ToDisplayString());
                    }
                }

                return false;
            }

            if (symbolPart == AttributeLocation.None)
            {
                return explicitTarget == attributeTarget.DefaultAttributeLocation;
            }
            else
            {
                return explicitTarget == symbolPart;
            }
        }

#nullable enable
        /// <summary>
        /// Method to early decode certain well-known attributes which can be queried by the binder.
        /// This method is called during attribute binding after we have bound the attribute types for all attributes,
        /// but haven't yet bound the attribute arguments/attribute constructor.
        /// Early decoding certain well-known attributes enables the binder to use this decoded information on this symbol
        /// when binding the attribute arguments/attribute constructor without causing attribute binding cycle.
        /// </summary>
        internal EarlyWellKnownAttributeData? EarlyDecodeWellKnownAttributes(
            ImmutableArray<Binder> binders,
            ImmutableArray<NamedTypeSymbol> boundAttributeTypes,
            ImmutableArray<AttributeSyntax> attributesToBind,
            AttributeLocation symbolPart,
            CSharpAttributeData?[] attributeDataArray,
            BoundAttribute?[]? boundAttributeArray)
        {
            Debug.Assert(boundAttributeTypes.Any());
            Debug.Assert(attributesToBind.Any());
            Debug.Assert(binders.Any());
            Debug.Assert(attributeDataArray != null);
            Debug.Assert(!attributeDataArray.Contains((attr) => attr != null));

            var earlyBinder = new EarlyWellKnownAttributeBinder(binders[0]);
            var arguments = new EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation>();
            arguments.SymbolPart = symbolPart;

            for (int i = 0; i < boundAttributeTypes.Length; i++)
            {
                NamedTypeSymbol boundAttributeType = boundAttributeTypes[i];
                if (!boundAttributeType.IsErrorType())
                {
                    if (binders[i] != earlyBinder.Next)
                    {
                        earlyBinder = new EarlyWellKnownAttributeBinder(binders[i]);
                    }

                    arguments.Binder = earlyBinder;
                    arguments.AttributeType = boundAttributeType;
                    arguments.AttributeSyntax = attributesToBind[i];

                    // Early bind some well-known attributes
                    (CSharpAttributeData? earlyAttributeDataOpt, BoundAttribute? boundAttributeOpt) = this.EarlyDecodeWellKnownAttribute(ref arguments);
                    Debug.Assert(earlyAttributeDataOpt == null || !earlyAttributeDataOpt.HasErrors);
                    Debug.Assert(boundAttributeOpt is null == earlyAttributeDataOpt is null);

                    attributeDataArray[i] = earlyAttributeDataOpt;
                    if (boundAttributeArray is not null)
                    {
                        boundAttributeArray[i] = boundAttributeOpt;
                    }
                }
            }

            return arguments.HasDecodedData ? arguments.DecodedData : null;
        }
#nullable disable

        private void EarlyDecodeWellKnownAttributeTypes(ImmutableArray<NamedTypeSymbol> attributeTypes, ImmutableArray<AttributeSyntax> attributeSyntaxList)
        {
            Debug.Assert(attributeSyntaxList.Any());
            Debug.Assert(attributeTypes.Any());

            for (int i = 0; i < attributeTypes.Length; i++)
            {
                var attributeType = attributeTypes[i];

                if (!attributeType.IsErrorType())
                {
                    this.EarlyDecodeWellKnownAttributeType(attributeType, attributeSyntaxList[i]);
                }
            }
        }

        /// <summary>
        /// This method validates attribute usage for each bound attribute and calls <see cref="DecodeWellKnownAttributeImpl"/>
        /// on attributes with valid attribute usage.
        /// This method is called by the binder when it is finished binding a set of attributes on the symbol so that
        /// the symbol can extract data from the attribute arguments and potentially perform validation specific to
        /// some well known attributes.
        /// </summary>
        private WellKnownAttributeData ValidateAttributeUsageAndDecodeWellKnownAttributes(
            ImmutableArray<Binder> binders,
            ImmutableArray<AttributeSyntax> attributeSyntaxList,
            ImmutableArray<CSharpAttributeData> boundAttributes,
            BindingDiagnosticBag diagnostics,
            AttributeLocation symbolPart)
        {
            Debug.Assert(binders.Any());
            Debug.Assert(attributeSyntaxList.Any());
            Debug.Assert(boundAttributes.Any());
            Debug.Assert(binders.Length == boundAttributes.Length);
            Debug.Assert(attributeSyntaxList.Length == boundAttributes.Length);

            int totalAttributesCount = boundAttributes.Length;
            HashSet<NamedTypeSymbol> uniqueAttributeTypes = new HashSet<NamedTypeSymbol>();
            var arguments = new DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation>();
            arguments.Diagnostics = diagnostics;
            arguments.AttributesCount = totalAttributesCount;
            arguments.SymbolPart = symbolPart;

            for (int i = 0; i < totalAttributesCount; i++)
            {
                CSharpAttributeData boundAttribute = boundAttributes[i];
                AttributeSyntax attributeSyntax = attributeSyntaxList[i];
                Binder binder = binders[i];

                // Decode attribute as a possible well-known attribute only if it has no binding errors and has valid AttributeUsage.
                if (!boundAttribute.HasErrors && ValidateAttributeUsage(boundAttribute, attributeSyntax, binder.Compilation, symbolPart, diagnostics, uniqueAttributeTypes))
                {
                    arguments.Attribute = boundAttribute;
                    arguments.AttributeSyntaxOpt = attributeSyntax;
                    arguments.Index = i;

                    this.DecodeWellKnownAttribute(ref arguments);
                }
            }

            return arguments.HasDecodedData ? arguments.DecodedData : null;
        }

        /// <summary>
        /// Validate attribute usage target and duplicate attributes.
        /// </summary>
        /// <param name="attribute">Bound attribute</param>
        /// <param name="node">Syntax node for attribute specification</param>
        /// <param name="compilation">Compilation</param>
        /// <param name="symbolPart">Symbol part to which the attribute has been applied.</param>
        /// <param name="diagnostics">Diagnostics</param>
        /// <param name="uniqueAttributeTypes">Set of unique attribute types applied to the symbol</param>
        private bool ValidateAttributeUsage(
            CSharpAttributeData attribute,
            AttributeSyntax node,
            CSharpCompilation compilation,
            AttributeLocation symbolPart,
            BindingDiagnosticBag diagnostics,
            HashSet<NamedTypeSymbol> uniqueAttributeTypes)
        {
            Debug.Assert(!attribute.HasErrors);

            NamedTypeSymbol attributeType = attribute.AttributeClass;
            AttributeUsageInfo attributeUsageInfo = attributeType.GetAttributeUsageInfo();

            // Given attribute can't be specified more than once if AllowMultiple is false.
            if (!uniqueAttributeTypes.Add(attributeType.OriginalDefinition) && !attributeUsageInfo.AllowMultiple)
            {
                diagnostics.Add(ErrorCode.ERR_DuplicateAttribute, node.Name.Location, node.GetErrorDisplayName());
                return false;
            }

            // Verify if the attribute type can be applied to given owner symbol.
            AttributeTargets attributeTarget;
            if (symbolPart == AttributeLocation.Return)
            {
                // attribute on return type
                Debug.Assert(this.Kind == SymbolKind.Method);
                attributeTarget = AttributeTargets.ReturnValue;
            }
            else
            {
                attributeTarget = this.GetAttributeTarget();
            }

            if ((attributeTarget & attributeUsageInfo.ValidTargets) == 0)
            {
                // generate error
                diagnostics.Add(ErrorCode.ERR_AttributeOnBadSymbolType, node.Name.Location, node.GetErrorDisplayName(), attributeUsageInfo.GetValidTargetsErrorArgument());
                return false;
            }

            if (attribute.IsSecurityAttribute(compilation))
            {
                switch (this.Kind)
                {
                    case SymbolKind.Assembly:
                    case SymbolKind.NamedType:
                    case SymbolKind.Method:
                        break;

                    default:
                        // CS7070: Security attribute '{0}' is not valid on this declaration type. Security attributes are only valid on assembly, type and method declarations.
                        diagnostics.Add(ErrorCode.ERR_SecurityAttributeInvalidTarget, node.Name.Location, node.GetErrorDisplayName());
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Ensure that attributes are bound and the ObsoleteState/ExperimentalState of this symbol is known.
        /// </summary>
        internal void ForceCompleteObsoleteAttribute()
        {
            if (this.ObsoleteKind == ObsoleteAttributeKind.Uninitialized)
            {
                this.GetAttributes();
            }
            Debug.Assert(this.ObsoleteState != ThreeState.Unknown, "ObsoleteState should be true or false now.");
            Debug.Assert(this.ExperimentalState != ThreeState.Unknown, "ExperimentalState should be true or false now.");

            this.ContainingSymbol?.ForceCompleteObsoleteAttribute();
        }
    }
}
