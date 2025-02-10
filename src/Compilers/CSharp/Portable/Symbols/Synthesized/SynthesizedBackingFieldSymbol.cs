﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated backing field for an automatically implemented property or
    /// a Primary Constructor parameter.
    /// </summary>
    internal abstract class SynthesizedBackingFieldSymbolBase : FieldSymbolWithAttributesAndModifiers
    {
        private readonly string _name;
        internal abstract bool HasInitializer { get; }
        protected override DeclarationModifiers Modifiers { get; }

        public SynthesizedBackingFieldSymbolBase(
            string name,
            bool isReadOnly,
            bool isStatic)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            _name = name;

            Modifiers = DeclarationModifiers.Private |
                (isReadOnly ? DeclarationModifiers.ReadOnly : DeclarationModifiers.None) |
                (isStatic ? DeclarationModifiers.Static : DeclarationModifiers.None);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;

            // do not emit CompilerGenerated attributes for fields inside compiler generated types:
            if (!this.ContainingType.IsImplicitlyDeclared)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            // Dev11 doesn't synthesize this attribute, the debugger has a knowledge
            // of special name C# compiler uses for backing fields, which is not desirable.
            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute());
        }

        public override string Name
            => _name;

        internal override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
            => null;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        internal override bool HasRuntimeSpecialName
            => false;

        public override bool IsImplicitlyDeclared
            => true;

        internal override bool IsRequired => false;
    }

    /// <summary>
    /// Represents a compiler generated backing field for an automatically implemented property.
    /// </summary>
    internal sealed class SynthesizedBackingFieldSymbol : SynthesizedBackingFieldSymbolBase
    {
        private readonly SourcePropertySymbolBase _property;
        internal override bool HasInitializer { get; }

        public SynthesizedBackingFieldSymbol(
            SourcePropertySymbolBase property,
            string name,
            bool isReadOnly,
            bool isStatic,
            bool hasInitializer)
            : base(name, isReadOnly, isStatic)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(property.RefKind is RefKind.None or RefKind.Ref or RefKind.RefReadOnly);
            _property = property;
            HasInitializer = hasInitializer;
        }

        protected override IAttributeTargetSymbol AttributeOwner
            => _property.AttributesOwner;

        internal override Location ErrorLocation
            => _property.Location;

        protected override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // The backing field for a partial property may have been calculated for either
            // the definition part or the implementation part. Regardless, we should use
            // the attributes from the definition part.
            var property = (_property as SourcePropertySymbol)?.SourcePartialDefinitionPart ?? _property;
            return property.GetAttributeDeclarations();
        }

        public override Symbol AssociatedSymbol
            => _property;

        public override ImmutableArray<Location> Locations
            => _property.Locations;

        public override RefKind RefKind => _property.RefKind;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _property.RefCustomModifiers;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
            => _property.TypeWithAnnotations;

