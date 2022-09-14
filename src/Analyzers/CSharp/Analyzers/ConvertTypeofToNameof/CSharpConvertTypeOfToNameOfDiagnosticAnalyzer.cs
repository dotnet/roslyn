// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf
{
    /// <summary>
    /// Finds code like typeof(someType).Name and determines whether it can be changed to nameof(someType), if yes then it offers a diagnostic
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpConvertTypeOfToNameOfDiagnosticAnalyzer : AbstractConvertTypeOfToNameOfDiagnosticAnalyzer
    {
        private static readonly string s_title = CSharpAnalyzersResources.typeof_can_be_converted__to_nameof;

        public CSharpConvertTypeOfToNameOfDiagnosticAnalyzer() : base(s_title)
        {
        }

        protected override bool IsValidTypeofAction(OperationAnalysisContext context)
        {
            var node = context.Operation.Syntax;

            // nameof was added in CSharp 6.0, so don't offer it for any languages before that time
            if (node.GetLanguageVersion() < LanguageVersion.CSharp6)
            {
                return false;
            }

            // Make sure that the syntax that we're looking at is actually a typeof expression and that
            // the parent syntax is a member access expression otherwise the syntax is not the kind of
            // expression that we want to analyze
            return node is TypeOfExpressionSyntax { Parent: MemberAccessExpressionSyntax } typeofExpression &&
                // nameof(System.Void) isn't allowed in C#.
                typeofExpression is not { Type: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword } };
        }
    }
}
