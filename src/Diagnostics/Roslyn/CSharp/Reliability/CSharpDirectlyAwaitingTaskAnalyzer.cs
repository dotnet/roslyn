// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.CSharp.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpDirectlyAwaitingTaskAnalyzer : DirectlyAwaitingTaskAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind AwaitSyntaxKind
        {
            get
            {
                return SyntaxKind.AwaitExpression;
            }
        }

        protected override SyntaxNode GetAwaitedExpression(SyntaxNode awaitNode)
        {
            return ((AwaitExpressionSyntax)awaitNode).Expression;
        }
    }
}
