// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.OrderModifiers;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpOrderModifiersDiagnosticAnalyzer : AbstractOrderModifiersDiagnosticAnalyzer
    {
        public CSharpOrderModifiersDiagnosticAnalyzer()
            : base(CSharpSyntaxFacts.Instance,
                   CSharpCodeStyleOptions.PreferredModifierOrder,
                   CSharpOrderModifiersHelper.Instance,
                   LanguageNames.CSharp)
        {
        }

        protected override void Recurse(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            ReportDiagnostic severity,
            SyntaxNode root)
        {
            foreach (var node in root.ChildNodes())
            {
                if (node is MemberDeclarationSyntax || node.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    CheckModifiers(context, preferredOrder, severity, node);
                }
                else if (node is AccessorListSyntax accessorList)
                {
                    foreach (var accessor in accessorList.Accessors)
                    {
                        CheckModifiers(context, preferredOrder, severity, accessor);
                    }
                }

                // We don't stop at member declarations only because this prevents us from visiting local functions.
                Recurse(context, preferredOrder, severity, node);
            }
        }
    }
}
