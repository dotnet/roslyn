// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Makes the System.Runtime.CompilerServices.NonNullTypesAttribute available in every compilation.
    /// </summary>
    internal sealed class InjectedNonNullTypesAttributeSymbol : InjectedAttributeSymbol
    {
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;
        private SymbolCompletionState _state;

        private InjectedNonNullTypesAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors,
            DiagnosticBag diagnostics)
            : base(description, containingNamespace, compilation, getConstructors, diagnostics)
        {
        }

        public static InjectedNonNullTypesAttributeSymbol Create(NamespaceSymbol containingNamespace)
        {
            return new InjectedNonNullTypesAttributeSymbol(AttributeDescription.NonNullTypesAttribute, containingNamespace, containingNamespace.DeclaringCompilation, makeNonNullTypesAttributeConstructor, new DiagnosticBag());

            ImmutableArray<MethodSymbol> makeNonNullTypesAttributeConstructor(CSharpCompilation compilation, NamedTypeSymbol containingType, DiagnosticBag diagnostics)
            {
                var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

                Binder.ReportUseSiteDiagnostics(boolType, diagnostics, Location.None);

                var boolWithAnnotations = TypeSymbolWithAnnotations.Create(boolType);
                // PROTOTYPE constructor should save the parameter into a field (for users of reflection)
                return ImmutableArray.Create<MethodSymbol>(
                    new NonNullTypesAttributeConstructorSymbol(
                        containingType,
                        m => ImmutableArray.Create(SynthesizedParameterSymbol.Create(m, boolWithAnnotations, 0, ConstantValue.True, name: "flag"))));
            }
        }

        internal override bool RequiresCompletion
            => true;

        internal override bool HasComplete(CompletionPart part)
            => _state.HasComplete(part);

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.InjectedSymbolAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            var attributes = _lazyCustomAttributes;
            if (!attributes.IsDefault)
            {
                return attributes;
            }

            if (ImmutableInterlocked.InterlockedInitialize(ref _lazyCustomAttributes, MakeAttributes()))
            {
                var completed = _state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(completed);
            }

            return _lazyCustomAttributes;
        }

        /// <summary>
        /// Adds an `[AttributeUsage(AttributeTargets.Class | ...)]` (if possible) and captures any diagnostics in the process.
        /// </summary>
        private ImmutableArray<CSharpAttributeData> MakeAttributes()
        {
            var ctor = (MethodSymbol)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_AttributeUsageAttribute__ctor);
            if (ctor is null)
            {
                // member is missing
                var memberDescriptor = WellKnownMembers.GetDescriptor(WellKnownMember.System_AttributeUsageAttribute__ctor);
                var diagnosticInfo = new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name);
                Diagnostics.Add(diagnosticInfo, Location.None);

                return ImmutableArray<CSharpAttributeData>.Empty;
            }

            Binder.ReportUseSiteDiagnostics(ctor, Diagnostics, Location.None);

            NamedTypeSymbol attributeTargets = DeclaringCompilation.GetWellKnownType(WellKnownType.System_AttributeTargets);
            Binder.ReportUseSiteDiagnostics(attributeTargets, Diagnostics, Location.None);

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
                Debug.Assert(containingType is InjectedAttributeSymbol);
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
                var containingType = (InjectedAttributeSymbol)ContainingType;
                GenerateMethodBodyCore(compilationState, containingType.Diagnostics);
            }
        }
    }
}
