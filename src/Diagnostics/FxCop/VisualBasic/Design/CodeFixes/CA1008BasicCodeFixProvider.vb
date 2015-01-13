' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ' <summary>
    ' CA1008: Enums should have zero value
    ' </summary>
    <ExportCodeFixProvider("CA1008", LanguageNames.VisualBasic), [Shared]>
    Public Class CA1008BasicCodeFixProvider
        Inherits CA1008CodeFixProviderBase

        Protected Overrides Function GetParentNodeOrSelfToFix(nodeToFix As SyntaxNode) As SyntaxNode
            If nodeToFix.IsKind(SyntaxKind.EnumStatement) And nodeToFix.Parent IsNot Nothing Then
                Return nodeToFix.Parent
            End If

            Return nodeToFix
        End Function
    End Class
End Namespace