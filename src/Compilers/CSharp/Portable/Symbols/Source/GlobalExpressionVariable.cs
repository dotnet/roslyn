// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents expression and deconstruction variables declared in a global statement.
    /// </summary>
    internal class GlobalExpressionVariable : SourceMemberFieldSymbol
    {
        private TypeWithAnnotations.Boxed _lazyType;

        /// <summary>
        /// The type syntax, if any, from source. Optional for patterns that can omit an explicit type.
        /// </summary>
        private readonly SyntaxReference _typeSyntaxOpt;

        internal GlobalExpressionVariable(
            SourceMemberContainerTypeSymbol containingType,
            DeclarationModifiers modifiers,
            TypeSyntax typeSyntax,
            string name,
            SyntaxReference syntax,
            TextSpan locationSpan)
            : base(containingType, modifiers, name, syntax, locationSpan)
        {
            Debug.Assert(DeclaredAccessibility == Accessibility.Private);
            _typeSyntaxOpt = typeSyntax?.GetReference();
        }

        internal static GlobalExpressionVariable Create(
                SourceMemberContainerTypeSymbol containingType,
                DeclarationModifiers modifiers,
                TypeSyntax typeSyntax,
                string name,
                SyntaxNode syntax,
                TextSpan locationSpan,
                FieldSymbol containingFieldOpt,
                SyntaxNode nodeToBind)
        {
            Debug.Assert(nodeToBind.Kind() == SyntaxKind.VariableDeclarator || nodeToBind is ExpressionSyntax);

            var syntaxReference = syntax.GetReference();
            return (typeSyntax == null || typeSyntax.SkipScoped(out _).SkipRef().IsVar)
                ? new InferrableGlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, locationSpan, containingFieldOpt, nodeToBind)
                : new GlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, locationSpan);
        }

        protected override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations() => OneOrMany<SyntaxList<AttributeListSyntax>>.Empty;
        protected override TypeSyntax TypeSyntax => (TypeSyntax)_typeSyntaxOpt?.GetSyntax();
        protected override SyntaxTokenList ModifiersTokenList => default(SyntaxTokenList);
        public override bool HasInitializer => false;
        protected override ConstantValue MakeConstantValue(
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies,
            bool earlyDecodingWellKnownAttributes,
            BindingDiagnosticBag diagnostics) => null;

        public sealed override RefKind RefKind => RefKind.None;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert(fieldsBeingBound != null);

            if (_lazyType != null)
            {
                return _lazyType.Value;
            }

            var typeSyntax = TypeSyntax;

            var compilation = this.DeclaringCompilation;

            var diagnostics = BindingDiagnosticBag.GetInstance();
            TypeWithAnnotations type;
            bool isVar;

            var binderFactory = compilation.GetBinderFactory(SyntaxTree);
            var binder = binderFactory.GetBinder(typeSyntax ?? SyntaxNode);

            if (typeSyntax != null)
            {
                type = binder.BindTypeOrVarKeyword(typeSyntax.SkipScoped(out _).SkipRef(), diagnostics, out isVar);
            }
            else
            {
                // Recursive patterns may omit the type syntax
                isVar = true;
                type = default;
            }

            Debug.Assert(type.HasType || isVar);

            if (isVar && !fieldsBeingBound.ContainsReference(this))
            {
                InferFieldType(fieldsBeingBound, binder);
                Debug.Assert(_lazyType != null);
            }
            else
            {
                if (isVar)
                {
                    diagnostics.Add(ErrorCode.ERR_RecursivelyTypedVariable, this.ErrorLocation, this);
                    type = TypeWithAnnotations.Create(binder.CreateErrorType("var"));
                }

                SetType(diagnostics, type);
            }

            diagnostics.Free();
            return _lazyType.Value;
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>. 
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        private TypeWithAnnotations SetType(BindingDiagnosticBag diagnostics, TypeWithAnnotations type)
        {
            var originalType = _lazyType?.Value.DefaultType;

            // In the event that we race to set the type of a field, we should
            // always deduce the same type, unless the cached type is an error.

            Debug.Assert((object)originalType == null ||
                originalType.IsErrorType() ||
                TypeSymbol.Equals(originalType, type.Type, TypeCompareKind.ConsiderEverything2));

            if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null) == null)
            {
                TypeChecks(type.Type, diagnostics);

                AddDeclarationDiagnostics(diagnostics);
                state.NotePartComplete(CompletionPart.Type);
            }
            return _lazyType.Value;
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>.
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        internal TypeWithAnnotations SetTypeWithAnnotations(TypeWithAnnotations type, BindingDiagnosticBag diagnostics)
        {
            return SetType(diagnostics, type);
        }

        protected virtual void InferFieldType(ConsList<FieldSymbol> fieldsBeingBound, Binder binder)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private class InferrableGlobalExpressionVariable : GlobalExpressionVariable
        {
            private readonly FieldSymbol _containingFieldOpt;
            private readonly SyntaxReference _nodeToBind;

            internal InferrableGlobalExpressionVariable(
                SourceMemberContainerTypeSymbol containingType,
                DeclarationModifiers modifiers,
                TypeSyntax typeSyntax,
                string name,
                SyntaxReference syntax,
                TextSpan locationSpan,
                FieldSymbol containingFieldOpt,
                SyntaxNode nodeToBind)
                : base(containingType, modifiers, typeSyntax, name, syntax, locationSpan)
            {
                Debug.Assert(nodeToBind.Kind() == SyntaxKind.VariableDeclarator || nodeToBind is ExpressionSyntax);

                _containingFieldOpt = containingFieldOpt;
                _nodeToBind = nodeToBind.GetReference();
            }

            protected override void InferFieldType(ConsList<FieldSymbol> fieldsBeingBound, Binder binder)
            {
                var nodeToBind = _nodeToBind.GetSyntax();

                if ((object)_containingFieldOpt != null && nodeToBind.Kind() != SyntaxKind.VariableDeclarator)
                {
                    binder = binder.WithContainingMemberOrLambda(_containingFieldOpt).WithAdditionalFlags(BinderFlags.FieldInitializer);
                }

                fieldsBeingBound = new ConsList<FieldSymbol>(this, fieldsBeingBound);

                binder = new ImplicitlyTypedFieldBinder(binder, fieldsBeingBound);

                switch (nodeToBind.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                        // This occurs, for example, in
                        // int x, y[out var Z, 1 is int I];
                        // for (int x, y[out var Z, 1 is int I]; ;) {}
                        binder.BindDeclaratorArguments((VariableDeclaratorSyntax)nodeToBind, BindingDiagnosticBag.Discarded);
                        break;

                    default:
                        binder.BindExpression((ExpressionSyntax)nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                }
            }
        }
    }
}
