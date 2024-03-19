// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        internal InitializerSemanticModel(CSharpSyntaxNode syntax,
                                     Symbol symbol,
                                     Binder rootBinder,
                                     PublicSemanticModel containingPublicSemanticModel,
                                     ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt = null) :
            base(syntax, symbol, rootBinder, containingPublicSemanticModel, parentRemappedSymbolsOpt)
        {
            Debug.Assert(!(syntax is ConstructorInitializerSyntax || syntax is PrimaryConstructorBaseTypeSyntax));
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
        internal static InitializerSemanticModel Create(PublicSemanticModel containingSemanticModel, ParameterSyntax syntax, ParameterSymbol parameterSymbol, Binder rootBinder, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt)
        {
            Debug.Assert(containingSemanticModel != null);
            return new InitializerSemanticModel(syntax, parameterSymbol, rootBinder, containingSemanticModel, parentRemappedSymbolsOpt: parentRemappedSymbolsOpt);
        }

        /// <summary>
        /// Creates a speculative SemanticModel for an initializer node (field initializer, constructor initializer, or parameter default value)
        /// that did not appear in the original source code.
        /// </summary>
        internal static SpeculativeSemanticModelWithMemberModel CreateSpeculative(SyntaxTreeSemanticModel parentSemanticModel, Symbol owner, EqualsValueClauseSyntax syntax, Binder rootBinder, ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt, int position)
        {
            return new SpeculativeSemanticModelWithMemberModel(parentSemanticModel, position, owner, syntax, rootBinder, parentRemappedSymbolsOpt);
        }

        protected internal override CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
        {
            return IsBindableInitializer(node) ? node : base.GetBindableSyntaxNode(node);
        }

        internal override BoundNode GetBoundRoot()
        {
            CSharpSyntaxNode rootSyntax = this.Root;
            switch (rootSyntax.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                    rootSyntax = ((VariableDeclaratorSyntax)rootSyntax).Initializer;
                    break;

                case SyntaxKind.Parameter:
                    rootSyntax = ((ParameterSyntax)rootSyntax).Default;
                    break;

                case SyntaxKind.EqualsValueClause:
                    rootSyntax = ((EqualsValueClauseSyntax)rootSyntax);
                    break;

                case SyntaxKind.EnumMemberDeclaration:
                    rootSyntax = ((EnumMemberDeclarationSyntax)rootSyntax).EqualsValue;
                    break;

                case SyntaxKind.PropertyDeclaration:
                    rootSyntax = ((PropertyDeclarationSyntax)rootSyntax).Initializer;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(rootSyntax.Kind());
            }

            return GetUpperBoundNode(GetBindableSyntaxNode(rootSyntax));
        }

        internal override BoundNode Bind(Binder binder, CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
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

        private BoundEqualsValue BindEqualsValue(Binder binder, EqualsValueClauseSyntax equalsValue, BindingDiagnosticBag diagnostics)
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

                default:
                    return false;
            }
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, EqualsValueClauseSyntax initializer, out PublicSemanticModel speculativeModel)
        {
            var binder = this.GetEnclosingBinder(position);
            if (binder == null)
            {
                speculativeModel = null;
                return false;
            }

            binder = new ExecutableCodeBinder(initializer, binder.ContainingMemberOrLambda, binder);
            speculativeModel = CreateSpeculative(parentModel, this.MemberSymbol, initializer, binder, GetRemappedSymbols(), position);
            return true;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ConstructorInitializerSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, PrimaryConstructorBaseTypeSyntax constructorInitializer, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, ArrowExpressionClauseSyntax expressionBody, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, StatementSyntax statement, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, BaseMethodDeclarationSyntax method, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        internal override bool TryGetSpeculativeSemanticModelForMethodBodyCore(SyntaxTreeSemanticModel parentModel, int position, AccessorDeclarationSyntax accessor, out PublicSemanticModel speculativeModel)
        {
            speculativeModel = null;
            return false;
        }

        protected override BoundNode RewriteNullableBoundNodesWithSnapshots(
            BoundNode boundRoot,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out NullableWalker.SnapshotManager snapshotManager,
            ref ImmutableDictionary<Symbol, Symbol> remappedSymbols)
        {
            // https://github.com/dotnet/roslyn/issues/46424
            // Bind and analyze preceding field initializers in order to give an accurate initial nullable state.
            return NullableWalker.AnalyzeAndRewrite(Compilation, MemberSymbol, boundRoot, binder, initialState: null, diagnostics, createSnapshots, out snapshotManager, ref remappedSymbols);
        }

        protected override void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots)
        {
            NullableWalker.AnalyzeWithoutRewrite(Compilation, MemberSymbol, boundRoot, binder, diagnostics, createSnapshots);
        }

#nullable enable

        protected override bool IsNullableAnalysisEnabledCore()
        {
            switch (MemberSymbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                    Debug.Assert(MemberSymbol.ContainingType is SourceMemberContainerTypeSymbol);
                    return MemberSymbol.ContainingType is SourceMemberContainerTypeSymbol type &&
                        type.IsNullableEnabledForConstructorsAndInitializers(useStatic: MemberSymbol.IsStatic);
                case SymbolKind.Parameter:
                    return SourceComplexParameterSymbolBase.GetDefaultValueSyntaxForIsNullableAnalysisEnabled(Root as ParameterSyntax) is { } value &&
                        Compilation.IsNullableAnalysisEnabledIn(value);
                default:
                    throw ExceptionUtilities.UnexpectedValue(MemberSymbol.Kind);
            }
        }
    }
}
