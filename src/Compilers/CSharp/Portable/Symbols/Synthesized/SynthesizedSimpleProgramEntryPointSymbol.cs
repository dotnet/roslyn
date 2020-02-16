// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSimpleProgramEntryPointSymbol : SourceMemberMethodSymbol
    {
        private readonly TypeSymbol _returnType;
        internal readonly CSharpSyntaxTree FakeSyntaxTree = new CSharpSyntaxTree.DummySyntaxTree();

        internal SynthesizedSimpleProgramEntryPointSymbol(SimpleProgramNamedTypeSymbol containingType, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(containingType, syntaxReferenceOpt: null, containingType.Locations, isIterator: declaration.IsIterator)
        {
            bool hasAwait = declaration.HasAwaitExpressions;

            // PROTOTYPE(SimplePrograms): report statements out of order
            // PROTOTYPE(SimplePrograms): report executable statements in multiple files
            // PROTOTYPE(SimplePrograms): Support the case of multiple files
            if (declaration.Declarations.Length > 1)
            {
                diagnostics.Add(ErrorCode.ERR_InternalError, declaration.Declarations[1].SyntaxReference.GetLocation());
            }

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
        }

        internal override bool IsExpressionBodied => false;

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

        protected override object MethodChecksLockObject => FakeSyntaxTree;

        internal override CSharpSyntaxNode SyntaxNode
        {
            get
            {
                return (CSharpSyntaxNode)FakeSyntaxTree.GetRoot();
            }
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, BinderFlags additionalFlags = BinderFlags.None)
        {
            // PROTOTYPE(SimplePrograms): Respect additional flags passed in by SemanticModel
            var syntaxReference = ((SimpleProgramNamedTypeSymbol)ContainingSymbol).MergedDeclaration.Declarations[0].SyntaxReference;
            Binder result = (binderFactoryOpt ?? this.DeclaringCompilation.GetBinderFactory(syntaxReference.SyntaxTree)).GetBinder(syntaxReference.GetSyntax());
            ExecutableCodeBinder? executableBinder;
            while ((executableBinder = result as ExecutableCodeBinder) is null)
            {
                result = result.Next!;
            }

            Debug.Assert(executableBinder.MemberSymbol == (object)this);
            return executableBinder;
        }
    }
}
