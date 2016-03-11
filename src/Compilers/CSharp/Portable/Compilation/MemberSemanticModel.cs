// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Binding info for expressions and statements that are part of a member declaration.
    /// </summary>
    internal abstract partial class MemberSemanticModel : CSharpSemanticModel
    {
        private readonly CSharpCompilation _compilation;
        private readonly Symbol _memberSymbol;
        private readonly CSharpSyntaxNode _root;
        private readonly DiagnosticBag _ignoredDiagnostics = new DiagnosticBag();
        private readonly ReaderWriterLockSlim _nodeMapLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        // The bound nodes associated with a syntax node, from highest in the tree to lowest.
        private readonly Dictionary<CSharpSyntaxNode, ImmutableArray<BoundNode>> _guardedNodeMap = new Dictionary<CSharpSyntaxNode, ImmutableArray<BoundNode>>();

        internal readonly Binder RootBinder;

        // Fields specific to a speculative MemberSemanticModel.
        private readonly SyntaxTreeSemanticModel _parentSemanticModelOpt;
        private readonly int _speculatedPosition;

        protected MemberSemanticModel(CSharpCompilation compilation, CSharpSyntaxNode root, Symbol memberSymbol, Binder rootBinder, SyntaxTreeSemanticModel parentSemanticModelOpt, int speculatedPosition)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(root != null);
            Debug.Assert((object)memberSymbol != null);
            Debug.Assert(parentSemanticModelOpt == null || !parentSemanticModelOpt.IsSpeculativeSemanticModel, CSharpResources.ChainingSpeculativeModelIsNotSupported);

            _compilation = compilation;
            _root = root;
            _memberSymbol = memberSymbol;

            if (root.Kind() == SyntaxKind.ArrowExpressionClause)
            {
                rootBinder = rootBinder.WithPatternVariablesIfAny(((ArrowExpressionClauseSyntax)root).Expression);
            }

            this.RootBinder = rootBinder.WithAdditionalFlags(GetSemanticModelBinderFlags());
            _parentSemanticModelOpt = parentSemanticModelOpt;
            _speculatedPosition = speculatedPosition;
        }

        public override CSharpCompilation Compilation
        {
            get
            {
                return _compilation;
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

        public override bool IsSpeculativeSemanticModel
        {
            get
            {
                return _parentSemanticModelOpt != null;
            }
        }

        public override int OriginalPositionForSpeculation
        {
            get
            {
                return _speculatedPosition;
            }
        }

        public override CSharpSemanticModel ParentModel
        {
            get
            {
                return _parentSemanticModelOpt;
            }
        }

        internal override MemberSemanticModel GetMemberModel(CSharpSyntaxNode node)
        {
            return IsInTree(node) ? this : null;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, TypeSyntax type, SpeculativeBindingOption bindingOption, out SemanticModel speculativeModel)
        {
            var expression = SyntaxFactory.GetStandaloneExpression(type);

            var binder = this.GetSpeculativeBinder(position, expression, bindingOption);
            if (binder != null)
            {
                speculativeModel = new SpeculativeMemberSemanticModel(parentModel, _memberSymbol, type, binder, position);
                return true;
            }

            speculativeModel = null;
            return false;
        }

        internal sealed override bool TryGetSpeculativeSemanticModelCore(SyntaxTreeSemanticModel parentModel, int position, CrefSyntax crefSyntax, out SemanticModel speculativeModel)
        {
            // crefs can never legally appear within members.
            speculativeModel = null;
            return false;
        }

        /// <summary>
        /// This overload exists for callers who
        ///   a) Already have a node in hand and don't want to search through the tree
        ///   b) May want to search from an indirect container (e.g. node containing node
        ///      containing position).
        /// </summary>
        private Binder GetEnclosingBinder(CSharpSyntaxNode node, int position)
        {
            AssertPositionAdjusted(position);
            return GetEnclosingBinder(node, position, RootBinder, _root).WithAdditionalFlags(GetSemanticModelBinderFlags());
        }

        private static Binder GetEnclosingBinder(CSharpSyntaxNode node, int position, Binder rootBinder, CSharpSyntaxNode root)
        {
            if (node == root)
            {
                return rootBinder;
            }

            ExpressionSyntax typeOfArgument = null;
            CSharpSyntaxNode unexpectedAnonymousFunction = null;

            // Keep track of which fix-up should be applied first.  If we see a typeof expression inside an unexpected
            // anonymous function, that the typeof binder should be innermost (i.e. should have the unexpected
            // anonymous function binder as its Next).
            // NOTE: only meaningful if typeOfArgument is non-null;
            bool typeOfEncounteredBeforeUnexpectedAnonymousFunction = false;

            Binder binder = null;
            for (var current = node; binder == null; current = current.ParentOrStructuredTriviaParent)
            {
                Debug.Assert(current != null); // Why were we asked for an enclosing binder for a node outside our root?
                StatementSyntax stmt = current as StatementSyntax;
                TypeOfExpressionSyntax typeOfExpression;
                if (stmt != null)
                {
                    if (LookupPosition.IsInStatementScope(position, stmt))
                    {
                        binder = rootBinder.GetBinder(current);

                        if (binder != null)
                        {
                            binder = AdjustBinderForPositionWithinStatement(position, binder, stmt);
                        }
                    }
                }
                else if (current.Kind() == SyntaxKind.CatchClause)
                {
                    if (LookupPosition.IsInCatchBlockScope(position, (CatchClauseSyntax)current))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (current.Kind() == SyntaxKind.CatchFilterClause)
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
                        binder = rootBinder.GetBinder(current);

                        // This should only happen in error scenarios.  For example, C# does not allow array rank
                        // specifiers in types, (e.g. int[1] x;), but the syntax model does.  In order to construct
                        // an appropriate binder chain for the anonymous method body, we need to construct an
                        // ExecutableCodeBinder.
                        if (binder == null && unexpectedAnonymousFunction == null && current != root)
                        {
                            unexpectedAnonymousFunction = current;
                        }
                    }
                }
                else if (current.Kind() == SyntaxKind.TypeOfExpression &&
                    typeOfArgument == null &&
                    LookupPosition.IsBetweenTokens(
                        position,
                        (typeOfExpression = (TypeOfExpressionSyntax)current).OpenParenToken,
                        typeOfExpression.CloseParenToken))
                {
                    typeOfArgument = typeOfExpression.Type;
                    typeOfEncounteredBeforeUnexpectedAnonymousFunction = unexpectedAnonymousFunction == null;
                }
                else if (current.Kind() == SyntaxKind.SwitchSection)
                {
                    if (LookupPosition.IsInSwitchSectionScope(position, (SwitchSectionSyntax)current))
                    {
                        binder = rootBinder.GetBinder(current);
                    }
                }
                else if (current.Kind() == SyntaxKind.ArrowExpressionClause && current.Parent?.Kind() == SyntaxKind.LocalFunctionStatement)
                {
                    binder = rootBinder.GetBinder(current);
                }
                else
                {
                    // If this ever breaks, make sure that all callers of
                    // CanHaveAssociatedLocalBinder are in sync.
                    Debug.Assert(!current.CanHaveAssociatedLocalBinder() || 
                                 (current == root && current.Kind() == SyntaxKind.ArrowExpressionClause));
                }

                if (current == root)
                {
                    break;
                }
            }

            binder = binder ?? rootBinder;
            Debug.Assert(binder != null);

            if (typeOfArgument != null && !typeOfEncounteredBeforeUnexpectedAnonymousFunction)
            {
                binder = new TypeofBinder(typeOfArgument, binder);
            }

            if (unexpectedAnonymousFunction != null)
            {
                binder = new ExecutableCodeBinder(unexpectedAnonymousFunction,
                                                  new LambdaSymbol(binder.ContainingMemberOrLambda,
                                                                   ImmutableArray<ParameterSymbol>.Empty,
                                                                   RefKind.None,
                                                                   ErrorTypeSymbol.UnknownResultType,
                                                                   unexpectedAnonymousFunction.Kind() == SyntaxKind.AnonymousMethodExpression ? MessageID.IDS_AnonMethod : MessageID.IDS_Lambda,
                                                                   unexpectedAnonymousFunction,
                                                                   isSynthesized: false,
                                                                   isAsync: false),
                                                  binder);
            }

            if (typeOfArgument != null && typeOfEncounteredBeforeUnexpectedAnonymousFunction)
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
                    if (LookupPosition.IsBetweenTokens(position, switchStmt.OpenParenToken, switchStmt.OpenBraceToken))
                    {
                        binder = binder.Next;
                        Debug.Assert(binder is PatternVariableBinder);
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

            var csdestination = destination.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>("destination");

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

            var binder = this.GetEnclosingBinder(expression, GetAdjustedNodePosition(expression));
            CSharpSyntaxNode bindableNode = this.GetBindableSyntaxNode(expression);
            var boundExpression = this.GetLowerBoundNode(bindableNode) as BoundExpression;
            if (binder == null || boundExpression == null)
            {
                return Conversion.NoConversion;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return binder.Conversions.ClassifyConversionFromExpression(boundExpression, csdestination, ref useSiteDiagnostics);
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

            var binder = this.GetEnclosingBinder(expression, GetAdjustedNodePosition(expression));
            CSharpSyntaxNode bindableNode = this.GetBindableSyntaxNode(expression);
            var boundExpression = this.GetLowerBoundNode(bindableNode) as BoundExpression;
            if (binder == null || boundExpression == null)
            {
                return Conversion.NoConversion;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return binder.Conversions.ClassifyConversionForCast(boundExpression, destination, ref useSiteDiagnostics);
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

            if (boundNodes.Length == 0)
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

            if (boundNodes.Length == 0)
            {
                return null;
            }
            else
            {
                return boundNodes[boundNodes.Length - 1];
            }
        }

        public override ImmutableArray<Diagnostic> GetSyntaxDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics(TextSpan? span = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        public override INamespaceSymbol GetDeclaredSymbol(NamespaceDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
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

        public override ISymbol GetDeclaredSymbol(LocalFunctionStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            var boundLocalFunction = GetLowerBoundNode(declarationSyntax) as BoundLocalFunctionStatement;
            return boundLocalFunction?.Symbol;
        }

        public override ISymbol GetDeclaredSymbol(MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define member inside member.
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
            return GetDeclaredLocal(declarationSyntax, declarationSyntax.Identifier);
        }

        private LocalSymbol GetDeclaredLocal(CSharpSyntaxNode declarationSyntax, SyntaxToken declaredIdentifier)
        {
            for (var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax)); binder != null; binder = binder.Next)
            {
                foreach (var local in binder.Locals)
                {
                    if (local.IdentifierToken == declaredIdentifier)
                    {
                        return local;
                    }
                }
            }

            return null;
        }

        public override ISymbol GetDeclaredSymbol(DeclarationPatternSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);
            return GetDeclaredLocal(declarationSyntax, declarationSyntax.Identifier);
        }

        public override ISymbol GetDeclaredSymbol(LetStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            if (declarationSyntax.Pattern == null)
            {
                return GetDeclaredLocal(declarationSyntax, declarationSyntax.Identifier);
            }

            return null;
        }

        public override ILabelSymbol GetDeclaredSymbol(LabeledStatementSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declarationSyntax);

            var binder = this.GetEnclosingBinder(GetAdjustedNodePosition(declarationSyntax));
            foreach (var label in binder.Labels)
            {
                if (label.IdentifierNodeOrToken.IsToken &&
                    label.IdentifierNodeOrToken.AsToken() == declarationSyntax.Identifier)
                {
                    return label;
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
                        return label;
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
            // Could be parameter of lambda.
            CheckSyntaxNode(declarationSyntax);

            return GetLambdaParameterSymbol(declarationSyntax, cancellationToken);
        }

        internal override ImmutableArray<ISymbol> GetDeclaredSymbols(BaseFieldDeclarationSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Can't define field inside member.
            return ImmutableArray.Create<ISymbol>();
        }

        private ParameterSymbol GetLambdaParameterSymbol(
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
            if (paramList == null)
            {
                return null;
            }

            if (paramList.Parent == null || !paramList.Parent.IsAnonymousFunction())
            {
                return null;
            }

            return GetLambdaParameterSymbol(parameter, (ExpressionSyntax)paramList.Parent, cancellationToken);
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
                lambdaSymbol = (LambdaSymbol)symbolInfo.Symbol;
            }
            else if (symbolInfo.CandidateSymbols.Length == 1)
            {
                lambdaSymbol = (LambdaSymbol)symbolInfo.CandidateSymbols.Single();
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
            return bound == null ? null : bound.DefinedSymbol;
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryClauseSyntax queryClause, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(queryClause);
            return bound == null ? null : bound.DefinedSymbol;
        }

        public override IRangeVariableSymbol GetDeclaredSymbol(QueryContinuationSyntax node, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bound = GetBoundQueryClause(node);
            return bound == null ? null : bound.DefinedSymbol;
        }

        public override AwaitExpressionInfo GetAwaitExpressionInfo(AwaitExpressionSyntax node)
        {
            if (node.Kind() != SyntaxKind.AwaitExpression)
            {
                throw new ArgumentException("node.Kind==" + node.Kind());
            }

            var bound = GetUpperBoundNode(node);
            BoundAwaitExpression boundAwait = ((bound as BoundExpressionStatement)?.Expression ?? bound) as BoundAwaitExpression;
            if (boundAwait == null)
            {
                return default(AwaitExpressionInfo);
            }

            return new AwaitExpressionInfo(boundAwait.GetAwaiter, boundAwait.IsCompleted, boundAwait.GetResult, boundAwait.IsDynamic);
        }

        public override ForEachStatementInfo GetForEachStatementInfo(ForEachStatementSyntax node)
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
            if (enumeratorInfoOpt.ElementType.IsPointerType())
            {
                Debug.Assert(!enumeratorInfoOpt.CurrentConversion.IsValid);
                return default(ForEachStatementInfo);
            }

            // NOTE: we're going to list GetEnumerator, etc for array and string
            // collections, even though we know that's not how the implementation
            // actually enumerates them.
            return new ForEachStatementInfo(
                enumeratorInfoOpt.GetEnumeratorMethod,
                enumeratorInfoOpt.MoveNextMethod,
                (PropertySymbol)enumeratorInfoOpt.CurrentPropertyGetter.AssociatedSymbol,
                enumeratorInfoOpt.NeedsDisposeMethod ? (MethodSymbol)Compilation.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose) : null,
                enumeratorInfoOpt.ElementType,
                boundForEach.ElementConversion,
                enumeratorInfoOpt.CurrentConversion);
        }

        private BoundQueryClause GetBoundQueryClause(CSharpSyntaxNode node)
        {
            CheckSyntaxNode(node);
            return this.GetLowerBoundNode(node) as BoundQueryClause;
        }

        private QueryClauseInfo GetQueryClauseInfo(BoundQueryClause bound)
        {
            if (bound == null) return default(QueryClauseInfo);
            var castInfo = (bound.Cast == null) ? default(SymbolInfo) : GetSymbolInfoForNode(SymbolInfoOptions.DefaultOptions, bound.Cast, bound.Cast, boundNodeForSyntacticParent: null, binderOpt: null);
            var operationInfo = GetSymbolInfoForQuery(bound);
            return new QueryClauseInfo(castInfo: castInfo, operationInfo: operationInfo);
        }

        private SymbolInfo GetSymbolInfoForQuery(BoundQueryClause bound)
        {
            var call = bound?.Operation as BoundCall;
            if (call == null)
            {
                return default(SymbolInfo);
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
            return AnonymousTypeManager.GetAnonymousTypeProperty(anonymousType, index);
        }

        public override INamedTypeSymbol GetDeclaredSymbol(AnonymousObjectCreationExpressionSyntax declaratorSyntax, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSyntaxNode(declaratorSyntax);
            var bound = this.GetLowerBoundNode(declaratorSyntax) as BoundAnonymousObjectCreationExpression;
            return (bound == null) ? null : bound.Type as NamedTypeSymbol;
        }

        public override SyntaxTree SyntaxTree
        {
            get
            {
                return _root.SyntaxTree;
            }
        }

        internal override IOperation GetOperationWorker(CSharpSyntaxNode node, GetOperationOptions options, CancellationToken cancellationToken)
        {
            CSharpSyntaxNode bindableNode;

            BoundNode lowestBoundNode;
            BoundNode highestBoundNode;
            BoundNode boundParent;

            GetBoundNodes(node, out bindableNode, out lowestBoundNode, out highestBoundNode, out boundParent);
            BoundNode result;
            switch (options)
            {
                case GetOperationOptions.Parent:
                    result = boundParent;
                    break;
                case GetOperationOptions.Highest:
                    result = highestBoundNode;
                    break;
                case GetOperationOptions.Lowest:
                default:
                    result = lowestBoundNode;
                    break;
            }

            return result as IOperation;
        }

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

        internal override ImmutableArray<PropertySymbol> GetIndexerGroupWorker(CSharpSyntaxNode node, SymbolInfoOptions options, CancellationToken cancellationToken = default(CancellationToken))
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

            ConstantValue constantValue = boundExpr.ConstantValue;
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

                // If the lambda that is "enclosing" us is in fact enclosing an explicit lambda
                // parameter type then keep on going; that guy is logically bound outside of the
                // lambda. For example, if we have:
                //
                // D d = (Foo f)=>{int Foo; };
                //
                // Then the type "Foo" is bound in the context outside the lambda body, not inside
                // where it might get confused with local "Foo".
                if (NodeIsExplicitType(node, current))
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

        private static bool NodeIsExplicitType(CSharpSyntaxNode node, CSharpSyntaxNode lambda)
        {
            Debug.Assert(node != null);
            Debug.Assert(lambda != null);
            Debug.Assert(lambda.IsAnonymousFunction() || lambda.IsQuery());

            // UNDONE;
            return false;
        }

        private CSharpSyntaxNode GetOutermostLambdaOrQuery(CSharpSyntaxNode node)
        {
            Debug.Assert(node != null);

            CSharpSyntaxNode lambda = null;
            for (var current = node; current != this.Root; current = current.ParentOrStructuredTriviaParent)
            {
                // (It is possible for the outermost lambda to be the node we were given.)
                if (current.IsAnonymousFunction() || current.IsQuery())
                {
                    lambda = current;
                }

                // current can only become null if we somehow got past the root. The only way we
                // could have gotten past the root is to have started outside of it. That's
                // unexpected; the binding should only be asked to provide an opinion on syntax
                // nodes that it knows about.
                Debug.Assert(current != null, "Why are we being asked to find an enclosing lambda outside of our root?");
            }

            // As above, if the root is a lambda then it does not count as the one "outside" of the
            // given node. We want the outermost lambda that we can find a binding for; if the root
            // of this binding is the lambda we're inside then "this" is the right binding.
            return lambda;
        }

        private ImmutableArray<BoundNode> GuardedGetBoundNodesFromMap(CSharpSyntaxNode node)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld || _nodeMapLock.IsReadLockHeld);
            ImmutableArray<BoundNode> result;
            return _guardedNodeMap.TryGetValue(node, out result) ? result : default(ImmutableArray<BoundNode>);
        }

        // Adds every syntax/bound pair in a tree rooted at the given bound node to the map, and the
        // performs a lookup of the given syntax node in the map. 
        private ImmutableArray<BoundNode> GuardedAddBoundTreeAndGetBoundNodeFromMap(CSharpSyntaxNode syntax, BoundNode bound)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld);

            bool alreadyInTree = false;

            if (bound != null)
            {
                alreadyInTree = _guardedNodeMap.ContainsKey(bound.Syntax);
            }

            // check if we already have node in the cache.
            // this may happen if we have races and in such case we are no longer interested in adding
            if (!alreadyInTree)
            {
                NodeMapBuilder.AddToMap(bound, _guardedNodeMap);
            }

            ImmutableArray<BoundNode> result;
            return _guardedNodeMap.TryGetValue(syntax, out result) ? result : default(ImmutableArray<BoundNode>);
        }

        internal void UnguardedAddBoundTreeForStandaloneSyntax(CSharpSyntaxNode syntax, BoundNode bound)
        {
            using (_nodeMapLock.DisposableWrite())
            {
                GuardedAddBoundTreeForStandaloneSyntax(syntax, bound);
            }
        }

        protected void GuardedAddBoundTreeForStandaloneSyntax(CSharpSyntaxNode syntax, BoundNode bound)
        {
            Debug.Assert(_nodeMapLock.IsWriteLockHeld);
            bool alreadyInTree = false;

            // check if we already have node in the cache.
            // this may happen if we have races and in such case we are no longer interested in adding
            if (bound != null)
            {
                alreadyInTree = _guardedNodeMap.ContainsKey(bound.Syntax);
            }

            if (!alreadyInTree)
            {
                if ((this.IsSpeculativeSemanticModel && syntax == _root) || syntax is StatementSyntax)
                {
                    // Note: For speculative model we want to always cache the entire bound tree.
                    // If syntax is a statement, we need to add all its children.
                    // Node cache assumes that if statement is cached, then all 
                    // its children are cached too.
                    NodeMapBuilder.AddToMap(bound, _guardedNodeMap);
                }
                else
                {
                    // expressions can be added individually.
                    NodeMapBuilder.AddToMap(bound, _guardedNodeMap, syntax);
                }
            }
        }

        // We might not have actually been given a bindable expression or statement; the caller can
        // give us variable declaration nodes, for example. If we're not at an expression or
        // statement, back up until we find one.
        private CSharpSyntaxNode GetBindingRoot(CSharpSyntaxNode node)
        {
            Debug.Assert(node != null);

            StatementSyntax enclosingStatement = null;

            for (CSharpSyntaxNode current = node; current != this.Root; current = current.ParentOrStructuredTriviaParent)
            {
                Debug.Assert(current != null, "How did we get outside the root?");

                if (enclosingStatement == null)
                {
                    enclosingStatement = current as StatementSyntax;
                }

                switch (current.Kind())
                {
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.LocalFunctionStatement:
                        // We can't use a statement that is inside a lambda.
                        enclosingStatement = null;
                        break;
                }
            }

            return enclosingStatement ?? this.Root;
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

            CSharpSyntaxNode innerLambda = GetInnermostLambdaOrQuery(node, position, allowStarting: true);

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

            if (innerLambda == null)
            {
                return GetEnclosingBinder(node, position);
            }

            // In the third case, we're in a child lambda. Have we already cached a binder for it?
            // If not, bind the outermost expression containing the lambda and then fill in the map.

            ImmutableArray<BoundNode> nodes;

            using (_nodeMapLock.DisposableRead())
            {
                nodes = GuardedGetBoundNodesFromMap(innerLambda);
            }

            if (nodes.IsDefaultOrEmpty)
            {
                CSharpSyntaxNode outerLambda = GetOutermostLambdaOrQuery(innerLambda);
                Debug.Assert(outerLambda != null);
                Debug.Assert(outerLambda != this.Root);
                CSharpSyntaxNode nodeToBind = GetBindingRoot(outerLambda);

                var statementBinder = GetEnclosingBinder(nodeToBind, position);
                Binder incrementalBinder = new IncrementalBinder(this, statementBinder);

                using (_nodeMapLock.DisposableWrite())
                {
                    BoundNode boundOuterExpression = this.Bind(incrementalBinder, nodeToBind, _ignoredDiagnostics);
                    GuardedAddBoundTreeAndGetBoundNodeFromMap(innerLambda, boundOuterExpression);
                }
            }

            // If there is a bug in the binder such that we "lose" a sub-expression containing a
            // lambda, and never put bound state for it into the bound tree, then the bound lambda
            // that comes back from the map lookup will be null. This can occur in error recovery
            // situations.  If it is null, we fall back to the outer binder.

            using (_nodeMapLock.DisposableRead())
            {
                nodes = GuardedGetBoundNodesFromMap(innerLambda);
            }

            if (nodes.IsDefaultOrEmpty)
            {
                return GetEnclosingBinder(node, position);
            }

            BoundNode boundInnerLambda = GetLowerBoundNode(innerLambda);
            Debug.Assert(boundInnerLambda != null);
            Binder result;
            switch (boundInnerLambda.Kind)
            {
                case BoundKind.UnboundLambda:
                    boundInnerLambda = ((UnboundLambda)boundInnerLambda).BindForErrorRecovery();
                    goto case BoundKind.Lambda;
                case BoundKind.Lambda:
                    AssertPositionAdjusted(position);
                    result = GetLambdaEnclosingBinder(position, node, innerLambda, ((BoundLambda)boundInnerLambda).Binder);
                    break;
                case BoundKind.QueryClause:
                    result = GetQueryEnclosingBinder(position, ((BoundQueryClause)boundInnerLambda));
                    break;
                default:
                    return GetEnclosingBinder(node, position); // Known to return non-null with BinderFlags.SemanticModel.
            }

            Debug.Assert(result != null);
            return result.WithAdditionalFlags(GetSemanticModelBinderFlags());
        }

        /// <remarks>
        /// Returned binder doesn't need to have <see cref="BinderFlags.SemanticModel"/> set - the caller will add it.
        /// </remarks>
        private static Binder GetQueryEnclosingBinder(int position, BoundQueryClause queryClause)
        {
            for (BoundNode n = queryClause.Value; n != null;)
            {
                switch (n.Kind)
                {
                    case BoundKind.QueryClause:
                        queryClause = (BoundQueryClause)n;
                        n = queryClause.Value;
                        continue;
                    case BoundKind.Call:
                        var call = (BoundCall)n;
                        if (call == null || call.Arguments.Length == 0) return queryClause.Binder;
                        // TODO: should we look for the "nearest" argument as a fallback?
                        n = call.Arguments[call.Arguments.Length - 1];
                        foreach (var arg in call.Arguments)
                        {
                            if (arg.Syntax.FullSpan.Contains(position)) n = arg;
                        }
                        continue;
                    case BoundKind.Conversion:
                        n = ((BoundConversion)n).Operand;
                        continue;
                    case BoundKind.UnboundLambda:
                        // NOTE: Calling GetLambdaEnclosingBinder would just return this binder.
                        return ((UnboundLambda)n).BindForErrorRecovery().Binder;
                    case BoundKind.Lambda:
                        // NOTE: Calling GetLambdaEnclosingBinder would just return this binder.
                        return ((BoundLambda)n).Binder;
                    default:
                        return queryClause.Binder;
                }
            }

            return queryClause.Binder;
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

            return GetEnclosingBinder(startingNode, position, lambdaBinder, containingLambda);
        }

        /// <summary>
        /// Get all bounds nodes associated with a node, ordered from highest to lowest in the bound tree.
        /// Strictly speaking, the order is that of a pre-order traversal of the bound tree.
        /// </summary>
        internal ImmutableArray<BoundNode> GetBoundNodes(CSharpSyntaxNode node)
        {
            // If this method is called with a null parameter, that implies that the Root should be
            // bound, but make sure that the Root is bindable.
            if (node == null)
            {
                node = GetBindableSyntaxNode(Root);
            }
            Debug.Assert(node == GetBindableSyntaxNode(node));

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
            ImmutableArray<BoundNode> results;

            using (_nodeMapLock.DisposableRead())
            {
                results = GuardedGetBoundNodesFromMap(node);
            }

            if (!results.IsDefaultOrEmpty)
            {
                return results;
            }

            // We might not actually have been given an expression or statement even though we were
            // allegedly given something that is "bindable".

            // If we didn't find in the cached bound nodes, find a binding root and bind it.
            // This will cache bound nodes under the binding root.
            CSharpSyntaxNode nodeToBind = GetBindingRoot(node);
            var statementBinder = GetEnclosingBinder(GetAdjustedNodePosition(nodeToBind));
            Binder incrementalBinder = new IncrementalBinder(this, statementBinder);

            using (_nodeMapLock.DisposableWrite())
            {
                BoundNode boundStatement = this.Bind(incrementalBinder, nodeToBind, _ignoredDiagnostics);
                results = GuardedAddBoundTreeAndGetBoundNodeFromMap(node, boundStatement);
            }

            if (!results.IsDefaultOrEmpty)
            {
                return results;
            }

            // If we still didn't find it, its still possible we could bind it directly.
            // For example, types are usually not represented by bound nodes, and some error conditions and
            // not yet implemented features do not create bound nodes for everything underneath them.
            //
            // In this case, however, we only add the single bound node we found to the map, not any child bound nodes,
            // to avoid duplicates in the map if a parent of this node comes through this code path also.

            var binder = GetEnclosingBinder(GetAdjustedNodePosition(node));

            using (_nodeMapLock.DisposableRead())
            {
                results = GuardedGetBoundNodesFromMap(node);
            }

            if (results.IsDefaultOrEmpty)
            {
                using (_nodeMapLock.DisposableWrite())
                {
                    var boundNode = this.Bind(binder, node, _ignoredDiagnostics);
                    GuardedAddBoundTreeForStandaloneSyntax(node, boundNode);
                    results = GuardedGetBoundNodesFromMap(node);
                }

                if (!results.IsDefaultOrEmpty)
                {
                    return results;
                }
            }
            else
            {
                return results;
            }

            return ImmutableArray<BoundNode>.Empty;
        }

        // some nodes don't have direct semantic meaning by themselves and so we need to bind a different node that does
        internal protected virtual CSharpSyntaxNode GetBindableSyntaxNode(CSharpSyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)node).Body;
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((BaseMethodDeclarationSyntax)node).Body;
                case SyntaxKind.GlobalStatement:
                    return node;
            }

            while (true)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ParenthesizedExpression:
                        node = ((ParenthesizedExpressionSyntax)node).Expression;
                        continue;

                    case SyntaxKind.CheckedExpression:
                    case SyntaxKind.UncheckedExpression:
                        node = ((CheckedExpressionSyntax)node).Expression;
                        continue;
                }

                break;
            }

            var parent = node.Parent;
            if (parent != null)
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

                    default:
                        if (node is QueryExpressionSyntax && parent is QueryContinuationSyntax ||
                            !(node is ExpressionSyntax) &&
                            !(node is StatementSyntax) &&
                            !(node is SelectOrGroupClauseSyntax) &&
                            !(node is QueryClauseSyntax) &&
                            !(node is OrderingSyntax) &&
                            !(node is JoinIntoClauseSyntax) &&
                            !(node is QueryContinuationSyntax) &&
                            !(node is ArrowExpressionClauseSyntax))
                        {
                            return GetBindableSyntaxNode(parent);
                        }

                        break;
                }
            }

            return node;
        }

        /// <summary>
        /// If the node is an expression, return the nearest parent node
        /// with semantic meaning. Otherwise return null.
        /// </summary>
        protected CSharpSyntaxNode GetBindableParentNode(CSharpSyntaxNode node)
        {
            if (!(node is ExpressionSyntax))
            {
                return null;
            }

            // The node is an expression, but its parent is null
            CSharpSyntaxNode parent = node.Parent;
            if (parent == null)
            {
                // For speculative model, expression might be the root of the syntax tree, in which case it can have a null parent.
                if (this.IsSpeculativeSemanticModel && this.Root == node)
                {
                    return null;
                }

                throw new ArgumentException();
            }

            // skip up past parens, as we have no bound nodes for them.
            while (parent.Kind() == SyntaxKind.ParenthesizedExpression)
            {
                var pp = parent.Parent;
                if (pp == null) break;
                parent = pp;
            }

            var bindableParent = this.GetBindableSyntaxNode(parent);
            Debug.Assert(bindableParent != null);

            // If the parent is a member used for a method invocation, then
            // the node is the instance associated with the method invocation.
            // In that case, return the invocation expression so that any conversion
            // of the receiver can be included in the resulting SemanticInfo.
            if ((bindableParent.Kind() == SyntaxKind.SimpleMemberAccessExpression) && (bindableParent.Parent.Kind() == SyntaxKind.InvocationExpression))
            {
                bindableParent = bindableParent.Parent;
            }
            else if (bindableParent.Kind() == SyntaxKind.ArrayType)
            {
                bindableParent = SyntaxFactory.GetStandaloneExpression((ArrayTypeSyntax)bindableParent);
            }

            return bindableParent;
        }

        /// <summary>
        /// The incremental binder is used when binding statements. Whenever a statement
        /// is bound, it checks the bound node cache to see if that statement was bound, 
        /// and returns it instead of rebinding it. 
        /// 
        /// For example, we might have:
        ///    while (x > foo())
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
            internal override Binder GetBinder(CSharpSyntaxNode node)
            {
                Binder binder = this.Next.GetBinder(node);

                if (binder != null)
                {
                    Debug.Assert(!(binder is IncrementalBinder));
                    return new IncrementalBinder(_semanticModel, binder.WithAdditionalFlags(BinderFlags.SemanticModel));
                }

                return null;
            }

            public override BoundStatement BindStatement(StatementSyntax node, DiagnosticBag diagnostics)
            {
                // Check the bound node cache to see if the statement was already bound.
                ImmutableArray<BoundNode> boundNodes = _semanticModel.GuardedGetBoundNodesFromMap(node);

                if (boundNodes.IsDefaultOrEmpty)
                {
                    // Not bound already. Bind it. It will get added to the cache later by a MemberSemanticModel.NodeMapBuilder.
                    return base.BindStatement(node, diagnostics);
                }
                else
                {
                    // Already bound. Return the top-most bound node associated with the statement. 
                    return (BoundStatement)boundNodes[0];
                }
            }
        }
    }
}
