' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ChangeSignature
    Friend Class AddParameterViewModelTestState
        Public ReadOnly ViewModel As AddParameterDialogViewModel

        Public Sub New(viewModel As AddParameterDialogViewModel)
            Me.ViewModel = viewModel
        End Sub
    End Class
End Namespace
