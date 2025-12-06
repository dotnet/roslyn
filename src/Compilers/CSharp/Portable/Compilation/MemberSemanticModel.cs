// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binding info for expressions and statements that are part of a member declaration.
    /// Instances of this class should not be exposed to external consumers.
    /// </summary>
    internal abstract partial class MemberSemanticModel : CSharpSemanticModel
    {
        private readonly Symbol _memberSymbol;
        private readonly CSharpSyntaxNode _root;
        private readonly ReaderWriterLockSlim _nodeMapLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        // The bound nodes associated with a syntax node, from highest in the tree to lowest.
        private readonly Dictionary<SyntaxNode, OneOrMany<BoundNode>> _guardedBoundNodeMap = new Dictionary<SyntaxNode, OneOrMany<BoundNode>>();
        private readonly Dictionary<SyntaxNode, IOperation> _guardedIOperationNodeMap = new Dictionary<SyntaxNode, IOperation>();
        private Dictionary<SyntaxNode, BoundStatement> _lazyGuardedSynthesizedStatementsMap;
        private NullableWalker.SnapshotManager _lazySnapshotManager;
        private ImmutableDictionary<Symbol, Symbol> _lazyRemappedSymbols;
        private readonly ImmutableDictionary<Symbol, Symbol> _parentRemappedSymbolsOpt;

        internal readonly Binder RootBinder;

        private readonly PublicSemanticModel _containingPublicSemanticModel;

        private readonly Lazy<CSharpOperationFactory> _operationFactory;

        protected MemberSemanticModel(
            CSharpSyntaxNode root,
            Symbol memberSymbol,
            Binder rootBinder,
            PublicSemanticModel containingPublicSemanticModel,
            ImmutableDictionary<Symbol, Symbol> parentRemappedSymbolsOpt)
        {
            Debug.Assert(root != null);
            Debug.Assert((object)memberSymbol != null);
            Debug.Assert(containingPublicSemanticModel.IsSpeculativeSemanticModel == (containingPublicSemanticModel is SpeculativeSemanticModelWithMemberModel));

            _root = root;
            _memberSymbol = memberSymbol;
            _containingPublicSemanticModel = containingPublicSemanticModel;
            _parentRemappedSymbolsOpt = parentRemappedSymbolsOpt;

            this.RootBinder = rootBinder.WithAdditionalFlags(GetSemanticModelBinderFlags());
            _operationFactory = new Lazy<CSharpOperationFactory>(() => new CSharpOperationFactory(this));
        }

        public override CSharpCompilation Compilation
        {
            get
            {
                return _containingPublicSemanticModel.Compilation;
            }
        }

        internal override CSharpSyntaxNode Root
        {
            get
            {
                return _root;
            }
        }

        /// <summary>
        /// The member symbol 
        /// </summary>
        internal Symbol MemberSymbol
        {
            get
            {
                return _memberSymbol;
            }
        }

        public sealed override bool IsSpeculativeSemanticModel
        {
            get
            {
                return _containingPublicSemanticModel.IsSpeculativeSemanticModel;
            }
        }

        public sealed override bool IgnoresAccessibility
        {
            get
            {
                return _containingPublicSemanticModel.IgnoresAccessibility;
            }
        }

        [Experimental(RoslynExperiments.NullableDisabledSemanticModel, UrlFormat = RoslynExperiments.NullableDisabledSemanticModel_Url)]
        public sealed override bool NullableAnalysisIsDisabled
        {
            get
            {
                return _containingPublicSemanticModel.NullableAnalysisIsDisabled;
            }
        }

        public sealed override int OriginalPositionForSpeculation
        {
            get
            {
                // This property is not meaningful for member semantic models.
                // An external consumer should never be able to access them directly.
                throw ExceptionUtilities.Unreachable();
            }
        }

        public sealed override CSharpSemanticModel ParentModel
        {
            get
            {
                // This property is not meaningful for member semantic models.
                // An external consumer should never be able to access them directly.
                throw ExceptionUtilities.Unreachable();
            }
        }

        internal sealed override SemanticModel ContainingPublicModelOrSelf
        {
            get
            {
                return _containingPublicSemanticModel;
            }
        }

        internal override MemberSemanticModel GetMemberModel(SyntaxNode node)
        {
            // We do have to override this method, but should never call it because it might not do the right thing. 
            Debug.Assert(false);
            return IsInTree(node) ? this : null;
        }

        /// <remarks>
        /// This will cause the bound node cache to be populated if nullable semantic analysis is enabled.
        /// </remarks>
        protected virtual NullableWalker.SnapshotManager GetSnapshotManager()
        {
            EnsureNullabilityAnalysisPerformedIfNecessary();
            Debug.Assert(_lazySnapshotManager is object || this is AttributeSemanticModel || !IsNullableAnalysisEnabled());
            return _lazySnapshotManager;
        }

        internal ImmutableDictionary<Symbol, Symbol> GetRemappedSymbols()
        {
            EnsureNullabilityAnalysisPerformedIfNecessary();
            Debug.Assert(_lazyRemappedSymbols is object || this is AttributeSemanticModel || !IsNullableAnalysisEnabled());
            return _lazyRemappedSymbols;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out PublicSemanticModel speculativeModel)
        {
            var expression = SyntaxFactory.GetStandaloneExpression(type);

            var binder = this.GetSpeculativeBinder(position, expression, bindingOption);
            if (binder != null)
            {
                speculativeModel = new SpeculativeSemanticModelWithMemberModel(parentModel, position, _memberSymbol, type, binder, GetRemappedSymbols(), GetSnapshotManager());
                return true;
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out PublicSemanticModel speculativeModel)
        {
            // crefs can never legally appear within members.
            speculativeModel = null;
            return false;
        }

        internal override BoundExpression GetSpeculativelyBoundExpression(int position, ExpressionSyntax expression, SpeculativeBindingOption bindingOption, out Binder binder, out ImmutableArray<Symbol> crefSymbols)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (bindingOption == SpeculativeBindingOption.BindAsExpression && GetSnapshotManager() is { } snapshotManager)
            {
                crefSymbols = default;
                position = CheckAndAdjustPosition(position);
                expression = SyntaxFactory.GetStandaloneExpression(expression);
                binder = GetSpeculativeBinder(position, expression, bindingOption);
                var boundRoot = binder.BindExpression(expression, BindingDiagnosticBag.Discarded);
                ImmutableDictionary<Symbol, Symbol> ignored = null;
                return (BoundExpression)NullableWalker.AnalyzeAndRewriteSpeculation(position, boundRoot, binder, snapshotManager, newSnapshots: out _, remappedSymbols: ref ignored);
            }
            else
            {
                return GetSpeculativelyBoundExpressionWithoutNullability(position, expression, bindingOption, out binder, out crefSymbols);
            }
        }

        private Binder GetEnclosingBinderInternalWithinRoot(SyntaxNode node, int position)
        {
            AssertPositionAdjusted(position);
            return GetEnclosingBinderInternalWithinRoot(node, position, RootBinder, _root).WithAdditionalFlags(GetSemanticModelBinderFlags());
        }

        private static Binder GetEnclosingBinderInternalWithinRoot(SyntaxNode node, int position, Binder rootBinder, SyntaxNode root)
        {
            if (node == root)
            {
                return rootBinder.GetBinder(node) ?? rootBinder;
            }

            Debug.Assert(root.Contains(node));

            ExpressionSyntax typeOfArgument = null;
            LocalFunctionStatementSyntax ownerOfTypeParametersInScope = null;

            Binder binder = null;

            for (var current = node; binder == null; current = current.ParentOrStructuredTriviaParent)
            {
                Debug.Assert(current != null); // Why were we asked for an enclosing binder for a node outside our root?
                StatementSyntax stmt = current as StatementSyntax;
                TypeOfExpressionSyntax typeOfExpression;
                SyntaxKind kind = current.Kind();

                if (stmt != null)
                {
                    if (LookupPosition.IsInStatementScope(position, stmt))
                    {
                        binder = rootBinder.GetBinder(current);

                        if (binder != null)
                        {
                            binder = AdjustBinderForPositionWithinStatement(position, binder, stmt);
                        }
                        else if (kind == SyntaxKind.LocalFunctionStatement)
                        {
                            Debug.Assert(ownerOfTypeParametersInScope == null);
                            var localFunction = (LocalFunctionStatementSyntax)stmt;
                            if (LookupPosition.IsInLocalFunctionTypeParameterScope(position, localFunction))
                            {
                                ownerOfTypeParametersInScope = localFunction;
                            }
                        }
                    }
                }
                else if (kind == SyntaxKind.CatchClause)
                {
                    if (LookupPosition.IsInCatchBlockScope(position, (CatchClauseSyntax)current))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (kind == SyntaxKind.CatchFilterClause)
                {
                    if (LookupPosition.IsInCatchFilterScope(position, (CatchFilterClauseSyntax)current))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (current.IsAnonymousFunction())
                {
                    if (LookupPosition.IsInAnonymousFunctionOrQuery(position, current))
                    {
                        binder = rootBinder.GetBinder(current.AnonymousFunctionBody());
                        Debug.Assert(binder != null);
                    }
                }
                else if (kind == SyntaxKind.TypeOfExpression &&
                    typeOfArgument == null &&
                    LookupPosition.IsBetweenTokens(
                        position,
                        (typeOfExpression = (TypeOfExpressionSyntax)current).OpenParenToken,
                        typeOfExpression.CloseParenToken))
                {
                    typeOfArgument = typeOfExpression.Type;
                }
                else if (kind == SyntaxKind.SwitchSection)
                {
                    if (LookupPosition.IsInSwitchSectionScope(position, (SwitchSectionSyntax)current))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (kind == SyntaxKind.ArgumentList)
                {
                    var argList = (ArgumentListSyntax)current;

                    if (LookupPosition.IsBetweenTokens(position, argList.OpenParenToken, argList.CloseParenToken))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (kind == SyntaxKind.EqualsValueClause)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.Attribute)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.ArrowExpressionClause)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.ThisConstructorInitializer || kind == SyntaxKind.BaseConstructorInitializer || kind == SyntaxKind.PrimaryConstructorBaseType)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.ConstructorDeclaration)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.SwitchExpression)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (kind == SyntaxKind.SwitchExpressionArm)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if ((current as ExpressionSyntax).IsValidScopeDesignator())
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if ((current is InvocationExpressionSyntax invocation) && invocation.MayBeNameofOperator())
                {
                    binder = rootBinder.GetBinder(current);
                }
                else if (current is CheckedExpressionSyntax checkedExpression)
                {
                    if (LookupPosition.IsBetweenTokens(position, checkedExpression.OpenParenToken, checkedExpression.CloseParenToken))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else
                {
                    // If this ever breaks, make sure that all callers of
                    // CanHaveAssociatedLocalBinder are in sync.
                    Debug.Assert(!current.CanHaveAssociatedLocalBinder());
                }

                if (current == root)
                {
                    break;
                }
            }

            binder = binder ?? rootBinder.GetBinder(root) ?? rootBinder;
            Debug.Assert(binder != null);

            if (ownerOfTypeParametersInScope != null)
            {
                LocalFunctionSymbol function = GetDeclaredLocalFunction(binder, ownerOfTypeParametersInScope.Identifier);
                if ((object)function != null)
                {
                    binder = function.WithTypeParametersBinder;
                }
            }

            if (typeOfArgument != null)
            {
                binder = new TypeofBinder(typeOfArgument, binder);
            }

            return binder;
        }

        private static Binder AdjustBinderForPositionWithinStatement(int position, Binder binder, StatementSyntax stmt)
        {
            switch (stmt.Kind())
            {
                case SyntaxKind.SwitchStatement:
                    var switchStmt = (SwitchStatementSyntax)stmt;
                    if (LookupPosition.IsBetweenTokens(position, switchStmt.SwitchKeyword, switchStmt.OpenBraceToken))
                    {
                        binder = binder.GetBinder(switchStmt.Expression);
                        Debug.Assert(binder != null);
                    }
                    break;

                case SyntaxKind.ForStatement:
                    var forStmt = (ForStatementSyntax)stmt;
                    if (LookupPosition.IsBetweenTokens(position, forStmt.SecondSemicolonToken, forStmt.CloseParenToken) &&
                        forStmt.Incrementors.Count > 0)
                    {
                        binder = binder.GetBinder(forStmt.Incrementors.First());
                        Debug.Assert(binder != null);
                    }
                    else if (LookupPosition.IsBetweenTokens(position, forStmt.FirstSemicolonToken, LookupPosition.GetFirstExcludedToken(forStmt)) &&
                        forStmt.Condition != null)
                    {
                        binder = binder.GetBinder(forStmt.Condition);
                        Debug.Assert(binder != null);
                    }
                    break;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    var foreachStmt = (CommonForEachStatementSyntax)stmt;
                    var start = stmt.Kind() == SyntaxKind.ForEachVariableStatement ? foreachStmt.InKeyword : foreachStmt.OpenParenToken;
                    if (LookupPosition.IsBetweenTokens(position, start, foreachStmt.Statement.GetFirstToken()))
                    {
                        binder = binder.GetBinder(foreachStmt.Expression);
                        Debug.Assert(binder != null);
                    }
                    break;
            }

            return binder;
        }

        public override Conversion ClassifyConversion(
            ExpressionSyntax expression,
            ITypeSymbol destination,
            bool isExplicitInSource = false)
        {
            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            TypeSymbol csdestination = destination.EnsureCSharpSymbolOrNull(nameof(destination));

            if (expression.Kind() == SyntaxKind.DeclarationExpression)
            {
                // Conversion from a declaration is unspecified.
                return Conversion.NoConversion;
            }

            // Special Case: We have to treat anonymous functions differently, because of the way
            // they are cached in the syntax-to-bound node map.  Specifically, UnboundLambda nodes
            // never appear in the map - they are converted to BoundLambdas, even in error scenarios.
            // Since a BoundLambda has a type, we would end up doing a conversion from the delegate
            // type, rather than from the anonymous function expression.  If we use the overload that
            // takes a position, it considers the request speculative and does not use the map.
            // Bonus: Since the other overload will always bind the anonymous function from scratch,
            // we don't have to worry about it affecting the trial-binding cache in the "real" 
            // UnboundLambda node (DevDiv #854548).
            if (expression.IsAnonymousFunction())
            {
                CheckSyntaxNode(expression);
                return this.ClassifyConversion(expression.SpanStart, expression, destination, isExplicitInSource);
            }

            if (isExplicitInSource)
            {
                return ClassifyConversionForCast(expression, csdestination);
            }

            // Note that it is possible for an expression to be convertible to a type
            // via both an implicit user-defined conversion and an explicit built-in conversion.
            // In that case, this method chooses the implicit conversion.

            CheckSyntaxNode(expression);

            var binder = this.GetEnclosingBinderInternal(expression, GetAdjustedNodePosition(expression));
            CSharpSyntaxNode bindableNode = this.GetBindableSyntaxNode(expression);
            var boundExpression = this.GetLowerBoundNode(bindableNode) as BoundExpression;
            if (binder == null || boundExpression == null)
            {
                return Conversion.NoConversion;
            }

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            return binder.Conversions.ClassifyConversionFromExpression(boundExpression, csdestination, isChecked: binder.CheckOverflowAtRuntime, ref discardedUseSiteInfo);
        }

        internal override Conversion ClassifyConversionForCast(
            ExpressionSyntax expression,
            TypeSymbol destination)
        {
            CheckSyntaxNode(expression);

            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var binder = this.GetEnclosingBinderInternal(expression, GetAdjustedNodePosition(expression));
            CSharpSyntaxNode bindableNode = this.GetBindableSyntaxNode(expression);
            var boundExpression = this.GetLowerBoundNode(bindableNode) as BoundExpression;
            if (binder == null || boundExpression == null)
            {
                return Conversion.NoConversion;
            }

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            return binder.Conversions.ClassifyConversionFromExpression(boundExpression, destination, isChecked: binder.CheckOverflowAtRuntime, ref discardedUseSiteInfo, forCast: true);
        }

        /// <summary>
        /// Get the bound node corresponding to the root.
        /// </summary> 
        internal virtual BoundNode GetBoundRoot()
        {
            return GetUpperBoundNode(GetBindableSyntaxNode(this.Root));
        }

        /// <summary>
        /// Get the highest bound node in the tree associated with a particular syntax node.
        /// </summary>
        internal BoundNode GetUpperBoundNode(CSharpSyntaxNode node, bool promoteToBindable = false)
        {
            if (promoteToBindable)
            {
                node = GetBindableSyntaxNode(node);
            }
            else
            {
                Debug.Assert(node == GetBindableSyntaxNode(node));
            }

            // The bound nodes are stored in the map from highest to lowest, so the first bound node is the highest.
            var boundNodes = GetBoundNodes(node);

            if (boundNodes.Count == 0)
            {
                return null;
            }
            else
            {
                return boundNodes[0];
            }
        }

        /// <summary>
        /// Get the lowest bound node in the tree associated with a particular syntax node. Lowest is defined as last
        /// in a pre-order traversal of the bound tree.
        /// </summary>
        internal BoundNode GetLowerBoundNode(CSharpSyntaxNode node)
        {
            Debug.Assert(node == GetBindableSyntaxNode(node));

            // The bound nodes are stored in the map from highest to lowest, so the last bound node is the lowest.
            var boundNodes = GetBoundNodes(node);

            if (boundNodes.Count == 0)
            {
                return null;
            }
            else
            {
                return GetLowerBoundNode(boundNodes);
            }
        }

        private static BoundNode GetLowerBoundNode(OneOrMany<BoundNode> boundNodes)
        {
            return boundNodes[boundNodes.Count - 1];
        }

        public sealed override ImmutableArray<Diagnostic> GetSyntaxDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public sealed override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public sealed override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public sealed override ImmutableArray<Diagnostic> GetDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't defined namespace inside a member.
            return null;
        }

        public override INamespaceSymbol GetDeclaredSymbol(FileScopedNamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't defined namespace inside a member.
            return null;
        }

        public override INamedTypeSymbol GetDeclaredSymbol(BaseTypeDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define type inside a member.
            return null;
        }

        public override INamedTypeSymbol GetDeclaredSymbol(DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define type inside a member.
            return null;
        }

        public override IFieldSymbol GetDeclaredSymbol(EnumMemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define member inside member.
            return null;
        }

        public override IMethodSymbol GetDeclaredSymbol(LocalFunctionStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);
            return GetDeclaredLocalFunction(declarationSyntax).GetPublicSymbol();
        }

        public override ISymbol GetDeclaredSymbol(MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define member inside member.
            return null;
        }

        public override IMethodSymbol GetDeclaredSymbol(CompilationUnitSyntax declarationSyntax, CancellationToken cancellationToken = default)
        {
            return null;
        }

        public override IMethodSymbol GetDeclaredSymbol(BaseMethodDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define method inside member.
            return null;
        }

        public override ISymbol GetDeclaredSymbol(BasePropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define property inside member.
            return null;
        }

        public override IPropertySymbol GetDeclaredSymbol(PropertyDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define property inside member.
            return null;
        }

        public override IPropertySymbol GetDeclaredSymbol(IndexerDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define indexer inside member.
            return null;
        }

        public override IEventSymbol GetDeclaredSymbol(EventDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define event inside member.
            return null;
        }

        public override IMethodSymbol GetDeclaredSymbol(AccessorDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define accessor inside member.
            return null;
        }

        public override IMethodSymbol GetDeclaredSymbol(ArrowExpressionClauseSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define another member inside member.
            return null;
        }

        public override ISymbol GetDeclaredSymbol(VariableDeclaratorSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);
            return GetDeclaredLocal(declarationSyntax, declarationSyntax.Identifier).GetPublicSymbol();
        }

        public override ISymbol GetDeclaredSymbol(SingleVariableDesignationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);
            return GetDeclaredLocal(declarationSyntax, declarationSyntax.Identifier).GetPublicSymbol();
        }

        private LocalSymbol GetDeclaredLocal(CSharpSyntaxNode declarationSyntax, SyntaxToken declaredIdentifier)
        {
            for (var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax)); binder != null; binder = binder.Next)
            {
                foreach (var local in binder.Locals)
                {
                    if (local.IdentifierToken == declaredIdentifier)
                    {
                        return GetAdjustedLocalSymbol((SourceLocalSymbol)local);
                    }
                }
            }

            return null;
        }

