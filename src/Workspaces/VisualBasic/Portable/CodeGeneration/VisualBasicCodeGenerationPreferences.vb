' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend NotInheritable Class VisualBasicCodeGenerationPreferences
        Inherits CodeGenerationPreferences

        Public Sub New(placeSystemNamespaceFirst As Boolean)
            MyBase.New(placeSystemNamespaceFirst)
        End Sub

        Public Overrides ReadOnly Property PlaceImportsInsideNamespaces As Boolean
            Get
                ' Visual Basic doesn't support imports inside namespaces
                Return False
            End Get
        End Property

        Public Overrides Function GetOptions(context As CodeGenerationContext) As CodeGenerationOptions
            Return New VisualBasicCodeGenerationOptions(context, Me)
        End Function

        Public Shared Function Create(documentOptions As OptionSet) As VisualBasicCodeGenerationPreferences
            Return New VisualBasicCodeGenerationPreferences(
                placeSystemNamespaceFirst:=documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.VisualBasic))
        End Function

        Public Shared Shadows Async Function FromDocumentAsync(document As Document, cancellationToken As CancellationToken) As Task(Of VisualBasicCodeGenerationPreferences)
            Dim documentOptions = Await document.GetOptionsAsync(cancellationToken).ConfigureAwait(False)
            Return Create(documentOptions)
        End Function
    End Class
End Namespace
