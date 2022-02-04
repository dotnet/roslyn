' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend Class MockVsSmartOpenScope
        Implements IVsSmartOpenScope

        Public Function OpenScope(wszScope As String, dwOpenFlags As UInteger, ByRef riid As Guid, ByRef ppIUnk As Object) As Integer Implements IVsSmartOpenScope.OpenScope
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
