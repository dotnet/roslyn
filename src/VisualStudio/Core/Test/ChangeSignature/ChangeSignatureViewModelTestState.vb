' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
    Friend Class ChangeSignatureViewModelTestState
        Public ReadOnly OriginalParameterList As ImmutableArray(Of IParameterSymbol)
        Public ReadOnly ViewModel As ChangeSignatureDialogViewModel

        Public Sub New(viewModel As ChangeSignatureDialogViewModel, originalParameterList As ImmutableArray(Of IParameterSymbol))
            Me.ViewModel = viewModel
            Me.OriginalParameterList = originalParameterList
        End Sub
    End Class
End Namespace