#nullable enable
        internal override LocalSymbol GetAdjustedLocalSymbol(SourceLocalSymbol local)
        {
            return GetRemappedSymbol<LocalSymbol>(local);
        }

        internal LocalFunctionSymbol GetDeclaredLocalFunction(LocalFunctionStatementSyntax declarationSyntax)
        {
            var originalSymbol = GetDeclaredLocalFunction(this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax)), declarationSyntax.Identifier);
            return GetRemappedSymbol(originalSymbol);
        }

        private T GetRemappedSymbol<T>(T originalSymbol) where T : Symbol
        {
            EnsureNullabilityAnalysisPerformedIfNecessary();
            if (_lazyRemappedSymbols is null) return originalSymbol;

            if (_lazyRemappedSymbols.TryGetValue(originalSymbol, out Symbol? remappedSymbol))
            {
                RoslynDebug.Assert(remappedSymbol is object);
                return (T)remappedSymbol;
            }

            return originalSymbol;
        }
#nullable disable

        private static LocalFunctionSymbol GetDeclaredLocalFunction(Binder enclosingBinder, SyntaxToken declaredIdentifier)
        {
            for (var binder = enclosingBinder; binder != null; binder = binder.Next)
            {
                foreach (var localFunction in binder.LocalFunctions)
                {
                    if (localFunction.NameToken == declaredIdentifier)
                    {
                        return localFunction;
                    }
                }
            }

            return null;
        }

        public override ILabelSymbol GetDeclaredSymbol(LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax));

            while (binder != null && !binder.IsLabelsScopeBinder)
            {
                binder = binder.Next;
            }

            if (binder != null)
            {
                foreach (var label in binder.Labels)
                {
                    if (label.IdentifierNodeOrToken.IsToken &&
                        label.IdentifierNodeOrToken.AsToken() == declarationSyntax.Identifier)
                    {
                        return label.GetPublicSymbol();
                    }
                }
            }

            return null;
        }

        public override ILabelSymbol GetDeclaredSymbol(SwitchLabelSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax));
            while (binder != null && !(binder is SwitchBinder))
            {
                binder = binder.Next;
            }

            if (binder != null)
            {
                foreach (var label in binder.Labels)
                {
                    if (label.IdentifierNodeOrToken.IsNode &&
                        label.IdentifierNodeOrToken.AsNode() == declarationSyntax)
                    {
                        return label.GetPublicSymbol();
                    }
                }
            }

            return null;
        }

        public override IAliasSymbol GetDeclaredSymbol(UsingDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define alias inside member.
            return null;
        }

        public override IAliasSymbol GetDeclaredSymbol(ExternAliasDirectiveSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define an extern alias inside a member.
            return null;
        }

        public override IParameterSymbol GetDeclaredSymbol(ParameterSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Could be parameter of a lambda or a local function.
            CheckSyntaxNode(declarationSyntax);

            return GetLambdaOrLocalFunctionParameterSymbol(declarationSyntax, cancellationToken).GetPublicSymbol();
        }

        internal override ImmutableArray<ISymbol> GetDeclaredSymbols(BaseFieldDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define field inside member.
            return ImmutableArray.Create<ISymbol>();
        }

        private ParameterSymbol GetLambdaOrLocalFunctionParameterSymbol(
            ParameterSyntax parameter,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter != null);

            var simpleLambda = parameter.Parent as SimpleLambdaExpressionSyntax;
            if (simpleLambda != null)
            {
                return GetLambdaParameterSymbol(parameter, simpleLambda, cancellationToken);
            }

            var paramList = parameter.Parent as ParameterListSyntax;
            if (paramList == null || paramList.Parent == null)
            {
                return null;
            }

            if (paramList.Parent.IsAnonymousFunction())
            {
                return GetLambdaParameterSymbol(parameter, (ExpressionSyntax)paramList.Parent, cancellationToken);
            }
            else if (paramList.Parent.Kind() == SyntaxKind.LocalFunctionStatement)
            {
                var localFunction = GetDeclaredSymbol((LocalFunctionStatementSyntax)paramList.Parent, cancellationToken).GetSymbol<MethodSymbol>();
                if ((object)localFunction != null)
                {
                    return GetParameterSymbol(localFunction.Parameters, parameter, cancellationToken);
                }
            }

            return null;
        }

        private ParameterSymbol GetLambdaParameterSymbol(
            ParameterSyntax parameter,
            ExpressionSyntax lambda,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameter != null);
            Debug.Assert(lambda != null && lambda.IsAnonymousFunction());

            // We should always be able to get at least an error binding for a lambda.

            SymbolInfo symbolInfo = this.GetSymbolInfo(lambda, cancellationToken);

            LambdaSymbol lambdaSymbol;
            if ((object)symbolInfo.Symbol != null)
            {
                lambdaSymbol = symbolInfo.Symbol.GetSymbol<LambdaSymbol>();
            }
            else if (symbolInfo.CandidateSymbols.Length == 1)
            {
                lambdaSymbol = symbolInfo.CandidateSymbols.Single().GetSymbol<LambdaSymbol>();
            }
            else
            {
                Debug.Assert(this.GetMemberModel(lambda) == null, "Did not find a unique LambdaSymbol for lambda in member.");
                return null;
            }
            return GetParameterSymbol(lambdaSymbol.Parameters, parameter, cancellationToken);
        }

        public override ITypeParameterSymbol GetDeclaredSymbol(TypeParameterSyntax typeParameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define alias inside member.
            return null;
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(JoinIntoClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return bound == null ? null : bound.DefinedSymbol.GetPublicSymbol();
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryClauseSyntax queryClause, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(queryClause);
            return bound == null ? null : bound.DefinedSymbol.GetPublicSymbol();
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return bound == null ? null : bound.DefinedSymbol.GetPublicSymbol();
        }

#nullable enable
        public override AwaitExpressionInfo GetAwaitExpressionInfo(AwaitExpressionSyntax node)
        {
            if (node.Kind() != SyntaxKind.AwaitExpression)
            {
                throw new ArgumentException("node.Kind==" + node.Kind());
            }

            var bound = GetLowerBoundNode(node);
            BoundAwaitableInfo? awaitableInfo = (((bound as BoundExpressionStatement)?.Expression ?? bound) as BoundAwaitExpression)?.AwaitableInfo;
            return GetAwaitExpressionInfo(awaitableInfo);
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(LocalDeclarationStatementSyntax node)
        {
            if (node.UsingKeyword.IsKind(SyntaxKind.None) || node.AwaitKeyword.IsKind(SyntaxKind.None))
            {
                throw new ArgumentException(CSharpResources.NodeIsNotAwaitUsingDeclaration, nameof(node));
            }

            var bound = GetLowerBoundNode(node);
            var awaitableInfo = (bound as BoundUsingLocalDeclarations)?.AwaitOpt;
            return GetAwaitExpressionInfo(awaitableInfo);
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(UsingStatementSyntax node)
        {
            if (node.AwaitKeyword.IsKind(SyntaxKind.None))
            {
                throw new ArgumentException(CSharpResources.NodeIsNotAwaitUsingStatement, nameof(node));
            }

            var bound = GetLowerBoundNode(node);
            var awaitableInfo = (bound as BoundUsingStatement)?.AwaitOpt;
            return GetAwaitExpressionInfo(awaitableInfo);
        }

        private static AwaitExpressionInfo GetAwaitExpressionInfo(BoundAwaitableInfo? awaitableInfo)
        {
            if (awaitableInfo == null)
            {
                return default(AwaitExpressionInfo);
            }

            return new AwaitExpressionInfo(
                getAwaiter: (IMethodSymbol?)awaitableInfo.GetAwaiter?.ExpressionSymbol.GetPublicSymbol(),
                isCompleted: awaitableInfo.IsCompleted.GetPublicSymbol(),
                getResult: awaitableInfo.GetResult.GetPublicSymbol(),
                runtimeAwaitMethod: awaitableInfo.RuntimeAsyncAwaitCall?.Method.GetPublicSymbol(),
                isDynamic: awaitableInfo.IsDynamic);
        }
#nullable disable

        public override ForEachStatementInfo GetForEachStatementInfo(ForEachStatementSyntax node)
        {
            return GetForEachStatementInfo((CommonForEachStatementSyntax)node);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(CommonForEachStatementSyntax node)
        {
            BoundForEachStatement boundForEach = (BoundForEachStatement)GetUpperBoundNode(node);

            if (boundForEach == null)
            {
                return default(ForEachStatementInfo);
            }

            ForEachEnumeratorInfo enumeratorInfoOpt = boundForEach.EnumeratorInfoOpt;

            Debug.Assert(enumeratorInfoOpt != null || boundForEach.HasAnyErrors);

            if (enumeratorInfoOpt == null)
            {
                return default(ForEachStatementInfo);
            }

            // Even though we usually pretend to be using System.Collection.IEnumerable
            // for arrays, that doesn't make sense for pointer arrays since object
            // (the type of System.Collections.IEnumerator.Current) isn't convertible
            // to pointer types.
            if (enumeratorInfoOpt.CurrentConversion is null && enumeratorInfoOpt.ElementType.IsPointerType())
            {
                return default(ForEachStatementInfo);
            }

            var moveNextAwaitableInfo = GetAwaitExpressionInfo(enumeratorInfoOpt.MoveNextAwaitableInfo);

            // NOTE: we're going to list GetEnumerator, etc for array and string
            // collections, even though we know that's not how the implementation
            // actually enumerates them.
            MethodSymbol disposeMethod = null;
            AwaitExpressionInfo disposeAwaitableInfo = default;
            if (enumeratorInfoOpt.NeedsDisposal)
            {
                disposeAwaitableInfo = GetAwaitExpressionInfo(enumeratorInfoOpt.DisposeAwaitableInfo);
                if (enumeratorInfoOpt.PatternDisposeInfo is { Method: var method })
                {
                    disposeMethod = method;
                }
                else
                {
                    disposeMethod = enumeratorInfoOpt.IsAsync
                    ? (MethodSymbol)Compilation.GetWellKnownTypeMember(WellKnownMember.System_IAsyncDisposable__DisposeAsync)
                    : (MethodSymbol)Compilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose);
                }
            }

            return new ForEachStatementInfo(
                enumeratorInfoOpt.IsAsync,
                enumeratorInfoOpt.GetEnumeratorInfo.Method.GetPublicSymbol(),
                enumeratorInfoOpt.MoveNextInfo.Method.GetPublicSymbol(),
                moveNextAwaitableInfo,
                currentProperty: ((PropertySymbol)enumeratorInfoOpt.CurrentPropertyGetter?.AssociatedSymbol).GetPublicSymbol(),
                disposeMethod.GetPublicSymbol(),
                disposeAwaitableInfo,
                enumeratorInfoOpt.ElementTypeWithAnnotations.GetPublicSymbol(),
                BoundNode.GetConversion(boundForEach.ElementConversion, boundForEach.ElementPlaceholder),
                BoundNode.GetConversion(enumeratorInfoOpt.CurrentConversion, enumeratorInfoOpt.CurrentPlaceholder));
        }

        public override DeconstructionInfo GetDeconstructionInfo(AssignmentExpressionSyntax node)
        {
            var boundDeconstruction = GetUpperBoundNode(node) as BoundDeconstructionAssignmentOperator;
            if (boundDeconstruction is null)
            {
                return default;
            }

            var boundConversion = boundDeconstruction.Right;
            Debug.Assert(boundConversion != null);
            if (boundConversion is null)
            {
                return default;
            }

            return new DeconstructionInfo(boundConversion.Conversion);
        }

        public override DeconstructionInfo GetDeconstructionInfo(ForEachVariableStatementSyntax node)
        {
            var boundForEach = (BoundForEachStatement)GetUpperBoundNode(node);
            if (boundForEach is null)
            {
                return default;
            }

            var boundDeconstruction = boundForEach.DeconstructionOpt;
            Debug.Assert(boundDeconstruction != null || boundForEach.HasAnyErrors);
            if (boundDeconstruction is null)
            {
                return default;
            }

            return new DeconstructionInfo(boundDeconstruction.DeconstructionAssignment.Right.Conversion);
        }

        private BoundQueryClause GetBoundQueryClause(CSharpSyntaxNode node)
        {
            CheckSyntaxNode(node);
            return this.GetLowerBoundNode(node) as BoundQueryClause;
        }

        private QueryClauseInfo GetQueryClauseInfo(BoundQueryClause bound)
        {
            if (bound == null) return default(QueryClauseInfo);
            var castInfo = (bound.Cast == null) ? SymbolInfo.None : GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, bound.Cast, bound.Cast, boundNodeForSyntacticParent: null, binderOpt: null);
            var operationInfo = GetSymbolInfoForQuery(bound);
            return new QueryClauseInfo(castInfo: castInfo, operationInfo: operationInfo);
        }

        private SymbolInfo GetSymbolInfoForQuery(BoundQueryClause bound)
        {
            var call = bound?.Operation as BoundCall;
            if (call == null)
            {
                return SymbolInfo.None;
            }

            var operation = call.IsDelegateCall ? call.ReceiverOpt : call;
            return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, operation, operation, boundNodeForSyntacticParent: null, binderOpt: null);
        }

        private CSharpTypeInfo GetTypeInfoForQuery(BoundQueryClause bound)
        {
            return bound == null ?
                CSharpTypeInfo.None :
                GetTypeInfoForNode(bound, bound, bound);
        }

        public override QueryClauseInfo GetQueryClauseInfo(QueryClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return GetQueryClauseInfo(bound);
        }

        public override IPropertySymbol GetDeclaredSymbol(AnonymousObjectMemberDeclaratorSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var anonymousObjectCreation = (AnonymousObjectCreationExpressionSyntax)declaratorSyntax.Parent;
            if (anonymousObjectCreation == null)
            {
                return null;
            }

            var bound = this.GetLowerBoundNode(anonymousObjectCreation) as BoundAnonymousObjectCreationExpression;
            if (bound == null)
            {
                return null;
            }

            var anonymousType = bound.Type as NamedTypeSymbol;
            if ((object)anonymousType == null)
            {
                return null;
            }

            int index = anonymousObjectCreation.Initializers.IndexOf(declaratorSyntax);
            Debug.Assert(index >= 0);
            Debug.Assert(index < anonymousObjectCreation.Initializers.Count);
            return AnonymousTypeManager.GetAnonymousTypeProperty(anonymousType, index).GetPublicSymbol();
        }

        public override INamedTypeSymbol GetDeclaredSymbol(AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var bound = this.GetLowerBoundNode(declaratorSyntax) as BoundAnonymousObjectCreationExpression;
            return (bound == null) ? null : (bound.Type as NamedTypeSymbol).GetPublicSymbol();
        }

        public override INamedTypeSymbol GetDeclaredSymbol(TupleExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            return GetTypeOfTupleLiteral(declaratorSyntax).GetPublicSymbol();
        }

        public override ISymbol GetDeclaredSymbol(ArgumentSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);

            var tupleLiteral = declaratorSyntax?.Parent as TupleExpressionSyntax;

            // for now only arguments of a tuple literal may declare symbols
            if (tupleLiteral == null)
            {
                return null;
            }

            var tupleLiteralType = GetTypeOfTupleLiteral(tupleLiteral);

            if ((object)tupleLiteralType != null)
            {
                var elements = tupleLiteralType.TupleElements;

                if (!elements.IsDefault)
                {
                    var idx = tupleLiteral.Arguments.IndexOf(declaratorSyntax);
                    return elements[idx].GetPublicSymbol();
                }
            }

            return null;
        }

        private NamedTypeSymbol GetTypeOfTupleLiteral(TupleExpressionSyntax declaratorSyntax)
        {
            var bound = this.GetLowerBoundNode(declaratorSyntax);

            return (bound as BoundTupleExpression)?.Type as NamedTypeSymbol;
        }

        public override SyntaxTree SyntaxTree
        {
            get
            {
                return _root.SyntaxTree;
            }
        }

