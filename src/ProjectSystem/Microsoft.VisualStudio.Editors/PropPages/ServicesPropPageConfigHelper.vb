' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml
Imports Microsoft.VisualStudio.Editors.SettingsDesigner
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend NotInheritable Class ServicesPropPageAppConfigHelper
        Private Shared s_clientRoleManagerType As Type = GetType(System.Web.ClientServices.Providers.ClientRoleProvider)
        Private Shared s_clientFormsMembershipProviderType As Type = GetType(System.Web.ClientServices.Providers.ClientFormsAuthenticationMembershipProvider)
        Private Shared s_clientWindowsMembershipProviderType As Type = GetType(System.Web.ClientServices.Providers.ClientWindowsAuthenticationMembershipProvider)

        Private Const s_systemWeb As String = "system.web"
        Private Const s_roleManager As String = "roleManager"
        Private Const s_providers As String = "providers"
        Private Const s_appSettings As String = "appSettings"
        Private Const s_connectionString As String = "connectionString"
        Private Const s_connectionStrings As String = "connectionStrings"
        Private Const s_membership As String = "membership"
        Private Const s_configuration As String = "configuration"
        Private Const s_clientSettingsProviderPrefix As String = "ClientSettingsProvider."
        Private Const s_defaultProvider As String = "defaultProvider"

        Private Const s_cacheTimeout As String = "cacheTimeout"
        Private Const s_cacheTimeoutDefault As String = "86400" '86400 seconds = 1 day

        Private Const s_enabled As String = "enabled"
        Private Const s_enabledDefault As String = "true"

        Private Const s_honorCookieExpiry As String = "honorCookieExpiry"
        Private Const s_honorCookieExpiryDefault As String = "false"

        Private Const s_serviceUri As String = "serviceUri"
        Private Shared s_serviceUriDefault As String = String.Empty

        Private Const s_connectionStringName As String = "connectionStringName"
        Private Const s_connectionStringNameDefault As String = "DefaultConnection"
        Friend Const connectionStringValueDefault As String = "Data Source = |SQL/CE|"

        Private Const s_credentialsProvider As String = "credentialsProvider"
        Private Shared s_credentialsProviderDefault As String = String.Empty

        Private Const s_savePasswordHashLocally As String = "savePasswordHashLocally"
        Private Const s_savePasswordHashLocallyDefault As String = "true"

        Private Const s_add As String = "add"
        Private Const s_key As String = "key"
        Private Const s_name As String = "name"
        Private Const s_type As String = "type"
        Private Const s_value As String = "value"
        Private Const s_childPrefix As String = "child::"

        Private Const s_roleManagerDefaultNameDefault As String = "ClientRoleProvider"
        Private Const s_membershipDefaultNameDefault As String = "ClientAuthenticationMembershipProvider"

        Private Sub New()
            'Don't create a public constructor
        End Sub

#Region "Document"
        Friend Shared Function AppConfigXmlDocument(ByVal provider As IServiceProvider, ByVal projectHierarchy As IVsHierarchy, Optional ByVal createIfNotPresent As Boolean = False) As XmlDocument
            Dim contents As String = Nothing
            Using docData As DocData = GetDocData(provider, projectHierarchy, createIfNotPresent, False)
                If docData Is Nothing Then Return Nothing

                Using textReader As New DocDataTextReader(docData)
                    contents = textReader.ReadToEnd()
                End Using
            End Using
            Return ServicesPropPageAppConfigHelper.XmlDocumentFromText(contents)
        End Function

        Friend Shared Function AppConfigXmlDocument(ByVal propertyPageSite As OLE.Interop.IPropertyPageSite, ByVal projectHierarchy As IVsHierarchy, Optional ByVal createIfNotPresent As Boolean = False) As XmlDocument
            If propertyPageSite IsNot Nothing Then
                Dim provider As IServiceProvider = CType(propertyPageSite, IServiceProvider)
                If provider IsNot Nothing Then
                    Return AppConfigXmlDocument(provider, projectHierarchy, createIfNotPresent)
                End If
            End If

            Return Nothing
        End Function

        Friend Shared Function GetDocData(ByVal provider As IServiceProvider, ByVal projectHierarchy As IVsHierarchy, Optional ByVal createIfNotPresent As Boolean = False, Optional ByVal writeable As Boolean = False) As DocData
            Return AppConfigSerializer.GetAppConfigDocData(provider, projectHierarchy, createIfNotPresent, writeable)
        End Function

        Friend Shared Function XmlDocumentFromText(ByVal contents As String) As XmlDocument
            Dim doc As XmlDocument = Nothing
            If Not String.IsNullOrEmpty(contents) Then
                doc = New XmlDocument()
                Try
                    Using reader As System.Xml.XmlReader = System.Xml.XmlReader.Create(New System.IO.StringReader(contents))
                        doc.Load(reader)
                    End Using
                Catch ex As XmlException
                    Return Nothing
                End Try
            End If
            Return doc
        End Function

        Friend Shared Function TryWriteXml(ByVal appConfigDocument As XmlDocument, ByVal provider As IServiceProvider, ByVal hierarchy As IVsHierarchy) As Boolean
            Dim fileName As String = Nothing
            Dim appConfigItemId As UInteger
            Dim flags As UInteger = CUInt(__PSFFLAGS.PSFF_CreateIfNotExist Or __PSFFLAGS.PSFF_FullPath)

            Dim ProjSpecialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
            ProjSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, Flags, appConfigItemId, fileName)

            Dim sb As New StringBuilder
            Using writer As New XmlTextWriter(New StringWriter(sb, CultureInfo.InvariantCulture))
                writer.Formatting = Formatting.Indented
                appConfigDocument.WriteContentTo(writer)
                writer.Flush()
            End Using

            Using docData As DocData = New DocData(provider, fileName)
                Using textWriter As New DocDataTextWriter(docData)
                    Try
                        textWriter.Write(sb.ToString())
                        textWriter.Close()
                        SaveAppConfig(fileName, provider, hierarchy)
                    Catch ex As COMException When IsCheckoutOrCancelError(ex)
                        Return False
                    End Try
                End Using
            End Using

            Return True
        End Function

        Private Shared Function IsCheckoutOrCancelError(ByVal cex As ComException) As Boolean
            Return cex IsNot Nothing AndAlso (cex.ErrorCode = &H80041004 OrElse cex.ErrorCode = &H8004000C)
        End Function

        'This code is stolen from SecurityPropertyPage.  If you don't do this, the document doesn't get written unless it
        'happens to be open

        <SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")> _
        Private Shared Sub SaveAppConfig(ByVal fileName As String, ByVal provider As IServiceProvider, ByVal hierarchy As IVsHierarchy)
            Dim rdt As IVsRunningDocumentTable = TryCast(provider.GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
            Debug.Assert((rdt IsNot Nothing), "What?  No RDT?")
            If rdt Is Nothing Then Throw New InvalidOperationException("No RDT")

            Dim hier As IVsHierarchy = Nothing
            Dim flags As UInteger
            Dim localPunk As IntPtr = IntPtr.Zero
            Dim localFileName As String = Nothing
            Dim itemId As UInteger
            Dim docCookie As UInteger = 0
            Dim readLocks As UInteger = 0
            Dim editLocks As UInteger = 0

            Try
                VSErrorHandler.ThrowOnFailure(hierarchy.ParseCanonicalName(fileName, itemId))
                VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), fileName, hier, itemid, localPunk, docCookie))
            Finally
                If (localPunk <> IntPtr.Zero) Then
                    Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            Try
                VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hier, itemid, localPunk))
            Finally
                If (localPunk <> IntPtr.Zero) Then
                    Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            If editLocks = 1 Then
                ' we're the only person with it open, save the document
                VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), hier, itemId, docCookie))
            End If
        End Sub
