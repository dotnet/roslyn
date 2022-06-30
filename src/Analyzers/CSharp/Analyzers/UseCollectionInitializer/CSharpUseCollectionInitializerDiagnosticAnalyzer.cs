// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCollectionInitializerDiagnosticAnalyzer :
        AbstractUseCollectionInitializerDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            BaseObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax>
    {
        protected override bool AreCollectionInitializersSupported(Compilation compilation)
            => compilation.LanguageVersion() >= LanguageVersion.CSharp3;

        protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;
    }
}
