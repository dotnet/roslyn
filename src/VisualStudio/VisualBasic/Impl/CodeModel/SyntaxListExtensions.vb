' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Friend Module SyntaxListExtensions

        <Extension>
        Public Function AsReadOnlyList(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode)) As IReadOnlyList(Of TNode)
            Return list
        End Function

        <Extension>
        Public Function AsReadOnlyList(Of TNode As SyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As IReadOnlyList(Of TNode)
            Return list
        End Function
    End Module
End Namespace