#End Region

#Region "Nodes/NodeList"
        Friend Shared Function GetXmlNode(ByVal currentNode As XmlNode, ByVal ParamArray path() As String) As XmlNode
            If path Is Nothing OrElse path.Length = 0 Then Return Nothing
            For i As Integer = 0 To path.Length - 1
                If currentNode Is Nothing Then Return Nothing
                currentNode = currentNode.SelectSingleNode(s_childPrefix & path(i))
            Next
            Return currentNode
        End Function

        Private Shared Function GetXmlNodeWithValueFromList(ByVal list As XmlNodeList, ByVal attributeName As String, ByVal searchValue As String) As XmlNode
            If list Is Nothing Then Return Nothing
            For Each node As XmlNode In list
                If GetAttribute(node, attributeName) = searchValue Then Return node
            Next
            Return Nothing
        End Function

        Private Shared Function GetConfigurationNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(doc, s_configuration)
        End Function

        Private Shared Function GetConnectionStringsNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), s_connectionStrings)
        End Function

        Private Shared Function GetSystemWebNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), s_systemWeb)
        End Function

        Private Shared Function GetRoleManagerNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetSystemWebNode(doc), s_roleManager)
        End Function

        Private Shared Function GetRoleManagerProvidersNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetRoleManagerNode(doc), s_providers)
        End Function

        Private Shared Function GetMembershipNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetSystemWebNode(doc), s_membership)
        End Function

        Private Shared Function GetMembershipProvidersNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetMembershipNode(doc), s_providers)
        End Function

        Private Shared Function GetDefaultClientServicesRoleManagerProviderNode(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As XmlNode
            Dim defaultProviderName As String = GetRoleManagerDefaultProviderName(doc)
            If String.IsNullOrEmpty(defaultProviderName) Then Return Nothing
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetRoleManagerProvidersNode(doc), s_add)
            Dim addNode As XmlNode = GetXmlNodeWithValueFromList(addNodeList, s_name, defaultProviderName)
            If addNode IsNot Nothing AndAlso IsClientRoleManagerProviderType(GetAttribute(addNode, s_type), projectHierarchy) Then
                Return addNode
            End If
            Return Nothing
        End Function

        Private Shared Function GetXmlNodeList(ByVal node As XmlNode, ByVal listName As String) As XmlNodeList
            If node Is Nothing Then Return Nothing
            Return node.SelectNodes(s_childPrefix & listName)
        End Function

        Private Shared Function GetDefaultClientServicesMembershipProviderNode(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As XmlNode
            Dim defaultProviderName As String = GetMembershipDefaultProviderName(doc)
            If String.IsNullOrEmpty(defaultProviderName) Then Return Nothing
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetMembershipProvidersNode(doc), s_add)
            Dim addNode As XmlNode = GetXmlNodeWithValueFromList(addNodeList, s_name, defaultProviderName)
            If addNode IsNot Nothing AndAlso IsClientMembershipProviderType(GetAttribute(addNode, s_type), projectHierarchy) Then
                Return addNode
            End If
            Return Nothing
        End Function

        Private Shared Function GetAppSettingsNode(ByVal doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), s_appSettings)
        End Function

        Private Shared Function GetAppSettingsServiceUriNode(ByVal doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), s_add)
            Return GetXmlNodeWithValueFromList(addList, s_key, AppSettingsName(s_serviceUri))
        End Function

        Private Shared Function GetAppSettingsConnectionStringNameNode(ByVal doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), s_add)
            Return GetXmlNodeWithValueFromList(addList, s_key, AppSettingsName(s_connectionStringName))
        End Function

        Private Shared Function GetAppSettingsHonorCookieExpiryNode(ByVal doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), s_add)
            Return GetXmlNodeWithValueFromList(addList, s_key, AppSettingsName(s_honorCookieExpiry))
        End Function

        Private Shared Function IsClientMembershipWindowsProviderNode(ByVal node As XmlNode, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return NodeHasTypeAttribute(node, GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy))
        End Function

        Private Shared Function NodeHasTypeAttribute(ByVal node As XmlNode, ByVal typeToCheck As Type) As Boolean
            Dim nodeType As String = GetAttribute(node, s_type)
            Return TypesMatch(nodeType, typeToCheck)
        End Function

        Private Shared Sub RemoveProvidersByType(ByVal node As XmlNode, ByVal typeToRemove As Type)
            Dim providers As XmlNodeList = GetXmlNodeList(node, s_add)
            If providers IsNot Nothing Then
                For Each provider As XmlNode In providers
                    Dim providerType As String = GetAttribute(provider, s_type)
                    If providerType IsNot Nothing AndAlso (providerType.Equals(typeToRemove.FullName, StringComparison.OrdinalIgnoreCase) OrElse providerType.StartsWith(typeToRemove.FullName + ",", StringComparison.OrdinalIgnoreCase)) Then
                        RemoveNode(node, provider)
                    End If
                Next
            End If
        End Sub