#nullable enable
        internal override IOperation? GetOperationWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            using (_nodeMapLock.DisposableRead())
            {
                if (_guardedIOperationNodeMap.Count != 0)
                {
                    return guardedGetIOperation();
                }
            }

            IOperation rootOperation = GetRootOperation();

            using var _ = _nodeMapLock.DisposableWrite();

            if (_guardedIOperationNodeMap.Count != 0)
            {
                return guardedGetIOperation();
            }

            OperationMapBuilder.AddToMap(rootOperation, _guardedIOperationNodeMap);
            return guardedGetIOperation();

            IOperation? guardedGetIOperation()
            {
                _nodeMapLock.AssertCanRead();
                return _guardedIOperationNodeMap.TryGetValue(node, out var operation) ? operation : null;
            }
        }

        private IOperation GetRootOperation()
        {
            BoundNode highestBoundNode = GetBoundRoot();
            Debug.Assert(highestBoundNode != null);

            if (highestBoundNode is BoundGlobalStatementInitializer { Statement: var innerStatement })
            {
                // Script top-level field declarations use a BoundGlobalStatementInitializer to wrap initializers.
                // We don't represent these nodes in IOperation, so skip it.
                highestBoundNode = innerStatement;
            }

            // The CSharp operation factory assumes that UnboundLambda will be bound for error recovery and never be passed to the factory
            // as the start of a tree to get operations for. This is guaranteed by the builder that populates the node map, as it will call
            // UnboundLambda.BindForErrorRecovery() when it encounters an UnboundLambda node.
            Debug.Assert(highestBoundNode.Kind != BoundKind.UnboundLambda);
            IOperation operation = _operationFactory.Value.Create(highestBoundNode);
            Operation.SetParentOperation(operation, null);
            return operation;
        }
