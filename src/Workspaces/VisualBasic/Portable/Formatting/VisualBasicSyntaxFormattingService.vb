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
    Friend Class VisualBasicSyntaxFormattingService
        Inherits VisualBasicSyntaxFormatting
        Implements ISyntaxFormattingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function ShouldFormatOnTypedCharacterAsync(document As Document, typedChar As Char, caretPosition As Integer, cancellationToken As CancellationToken) As Task(Of Boolean) Implements ISyntaxFormattingService.ShouldFormatOnTypedCharacterAsync
            Return Task.FromResult(False)
        End Function

        Public Function GetFormattingChangesOnTypedCharacterAsync(document As Document, caretPosition As Integer, indentationOptions As IndentationOptions, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TextChange)) Implements ISyntaxFormattingService.GetFormattingChangesOnTypedCharacterAsync
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Function GetFormattingChangesOnPasteAsync(document As Document, textSpan As TextSpan, options As SyntaxFormattingOptions, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of TextChange)) Implements ISyntaxFormattingService.GetFormattingChangesOnPasteAsync
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
