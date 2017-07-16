' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer
        Inherits AbstractAddAccessibilityModifiersDiagnosticAnalyzer(Of CompilationUnitSyntax)

        'Protected Overrides Function IsFirstFieldDeclarator(node As SyntaxNode) As Boolean
        '    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
        '    Dim declarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
        '    Dim fieldDeclaration = DirectCast(declarator.Parent, FieldDeclarationSyntax)

        '    Return fieldDeclaration.Declarators(0) Is declarator AndAlso
        '           declarator.Names(0) Is modifiedIdentifier
        'End Function

        'Protected Overrides Function CanHaveModifiersWorker(symbol As ISymbol) As Boolean
        '    Return True
        'End Function

        Protected Overrides Sub ProcessCompilationUnit(context As SyntaxTreeAnalysisContext, generator As SyntaxGenerator, [option] As CodeStyleOption(Of AccessibilityModifiersRequired), compilationUnitSyntax As CompilationUnitSyntax)
        End Sub
    End Class
End Namespace
