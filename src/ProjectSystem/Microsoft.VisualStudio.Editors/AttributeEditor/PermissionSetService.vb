' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.Security
Imports System.Security.Permissions
Imports System.Xml
Imports System.IO
Imports Microsoft.Build.Tasks.Deployment.ManifestUtilities
Imports Microsoft.VisualStudio.Shell.Design.Serialization

Imports NativeMethods = Microsoft.VisualStudio.Editors.Interop.NativeMethods

Imports Microsoft.VisualStudio.Editors.SR

Namespace Microsoft.VisualStudio.Editors.VBAttributeEditor

    '--------------------------------------------------------------------------
    ' PermissionSetService:
    '   Builder Service Class. Implements SVbPermissionSetService 
    '   exposed via the IVbPermissionSetService interface.
    '--------------------------------------------------------------------------
    <CLSCompliant(False)> _
    Friend NotInheritable Class PermissionSetService
        Implements Interop.IVbPermissionSetService

        Private _serviceProvider As IServiceProvider

        Friend Sub New(ByVal sp As IServiceProvider)
            _serviceProvider = sp
        End Sub

        Public Function CreateSecurityElementFromXmlElement(ByVal element As XmlElement) As SecurityElement

            ' Create the new security element
            Dim securityElement As New SecurityElement(element.Name)

            ' Add the attributes
            For Each attribute As XmlAttribute In element.Attributes
                securityElement.AddAttribute(attribute.Name, attribute.Value)
            Next

            ' Add the child nodes
            For Each node As XmlNode In element.ChildNodes
                If node.NodeType = XmlNodeType.Element Then
                    securityElement.AddChild(CreateSecurityElementFromXmlElement(CType(node, XMLElement)))
                End If
            Next

            Return securityElement
        End Function

        Public Function LoadPermissionSet(ByVal strPermissionSet As String) As PermissionSet

            ' Load the XML
            Dim document As New XmlDocument
            Using xmlReader As System.Xml.XmlReader = System.Xml.XmlReader.Create(New System.IO.StringReader(strPermissionSet))
                document.Load(xmlReader)
            End Using

            ' Create the permission set from the XML
            Dim permissionSet As New PermissionSet(PermissionState.None)
            permissionSet.FromXml(CreateSecurityElementFromXmlElement(document.DocumentElement))
            Return permissionSet
        End Function

        Private Function DocDataToStream(ByVal doc As DocData) As Stream
            Dim retStream As New MemoryStream()
            Using docReader As New DocDataTextReader(doc, False)
                Dim writer As New StreamWriter(retStream)
                writer.Write(docReader.ReadToEnd())
                writer.Flush()
                retStream.Seek(0, SeekOrigin.Begin)
            End Using
            Return retStream
        End Function

        Public Function ComputeZonePermissionSet(ByVal strManifestFileName As String, ByVal strTargetZone As String, ByVal strExcludedPermissions As String) As Object Implements Interop.IVbPermissionSetService.ComputeZonePermissionSet

            Try

                Dim projectPermissionSet As PermissionSet = Nothing

                If (strManifestFileName IsNot Nothing) AndAlso (strManifestFileName.Length > 0) Then

                    Dim manifestInfo As New TrustInfo
                    manifestInfo.PreserveFullTrustPermissionSet = true

                    Try
                        Using appManifestDocData as New DocData(_serviceProvider, strManifestFileName)

                            manifestInfo.ReadManifest(DocDataToStream(appManifestDocData))

                        End Using

                        projectPermissionSet = manifestInfo.PermissionSet

                    Catch

                        ' If this fails, there is no project permission set

                    End Try

                    If manifestInfo.IsFullTrust Then
                        Return Nothing
                    End If

                End If

                Dim identityList As String() = Nothing

                If (strExcludedPermissions IsNot Nothing) AndAlso (strExcludedPermissions.Length > 0) Then
                    identityList = StringToIdentityList(strExcludedPermissions)
                End If

                Return SecurityUtilities.ComputeZonePermissionSet( _
                    strTargetZone, _
                    projectPermissionSet, _
                    identityList)

            Catch ex As Exception
            End Try

            Return Nothing

        End Function

        Public Function IsAvailableInProject(ByVal strPermissionSet As String, ByVal ProjectPermissionSet As Object, ByRef isAvailable As Boolean) As Integer Implements Interop.IVbPermissionSetService.IsAvailableInProject

            Try

                isAvailable = True

                ' Validate the project permission set
                If (ProjectPermissionSet IsNot Nothing) AndAlso (TypeOf (ProjectPermissionSet) Is PermissionSet) Then

                    ' Load the string permission set
                    Dim permissionSet As PermissionSet = LoadPermissionSet(strPermissionSet)
                    If permissionSet IsNot Nothing Then

                        ' Check the subset relationship
                        isAvailable = permissionSet.IsSubsetOf(CType(ProjectPermissionSet, PermissionSet))

                    End If

                End If

            Catch ex As Exception
            End Try

            Return NativeMethods.S_OK
        End Function

        ' Returns S_FALSE if there is no tip
        Public Function GetRequiredPermissionsTip(ByVal strPermissionSet As String, ByRef strTip As String) As Integer Implements Interop.IVbPermissionSetService.GetRequiredPermissionsTip

            Dim hasTip As Boolean = False

            Try

                Dim isFirstPermission As Boolean = True

                ' Load the string permission set
                Dim permissionSet As PermissionSet = LoadPermissionSet(strPermissionSet)
                If permissionSet IsNot Nothing Then

                    Const strPrefix As String = "System.Security.Permissions."

                    For Each permission As Object In permissionSet

                        If Not isFirstPermission Then
                            strTip &= vbCrLf
                        Else

                            strTip &= SR.GetString(PermissionSet_Requires) & vbCrLf

                            hasTip = True
                            isFirstPermission = False
                        End If

                        ' Chop off the type prefix if present
                        Dim strTemp As String = permission.GetType.ToString()
                        If strTemp.StartsWith(strPrefix) Then
                            strTemp = strTemp.Substring(strPrefix.Length)
                        End If

                        strTip &= strTemp
                    Next

                End If

            Catch ex As Exception
            End Try

            If hasTip Then
                Return NativeMethods.S_OK
            Else
                Return NativeMethods.S_FALSE
            End If
        End Function


        Private Shared Function StringToIdentityList(ByVal s As String) As String()
            Dim a() As String = s.Split(CChar(";"))
            For i As Integer = 0 To a.Length - 1
                a(i) = a(i).Trim()
            Next
            Return a
        End Function


    End Class

End Namespace
