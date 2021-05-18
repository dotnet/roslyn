// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpEnableConcurrentExecutionFix)), Shared]
    public sealed class CSharpEnableConcurrentExecutionFix : EnableConcurrentExecutionFix
    {
        protected override IEnumerable<SyntaxNode> GetStatements(SyntaxNode methodDeclaration)
        {
            if (methodDeclaration is MethodDeclarationSyntax method)
            {
                if (method.ExpressionBody != null)
                {
                    return new[] { method.ExpressionBody.Expression };
                }
                else if (method.Body != null)
                {
                    return method.Body.Statements;
                }
            }

            return Enumerable.Empty<SyntaxNode>();
        }
    }
}
