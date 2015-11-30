' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Friend Class MockServiceProvider
        Implements SVsServiceProvider

        Private ReadOnly _navigationTool As IVsNavigationTool

        Public Sub New(navigationTool As IVsNavigationTool)
            _navigationTool = navigationTool
        End Sub

        Public Function GetService(serviceType As Type) As Object Implements SVsServiceProvider.GetService
            If serviceType Is GetType(SVsClassView) Then
                Return _navigationTool
            End If

            Return Contract.FailWithReturn(Of Object)($"GetService only handles {NameOf(SVsClassView)}")
        End Function
    End Class
End Namespace