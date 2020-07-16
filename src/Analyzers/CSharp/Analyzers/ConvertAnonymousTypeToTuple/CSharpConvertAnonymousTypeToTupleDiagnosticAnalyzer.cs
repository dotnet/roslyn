// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToTuple
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpConvertAnonymousTypeToTupleDiagnosticAnalyzer
        : AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer<
            SyntaxKind,
            AnonymousObjectCreationExpressionSyntax>
    {
        public CSharpConvertAnonymousTypeToTupleDiagnosticAnalyzer()
            : base(CSharpSyntaxKinds.Instance)
        {
        }

        protected override int GetInitializerCount(AnonymousObjectCreationExpressionSyntax anonymousType)
            => anonymousType.Initializers.Count;
    }
}
