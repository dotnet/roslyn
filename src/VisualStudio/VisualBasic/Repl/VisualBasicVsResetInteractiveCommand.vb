' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Interactive
Imports Roslyn.VisualStudio.Services.Interactive

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive
    Friend Class VisualBasicVsResetInteractiveCommand
        Inherits AbstractResetInteractiveCommand

        Public Sub New(workspace As VisualStudioWorkspace, interactiveWindowProvider As VsInteractiveWindowProvider, serviceProvider As IServiceProvider)
            MyBase.New(workspace, interactiveWindowProvider, serviceProvider)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return "VisualBasic"
            End Get
        End Property

        Protected Overrides Function CreateImport(referenceName As String) As String
            Return String.Format("#R ""{0}""", referenceName)
        End Function

        Protected Overrides Function CreateReference(namespaceName As String) As String
            Return String.Format("Imports {0}", namespaceName)
        End Function
    End Class

End Namespace
