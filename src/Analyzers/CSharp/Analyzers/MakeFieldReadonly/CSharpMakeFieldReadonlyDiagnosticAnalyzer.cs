// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.MakeFieldReadonly;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MakeFieldReadonly
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpMakeFieldReadonlyDiagnosticAnalyzer
        : AbstractMakeFieldReadonlyDiagnosticAnalyzer<SyntaxKind, ThisExpressionSyntax>
    {
        protected override ISyntaxKinds SyntaxKinds => CSharpSyntaxKinds.Instance;

        protected override bool IsWrittenTo(SemanticModel semanticModel, ThisExpressionSyntax expression, CancellationToken cancellationToken)
            => expression.IsWrittenTo(semanticModel, cancellationToken);
    }
}
