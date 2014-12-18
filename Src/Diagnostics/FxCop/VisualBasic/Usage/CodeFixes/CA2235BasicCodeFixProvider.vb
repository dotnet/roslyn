' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <ExportCodeFixProvider("CA2229 CodeFix provider", LanguageNames.VisualBasic), [Shared]>
    Public Class CA2235BasicCodeFixProvider
        Inherits CA2235CodeFixProviderBase

        Protected Overrides Function GetFieldDeclarationNode(node As SyntaxNode) As SyntaxNode
            Dim fieldNode = node
            While fieldNode IsNot Nothing AndAlso fieldNode.Kind() <> VisualBasic.SyntaxKind.FieldDeclaration
                fieldNode = fieldNode.Parent
            End While

            Return If(fieldNode.Kind() = VisualBasic.SyntaxKind.FieldDeclaration, fieldNode, Nothing)
        End Function
    End Class
End Namespace