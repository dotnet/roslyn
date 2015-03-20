' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
