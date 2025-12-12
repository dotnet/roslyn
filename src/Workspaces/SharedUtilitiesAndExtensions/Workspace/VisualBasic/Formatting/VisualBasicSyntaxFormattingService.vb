' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Indentation
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportLanguageService(GetType(ISyntaxFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicSyntaxFormattingService
        Inherits VisualBasicSyntaxFormatting
        Implements ISyntaxFormattingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function ShouldFormatOnTypedCharacter(document As ParsedDocument, typedChar As Char, caretPosition As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFormattingService.ShouldFormatOnTypedCharacter
            Return False
        End Function

        Public Function GetFormattingChangesOnTypedCharacter(document As ParsedDocument, caretPosition As Integer, indentationOptions As IndentationOptions, cancellationToken As CancellationToken) As ImmutableArray(Of TextChange) Implements ISyntaxFormattingService.GetFormattingChangesOnTypedCharacter
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Function GetFormattingChangesOnPaste(document As ParsedDocument, textSpan As TextSpan, options As SyntaxFormattingOptions, cancellationToken As CancellationToken) As ImmutableArray(Of TextChange) Implements ISyntaxFormattingService.GetFormattingChangesOnPaste
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
