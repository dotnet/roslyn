// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal sealed class CSharpEditAndContinueAnalyzer : AbstractEditAndContinueAnalyzer
    {
        [ExportLanguageServiceFactory(typeof(IEditAndContinueAnalyzer), LanguageNames.CSharp), Shared]
        internal sealed class Factory : ILanguageServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            {
                return new CSharpEditAndContinueAnalyzer(testFaultInjector: null);
            }
        }

        // Public for testing purposes
        public CSharpEditAndContinueAnalyzer(Action<SyntaxNode>? testFaultInjector = null)
            : base(testFaultInjector)
        {
        }

        #region Syntax Analysis

        private enum BlockPart
        {
            OpenBrace = DefaultStatementPart,
            CloseBrace = 1,
        }

        private enum ForEachPart
        {
            ForEach = DefaultStatementPart,
            VariableDeclaration = 1,
            In = 2,
            Expression = 3,
        }

        private enum SwitchExpressionPart
        {
            WholeExpression = DefaultStatementPart,

            // An active statement that covers IL generated for the decision tree:
            //   <governing-expression> [|switch { <arm>, ..., <arm> }|]
            // This active statement is never a leaf active statement (does not correspond to a breakpoint span).
            SwitchBody = 1,
        }

        /// <returns>
        /// <see cref="BaseMethodDeclarationSyntax"/> for methods, operators, constructors, destructors and accessors.
        /// <see cref="VariableDeclaratorSyntax"/> for field initializers.
        /// <see cref="PropertyDeclarationSyntax"/> for property initializers and expression bodies.
        /// <see cref="IndexerDeclarationSyntax"/> for indexer expression bodies.
        /// <see cref="ArrowExpressionClauseSyntax"/> for getter of an expression-bodied property/indexer.
        /// </returns>
        internal override bool TryFindMemberDeclaration(SyntaxNode? root, SyntaxNode node, out OneOrMany<SyntaxNode> declarations)
        {
            var current = node;
            while (current != null && current != root)
            {
                switch (current.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                        declarations = new(current);
                        return true;

                    case SyntaxKind.PropertyDeclaration:
                        // int P { get; } = [|initializer|];
                        RoslynDebug.Assert(((PropertyDeclarationSyntax)current).Initializer != null);
                        declarations = new(current);
                        return true;

                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        // Active statements encompassing modifiers or type correspond to the first initialized field.
                        // [|public static int F = 1|], G = 2;
                        declarations = new(((BaseFieldDeclarationSyntax)current).Declaration.Variables.First());
                        return true;

                    case SyntaxKind.VariableDeclarator:
                        // public static int F = 1, [|G = 2|];
                        RoslynDebug.Assert(current.Parent.IsKind(SyntaxKind.VariableDeclaration));

                        switch (current.Parent.Parent!.Kind())
                        {
                            case SyntaxKind.FieldDeclaration:
                            case SyntaxKind.EventFieldDeclaration:
                                declarations = new(current);
                                return true;
                        }

                        current = current.Parent;
                        break;

                    case SyntaxKind.ArrowExpressionClause:
                        // represents getter symbol declaration node of a property/indexer with expression body
                        if (current.Parent.IsKind(SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration))
                        {
                            declarations = new(current);
                            return true;
                        }

                        break;
                }

                current = current.Parent;
            }

            declarations = default;
            return false;
        }

        /// <returns>
        /// Given a node representing a declaration or a top-level match node returns:
        /// - <see cref="BlockSyntax"/> for method-like member declarations with block bodies (methods, operators, constructors, destructors, accessors).
        /// - <see cref="ExpressionSyntax"/> for variable declarators of fields, properties with an initializer expression, or 
        ///   for method-like member declarations with expression bodies (methods, properties, indexers, operators)
        /// 
        /// A null reference otherwise.
        /// </returns>
        internal override SyntaxNode? TryGetDeclarationBody(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.VariableDeclarator, out VariableDeclaratorSyntax? variableDeclarator))
            {
                return variableDeclarator.Initializer?.Value;
            }

            return SyntaxUtilities.TryGetMethodDeclarationBody(node);
        }

        internal override bool IsDeclarationWithSharedBody(SyntaxNode declaration)
            => false;

        protected override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model, SyntaxNode memberBody)
        {
            Debug.Assert(memberBody.IsKind(SyntaxKind.Block) || memberBody is ExpressionSyntax);
            return model.AnalyzeDataFlow(memberBody).Captured;
        }

        internal override bool HasParameterClosureScope(ISymbol member)
        {
            // in instance constructor parameters are lifted to a closure different from method body
            return (member as IMethodSymbol)?.MethodKind == MethodKind.Constructor;
        }

        protected override IEnumerable<SyntaxNode> GetVariableUseSites(IEnumerable<SyntaxNode> roots, ISymbol localOrParameter, SemanticModel model, CancellationToken cancellationToken)
        {
            Debug.Assert(localOrParameter is IParameterSymbol || localOrParameter is ILocalSymbol || localOrParameter is IRangeVariableSymbol);

            // not supported (it's non trivial to find all places where "this" is used):
            Debug.Assert(!localOrParameter.IsThisParameter());

            return from root in roots
                   from node in root.DescendantNodesAndSelf()
                   where node.IsKind(SyntaxKind.IdentifierName)
                   let nameSyntax = (IdentifierNameSyntax)node
                   where (string?)nameSyntax.Identifier.Value == localOrParameter.Name &&
                         (model.GetSymbolInfo(nameSyntax, cancellationToken).Symbol?.Equals(localOrParameter) ?? false)
                   select node;
        }

        /// <returns>
        /// If <paramref name="node"/> is a method, accessor, operator, destructor, or constructor without an initializer,
        /// tokens of its block body, or tokens of the expression body.
        /// 
        /// If <paramref name="node"/> is an indexer declaration the tokens of its expression body.
        /// 
        /// If <paramref name="node"/> is a property declaration the tokens of its expression body or initializer.
        ///   
        /// If <paramref name="node"/> is a constructor with an initializer, 
        /// tokens of the initializer concatenated with tokens of the constructor body.
        /// 
        /// If <paramref name="node"/> is a variable declarator of a field with an initializer,
        /// subset of the tokens of the field declaration depending on which variable declarator it is.
        /// 
        /// Null reference otherwise.
        /// </returns>
        internal override IEnumerable<SyntaxToken>? TryGetActiveTokens(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.VariableDeclarator))
            {
                // TODO: The logic is similar to BreakpointSpans.TryCreateSpanForVariableDeclaration. Can we abstract it?

                var declarator = node;
                var fieldDeclaration = (BaseFieldDeclarationSyntax)declarator.Parent!.Parent!;
                var variableDeclaration = fieldDeclaration.Declaration;

                if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    return null;
                }

                if (variableDeclaration.Variables.Count == 1)
                {
                    if (variableDeclaration.Variables[0].Initializer == null)
                    {
                        return null;
                    }

                    return fieldDeclaration.Modifiers.Concat(variableDeclaration.DescendantTokens()).Concat(fieldDeclaration.SemicolonToken);
                }

                if (declarator == variableDeclaration.Variables[0])
                {
                    return fieldDeclaration.Modifiers.Concat(variableDeclaration.Type.DescendantTokens()).Concat(node.DescendantTokens());
                }

                return declarator.DescendantTokens();
            }

            if (node is PropertyDeclarationSyntax { ExpressionBody: var propertyExpressionBody and not null })
            {
                return propertyExpressionBody.Expression.DescendantTokens();
            }

            if (node is IndexerDeclarationSyntax { ExpressionBody: var indexerExpressionBody and not null })
            {
                return indexerExpressionBody.Expression.DescendantTokens();
            }

            var bodyTokens = SyntaxUtilities.TryGetMethodDeclarationBody(node)?.DescendantTokens();

            if (node.IsKind(SyntaxKind.ConstructorDeclaration, out ConstructorDeclarationSyntax? ctor))
            {
                if (ctor.Initializer != null)
                {
                    bodyTokens = ctor.Initializer.DescendantTokens().Concat(bodyTokens ?? Enumerable.Empty<SyntaxToken>());
                }
            }

            return bodyTokens;
        }

        internal override (TextSpan envelope, TextSpan hole) GetActiveSpanEnvelope(SyntaxNode declaration)
            => (BreakpointSpans.GetEnvelope(declaration), default);

        protected override SyntaxNode GetEncompassingAncestorImpl(SyntaxNode bodyOrMatchRoot)
        {
            // Constructor may contain active nodes outside of its body (constructor initializer),
            // but within the body of the member declaration (the parent).
            if (bodyOrMatchRoot.Parent.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return bodyOrMatchRoot.Parent;
            }

            // Field initializer match root -- an active statement may include the modifiers 
            // and type specification of the field declaration.
            if (bodyOrMatchRoot.IsKind(SyntaxKind.EqualsValueClause) &&
                bodyOrMatchRoot.Parent.IsKind(SyntaxKind.VariableDeclarator) &&
                bodyOrMatchRoot.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
            {
                return bodyOrMatchRoot.Parent.Parent;
            }

            // Field initializer body -- an active statement may include the modifiers 
            // and type specification of the field declaration.
            if (bodyOrMatchRoot.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
                bodyOrMatchRoot.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator) &&
                bodyOrMatchRoot.Parent.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration))
            {
                return bodyOrMatchRoot.Parent.Parent.Parent;
            }

            // otherwise all active statements are covered by the body/match root itself:
            return bodyOrMatchRoot;
        }

        protected override SyntaxNode FindStatementAndPartner(SyntaxNode declarationBody, TextSpan span, SyntaxNode? partnerDeclarationBody, out SyntaxNode? partner, out int statementPart)
        {
            var position = span.Start;

            SyntaxUtilities.AssertIsBody(declarationBody, allowLambda: false);

            if (position < declarationBody.SpanStart)
            {
                // Only constructors and the field initializers may have an [|active statement|] starting outside of the <<body>>.
                // Constructor:                          [|public C()|] <<{ }>>
                // Constructor initializer:              public C() : [|base(expr)|] <<{ }>>
                // Constructor initializer with lambda:  public C() : base(() => { [|...|] }) <<{ }>>
                // Field initializers:                   [|public int a = <<expr>>|], [|b = <<expr>>|];

                // No need to special case property initializers here, the active statement always spans the initializer expression.

                if (declarationBody.Parent.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    var constructor = (ConstructorDeclarationSyntax)declarationBody.Parent;
                    var partnerConstructor = (ConstructorDeclarationSyntax?)partnerDeclarationBody?.Parent;

                    if (constructor.Initializer == null || position < constructor.Initializer.ColonToken.SpanStart)
                    {
                        statementPart = DefaultStatementPart;
                        partner = partnerConstructor;
                        return constructor;
                    }

                    declarationBody = constructor.Initializer;
                    partnerDeclarationBody = partnerConstructor?.Initializer;
                }
            }

            if (!declarationBody.FullSpan.Contains(position))
            {
                // invalid position, let's find a labeled node that encompasses the body:
                position = declarationBody.SpanStart;
            }

            SyntaxNode node;
            if (partnerDeclarationBody != null)
            {
                SyntaxUtilities.FindLeafNodeAndPartner(declarationBody, position, partnerDeclarationBody, out node, out partner);
            }
            else
            {
                node = declarationBody.FindToken(position).Parent!;
                partner = null;
            }

            while (true)
            {
                var isBody = node == declarationBody || LambdaUtilities.IsLambdaBodyStatementOrExpression(node);

                if (isBody || SyntaxComparer.Statement.HasLabel(node))
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.Block:
                            statementPart = (int)GetStatementPart((BlockSyntax)node, position);
                            return node;

                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.ForEachVariableStatement:
                            Debug.Assert(!isBody);
                            statementPart = (int)GetStatementPart((CommonForEachStatementSyntax)node, position);
                            return node;

                        case SyntaxKind.DoStatement:
                            // The active statement of DoStatement node is the while condition,
                            // which is lexically not the closest breakpoint span (the body is).
                            // do { ... } [|while (condition);|]
                            Debug.Assert(position == ((DoStatementSyntax)node).WhileKeyword.SpanStart);
                            Debug.Assert(!isBody);
                            goto default;

                        case SyntaxKind.PropertyDeclaration:
                            // The active span corresponding to a property declaration is the span corresponding to its initializer (if any),
                            // not the span corresponding to the accessor.
                            // int P { [|get;|] } = [|<initializer>|];
                            Debug.Assert(position == ((PropertyDeclarationSyntax)node).Initializer!.SpanStart);
                            goto default;

                        case SyntaxKind.VariableDeclaration:
                            // VariableDeclaration ::= TypeSyntax CommaSeparatedList(VariableDeclarator)
                            // 
                            // The compiler places sequence points after each local variable initialization.
                            // The TypeSyntax is considered to be part of the first sequence span.
                            Debug.Assert(!isBody);

                            node = ((VariableDeclarationSyntax)node).Variables.First();

                            if (partner != null)
                            {
                                partner = ((VariableDeclarationSyntax)partner).Variables.First();
                            }

                            statementPart = DefaultStatementPart;
                            return node;

                        case SyntaxKind.SwitchExpression:
                            // An active statement that covers IL generated for the decision tree:
                            //   <governing-expression> [|switch { <arm>, ..., <arm> }|]
                            // This active statement is never a leaf active statement (does not correspond to a breakpoint span).

                            var switchExpression = (SwitchExpressionSyntax)node;
                            if (position == switchExpression.SwitchKeyword.SpanStart)
                            {
                                Debug.Assert(span.End == switchExpression.CloseBraceToken.Span.End);
                                statementPart = (int)SwitchExpressionPart.SwitchBody;
                                return node;
                            }

                            // The switch expression itself can be (a part of) an active statement associated with the containing node
                            // For example, when it is used as a switch arm expression like so: 
                            //   <expr> switch { <pattern> [|when <expr> switch { ... }|] ... }
                            Debug.Assert(position == switchExpression.Span.Start);
                            if (isBody)
                            {
                                goto default;
                            }

                            // ascend to parent node:
                            break;

                        case SyntaxKind.SwitchExpressionArm:
                            // An active statement may occur in the when clause and in the arm expression:
                            //   <constant-pattern> [|when <condition>|] => [|<expression>|]
                            // The former is covered by when-clause node - it's a labeled node.
                            // The latter isn't enclosed in a distinct labeled syntax node and thus needs to be covered 
                            // by the arm node itself.
                            Debug.Assert(position == ((SwitchExpressionArmSyntax)node).Expression.SpanStart);
                            Debug.Assert(!isBody);
                            goto default;

                        default:
                            statementPart = DefaultStatementPart;
                            return node;
                    }
                }

                node = node.Parent!;
                if (partner != null)
                {
                    partner = partner.Parent;
                }
            }
        }

        private static BlockPart GetStatementPart(BlockSyntax node, int position)
            => position < node.OpenBraceToken.Span.End ? BlockPart.OpenBrace : BlockPart.CloseBrace;

        private static TextSpan GetActiveSpan(BlockSyntax node, BlockPart part)
            => part switch
            {
                BlockPart.OpenBrace => node.OpenBraceToken.Span,
                BlockPart.CloseBrace => node.CloseBraceToken.Span,
                _ => throw ExceptionUtilities.UnexpectedValue(part),
            };

        private static ForEachPart GetStatementPart(CommonForEachStatementSyntax node, int position)
            => position < node.OpenParenToken.SpanStart ? ForEachPart.ForEach :
               position < node.InKeyword.SpanStart ? ForEachPart.VariableDeclaration :
               position < node.Expression.SpanStart ? ForEachPart.In :
               ForEachPart.Expression;

        private static TextSpan GetActiveSpan(ForEachStatementSyntax node, ForEachPart part)
            => part switch
            {
                ForEachPart.ForEach => node.ForEachKeyword.Span,
                ForEachPart.VariableDeclaration => TextSpan.FromBounds(node.Type.SpanStart, node.Identifier.Span.End),
                ForEachPart.In => node.InKeyword.Span,
                ForEachPart.Expression => node.Expression.Span,
                _ => throw ExceptionUtilities.UnexpectedValue(part),
            };

        private static TextSpan GetActiveSpan(ForEachVariableStatementSyntax node, ForEachPart part)
            => part switch
            {
                ForEachPart.ForEach => node.ForEachKeyword.Span,
                ForEachPart.VariableDeclaration => TextSpan.FromBounds(node.Variable.SpanStart, node.Variable.Span.End),
                ForEachPart.In => node.InKeyword.Span,
                ForEachPart.Expression => node.Expression.Span,
                _ => throw ExceptionUtilities.UnexpectedValue(part),
            };

        private static TextSpan GetActiveSpan(SwitchExpressionSyntax node, SwitchExpressionPart part)
            => part switch
            {
                SwitchExpressionPart.WholeExpression => node.Span,
                SwitchExpressionPart.SwitchBody => TextSpan.FromBounds(node.SwitchKeyword.SpanStart, node.CloseBraceToken.Span.End),
                _ => throw ExceptionUtilities.UnexpectedValue(part),
            };

        protected override bool AreEquivalent(SyntaxNode left, SyntaxNode right)
            => SyntaxFactory.AreEquivalent(left, right);

        private static bool AreEquivalentIgnoringLambdaBodies(SyntaxNode left, SyntaxNode right)
        {
            // usual case:
            if (SyntaxFactory.AreEquivalent(left, right))
            {
                return true;
            }

            return LambdaUtilities.AreEquivalentIgnoringLambdaBodies(left, right);
        }

        internal override SyntaxNode FindDeclarationBodyPartner(SyntaxNode leftRoot, SyntaxNode rightRoot, SyntaxNode leftNode)
            => SyntaxUtilities.FindPartner(leftRoot, rightRoot, leftNode);

        internal override bool IsClosureScope(SyntaxNode node)
            => LambdaUtilities.IsClosureScope(node);

        protected override SyntaxNode? FindEnclosingLambdaBody(SyntaxNode? container, SyntaxNode node)
        {
            var root = GetEncompassingAncestor(container);

            var current = node;
            while (current != root && current != null)
            {
                if (LambdaUtilities.IsLambdaBodyStatementOrExpression(current, out var body))
                {
                    return body;
                }

                current = current.Parent;
            }

            return null;
        }

        protected override IEnumerable<SyntaxNode> GetLambdaBodyExpressionsAndStatements(SyntaxNode lambdaBody)
            => SpecializedCollections.SingletonEnumerable(lambdaBody);

        protected override SyntaxNode? TryGetPartnerLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda)
            => LambdaUtilities.TryGetCorrespondingLambdaBody(oldBody, newLambda);

        protected override Match<SyntaxNode> ComputeTopLevelMatch(SyntaxNode oldCompilationUnit, SyntaxNode newCompilationUnit)
            => SyntaxComparer.TopLevel.ComputeMatch(oldCompilationUnit, newCompilationUnit);

        protected override Match<SyntaxNode> ComputeTopLevelDeclarationMatch(SyntaxNode oldDeclaration, SyntaxNode newDeclaration)
        {
            Contract.ThrowIfNull(oldDeclaration.Parent);
            Contract.ThrowIfNull(newDeclaration.Parent);
            var comparer = new SyntaxComparer(oldDeclaration.Parent, newDeclaration.Parent, new[] { oldDeclaration }, new[] { newDeclaration }, compareStatementSyntax: false);
            return comparer.ComputeMatch(oldDeclaration.Parent, newDeclaration.Parent);
        }

        protected override Match<SyntaxNode> ComputeBodyMatch(SyntaxNode oldBody, SyntaxNode newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        {
            SyntaxUtilities.AssertIsBody(oldBody, allowLambda: true);
            SyntaxUtilities.AssertIsBody(newBody, allowLambda: true);

            if (oldBody is ExpressionSyntax || newBody is ExpressionSyntax || (oldBody.Parent.IsKind(SyntaxKind.LocalFunctionStatement) && newBody.Parent.IsKind(SyntaxKind.LocalFunctionStatement)))
            {
                Debug.Assert(oldBody is ExpressionSyntax || oldBody is BlockSyntax);
                Debug.Assert(newBody is ExpressionSyntax || newBody is BlockSyntax);

                // The matching algorithm requires the roots to match each other.
                // Lambda bodies, field/property initializers, and method/property/indexer/operator expression-bodies may also be lambda expressions.
                // Say we have oldBody 'x => x' and newBody 'F(x => x + 1)', then 
                // the algorithm would match 'x => x' to 'F(x => x + 1)' instead of 
                // matching 'x => x' to 'x => x + 1'.

                // We use the parent node as a root:
                // - for field/property initializers the root is EqualsValueClause. 
                // - for member expression-bodies the root is ArrowExpressionClauseSyntax.
                // - for block bodies the root is a method/operator/accessor declaration (only happens when matching expression body with a block body)
                // - for lambdas the root is a LambdaExpression.
                // - for query lambdas the root is the query clause containing the lambda (e.g. where).
                // - for local functions the root is LocalFunctionStatement.

                static SyntaxNode GetMatchingRoot(SyntaxNode body)
                {
                    var parent = body.Parent!;
                    // We could apply this change across all ArrowExpressionClause consistently not just for ones with LocalFunctionStatement parents
                    // but it would require an essential refactoring. 
                    return parent.IsKind(SyntaxKind.ArrowExpressionClause) && parent.Parent.IsKind(SyntaxKind.LocalFunctionStatement) ? parent.Parent : parent;
                }

                var oldRoot = GetMatchingRoot(oldBody);
                var newRoot = GetMatchingRoot(newBody);
                return new SyntaxComparer(oldRoot, newRoot, GetChildNodes(oldRoot, oldBody), GetChildNodes(newRoot, newBody), compareStatementSyntax: true).ComputeMatch(oldRoot, newRoot, knownMatches);
            }

            if (oldBody.Parent.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                // We need to include constructor initializer in the match, since it may contain lambdas.
                // Use the constructor declaration as a root.
                RoslynDebug.Assert(oldBody.IsKind(SyntaxKind.Block));
                RoslynDebug.Assert(newBody.IsKind(SyntaxKind.Block));
                RoslynDebug.Assert(newBody.Parent.IsKind(SyntaxKind.ConstructorDeclaration));
                RoslynDebug.Assert(newBody.Parent is object);

                return SyntaxComparer.Statement.ComputeMatch(oldBody.Parent, newBody.Parent, knownMatches);
            }

            return SyntaxComparer.Statement.ComputeMatch(oldBody, newBody, knownMatches);
        }

        private static IEnumerable<SyntaxNode> GetChildNodes(SyntaxNode root, SyntaxNode body)
        {
            if (root is LocalFunctionStatementSyntax localFunc)
            {
                // local functions have multiple children we need to process for matches, but we won't automatically
                // descend into them, assuming they're nested, so we override the default behaviour and return
                // multiple children
                foreach (var attributeList in localFunc.AttributeLists)
                {
                    yield return attributeList;
                }

                yield return localFunc.ReturnType;

                if (localFunc.TypeParameterList is not null)
                {
                    yield return localFunc.TypeParameterList;
                }

                yield return localFunc.ParameterList;

                if (localFunc.Body is not null)
                {
                    yield return localFunc.Body;
                }
                else if (localFunc.ExpressionBody is not null)
                {
                    // Skip the ArrowExpressionClause that is ExressionBody and just return the expression itself
                    yield return localFunc.ExpressionBody.Expression;
                }
            }
            else
            {
                yield return body;
            }
        }

        internal override void ReportDeclarationInsertDeleteRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode, ISymbol oldSymbol, ISymbol newSymbol)
        {
            // Compiler generated methods of records have a declaring syntax reference to the record declaration itself
            // but their explicitly implemented counterparts reference the actual member. Compiler generated properties
            // of records reference the parameter that names them.
            //
            // Since there is no useful "old" syntax node for these members, we can't compute declaration or body edits
            // using the standard tree comparison code.
            //
            // Based on this, we can detect a new explicit implementation of a record member by checking if the
            // declaration kind has changed. If it hasn't changed, then our standard code will handle it.
            if (oldNode.RawKind == newNode.RawKind)
            {
                base.ReportDeclarationInsertDeleteRudeEdits(diagnostics, oldNode, newNode, oldSymbol, newSymbol);
                return;
            }

            // When explicitly implementing a property that is represented by a positional parameter
            // what looks like an edit could actually be a rude delete, or something else
            if (oldNode is ParameterSyntax &&
                newNode is PropertyDeclarationSyntax property)
            {
                if (property.AccessorList!.Accessors.Count == 1)
                {
                    // Explicitly implementing a property with only one accessor is a delete of the init accessor, so a rude edit.
                    // Not implementing the get accessor would be a compile error

                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ImplementRecordParameterAsReadOnly,
                        GetDiagnosticSpan(newNode, EditKind.Delete),
                        oldNode,
                        new[] {
                            property.Identifier.ToString()
                        }));
                }
                else if (property.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
                {
                    // The compiler implements the properties with an init accessor so explicitly implementing
                    // it with a set accessor is a rude accessor change edit

                    diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ImplementRecordParameterWithSet,
                        GetDiagnosticSpan(newNode, EditKind.Delete),
                        oldNode,
                        new[] {
                            property.Identifier.ToString()
                        }));
                }
            }
            else if (oldNode is RecordDeclarationSyntax &&
                newNode is MethodDeclarationSyntax &&
                !oldSymbol.GetParameters().Select(p => p.Name).SequenceEqual(newSymbol.GetParameters().Select(p => p.Name)))
            {
                // TODO: Remove this requirement with https://github.com/dotnet/roslyn/issues/52563
                // Explicitly implemented methods must have parameter names that match the compiler generated versions
                // exactly otherwise symbol matching won't work for them.
                // We don't need to worry about parameter types, because if they were different then we wouldn't get here
                // as this wouldn't be the explicit implementation of a known method.
                // We don't need to worry about access modifiers because the symbol matching still works, and most of the
                // time changing access modifiers for these known methods is a compile error anyway.

                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.ExplicitRecordMethodParameterNamesMustMatch,
                    GetDiagnosticSpan(newNode, EditKind.Update),
                    oldNode,
                    new[] {
                            oldSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat)
                    }));
            }
        }

        protected override void ReportLocalFunctionsDeclarationRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> bodyMatch)
        {
            var bodyEditsForLambda = bodyMatch.GetTreeEdits();
            var editMap = BuildEditMap(bodyEditsForLambda);
            foreach (var edit in bodyEditsForLambda.Edits)
            {
                if (HasParentEdit(editMap, edit))
                {
                    return;
                }

                var classifier = new EditClassifier(this, diagnostics, edit.OldNode, edit.NewNode, edit.Kind, bodyMatch, classifyStatementSyntax: true);
                classifier.ClassifyEdit();
            }
        }

        protected override bool TryMatchActiveStatement(
            SyntaxNode oldStatement,
            int statementPart,
            SyntaxNode oldBody,
            SyntaxNode newBody,
            [NotNullWhen(true)] out SyntaxNode? newStatement)
        {
            SyntaxUtilities.AssertIsBody(oldBody, allowLambda: true);
            SyntaxUtilities.AssertIsBody(newBody, allowLambda: true);

            switch (oldStatement.Kind())
            {
                case SyntaxKind.ThisConstructorInitializer:
                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ConstructorDeclaration:
                    var newConstructor = (ConstructorDeclarationSyntax)(newBody.Parent.IsKind(SyntaxKind.ArrowExpressionClause) ? newBody.Parent.Parent : newBody.Parent)!;
                    newStatement = (SyntaxNode?)newConstructor.Initializer ?? newConstructor;
                    return true;

                default:
                    // TODO: Consider mapping an expression body to an equivalent statement expression or return statement and vice versa.
                    // It would benefit transformations of expression bodies to block bodies of lambdas, methods, operators and properties.
                    // See https://github.com/dotnet/roslyn/issues/22696

                    // field initializer, lambda and query expressions:
                    if (oldStatement == oldBody && !newBody.IsKind(SyntaxKind.Block))
                    {
                        newStatement = newBody;
                        return true;
                    }

                    newStatement = null;
                    return false;
            }
        }

        #endregion

        #region Syntax and Semantic Utils

        protected override string LineDirectiveKeyword
            => "line";

        protected override ushort LineDirectiveSyntaxKind
            => (ushort)SyntaxKind.LineDirectiveTrivia;

        protected override IEnumerable<SequenceEdit> GetSyntaxSequenceEdits(ImmutableArray<SyntaxNode> oldNodes, ImmutableArray<SyntaxNode> newNodes)
            => SyntaxComparer.GetSequenceEdits(oldNodes, newNodes);

        internal override SyntaxNode EmptyCompilationUnit
            => SyntaxFactory.CompilationUnit();

        // there are no experimental features at this time.
        internal override bool ExperimentalFeaturesEnabled(SyntaxTree tree)
            => false;

        protected override bool StateMachineSuspensionPointKindEquals(SyntaxNode suspensionPoint1, SyntaxNode suspensionPoint2)
            => (suspensionPoint1 is CommonForEachStatementSyntax) ? suspensionPoint2 is CommonForEachStatementSyntax : suspensionPoint1.RawKind == suspensionPoint2.RawKind;

        protected override bool StatementLabelEquals(SyntaxNode node1, SyntaxNode node2)
            => SyntaxComparer.Statement.GetLabel(node1) == SyntaxComparer.Statement.GetLabel(node2);

        protected override bool TryGetEnclosingBreakpointSpan(SyntaxNode root, int position, out TextSpan span)
            => BreakpointSpans.TryGetClosestBreakpointSpan(root, position, out span);

        protected override bool TryGetActiveSpan(SyntaxNode node, int statementPart, int minLength, out TextSpan span)
        {
            switch (node.Kind())
            {
                case SyntaxKind.Block:
                    span = GetActiveSpan((BlockSyntax)node, (BlockPart)statementPart);
                    return true;

                case SyntaxKind.ForEachStatement:
                    span = GetActiveSpan((ForEachStatementSyntax)node, (ForEachPart)statementPart);
                    return true;

                case SyntaxKind.ForEachVariableStatement:
                    span = GetActiveSpan((ForEachVariableStatementSyntax)node, (ForEachPart)statementPart);
                    return true;

                case SyntaxKind.DoStatement:
                    // The active statement of DoStatement node is the while condition,
                    // which is lexically not the closest breakpoint span (the body is).
                    // do { ... } [|while (condition);|]
                    Debug.Assert(statementPart == DefaultStatementPart);

                    var doStatement = (DoStatementSyntax)node;
                    return BreakpointSpans.TryGetClosestBreakpointSpan(node, doStatement.WhileKeyword.SpanStart, out span);

                case SyntaxKind.PropertyDeclaration:
                    // The active span corresponding to a property declaration is the span corresponding to its initializer (if any),
                    // not the span corresponding to the accessor.
                    // int P { [|get;|] } = [|<initializer>|];
                    Debug.Assert(statementPart == DefaultStatementPart);

                    var propertyDeclaration = (PropertyDeclarationSyntax)node;

                    if (propertyDeclaration.Initializer != null &&
                        BreakpointSpans.TryGetClosestBreakpointSpan(node, propertyDeclaration.Initializer.SpanStart, out span))
                    {
                        return true;
                    }

                    span = default;
                    return false;

                case SyntaxKind.SwitchExpression:
                    span = GetActiveSpan((SwitchExpressionSyntax)node, (SwitchExpressionPart)statementPart);
                    return true;

                case SyntaxKind.SwitchExpressionArm:
                    // An active statement may occur in the when clause and in the arm expression:
                    //   <constant-pattern> [|when <condition>|] => [|<expression>|]
                    // The former is covered by when-clause node - it's a labeled node.
                    // The latter isn't enclosed in a distinct labeled syntax node and thus needs to be covered 
                    // by the arm node itself.
                    Debug.Assert(statementPart == DefaultStatementPart);

                    span = ((SwitchExpressionArmSyntax)node).Expression.Span;
                    return true;

                default:
                    // make sure all nodes that use statement parts are handled above:
                    Debug.Assert(statementPart == DefaultStatementPart);

                    return BreakpointSpans.TryGetClosestBreakpointSpan(node, node.SpanStart, out span);
            }
        }

        protected override IEnumerable<(SyntaxNode statement, int statementPart)> EnumerateNearStatements(SyntaxNode statement)
        {
            var direction = +1;
            SyntaxNodeOrToken nodeOrToken = statement;
            var fieldOrPropertyModifiers = SyntaxUtilities.TryGetFieldOrPropertyModifiers(statement);

            while (true)
            {
                nodeOrToken = (direction < 0) ? nodeOrToken.GetPreviousSibling() : nodeOrToken.GetNextSibling();

                if (nodeOrToken.RawKind == 0)
                {
                    var parent = statement.Parent;
                    if (parent == null)
                    {
                        yield break;
                    }

                    switch (parent.Kind())
                    {
                        case SyntaxKind.Block:
                            // The next sequence point hit after the last statement of a block is the closing brace:
                            yield return (parent, (int)(direction > 0 ? BlockPart.CloseBrace : BlockPart.OpenBrace));
                            break;

                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.ForEachVariableStatement:
                            // The next sequence point hit after the body is the in keyword:
                            //   [|foreach|] ([|variable-declaration|] [|in|] [|expression|]) [|<body>|]
                            yield return (parent, (int)ForEachPart.In);
                            break;
                    }

                    if (direction > 0)
                    {
                        nodeOrToken = statement;
                        direction = -1;
                        continue;
                    }

                    if (fieldOrPropertyModifiers.HasValue)
                    {
                        // We enumerated all members and none of them has an initializer.
                        // We don't have any better place where to place the span than the initial field.
                        // Consider: in non-partial classes we could find a single constructor. 
                        // Otherwise, it would be confusing to select one arbitrarily.
                        yield return (statement, -1);
                    }

                    nodeOrToken = statement = parent;
                    fieldOrPropertyModifiers = SyntaxUtilities.TryGetFieldOrPropertyModifiers(statement);
                    direction = +1;

                    yield return (nodeOrToken.AsNode()!, DefaultStatementPart);
                }
                else
                {
                    var node = nodeOrToken.AsNode();
                    if (node == null)
                    {
                        continue;
                    }

                    if (fieldOrPropertyModifiers.HasValue)
                    {
                        var nodeModifiers = SyntaxUtilities.TryGetFieldOrPropertyModifiers(node);

                        if (!nodeModifiers.HasValue ||
                            nodeModifiers.Value.Any(SyntaxKind.StaticKeyword) != fieldOrPropertyModifiers.Value.Any(SyntaxKind.StaticKeyword))
                        {
                            continue;
                        }
                    }

                    switch (node.Kind())
                    {
                        case SyntaxKind.Block:
                            yield return (node, (int)(direction > 0 ? BlockPart.OpenBrace : BlockPart.CloseBrace));
                            break;

                        case SyntaxKind.ForEachStatement:
                        case SyntaxKind.ForEachVariableStatement:
                            yield return (node, (int)ForEachPart.ForEach);
                            break;
                    }

                    yield return (node, DefaultStatementPart);
                }
            }
        }

        protected override bool AreEquivalentActiveStatements(SyntaxNode oldStatement, SyntaxNode newStatement, int statementPart)
        {
            if (oldStatement.Kind() != newStatement.Kind())
            {
                return false;
            }

            switch (oldStatement.Kind())
            {
                case SyntaxKind.Block:
                    // closing brace of a using statement or a block that contains using local declarations:
                    if (statementPart == (int)BlockPart.CloseBrace)
                    {
                        if (oldStatement.Parent.IsKind(SyntaxKind.UsingStatement, out UsingStatementSyntax? oldUsing))
                        {
                            return newStatement.Parent.IsKind(SyntaxKind.UsingStatement, out UsingStatementSyntax? newUsing) &&
                                AreEquivalentActiveStatements(oldUsing, newUsing);
                        }

                        return HasEquivalentUsingDeclarations((BlockSyntax)oldStatement, (BlockSyntax)newStatement);
                    }

                    return true;

                case SyntaxKind.ConstructorDeclaration:
                    // The call could only change if the base type of the containing class changed.
                    return true;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    // only check the expression, edits in the body and the variable declaration are allowed:
                    return AreEquivalentActiveStatements((CommonForEachStatementSyntax)oldStatement, (CommonForEachStatementSyntax)newStatement);

                case SyntaxKind.IfStatement:
                    // only check the condition, edits in the body are allowed:
                    return AreEquivalentActiveStatements((IfStatementSyntax)oldStatement, (IfStatementSyntax)newStatement);

                case SyntaxKind.WhileStatement:
                    // only check the condition, edits in the body are allowed:
                    return AreEquivalentActiveStatements((WhileStatementSyntax)oldStatement, (WhileStatementSyntax)newStatement);

                case SyntaxKind.DoStatement:
                    // only check the condition, edits in the body are allowed:
                    return AreEquivalentActiveStatements((DoStatementSyntax)oldStatement, (DoStatementSyntax)newStatement);

                case SyntaxKind.SwitchStatement:
                    return AreEquivalentActiveStatements((SwitchStatementSyntax)oldStatement, (SwitchStatementSyntax)newStatement);

                case SyntaxKind.LockStatement:
                    return AreEquivalentActiveStatements((LockStatementSyntax)oldStatement, (LockStatementSyntax)newStatement);

                case SyntaxKind.UsingStatement:
                    return AreEquivalentActiveStatements((UsingStatementSyntax)oldStatement, (UsingStatementSyntax)newStatement);

                // fixed and for statements don't need special handling since the active statement is a variable declaration
                default:
                    return AreEquivalentIgnoringLambdaBodies(oldStatement, newStatement);
            }
        }

        private static bool HasEquivalentUsingDeclarations(BlockSyntax oldBlock, BlockSyntax newBlock)
        {
            var oldUsingDeclarations = oldBlock.Statements.Where(s => s is LocalDeclarationStatementSyntax l && l.UsingKeyword != default);
            var newUsingDeclarations = newBlock.Statements.Where(s => s is LocalDeclarationStatementSyntax l && l.UsingKeyword != default);

            return oldUsingDeclarations.SequenceEqual(newUsingDeclarations, AreEquivalentIgnoringLambdaBodies);
        }

        private static bool AreEquivalentActiveStatements(IfStatementSyntax oldNode, IfStatementSyntax newNode)
        {
            // only check the condition, edits in the body are allowed:
            return AreEquivalentIgnoringLambdaBodies(oldNode.Condition, newNode.Condition);
        }

        private static bool AreEquivalentActiveStatements(WhileStatementSyntax oldNode, WhileStatementSyntax newNode)
        {
            // only check the condition, edits in the body are allowed:
            return AreEquivalentIgnoringLambdaBodies(oldNode.Condition, newNode.Condition);
        }

        private static bool AreEquivalentActiveStatements(DoStatementSyntax oldNode, DoStatementSyntax newNode)
        {
            // only check the condition, edits in the body are allowed:
            return AreEquivalentIgnoringLambdaBodies(oldNode.Condition, newNode.Condition);
        }

        private static bool AreEquivalentActiveStatements(SwitchStatementSyntax oldNode, SwitchStatementSyntax newNode)
        {
            // only check the expression, edits in the body are allowed, unless the switch expression contains patterns:
            if (!AreEquivalentIgnoringLambdaBodies(oldNode.Expression, newNode.Expression))
            {
                return false;
            }

            // Check that switch statement decision tree has not changed.
            var hasDecitionTree = oldNode.Sections.Any(s => s.Labels.Any(l => l is CasePatternSwitchLabelSyntax));
            return !hasDecitionTree || AreEquivalentSwitchStatementDecisionTrees(oldNode, newNode);
        }

        private static bool AreEquivalentActiveStatements(LockStatementSyntax oldNode, LockStatementSyntax newNode)
        {
            // only check the expression, edits in the body are allowed:
            return AreEquivalentIgnoringLambdaBodies(oldNode.Expression, newNode.Expression);
        }

        private static bool AreEquivalentActiveStatements(FixedStatementSyntax oldNode, FixedStatementSyntax newNode)
            => AreEquivalentIgnoringLambdaBodies(oldNode.Declaration, newNode.Declaration);

        private static bool AreEquivalentActiveStatements(UsingStatementSyntax oldNode, UsingStatementSyntax newNode)
        {
            // only check the expression/declaration, edits in the body are allowed:
            return AreEquivalentIgnoringLambdaBodies(
                (SyntaxNode?)oldNode.Declaration ?? oldNode.Expression!,
                (SyntaxNode?)newNode.Declaration ?? newNode.Expression!);
        }

        private static bool AreEquivalentActiveStatements(CommonForEachStatementSyntax oldNode, CommonForEachStatementSyntax newNode)
        {
            if (oldNode.Kind() != newNode.Kind() || !AreEquivalentIgnoringLambdaBodies(oldNode.Expression, newNode.Expression))
            {
                return false;
            }

            switch (oldNode.Kind())
            {
                case SyntaxKind.ForEachStatement: return AreEquivalentIgnoringLambdaBodies(((ForEachStatementSyntax)oldNode).Type, ((ForEachStatementSyntax)newNode).Type);
                case SyntaxKind.ForEachVariableStatement: return AreEquivalentIgnoringLambdaBodies(((ForEachVariableStatementSyntax)oldNode).Variable, ((ForEachVariableStatementSyntax)newNode).Variable);
                default: throw ExceptionUtilities.UnexpectedValue(oldNode.Kind());
            }
        }

        private static bool AreSimilarActiveStatements(CommonForEachStatementSyntax oldNode, CommonForEachStatementSyntax newNode)
        {
            List<SyntaxToken>? oldTokens = null;
            List<SyntaxToken>? newTokens = null;

            SyntaxComparer.GetLocalNames(oldNode, ref oldTokens);
            SyntaxComparer.GetLocalNames(newNode, ref newTokens);

            // A valid foreach statement declares at least one variable.
            RoslynDebug.Assert(oldTokens != null);
            RoslynDebug.Assert(newTokens != null);

            return DeclareSameIdentifiers(oldTokens.ToArray(), newTokens.ToArray());
        }

        internal override bool IsInterfaceDeclaration(SyntaxNode node)
            => node.IsKind(SyntaxKind.InterfaceDeclaration);

        internal override bool IsRecordDeclaration(SyntaxNode node)
            => node.IsKind(SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);

        internal override SyntaxNode? TryGetContainingTypeDeclaration(SyntaxNode node)
            => node.Parent!.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();

        internal override bool HasBackingField(SyntaxNode propertyOrIndexerDeclaration)
            => propertyOrIndexerDeclaration.IsKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax? propertyDecl) &&
               SyntaxUtilities.HasBackingField(propertyDecl);

        internal override SyntaxNode? TryGetAssociatedMemberDeclaration(SyntaxNode node)
            => node.Parent.IsParentKind(SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.EventDeclaration) ? node.Parent.Parent : null;

        internal override bool IsDeclarationWithInitializer(SyntaxNode declaration)
            => declaration is VariableDeclaratorSyntax { Initializer: not null } || declaration is PropertyDeclarationSyntax { Initializer: not null };

        internal override bool IsRecordPrimaryConstructorParameter(SyntaxNode declaration)
            => declaration is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } };

        private static bool IsPropertyDeclarationMatchingPrimaryConstructorParameter(SyntaxNode declaration, INamedTypeSymbol newContainingType)
        {
            if (newContainingType.IsRecord &&
                declaration is PropertyDeclarationSyntax { Identifier: { ValueText: var name } })
            {
                // We need to use symbol information to find the primary constructor, because it could be in another file if the type is partial
                foreach (var reference in newContainingType.DeclaringSyntaxReferences)
                {
                    // Since users can define as many constructors as they like, going back to syntax to find the parameter list
                    // in the record declaration is the simplest way to check if there is a matching parameter
                    if (reference.GetSyntax() is RecordDeclarationSyntax record &&
                        record.ParameterList is not null &&
                        record.ParameterList.Parameters.Any(p => p.Identifier.ValueText.Equals(name)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal override bool IsPropertyAccessorDeclarationMatchingPrimaryConstructorParameter(SyntaxNode declaration, INamedTypeSymbol newContainingType, out bool isFirstAccessor)
        {
            isFirstAccessor = false;
            if (declaration is AccessorDeclarationSyntax { Parent: AccessorListSyntax { Parent: PropertyDeclarationSyntax property } list } &&
                IsPropertyDeclarationMatchingPrimaryConstructorParameter(property, newContainingType))
            {
                isFirstAccessor = list.Accessors[0] == declaration;
                return true;
            }
            return false;
        }

        internal override bool IsConstructorWithMemberInitializers(SyntaxNode constructorDeclaration)
            => constructorDeclaration is ConstructorDeclarationSyntax ctor && (ctor.Initializer == null || ctor.Initializer.IsKind(SyntaxKind.BaseConstructorInitializer));

        internal override bool IsPartial(INamedTypeSymbol type)
        {
            var syntaxRefs = type.DeclaringSyntaxReferences;
            return syntaxRefs.Length > 1
                || ((BaseTypeDeclarationSyntax)syntaxRefs.Single().GetSyntax()).Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        protected override SyntaxNode GetSymbolDeclarationSyntax(SyntaxReference reference, CancellationToken cancellationToken)
            => reference.GetSyntax(cancellationToken);

        protected override OneOrMany<(ISymbol? oldSymbol, ISymbol? newSymbol)> GetSymbolsForEdit(
            EditKind editKind,
            SyntaxNode? oldNode,
            SyntaxNode? newNode,
            SemanticModel? oldModel,
            SemanticModel newModel,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            CancellationToken cancellationToken)
        {
            var oldSymbol = (oldNode != null) ? GetSymbolForEdit(oldNode, editKind, oldModel!, editMap, cancellationToken) : null;
            var newSymbol = (newNode != null) ? GetSymbolForEdit(newNode, editKind, newModel, editMap, cancellationToken) : null;

            switch (editKind)
            {
                case EditKind.Update:
                    Contract.ThrowIfNull(oldNode);
                    Contract.ThrowIfNull(newNode);

                    // Either old or new property/indexer has an expression body (or both do).
                    //   int this[...] => expr;
                    //   int this[...] { get => expr; }
                    //   int P => expr;
                    //   int P { get => expr; } = init
                    if (oldNode is PropertyDeclarationSyntax { ExpressionBody: not null } or IndexerDeclarationSyntax { ExpressionBody: not null } ||
                        newNode is PropertyDeclarationSyntax { ExpressionBody: not null } or IndexerDeclarationSyntax { ExpressionBody: not null })
                    {
                        Debug.Assert(oldSymbol is IPropertySymbol);
                        Debug.Assert(newSymbol is IPropertySymbol);

                        var oldGetterSymbol = ((IPropertySymbol)oldSymbol).GetMethod;
                        var newGetterSymbol = ((IPropertySymbol)newSymbol).GetMethod;

                        return OneOrMany.Create(ImmutableArray.Create((oldSymbol, newSymbol), (oldGetterSymbol, newGetterSymbol)));
                    }

                    break;

                case EditKind.Delete:
                case EditKind.Insert:
                    var node = oldNode ?? newNode;

                    // If the entire block-bodied property/indexer is deleted/inserted (accessors and the list they are contained in),
                    // ignore this edit. We will have a semantic edit for the property/indexer itself.
                    if (node.IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        Debug.Assert(node.Parent.IsKind(SyntaxKind.AccessorList));

                        if (HasEdit(editMap, node.Parent, editKind) && !HasEdit(editMap, node.Parent.Parent, editKind))
                        {
                            return OneOrMany<(ISymbol?, ISymbol?)>.Empty;
                        }
                    }

                    // Inserting/deleting an expression-bodied property/indexer affects two symbols:
                    // the property/indexer itself and the getter.
                    // int this[...] => expr;
                    // int P => expr;
                    if (node is PropertyDeclarationSyntax { ExpressionBody: not null } or IndexerDeclarationSyntax { ExpressionBody: not null })
                    {
                        var oldGetterSymbol = ((IPropertySymbol?)oldSymbol)?.GetMethod;
                        var newGetterSymbol = ((IPropertySymbol?)newSymbol)?.GetMethod;
                        return OneOrMany.Create(ImmutableArray.Create((oldSymbol, newSymbol), (oldGetterSymbol, newGetterSymbol)));
                    }

                    break;
            }

            return (editKind == EditKind.Delete ? oldSymbol : newSymbol) is null ?
                OneOrMany<(ISymbol?, ISymbol?)>.Empty : new OneOrMany<(ISymbol?, ISymbol?)>((oldSymbol, newSymbol));
        }

        private static ISymbol? GetSymbolForEdit(
            SyntaxNode node,
            EditKind editKind,
            SemanticModel model,
            IReadOnlyDictionary<SyntaxNode, EditKind> editMap,
            CancellationToken cancellationToken)
        {
            if (node.IsKind(SyntaxKind.Parameter, SyntaxKind.TypeParameter, SyntaxKind.UsingDirective, SyntaxKind.NamespaceDeclaration))
            {
                return null;
            }

            if (editKind == EditKind.Update)
            {
                if (node.IsKind(SyntaxKind.EnumDeclaration))
                {
                    // Enum declaration update that removes/adds a trailing comma.
                    return null;
                }
            }

            var symbol = model.GetDeclaredSymbol(node, cancellationToken);

            // Ignore partial method definition parts.
            // Partial method that does not have implementation part is not emitted to metadata.
            // Partial method without a definition part is a compilation error.
            if (symbol is IMethodSymbol { IsPartialDefinition: true })
            {
                return null;
            }

            return symbol;
        }

        internal override bool ContainsLambda(SyntaxNode declaration)
            => declaration.DescendantNodes().Any(LambdaUtilities.IsLambda);

        internal override bool IsLambda(SyntaxNode node)
            => LambdaUtilities.IsLambda(node);

        internal override bool IsLocalFunction(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalFunctionStatement);

        internal override bool IsNestedFunction(SyntaxNode node)
            => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax;

        internal override bool TryGetLambdaBodies(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? body1, out SyntaxNode? body2)
            => LambdaUtilities.TryGetLambdaBodies(node, out body1, out body2);

        internal override SyntaxNode GetLambda(SyntaxNode lambdaBody)
            => LambdaUtilities.GetLambda(lambdaBody);

        internal override IMethodSymbol GetLambdaExpressionSymbol(SemanticModel model, SyntaxNode lambdaExpression, CancellationToken cancellationToken)
        {
            var bodyExpression = LambdaUtilities.GetNestedFunctionBody(lambdaExpression);
            return (IMethodSymbol)model.GetRequiredEnclosingSymbol(bodyExpression.SpanStart, cancellationToken);
        }

        internal override SyntaxNode? GetContainingQueryExpression(SyntaxNode node)
            => node.FirstAncestorOrSelf<QueryExpressionSyntax>();

        internal override bool QueryClauseLambdasTypeEquivalent(SemanticModel oldModel, SyntaxNode oldNode, SemanticModel newModel, SyntaxNode newNode, CancellationToken cancellationToken)
        {
            switch (oldNode.Kind())
            {
                case SyntaxKind.FromClause:
                case SyntaxKind.LetClause:
                case SyntaxKind.WhereClause:
                case SyntaxKind.OrderByClause:
                case SyntaxKind.JoinClause:
                    var oldQueryClauseInfo = oldModel.GetQueryClauseInfo((QueryClauseSyntax)oldNode, cancellationToken);
                    var newQueryClauseInfo = newModel.GetQueryClauseInfo((QueryClauseSyntax)newNode, cancellationToken);

                    return MemberSignaturesEquivalent(oldQueryClauseInfo.CastInfo.Symbol, newQueryClauseInfo.CastInfo.Symbol) &&
                           MemberSignaturesEquivalent(oldQueryClauseInfo.OperationInfo.Symbol, newQueryClauseInfo.OperationInfo.Symbol);

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    var oldOrderingInfo = oldModel.GetSymbolInfo(oldNode, cancellationToken);
                    var newOrderingInfo = newModel.GetSymbolInfo(newNode, cancellationToken);

                    return MemberSignaturesEquivalent(oldOrderingInfo.Symbol, newOrderingInfo.Symbol);

                case SyntaxKind.SelectClause:
                    var oldSelectInfo = oldModel.GetSymbolInfo(oldNode, cancellationToken);
                    var newSelectInfo = newModel.GetSymbolInfo(newNode, cancellationToken);

                    // Changing reduced select clause to a non-reduced form or vice versa
                    // adds/removes a call to Select method, which is a supported change.

                    return oldSelectInfo.Symbol == null ||
                           newSelectInfo.Symbol == null ||
                           MemberSignaturesEquivalent(oldSelectInfo.Symbol, newSelectInfo.Symbol);

                case SyntaxKind.GroupClause:
                    var oldGroupByInfo = oldModel.GetSymbolInfo(oldNode, cancellationToken);
                    var newGroupByInfo = newModel.GetSymbolInfo(newNode, cancellationToken);
                    return MemberSignaturesEquivalent(oldGroupByInfo.Symbol, newGroupByInfo.Symbol, GroupBySignatureComparer);

                default:
                    return true;
            }
        }

        protected override void ReportLambdaSignatureRudeEdits(
            SemanticModel oldModel,
            SyntaxNode oldLambdaBody,
            SemanticModel newModel,
            SyntaxNode newLambdaBody,
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            out bool hasErrors,
            CancellationToken cancellationToken)
        {
            base.ReportLambdaSignatureRudeEdits(oldModel, oldLambdaBody, newModel, newLambdaBody, diagnostics, out hasErrors, cancellationToken);

            if (IsLocalFunctionBody(oldLambdaBody) != IsLocalFunctionBody(newLambdaBody))
            {
                var newLambda = GetLambda(newLambdaBody);
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.SwitchBetweenLambdaAndLocalFunction,
                    GetDiagnosticSpan(newLambda, EditKind.Update),
                    newLambda,
                    new[] { GetDisplayName(newLambda) }));
                hasErrors = true;
            }
        }

        private static bool IsLocalFunctionBody(SyntaxNode lambdaBody)
        {
            var lambda = LambdaUtilities.GetLambda(lambdaBody);
            return lambda.Kind() == SyntaxKind.LocalFunctionStatement;
        }

        private static bool GroupBySignatureComparer(ImmutableArray<IParameterSymbol> oldParameters, ITypeSymbol oldReturnType, ImmutableArray<IParameterSymbol> newParameters, ITypeSymbol newReturnType)
        {
            // C# spec paragraph 7.16.2.6 "Groupby clauses":
            //
            // A query expression of the form
            //   from x in e group v by k
            // is translated into
            //   (e).GroupBy(x => k, x => v)
            // except when v is the identifier x, the translation is
            //   (e).GroupBy(x => k)
            //
            // Possible signatures:
            //   C<G<K, T>> GroupBy<K>(Func<T, K> keySelector);
            //   C<G<K, E>> GroupBy<K, E>(Func<T, K> keySelector, Func<T, E> elementSelector);

            if (!s_assemblyEqualityComparer.Equals(oldReturnType, newReturnType))
            {
                return false;
            }

            Debug.Assert(oldParameters.Length == 1 || oldParameters.Length == 2);
            Debug.Assert(newParameters.Length == 1 || newParameters.Length == 2);

            // The types of the lambdas have to be the same if present.
            // The element selector may be added/removed.

            if (!s_assemblyEqualityComparer.ParameterEquivalenceComparer.Equals(oldParameters[0], newParameters[0]))
            {
                return false;
            }

            if (oldParameters.Length == newParameters.Length && newParameters.Length == 2)
            {
                return s_assemblyEqualityComparer.ParameterEquivalenceComparer.Equals(oldParameters[1], newParameters[1]);
            }

            return true;
        }

        #endregion

        #region Diagnostic Info

        protected override SymbolDisplayFormat ErrorDisplayFormat => SymbolDisplayFormat.CSharpErrorMessageFormat;

        protected override TextSpan? TryGetDiagnosticSpan(SyntaxNode node, EditKind editKind)
            => TryGetDiagnosticSpanImpl(node, editKind);

        internal static new TextSpan GetDiagnosticSpan(SyntaxNode node, EditKind editKind)
            => TryGetDiagnosticSpanImpl(node, editKind) ?? node.Span;

        private static TextSpan? TryGetDiagnosticSpanImpl(SyntaxNode node, EditKind editKind)
            => TryGetDiagnosticSpanImpl(node.Kind(), node, editKind);

        // internal for testing; kind is passed explicitly for testing as well
        internal static TextSpan? TryGetDiagnosticSpanImpl(SyntaxKind kind, SyntaxNode node, EditKind editKind)
        {
            switch (kind)
            {
                case SyntaxKind.CompilationUnit:
                    return default(TextSpan);

                case SyntaxKind.GlobalStatement:
                    // TODO:
                    return node.Span;

                case SyntaxKind.ExternAliasDirective:
                case SyntaxKind.UsingDirective:
                    return node.Span;

                case SyntaxKind.NamespaceDeclaration:
                    var ns = (NamespaceDeclarationSyntax)node;
                    return TextSpan.FromBounds(ns.NamespaceKeyword.SpanStart, ns.Name.Span.End);

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    var typeDeclaration = (TypeDeclarationSyntax)node;
                    return GetDiagnosticSpan(typeDeclaration.Modifiers, typeDeclaration.Keyword,
                        typeDeclaration.TypeParameterList ?? (SyntaxNodeOrToken)typeDeclaration.Identifier);

                case SyntaxKind.EnumDeclaration:
                    var enumDeclaration = (EnumDeclarationSyntax)node;
                    return GetDiagnosticSpan(enumDeclaration.Modifiers, enumDeclaration.EnumKeyword, enumDeclaration.Identifier);

                case SyntaxKind.DelegateDeclaration:
                    var delegateDeclaration = (DelegateDeclarationSyntax)node;
                    return GetDiagnosticSpan(delegateDeclaration.Modifiers, delegateDeclaration.DelegateKeyword, delegateDeclaration.ParameterList);

                case SyntaxKind.FieldDeclaration:
                    var fieldDeclaration = (BaseFieldDeclarationSyntax)node;
                    return GetDiagnosticSpan(fieldDeclaration.Modifiers, fieldDeclaration.Declaration, fieldDeclaration.Declaration);

                case SyntaxKind.EventFieldDeclaration:
                    var eventFieldDeclaration = (EventFieldDeclarationSyntax)node;
                    return GetDiagnosticSpan(eventFieldDeclaration.Modifiers, eventFieldDeclaration.EventKeyword, eventFieldDeclaration.Declaration);

                case SyntaxKind.VariableDeclaration:
                    return TryGetDiagnosticSpanImpl(node.Parent!, editKind);

                case SyntaxKind.VariableDeclarator:
                    return node.Span;

                case SyntaxKind.MethodDeclaration:
                    var methodDeclaration = (MethodDeclarationSyntax)node;
                    return GetDiagnosticSpan(methodDeclaration.Modifiers, methodDeclaration.ReturnType, methodDeclaration.ParameterList);

                case SyntaxKind.ConversionOperatorDeclaration:
                    var conversionOperatorDeclaration = (ConversionOperatorDeclarationSyntax)node;
                    return GetDiagnosticSpan(conversionOperatorDeclaration.Modifiers, conversionOperatorDeclaration.ImplicitOrExplicitKeyword, conversionOperatorDeclaration.ParameterList);

                case SyntaxKind.OperatorDeclaration:
                    var operatorDeclaration = (OperatorDeclarationSyntax)node;
                    return GetDiagnosticSpan(operatorDeclaration.Modifiers, operatorDeclaration.ReturnType, operatorDeclaration.ParameterList);

                case SyntaxKind.ConstructorDeclaration:
                    var constructorDeclaration = (ConstructorDeclarationSyntax)node;
                    return GetDiagnosticSpan(constructorDeclaration.Modifiers, constructorDeclaration.Identifier, constructorDeclaration.ParameterList);

                case SyntaxKind.DestructorDeclaration:
                    var destructorDeclaration = (DestructorDeclarationSyntax)node;
                    return GetDiagnosticSpan(destructorDeclaration.Modifiers, destructorDeclaration.TildeToken, destructorDeclaration.ParameterList);

                case SyntaxKind.PropertyDeclaration:
                    var propertyDeclaration = (PropertyDeclarationSyntax)node;
                    return GetDiagnosticSpan(propertyDeclaration.Modifiers, propertyDeclaration.Type, propertyDeclaration.Identifier);

                case SyntaxKind.IndexerDeclaration:
                    var indexerDeclaration = (IndexerDeclarationSyntax)node;
                    return GetDiagnosticSpan(indexerDeclaration.Modifiers, indexerDeclaration.Type, indexerDeclaration.ParameterList);

                case SyntaxKind.EventDeclaration:
                    var eventDeclaration = (EventDeclarationSyntax)node;
                    return GetDiagnosticSpan(eventDeclaration.Modifiers, eventDeclaration.EventKeyword, eventDeclaration.Identifier);

                case SyntaxKind.EnumMemberDeclaration:
                    return node.Span;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                    var accessorDeclaration = (AccessorDeclarationSyntax)node;
                    return GetDiagnosticSpan(accessorDeclaration.Modifiers, accessorDeclaration.Keyword, accessorDeclaration.Keyword);

                case SyntaxKind.TypeParameterConstraintClause:
                    var constraint = (TypeParameterConstraintClauseSyntax)node;
                    return TextSpan.FromBounds(constraint.WhereKeyword.SpanStart, constraint.Constraints.Last().Span.End);

                case SyntaxKind.TypeParameter:
                    var typeParameter = (TypeParameterSyntax)node;
                    return typeParameter.Identifier.Span;

                case SyntaxKind.AccessorList:
                case SyntaxKind.TypeParameterList:
                case SyntaxKind.ParameterList:
                case SyntaxKind.BracketedParameterList:
                    if (editKind == EditKind.Delete)
                    {
                        return TryGetDiagnosticSpanImpl(node.Parent!, editKind);
                    }
                    else
                    {
                        return node.Span;
                    }

                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)node;
                    // Lambda parameters don't have types or modifiers, so the parameter is the only node
                    var startNode = parameter.Type ?? (SyntaxNode)parameter;
                    return GetDiagnosticSpan(parameter.Modifiers, startNode, parameter);

                case SyntaxKind.AttributeList:
                    var attributeList = (AttributeListSyntax)node;
                    return attributeList.Span;

                case SyntaxKind.Attribute:
                    return node.Span;

                case SyntaxKind.ArrowExpressionClause:
                    return TryGetDiagnosticSpanImpl(node.Parent!, editKind);

                // We only need a diagnostic span if reporting an error for a child statement.
                // The following statements may have child statements.

                case SyntaxKind.Block:
                    return ((BlockSyntax)node).OpenBraceToken.Span;

                case SyntaxKind.UsingStatement:
                    var usingStatement = (UsingStatementSyntax)node;
                    return TextSpan.FromBounds(usingStatement.UsingKeyword.SpanStart, usingStatement.CloseParenToken.Span.End);

                case SyntaxKind.FixedStatement:
                    var fixedStatement = (FixedStatementSyntax)node;
                    return TextSpan.FromBounds(fixedStatement.FixedKeyword.SpanStart, fixedStatement.CloseParenToken.Span.End);

                case SyntaxKind.LockStatement:
                    var lockStatement = (LockStatementSyntax)node;
                    return TextSpan.FromBounds(lockStatement.LockKeyword.SpanStart, lockStatement.CloseParenToken.Span.End);

                case SyntaxKind.StackAllocArrayCreationExpression:
                    return ((StackAllocArrayCreationExpressionSyntax)node).StackAllocKeyword.Span;

                case SyntaxKind.ImplicitStackAllocArrayCreationExpression:
                    return ((ImplicitStackAllocArrayCreationExpressionSyntax)node).StackAllocKeyword.Span;

                case SyntaxKind.TryStatement:
                    return ((TryStatementSyntax)node).TryKeyword.Span;

                case SyntaxKind.CatchClause:
                    return ((CatchClauseSyntax)node).CatchKeyword.Span;

                case SyntaxKind.CatchDeclaration:
                case SyntaxKind.CatchFilterClause:
                    return node.Span;

                case SyntaxKind.FinallyClause:
                    return ((FinallyClauseSyntax)node).FinallyKeyword.Span;

                case SyntaxKind.IfStatement:
                    var ifStatement = (IfStatementSyntax)node;
                    return TextSpan.FromBounds(ifStatement.IfKeyword.SpanStart, ifStatement.CloseParenToken.Span.End);

                case SyntaxKind.ElseClause:
                    return ((ElseClauseSyntax)node).ElseKeyword.Span;

                case SyntaxKind.SwitchStatement:
                    var switchStatement = (SwitchStatementSyntax)node;
                    return TextSpan.FromBounds(switchStatement.SwitchKeyword.SpanStart,
                        (switchStatement.CloseParenToken != default) ? switchStatement.CloseParenToken.Span.End : switchStatement.Expression.Span.End);

                case SyntaxKind.SwitchSection:
                    return ((SwitchSectionSyntax)node).Labels.Last().Span;

                case SyntaxKind.WhileStatement:
                    var whileStatement = (WhileStatementSyntax)node;
                    return TextSpan.FromBounds(whileStatement.WhileKeyword.SpanStart, whileStatement.CloseParenToken.Span.End);

                case SyntaxKind.DoStatement:
                    return ((DoStatementSyntax)node).DoKeyword.Span;

                case SyntaxKind.ForStatement:
                    var forStatement = (ForStatementSyntax)node;
                    return TextSpan.FromBounds(forStatement.ForKeyword.SpanStart, forStatement.CloseParenToken.Span.End);

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    var commonForEachStatement = (CommonForEachStatementSyntax)node;
                    return TextSpan.FromBounds(
                        (commonForEachStatement.AwaitKeyword.Span.Length > 0) ? commonForEachStatement.AwaitKeyword.SpanStart : commonForEachStatement.ForEachKeyword.SpanStart,
                        commonForEachStatement.CloseParenToken.Span.End);

                case SyntaxKind.LabeledStatement:
                    return ((LabeledStatementSyntax)node).Identifier.Span;

                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                    return ((CheckedStatementSyntax)node).Keyword.Span;

                case SyntaxKind.UnsafeStatement:
                    return ((UnsafeStatementSyntax)node).UnsafeKeyword.Span;

                case SyntaxKind.LocalFunctionStatement:
                    var lfd = (LocalFunctionStatementSyntax)node;
                    return lfd.Identifier.Span;

                case SyntaxKind.YieldBreakStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.EmptyStatement:
                case SyntaxKind.GotoStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoDefaultStatement:
                case SyntaxKind.BreakStatement:
                case SyntaxKind.ContinueStatement:
                    return node.Span;

                case SyntaxKind.LocalDeclarationStatement:
                    var localDeclarationStatement = (LocalDeclarationStatementSyntax)node;
                    return CombineSpans(localDeclarationStatement.AwaitKeyword.Span, localDeclarationStatement.UsingKeyword.Span, node.Span);

                case SyntaxKind.AwaitExpression:
                    return ((AwaitExpressionSyntax)node).AwaitKeyword.Span;

                case SyntaxKind.AnonymousObjectCreationExpression:
                    return ((AnonymousObjectCreationExpressionSyntax)node).NewKeyword.Span;

                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)node).ParameterList.Span;

                case SyntaxKind.SimpleLambdaExpression:
                    return ((SimpleLambdaExpressionSyntax)node).Parameter.Span;

                case SyntaxKind.AnonymousMethodExpression:
                    return ((AnonymousMethodExpressionSyntax)node).DelegateKeyword.Span;

                case SyntaxKind.QueryExpression:
                    return ((QueryExpressionSyntax)node).FromClause.FromKeyword.Span;

                case SyntaxKind.QueryBody:
                    var queryBody = (QueryBodySyntax)node;
                    return TryGetDiagnosticSpanImpl(queryBody.Clauses.FirstOrDefault() ?? queryBody.Parent!, editKind);

                case SyntaxKind.QueryContinuation:
                    return ((QueryContinuationSyntax)node).IntoKeyword.Span;

                case SyntaxKind.FromClause:
                    return ((FromClauseSyntax)node).FromKeyword.Span;

                case SyntaxKind.JoinClause:
                    return ((JoinClauseSyntax)node).JoinKeyword.Span;

                case SyntaxKind.JoinIntoClause:
                    return ((JoinIntoClauseSyntax)node).IntoKeyword.Span;

                case SyntaxKind.LetClause:
                    return ((LetClauseSyntax)node).LetKeyword.Span;

                case SyntaxKind.WhereClause:
                    return ((WhereClauseSyntax)node).WhereKeyword.Span;

                case SyntaxKind.OrderByClause:
                    return ((OrderByClauseSyntax)node).OrderByKeyword.Span;

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    return node.Span;

                case SyntaxKind.SelectClause:
                    return ((SelectClauseSyntax)node).SelectKeyword.Span;

                case SyntaxKind.GroupClause:
                    return ((GroupClauseSyntax)node).GroupKeyword.Span;

                case SyntaxKind.IsPatternExpression:
                case SyntaxKind.TupleType:
                case SyntaxKind.TupleExpression:
                case SyntaxKind.DeclarationExpression:
                case SyntaxKind.RefType:
                case SyntaxKind.RefExpression:
                case SyntaxKind.DeclarationPattern:
                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.WhenClause:
                case SyntaxKind.SingleVariableDesignation:
                case SyntaxKind.CasePatternSwitchLabel:
                    return node.Span;

                case SyntaxKind.SwitchExpression:
                    return ((SwitchExpressionSyntax)node).SwitchKeyword.Span;

                case SyntaxKind.SwitchExpressionArm:
                    return ((SwitchExpressionArmSyntax)node).EqualsGreaterThanToken.Span;

                default:
                    return null;
            }
        }

        private static TextSpan GetDiagnosticSpan(SyntaxTokenList modifiers, SyntaxNodeOrToken start, SyntaxNodeOrToken end)
            => TextSpan.FromBounds((modifiers.Count != 0) ? modifiers.First().SpanStart : start.SpanStart, end.Span.End);

        private static TextSpan CombineSpans(TextSpan first, TextSpan second, TextSpan defaultSpan)
           => (first.Length > 0 && second.Length > 0) ? TextSpan.FromBounds(first.Start, second.End) : (first.Length > 0) ? first : (second.Length > 0) ? second : defaultSpan;

        internal override TextSpan GetLambdaParameterDiagnosticSpan(SyntaxNode lambda, int ordinal)
        {
            Debug.Assert(ordinal >= 0);

            switch (lambda.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ((ParenthesizedLambdaExpressionSyntax)lambda).ParameterList.Parameters[ordinal].Identifier.Span;

                case SyntaxKind.SimpleLambdaExpression:
                    Debug.Assert(ordinal == 0);
                    return ((SimpleLambdaExpressionSyntax)lambda).Parameter.Identifier.Span;

                case SyntaxKind.AnonymousMethodExpression:
                    // since we are given a parameter ordinal there has to be a parameter list:
                    return ((AnonymousMethodExpressionSyntax)lambda).ParameterList!.Parameters[ordinal].Identifier.Span;

                default:
                    return lambda.Span;
            }
        }

        protected override string? TryGetDisplayName(SyntaxNode node, EditKind editKind)
            => TryGetDisplayNameImpl(node, editKind);

        internal static new string? GetDisplayName(SyntaxNode node, EditKind editKind)
            => TryGetDisplayNameImpl(node, editKind) ?? throw ExceptionUtilities.UnexpectedValue(node.Kind());

        internal static string? TryGetDisplayNameImpl(SyntaxNode node, EditKind editKind)
        {
            switch (node.Kind())
            {
                // top-level

                case SyntaxKind.GlobalStatement:
                    return CSharpFeaturesResources.global_statement;

                case SyntaxKind.ExternAliasDirective:
                    return CSharpFeaturesResources.extern_alias;

                case SyntaxKind.UsingDirective:
                    // Dev12 distinguishes using alias from using namespace and reports different errors for removing alias.
                    // None of these changes are allowed anyways, so let's keep it simple.
                    return CSharpFeaturesResources.using_directive;

                case SyntaxKind.NamespaceDeclaration:
                    return FeaturesResources.namespace_;

                case SyntaxKind.ClassDeclaration:
                    return FeaturesResources.class_;

                case SyntaxKind.StructDeclaration:
                    return CSharpFeaturesResources.struct_;

                case SyntaxKind.InterfaceDeclaration:
                    return FeaturesResources.interface_;

                case SyntaxKind.RecordDeclaration:
                    return CSharpFeaturesResources.record_;

                case SyntaxKind.RecordStructDeclaration:
                    return CSharpFeaturesResources.record_struct;

                case SyntaxKind.EnumDeclaration:
                    return FeaturesResources.enum_;

                case SyntaxKind.DelegateDeclaration:
                    return FeaturesResources.delegate_;

                case SyntaxKind.FieldDeclaration:
                    var declaration = (FieldDeclarationSyntax)node;
                    return declaration.Modifiers.Any(SyntaxKind.ConstKeyword) ? FeaturesResources.const_field : FeaturesResources.field;

                case SyntaxKind.EventFieldDeclaration:
                    return CSharpFeaturesResources.event_field;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    return TryGetDisplayNameImpl(node.Parent!, editKind);

                case SyntaxKind.MethodDeclaration:
                    return FeaturesResources.method;

                case SyntaxKind.ConversionOperatorDeclaration:
                    return CSharpFeaturesResources.conversion_operator;

                case SyntaxKind.OperatorDeclaration:
                    return FeaturesResources.operator_;

                case SyntaxKind.ConstructorDeclaration:
                    var ctor = (ConstructorDeclarationSyntax)node;
                    return ctor.Modifiers.Any(SyntaxKind.StaticKeyword) ? FeaturesResources.static_constructor : FeaturesResources.constructor;

                case SyntaxKind.DestructorDeclaration:
                    return CSharpFeaturesResources.destructor;

                case SyntaxKind.PropertyDeclaration:
                    return SyntaxUtilities.HasBackingField((PropertyDeclarationSyntax)node) ? FeaturesResources.auto_property : FeaturesResources.property_;

                case SyntaxKind.IndexerDeclaration:
                    return CSharpFeaturesResources.indexer;

                case SyntaxKind.EventDeclaration:
                    return FeaturesResources.event_;

                case SyntaxKind.EnumMemberDeclaration:
                    return FeaturesResources.enum_value;

                case SyntaxKind.GetAccessorDeclaration:
                    if (node.Parent!.Parent!.IsKind(SyntaxKind.PropertyDeclaration))
                    {
                        return CSharpFeaturesResources.property_getter;
                    }
                    else
                    {
                        RoslynDebug.Assert(node.Parent.Parent.IsKind(SyntaxKind.IndexerDeclaration));
                        return CSharpFeaturesResources.indexer_getter;
                    }

                case SyntaxKind.InitAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    if (node.Parent!.Parent!.IsKind(SyntaxKind.PropertyDeclaration))
                    {
                        return CSharpFeaturesResources.property_setter;
                    }
                    else
                    {
                        RoslynDebug.Assert(node.Parent.Parent.IsKind(SyntaxKind.IndexerDeclaration));
                        return CSharpFeaturesResources.indexer_setter;
                    }

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return FeaturesResources.event_accessor;

                case SyntaxKind.ArrowExpressionClause:
                    return node.Parent!.Kind() switch
                    {
                        SyntaxKind.PropertyDeclaration => CSharpFeaturesResources.property_getter,
                        SyntaxKind.IndexerDeclaration => CSharpFeaturesResources.indexer_getter,
                        _ => null
                    };

                case SyntaxKind.TypeParameterConstraintClause:
                    return FeaturesResources.type_constraint;

                case SyntaxKind.TypeParameterList:
                case SyntaxKind.TypeParameter:
                    return FeaturesResources.type_parameter;

                case SyntaxKind.Parameter:
                    return FeaturesResources.parameter;

                case SyntaxKind.AttributeList:
                    return FeaturesResources.attribute;

                case SyntaxKind.Attribute:
                    return FeaturesResources.attribute;

                case SyntaxKind.AttributeTargetSpecifier:
                    return CSharpFeaturesResources.attribute_target;

                // statement:

                case SyntaxKind.TryStatement:
                    return CSharpFeaturesResources.try_block;

                case SyntaxKind.CatchClause:
                case SyntaxKind.CatchDeclaration:
                    return CSharpFeaturesResources.catch_clause;

                case SyntaxKind.CatchFilterClause:
                    return CSharpFeaturesResources.filter_clause;

                case SyntaxKind.FinallyClause:
                    return CSharpFeaturesResources.finally_clause;

                case SyntaxKind.FixedStatement:
                    return CSharpFeaturesResources.fixed_statement;

                case SyntaxKind.UsingStatement:
                    return CSharpFeaturesResources.using_statement;

                case SyntaxKind.LockStatement:
                    return CSharpFeaturesResources.lock_statement;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    return CSharpFeaturesResources.foreach_statement;

                case SyntaxKind.CheckedStatement:
                    return CSharpFeaturesResources.checked_statement;

                case SyntaxKind.UncheckedStatement:
                    return CSharpFeaturesResources.unchecked_statement;

                case SyntaxKind.YieldBreakStatement:
                    return CSharpFeaturesResources.yield_break_statement;

                case SyntaxKind.YieldReturnStatement:
                    return CSharpFeaturesResources.yield_return_statement;

                case SyntaxKind.AwaitExpression:
                    return CSharpFeaturesResources.await_expression;

                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                    return CSharpFeaturesResources.lambda;

                case SyntaxKind.AnonymousMethodExpression:
                    return CSharpFeaturesResources.anonymous_method;

                case SyntaxKind.FromClause:
                    return CSharpFeaturesResources.from_clause;

                case SyntaxKind.JoinClause:
                case SyntaxKind.JoinIntoClause:
                    return CSharpFeaturesResources.join_clause;

                case SyntaxKind.LetClause:
                    return CSharpFeaturesResources.let_clause;

                case SyntaxKind.WhereClause:
                    return CSharpFeaturesResources.where_clause;

                case SyntaxKind.OrderByClause:
                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    return CSharpFeaturesResources.orderby_clause;

                case SyntaxKind.SelectClause:
                    return CSharpFeaturesResources.select_clause;

                case SyntaxKind.GroupClause:
                    return CSharpFeaturesResources.groupby_clause;

                case SyntaxKind.QueryBody:
                    return CSharpFeaturesResources.query_body;

                case SyntaxKind.QueryContinuation:
                    return CSharpFeaturesResources.into_clause;

                case SyntaxKind.IsPatternExpression:
                    return CSharpFeaturesResources.is_pattern;

                case SyntaxKind.SimpleAssignmentExpression:
                    if (((AssignmentExpressionSyntax)node).IsDeconstruction())
                    {
                        return CSharpFeaturesResources.deconstruction;
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(node.Kind());
                    }

                case SyntaxKind.TupleType:
                case SyntaxKind.TupleExpression:
                    return CSharpFeaturesResources.tuple;

                case SyntaxKind.LocalFunctionStatement:
                    return CSharpFeaturesResources.local_function;

                case SyntaxKind.DeclarationExpression:
                    return CSharpFeaturesResources.out_var;

                case SyntaxKind.RefType:
                case SyntaxKind.RefExpression:
                    return CSharpFeaturesResources.ref_local_or_expression;

                case SyntaxKind.SwitchStatement:
                    return CSharpFeaturesResources.switch_statement;

                case SyntaxKind.LocalDeclarationStatement:
                    if (((LocalDeclarationStatementSyntax)node).UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                    {
                        return CSharpFeaturesResources.using_declaration;
                    }

                    return CSharpFeaturesResources.local_variable_declaration;

                default:
                    return null;
            }
        }

        protected override string GetSuspensionPointDisplayName(SyntaxNode node, EditKind editKind)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ForEachStatement:
                    Debug.Assert(((CommonForEachStatementSyntax)node).AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword));
                    return CSharpFeaturesResources.asynchronous_foreach_statement;

                case SyntaxKind.VariableDeclarator:
                    RoslynDebug.Assert(((LocalDeclarationStatementSyntax)node.Parent!.Parent!).AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword));
                    return CSharpFeaturesResources.asynchronous_using_declaration;

                default:
                    return base.GetSuspensionPointDisplayName(node, editKind);
            }
        }

        #endregion

        #region Top-Level Syntactic Rude Edits

        private readonly struct EditClassifier
        {
            private readonly CSharpEditAndContinueAnalyzer _analyzer;
            private readonly ArrayBuilder<RudeEditDiagnostic> _diagnostics;
            private readonly Match<SyntaxNode>? _match;
            private readonly SyntaxNode? _oldNode;
            private readonly SyntaxNode? _newNode;
            private readonly EditKind _kind;
            private readonly TextSpan? _span;
            private readonly bool _classifyStatementSyntax;

            public EditClassifier(
                CSharpEditAndContinueAnalyzer analyzer,
                ArrayBuilder<RudeEditDiagnostic> diagnostics,
                SyntaxNode? oldNode,
                SyntaxNode? newNode,
                EditKind kind,
                Match<SyntaxNode>? match = null,
                TextSpan? span = null,
                bool classifyStatementSyntax = false)
            {
                RoslynDebug.Assert(oldNode != null || newNode != null);

                // if the node is deleted we have map that can be used to closest new ancestor
                RoslynDebug.Assert(newNode != null || match != null);

                _analyzer = analyzer;
                _diagnostics = diagnostics;
                _oldNode = oldNode;
                _newNode = newNode;
                _kind = kind;
                _span = span;
                _match = match;
                _classifyStatementSyntax = classifyStatementSyntax;
            }

            private void ReportError(RudeEditKind kind, SyntaxNode? spanNode = null, SyntaxNode? displayNode = null)
            {
                var span = (spanNode != null) ? GetDiagnosticSpan(spanNode, _kind) : GetSpan();
                var node = displayNode ?? _newNode ?? _oldNode;
                var displayName = GetDisplayName(node!, _kind);

                _diagnostics.Add(new RudeEditDiagnostic(kind, span, node, arguments: new[] { displayName }));
            }

            private TextSpan GetSpan()
            {
                if (_span.HasValue)
                {
                    return _span.Value;
                }

                if (_newNode == null)
                {
                    return _analyzer.GetDeletedNodeDiagnosticSpan(_match!.Matches, _oldNode!);
                }

                return GetDiagnosticSpan(_newNode, _kind);
            }

            public void ClassifyEdit()
            {
                switch (_kind)
                {
                    case EditKind.Delete:
                        ClassifyDelete(_oldNode!);
                        return;

                    case EditKind.Update:
                        ClassifyUpdate(_oldNode!, _newNode!);
                        return;

                    case EditKind.Move:
                        ClassifyMove(_newNode!);
                        return;

                    case EditKind.Insert:
                        ClassifyInsert(_newNode!);
                        return;

                    case EditKind.Reorder:
                        ClassifyReorder(_newNode!);
                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_kind);
                }
            }

            #region Move and Reorder

            private void ClassifyMove(SyntaxNode newNode)
            {
                if (newNode.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    return;
                }

                // We could perhaps allow moving a type declaration to a different namespace syntax node
                // as long as it represents semantically the same namespace as the one of the original type declaration.
                ReportError(RudeEditKind.Move);
            }

            private void ClassifyReorder(SyntaxNode newNode)
            {
                if (_newNode.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    return;
                }

                switch (newNode.Kind())
                {
                    case SyntaxKind.GlobalStatement:
                        // TODO:
                        ReportError(RudeEditKind.Move);
                        return;

                    case SyntaxKind.ExternAliasDirective:
                    case SyntaxKind.UsingDirective:
                    case SyntaxKind.NamespaceDeclaration:
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.VariableDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.TypeParameterConstraintClause:
                    case SyntaxKind.AttributeList:
                    case SyntaxKind.Attribute:
                        // We'll ignore these edits. A general policy is to ignore edits that are only discoverable via reflection.
                        return;

                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.VariableDeclarator:
                        // Maybe we could allow changing order of field declarations unless the containing type layout is sequential.
                        ReportError(RudeEditKind.Move);
                        return;

                    case SyntaxKind.EnumMemberDeclaration:
                        // To allow this change we would need to check that values of all fields of the enum 
                        // are preserved, or make sure we can update all method bodies that accessed those that changed.
                        ReportError(RudeEditKind.Move);
                        return;

                    case SyntaxKind.TypeParameter:
                    case SyntaxKind.Parameter:
                        ReportError(RudeEditKind.Move);
                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(newNode.Kind());
                }
            }

            #endregion

            #region Insert

            private void ClassifyInsert(SyntaxNode node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.GlobalStatement:
                        // TODO:
                        ReportError(RudeEditKind.Insert);
                        return;

                    case SyntaxKind.ExternAliasDirective:
                    case SyntaxKind.NamespaceDeclaration:
                        ReportError(RudeEditKind.Insert);
                        return;

                    case SyntaxKind.UsingDirective:
                        // We don't report rude edits for using directives though we also don't do any
                        // special processing, so inserting a using directive that changes semantics in
                        // unedited code will not issue edits for that code. Inserting a using directive
                        // and then consunming it will result in edits for the changes code as per usual.
                        return;

                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.AccessorList:
                    case SyntaxKind.VariableDeclarator:
                    case SyntaxKind.VariableDeclaration:
                        return;

                    case SyntaxKind.ArrowExpressionClause:
                        if (node.Parent.IsKind(SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration))
                        {
                            return;
                        }

                        break;

                    case SyntaxKind.Parameter when !_classifyStatementSyntax:
                        // Parameter inserts are allowed for local functions
                        if (node.Parent?.Parent is RecordDeclarationSyntax)
                        {
                            ReportError(RudeEditKind.AddRecordPositionalParameter);
                        }
                        else
                        {
                            ReportError(RudeEditKind.Insert);
                        }
                        return;

                    case SyntaxKind.EnumMemberDeclaration:
                    case SyntaxKind.TypeParameter:
                    case SyntaxKind.TypeParameterConstraintClause:
                    case SyntaxKind.TypeParameterList:
                    case SyntaxKind.Attribute:
                    case SyntaxKind.AttributeList:
                        ReportError(RudeEditKind.Insert);
                        return;
                }

                // When classifying statement syntax we could see potentially any node as an edit
                if (!_classifyStatementSyntax)
                {
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
                }
            }

            #endregion

            #region Delete

            private void ClassifyDelete(SyntaxNode oldNode)
            {
                switch (oldNode.Kind())
                {
                    case SyntaxKind.GlobalStatement:
                        // TODO:
                        ReportError(RudeEditKind.Delete);
                        return;

                    case SyntaxKind.ExternAliasDirective:
                    case SyntaxKind.NamespaceDeclaration:
                        // To allow removal of declarations we would need to update method bodies that 
                        // were previously binding to them but now are binding to another symbol that was previously hidden.
                        ReportError(RudeEditKind.Delete);
                        return;

                    case SyntaxKind.UsingDirective:
                        // We don't report rude edits for using directives though we also don't do any
                        // special processing, so deleting a using directive that changes semantics in
                        // unedited code will not issue edits for that code. Deleting code and then doing
                        // something like Remove Unused Usings will report edits for the changes code as
                        // normal.
                        return;

                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.VariableDeclarator:
                    case SyntaxKind.VariableDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                        // We do not report member delete here since the member might be moving to a different part of a partial type declaration.
                        // If that is not the case the semantic analysis reports the rude edit.
                        return;

                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.EnumMemberDeclaration:
                    case SyntaxKind.AccessorList:
                        // We do not report error here since it will be reported in semantic analysis.
                        return;

                    case SyntaxKind.ArrowExpressionClause:
                        // We do not report error here since it will be reported in semantic analysis.
                        if (oldNode.Parent.IsKind(SyntaxKind.PropertyDeclaration, SyntaxKind.IndexerDeclaration))
                        {
                            return;
                        }

                        break;

                    case SyntaxKind.AttributeList:
                    case SyntaxKind.Attribute:
                        // To allow removal of attributes we would need to check if the removed attribute
                        // is a pseudo-custom attribute that CLR allows us to change, or if it is a compiler well-know attribute
                        // that affects the generated IL.
                        ReportError(RudeEditKind.Delete);
                        return;

                    case SyntaxKind.TypeParameter:
                    case SyntaxKind.TypeParameterList:
                    case SyntaxKind.TypeParameterConstraintClause:
                        ReportError(RudeEditKind.Delete);
                        return;

                    case SyntaxKind.Parameter when !_classifyStatementSyntax:
                    case SyntaxKind.ParameterList when !_classifyStatementSyntax:
                        if (oldNode.Parent?.Parent is RecordDeclarationSyntax)
                        {
                            ReportError(RudeEditKind.DeleteRecordPositionalParameter);
                        }
                        else
                        {
                            ReportError(RudeEditKind.Delete);
                        }
                        return;
                }

                // When classifying statement syntax we could see potentially any node as an edit
                if (!_classifyStatementSyntax)
                {
                    throw ExceptionUtilities.UnexpectedValue(oldNode.Kind());
                }
            }

            #endregion

            #region Update

            private void ClassifyUpdate(SyntaxNode oldNode, SyntaxNode newNode)
            {
                switch (newNode.Kind())
                {
                    case SyntaxKind.GlobalStatement:
                        ReportError(RudeEditKind.Update);
                        return;

                    case SyntaxKind.ExternAliasDirective:
                        ReportError(RudeEditKind.Update);
                        return;

                    case SyntaxKind.UsingDirective:
                        // We don't report rude edits for using directives though we also don't do any
                        // special processing, so updating a using directive that changes semantics in
                        // unedited code will not issue edits for that code.
                        return;

                    case SyntaxKind.NamespaceDeclaration:
                        ClassifyUpdate((NamespaceDeclarationSyntax)oldNode, (NamespaceDeclarationSyntax)newNode);
                        return;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        ClassifyUpdate((TypeDeclarationSyntax)oldNode, (TypeDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.EnumDeclaration:
                        ClassifyUpdate((EnumDeclarationSyntax)oldNode, (EnumDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.DelegateDeclaration:
                        ClassifyUpdate((DelegateDeclarationSyntax)oldNode, (DelegateDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.FieldDeclaration:
                        ClassifyUpdate((BaseFieldDeclarationSyntax)oldNode, (BaseFieldDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.EventFieldDeclaration:
                        ClassifyUpdate((BaseFieldDeclarationSyntax)oldNode, (BaseFieldDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.VariableDeclaration:
                        ClassifyUpdate((VariableDeclarationSyntax)oldNode, (VariableDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.VariableDeclarator:
                        ClassifyUpdate((VariableDeclaratorSyntax)oldNode, (VariableDeclaratorSyntax)newNode);
                        return;

                    case SyntaxKind.MethodDeclaration:
                        ClassifyUpdate((MethodDeclarationSyntax)oldNode, (MethodDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.ConversionOperatorDeclaration:
                        ClassifyUpdate((ConversionOperatorDeclarationSyntax)oldNode, (ConversionOperatorDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.OperatorDeclaration:
                        ClassifyUpdate((OperatorDeclarationSyntax)oldNode, (OperatorDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.ConstructorDeclaration:
                        ClassifyUpdate((ConstructorDeclarationSyntax)oldNode, (ConstructorDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.DestructorDeclaration:
                        ClassifyUpdate((DestructorDeclarationSyntax)oldNode, (DestructorDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.PropertyDeclaration:
                        ClassifyUpdate((PropertyDeclarationSyntax)oldNode, (PropertyDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.IndexerDeclaration:
                        ClassifyUpdate((IndexerDeclarationSyntax)oldNode, (IndexerDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.EventDeclaration:
                        return;

                    case SyntaxKind.EnumMemberDeclaration:
                        ClassifyUpdate((EnumMemberDeclarationSyntax)oldNode, (EnumMemberDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        ClassifyUpdate((AccessorDeclarationSyntax)oldNode, (AccessorDeclarationSyntax)newNode);
                        return;

                    case SyntaxKind.TypeParameterConstraintClause:
                        ClassifyUpdate((TypeParameterConstraintClauseSyntax)oldNode, (TypeParameterConstraintClauseSyntax)newNode);
                        return;

                    case SyntaxKind.TypeParameter:
                        ClassifyUpdate((TypeParameterSyntax)oldNode, (TypeParameterSyntax)newNode);
                        return;

                    case SyntaxKind.Parameter when !_classifyStatementSyntax:
                        // Parameter updates are allowed for local functions
                        ClassifyUpdate((ParameterSyntax)oldNode, (ParameterSyntax)newNode);
                        return;

                    case SyntaxKind.AttributeList:
                        ClassifyUpdate((AttributeListSyntax)oldNode, (AttributeListSyntax)newNode);
                        return;

                    case SyntaxKind.Attribute:
                        // Dev12 reports "Rename" if the attribute type name is changed. 
                        // But such update is actually not renaming the attribute, it's changing what attribute is applied.
                        ReportError(RudeEditKind.Update);
                        return;

                    case SyntaxKind.TypeParameterList:
                    case SyntaxKind.ParameterList:
                    case SyntaxKind.BracketedParameterList:
                    case SyntaxKind.AccessorList:
                        return;

                }

                // When classifying statement syntax we could see potentially any node as an edit
                if (!_classifyStatementSyntax)
                {
                    throw ExceptionUtilities.UnexpectedValue(newNode.Kind());
                }
            }

            private void ClassifyUpdate(NamespaceDeclarationSyntax oldNode, NamespaceDeclarationSyntax newNode)
            {
                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.Name, newNode.Name));
                ReportError(RudeEditKind.Renamed);
            }

            private void ClassifyUpdate(TypeDeclarationSyntax oldNode, TypeDeclarationSyntax newNode)
            {
                if (oldNode.Kind() != newNode.Kind())
                {
                    ReportError(RudeEditKind.TypeKindUpdate);
                    return;
                }

                // Allow partial keyword to be added or removed.
                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.PartialKeyword, ignore2: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.BaseList, newNode.BaseList))
                {
                    ReportError(RudeEditKind.BaseTypeOrInterfaceUpdate);
                }
            }

            private void ClassifyUpdate(EnumDeclarationSyntax oldNode, EnumDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.BaseList, newNode.BaseList))
                {
                    ReportError(RudeEditKind.EnumUnderlyingTypeUpdate);
                    return;
                }

                // The list of members has been updated (separators added).
                // We report a Rude Edit for each updated member.
            }

            private void ClassifyUpdate(DelegateDeclarationSyntax oldNode, DelegateDeclarationSyntax newNode)
            {
                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ReturnType, newNode.ReturnType))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                }
            }

            private void ClassifyUpdate(BaseFieldDeclarationSyntax oldNode, BaseFieldDeclarationSyntax newNode)
            {
                if (oldNode.Kind() != newNode.Kind())
                {
                    ReportError(RudeEditKind.FieldKindUpdate);
                    return;
                }

                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }
            }

            private void ClassifyUpdate(VariableDeclarationSyntax oldNode, VariableDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                // separators may be added/removed:
            }

            private void ClassifyUpdate(VariableDeclaratorSyntax oldNode, VariableDeclaratorSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                // If the argument lists are mismatched the field must have mismatched "fixed" modifier, 
                // which is reported by the field declaration.
                if (oldNode.ArgumentList is null == newNode.ArgumentList is null)
                {
                    if (!SyntaxFactory.AreEquivalent(oldNode.ArgumentList, newNode.ArgumentList))
                    {
                        ReportError(RudeEditKind.FixedSizeFieldUpdate);
                        return;
                    }
                }

                var typeDeclaration = (TypeDeclarationSyntax?)oldNode.Parent!.Parent!.Parent!;
                if (typeDeclaration.Arity > 0)
                {
                    ReportError(RudeEditKind.GenericTypeInitializerUpdate);
                    return;
                }

                // Check if a constant field is updated:
                var fieldDeclaration = (BaseFieldDeclarationSyntax)oldNode.Parent.Parent;
                if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
                {
                    ReportError(RudeEditKind.Update);
                    return;
                }

                ClassifyDeclarationBodyRudeUpdates(newNode);
            }

            private void ClassifyUpdate(MethodDeclarationSyntax oldNode, MethodDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                // Ignore async keyword when matching modifiers. Async checks are done in ComputeBodyMatch.
                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.AsyncKeyword, ignore2: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ReturnType, newNode.ReturnType))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ExplicitInterfaceSpecifier, newNode.ExplicitInterfaceSpecifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: newNode,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(ConversionOperatorDeclarationSyntax oldNode, ConversionOperatorDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ImplicitOrExplicitKeyword, newNode.ImplicitOrExplicitKeyword))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(OperatorDeclarationSyntax oldNode, OperatorDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.OperatorToken, newNode.OperatorToken))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ReturnType, newNode.ReturnType))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(AccessorDeclarationSyntax oldNode, AccessorDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (oldNode.Kind() != newNode.Kind())
                {
                    ReportError(RudeEditKind.AccessorKindUpdate);
                    return;
                }

                RoslynDebug.Assert(newNode.Parent is AccessorListSyntax);
                RoslynDebug.Assert(newNode.Parent.Parent is BasePropertyDeclarationSyntax);

                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent.Parent.Parent);
            }

            private void ClassifyUpdate(EnumMemberDeclarationSyntax oldNode, EnumMemberDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.EqualsValue, newNode.EqualsValue));
                ReportError(RudeEditKind.InitializerUpdate);
            }

            private void ClassifyUpdate(ConstructorDeclarationSyntax oldNode, ConstructorDeclarationSyntax newNode)
            {
                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(DestructorDeclarationSyntax oldNode, DestructorDeclarationSyntax newNode)
            {
                ClassifyMethodBodyRudeUpdate(
                    (SyntaxNode?)oldNode.Body ?? oldNode.ExpressionBody?.Expression,
                    (SyntaxNode?)newNode.Body ?? newNode.ExpressionBody?.Expression,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(PropertyDeclarationSyntax oldNode, PropertyDeclarationSyntax newNode)
            {
                if (!AreModifiersEquivalent(oldNode.Modifiers, newNode.Modifiers, ignore: SyntaxKind.UnsafeKeyword))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ExplicitInterfaceSpecifier, newNode.ExplicitInterfaceSpecifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                var containingType = (TypeDeclarationSyntax)newNode.Parent!;

                // TODO: We currently don't support switching from auto-props to properties with accessors and vice versa.
                // If we do we should also allow it for expression bodies.

                if (!SyntaxFactory.AreEquivalent(oldNode.ExpressionBody, newNode.ExpressionBody))
                {
                    var oldBody = SyntaxUtilities.TryGetEffectiveGetterBody(oldNode.ExpressionBody, oldNode.AccessorList);
                    var newBody = SyntaxUtilities.TryGetEffectiveGetterBody(newNode.ExpressionBody, newNode.AccessorList);

                    ClassifyMethodBodyRudeUpdate(
                        oldBody,
                        newBody,
                        containingMethod: null,
                        containingType: containingType);

                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Initializer, newNode.Initializer))
                {
                    if (containingType.Arity > 0)
                    {
                        ReportError(RudeEditKind.GenericTypeInitializerUpdate);
                        return;
                    }

                    if (newNode.Initializer != null)
                    {
                        ClassifyDeclarationBodyRudeUpdates(newNode.Initializer);
                    }
                }
            }

            private void ClassifyUpdate(IndexerDeclarationSyntax oldNode, IndexerDeclarationSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.ExplicitInterfaceSpecifier, newNode.ExplicitInterfaceSpecifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.ExpressionBody, newNode.ExpressionBody));

                var oldBody = SyntaxUtilities.TryGetEffectiveGetterBody(oldNode.ExpressionBody, oldNode.AccessorList);
                var newBody = SyntaxUtilities.TryGetEffectiveGetterBody(newNode.ExpressionBody, newNode.AccessorList);

                ClassifyMethodBodyRudeUpdate(
                    oldBody,
                    newBody,
                    containingMethod: null,
                    containingType: (TypeDeclarationSyntax?)newNode.Parent);
            }

            private void ClassifyUpdate(TypeParameterSyntax oldNode, TypeParameterSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.VarianceKeyword, newNode.VarianceKeyword));
                ReportError(RudeEditKind.VarianceUpdate);
            }

            private void ClassifyUpdate(TypeParameterConstraintClauseSyntax oldNode, TypeParameterConstraintClauseSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Name, newNode.Name))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.Constraints, newNode.Constraints));
                ReportError(RudeEditKind.TypeUpdate);
            }

            private void ClassifyUpdate(ParameterSyntax oldNode, ParameterSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Identifier, newNode.Identifier))
                {
                    ReportError(RudeEditKind.Renamed);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Modifiers, newNode.Modifiers))
                {
                    ReportError(RudeEditKind.ModifiersUpdate);
                    return;
                }

                if (!SyntaxFactory.AreEquivalent(oldNode.Type, newNode.Type))
                {
                    ReportError(RudeEditKind.TypeUpdate);
                    return;
                }

                Debug.Assert(!SyntaxFactory.AreEquivalent(oldNode.Default, newNode.Default));
                ReportError(RudeEditKind.InitializerUpdate);
            }

            private void ClassifyUpdate(AttributeListSyntax oldNode, AttributeListSyntax newNode)
            {
                if (!SyntaxFactory.AreEquivalent(oldNode.Target, newNode.Target))
                {
                    var spanNode = ((SyntaxNode?)newNode.Target) ?? newNode;
                    ReportError(RudeEditKind.Update, spanNode: spanNode, displayNode: spanNode);
                    return;
                }

                // changes in attribute separators are not interesting:
            }

            private static bool AreModifiersEquivalent(SyntaxTokenList oldModifiers, SyntaxTokenList newModifiers, SyntaxKind ignore, SyntaxKind? ignore2 = null)
            {
                var oldIgnoredModifierIndex = oldModifiers.IndexOf(ignore);
                var newIgnoredModifierIndex = newModifiers.IndexOf(ignore);

                if (oldIgnoredModifierIndex >= 0)
                {
                    oldModifiers = oldModifiers.RemoveAt(oldIgnoredModifierIndex);
                }

                if (newIgnoredModifierIndex >= 0)
                {
                    newModifiers = newModifiers.RemoveAt(newIgnoredModifierIndex);
                }

                return ignore2 is null
                    ? SyntaxFactory.AreEquivalent(oldModifiers, newModifiers)
                    : AreModifiersEquivalent(oldModifiers, newModifiers, ignore2.Value);
            }

            private void ClassifyMethodBodyRudeUpdate(
                SyntaxNode? oldBody,
                SyntaxNode? newBody,
                MethodDeclarationSyntax? containingMethod,
                TypeDeclarationSyntax? containingType)
            {
                Debug.Assert(oldBody is BlockSyntax || oldBody is ExpressionSyntax || oldBody == null);
                Debug.Assert(newBody is BlockSyntax || newBody is ExpressionSyntax || newBody == null);

                if ((oldBody == null) != (newBody == null))
                {
                    if (oldBody == null)
                    {
                        ReportError(RudeEditKind.MethodBodyAdd);
                        return;
                    }
                    else
                    {
                        ReportError(RudeEditKind.MethodBodyDelete);
                        return;
                    }
                }

                ClassifyMemberBodyRudeUpdate(containingMethod, containingType, isTriviaUpdate: false);

                if (newBody != null)
                {
                    ClassifyDeclarationBodyRudeUpdates(newBody);
                }
            }

            public void ClassifyMemberBodyRudeUpdate(
                MethodDeclarationSyntax? containingMethod,
                TypeDeclarationSyntax? containingType,
                bool isTriviaUpdate)
            {
                if (SyntaxUtilities.Any(containingMethod?.TypeParameterList))
                {
                    ReportError(isTriviaUpdate ? RudeEditKind.GenericMethodTriviaUpdate : RudeEditKind.GenericMethodUpdate);
                    return;
                }

                if (SyntaxUtilities.Any(containingType?.TypeParameterList))
                {
                    ReportError(isTriviaUpdate ? RudeEditKind.GenericTypeTriviaUpdate : RudeEditKind.GenericTypeUpdate);
                    return;
                }
            }

            public void ClassifyDeclarationBodyRudeUpdates(SyntaxNode newDeclarationOrBody)
            {
                foreach (var node in newDeclarationOrBody.DescendantNodesAndSelf(LambdaUtilities.IsNotLambda))
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.StackAllocArrayCreationExpression:
                        case SyntaxKind.ImplicitStackAllocArrayCreationExpression:
                            ReportError(RudeEditKind.StackAllocUpdate, node, _newNode);
                            return;
                    }
                }
            }

            #endregion
        }

        internal override void ReportTopLevelSyntacticRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            Edit<SyntaxNode> edit,
            Dictionary<SyntaxNode, EditKind> editMap)
        {
            if (HasParentEdit(editMap, edit))
            {
                return;
            }

            var classifier = new EditClassifier(this, diagnostics, edit.OldNode, edit.NewNode, edit.Kind, match);
            classifier.ClassifyEdit();
        }

        internal override void ReportMemberUpdateRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode newMember, TextSpan? span)
        {
            var classifier = new EditClassifier(this, diagnostics, oldNode: null, newMember, EditKind.Update, span: span);

            classifier.ClassifyMemberBodyRudeUpdate(
                newMember as MethodDeclarationSyntax,
                newMember.FirstAncestorOrSelf<TypeDeclarationSyntax>(),
                isTriviaUpdate: true);

            classifier.ClassifyDeclarationBodyRudeUpdates(newMember);
        }

        #endregion

        #region Semantic Rude Edits

        internal override void ReportInsertedMemberSymbolRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, ISymbol newSymbol, SyntaxNode newNode, bool insertingIntoExistingContainingType)
        {
            var rudeEditKind = newSymbol switch
            {
                // Inserting extern member into a new or existing type is not allowed.
                { IsExtern: true }
                    => RudeEditKind.InsertExtern,

                // All rude edits below only apply when inserting into an existing type (not when the type itself is inserted):
                _ when !insertingIntoExistingContainingType => RudeEditKind.None,

                // Inserting a member into an existing generic type is not allowed.
                { ContainingType: { Arity: > 0 } } and not INamedTypeSymbol
                    => RudeEditKind.InsertIntoGenericType,

                // Inserting virtual or interface member into an existing type is not allowed.
                { IsVirtual: true } or { IsOverride: true } or { IsAbstract: true } and not INamedTypeSymbol
                    => RudeEditKind.InsertVirtual,

                // Inserting generic method into an existing type is not allowed.
                IMethodSymbol { Arity: > 0 }
                    => RudeEditKind.InsertGenericMethod,

                // Inserting destructor to an existing type is not allowed.
                IMethodSymbol { MethodKind: MethodKind.Destructor }
                    => RudeEditKind.Insert,

                // Inserting operator to an existing type is not allowed.
                IMethodSymbol { MethodKind: MethodKind.Conversion or MethodKind.UserDefinedOperator }
                    => RudeEditKind.InsertOperator,

                // Inserting a method that explictly implements an interface method into an existing type is not allowed.
                IMethodSymbol { ExplicitInterfaceImplementations: { IsEmpty: false } }
                    => RudeEditKind.InsertMethodWithExplicitInterfaceSpecifier,

                // TODO: Inserting non-virtual member to an interface (https://github.com/dotnet/roslyn/issues/37128)
                { ContainingType: { TypeKind: TypeKind.Interface } } and not INamedTypeSymbol
                    => RudeEditKind.InsertIntoInterface,

                _ => RudeEditKind.None
            };

            if (rudeEditKind != RudeEditKind.None)
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    rudeEditKind,
                    GetDiagnosticSpan(newNode, EditKind.Insert),
                    newNode,
                    arguments: new[] { GetDisplayName(newNode, EditKind.Insert) }));
            }
        }

        internal override void ReportTypeDeclarationInsertDeleteRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, INamedTypeSymbol oldType, INamedTypeSymbol newType, SyntaxNode newDeclaration, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<SyntaxNode>.GetInstance(out var oldNodes);
            using var _2 = ArrayBuilder<SyntaxNode>.GetInstance(out var newNodes);

            // Consider: better error messages
            Report((b, t) => AddNodes(b, t.AttributeLists), RudeEditKind.Update);
            Report((b, t) => AddNodes(b, t.TypeParameterList?.Parameters), RudeEditKind.Update);
            Report((b, t) => AddNodes(b, t.ConstraintClauses), RudeEditKind.Update);
            Report((b, t) => AddNodes(b, t.BaseList?.Types), RudeEditKind.BaseTypeOrInterfaceUpdate);

            void Report(Action<ArrayBuilder<SyntaxNode>, TypeDeclarationSyntax> addNodes, RudeEditKind rudeEditKind)
            {
                foreach (var syntaxRef in oldType.DeclaringSyntaxReferences)
                {
                    addNodes(oldNodes, (TypeDeclarationSyntax)syntaxRef.GetSyntax(cancellationToken));
                }

                foreach (var syntaxRef in newType.DeclaringSyntaxReferences)
                {
                    addNodes(newNodes, (TypeDeclarationSyntax)syntaxRef.GetSyntax(cancellationToken));
                }

                if (oldNodes.Count != newNodes.Count ||
                    oldNodes.Zip(newNodes, (oldNode, newNode) => SyntaxFactory.AreEquivalent(oldNode, newNode)).Any(isEquivalent => !isEquivalent))
                {
                    diagnostics.Add(new RudeEditDiagnostic(
                        rudeEditKind,
                        GetDiagnosticSpan(newDeclaration, EditKind.Update),
                        newDeclaration,
                        arguments: new[] { GetDisplayName(newDeclaration, EditKind.Update) }));
                }

                oldNodes.Clear();
                newNodes.Clear();
            }
        }

        #endregion

        #region Exception Handling Rude Edits

        /// <summary>
        /// Return nodes that represent exception handlers encompassing the given active statement node.
        /// </summary>
        protected override List<SyntaxNode> GetExceptionHandlingAncestors(SyntaxNode node, bool isNonLeaf)
        {
            var result = new List<SyntaxNode>();

            var current = node;
            while (current != null)
            {
                var kind = current.Kind();

                switch (kind)
                {
                    case SyntaxKind.TryStatement:
                        if (isNonLeaf)
                        {
                            result.Add(current);
                        }

                        break;

                    case SyntaxKind.CatchClause:
                    case SyntaxKind.FinallyClause:
                        result.Add(current);

                        // skip try:
                        RoslynDebug.Assert(current.Parent is object);
                        RoslynDebug.Assert(current.Parent.Kind() == SyntaxKind.TryStatement);
                        current = current.Parent;

                        break;

                    // stop at type declaration:
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        return result;
                }

                // stop at lambda:
                if (LambdaUtilities.IsLambda(current))
                {
                    return result;
                }

                current = current.Parent;
            }

            return result;
        }

        internal override void ReportEnclosingExceptionHandlingRudeEdits(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            IEnumerable<Edit<SyntaxNode>> exceptionHandlingEdits,
            SyntaxNode oldStatement,
            TextSpan newStatementSpan)
        {
            foreach (var edit in exceptionHandlingEdits)
            {
                // try/catch/finally have distinct labels so only the nodes of the same kind may match:
                Debug.Assert(edit.Kind != EditKind.Update || edit.OldNode.RawKind == edit.NewNode.RawKind);

                if (edit.Kind != EditKind.Update || !AreExceptionClausesEquivalent(edit.OldNode, edit.NewNode))
                {
                    AddAroundActiveStatementRudeDiagnostic(diagnostics, edit.OldNode, edit.NewNode, newStatementSpan);
                }
            }
        }

        private static bool AreExceptionClausesEquivalent(SyntaxNode oldNode, SyntaxNode newNode)
        {
            switch (oldNode.Kind())
            {
                case SyntaxKind.TryStatement:
                    var oldTryStatement = (TryStatementSyntax)oldNode;
                    var newTryStatement = (TryStatementSyntax)newNode;
                    return SyntaxFactory.AreEquivalent(oldTryStatement.Finally, newTryStatement.Finally)
                        && SyntaxFactory.AreEquivalent(oldTryStatement.Catches, newTryStatement.Catches);

                case SyntaxKind.CatchClause:
                case SyntaxKind.FinallyClause:
                    return SyntaxFactory.AreEquivalent(oldNode, newNode);

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldNode.Kind());
            }
        }

        /// <summary>
        /// An active statement (leaf or not) inside a "catch" makes the catch block read-only.
        /// An active statement (leaf or not) inside a "finally" makes the whole try/catch/finally block read-only.
        /// An active statement (non leaf)    inside a "try" makes the catch/finally block read-only.
        /// </summary>
        /// <remarks>
        /// Exception handling regions are only needed to be tracked if they contain user code.
        /// <see cref="UsingStatementSyntax"/> and using <see cref="LocalDeclarationStatementSyntax"/> generate finally blocks,
        /// but they do not contain non-hidden sequence points.
        /// </remarks>
        /// <param name="node">An exception handling ancestor of an active statement node.</param>
        /// <param name="coversAllChildren">
        /// True if all child nodes of the <paramref name="node"/> are contained in the exception region represented by the <paramref name="node"/>.
        /// </param>
        protected override TextSpan GetExceptionHandlingRegion(SyntaxNode node, out bool coversAllChildren)
        {
            TryStatementSyntax tryStatement;
            switch (node.Kind())
            {
                case SyntaxKind.TryStatement:
                    tryStatement = (TryStatementSyntax)node;
                    coversAllChildren = false;

                    if (tryStatement.Catches.Count == 0)
                    {
                        RoslynDebug.Assert(tryStatement.Finally != null);
                        return tryStatement.Finally.Span;
                    }

                    return TextSpan.FromBounds(
                        tryStatement.Catches.First().SpanStart,
                        (tryStatement.Finally != null) ?
                            tryStatement.Finally.Span.End :
                            tryStatement.Catches.Last().Span.End);

                case SyntaxKind.CatchClause:
                    coversAllChildren = true;
                    return node.Span;

                case SyntaxKind.FinallyClause:
                    coversAllChildren = true;
                    tryStatement = (TryStatementSyntax)node.Parent!;
                    return tryStatement.Span;

                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        #endregion

        #region State Machines

        internal override bool IsStateMachineMethod(SyntaxNode declaration)
            => SyntaxUtilities.IsAsyncDeclaration(declaration) || SyntaxUtilities.GetSuspensionPoints(declaration).Any();

        protected override void GetStateMachineInfo(SyntaxNode body, out ImmutableArray<SyntaxNode> suspensionPoints, out StateMachineKinds kinds)
        {
            suspensionPoints = SyntaxUtilities.GetSuspensionPoints(body).ToImmutableArray();

            kinds = StateMachineKinds.None;

            if (suspensionPoints.Any(n => n.IsKind(SyntaxKind.YieldBreakStatement) || n.IsKind(SyntaxKind.YieldReturnStatement)))
            {
                kinds |= StateMachineKinds.Iterator;
            }

            if (SyntaxUtilities.IsAsyncDeclaration(body.Parent))
            {
                kinds |= StateMachineKinds.Async;
            }
        }

        internal override void ReportStateMachineSuspensionPointRudeEdits(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldNode, SyntaxNode newNode)
        {
            // TODO: changes around suspension points (foreach, lock, using, etc.)

            if (newNode.IsKind(SyntaxKind.AwaitExpression))
            {
                var oldContainingStatementPart = FindContainingStatementPart(oldNode);
                var newContainingStatementPart = FindContainingStatementPart(newNode);

                // If the old statement has spilled state and the new doesn't the edit is ok. We'll just not use the spilled state.
                if (!SyntaxFactory.AreEquivalent(oldContainingStatementPart, newContainingStatementPart) &&
                    !HasNoSpilledState(newNode, newContainingStatementPart))
                {
                    diagnostics.Add(new RudeEditDiagnostic(RudeEditKind.AwaitStatementUpdate, newContainingStatementPart.Span));
                }
            }
        }

        internal override void ReportStateMachineSuspensionPointDeletedRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode deletedSuspensionPoint)
        {
            // Handle deletion of await keyword from await foreach statement.
            if (deletedSuspensionPoint is CommonForEachStatementSyntax deletedForeachStatement &&
                match.Matches.TryGetValue(deletedSuspensionPoint, out var newForEachStatement) &&
                newForEachStatement is CommonForEachStatementSyntax &&
                deletedForeachStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.ChangingFromAsynchronousToSynchronous,
                    GetDiagnosticSpan(newForEachStatement, EditKind.Update),
                    newForEachStatement,
                    new[] { GetDisplayName(newForEachStatement, EditKind.Update) }));

                return;
            }

            // Handle deletion of await keyword from await using declaration.
            if (deletedSuspensionPoint.IsKind(SyntaxKind.VariableDeclarator) &&
                match.Matches.TryGetValue(deletedSuspensionPoint.Parent!.Parent!, out var newLocalDeclaration) &&
                !((LocalDeclarationStatementSyntax)newLocalDeclaration).AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                diagnostics.Add(new RudeEditDiagnostic(
                        RudeEditKind.ChangingFromAsynchronousToSynchronous,
                        GetDiagnosticSpan(newLocalDeclaration, EditKind.Update),
                        newLocalDeclaration,
                        new[] { GetDisplayName(newLocalDeclaration, EditKind.Update) }));

                return;
            }

            base.ReportStateMachineSuspensionPointDeletedRudeEdit(diagnostics, match, deletedSuspensionPoint);
        }

        internal override void ReportStateMachineSuspensionPointInsertedRudeEdit(ArrayBuilder<RudeEditDiagnostic> diagnostics, Match<SyntaxNode> match, SyntaxNode insertedSuspensionPoint, bool aroundActiveStatement)
        {
            // Handle addition of await keyword to foreach statement.
            if (insertedSuspensionPoint is CommonForEachStatementSyntax insertedForEachStatement &&
                match.ReverseMatches.TryGetValue(insertedSuspensionPoint, out var oldNode) &&
                oldNode is CommonForEachStatementSyntax oldForEachStatement &&
                !oldForEachStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.Insert,
                    insertedForEachStatement.AwaitKeyword.Span,
                    insertedForEachStatement,
                    new[] { insertedForEachStatement.AwaitKeyword.ToString() }));

                return;
            }

            // Handle addition of using keyword to using declaration.
            if (insertedSuspensionPoint.IsKind(SyntaxKind.VariableDeclarator) &&
                match.ReverseMatches.TryGetValue(insertedSuspensionPoint.Parent!.Parent!, out var oldLocalDeclaration) &&
                !((LocalDeclarationStatementSyntax)oldLocalDeclaration).AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
            {
                var newLocalDeclaration = (LocalDeclarationStatementSyntax)insertedSuspensionPoint!.Parent!.Parent!;

                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.Insert,
                    newLocalDeclaration.AwaitKeyword.Span,
                    newLocalDeclaration,
                    new[] { newLocalDeclaration.AwaitKeyword.ToString() }));

                return;
            }

            base.ReportStateMachineSuspensionPointInsertedRudeEdit(diagnostics, match, insertedSuspensionPoint, aroundActiveStatement);
        }

        private static SyntaxNode FindContainingStatementPart(SyntaxNode node)
        {
            while (true)
            {
                if (node is StatementSyntax statement)
                {
                    return statement;
                }

                RoslynDebug.Assert(node is object);
                RoslynDebug.Assert(node.Parent is object);
                switch (node.Parent.Kind())
                {
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.SwitchStatement:
                    case SyntaxKind.LockStatement:
                    case SyntaxKind.UsingStatement:
                    case SyntaxKind.ArrowExpressionClause:
                        return node;
                }

                if (LambdaUtilities.IsLambdaBodyStatementOrExpression(node))
                {
                    return node;
                }

                node = node.Parent;
            }
        }

        private static bool HasNoSpilledState(SyntaxNode awaitExpression, SyntaxNode containingStatementPart)
        {
            Debug.Assert(awaitExpression.IsKind(SyntaxKind.AwaitExpression));

            // There is nothing within the statement part surrounding the await expression.
            if (containingStatementPart == awaitExpression)
            {
                return true;
            }

            switch (containingStatementPart.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.ReturnStatement:
                    var expression = GetExpressionFromStatementPart(containingStatementPart);

                    // await expr;
                    // return await expr;
                    if (expression == awaitExpression)
                    {
                        return true;
                    }

                    // identifier = await expr; 
                    // return identifier = await expr; 
                    return IsSimpleAwaitAssignment(expression, awaitExpression);

                case SyntaxKind.VariableDeclaration:
                    // var idf = await expr in using, for, etc.
                    // EqualsValueClause -> VariableDeclarator -> VariableDeclaration
                    return awaitExpression.Parent!.Parent!.Parent == containingStatementPart;

                case SyntaxKind.LocalDeclarationStatement:
                    // var idf = await expr;
                    // EqualsValueClause -> VariableDeclarator -> VariableDeclaration -> LocalDeclarationStatement
                    return awaitExpression.Parent!.Parent!.Parent!.Parent == containingStatementPart;
            }

            return IsSimpleAwaitAssignment(containingStatementPart, awaitExpression);
        }

        private static ExpressionSyntax GetExpressionFromStatementPart(SyntaxNode statement)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.ExpressionStatement:
                    return ((ExpressionStatementSyntax)statement).Expression;

                case SyntaxKind.ReturnStatement:
                    // Must have an expression since we are only inspecting at statements that contain an expression.
                    return ((ReturnStatementSyntax)statement).Expression!;

                default:
                    throw ExceptionUtilities.UnexpectedValue(statement.Kind());
            }
        }

        private static bool IsSimpleAwaitAssignment(SyntaxNode node, SyntaxNode awaitExpression)
        {
            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression, out AssignmentExpressionSyntax? assignment))
            {
                return assignment.Left.IsKind(SyntaxKind.IdentifierName) && assignment.Right == awaitExpression;
            }

            return false;
        }

        #endregion

        #region Rude Edits around Active Statement

        internal override void ReportOtherRudeEditsAroundActiveStatement(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement,
            bool isNonLeaf)
        {
            ReportRudeEditsForSwitchWhenClauses(diagnostics, oldActiveStatement, newActiveStatement);
            ReportRudeEditsForAncestorsDeclaringInterStatementTemps(diagnostics, match, oldActiveStatement, newActiveStatement);
            ReportRudeEditsForCheckedStatements(diagnostics, oldActiveStatement, newActiveStatement, isNonLeaf);
        }

        /// <summary>
        /// Reports rude edits when an active statement is a when clause in a switch statement and any of the switch cases or the switch value changed.
        /// This is necessary since the switch emits long-lived synthesized variables to store results of pattern evaluations.
        /// These synthesized variables are mapped to the slots of the new methods via ordinals. The mapping preserves the values of these variables as long as 
        /// exactly the same variables are emitted for the new switch as they were for the old one and their order didn't change either.
        /// This is guaranteed if none of the case clauses have changed.
        /// </summary>
        private void ReportRudeEditsForSwitchWhenClauses(ArrayBuilder<RudeEditDiagnostic> diagnostics, SyntaxNode oldActiveStatement, SyntaxNode newActiveStatement)
        {
            if (!oldActiveStatement.IsKind(SyntaxKind.WhenClause))
            {
                return;
            }

            // switch expression does not have sequence points (active statements):
            if (!(oldActiveStatement.Parent!.Parent!.Parent is SwitchStatementSyntax oldSwitch))
            {
                return;
            }

            // switch statement does not match switch expression, so it must be part of a switch statement as well.
            var newSwitch = (SwitchStatementSyntax)newActiveStatement.Parent!.Parent!.Parent!;

            // when clauses can only match other when clauses:
            Debug.Assert(newActiveStatement.IsKind(SyntaxKind.WhenClause));

            if (!AreEquivalentIgnoringLambdaBodies(oldSwitch.Expression, newSwitch.Expression))
            {
                AddRudeUpdateAroundActiveStatement(diagnostics, newSwitch);
            }

            if (!AreEquivalentSwitchStatementDecisionTrees(oldSwitch, newSwitch))
            {
                diagnostics.Add(new RudeEditDiagnostic(
                    RudeEditKind.UpdateAroundActiveStatement,
                    GetDiagnosticSpan(newSwitch, EditKind.Update),
                    newSwitch,
                    new[] { CSharpFeaturesResources.switch_statement_case_clause }));
            }
        }

        private static bool AreEquivalentSwitchStatementDecisionTrees(SwitchStatementSyntax oldSwitch, SwitchStatementSyntax newSwitch)
            => oldSwitch.Sections.SequenceEqual(newSwitch.Sections, AreSwitchSectionsEquivalent);

        private static bool AreSwitchSectionsEquivalent(SwitchSectionSyntax oldSection, SwitchSectionSyntax newSection)
            => oldSection.Labels.SequenceEqual(newSection.Labels, AreLabelsEquivalent);

        private static bool AreLabelsEquivalent(SwitchLabelSyntax oldLabel, SwitchLabelSyntax newLabel)
        {
            if (oldLabel.IsKind(SyntaxKind.CasePatternSwitchLabel, out CasePatternSwitchLabelSyntax? oldCasePatternLabel) &&
                newLabel.IsKind(SyntaxKind.CasePatternSwitchLabel, out CasePatternSwitchLabelSyntax? newCasePatternLabel))
            {
                // ignore the actual when expressions:
                return SyntaxFactory.AreEquivalent(oldCasePatternLabel.Pattern, newCasePatternLabel.Pattern) &&
                       (oldCasePatternLabel.WhenClause != null) == (newCasePatternLabel.WhenClause != null);
            }
            else
            {
                return SyntaxFactory.AreEquivalent(oldLabel, newLabel);
            }
        }

        private void ReportRudeEditsForCheckedStatements(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement,
            bool isNonLeaf)
        {
            // checked context can't be changed around non-leaf active statement:
            if (!isNonLeaf)
            {
                return;
            }

            // Changing checked context around an internal active statement may change the instructions
            // executed after method calls in the active statement but before the next sequence point.
            // Since the debugger remaps the IP at the first sequence point following a call instruction
            // allowing overflow context to be changed may lead to execution of code with old semantics.

            var oldCheckedStatement = TryGetCheckedStatementAncestor(oldActiveStatement);
            var newCheckedStatement = TryGetCheckedStatementAncestor(newActiveStatement);

            bool isRude;
            if (oldCheckedStatement == null || newCheckedStatement == null)
            {
                isRude = oldCheckedStatement != newCheckedStatement;
            }
            else
            {
                isRude = oldCheckedStatement.Kind() != newCheckedStatement.Kind();
            }

            if (isRude)
            {
                AddAroundActiveStatementRudeDiagnostic(diagnostics, oldCheckedStatement, newCheckedStatement, newActiveStatement.Span);
            }
        }

        private static CheckedStatementSyntax? TryGetCheckedStatementAncestor(SyntaxNode? node)
        {
            // Ignoring lambda boundaries since checked context flows through.

            while (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.CheckedStatement:
                    case SyntaxKind.UncheckedStatement:
                        return (CheckedStatementSyntax)node;
                }

                node = node.Parent;
            }

            return null;
        }

        private void ReportRudeEditsForAncestorsDeclaringInterStatementTemps(
            ArrayBuilder<RudeEditDiagnostic> diagnostics,
            Match<SyntaxNode> match,
            SyntaxNode oldActiveStatement,
            SyntaxNode newActiveStatement)
        {
            // Rude Edits for fixed/using/lock/foreach statements that are added/updated around an active statement.
            // Although such changes are technically possible, they might lead to confusion since 
            // the temporary variables these statements generate won't be properly initialized.
            //
            // We use a simple algorithm to match each new node with its old counterpart.
            // If all nodes match this algorithm is linear, otherwise it's quadratic.
            // 
            // Unlike exception regions matching where we use LCS, we allow reordering of the statements.

            ReportUnmatchedStatements<LockStatementSyntax>(diagnostics, match, n => n.IsKind(SyntaxKind.LockStatement), oldActiveStatement, newActiveStatement,
                areEquivalent: AreEquivalentActiveStatements,
                areSimilar: null);

            ReportUnmatchedStatements<FixedStatementSyntax>(diagnostics, match, n => n.IsKind(SyntaxKind.FixedStatement), oldActiveStatement, newActiveStatement,
                areEquivalent: AreEquivalentActiveStatements,
                areSimilar: (n1, n2) => DeclareSameIdentifiers(n1.Declaration.Variables, n2.Declaration.Variables));

            // Using statements with declaration do not introduce compiler generated temporary.
            ReportUnmatchedStatements<UsingStatementSyntax>(
                diagnostics,
                match,
                n => n.IsKind(SyntaxKind.UsingStatement, out UsingStatementSyntax? usingStatement) && usingStatement.Declaration is null,
                oldActiveStatement,
                newActiveStatement,
                areEquivalent: AreEquivalentActiveStatements,
                areSimilar: null);

            ReportUnmatchedStatements<CommonForEachStatementSyntax>(
                diagnostics,
                match,
                n => n.IsKind(SyntaxKind.ForEachStatement) || n.IsKind(SyntaxKind.ForEachVariableStatement),
                oldActiveStatement,
                newActiveStatement,
                areEquivalent: AreEquivalentActiveStatements,
                areSimilar: AreSimilarActiveStatements);
        }

        private static bool DeclareSameIdentifiers(SeparatedSyntaxList<VariableDeclaratorSyntax> oldVariables, SeparatedSyntaxList<VariableDeclaratorSyntax> newVariables)
            => DeclareSameIdentifiers(oldVariables.Select(v => v.Identifier).ToArray(), newVariables.Select(v => v.Identifier).ToArray());

        private static bool DeclareSameIdentifiers(SyntaxToken[] oldVariables, SyntaxToken[] newVariables)
        {
            if (oldVariables.Length != newVariables.Length)
            {
                return false;
            }

            for (var i = 0; i < oldVariables.Length; i++)
            {
                if (!SyntaxFactory.AreEquivalent(oldVariables[i], newVariables[i]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
