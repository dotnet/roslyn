// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods synthesized by compiler and added to the <see cref="NamedTypeSymbol.GetMembers()"/> result.
    /// </summary>
    internal abstract class SynthesizedSourceOrdinaryMethodSymbol : SourceOrdinaryMethodSymbolBase
    {
        protected SynthesizedSourceOrdinaryMethodSymbol(SourceMemberContainerTypeSymbol containingType, string name, Location location, CSharpSyntaxNode syntax, (DeclarationModifiers declarationModifiers, Flags flags) modifiersAndFlags)
            : base(containingType, name, location, syntax, isIterator: false, modifiersAndFlags)
        {
        }

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(Arity == 0);
            var (returnType, parameters) = MakeParametersAndBindReturnType(diagnostics);
            MethodChecks(returnType, parameters, diagnostics);
        }

        protected abstract (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics);

        public sealed override bool IsImplicitlyDeclared => true;

        protected sealed override Location ReturnTypeLocation => GetFirstLocation();

        protected sealed override MethodSymbol? FindExplicitlyImplementedMethod(BindingDiagnosticBag diagnostics) => null;

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes() => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds() => ImmutableArray<TypeParameterConstraintKind>.Empty;

        protected sealed override void PartialMethodChecks(BindingDiagnosticBag diagnostics)
        {
        }

        protected sealed override void ExtensionMethodChecks(BindingDiagnosticBag diagnostics)
        {
        }

        protected sealed override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
        }

        protected sealed override TypeSymbol? ExplicitInterfaceType => null;

        protected sealed override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
        }

        protected sealed override SourceMemberMethodSymbol? BoundAttributesSource => null;

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));

        public sealed override string? GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default) => null;

        internal sealed override bool GenerateDebugInfo => false;

        internal sealed override bool SynthesizesLoweredBoundBody => true;
        internal sealed override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();
        internal abstract override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics);
    }
}
