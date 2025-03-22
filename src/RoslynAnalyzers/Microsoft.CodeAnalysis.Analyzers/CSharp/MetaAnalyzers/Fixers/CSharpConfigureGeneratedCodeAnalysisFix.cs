// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpConfigureGeneratedCodeAnalysisFix)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class CSharpConfigureGeneratedCodeAnalysisFix() : ConfigureGeneratedCodeAnalysisFix
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