#nullable disable

        internal override SymbolInfo GetSymbolInfoWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateSymbolInfoOptions(options);

            CSharpSyntaxNode bindableNode;
            BoundNode lowestBoundNode;
            BoundNode highestBoundNode;
            BoundNode boundParent;
            GetBoundNodes(node, out bindableNode, out lowestBoundNode, out highestBoundNode, out boundParent);

            Debug.Assert(IsInTree(node), "Since the node is in the tree, we can always recompute the binder later");
            return base.GetSymbolInfoForNode(options, lowestBoundNode, highestBoundNode, boundParent, binderOpt: null);
        }

        internal override CSharpTypeInfo GetTypeInfoWorker(CSharpSyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            CSharpSyntaxNode bindableNode;
            BoundNode lowestBoundNode;
            BoundNode highestBoundNode;
            BoundNode boundParent;
            GetBoundNodes(node, out bindableNode, out lowestBoundNode, out highestBoundNode, out boundParent);

            return GetTypeInfoForNode(lowestBoundNode, highestBoundNode, boundParent);
        }

        internal override ImmutableArray<Symbol> GetMemberGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            CSharpSyntaxNode bindableNode;
            BoundNode lowestBoundNode;
            BoundNode highestBoundNode;
            BoundNode boundParent;
            GetBoundNodes(node, out bindableNode, out lowestBoundNode, out highestBoundNode, out boundParent);

            Debug.Assert(IsInTree(node), "Since the node is in the tree, we can always recompute the binder later");
            return base.GetMemberGroupForNode(options, lowestBoundNode, boundParent, binderOpt: null);
        }

        internal override ImmutableArray<IPropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            CSharpSyntaxNode bindableNode;
            BoundNode lowestBoundNode;
            BoundNode highestBoundNode;
            BoundNode boundParent;
            GetBoundNodes(node, out bindableNode, out lowestBoundNode, out highestBoundNode, out boundParent);

            Debug.Assert(IsInTree(node), "Since the node is in the tree, we can always recompute the binder later");
            return base.GetIndexerGroupForNode(lowestBoundNode, binderOpt: null);
        }

        internal override Optional<object> GetConstantValueWorker(CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            CSharpSyntaxNode bindableNode = this.GetBindableSyntaxNode(node);
            BoundExpression boundExpr = this.GetLowerBoundNode(bindableNode) as BoundExpression;

            if (boundExpr == null) return default(Optional<object>);

            ConstantValue constantValue = boundExpr.ConstantValueOpt;
            return constantValue == null || constantValue.IsBad
                ? default(Optional<object>)
                : new Optional<object>(constantValue.Value);
        }

        internal override SymbolInfo GetCollectionInitializerSymbolInfoWorker(InitializerExpressionSyntax collectionInitializer, ExpressionSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var boundCollectionInitializer = GetLowerBoundNode(collectionInitializer) as BoundCollectionInitializerExpression;

            if (boundCollectionInitializer != null)
            {
                var boundAdd = boundCollectionInitializer.Initializers[collectionInitializer.Expressions.IndexOf(node)];

                return GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, boundAdd, boundAdd, null, binderOpt: null);
            }

            return SymbolInfo.None;
        }

        public override SymbolInfo GetSymbolInfo(OrderingSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return GetSymbolInfoForQuery(bound);
        }

        public override SymbolInfo GetSymbolInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return GetSymbolInfoForQuery(bound);
        }

        public override TypeInfo GetTypeInfo(SelectOrGroupClauseSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return GetTypeInfoForQuery(bound);
        }

        private void GetBoundNodes(CSharpSyntaxNode node, out CSharpSyntaxNode bindableNode, out BoundNode lowestBoundNode, out BoundNode highestBoundNode, out BoundNode boundParent)
        {
            bindableNode = this.GetBindableSyntaxNode(node);

            CSharpSyntaxNode bindableParent = this.GetBindableParentNode(bindableNode);

            // Special handling for the Color Color case.
            //
            // Suppose we have:
            // public class Color {
            //   public void M(int x) {}
            //   public static void M(params int[] x) {}
            // }
            // public class C {
            //   public void Test() {
            //     Color Color = new Color();
            //     System.Action<int> d = Color.M;
            //   }
            // }
            //
            // We actually don't know how to interpret the "Color" in "Color.M" until we
            // perform overload resolution on the method group.  Now, if we were getting
            // the semantic info for the method group, then bindableParent would be the
            // variable declarator "d = Color.M" and so we would be able to pull the result
            // of overload resolution out of the bound (method group) conversion.  However,
            // if we are getting the semantic info for just the "Color" part, then
            // bindableParent will be the member access, which doesn't have enough information
            // to determine which "Color" to use (since no overload resolution has been
            // performed).  We resolve this problem by detecting the case where we're looking
            // up the LHS of a member access and calling GetBindableParentNode one more time.
            // This gets us up to the level where the method group conversion occurs.
            if (bindableParent != null && bindableParent.Kind() == SyntaxKind.SimpleMemberAccessExpression && ((MemberAccessExpressionSyntax)bindableParent).Expression == bindableNode)
            {
                bindableParent = this.GetBindableParentNode(bindableParent);
            }

            boundParent = bindableParent == null ? null : this.GetLowerBoundNode(bindableParent);

            lowestBoundNode = this.GetLowerBoundNode(bindableNode);
            highestBoundNode = this.GetUpperBoundNode(bindableNode);
        }

        // In lambda binding scenarios we need to know two things: First,
        // what is the *innermost* lambda that contains the expression we're
        // interested in?  Second, what is the smallest expression that contains 
        // the *outermost* lambda that we can bind in order to get a sensible
        // lambda binding?
        //
        // For example, suppose we have the statement:
        //
        // A().B(x=>x.C(y=>y.D().E())).F().G();
        //
        // and the user wants binding information about method group "D".  We must know
        // the bindable expression that is outside of every lambda:
        //
        // A().B(x=>x.C(y=>y.D().E()))
        //
        // By binding that we can determine the type of lambda parameters "x" and "y" and
        // put that information in the bound tree. Once we know those facts then
        // we can obtain the binding object associated with the innermost lambda:
        //
        // y=>y.D().E()
        //
        // And use that binding to obtain the analysis of:
        //
        // y.D
        //
        private CSharpSyntaxNode GetInnermostLambdaOrQuery(CSharpSyntaxNode node, int position, bool allowStarting = false)
        {
            Debug.Assert(node != null);

            for (var current = node; current != this.Root; current = current.ParentOrStructuredTriviaParent)
            {
                // current can only become null if we somehow got past the root. The only way we
                // could have gotten past the root is to have started outside of it. That's
                // unexpected; the binding should only be asked to provide an opinion on syntax
                // nodes that it knows about.
                Debug.Assert(current != null, "Why are we being asked to find an enclosing lambda outside of our root?");

                if (!(current.IsAnonymousFunction() || current.IsQuery()))
                {
                    continue;
                }

                // If the position is not actually within the scope of the lambda, then keep
                // looking.
                if (!LookupPosition.IsInAnonymousFunctionOrQuery(position, current))
                {
                    continue;
                }

                // If we were asked for the innermost lambda enclosing a lambda then don't return
                // that; it's not enclosing anything. Only return the lambda if it's enclosing the
                // original node.

                if (!allowStarting && current == node)
                {
                    continue;
                }

                return current;
            }

            // If we made it to the root, then we are not "inside" a lambda even if the root is a
            // lambda. Remember, the point of this code is to get the binding that is associated
            // with the innermost lambda; if we are already in a binding associated with the
            // innermost lambda then we're done.
            return null;
        }

        private void GuardedAddSynthesizedStatementToMap(StatementSyntax node, BoundStatement statement)
        {
            if (_lazyGuardedSynthesizedStatementsMap == null)
            {
                _lazyGuardedSynthesizedStatementsMap = new Dictionary<SyntaxNode, BoundStatement>();
            }

            _lazyGuardedSynthesizedStatementsMap.Add(node, statement);
        }

        private BoundStatement GuardedGetSynthesizedStatementFromMap(StatementSyntax node)
        {
            if (_lazyGuardedSynthesizedStatementsMap != null &&
                _lazyGuardedSynthesizedStatementsMap.TryGetValue(node, out BoundStatement result))
            {
                return result;
            }

            return null;
        }

        private OneOrMany<BoundNode> GuardedGetBoundNodesFromMap(CSharpSyntaxNode node)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld || _nodeMapLock.IsReadLockHeld);
            OneOrMany<BoundNode> result;
            return _guardedBoundNodeMap.TryGetValue(node, out result) ? result : OneOrMany<BoundNode>.Empty;
        }

        /// <summary>
        /// Internal for test purposes only
        /// </summary>
        internal OneOrMany<BoundNode> TestOnlyTryGetBoundNodesFromMap(CSharpSyntaxNode node)
        {
            OneOrMany<BoundNode> result;
            return _guardedBoundNodeMap.TryGetValue(node, out result) ? result : OneOrMany<BoundNode>.Empty;
        }

        // Adds every syntax/bound pair in a tree rooted at the given bound node to the map, and the
        // performs a lookup of the given syntax node in the map. 
        private OneOrMany<BoundNode> GuardedAddBoundTreeAndGetBoundNodeFromMap(CSharpSyntaxNode syntax, BoundNode bound)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld);

            bool alreadyInTree = false;

            if (bound != null)
            {
                alreadyInTree = _guardedBoundNodeMap.ContainsKey(bound.Syntax);
            }

            // check if we already have node in the cache.
            // this may happen if we have races and in such case we are no longer interested in adding
            if (!alreadyInTree)
            {
                NodeMapBuilder.AddToMap(bound, _guardedBoundNodeMap, SyntaxTree);
                Debug.Assert(syntax != _root || _guardedBoundNodeMap.ContainsKey(bound.Syntax));
            }

            OneOrMany<BoundNode> result;
            return _guardedBoundNodeMap.TryGetValue(syntax, out result) ? result : OneOrMany<BoundNode>.Empty;
        }

        protected void UnguardedAddBoundTreeForStandaloneSyntax(SyntaxNode syntax, BoundNode bound, NullableWalker.SnapshotManager manager = null, ImmutableDictionary<Symbol, Symbol> remappedSymbols = null)
        {
            using (_nodeMapLock.DisposableWrite())
            {
                GuardedAddBoundTreeForStandaloneSyntax(syntax, bound, manager, remappedSymbols);
            }
        }

        protected void GuardedAddBoundTreeForStandaloneSyntax(SyntaxNode syntax, BoundNode bound, NullableWalker.SnapshotManager manager = null, ImmutableDictionary<Symbol, Symbol> remappedSymbols = null)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld);
            bool alreadyInTree = false;

            // check if we already have node in the cache.
            // this may happen if we have races and in such case we are no longer interested in adding
            if (bound != null)
            {
                alreadyInTree = _guardedBoundNodeMap.ContainsKey(bound.Syntax);
            }

            if (!alreadyInTree)
            {
                if (syntax == _root || syntax is StatementSyntax)
                {
                    // Note: For speculative model we want to always cache the entire bound tree.
                    // If syntax is a statement, we need to add all its children.
                    // Node cache assumes that if statement is cached, then all 
                    // its children are cached too.
                    NodeMapBuilder.AddToMap(bound, _guardedBoundNodeMap, SyntaxTree);
                    Debug.Assert(syntax != _root || _guardedBoundNodeMap.ContainsKey(bound.Syntax));
                }
                else
                {
                    // expressions can be added individually.
                    NodeMapBuilder.AddToMap(bound, _guardedBoundNodeMap, SyntaxTree, syntax);
                }

                Debug.Assert((manager is null && (!IsNullableAnalysisEnabled() || syntax != Root || syntax is TypeSyntax ||
                                                  // Supporting attributes is tracked by
                                                  // https://github.com/dotnet/roslyn/issues/36066
                                                  this is AttributeSemanticModel)) ||
                             (manager is object && remappedSymbols is object && syntax == Root && IsNullableAnalysisEnabled() && _lazySnapshotManager is null));
                if (manager is object)
                {
                    _lazySnapshotManager = manager;
                    _lazyRemappedSymbols = remappedSymbols;
                }
            }
        }

        // We might not have actually been given a bindable expression or statement; the caller can
        // give us variable declaration nodes, for example. If we're not at an expression or
        // statement, back up until we find one.
        private CSharpSyntaxNode GetBindingRoot(CSharpSyntaxNode node)
        {
            Debug.Assert(node != null);

#if DEBUG
            for (CSharpSyntaxNode current = node; current != this.Root; current = current.ParentOrStructuredTriviaParent)
            {
                // make sure we never go out of Root
                Debug.Assert(current != null, "How did we get outside the root?");
            }
#endif

            for (CSharpSyntaxNode current = node; current != this.Root; current = current.ParentOrStructuredTriviaParent)
            {
                if (current is StatementSyntax)
                {
                    return current;
                }

                switch (current.Kind())
                {
                    case SyntaxKind.ThisConstructorInitializer:
                    case SyntaxKind.BaseConstructorInitializer:
                    case SyntaxKind.PrimaryConstructorBaseType:
                        return current;
                    case SyntaxKind.ArrowExpressionClause:
                        // If this is an arrow expression on a local function statement, then our bindable root is actually our parent syntax as it's
                        // a statement in a function. If this is returned directly in IOperation, we'll end up with a separate tree.
                        if (current.Parent == null || current.Parent.Kind() != SyntaxKind.LocalFunctionStatement)
                        {
                            return current;
                        }
                        break;
                }
            }

            return this.Root;
        }

        // We want the binder in which this syntax node is going to be bound, NOT the binder which
        // this syntax node *produces*. That is, suppose we have
        //
        // void M() { int x; { int y; { int z; } } } 
        //
        // We want the enclosing binder of the syntax node for { int z; }.  We do not want the binder
        // that has local z, but rather the binder that has local y. The inner block is going to be
        // bound in the context of its enclosing binder; it's contents are going to be bound in the
        // context of its binder.
        internal override Binder GetEnclosingBinderInternal(int position)
        {
            AssertPositionAdjusted(position);

            // If we have a root binder with no tokens in it, position can be outside the span event
            // after position is adjusted. If this happens, there can't be any 
            if (!this.Root.FullSpan.Contains(position))
                return this.RootBinder;

            SyntaxToken token = this.Root.FindToken(position);
            CSharpSyntaxNode node = (CSharpSyntaxNode)token.Parent;

            return GetEnclosingBinderInternal(node, position);
        }

        /// <summary>
        /// This overload exists for callers who already have a node in hand 
        /// and don't want to search through the tree.
        /// </summary>
        private Binder GetEnclosingBinderInternal(CSharpSyntaxNode node, int position)
        {
            AssertPositionAdjusted(position);

            CSharpSyntaxNode innerLambdaOrQuery = GetInnermostLambdaOrQuery(node, position, allowStarting: true);

            // There are three possible scenarios here.
            //
            // 1) the node is outside all lambdas in this context, or
            // 2) The node is an outermost lambda in this context, or
            // 3) the node is inside the outermost lambda in this context.
            //
            // In the first case, no lambdas are involved at all so let's just fall back on the
            // original enclosing binder code.
            //
            // In the second case, we have been asked to bind an entire lambda and we know it to be
            // the outermost lambda in this context. Therefore the enclosing binder is going to be
            // the enclosing binder of this expression. However, we do not simply want to say
            // "here's the enclosing binder":
            // 
            // void M() { Func<int, int> f = x=>x+1; }
            //
            // We should step out to the enclosing statement or expression, if there is one, and
            // bind that.

            if (innerLambdaOrQuery == null)
            {
                return GetEnclosingBinderInternalWithinRoot(node, position);
            }

            // In the third case, we're in a child lambda. 
            BoundNode boundInnerLambdaOrQuery = GetBoundLambdaOrQuery(innerLambdaOrQuery);
            return GetEnclosingBinderInLambdaOrQuery(position, node, innerLambdaOrQuery, ref boundInnerLambdaOrQuery);
        }

        private BoundNode GetBoundLambdaOrQuery(CSharpSyntaxNode lambdaOrQuery)
        {
            // Have we already cached a bound node for it?
            // If not, bind the outermost expression containing the lambda and then fill in the map.
            OneOrMany<BoundNode> nodes;

            EnsureNullabilityAnalysisPerformedIfNecessary();

            using (_nodeMapLock.DisposableRead())
            {
                nodes = GuardedGetBoundNodesFromMap(lambdaOrQuery);
            }

            if (!nodes.IsEmpty)
            {
                return GetLowerBoundNode(nodes);
            }

            // We probably never tried to bind an enclosing statement
            // Let's do that
            Binder lambdaRecoveryBinder;
            CSharpSyntaxNode bindingRoot = GetBindingRoot(lambdaOrQuery);
            CSharpSyntaxNode enclosingLambdaOrQuery = GetInnermostLambdaOrQuery(lambdaOrQuery, lambdaOrQuery.SpanStart, allowStarting: false);
            BoundNode boundEnclosingLambdaOrQuery = null;
            CSharpSyntaxNode nodeToBind;

            if (enclosingLambdaOrQuery == null)
            {
                nodeToBind = bindingRoot;
                lambdaRecoveryBinder = GetEnclosingBinderInternalWithinRoot(nodeToBind, GetAdjustedNodePosition(nodeToBind));
            }
            else
            {
                if (enclosingLambdaOrQuery == bindingRoot || !enclosingLambdaOrQuery.Contains(bindingRoot))
                {
                    Debug.Assert(bindingRoot.Contains(enclosingLambdaOrQuery));
                    nodeToBind = lambdaOrQuery;
                }
                else
                {
                    nodeToBind = bindingRoot;
                }

                boundEnclosingLambdaOrQuery = GetBoundLambdaOrQuery(enclosingLambdaOrQuery);

                using (_nodeMapLock.DisposableRead())
                {
                    nodes = GuardedGetBoundNodesFromMap(lambdaOrQuery);
                }

                if (!nodes.IsEmpty)
                {
                    // If everything is working as expected we should end up here because binding the enclosing lambda
                    // should also take care of binding and caching this lambda.
                    return GetLowerBoundNode(nodes);
                }

                lambdaRecoveryBinder = GetEnclosingBinderInLambdaOrQuery(GetAdjustedNodePosition(nodeToBind), nodeToBind, enclosingLambdaOrQuery, ref boundEnclosingLambdaOrQuery);
            }

            Binder incrementalBinder = new IncrementalBinder(this, lambdaRecoveryBinder);

            using (_nodeMapLock.DisposableWrite())
            {
                BoundNode boundOuterExpression = this.Bind(incrementalBinder, nodeToBind, BindingDiagnosticBag.Discarded);

                // https://github.com/dotnet/roslyn/issues/35038: Rewrite the above node and add a test that hits this path with nullable
                // enabled

                nodes = GuardedAddBoundTreeAndGetBoundNodeFromMap(lambdaOrQuery, boundOuterExpression);
            }

            if (!nodes.IsEmpty)
            {
                return GetLowerBoundNode(nodes);
            }

            Debug.Assert(lambdaOrQuery != nodeToBind);

            // If there is a bug in the binder such that we "lose" a sub-expression containing a
            // lambda, and never put bound state for it into the bound tree, then the bound lambda
            // that comes back from the map lookup will be null. This can occur in error recovery
            // situations. Let's bind the node directly.
            if (enclosingLambdaOrQuery == null)
            {
                lambdaRecoveryBinder = GetEnclosingBinderInternalWithinRoot(lambdaOrQuery, GetAdjustedNodePosition(lambdaOrQuery));
            }
            else
            {
                lambdaRecoveryBinder = GetEnclosingBinderInLambdaOrQuery(GetAdjustedNodePosition(lambdaOrQuery), lambdaOrQuery, enclosingLambdaOrQuery, ref boundEnclosingLambdaOrQuery);
            }

            incrementalBinder = new IncrementalBinder(this, lambdaRecoveryBinder);

            using (_nodeMapLock.DisposableWrite())
            {
                BoundNode boundOuterExpression = this.Bind(incrementalBinder, lambdaOrQuery, BindingDiagnosticBag.Discarded);

                // https://github.com/dotnet/roslyn/issues/35038: We need to do a rewrite here, and create a test that can hit this.
                if (!IsNullableAnalysisEnabled() && Compilation.IsNullableAnalysisEnabledAlways)
                {
                    AnalyzeBoundNodeNullability(boundOuterExpression, incrementalBinder, diagnostics: new DiagnosticBag(), createSnapshots: false);
                }

                nodes = GuardedAddBoundTreeAndGetBoundNodeFromMap(lambdaOrQuery, boundOuterExpression);
            }

            return GetLowerBoundNode(nodes);
        }

        private Binder GetEnclosingBinderInLambdaOrQuery(int position, CSharpSyntaxNode node, CSharpSyntaxNode innerLambdaOrQuery, ref BoundNode boundInnerLambdaOrQuery)
        {
            Debug.Assert(boundInnerLambdaOrQuery != null);
            Binder result;
            switch (boundInnerLambdaOrQuery.Kind)
            {
                case BoundKind.UnboundLambda:
                    boundInnerLambdaOrQuery = ((UnboundLambda)boundInnerLambdaOrQuery).BindForErrorRecovery();
                    goto case BoundKind.Lambda;
                case BoundKind.Lambda:
                    AssertPositionAdjusted(position);
                    result = GetLambdaEnclosingBinder(position, node, innerLambdaOrQuery, ((BoundLambda)boundInnerLambdaOrQuery).Binder);
                    break;
                case BoundKind.QueryClause:
                    result = GetQueryEnclosingBinder(position, node, ((BoundQueryClause)boundInnerLambdaOrQuery));
                    break;
                default:
                    return GetEnclosingBinderInternalWithinRoot(node, position); // Known to return non-null with BinderFlags.SemanticModel.
            }

            Debug.Assert(result != null);
            return result.WithAdditionalFlags(GetSemanticModelBinderFlags());
        }

        /// <remarks>
        /// Returned binder doesn't need to have <see cref="BinderFlags.SemanticModel"/> set - the caller will add it.
        /// </remarks>
        private static Binder GetQueryEnclosingBinder(int position, CSharpSyntaxNode startingNode, BoundQueryClause queryClause)
        {
            BoundExpression node = queryClause;

            do
            {
                switch (node.Kind)
                {
                    case BoundKind.QueryClause:
                        queryClause = (BoundQueryClause)node;
                        node = GetQueryClauseValue(queryClause);
                        continue;
                    case BoundKind.Call:
                        var call = (BoundCall)node;
                        node = GetContainingArgument(call.Arguments, position);
                        if (node != null)
                        {
                            continue;
                        }

                        BoundExpression receiver = call.ReceiverOpt;

                        // In some error scenarios, we end-up with a method group as the receiver,
                        // let's get to real receiver.
                        while (receiver?.Kind == BoundKind.MethodGroup)
                        {
                            receiver = ((BoundMethodGroup)receiver).ReceiverOpt;
                        }

                        if (receiver != null)
                        {
                            node = GetContainingExprOrQueryClause(receiver, position);
                            if (node != null)
                            {
                                continue;
                            }
                        }

                        // TODO: should we look for the "nearest" argument as a fallback?
                        node = call.Arguments.LastOrDefault();
                        continue;
                    case BoundKind.Conversion:
                        node = ((BoundConversion)node).Operand;
                        continue;
                    case BoundKind.UnboundLambda:
                        var unbound = (UnboundLambda)node;
                        return GetEnclosingBinderInternalWithinRoot(AdjustStartingNodeAccordingToNewRoot(startingNode, unbound.Syntax),
                                                  position, unbound.BindForErrorRecovery().Binder, unbound.Syntax);
                    case BoundKind.Lambda:
                        var lambda = (BoundLambda)node;
                        return GetEnclosingBinderInternalWithinRoot(AdjustStartingNodeAccordingToNewRoot(startingNode, lambda.Body.Syntax),
                                                  position, lambda.Binder, lambda.Body.Syntax);
                    default:
                        goto done;
                }
            }
            while (node != null);

done:
            return GetEnclosingBinderInternalWithinRoot(AdjustStartingNodeAccordingToNewRoot(startingNode, queryClause.Syntax),
                                      position, queryClause.Binder, queryClause.Syntax);
        }

        // Return the argument containing the position. For query
        // expressions, the span of an argument may include other
        // arguments, so the argument with the smallest span is returned.
        private static BoundExpression GetContainingArgument(ImmutableArray<BoundExpression> arguments, int position)
        {
            BoundExpression result = null;
            TextSpan resultSpan = default(TextSpan);
            foreach (var arg in arguments)
            {
                var expr = GetContainingExprOrQueryClause(arg, position);
                if (expr != null)
                {
                    var span = expr.Syntax.FullSpan;
                    if (result == null || resultSpan.Contains(span))
                    {
                        result = expr;
                        resultSpan = span;
                    }
                }
            }
            return result;
        }

        // Returns the expr if the syntax span contains the position;
        // returns the BoundQueryClause value if expr is a BoundQueryClause
        // and the value contains the position; otherwise returns null.
        private static BoundExpression GetContainingExprOrQueryClause(BoundExpression expr, int position)
        {
            if (expr.Kind == BoundKind.QueryClause)
            {
                var value = GetQueryClauseValue((BoundQueryClause)expr);
                if (value.Syntax.FullSpan.Contains(position))
                {
                    return value;
                }
            }
            if (expr.Syntax.FullSpan.Contains(position))
            {
                return expr;
            }
            return null;
        }

        private static BoundExpression GetQueryClauseValue(BoundQueryClause queryClause)
        {
            return queryClause.UnoptimizedForm ?? queryClause.Value;
        }

        private static SyntaxNode AdjustStartingNodeAccordingToNewRoot(SyntaxNode startingNode, SyntaxNode root)
        {
            SyntaxNode result = startingNode.Contains(root) ? root : startingNode;
            if (result != root && !root.Contains(result))
            {
                result = root;
            }

            return result;
        }

        /// <summary>
        /// Performs the same function as GetEnclosingBinder, but is known to take place within a
        /// specified lambda.  Walks up the syntax hierarchy until a node with an associated binder
        /// is found.
        /// </summary>
        /// <remarks>
        /// CONSIDER: can this share code with MemberSemanticModel.GetEnclosingBinder?
        /// 
        /// Returned binder doesn't need to have <see cref="BinderFlags.SemanticModel"/> set - the caller will add it.
        /// </remarks>
        private static Binder GetLambdaEnclosingBinder(int position, CSharpSyntaxNode startingNode, CSharpSyntaxNode containingLambda, Binder lambdaBinder)
        {
            Debug.Assert(containingLambda.IsAnonymousFunction());
            Debug.Assert(LookupPosition.IsInAnonymousFunctionOrQuery(position, containingLambda));

            return GetEnclosingBinderInternalWithinRoot(startingNode, position, lambdaBinder, containingLambda);
        }

        /// <summary>
        /// If we're doing nullable analysis, we need to fully bind this member, and then run
        /// nullable analysis on the resulting nodes before putting them in the map. Nullable
        /// analysis does not run a subset of code, so we need to fully bind the entire member
        /// first
        /// </summary>
        protected void EnsureNullabilityAnalysisPerformedIfNecessary()
        {
#if !DEBUG
            // In release mode, when the semantic model options include DisableNullableAnalysis,
            // we want to completely avoid doing any work for nullable analysis.
#pragma warning disable RSEXPERIMENTAL001 // Internal usage of experimental API
            if (NullableAnalysisIsDisabled)
#pragma warning restore RSEXPERIMENTAL001
            {
                return;
            }
#endif

            bool isNullableAnalysisEnabled = IsNullableAnalysisEnabled();
            // When 'isNullableAnalysisEnabled' is false but 'Compilation.IsNullableAnalysisEnabledAlways' is true here,
            // we still need to perform a nullable analysis whose results are discarded for debug verification purposes.
            if (!isNullableAnalysisEnabled && !Compilation.IsNullableAnalysisEnabledAlways)
            {
                return;
            }

            // If we have a snapshot manager, then we've already done
            // all the work necessary and we should avoid taking an
            // unnecessary read lock.
            if (_lazySnapshotManager is object)
            {
                return;
            }

            var bindableRoot = GetBindableSyntaxNode(Root);
            using var upgradeableLock = _nodeMapLock.DisposableUpgradeableRead();

            // If there are already nodes in the map, then we've already done work here. Since
            // EnsureNullabilityAnalysis is guaranteed to have run first, that means we've
            // already bound the root node and we can just exit. We can't just assert that Root
            // is in the map, as there are some models for which there is no BoundNode for the
            // Root elements (such as fields, where the root is a VariableDeclarator but the
            // first BoundNode corresponds to the underlying EqualsValueSyntax of the initializer)
            if (_guardedBoundNodeMap.Count > 0)
            {
                Debug.Assert(!isNullableAnalysisEnabled ||
                             _guardedBoundNodeMap.ContainsKey(bindableRoot) ||
                             _guardedBoundNodeMap.ContainsKey(bind(bindableRoot, out _).Syntax));
                return;
            }

            upgradeableLock.EnterWrite();

            NullableWalker.SnapshotManager snapshotManager;
            var remappedSymbols = _parentRemappedSymbolsOpt;
            Binder binder;

            BoundNode boundRoot = bind(bindableRoot, out binder);

            if (IsSpeculativeSemanticModel)
            {
                // Not all speculative models are created with existing snapshots. Attributes,
                // TypeSyntaxes, and MethodBodies do not depend on existing state in a member,
                // and so the SnapshotManager can be null in these cases.
                var parentSnapshotManagerOpt = ((SpeculativeSemanticModelWithMemberModel)_containingPublicSemanticModel).ParentSnapshotManagerOpt;
                if (parentSnapshotManagerOpt is null || !isNullableAnalysisEnabled)
                {
                    rewriteAndCache();
                    return;
                }

                boundRoot = NullableWalker.AnalyzeAndRewriteSpeculation(_containingPublicSemanticModel.OriginalPositionForSpeculation, boundRoot, binder, parentSnapshotManagerOpt, out var newSnapshots, ref remappedSymbols);
                GuardedAddBoundTreeForStandaloneSyntax(bindableRoot, boundRoot, newSnapshots, remappedSymbols);
            }
            else
            {
                rewriteAndCache();
            }

            BoundNode bind(CSharpSyntaxNode root, out Binder binder)
            {
                binder = GetBinderToBindNode(root);
                return Bind(binder, root, BindingDiagnosticBag.Discarded);
            }

            void rewriteAndCache()
            {
                var diagnostics = DiagnosticBag.GetInstance();
#if DEBUG
                if (!isNullableAnalysisEnabled)
                {
                    Debug.Assert(Compilation.IsNullableAnalysisEnabledAlways);
                    AnalyzeBoundNodeNullability(boundRoot, binder, diagnostics, createSnapshots: true);
                    diagnostics.Free();
                    return;
                }
#endif

                boundRoot = RewriteNullableBoundNodesWithSnapshots(boundRoot, binder, diagnostics, createSnapshots: true, out snapshotManager, ref remappedSymbols);
                diagnostics.Free();
                GuardedAddBoundTreeForStandaloneSyntax(bindableRoot, boundRoot, snapshotManager, remappedSymbols);
            }
        }

        private Binder GetBinderToBindNode(CSharpSyntaxNode nodeToBind)
        {
            Binder binder;
            if (nodeToBind is CompilationUnitSyntax)
            {
                // Top level statements are unique among our nodes: if there are no syntax nodes before local functions,
                // then that means the start of the span of the top-level statement is the same as the start of the local
                // function. Therefore, GetEnclosingBinder can't tell the difference, and it will get the binder for the
                // local function, not for the CompilationUnitSyntax. This is desirable in almost all cases but this one:
                // There are no locals or invocations before this, meaning there's nothing to call GetDeclaredSymbol,
                // GetTypeInfo, or GetSymbolInfo on. GetDeclaredSymbol(CompilationUnitSyntax) goes down another path that
                // does not need to do any binding whatsoever, so it also doesn't care about this behavior. The only place
                // that actually needs to get the enclosing binding for a CompilationUnitSyntax in such a scenario is this
                // method. So, if our root is the CompilationUnitSyntax, directly get the binder for it.
                binder = RootBinder.GetBinder(nodeToBind);
                Debug.Assert(binder is SimpleProgramBinder);
            }
            else
            {
                binder = GetEnclosingBinder(GetAdjustedNodePosition(nodeToBind));
            }

            return binder;
        }

