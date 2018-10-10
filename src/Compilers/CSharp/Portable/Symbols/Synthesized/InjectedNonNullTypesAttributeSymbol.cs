// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Makes the System.Runtime.CompilerServices.NonNullTypesAttribute available in every compilation.
    /// </summary>
    internal sealed class InjectedNonNullTypesAttributeSymbol : InjectedAttributeSymbol
    {
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private InjectedNonNullTypesAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors)
            : base(description, containingNamespace, compilation, getConstructors)
        {
        }

        public static InjectedNonNullTypesAttributeSymbol Create(NamespaceSymbol containingNamespace)
        {
            return new InjectedNonNullTypesAttributeSymbol(AttributeDescription.NonNullTypesAttribute, containingNamespace, containingNamespace.DeclaringCompilation, makeConstructor);

            ImmutableArray<MethodSymbol> makeConstructor(CSharpCompilation compilation, NamedTypeSymbol containingType, DiagnosticBag diagnostics)
            {
                var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

                Binder.ReportUseSiteDiagnostics(boolType, diagnostics, Location.None);

                var boolWithAnnotations = TypeSymbolWithAnnotations.Create(boolType);
                // https://github.com/dotnet/roslyn/issues/30143: Constructor should save the parameter into a field (for users of reflection)
                return ImmutableArray.Create<MethodSymbol>(
                    new NonNullTypesAttributeConstructorSymbol(
                        containingType,
                        m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, boolWithAnnotations, 0, ConstantValue.True, name: "flag"))));
            }
        }

        internal override void AddDiagnostics(DiagnosticBag recipient)
        {
            // pull on the attributes to collect their diagnostics too
            _ = GetAttributes();
            recipient.AddRange(_diagnostics);
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                // https://github.com/dotnet/roslyn/issues/29732 A race condition can produce duplicate diagnostics here
                ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, MakeAttributes());
            }
            return _lazyCustomAttributes;
        }

        /// <summary>
        /// Adds an `[AttributeUsage(AttributeTargets.Class | ...)]` (if possible) and captures any diagnostics in the process.
        /// </summary>
        private ImmutableArray<CSharpAttributeData> MakeAttributes()
        {
            var ctor = (MethodSymbol)Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_AttributeUsageAttribute__ctor, _diagnostics, Location.None);
            if (ctor is null)
            {
                // member is missing
                return ImmutableArray<CSharpAttributeData>.Empty;
            }

            NamedTypeSymbol attributeTargets = DeclaringCompilation.GetWellKnownType(WellKnownType.System_AttributeTargets);
            Binder.ReportUseSiteDiagnostics(attributeTargets, _diagnostics, Location.None);

            var usage = new SynthesizedAttributeData(ctor,
                arguments: ImmutableArray.Create(new TypedConstant(attributeTargets, TypedConstantKind.Enum, attributeUsage)),
                namedArguments: ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);

            return ImmutableArray.Create<CSharpAttributeData>(usage);
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
            => new AttributeUsageInfo(validTargets: attributeUsage, allowMultiple: false, inherited: false);

        private const AttributeTargets attributeUsage =
            AttributeTargets.Class |
            AttributeTargets.Constructor |
            AttributeTargets.Delegate |
            AttributeTargets.Enum |
            AttributeTargets.Event |
            AttributeTargets.Field |
            AttributeTargets.Interface |
            AttributeTargets.Method |
            AttributeTargets.Module |
            AttributeTargets.Property |
            AttributeTargets.Struct;

        private sealed class NonNullTypesAttributeConstructorSymbol : SynthesizedInstanceConstructor
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;

            internal NonNullTypesAttributeConstructorSymbol(
                NamedTypeSymbol containingType,
                Func<MethodSymbol, ImmutableArray<ParameterSymbol>> getParameters) :
                base(containingType)
            {
                Debug.Assert(containingType is InjectedNonNullTypesAttributeSymbol);
                _parameters = getParameters(this);
            }

            public override ImmutableArray<ParameterSymbol> Parameters
                => _parameters;

            internal override bool SynthesizesLoweredBoundBody
                => true;

            /// <summary>
            /// Note: this method captures diagnostics into the containing type (an injected attribute symbol) instead,
            /// as we don't yet know if the containing type will be emitted.
            /// </summary>
            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                var containingType = (InjectedNonNullTypesAttributeSymbol)ContainingType;
                GenerateMethodBodyCore(compilationState, containingType._diagnostics);
            }
        }
    }
}
