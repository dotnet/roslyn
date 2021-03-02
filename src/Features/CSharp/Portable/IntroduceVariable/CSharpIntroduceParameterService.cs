// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterService : AbstractIntroduceParameterService<
        CSharpIntroduceParameterService,
        ExpressionSyntax,
        InvocationExpressionSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceParameterService()
        {
        }

        protected override SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(SeparatedSyntaxList<SyntaxNode> invocationArguments, SyntaxNode newArgumentExpression)
        {
            return invocationArguments.Add(SyntaxFactory.Argument((ExpressionSyntax)newArgumentExpression.WithoutAnnotations(ExpressionAnnotationKind).WithAdditionalAnnotations(Simplifier.Annotation)));
        }

        protected override ImmutableArray<SyntaxNode> AddExpressionArgumentToArgumentList(ImmutableArray<SyntaxNode> arguments, SyntaxNode expression)
        {
            var newArgument = SyntaxFactory.Argument((ExpressionSyntax)expression.WithoutAnnotations(ExpressionAnnotationKind));
            return arguments.Add(newArgument);
        }

        protected override bool IsMethodDeclaration(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalFunctionStatement) || node.IsKind(SyntaxKind.MethodDeclaration) || node.IsKind(SyntaxKind.SimpleLambdaExpression);

        protected override TNode RewriteCore<TNode>(
            TNode node,
            SyntaxNode replacementNode,
            ISet<ExpressionSyntax> matches)
        {
            var newNode = (TNode?)Rewriter.Visit(node, replacementNode, matches);
            RoslynDebug.AssertNotNull(newNode);
            return newNode;
        }
    }
}