#nullable enable
        /// <summary>
        /// Rewrites the given bound node with nullability information, and returns snapshots for later speculative analysis at positions inside this member.
        /// </summary>
        protected abstract BoundNode RewriteNullableBoundNodesWithSnapshots(
            BoundNode boundRoot,
            Binder binder,
            DiagnosticBag diagnostics,
            bool createSnapshots,
            out NullableWalker.SnapshotManager? snapshotManager,
            ref ImmutableDictionary<Symbol, Symbol>? remappedSymbols);

        /// <summary>
        /// Performs the analysis step of getting nullability information for a semantic model but
        /// does not actually use the results. This gives us extra verification of nullable flow analysis.
        /// It is only used in contexts where nullable analysis is disabled in the compilation but requested
        /// through "run-nullable-analysis=always" or when the compiler is running in DEBUG.
        /// </summary>
        protected abstract void AnalyzeBoundNodeNullability(BoundNode boundRoot, Binder binder, DiagnosticBag diagnostics, bool createSnapshots);

        protected abstract bool IsNullableAnalysisEnabledCore();

        protected bool IsNullableAnalysisEnabled()
#pragma warning disable RSEXPERIMENTAL001 // internal use of experimental API
            => !NullableAnalysisIsDisabled && IsNullableAnalysisEnabledCore();