#End Region

#Region "Attributes"
        Friend Shared Function GetDefaultProviderName(ByVal node As XmlNode) As String
            If node Is Nothing Then Return Nothing

            Dim defaultProviderAttribute As XmlAttribute = node.Attributes(s_defaultProvider)
            If defaultProviderAttribute IsNot Nothing Then
                Return defaultProviderAttribute.Value
            End If
            Return Nothing
        End Function

        Friend Shared Function GetRoleManagerDefaultProviderName(ByVal doc As XmlDocument) As String
            Return GetDefaultProviderName(GetRoleManagerNode(doc))
        End Function

        Friend Shared Function GetMembershipDefaultProviderName(ByVal doc As XmlDocument) As String
            Return GetDefaultProviderName(GetMembershipNode(doc))
        End Function

        Friend Shared Function GetAttribute(ByVal node As XmlNode, ByVal attributeName As String) As String
            If node Is Nothing Then Return Nothing
            Dim attribute As XmlAttribute = node.Attributes(attributeName)
            If attribute Is Nothing Then Return Nothing
            Return attribute.Value
        End Function

        Friend Shared Sub SetAttribute(ByVal doc As XmlDocument, ByVal node As XmlNode, ByVal attributeName As String, ByVal value As String)
            If node Is Nothing Then Return
            Dim attribute As XmlAttribute = node.Attributes(attributeName)
            If attribute Is Nothing Then
                attribute = CType(CreateNode(doc, XmlNodeType.Attribute, attributeName), XmlAttribute)
                node.Attributes.SetNamedItem(attribute)
            End If

            Debug.Assert(attribute IsNot Nothing, "No attribute")
            If attribute IsNot Nothing Then
                attribute.Value = value
            End If
        End Sub

        Friend Shared Sub SetAttributeIfNonNull(ByVal doc As XmlDocument, ByVal node As XmlNode, ByVal attributeName As String, ByVal value As String)
            If value IsNot Nothing Then SetAttribute(doc, node, attributeName, value)
        End Sub

        Private Shared Function SetAttributeValueAndCheckForChange(ByVal doc As XmlDocument, ByVal node As XmlNode, ByVal attributeName As String, ByVal value As IConvertible) As Boolean
            If node Is Nothing Then Return False
            Dim initialValue As String = GetAttribute(node, attributeName)
            Dim newValue As String = value.ToString(CultureInfo.InvariantCulture)
            SetAttribute(doc, node, attributeName, newValue)
            Return Not String.Equals(newValue, initialValue)
        End Function

        Friend Shared Sub SetAttributeIfNull(ByVal doc As XmlDocument, ByVal node As XmlNode, ByVal attributeName As String, ByVal value As String)
            If GetAttribute(node, attributeName) IsNot Nothing Then Return
            SetAttribute(doc, node, attributeName, value)
        End Sub

        Friend Shared Sub RemoveAttribute(ByVal node As XmlNode, ByVal attributeName As String)
            If node IsNot Nothing AndAlso Not String.IsNullOrEmpty(attributeName) Then
                Dim attributeToRemove As XmlAttribute = node.Attributes(attributeName)
                If attributeToRemove IsNot Nothing Then
                    node.Attributes.Remove(attributeToRemove)
                End If
            End If
        End Sub

        Friend Shared Sub RemoveNode(ByVal parentNode As XmlNode, ByVal childNode As XmlNode)
            If parentNode Is Nothing OrElse childNode Is Nothing Then Return
            parentNode.RemoveChild(childNode)
        End Sub
#End Region

