// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportLanguageService(typeof(IIntroduceVariableService), LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceVariableService :
        AbstractIntroduceVariableService<CSharpIntroduceVariableService, ExpressionSyntax, TypeSyntax, TypeDeclarationSyntax, QueryExpressionSyntax>
    {
        protected override bool IsInNonFirstQueryClause(ExpressionSyntax expression)
        {
            var query = expression.GetAncestor<QueryExpressionSyntax>();
            if (query != null)
            {
                // Can't introduce for the first clause in a query.
                var fromClause = expression.GetAncestor<FromClauseSyntax>();
                if (fromClause == null || query.FromClause != fromClause)
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool IsInFieldInitializer(ExpressionSyntax expression)
        {
            return expression.GetAncestorOrThis<VariableDeclaratorSyntax>()
                             .GetAncestorOrThis<FieldDeclarationSyntax>() != null;
        }

        protected override bool IsInParameterInitializer(ExpressionSyntax expression)
        {
            return expression.GetAncestorOrThis<EqualsValueClauseSyntax>().IsParentKind(SyntaxKind.Parameter);
        }

        protected override bool IsInConstructorInitializer(ExpressionSyntax expression)
        {
            return expression.GetAncestorOrThis<ConstructorInitializerSyntax>() != null;
        }

        protected override bool IsInAttributeArgumentInitializer(ExpressionSyntax expression)
        {
            // Don't call the base here.  We want to let the user extract a constant if they've
            // said "Foo(a = 10)"
            var attributeArgument = expression.GetAncestorOrThis<AttributeArgumentSyntax>();
            if (attributeArgument != null)
            {
                // Can't extract an attribute initializer if it contains an array initializer of any
                // sort.  Also, we can't extract if there's any typeof expression within it.
                if (!expression.DepthFirstTraversal().Any(n => n.RawKind == (int)SyntaxKind.ArrayCreationExpression) &&
                    !expression.DepthFirstTraversal().Any(n => n.RawKind == (int)SyntaxKind.TypeOfExpression))
                {
                    var attributeDecl = attributeArgument.GetAncestorOrThis<AttributeListSyntax>();

                    // Also can't extract an attribute initializer if the attribute is a global one.
                    if (!attributeDecl.IsParentKind(SyntaxKind.CompilationUnit))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool CanIntroduceVariableFor(ExpressionSyntax expression)
        {
            if (expression.WalkUpParentheses().IsParentKind(SyntaxKind.ExpressionStatement))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<SyntaxNode> GetContainingExecutableBlocks(ExpressionSyntax expression)
        {
            return expression.GetAncestorsOrThis<BlockSyntax>();
        }

        protected override IList<bool> GetInsertionIndices(TypeDeclarationSyntax destination, CancellationToken cancellationToken)
        {
            return destination.GetInsertionIndices(cancellationToken);
        }

        protected override bool CanReplace(ExpressionSyntax expression)
        {
            return true;
        }

        protected override TNode RewriteCore<TNode>(
            TNode node,
            SyntaxNode replacementNode,
            ISet<ExpressionSyntax> matches)
        {
            return (TNode)Rewriter.Visit(node, replacementNode, matches);
        }
    }
}