#pragma warning restore RSEXPERIMENTAL001
#nullable disable

        /// <summary>
        /// Get all bounds nodes associated with a node, ordered from highest to lowest in the bound tree.
        /// Strictly speaking, the order is that of a pre-order traversal of the bound tree.
        /// </summary>
        internal OneOrMany<BoundNode> GetBoundNodes(CSharpSyntaxNode node)
        {
            // If this method is called with a null parameter, that implies that the Root should be
            // bound, but make sure that the Root is bindable.
            if (node == null)
            {
                node = GetBindableSyntaxNode(Root);
            }
            Debug.Assert(node == GetBindableSyntaxNode(node));

            // Note: This nullability analysis can be distracting when debugging. This can be disabled with a feature flag in parse options:
            //       `.WithFeature("run-nullable-analysis", "never")` 
            EnsureNullabilityAnalysisPerformedIfNecessary();

            // We have one SemanticModel for each method.
            //
            // The SemanticModel contains a lazily-built immutable map from scope-introducing 
            // syntactic statements (such as blocks) to binders, but not from lambdas to binders.
            //
            // The SemanticModel also contains a mutable map from syntax to bound nodes; that is 
            // declared here. Since the map is not thread-safe we ensure that it is guarded with a
            // reader-writer lock.
            //
            // Have we already got the desired bound node in the mutable map? If so, return it.
            OneOrMany<BoundNode> results;

            using (_nodeMapLock.DisposableRead())
            {
                results = GuardedGetBoundNodesFromMap(node);
            }

            if (!results.IsEmpty)
            {
                return results;
            }

            // We might not actually have been given an expression or statement even though we were
            // allegedly given something that is "bindable".

            // If we didn't find in the cached bound nodes, find a binding root and bind it.
            // This will cache bound nodes under the binding root.
            CSharpSyntaxNode nodeToBind = GetBindingRoot(node);
            var statementBinder = GetBinderToBindNode(nodeToBind);
            Binder incrementalBinder = new IncrementalBinder(this, statementBinder);

            using (_nodeMapLock.DisposableWrite())
            {
                BoundNode boundStatement = this.Bind(incrementalBinder, nodeToBind, BindingDiagnosticBag.Discarded);
                results = GuardedAddBoundTreeAndGetBoundNodeFromMap(node, boundStatement);
            }

            if (!results.IsEmpty)
            {
                return results;
            }

            // If we still didn't find it, its still possible we could bind it directly.
            // For example, types are usually not represented by bound nodes, and some error conditions and
            // not yet implemented features do not create bound nodes for everything underneath them.
            //
            // In this case, however, we only add the single bound node we found to the map, not any child bound nodes,
            // to avoid duplicates in the map if a parent of this node comes through this code path also.

            var binder = GetBinderToBindNode(node);
            incrementalBinder = new IncrementalBinder(this, binder);

            using (_nodeMapLock.DisposableRead())
            {
                results = GuardedGetBoundNodesFromMap(node);
            }

            if (results.IsEmpty)
            {
                // https://github.com/dotnet/roslyn/issues/35038: We have to run analysis on this node in some manner
                using (_nodeMapLock.DisposableWrite())
                {
                    var boundNode = this.Bind(incrementalBinder, node, BindingDiagnosticBag.Discarded);
                    GuardedAddBoundTreeForStandaloneSyntax(node, boundNode);
                    results = GuardedGetBoundNodesFromMap(node);
                }

                if (!results.IsEmpty)
                {
                    return results;
                }
            }
            else
            {
                return results;
            }

            return OneOrMany<BoundNode>.Empty;
        }

        // some nodes don't have direct semantic meaning by themselves and so we need to bind a different node that does
        protected internal virtual CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.GlobalStatement:
                case SyntaxKind.Subpattern:
                    return node;
                case SyntaxKind.PositionalPatternClause:
                    return node.Parent;
            }

            while (true)
            {
                switch (node)
                {
                    case ParenthesizedExpressionSyntax n:
                        node = n.Expression;
                        continue;

                    case CheckedExpressionSyntax n:
                        node = n.Expression;
                        continue;

                    // Simple mitigation to give a result for suppressions. Public API tracked by https://github.com/dotnet/roslyn/issues/26198
                    case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } n:
                        node = n.Operand;
                        continue;

                    case UnsafeStatementSyntax n:
                        node = n.Block;
                        continue;

                    case CheckedStatementSyntax n:
                        node = n.Block;
                        continue;
                }

                break;
            }

            var parent = node.Parent;
            if (parent != null && node != this.Root)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        var tmp = SyntaxFactory.GetStandaloneNode(node);
                        if (tmp != node)
                        {
                            return GetBindableSyntaxNode(tmp);
                        }

                        break;

                    case SyntaxKind.AnonymousObjectMemberDeclarator:
                        return GetBindableSyntaxNode(parent);

                    case SyntaxKind.VariableDeclarator: // declarators are mapped in SyntaxBinder

                        // When a local variable declaration contains a single declarator, the bound node
                        // is associated with the declaration, rather than with the declarator.  If we
                        // used the declarator here, we would have enough context to bind it, but we wouldn't
                        // end up with an entry in the syntax-to-bound node map.
                        Debug.Assert(parent.Kind() == SyntaxKind.VariableDeclaration);
                        var grandparent = parent.Parent;
                        if (grandparent != null && grandparent.Kind() == SyntaxKind.LocalDeclarationStatement &&
                            ((VariableDeclarationSyntax)parent).Variables.Count == 1)
                        {
                            return GetBindableSyntaxNode(parent);
                        }
                        break;

                    case SyntaxKind.AddressOfExpression:
                        // When the parent of an AddressOf is an explicit cast, the bound node is associated with the cast, instead of the address of.
                        var bindableParent = GetBindableParentNode(node);
                        if (bindableParent.IsKind(SyntaxKind.CastExpression))
                        {
                            return bindableParent;
                        }
                        break;

                    default:
                        if (node is QueryExpressionSyntax && parent is QueryContinuationSyntax ||
                            !(node is ExpressionSyntax) &&
                            !(node is StatementSyntax) &&
                            !(node is SelectOrGroupClauseSyntax) &&
                            !(node is QueryClauseSyntax) &&
                            !(node is OrderingSyntax) &&
                            !(node is JoinIntoClauseSyntax) &&
                            !(node is QueryContinuationSyntax) &&
                            !(node is ConstructorInitializerSyntax) &&
                            !(node is PrimaryConstructorBaseTypeSyntax) &&
                            !(node is ArrowExpressionClauseSyntax) &&
                            !(node is PatternSyntax))
                        {
                            return GetBindableSyntaxNode(parent);
                        }

                        break;
                }
            }

            return node;
        }

