// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods synthesized by compiler for records.
    /// </summary>
    internal abstract class SynthesizedRecordOrdinaryMethod : SourceOrdinaryMethodSymbolBase
    {
        private readonly int _memberOffset;

        protected SynthesizedRecordOrdinaryMethod(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, DeclarationModifiers declarationModifiers)
            : base(containingType, name, containingType.GetFirstLocation(), (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                   isIterator: false,
                   (declarationModifiers, MakeFlags(
                                                    MethodKind.Ordinary, RefKind.None, declarationModifiers, returnsVoid: false, returnsVoidIsSet: false,
                                                    isExpressionBodied: false, isExtensionMethod: false, isNullableAnalysisEnabled: false, isVarArg: false,
                                                    isExplicitInterfaceImplementation: false, hasThisInitializer: false)))
        {
            _memberOffset = memberOffset;
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

        internal sealed override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

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

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            Debug.Assert(IsImplicitlyDeclared);
            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
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
