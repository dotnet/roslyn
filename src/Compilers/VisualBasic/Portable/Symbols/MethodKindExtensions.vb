' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module MethodKindExtensions
        <Extension>
        Friend Function TryGetAccessorDisplayName(kind As MethodKind) As String
            Select Case kind
                Case MethodKind.EventAdd
                    Return SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword)

                Case MethodKind.EventRaise
                    Return SyntaxFacts.GetText(SyntaxKind.RaiseEventKeyword)

                Case MethodKind.EventRemove
                    Return SyntaxFacts.GetText(SyntaxKind.RemoveHandlerKeyword)

                Case MethodKind.PropertyGet
                    Return SyntaxFacts.GetText(SyntaxKind.GetKeyword)

                Case MethodKind.PropertySet
                    Return SyntaxFacts.GetText(SyntaxKind.SetKeyword)

                Case Else
                    Return Nothing
            End Select
        End Function
    End Module
End Namespace
