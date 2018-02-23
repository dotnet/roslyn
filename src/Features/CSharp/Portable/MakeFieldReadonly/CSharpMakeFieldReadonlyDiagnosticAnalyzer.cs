// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MakeFieldReadonly;

namespace Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpMakeFieldReadonlyDiagnosticAnalyzer :
        AbstractMakeFieldReadonlyDiagnosticAnalyzer<IdentifierNameSyntax, ConstructorDeclarationSyntax>
    {
        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override bool IsWrittenTo(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
            => name.IsWrittenTo();

        protected override bool IsMemberOfThisInstance(SyntaxNode node)
        {
            // if it is a qualified name, make sure it is `this.name`
            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Expression is ThisExpressionSyntax;
            }

            // make sure it isn't in an object initializer
            if (node.Parent.Parent is InitializerExpressionSyntax)
            {
                return false;
            }

            return true;
        }
    }
}
