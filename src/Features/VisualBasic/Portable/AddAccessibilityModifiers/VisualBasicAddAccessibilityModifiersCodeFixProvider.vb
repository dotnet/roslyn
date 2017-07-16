' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddAccessibilityModifiers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddAccessibilityModifiersCodeFixProvider
        Inherits AbstractAddAccessibilityModifiersCodeFixProvider

        Protected Overrides Function MapFieldDeclaration(node As SyntaxNode) As SyntaxNode
            Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
            Dim declarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
            Dim fieldDeclaration = DirectCast(declarator.Parent, FieldDeclarationSyntax)

            Return fieldDeclaration
        End Function
    End Class
End Namespace
