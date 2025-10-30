// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
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
        private int _inferredNullableAnnotation = (int)NullableAnnotation.Ignored;
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

        /// <summary>
        /// Gets the inferred nullable annotation of the backing field,
        /// potentially binding and nullable-analyzing the associated get accessor.
        /// </summary>
        /// <remarks>
        /// The <see cref="FieldSymbol.TypeWithAnnotations"/> for this symbol does not expose this inferred nullable annotation.
        /// For that API, the nullable annotation of the associated property is used instead.
        /// </remarks>
        internal NullableAnnotation GetInferredNullableAnnotation()
        {
            if (_inferredNullableAnnotation == (int)NullableAnnotation.Ignored)
            {
                var inferredAnnotation = ComputeInferredNullableAnnotation();
                Debug.Assert(inferredAnnotation is not NullableAnnotation.Ignored);
                Interlocked.CompareExchange(ref _inferredNullableAnnotation, (int)inferredAnnotation, (int)NullableAnnotation.Ignored);
            }
            Debug.Assert((NullableAnnotation)_inferredNullableAnnotation is NullableAnnotation.NotAnnotated or NullableAnnotation.Annotated);
            return (NullableAnnotation)_inferredNullableAnnotation;
        }

        private NullableAnnotation ComputeInferredNullableAnnotation()
        {
            // https://github.com/dotnet/csharplang/blob/94205582d0f5c73e5765cb5888311c2f14890b95/proposals/field-keyword.md#nullability-of-the-backing-field
            Debug.Assert(InfersNullableAnnotation);

            // If the property does not have a get accessor, it is (vacuously) null-resilient.
            if (_property.GetMethod is not SourcePropertyAccessorSymbol getAccessor)
            {
                Debug.Assert(_property.GetMethod is null);
                return NullableAnnotation.Annotated;
            }

            getAccessor = (SourcePropertyAccessorSymbol?)getAccessor.PartialImplementationPart ?? getAccessor;
            // If the get accessor is auto-implemented, the property is not null-resilient.
            if (getAccessor.IsAutoPropertyAccessor)
                return NullableAnnotation.NotAnnotated;

            var binder = getAccessor.TryGetBodyBinder() ?? throw ExceptionUtilities.UnexpectedValue(getAccessor);
            var boundGetAccessor = binder.BindMethodBody(getAccessor.SyntaxNode, BindingDiagnosticBag.Discarded);

            var annotatedDiagnostics = nullableAnalyzeAndFilterDiagnostics(assumedNullableAnnotation: NullableAnnotation.Annotated);
            if (annotatedDiagnostics.IsEmptyWithoutResolution)
            {
                // If the pass where the field was annotated results in no diagnostics at all, then the property is null-resilient and the not-annotated pass can be skipped.
                annotatedDiagnostics.Free();
                return NullableAnnotation.Annotated;
            }

            var notAnnotatedDiagnostics = nullableAnalyzeAndFilterDiagnostics(assumedNullableAnnotation: NullableAnnotation.NotAnnotated);
            if (notAnnotatedDiagnostics.IsEmptyWithoutResolution)
            {
                // annotated pass had diagnostics, and not-annotated pass had no diagnostics.
                annotatedDiagnostics.Free();
                notAnnotatedDiagnostics.Free();
                return NullableAnnotation.NotAnnotated;
            }

            // Both annotated and not-annotated cases had nullable warnings.
            var notAnnotatedDiagnosticsSet = new HashSet<Diagnostic>(notAnnotatedDiagnostics.AsEnumerable(), SameDiagnosticComparer.Instance);
            notAnnotatedDiagnostics.Free();

            foreach (var diagnostic in annotatedDiagnostics.AsEnumerable())
            {
                if (!notAnnotatedDiagnosticsSet.Contains(diagnostic))
                {
                    // There is a nullable diagnostic in the pass where the field was *annotated*,
                    // which was not present in the pass where the field is *not-annotated*. The property is not null-resilient.
                    annotatedDiagnostics.Free();
                    return NullableAnnotation.NotAnnotated;
                }
            }

            annotatedDiagnostics.Free();
            return NullableAnnotation.Annotated;

            DiagnosticBag nullableAnalyzeAndFilterDiagnostics(NullableAnnotation assumedNullableAnnotation)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                NullableWalker.AnalyzeIfNeeded(binder, boundGetAccessor, boundGetAccessor.Syntax, diagnostics, symbolAndGetterNullResilienceData: (getAccessor, new NullableWalker.GetterNullResilienceData(_property.BackingField, assumedNullableAnnotation)));
                if (diagnostics.IsEmptyWithoutResolution)
                {
                    return diagnostics;
                }

                var filteredDiagnostics = DiagnosticBag.GetInstance();
                _ = DeclaringCompilation.FilterAndAppendAndFreeDiagnostics(filteredDiagnostics, ref diagnostics, cancellationToken: default);
                return filteredDiagnostics;
            }
        }
#nullable disable

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