#nullable enable
        /// <summary>
        /// If the node is an expression, return the nearest parent node
        /// with semantic meaning. Otherwise return null.
        /// </summary>
        protected CSharpSyntaxNode? GetBindableParentNode(CSharpSyntaxNode node)
        {
            if (!(node is ExpressionSyntax))
            {
                return null;
            }

            // The node is an expression, but its parent is null
            CSharpSyntaxNode? parent = node.Parent;
            if (parent == null)
            {
                // For speculative model, expression might be the root of the syntax tree, in which case it can have a null parent.
                if (this.IsSpeculativeSemanticModel && this.Root == node)
                {
                    return null;
                }

                throw new ArgumentException($"The parent of {nameof(node)} must not be null unless this is a speculative semantic model.", nameof(node));
            }

            // skip up past parens and ref expressions, as we have no bound nodes for them.
            while (true)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.ParenthesizedExpression:
                    case SyntaxKind.RefExpression:
                    case SyntaxKind.RefType:
                    case SyntaxKind.ScopedType:
                        var pp = parent.Parent;
                        if (pp == null) break;
                        parent = pp;
                        break;
                    default:
                        goto foundParent;
                }
            }
foundParent:;

            var bindableParent = this.GetBindableSyntaxNode(parent);
            Debug.Assert(bindableParent != null);

            // If the parent is a member used for a method invocation, then
            // the node is the instance associated with the method invocation.
            // In that case, return the invocation expression so that any conversion
            // of the receiver can be included in the resulting SemanticInfo.
            switch (bindableParent)
            {
                case { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Parent.RawKind: (int)SyntaxKind.InvocationExpression }:
                    bindableParent = bindableParent.Parent;
                    break;
                case ArrayTypeSyntax arrayType:
                    bindableParent = SyntaxFactory.GetStandaloneExpression(arrayType);
                    break;
                case { RawKind: (int)SyntaxKind.ComplexElementInitializerExpression }:
                    // The { "a", "b" } node in a collection initializer is marked as compiler-generated, and will never
                    // end up in the bound node map. We don't need the parent node for any calculations here, so just return
                    // null to avoid rebinding the initializer (which can cause exponential binding problems, see EndToEndTests.LongInitializerList).
                    bindableParent = null;
                    break;
            }

            return bindableParent;
        }
