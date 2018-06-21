// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToTuple
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpConvertAnonymousTypeToTupleDiagnosticAnalyzer
        : AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer<SyntaxKind>
    {
        protected override SyntaxKind GetAnonymousObjectCreationExpressionSyntaxKind()
            => SyntaxKind.AnonymousObjectCreationExpression;
    }
}
