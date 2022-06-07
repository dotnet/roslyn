' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend Class MockXmlMemberIndexService
        Implements IVsXMLMemberIndexService

        Public Function CreateXMLMemberIndex(pszBinaryName As String, ByRef ppIndex As IVsXMLMemberIndex) As Integer Implements IVsXMLMemberIndexService.CreateXMLMemberIndex
            Throw New NotImplementedException()
        End Function

        Public Function GetMemberDataFromXML(pszXML As String, ByRef ppObj As IVsXMLMemberData) As Integer Implements IVsXMLMemberIndexService.GetMemberDataFromXML
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