#nullable enable
        /// <summary>If null, binding+flow analysis of getter is required to determine null resilience. If non-null, then whether the getter is null-resilient is trivially known.</summary>
        private bool? IsGetterTriviallyNullResilient
        {
            get
            {
                // getter is vacuously null-resilient, since it doesn't exist.
                if (_property.GetMethod is not SourcePropertyAccessorSymbol getMethod)
                {
                    Debug.Assert(_property.GetMethod is null);
                    return true;
                }

                // auto-implemented getter is not null-resilient.
                if (getMethod.IsAutoPropertyAccessor)
                    return false;

                // getter exists and is manually implemented.
                return null;
            }
        }

        internal bool InfersNullableAnnotation
        {
            get
            {
                if (FlowAnalysisAnnotations != FlowAnalysisAnnotations.None)
                {
                    return false;
                }

                var propertyType = _property.TypeWithAnnotations;
                if (propertyType.NullableAnnotation != NullableAnnotation.NotAnnotated
                    || !_property.UsesFieldKeyword)
                {
                    return false;
                }

                return true;
            }
        }

        // TODO2: thread safe
        // TODO2: need to look for a *difference* in nullable diagnostics when annotated versus not.
        // e.g. non-annotated version contains a *nullable* diagnostic which is missing in the annotated version.
        // Also, do not double bind.
        // Therefore this probably should be occurring when we are compiling methods within a single type.
        // e.g. if we are compiling+nullable analyzing a constructor, we should have a way to say:
        // hold on, first, let's do the getters which need null resilience analysis.
        private NullableAnnotation? _inferredNullableAnnotation;
        internal NullableAnnotation GetInferredNullableAnnotation() => _inferredNullableAnnotation ??= ComputeInferredNullableAnnotation();

        private NullableAnnotation ComputeInferredNullableAnnotation()
        {
            Debug.Assert(InfersNullableAnnotation);
            if (IsGetterTriviallyNullResilient is { } isNullResilient)
                return isNullResilient ? NullableAnnotation.Annotated : NullableAnnotation.NotAnnotated;

            var getAccessor = (SourcePropertyAccessorSymbol?)_property.GetMethod;
            Debug.Assert(getAccessor is not null);
            getAccessor = (SourcePropertyAccessorSymbol?)getAccessor.PartialImplementationPart ?? getAccessor;

            var binder = getAccessor.TryGetBodyBinder() ?? throw ExceptionUtilities.UnexpectedValue(getAccessor);
            var boundGetAccessor = binder.BindMethodBody(getAccessor.SyntaxNode, BindingDiagnosticBag.Discarded);

            var annotatedDiagnostics = DiagnosticBag.GetInstance();
            NullableWalker.AnalyzeIfNeeded(binder, boundGetAccessor, boundGetAccessor.Syntax, annotatedDiagnostics, getterNullResilienceData: (getAccessor, _property.BackingField, NullableAnnotation.Annotated));

            if (annotatedDiagnostics.IsEmptyWithoutResolution)
            {
                // No nullable warnings occur at all when we treat the field as nullable.
                annotatedDiagnostics.Free();
                return NullableAnnotation.Annotated;
            }

            var notAnnotatedDiagnostics = DiagnosticBag.GetInstance();
            NullableWalker.AnalyzeIfNeeded(binder, boundGetAccessor, boundGetAccessor.Syntax, notAnnotatedDiagnostics, getterNullResilienceData: (getAccessor, _property.BackingField, NullableAnnotation.NotAnnotated));
            var notAnnotatedDiagnosticsSet = new HashSet<Diagnostic>(notAnnotatedDiagnostics.AsEnumerable(), SameDiagnosticComparer.Instance);
            notAnnotatedDiagnostics.Free();

            foreach (var diagnostic in annotatedDiagnostics.AsEnumerable())
            {
                if (!notAnnotatedDiagnosticsSet.Contains(diagnostic))
                {
                    // Treating the field as non-nullable removes a nullable warning compared to treating it as annotated.
                    annotatedDiagnostics.Free();
                    return NullableAnnotation.NotAnnotated;
                }
            }

            annotatedDiagnostics.Free();
            return NullableAnnotation.Annotated;
        }
#nullable disable

        // TODO2: dedupe
        private sealed class SameDiagnosticComparer : EqualityComparer<Diagnostic>
        {
            public static readonly SameDiagnosticComparer Instance = new SameDiagnosticComparer();
            public override bool Equals(Diagnostic x, Diagnostic y) => x.Equals(y);
            public override int GetHashCode(Diagnostic obj) =>
                Hash.Combine(Hash.CombineValues(obj.Arguments), Hash.Combine(obj.Location.GetHashCode(), obj.Code));
        }

        internal override bool HasPointerType
            => _property.HasPointerType;

        protected sealed override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);
            Debug.Assert(arguments.Diagnostics is BindingDiagnosticBag);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(AttributeDescription.FixedBufferAttribute))
            {
                // error CS8362: Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute on property
                ((BindingDiagnosticBag)arguments.Diagnostics).Add(ErrorCode.ERR_DoNotUseFixedBufferAttrOnProperty, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else
            {
                base.DecodeWellKnownAttributeImpl(ref arguments);
            }
        }

        public override Symbol ContainingSymbol
            => _property.ContainingSymbol;

        public override NamedTypeSymbol ContainingType
            => _property.ContainingType;

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);

            if (!allAttributeSyntaxNodes.IsEmpty && _property.IsAutoPropertyOrUsesFieldKeyword)
            {
                CheckForFieldTargetedAttribute(diagnostics);
            }
        }

        private void CheckForFieldTargetedAttribute(BindingDiagnosticBag diagnostics)
        {
            var languageVersion = this.DeclaringCompilation.LanguageVersion;
            if (languageVersion.AllowAttributesOnBackingFields())
            {
                return;
            }

            foreach (var attributeList in GetAttributeDeclarations())
            {
                foreach (var attribute in attributeList)
                {
                    if (attribute.Target?.GetAttributeLocation() == AttributeLocation.Field)
                    {
                        diagnostics.Add(
                            new CSDiagnosticInfo(ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable,
                                languageVersion.ToDisplayString(),
                                new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureAttributesOnBackingFields.RequiredVersion())),
                            attribute.Target.Location);
                    }
                }
            }
        }
    }
}
