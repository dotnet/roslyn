// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public class CSharpTrackingDiagnosticAnalyzer : TrackingDiagnosticAnalyzer<SyntaxKind>
    {
        private static readonly Regex s_omittedSyntaxKindRegex =
            new Regex(@"Using|Extern|Parameter|Constraint|Specifier|Initializer|Global|Method|Destructor|MemberBindingExpression|ElementBindingExpression|ArrowExpressionClause|NameOfExpression");

        protected override bool IsOnCodeBlockSupported(SymbolKind symbolKind, MethodKind methodKind, bool returnsVoid)
        {
            return base.IsOnCodeBlockSupported(symbolKind, methodKind, returnsVoid) && methodKind != MethodKind.EventRaise;
        }

        protected override bool IsAnalyzeNodeSupported(SyntaxKind syntaxKind)
        {
            return base.IsAnalyzeNodeSupported(syntaxKind) && !s_omittedSyntaxKindRegex.IsMatch(syntaxKind.ToString());
        }
    }
}
