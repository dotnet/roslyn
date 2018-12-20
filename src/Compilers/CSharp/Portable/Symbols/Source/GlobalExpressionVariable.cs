// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents expression and deconstruction variables declared in a global statement.
    /// </summary>
    internal class GlobalExpressionVariable : SourceMemberFieldSymbol
    {
        private TypeSymbolWithAnnotations.Builder _lazyType;
        private SyntaxReference _typeSyntax;

        internal GlobalExpressionVariable(
            SourceMemberContainerTypeSymbol containingType,
            DeclarationModifiers modifiers,
            TypeSyntax typeSyntax,
            string name,
            SyntaxReference syntax,
            Location location)
            : base(containingType, modifiers, name, syntax, location)
        {
            Debug.Assert(DeclaredAccessibility == Accessibility.Private);
            _typeSyntax = typeSyntax.GetReference();
        }

        internal static GlobalExpressionVariable Create(
                SourceMemberContainerTypeSymbol containingType,
                DeclarationModifiers modifiers,
                TypeSyntax typeSyntax,
                string name,
                SyntaxNode syntax,
                Location location,
                FieldSymbol containingFieldOpt,
                SyntaxNode nodeToBind)
        {
            Debug.Assert(nodeToBind.Kind() == SyntaxKind.VariableDeclarator || nodeToBind is ExpressionSyntax);

            var syntaxReference = syntax.GetReference();
            return typeSyntax.IsVar
                ? new InferrableGlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, location, containingFieldOpt, nodeToBind)
                : new GlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, location);
        }


        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList => default(SyntaxList<AttributeListSyntax>);
        protected override TypeSyntax TypeSyntax => (TypeSyntax)_typeSyntax.GetSyntax();
        protected override SyntaxTokenList ModifiersTokenList => default(SyntaxTokenList);
        public override bool HasInitializer => false;
        protected override ConstantValue MakeConstantValue(
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies,
            bool earlyDecodingWellKnownAttributes,
            DiagnosticBag diagnostics) => null;

        internal override TypeSymbolWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            Debug.Assert(fieldsBeingBound != null);

            if (!_lazyType.IsNull)
            {
                return _lazyType.ToType();
            }

            var typeSyntax = TypeSyntax;

            var compilation = this.DeclaringCompilation;

            var diagnostics = DiagnosticBag.GetInstance();

            var binderFactory = compilation.GetBinderFactory(SyntaxTree);
            var binder = binderFactory.GetBinder(typeSyntax);

            bool isVar;
            TypeSymbolWithAnnotations type = binder.BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar);

            Debug.Assert(!type.IsNull || isVar);

            if (isVar && !fieldsBeingBound.ContainsReference(this))
            {
                InferFieldType(fieldsBeingBound, binder);
                Debug.Assert(!_lazyType.IsNull);
            }
            else
            {
                if (isVar)
                {
                    diagnostics.Add(ErrorCode.ERR_RecursivelyTypedVariable, this.ErrorLocation, this);
                    type = TypeSymbolWithAnnotations.Create(binder.CreateErrorType("var"));
                }

                SetType(compilation, diagnostics, type);
            }

            diagnostics.Free();
            return _lazyType.ToType();
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>. 
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        private TypeSymbolWithAnnotations SetType(CSharpCompilation compilation, DiagnosticBag diagnostics, TypeSymbolWithAnnotations type)
        {
            var originalType = _lazyType.DefaultType;

            // In the event that we race to set the type of a field, we should
            // always deduce the same type, unless the cached type is an error.

            Debug.Assert((object)originalType == null ||
                originalType.IsErrorType() ||
                TypeSymbol.Equals(originalType, type.TypeSymbol, TypeCompareKind.ConsiderEverything2));

            if (_lazyType.InterlockedInitialize(type))
            {
                TypeChecks(type.TypeSymbol, diagnostics);

                compilation.DeclarationDiagnostics.AddRange(diagnostics);
                state.NotePartComplete(CompletionPart.Type);
            }
            return _lazyType.ToType();
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>.
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        internal TypeSymbolWithAnnotations SetType(TypeSymbolWithAnnotations type, DiagnosticBag diagnostics)
        {
            return SetType(DeclaringCompilation, diagnostics, type);
        }

        protected virtual void InferFieldType(ConsList<FieldSymbol> fieldsBeingBound, Binder binder)
        {
            throw ExceptionUtilities.Unreachable;
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
                Location location,
                FieldSymbol containingFieldOpt,
                SyntaxNode nodeToBind)
                : base(containingType, modifiers, typeSyntax, name, syntax, location)
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
                var diagnostics = DiagnosticBag.GetInstance();

                switch (nodeToBind.Kind())
                {
                    case SyntaxKind.VariableDeclarator:
                        // This occurs, for example, in
                        // int x, y[out var Z, 1 is int I];
                        // for (int x, y[out var Z, 1 is int I]; ;) {}
                        binder.BindDeclaratorArguments((VariableDeclaratorSyntax)nodeToBind, diagnostics);
                        break;

                    default:
                        binder.BindExpression((ExpressionSyntax)nodeToBind, diagnostics);
                        break;
                }

                diagnostics.Free();
            }
        }
    }
}
