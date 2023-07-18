' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.[Shared].Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Venus
    <ExportLanguageService(GetType(IVenusBraceMatchingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicVenusBraceMatchingService
        Implements IVenusBraceMatchingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function TryGetCorrespondingOpenBrace(token As SyntaxToken, ByRef openBrace As SyntaxToken) As Boolean Implements IVenusBraceMatchingService.TryGetCorrespondingOpenBrace
            If token.Kind = SyntaxKind.CloseBraceToken Then
                Dim tuples = token.GetRequiredParent().GetBraces()
                openBrace = tuples.openBrace
                Return openBrace.Kind = SyntaxKind.OpenBraceToken
            End If

            Return False
        End Function
    End Class
End Namespace
