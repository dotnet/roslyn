' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks
    Friend Class MockServiceProvider
        Implements IServiceProvider

        Private ReadOnly _componentModel As MockComponentModel

        Public Sub New(componentModel As MockComponentModel)
            Me._componentModel = componentModel
        End Sub

        Public Function GetService(serviceType As Type) As Object Implements IServiceProvider.GetService
            If serviceType Is GetType(SComponentModel) Then
                Return _componentModel
            End If

            If serviceType Is GetType(SVsShell) OrElse
                   serviceType Is GetType(SVsSolution) Then
                ' All calls to retrieve these services should be resilient against null.
                Return Nothing
            End If

            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
