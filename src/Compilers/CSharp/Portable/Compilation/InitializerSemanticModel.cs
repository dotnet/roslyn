// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binding for a field initializer, property initializer, constructor
    /// initializer, or a parameter default value.
    /// Represents the result of binding a value expression rather than a
    /// block (for that, use a <see cref="MethodBodySemanticModel"/>).
    /// </summary>
    internal sealed class InitializerSemanticModel : MemberSemanticModel
    {
        // create a SemanticModel for:
        // (a) A true field initializer (field = value) of a named type (incl. Enums) OR
        // (b) A parameter default value
        private InitializerSemanticModel(CSharpSyntaxNode syntax,
                                     Symbol symbol,
                                     Binder rootBinder,
                                     SyntaxTreeSemanticModel containingSemanticModelOpt = null,
                                     SyntaxTreeSemanticModel parentSemanticModelOpt = null,
                                     int speculatedPosition = 0) :
            base(syntax, symbol, rootBinder, containingSemanticModelOpt, parentSemanticModelOpt, snapshotManagerOpt: null, speculatedPosition)
        {
            Debug.Assert(!(syntax is ConstructorInitializerSyntax));
        }

        /// <summary>
        /// Creates a SemanticModel for a true field initializer (field = value) of a named type (incl. Enums).
        /// </summary>
        internal static InitializerSemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, CSharpSyntaxNode syntax, FieldSymbol fieldSymbol, Binder rootBinder)
        {
            Debug.Assert(containingSemanticModel != null);
            Debug.Assert(syntax.IsKind(SyntaxKind.VariableDeclarator) || syntax.IsKind(SyntaxKind.EnumMemberDeclaration));
            return new InitializerSemanticModel(syntax, fieldSymbol, rootBinder, containingSemanticModel);
        }

        /// <summary>
        /// Creates a SemanticModel for an autoprop initializer of a named type
        /// </summary>
        internal static InitializerSemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, CSharpSyntaxNode syntax, PropertySymbol propertySymbol, Binder rootBinder)
        {
            Debug.Assert(containingSemanticModel != null);
            Debug.Assert(syntax.IsKind(SyntaxKind.PropertyDeclaration));
            return new InitializerSemanticModel(syntax, propertySymbol, rootBinder, containingSemanticModel);
        }

        /// <summary>
        /// Creates a SemanticModel for a parameter default value.
        /// </summary>
        internal static InitializerSemanticModel Create(SyntaxTreeSemanticModel containingSemanticModel, ParameterSyntax syntax, ParameterSymbol parameterSymbol, Binder rootBinder)
        {
            Debug.Assert(containingSemanticModel != null);
            return new InitializerSemanticModel(syntax, parameterSymbol, rootBinder, containingSemanticModel);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for an initializer node (field initializer, constructor initializer, or parameter default value)
        /// that did not appear in the original source code.
        /// </summary>
        internal static InitializerSemanticModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, Symbol owner, CSharpSyntaxNode syntax, Binder rootBinder, int position)
        {
            Debug.Assert(parentSemanticModel != null);
            Debug.Assert(syntax != null);
            Debug.Assert(syntax.IsKind(SyntaxKind.EqualsValueClause));
            Debug.Assert(rootBinder != null);
            Debug.Assert(rootBinder.IsSemanticModelBinder);

            return new InitializerSemanticModel(syntax, owner, rootBinder, parentSemanticModelOpt: parentSemanticModel, speculatedPosition: position);
        }

        internal protected override CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
        {
            return IsBindableInitializer(node) ? node : base.GetBindableSyntaxNode(node);
        }

        internal override BoundNode GetBoundRoot()
        {
            CSharpSyntaxNode rootSyntax = this.Root;
            switch (rootSyntax.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    rootSyntax = ((VariableDeclaratorSyntax)rootSyntax).Initializer.Value;
                    break;

                case SyntaxKind.Parameter:
                    var paramDefault = ((ParameterSyntax)rootSyntax).Default;
                    rootSyntax = (paramDefault == null) ? null : paramDefault.Value;
                    break;

                case SyntaxKind.EqualsValueClause:
                    rootSyntax = ((EqualsValueClauseSyntax)rootSyntax).Value;
                    break;

                case SyntaxKind.EnumMemberDeclaration:
                    rootSyntax = ((EnumMemberDeclarationSyntax)rootSyntax).EqualsValue.Value;
                    break;

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                case SyntaxKind.ArgumentList:
                    break;

                case SyntaxKind.PropertyDeclaration:
                    rootSyntax = ((PropertyDeclarationSyntax)rootSyntax).Initializer.Value;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(rootSyntax.Kind());
            }

            return GetUpperBoundNode(GetBindableSyntaxNode(rootSyntax));
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            EqualsValueClauseSyntax equalsValue = null;

            switch (node.Kind())
            {
                case SyntaxKind.EqualsValueClause:
                    equalsValue = (EqualsValueClauseSyntax)node;
                    break;

                case SyntaxKind.VariableDeclarator:
                    equalsValue = ((VariableDeclaratorSyntax)node).Initializer;
                    break;

                case SyntaxKind.PropertyDeclaration:
                    equalsValue = ((PropertyDeclarationSyntax)node).Initializer;
                    break;

                case SyntaxKind.Parameter:
                    equalsValue = ((ParameterSyntax)node).Default;
                    break;

                case SyntaxKind.EnumMemberDeclaration:
                    equalsValue = ((EnumMemberDeclarationSyntax)node).EqualsValue;
                    break;
            }

            if (equalsValue != null)
            {
                return BindEqualsValue(binder, equalsValue, diagnostics);
            }

            return base.Bind(binder, node, diagnostics);
        }

        private BoundEqualsValue BindEqualsValue(Binder binder, EqualsValueClauseSyntax equalsValue, DiagnosticBag diagnostics)
        {
            switch (this.MemberSymbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var field = (FieldSymbol)this.MemberSymbol;
                        var enumField = field as SourceEnumConstantSymbol;
                        if ((object)enumField != null)
                        {
                            return binder.BindEnumConstantInitializer(enumField, equalsValue, diagnostics);
                        }
                        else
                        {
                            return binder.BindFieldInitializer(field, equalsValue, diagnostics);
                        }
                    }

                case SymbolKind.Property:
                    {
                        var property = (SourcePropertySymbol)this.MemberSymbol;
                        BoundFieldEqualsValue result = binder.BindFieldInitializer(property.BackingField, equalsValue, diagnostics);
                        return new BoundPropertyEqualsValue(result.Syntax, property, result.Locals, result.Value);
                    }

                case SymbolKind.Parameter:
                    {
                        var parameter = (ParameterSymbol)this.MemberSymbol;
                        return binder.BindParameterDefaultValue(
                            equalsValue,
                            parameter,
                            diagnostics,
                            out _);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.MemberSymbol.Kind);
            }
        }

        private bool IsBindableInitializer(CSharpSyntaxNode node)
        {
            // If we are being asked to bind the equals clause (the "=1" part of "double x=1,y=2;"),
            // that's our root and we know how to bind that thing even if it is not an 
            // expression or a statement.

            switch (node.Kind())
            {
                case SyntaxKind.EqualsValueClause:
                    return this.Root == node ||     /*enum or parameter initializer*/
                           this.Root == node.Parent /*field initializer*/;

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                case SyntaxKind.ArgumentList:
                    return this.Root == node;

                default:
                    return false;
            }
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out SemanticModel speculativeModel)
        {
            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            binder = new ExecutableCodeBinder(initializer, binder.ContainingMemberOrLambda, binder);
            speculativeModel = CreateSpeculative(parentModel, this.MemberSymbol, initializer, binder, position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out SemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        protected override BoundNode RewriteNullableBoundNodesWithSnapshots(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots, out NullableWalker.SnapshotManager snapshotManager)
        {
            return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol, boundRoot, binder, diagnostics, createSnapshots, out snapshotManager);
        }
    }
}
