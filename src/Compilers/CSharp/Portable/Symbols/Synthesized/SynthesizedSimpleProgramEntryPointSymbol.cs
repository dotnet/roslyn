// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSimpleProgramEntryPointSymbol : SourceMemberMethodSymbol
    {
        /// <summary>
        /// A synthetic syntax tree associated with this method. An empty <see cref="CompilationUnitSyntax"/>
        /// is the root of this tree. In several places in the compiler (like for example in 
        /// <see cref="MethodCompiler.BindMethodBody(MethodSymbol, TypeCompilationState, DiagnosticBag, out ImportChain, out bool, out MethodBodySemanticModel.InitialState)"/>)
        /// we expect that a <see cref="SourceMemberMethodSymbol"/> that contains user code is assosiated with 
        /// a single syntax node. We are using the empty <see cref="CompilationUnitSyntax"/> for this purpose.
        /// Also, since multiple compilation units contribute locals and local functions to a single scope, we
        /// need a single syntax node that can be used as a <see cref="Binder.ScopeDesignator"/> for it without
        /// creating an ambiguity.
        /// Another dependency on a presence of a single syntax node is a <see cref="ExecutableCodeBinder"/> that is
        /// now also responsible for building binders for the compound simple program body.
        /// 
        /// There is one-to-one correspondence between an instance of <see cref="SynthesizedSimpleProgramEntryPointSymbol"/> 
        /// and the tree. Even though the <see cref="CompilationUnitSyntax"/> node it empty, logically it represents a
        /// compound simple program body, the logical children are all real <see cref="CompilationUnitSyntax"/> nodes with
        /// top-level statements in the compilation.
        /// </summary>
        private readonly CSharpSyntaxTree _syntheticSyntaxTree = new CSharpSyntaxTree.DummySyntaxTree();

        /// <summary>
        /// The first syntax tree with a top-level statement that is not a <see cref="LocalFunctionStatementSyntax"/>.
        /// That is the tree that comes first in the logical method body. It is possible to have no tree like that. 
        /// </summary>
        internal readonly SyntaxTree? PrimarySyntaxTree;

        private readonly TypeSymbol _returnType;
        private WeakReference<ExecutableCodeBinder>? _weakBodyBinder;
        private MemberSemanticModel.SimpleProgramBodySemanticModelMergedBoundNodeCache? _lazyMergedBoundNodeCache;

        internal SynthesizedSimpleProgramEntryPointSymbol(SimpleProgramNamedTypeSymbol containingType, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(containingType, syntaxReferenceOpt: null, containingType.Locations, isIterator: declaration.IsIterator)
        {
            bool hasAwait = declaration.HasAwaitExpressions;

            foreach (var singleDecl in declaration.Declarations)
            {
                if (!singleDecl.AllTopLevelStatementsLocalFunctions)
                {
                    PrimarySyntaxTree = singleDecl.SyntaxReference.SyntaxTree;
                    break;
                }
            }

            // PROTOTYPE(SimplePrograms): report statements out of order, i.e. after namespace/type declarations, etc.
            if (hasAwait)
            {
                _returnType = Binder.GetWellKnownType(containingType.DeclaringCompilation, WellKnownType.System_Threading_Tasks_Task, diagnostics, NoLocation.Singleton);
            }
            else
            {
                _returnType = Binder.GetSpecialType(containingType.DeclaringCompilation, SpecialType.System_Void, NoLocation.Singleton, diagnostics);
            }

            this.MakeFlags(
                MethodKind.Ordinary,
                DeclarationModifiers.Static | DeclarationModifiers.Private | (hasAwait ? DeclarationModifiers.Async : DeclarationModifiers.None),
                returnsVoid: !hasAwait,
                isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: false);
        }

        public override string Name
        {
            get
            {
                return "$Main";
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        public override bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.Private;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return LexicalSortKey.NotInSource;
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ContainingType.DeclaringSyntaxReferences;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingType.Locations;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return RefKind.None;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return TypeWithAnnotations.Create(_returnType);
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get
            {
                return true;
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return localPosition;
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            SyntaxTree? primarySyntaxTree = PrimarySyntaxTree;

            if (primarySyntaxTree is object)
            {
                var type = (SimpleProgramNamedTypeSymbol)ContainingSymbol;

                foreach (var singleDecl in type.MergedDeclaration.Declarations)
                {
                    if (!singleDecl.AllTopLevelStatementsLocalFunctions)
                    {
                        var syntaxTree = singleDecl.SyntaxReference.SyntaxTree;
                        if (syntaxTree != primarySyntaxTree)
                        {
                            foreach (var statement in ((CompilationUnitSyntax)syntaxTree.GetRoot()).Members)
                            {
                                if (statement is GlobalStatementSyntax topLevelStatement && !topLevelStatement.Statement.IsKind(SyntaxKind.LocalFunctionStatement))
                                {
                                    Binder.Error(diagnostics, ErrorCode.ERR_SimpleProgramMultipleUnitsWithExecutableStatements, topLevelStatement.Statement.GetFirstToken());
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal override bool IsExpressionBodied => false;

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

        protected override object MethodChecksLockObject => _syntheticSyntaxTree;

        internal override CSharpSyntaxNode SyntaxNode
        {
            get
            {
                return (CSharpSyntaxNode)_syntheticSyntaxTree.GetRoot();
            }
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, BinderFlags additionalFlags = BinderFlags.None)
        {
            // PROTOTYPE(SimplePrograms): Respect additional flags passed in by SemanticModel
            return GetBodyBinder();
        }

        private ExecutableCodeBinder CreateBodyBinder()
        {
            CSharpCompilation compilation = DeclaringCompilation;

            Binder result = new BuckStopsHereBinder(compilation);
            result = new InContainerBinder(compilation.GlobalNamespace, result, SyntaxNode, inUsing: false);
            result = new InContainerBinder(ContainingType, result);
            result = new InMethodBinder(this, result);
            return new ExecutableCodeBinder(SyntaxNode, this, result);
        }

        internal ExecutableCodeBinder GetBodyBinder()
        {
            while (true)
            {
                var previousWeakReference = _weakBodyBinder;
                if (previousWeakReference != null && previousWeakReference.TryGetTarget(out ExecutableCodeBinder? previousBinder))
                {
                    return previousBinder;
                }

                ExecutableCodeBinder newBinder = CreateBodyBinder();
                if (Interlocked.CompareExchange(ref _weakBodyBinder, new WeakReference<ExecutableCodeBinder>(newBinder), previousWeakReference) == previousWeakReference)
                {
                    return newBinder;
                }
            }
        }

        internal MemberSemanticModel.SimpleProgramBodySemanticModelMergedBoundNodeCache? GetSemanticModelMergedBoundNodeCache(Binder rootBinder)
        {
            var binder = (SimpleProgramBinder)rootBinder.GetBinder(SyntaxNode)!;
            Debug.Assert(binder == _weakBodyBinder?.GetTarget()?.GetBinder(SyntaxNode));

            if (((SimpleProgramNamedTypeSymbol)ContainingSymbol).MergedDeclaration.Declarations.Length == 1)
            {
                // No need to merge
                return null;
            }

            var oldCache = _lazyMergedBoundNodeCache;

            if (oldCache is object && oldCache.WeakBodyBinder.GetTarget() == binder)
            {
                return oldCache;
            }

            Debug.Assert(oldCache is null || oldCache.WeakBodyBinder.GetTarget() == null);

            var newCache = new MemberSemanticModel.SimpleProgramBodySemanticModelMergedBoundNodeCache(binder);

            var original = Interlocked.CompareExchange(ref _lazyMergedBoundNodeCache, newCache, oldCache);

            if (original == oldCache)
            {
                return newCache;
            }

            RoslynDebug.Assert(original is object);
            RoslynDebug.Assert(original.WeakBodyBinder.GetTarget() == binder);
            return original;
        }

        /// <summary>
        /// Returns the set of children for the synthesized entry point body.
        /// If there is a <see cref="PrimarySyntaxTree"/>, a <see cref="CompilationUnitSyntax"/>
        /// from it is returned first. The order of remaining units matches the order 
        /// of syntax referemces in <see cref="MergedTypeDeclaration"/> for the simple program type.
        /// </summary>
        internal IEnumerable<CompilationUnitSyntax> GetUnits()
        {
            SyntaxTree? primarySyntaxTree = PrimarySyntaxTree;

            if (primarySyntaxTree is object)
            {
                yield return (CompilationUnitSyntax)primarySyntaxTree.GetRoot();
            }

            var type = (SimpleProgramNamedTypeSymbol)ContainingSymbol;

            foreach (var singleDecl in type.MergedDeclaration.Declarations)
            {
                var unit = (CompilationUnitSyntax)singleDecl.SyntaxReference.SyntaxTree.GetRoot();
                if (unit.SyntaxTree != primarySyntaxTree)
                {
                    yield return unit;
                }
            }
        }

        internal override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken)
        {
            return ContainingSymbol.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken);
        }
    }
}