#Region "ServicesEnabled"
        Friend Shared Function ApplicationServicesAreEnabled(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing, Optional ByVal checkForAllEnabled As Boolean = False) As Boolean
            If checkForAllEnabled Then
                Return HasDefaultClientServicesRoleManagerProvider(doc, projectHierarchy) AndAlso HasAppSettings(doc) AndAlso HasDefaultClientServicesAuthProvider(doc, projectHierarchy)
            Else
                Return HasDefaultClientServicesRoleManagerProvider(doc, projectHierarchy) OrElse HasAppSettings(doc) OrElse HasDefaultClientServicesAuthProvider(doc, projectHierarchy)
            End If
        End Function

        Friend Shared Function HasDefaultClientServicesRoleManagerProvider(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy) IsNot Nothing
        End Function

        Friend Shared Function HasAppSettings(ByVal doc As XmlDocument) As Boolean
            Return GetAppSettingsNode(doc) IsNot Nothing
        End Function

        Friend Shared Function HasDefaultClientServicesAuthProvider(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy) IsNot Nothing
        End Function

        Friend Shared Sub EnsureApplicationServicesEnabled(ByVal appConfigDocument As XmlDocument, ByVal enable As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            EnsureAppSettings(appConfigDocument, enable, projectHierarchy)
            EnsureDefaultMembershipProvider(appConfigDocument, enable, projectHierarchy)
            EnsureDefaultRoleManagerProvider(appConfigDocument, enable, projectHierarchy)
        End Sub

        Private Shared Sub EnsureDefaultRoleManagerProvider(ByVal appConfigDocument As XmlDocument, ByVal enable As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureDefaultRoleManagerNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureDefaultRoleManagerNodeDoesntExist(appConfigDocument, projectHierarchy)
            End If
        End Sub

        Private Shared Sub EnsureDefaultRoleManagerNodeExists(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<roleManager defaultProvider="Default" enabled="true">
            '  <providers>
            '    <add name="Default" type="System.Web.ClientServices.Providers.ClientRoleProvider,System.Web.Extensions, Version=2.0.0.0, Culture=neutral PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL"
            '              connectionStringName = "connSE"
            '              serviceUri = "http://localhost/testservices/rolesservice.svc"
            '              cacheTimeout = "60"
            '              honorCookieExpiry = "true"
            '         />
            '  </providers>
            '</roleManager>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, s_configuration, appConfigDocument)
            Dim systemWebNode As XmlNode = EnsureNode(appConfigDocument, s_systemWeb, configurationNode)
            Dim roleManagerNode As XmlNode = EnsureNode(appConfigDocument, s_roleManager, systemWebNode)
            Dim defaultSettingNode As XmlNode

            Dim defaultProviderAttribute As XmlAttribute = roleManagerNode.Attributes(s_defaultProvider)

            'If we already have a default provider, make sure it's one of ours
            If defaultProviderAttribute IsNot Nothing Then
                defaultSettingNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    'We had a default, and it wasn't one of ours.  Remove the default attribute
                    RemoveAttribute(roleManagerNode, s_defaultProvider)
                    defaultProviderAttribute = Nothing
                End If
            End If

            If defaultProviderAttribute Is Nothing Then
                'Remove any existing provider with the same type as clientRoleManagerType
                RemoveProvidersByType(GetRoleManagerProvidersNode(appConfigDocument), s_clientRoleManagerType)
                Dim nameValue As String = GetRoleManagerCreateDefaultProviderName(appConfigDocument)
                SetAttribute(appConfigDocument, roleManagerNode, s_defaultProvider, nameValue)
                SetAttribute(appConfigDocument, roleManagerNode, s_enabled, s_enabledDefault)
                defaultSettingNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    Dim providersNode As XmlNode = EnsureNode(appConfigDocument, s_providers, roleManagerNode)
                    Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, s_add)
                    SetAttribute(appConfigDocument, addNode, s_name, nameValue)
                    Dim defaultName As String = DefaultConnectionStringName(appConfigDocument, projectHierarchy)
                    SetAttributeIfNonNull(appConfigDocument, addNode, s_connectionStringName, defaultName)
                    SetAttribute(appConfigDocument, addNode, s_type, GetSupportedType(s_clientRoleManagerType, projectHierarchy).AssemblyQualifiedName)
                    SetAttribute(appConfigDocument, addNode, s_serviceUri, s_serviceUriDefault)
                    SetAttribute(appConfigDocument, addNode, s_cacheTimeout, s_cacheTimeoutDefault)
                    SetAttributeIfNonNull(appConfigDocument, addNode, s_honorCookieExpiry, DefaultHonorCookieExpiry(appConfigDocument, projectHierarchy))
                    providersNode.AppendChild(addNode)
                End If
            End If
        End Sub

        Private Shared Function GetRoleManagerCreateDefaultProviderName(ByVal doc As XmlDocument) As String
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetRoleManagerProvidersNode(doc), s_add)
            Return FindUniqueValueInList(addNodeList, s_roleManagerDefaultNameDefault, s_name)
        End Function

        Private Shared Function FindUniqueValueInList(ByVal nodeList As XmlNodeList, ByVal defaultName As String, ByVal attributeName As String) As String
            Dim count As Integer = 0
            Dim currentName As String = defaultName

            'If we can use the default name, just use that (don't append a number).  If we need to find another name, use
            '<defaultName>1, <defaultName>2, etc. until we find a name that's not being used.
            While GetXmlNodeWithValueFromList(nodeList, attributeName, currentName) IsNot Nothing
                count += 1
                currentName = defaultName & count.ToString(CultureInfo.InvariantCulture)
            End While

            Return currentName
        End Function

        Private Shared Function GetConnectionStringCreateDefaultProviderName(ByVal doc As XmlDocument) As String
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetConnectionStringsNode(doc), s_add)
            Return FindUniqueValueInList(addNodeList, s_connectionStringNameDefault, s_name)
        End Function

        Private Shared Sub EnsureDefaultRoleManagerNodeDoesntExist(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            Dim systemWebNode As XmlNode = GetSystemWebNode(appConfigDocument)
            Dim roleManagerNode As XmlNode = GetRoleManagerNode(appConfigDocument)
            Dim roleManagerProvidersNode As XmlNode = GetRoleManagerProvidersNode(appConfigDocument)

            'Remove roleManager's defaultProvider and associated data, including "providers" if it's now
            'empty, and roleManager if it's now empty.
            Dim defaultSettingNode As XmlNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            If defaultSettingNode IsNot Nothing Then
                RemoveAttribute(roleManagerNode, s_defaultProvider)
                'Remove the <add> for the default node
                RemoveNode(roleManagerProvidersNode, defaultSettingNode)
            End If

            RemoveChildIfItHasNoChildren(roleManagerNode, roleManagerProvidersNode)
            RemoveChildIfItHasNoChildren(systemWebNode, roleManagerNode)
            RemoveChildIfItHasNoChildren(GetConfigurationNode(appConfigDocument), systemWebNode)
        End Sub

        Private Shared Sub EnsureDefaultMembershipProvider(ByVal appConfigDocument As XmlDocument, ByVal enable As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureDefaultMembershipProviderNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureDefaultMembershipProviderNodeDoesntExist(appConfigDocument, projectHierarchy)
            End If
        End Sub

        Private Shared Sub EnsureDefaultMembershipProviderNodeExists(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<membership defaultProvider="DefaultFormAuthenticationProvider">
            '  <providers>
            '    <add name="DefaultFormAuthenticationProvider" type="System.Web.ClientServices.Providers.ClientFormsAuthenticationMembershipProvider, System.Web.ClientServices, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, processorArchitecture=MSIL"
            '              connectionStringName = "connSE"
            '              serviceUri = "http://localhost/testservices/rolesservice.svc"
            '              credentialsProvider = "AppServicesConsoleApp.UICredentialProvider,AppServicesConsoleApp"
            '              savePasswordHashLocally="false"
            '         />
            '  </providers>
            '</roleManager>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, s_configuration, appConfigDocument)
            Dim systemWebNode As XmlNode = EnsureNode(appConfigDocument, s_systemWeb, configurationNode)
            Dim membershipNode As XmlNode = EnsureNode(appConfigDocument, s_membership, systemWebNode)
            Dim defaultSettingNode As XmlNode

            Dim defaultProviderAttribute As XmlAttribute = membershipNode.Attributes(s_defaultProvider)

            'If we already have a default provider, make sure it's one of ours
            If defaultProviderAttribute IsNot Nothing Then
                defaultSettingNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    'We had a default, and it wasn't one of ours.  Remove the default attribute
                    RemoveAttribute(membershipNode, s_defaultProvider)
                    defaultProviderAttribute = Nothing
                End If
            End If

            If defaultProviderAttribute Is Nothing Then
                'Remove any existing provider with the same type as clientFormsMembershipProviderType and clientWindowsMembershipProviderType
                RemoveProvidersByType(GetMembershipProvidersNode(appConfigDocument), s_clientFormsMembershipProviderType)
                RemoveProvidersByType(GetMembershipProvidersNode(appConfigDocument), s_clientWindowsMembershipProviderType)
                Dim addNodeList As XmlNodeList = GetXmlNodeList(GetMembershipProvidersNode(appConfigDocument), s_add)
                Dim nameValue As String = FindUniqueValueInList(addNodeList, s_membershipDefaultNameDefault, s_name)
                SetAttribute(appConfigDocument, membershipNode, s_defaultProvider, nameValue)
                defaultSettingNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    Dim providersNode As XmlNode = EnsureNode(appConfigDocument, s_providers, membershipNode)
                    Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, s_add)
                    SetAttribute(appConfigDocument, addNode, s_name, nameValue)
                    SetAttribute(appConfigDocument, addNode, s_type, GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy).AssemblyQualifiedName)
                    SetAttributeIfNonNull(appConfigDocument, addNode, s_connectionStringName, DefaultConnectionStringName(appConfigDocument, projectHierarchy))
                    SetAttribute(appConfigDocument, addNode, s_serviceUri, s_serviceUriDefault)
                    providersNode.AppendChild(addNode)
                End If
            End If
        End Sub

        Private Shared Sub EnsureDefaultMembershipProviderNodeDoesntExist(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            Dim systemWebNode As XmlNode = GetSystemWebNode(appConfigDocument)
            Dim membershipNode As XmlNode = GetMembershipNode(appConfigDocument)
            Dim membershipProvidersNode As XmlNode = GetMembershipProvidersNode(appConfigDocument)

            'Remove membership's defaultProvider and associated data, including "providers" if it's now
            'empty, and membership if it's now empty.
            Dim defaultSettingNode As XmlNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
            If defaultSettingNode IsNot Nothing Then
                RemoveAttribute(membershipNode, s_defaultProvider)
                'Remove the <add> for the default node
                If membershipProvidersNode IsNot Nothing Then
                    membershipProvidersNode.RemoveChild(defaultSettingNode)
                End If
            End If

            RemoveChildIfItHasNoChildren(membershipNode, membershipProvidersNode)
            RemoveChildIfItHasNoChildren(systemWebNode, membershipNode)
            RemoveChildIfItHasNoChildren(GetConfigurationNode(appConfigDocument), systemWebNode)
        End Sub

        Private Shared Function DefaultConnectionStringName(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Dim node As XmlNode
            Dim returnValue As String

            node = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, s_connectionStringName)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, s_connectionStringName)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetAppSettingsConnectionStringNameNode(appConfigDocument)
            Return GetAttribute(node, s_value)
        End Function

        Private Shared Function DefaultHonorCookieExpiry(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Dim node As XmlNode
            Dim returnValue As String

            node = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, s_honorCookieExpiry)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetAppSettingsHonorCookieExpiryNode(appConfigDocument)
            Return GetAttribute(node, s_value)
        End Function

        Private Shared Sub EnsureAppSettings(ByVal appConfigDocument As XmlDocument, ByVal enable As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureAppSettingsNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureClientAppSettingsDontExist(appConfigDocument)
            End If
        End Sub

        Private Shared Sub EnsureAppSettingsNodeExists(ByVal appConfigDocument As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<appSettings>
            '  <add key=ClientSettingsProvider.ServiceUri" value=""
            '</appSettings>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, s_configuration, appConfigDocument)
            EnsureNode(appConfigDocument, s_appSettings, configurationNode)

            If GetAppSettingsServiceUriNode(appConfigDocument) Is Nothing Then
                AddAppConfigNode(appConfigDocument, AppSettingsName(s_serviceUri), s_serviceUriDefault)
            End If

            Dim node As XmlNode
            Dim currentConnectionStringName As String = DefaultConnectionStringName(appConfigDocument, projectHierarchy)
            If Not String.IsNullOrEmpty(currentConnectionStringName) Then
                node = GetAppSettingsConnectionStringNameNode(appConfigDocument)
                If node Is Nothing Then
                    AddAppConfigNode(appConfigDocument, AppSettingsName(s_connectionStringName), currentConnectionStringName)
                End If
            End If

            Dim honorCookieValue As String = DefaultHonorCookieExpiry(appConfigDocument, projectHierarchy)
            If honorCookieValue IsNot Nothing Then
                node = GetAppSettingsHonorCookieExpiryNode(appConfigDocument)
                If node Is Nothing Then
                    AddAppConfigNode(appConfigDocument, AppSettingsName(s_honorCookieExpiry), honorCookieValue)
                End If
            End If
        End Sub

        Private Shared Sub AddAppConfigNode(ByVal appConfigDocument As XmlDocument, ByVal keyValue As String, ByVal valueValue As String)
            Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, s_add)
            SetAttribute(appConfigDocument, addNode, s_key, keyValue)
            SetAttribute(appConfigDocument, addNode, s_value, valueValue)
            GetAppSettingsNode(appConfigDocument).AppendChild(addNode)
        End Sub

        Private Shared Sub EnsureClientAppSettingsDontExist(ByVal appConfigDocument As XmlDocument)
            Dim appSettingsNode As XmlNode = GetAppSettingsNode(appConfigDocument)
            If appSettingsNode Is Nothing Then Return

            RemoveNode(appSettingsNode, GetAppSettingsServiceUriNode(appConfigDocument))
            RemoveNode(appSettingsNode, GetAppSettingsConnectionStringNameNode(appConfigDocument))
            RemoveNode(appSettingsNode, GetAppSettingsHonorCookieExpiryNode(appConfigDocument))
            Dim configurationNode As XmlNode = GetConfigurationNode(appConfigDocument)
            If configurationNode Is Nothing Then Return
            RemoveChildIfItHasNoChildren(configurationNode, appSettingsNode)
        End Sub

        'Change the input string from "serviceUri" to "ClientSettingsProvider.ServiceUri"
        Private Shared Function AppSettingsName(ByVal inputName As String) As String
            Return String.Concat(s_clientSettingsProviderPrefix, Char.ToUpperInvariant(inputName(0)), inputName.SubString(1))
        End Function

        Private Shared Function EnsureNode(ByVal doc As XmlDocument, ByVal nodeName As String, ByVal parentNode As XmlNode) As XmlNode
            Dim newNode As XmlNode = GetXmlNode(parentNode, nodeName)
            If newNode Is Nothing Then
                newNode = CreateNode(doc, XmlNodeType.Element, nodeName)
                parentNode.AppendChild(newNode)
            End If

            Return newNode
        End Function

        Private Shared Function CreateNode(ByVal doc As XmlDocument, ByVal nodeType As XmlNodeType, ByVal nodeName As String) As XmlNode
            Return doc.CreateNode(nodeType, nodeName, String.Empty)
        End Function

        'Note: The child may have attributes and still be removed
        Private Shared Sub RemoveChildIfItHasNoChildren(ByVal parentNode As XmlNode, ByVal childNode As XmlNode)
            If parentNode Is Nothing OrElse childNode Is Nothing Then Return

            If String.IsNullOrEmpty(childNode.InnerXml) Then
                parentNode.RemoveChild(childNode)
            End If
        End Sub
#End Region

#Region "Helpers"
        Friend Shared Function IsClientRoleManagerProviderType(ByVal fullTypeName As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TypesMatch(fullTypeName, GetSupportedType(s_clientRoleManagerType, projectHierarchy))
        End Function

        Friend Shared Function IsClientMembershipProviderType(ByVal fullTypeName As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TypesMatch(fullTypeName, GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy)) OrElse _
            TypesMatch(fullTypeName, GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy))
        End Function

        Private Shared Function TypesMatch(ByVal typeNameToCheck As String, ByVal desiredType As Type) As Boolean
            Return typeNameToCheck IsNot Nothing AndAlso desiredType IsNot Nothing AndAlso typeNameToCheck.Equals(desiredType.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase)
        End Function

        Friend Shared Function GetServiceUri(ByVal node As XmlNode) As String
            Return GetAttribute(node, s_serviceUri)
        End Function

        Friend Shared Function AuthenticationServiceUrl(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetServiceUri(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy))
        End Function

        Friend Shared Function AuthenticationServiceHost(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetHostFromUrl(AuthenticationServiceUrl(doc, projectHierarchy))
        End Function

        Friend Shared Function RolesServiceHost(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Dim url As String = GetServiceUri(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy))
            Return GetHostFromUrl(url)
        End Function

        Friend Shared Function WebSettingsUrl(ByVal doc As XmlDocument) As String
            Return GetAttribute(GetAppSettingsServiceUriNode(doc), s_value)
        End Function

        Friend Shared Function WebSettingsHost(ByVal doc As XmlDocument) As String
            Return GetHostFromUrl(WebSettingsUrl(doc))
        End Function

        Private Shared Function GetHostFromUrl(ByVal url As String) As String
            If url Is Nothing Then Return Nothing
            url = url.Trim()
            If url = "" Then Return ""
            Dim separatorIndex As Integer = url.LastIndexOf("/")
            If separatorIndex = -1 Or Not url.ToUpperInvariant().EndsWith(".AXD") Then
                Throw New InvalidOperationException(SR.GetString(SR.PPG_Services_InvalidUrls))
            End If
            Return url.Substring(0, separatorIndex)
        End Function

        Friend Shared Function GetSavePasswordHashLocally(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TryGettingBooleanAttributeValue(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_savePasswordHashLocally, s_savePasswordHashLocallyDefault)
        End Function

        'Return value Nothing means the connection strings don't match
        Friend Shared Function GetEffectiveDefaultConnectionString(ByVal doc As XmlDocument, ByRef connectionStringSpecified As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Dim appSettingsConnectionStringNode As XmlNode = GetAppSettingsConnectionStringNameNode(doc)
            Dim appSettingsConnectionStringName As String = GetAttribute(appSettingsConnectionStringNode, s_value)

            Dim roleManagerProviderNode As XmlNode = GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy)
            Dim roleManagerConnectionStringName As String = GetAttribute(roleManagerProviderNode, s_connectionStringName)

            Dim membershipProviderNode As XmlNode = GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy)
            Dim membershipConnectionStringName As String = GetAttribute(membershipProviderNode, s_connectionStringName)

            'If no connection strings are specified, use the default
            If appSettingsConnectionStringName Is Nothing AndAlso _
                    roleManagerConnectionStringName Is Nothing AndAlso _
                    membershipConnectionStringName Is Nothing Then
                connectionStringSpecified = True
                Return Nothing
            End If

            If appSettingsConnectionStringName <> roleManagerConnectionStringName OrElse _
                    appSettingsConnectionStringName <> membershipConnectionStringName Then
                connectionStringSpecified = False
                Return Nothing
            End If

            'OK, the connection string names match: get the actual connection string.
            connectionStringSpecified = True
            Return GetConnectionString(doc, appSettingsConnectionStringName)
        End Function

        Friend Shared Function GetConnectionStringNode(ByVal doc As XmlDocument, ByVal whichString As String) As XmlNode
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetConnectionStringsNode(doc), s_add)
            Return GetXmlNodeWithValueFromList(addNodeList, s_name, whichString)
        End Function

        Friend Shared Function GetConnectionString(ByVal doc As XmlDocument, ByVal whichString As String) As String
            Return GetAttribute(GetConnectionStringNode(doc, whichString), s_connectionString)
        End Function

        Private Shared Sub SetConnectionString(ByVal doc As XmlDocument, ByVal whichString As String, ByVal newValue As String)
            Dim node As XmlNode = GetConnectionStringNode(doc, whichString)
            If newValue Is Nothing Then
                If node IsNot Nothing Then
                    Dim configurationNode As XmlNode = EnsureNode(doc, s_configuration, doc)
                    Dim connectionStringsNode As XmlNode = EnsureNode(doc, s_connectionStrings, configurationNode)
                    connectionStringsNode.RemoveChild(node)
                End If
            Else
                If node Is Nothing Then
                    Dim configurationNode As XmlNode = EnsureNode(doc, s_configuration, doc)
                    Dim connectionStringsNode As XmlNode = EnsureNode(doc, s_connectionStrings, configurationNode)
                    node = CreateNode(doc, XmlNodeType.Element, s_add)
                    connectionStringsNode.AppendChild(node)
                    SetAttribute(doc, node, s_name, whichString)
                End If
                SetAttribute(doc, node, s_connectionString, newValue)
            End If
        End Sub

        Friend Shared Function GetEffectiveHonorCookieExpiry(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Nullable(Of Boolean)
            Dim node As XmlNode
            Dim stringValue As String
            Dim roleManagerHonorCookieExpiry, appConfigHonorCookieExpiry As Boolean

            node = GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy)
            stringValue = GetAttribute(node, s_honorCookieExpiry)

            'If we didn't get a value, parse the default (string) value.
            If stringValue Is Nothing OrElse Not Boolean.TryParse(stringValue, roleManagerHonorCookieExpiry) Then
                roleManagerHonorCookieExpiry = Boolean.Parse(s_honorCookieExpiryDefault)
            End If

            node = GetAppSettingsHonorCookieExpiryNode(doc)
            stringValue = GetAttribute(node, s_value)

            'If we didn't get a value, parse the default (string) value.
            If stringValue Is Nothing OrElse Not Boolean.TryParse(stringValue, appConfigHonorCookieExpiry) Then
                appConfigHonorCookieExpiry = Boolean.Parse(s_honorCookieExpiryDefault)
            End If

            If roleManagerHonorCookieExpiry = appConfigHonorCookieExpiry Then
                Return appConfigHonorCookieExpiry
            End If
            Return Nothing
        End Function


        Friend Shared Function GetCacheTimeout(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Integer
            Return TryGettingIntegerAttributeValue(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_cacheTimeout, s_cacheTimeoutDefault)
        End Function

        Private Shared Function TryGettingBooleanAttributeValue(ByVal node As XmlNode, ByVal attributeName As String, ByVal defaultValue As String) As Boolean
            Dim stringValue As String = GetAttribute(node, attributeName)
            If stringValue Is Nothing Then stringValue = defaultValue
            Dim result As Boolean
            If Boolean.TryParse(stringValue, result) Then
                Return result
            Else
                Return Boolean.Parse(defaultValue)
            End If
        End Function

        Private Shared Function TryGettingIntegerAttributeValue(ByVal node As XmlNode, ByVal attributeName As String, ByVal defaultValue As String) As Integer
            Dim stringValue As String = GetAttribute(node, attributeName)
            If stringValue Is Nothing Then stringValue = defaultValue
            Dim result As Integer
            If Integer.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, result) Then
                Return result
            Else
                Return Integer.Parse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Shared Function CustomCredentialProviderType(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetAttribute(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_credentialsProvider)
        End Function

        Friend Shared Function WindowsAuthSelected(ByVal doc As XmlDocument, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return IsClientMembershipWindowsProviderNode(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), projectHierarchy)
        End Function

        'Return whether the value changed
        Friend Shared Function SetAuthenticationServiceUri(ByVal doc As XmlDocument, ByVal value As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_serviceUri, Normalize(value, AuthenticationSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetCustomCredentialProviderType(ByVal doc As XmlDocument, ByVal value As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_credentialsProvider, value)
        End Function

        'Return whether the value changed
        'We pass in whether we want windows auth (the alternative is form auth)
        Friend Shared Function SetMembershipDefaultProvider(ByVal doc As XmlDocument, ByVal changeToWindows As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Dim node As XmlNode = GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy)
            If node Is Nothing Then Return False

            Dim initialValue As Boolean = WindowsAuthSelected(doc, projectHierarchy)
            Dim typeName As String
            If changeToWindows Then
                typeName = GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy).AssemblyQualifiedName
            Else
                typeName = GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy).AssemblyQualifiedName
            End If
            SetAttribute(doc, node, s_type, typeName)
            If changeToWindows Then
                Dim connectionStringNameToUse As String = DefaultConnectionStringName(doc, projectHierarchy)
                If String.IsNullOrEmpty(connectionStringNameToUse) Then
                    connectionStringNameToUse = GetConnectionStringCreateDefaultProviderName(doc)
                End If
                If GetConnectionStringNode(doc, connectionStringNameToUse) Is Nothing Then
                    SetConnectionStringText(doc, connectionStringValueDefault, projectHierarchy)
                End If
                SetAttributeIfNull(doc, node, s_connectionStringName, connectionStringNameToUse)
                SetAttributeIfNull(doc, node, s_credentialsProvider, s_credentialsProviderDefault)
            End If
            Return Not String.Equals(changeToWindows, initialValue)
        End Function

        Friend Shared Sub SetConnectionStringText(ByVal doc As XmlDocument, ByVal newConnectionString As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            Dim appSettingsConnectionStringNameNode As XmlNode = GetAppSettingsConnectionStringNameNode(doc)
            Dim connStrName As String

            'Create the connection string node, if we have to.
            If appSettingsConnectionStringNameNode Is Nothing Then
                Dim configurationNode As XmlNode = EnsureNode(doc, s_configuration, doc)
                Dim appSettingsNode As XmlNode = EnsureNode(doc, s_appSettings, configurationNode)
                appSettingsConnectionStringNameNode = CreateNode(doc, XmlNodeType.Element, s_add)
                SetAttribute(doc, appSettingsConnectionStringNameNode, s_key, AppSettingsName(s_connectionStringName))
                connStrName = DefaultConnectionStringName(doc, projectHierarchy)
                If connStrName Is Nothing Then
                    connStrName = GetConnectionStringCreateDefaultProviderName(doc)
                End If

                SetAttribute(doc, appSettingsConnectionStringNameNode, s_value, connStrName)
                appSettingsNode.AppendChild(appSettingsConnectionStringNameNode)
            Else
                connStrName = GetAttribute(appSettingsConnectionStringNameNode, s_value)
            End If

            If newConnectionString Is Nothing Then
                RemoveAttribute(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_connectionStringName)
                RemoveAttribute(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_connectionStringName)
                EnsureAppSettingsNodeExists(doc, projectHierarchy)
                If Not appSettingsConnectionStringNameNode Is Nothing Then
                    GetAppSettingsNode(doc).RemoveChild(appSettingsConnectionStringNameNode)
                End If
            Else
                SetAttribute(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_connectionStringName, connStrName)
                SetAttribute(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_connectionStringName, connStrName)
            End If

            SetConnectionString(doc, connStrName, newConnectionString)
        End Sub

        'Return whether the value changed
        Friend Shared Function SetRoleServiceUri(ByVal doc As XmlDocument, ByVal inputValue As String, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_serviceUri, Normalize(inputValue, RolesSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetAppServicesServiceUri(ByVal doc As XmlDocument, ByVal inputValue As String) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetAppSettingsServiceUriNode(doc), s_value, Normalize(inputValue, ProfileSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetCacheTimeout(ByVal doc As XmlDocument, ByVal inputValue As Integer, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_cacheTimeout, inputValue)
        End Function

        Friend Shared Sub SetHonorCookieExpiry(ByVal doc As XmlDocument, ByVal inputValue As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing)
            Dim stringInputValue As String = inputValue.ToString(CultureInfo.InvariantCulture)
            SetAttribute(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), s_honorCookieExpiry, stringInputValue)
            Dim appSettingsNode As XmlNode = GetAppSettingsHonorCookieExpiryNode(doc)
            If appSettingsNode Is Nothing Then
                AddAppConfigNode(doc, AppSettingsName(s_honorCookieExpiry), stringInputValue)
            Else
                SetAttribute(doc, appSettingsNode, s_value, stringInputValue)
            End If
        End Sub

        'Return whether the value changed
        Friend Shared Function SetSavePasswordHashLocally(ByVal doc As XmlDocument, ByVal inputValue As Boolean, Optional ByVal projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), s_savePasswordHashLocally, inputValue)
        End Function

        Private Shared Function Normalize(ByVal val As String, ByVal suffix As String) As String
            If val Is Nothing Then Return Nothing
            val = val.Trim()
            If val = String.Empty Then Return String.Empty
            If val.EndsWith("/") Then
                return val & suffix
            End If
            return val & "/" & suffix
        End Function

        Friend Shared ReadOnly Property AuthenticationSuffix() As String
            Get
                Return GetSuffix("Authentication")
            End Get
        End Property

        Friend Shared ReadOnly Property RolesSuffix() As String
            Get
                Return GetSuffix("Role")
            End Get
        End Property

        Friend Shared ReadOnly Property ProfileSuffix() As String
            Get
                Return GetSuffix("Profile")
            End Get
        End Property

        Private Shared Function GetSuffix(ByVal input As String) As String
            return String.Format("{0}_JSON_AppService.axd", input)
        End Function

        Friend Shared Function ClientSettingsProviderName() As String
            Return GetType(System.Web.ClientServices.Providers.ClientSettingsProvider).FullName
        End Function

        Private Shared Function GetSupportedType(ByVal sourceType As Type, ByVal projectHierarchy As IVsHierarchy) As Type
            If projectHierarchy Is Nothing Then Return sourceType
            Dim mtSvc As MultiTargetService = New MultiTargetService(projectHierarchy, VSConstants.VSITEMID_ROOT, False)
            Dim supportedType As Type = mtSvc.GetSupportedType(sourceType, True)
            If supportedType Is Nothing Then
                Return sourceType
            Else
                Return supportedType
            End If
        End Function
#End Region
    End Class
End Namespace