// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAddAccessibilityModifiersDiagnosticAnalyzer : AbstractAddAccessibilityModifiersDiagnosticAnalyzer
    {
        public CSharpAddAccessibilityModifiersDiagnosticAnalyzer()
        {
        }

        protected override bool IsFirstFieldDeclarator(SyntaxNode node)
        {
            var declarator = (VariableDeclaratorSyntax)node;
            var declaration = (VariableDeclarationSyntax)declarator.Parent;

#if DEBUG
            var field = (FieldDeclarationSyntax)declaration.Parent;
#endif

            return declaration.Variables[0] == declarator;
        }

        protected override bool CanHaveModifiersWorker(ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
                if (method.MethodKind == MethodKind.Destructor ||
                    method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
