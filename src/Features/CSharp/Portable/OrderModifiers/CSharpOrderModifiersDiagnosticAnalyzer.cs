// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.OrderModifiers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpOrderModifiersDiagnosticAnalyzer : AbstractOrderModifiersDiagnosticAnalyzer
    {
        public CSharpOrderModifiersDiagnosticAnalyzer()
            : base(CSharpSyntaxFactsService.Instance,
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
            foreach (var child in root.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var node = child.AsNode();
                    if (node is MemberDeclarationSyntax memberDeclaration)
                    {
                        CheckModifiers(context, preferredOrder, severity, memberDeclaration);

                        // Recurse and check children.  Note: we only do this if we're on an actual 
                        // member declaration.  Once we hit something that isn't, we don't need to 
                        // keep recursing.  This prevents us from actually entering things like method 
                        // bodies.
                        Recurse(context, preferredOrder, severity, node);
                    }
                    else if (node is AccessorListSyntax accessorList)
                    {
                        foreach (var accessor in accessorList.Accessors)
                        {
                            CheckModifiers(context, preferredOrder, severity, accessor);
                        }
                    }
                }
            }
        }
    }
}
