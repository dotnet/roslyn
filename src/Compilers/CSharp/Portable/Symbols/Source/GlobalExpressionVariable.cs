// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        private TypeWithAnnotations.Boxed? _lazyType;

        /// <summary>
        /// The type syntax, if any, from source. Optional for patterns that can omit an explicit type.
        /// </summary>
        private SyntaxReference? _typeSyntaxOpt;

        internal GlobalExpressionVariable(
            SourceMemberContainerTypeSymbol containingType,
            DeclarationModifiers modifiers,
            TypeSyntax? typeSyntax,
            string name,
            SyntaxReference syntax,
            Location location)
            : base(containingType, modifiers, name, syntax, location)
        {
            Debug.Assert(DeclaredAccessibility == Accessibility.Private);
            _typeSyntaxOpt = typeSyntax?.GetReference();
        }

        internal static GlobalExpressionVariable Create(
                SourceMemberContainerTypeSymbol containingType,
                DeclarationModifiers modifiers,
                TypeSyntax? typeSyntax,
                string name,
                SyntaxNode syntax,
                Location location,
                FieldSymbol containingFieldOpt,
                SyntaxNode nodeToBind)
        {
            Debug.Assert(nodeToBind.Kind() == SyntaxKind.VariableDeclarator || nodeToBind is ExpressionSyntax);

            var syntaxReference = syntax.GetReference();
            return (typeSyntax == null || typeSyntax.IsVar)
                ? new InferrableGlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, location, containingFieldOpt, nodeToBind)
                : new GlobalExpressionVariable(containingType, modifiers, typeSyntax, name, syntaxReference, location);
        }


        protected override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList => default(SyntaxList<AttributeListSyntax>);
        protected override TypeSyntax? TypeSyntax => (TypeSyntax?)_typeSyntaxOpt?.GetSyntax();
        protected override SyntaxTokenList ModifiersTokenList => default(SyntaxTokenList);
        public override bool HasInitializer => false;
        protected override ConstantValue? MakeConstantValue(
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies,
            bool earlyDecodingWellKnownAttributes,
            DiagnosticBag diagnostics) => null;

        internal override TypeWithAnnotations GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            RoslynDebug.Assert(fieldsBeingBound != null);

            if (_lazyType != null)
            {
                return _lazyType.Value;
            }

            var typeSyntax = TypeSyntax;

            var compilation = this.DeclaringCompilation;

            var diagnostics = DiagnosticBag.GetInstance();
            TypeWithAnnotations type;
            bool isVar;

            var binderFactory = compilation.GetBinderFactory(SyntaxTree);
            var binder = binderFactory.GetBinder(typeSyntax ?? SyntaxNode);

            if (typeSyntax != null)
            {
                type = binder.BindTypeOrVarKeyword(typeSyntax, diagnostics, out isVar);
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
                RoslynDebug.Assert(_lazyType != null);
            }
            else
            {
                if (isVar)
                {
                    diagnostics.Add(ErrorCode.ERR_RecursivelyTypedVariable, this.ErrorLocation, this);
                    type = TypeWithAnnotations.Create(binder.CreateErrorType("var"));
                }

                SetType(compilation, diagnostics, type);
            }

            diagnostics.Free();
#nullable disable // The compiler can't tell that 'SetType' ensures '_lazyType' is not null. https://github.com/dotnet/roslyn/issues/39166
            return _lazyType.Value;
#nullable enable
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>. 
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        private TypeWithAnnotations SetType(CSharpCompilation compilation, DiagnosticBag diagnostics, TypeWithAnnotations type)
        {
            var originalType = _lazyType?.Value.DefaultType;

            // In the event that we race to set the type of a field, we should
            // always deduce the same type, unless the cached type is an error.

            Debug.Assert((object?)originalType == null ||
                originalType.IsErrorType() ||
                TypeSymbol.Equals(originalType, type.Type, TypeCompareKind.ConsiderEverything2));

            if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null) == null)
            {
                TypeChecks(type.Type, diagnostics);

                compilation.DeclarationDiagnostics.AddRange(diagnostics);
                state.NotePartComplete(CompletionPart.Type);
            }
            return _lazyType.Value;
        }

        /// <summary>
        /// Can add some diagnostics into <paramref name="diagnostics"/>.
        /// Returns the type that it actually locks onto (it's possible that it had already locked onto ErrorType).
        /// </summary>
        internal TypeWithAnnotations SetTypeWithAnnotations(TypeWithAnnotations type, DiagnosticBag diagnostics)
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
                TypeSyntax? typeSyntax,
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
