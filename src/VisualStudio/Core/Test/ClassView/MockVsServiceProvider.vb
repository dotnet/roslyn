' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

            throw ExceptionUtilities.UnexpectedValue(serviceType)
        End Function
    End Class
End Namespace