#nullable disable

        internal override Symbol RemapSymbolIfNecessaryCore(Symbol symbol)
        {
            Debug.Assert(symbol is LocalSymbol or ParameterSymbol or MethodSymbol { MethodKind: MethodKind.LambdaMethod });

            EnsureNullabilityAnalysisPerformedIfNecessary();

            if (_lazyRemappedSymbols is null)
            {
                return symbol;
            }

            if (_lazyRemappedSymbols.TryGetValue(symbol, out var remappedSymbol))
            {
                return remappedSymbol;
            }
            else
            {
                return symbol;
            }
        }

        internal sealed override Func<SyntaxNode, bool> GetSyntaxNodesToAnalyzeFilter(SyntaxNode declaredNode, ISymbol declaredSymbol)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override bool ShouldSkipSyntaxNodeAnalysis(SyntaxNode node, ISymbol containingSymbol)
        {
            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// The incremental binder is used when binding statements. Whenever a statement
        /// is bound, it checks the bound node cache to see if that statement was bound, 
        /// and returns it instead of rebinding it. 
        /// 
        /// For example, we might have:
        ///    while (x > goo())
        ///    {
        ///      y = y * x;
        ///      z = z + y;
        ///    }
        /// 
        /// We might first get semantic info about "z", and thus bind just the statement
        /// "z = z + y". Later, we might bind the entire While block. While binding the while
        /// block, we can reuse the binding we did of "z = z + y".
        /// </summary>
        /// <remarks>
        /// NOTE: any member overridden by this binder should follow the BuckStopsHereBinder pattern.
        /// Otherwise, a subsequent binder in the chain could suppress the caching behavior.
        /// </remarks>
        internal class IncrementalBinder : Binder
        {
            private readonly MemberSemanticModel _semanticModel;

            internal IncrementalBinder(MemberSemanticModel semanticModel, Binder next)
                : base(next)
            {
                _semanticModel = semanticModel;
            }

            /// <summary>
            /// We override GetBinder so that the BindStatement override is still
            /// in effect on nested binders.
            /// </summary>
            internal override Binder GetBinder(SyntaxNode node)
            {
                Binder binder = this.Next.GetBinder(node);

                if (binder != null)
                {
                    Debug.Assert(!(binder is IncrementalBinder));
                    return new IncrementalBinder(_semanticModel, binder.WithAdditionalFlags(BinderFlags.SemanticModel));
                }

                return null;
            }

            private BoundStatement TryGetBoundStatementFromMap(StatementSyntax node)
            {
                if (node.SyntaxTree == _semanticModel.SyntaxTree)
                {
                    BoundStatement synthesizedStatement = _semanticModel.GuardedGetSynthesizedStatementFromMap(node);

                    if (synthesizedStatement != null)
                    {
                        return synthesizedStatement;
                    }

                    return (BoundStatement)TryGetBoundNodeFromMap(node);
                }

                return null;
            }

            public override BoundStatement BindStatement(StatementSyntax node, BindingDiagnosticBag diagnostics)
            {
                // Check the bound node cache to see if the statement was already bound.
                BoundStatement statement = TryGetBoundStatementFromMap(node);

                if (statement != null)
                {
                    return statement;
                }

                statement = base.BindStatement(node, diagnostics);

                // Synthesized statements are not added to the _guardedNodeMap, we cache them explicitly here in  
                // _lazyGuardedSynthesizedStatementsMap
                if (statement.WasCompilerGenerated && node.SyntaxTree == _semanticModel.SyntaxTree)
                {
                    _semanticModel.GuardedAddSynthesizedStatementToMap(node, statement);
                }

                return statement;
            }

            internal override BoundBlock BindEmbeddedBlock(BlockSyntax node, BindingDiagnosticBag diagnostics)
            {
                BoundBlock block = (BoundBlock)TryGetBoundNodeFromMap(node);

                if (block is object)
                {
                    return block;
                }

                block = base.BindEmbeddedBlock(node, diagnostics);

                Debug.Assert(!block.WasCompilerGenerated);
                return block;
            }

            private BoundNode TryGetBoundNodeFromMap(CSharpSyntaxNode node)
            {
                if (node.SyntaxTree == _semanticModel.SyntaxTree)
                {
                    OneOrMany<BoundNode> boundNodes = _semanticModel.GuardedGetBoundNodesFromMap(node);

                    if (!boundNodes.IsEmpty)
                    {
                        // Already bound. Return the top-most bound node associated with the statement. 
                        return boundNodes[0];
                    }
                }

                return null;
            }

            public override BoundNode BindMethodBody(CSharpSyntaxNode node, BindingDiagnosticBag diagnostics)
            {
                BoundNode boundNode = TryGetBoundNodeFromMap(node);

                if (boundNode is object)
                {
                    return boundNode;
                }

                boundNode = base.BindMethodBody(node, diagnostics);

                return boundNode;
            }

            internal override BoundExpressionStatement BindConstructorInitializer(ConstructorInitializerSyntax node, BindingDiagnosticBag diagnostics)
            {
                return (BoundExpressionStatement)TryGetBoundNodeFromMap(node) ?? base.BindConstructorInitializer(node, diagnostics);
            }

            internal override BoundExpressionStatement BindConstructorInitializer(PrimaryConstructorBaseTypeSyntax node, BindingDiagnosticBag diagnostics)
            {
                return (BoundExpressionStatement)TryGetBoundNodeFromMap(node) ?? base.BindConstructorInitializer(node, diagnostics);
            }

            internal override BoundBlock BindExpressionBodyAsBlock(ArrowExpressionClauseSyntax node, BindingDiagnosticBag diagnostics)
            {
                BoundBlock block = (BoundBlock)TryGetBoundNodeFromMap(node);

                if (block is object)
                {
                    return block;
                }

                block = base.BindExpressionBodyAsBlock(node, diagnostics);

                return block;
            }

            protected override bool TryGetBoundElseIfStatement(IfStatementSyntax node, out BoundStatement alternative)
            {
                alternative = TryGetBoundStatementFromMap(node);

                if (alternative is not null)
                {
                    Debug.Assert(alternative is BoundIfStatement);
                    alternative = WrapWithVariablesIfAny(node, alternative);
                    return true;
                }

                return base.TryGetBoundElseIfStatement(node, out alternative);
            }
        }

        internal sealed class MemberSemanticBindingCounter
        {
            internal int BindCount;
        }
    }
}
