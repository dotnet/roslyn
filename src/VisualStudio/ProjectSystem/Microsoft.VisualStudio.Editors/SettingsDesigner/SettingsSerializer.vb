'------------------------------------------------------------------------------
' <copyright from='1997' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Xml

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner


    ''' <summary>
    ''' Utility class to (de)serialize the contents of a DesignTimeSetting object 
    ''' given a stream reader/writer
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SettingsSerializer

        Friend Class SettingsSerializerException
            Inherits ApplicationException

            Public Sub New(ByVal message As String)
                MyBase.New(message)
            End Sub

            Public Sub New(ByVal message As String, ByVal inner As Exception)
                MyBase.New(message, inner)
            End Sub
        End Class

        Public Const SettingsSchemaUri As String = "http://schemas.microsoft.com/VisualStudio/2004/01/settings"
        Public Const SettingsSchemaUriOLD As String = "uri:settings"

        Public Const CultureInvariantVirtualTypeNameConnectionString As String = "(Connection string)"
        Public Const CultureInvariantVirtualTypeNameWebReference As String = "(Web Service URL)"

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
        ' We have disabled the schema validation for now - it caused perf problems for a couple of user scenarios
        ' (i.e. adding a database to an empty project, which in turn adds a connection string to the settings object)
        ' 
        ' Loading the schema added ~1s which was not acceptable. I have left the code in here in case we find another
        ' way to load it....
        '
        ' #define:ing USE_SETTINGS_XML_SCHEMA_VALIDATION will re-enable schema validation...
        Private Shared s_SchemaLoadFailed As Boolean = False

        ''' <summary>
        ''' Demand create an XML Schema instance for .settings files
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Shared ReadOnly Property Schema() As System.Xml.Schema.XmlSchema
            Get
                Static schemaInstance As System.Xml.Schema.XmlSchema
                If schemaInstance Is Nothing AndAlso Not s_SchemaLoadFailed Then
                    Dim SchemaStream As Stream
                    SchemaStream = GetType(SettingsSerializer).Assembly.GetManifestResourceStream(GetType(SettingsSerializer), "SettingsSchema")
                    schemaInstance = System.Xml.Schema.XmlSchema.Read(SchemaStream, AddressOf SchemaValidationEventHandler)
                End If
                Return schemaInstance
            End Get
        End Property

        ''' <summary>
        ''' If we fail to load the schema, things are bad indeed...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Shared Sub SchemaValidationEventHandler(ByVal sender As Object, ByVal e As System.Xml.Schema.ValidationEventArgs)
            System.Diagnostics.Debug.Fail("Failed to load XML schema from manifest resource stream!")
            s_SchemaLoadFailed = True
        End Sub

        ''' <summary>
        ''' Stores all validation errors from a ValidatingReader
        ''' </summary>
        ''' <remarks></remarks>
        Private Class ValidationErrorBag
            Private m_ValidationErrors As New System.Collections.ArrayList

            Friend ReadOnly Property Errors() As System.Collections.ICollection
                Get
                    Return m_ValidationErrors
                End Get
            End Property

            Friend Sub ValidationEventHandler(ByVal sender As Object, ByVal e As System.Xml.Schema.ValidationEventArgs)
                m_ValidationErrors.Add(e)
            End Sub
        End Class
#End If

        ''' <summary>
        '''  Deserialize XML stream of settings
        ''' </summary>
        ''' <param name="Settings">Instance to populate</param>
        ''' <param name="Reader">Text reader on stream containing serialized settings</param>
        ''' <remarks></remarks>
        Public Shared Sub Deserialize(ByVal Settings As DesignTimeSettings, ByVal Reader As TextReader, ByVal getRuntimeValue As Boolean)
            Dim XmlDoc As New XmlDocument()

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
            Dim ValidationErrors As New ValidationErrorBag
            If Schema IsNot Nothing Then
                Dim ValidatingReader As New XmlValidatingReader(New XmlTextReader(Reader))
                Dim SchemaCol As New System.Xml.Schema.XmlSchemaCollection()
                ValidatingReader.Schemas.Add(Schema)
                Try
                    AddHandler ValidatingReader.ValidationEventHandler, AddressOf ValidationErrors.ValidationEventHandler
                    XmlDoc.Load(ValidatingReader)
                Finally
                    RemoveHandler ValidatingReader.ValidationEventHandler, AddressOf ValidationErrors.ValidationEventHandler
                End Try
            Else
#End If
            ' CONSIDER, should I throw here to prevent the designer loader from blowing up / loading only part
            ' of the file and clobber it on the next write
            
            Dim xmlReader As System.Xml.XmlTextReader = New System.Xml.XmlTextReader(Reader)
            xmlReader.Normalization = False

            XmlDoc.Load(xmlReader)

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
            End If
            If ValidationErrors.Errors.Count > 0 Then
                Dim sb As New System.Text.StringBuilder
                For Each e As System.Xml.Schema.ValidationEventArgs In ValidationErrors.Errors
                    sb.AppendLine(e.Message)
                Next
                Throw New XmlException(sb.ToString())
            End If
#End If

            Dim XmlNamespaceManager As New XmlNamespaceManager(XmlDoc.NameTable)
            XmlNamespaceManager.AddNamespace("Settings", SettingsSchemaUri)

            Dim RootNode As XmlNode = XmlDoc.SelectSingleNode("Settings:SettingsFile", XmlNamespaceManager)

            ' Enable support of pre-Beta2 settings namespace files -- if we didn't find the root node
            '   using the new namespace, then try the old one
            '
            If (RootNode Is Nothing) Then

                XmlNamespaceManager.RemoveNamespace("Settings", SettingsSchemaUri)
                XmlNamespaceManager.AddNamespace("Settings", SettingsSchemaUriOLD)

                ' now that we have the old namespace set up, try selecting the root node again
                '
                RootNode = XmlDoc.SelectSingleNode("Settings:SettingsFile", XmlNamespaceManager)
            End If

            ' Deserialize setting group/description
            If RootNode IsNot Nothing Then
                ' Deserialize persisted namespace
                If RootNode.Attributes("GeneratedClassNamespace") IsNot Nothing Then
                    Settings.PersistedNamespace = RootNode.Attributes("GeneratedClassNamespace").Value
                End If

                ' In some cases, we want to use a specific class name and not base it on the name of the 
                ' .settings file...
                Dim mungeClassNameAttribute As XmlAttribute = RootNode.Attributes("UseMySettingsClassName")
                If mungeClassNameAttribute IsNot Nothing Then
                    Try
                        Settings.UseSpecialClassName = XmlConvert.ToBoolean(mungeClassNameAttribute.Value)
                    Catch ex As Exception When Not Common.IsUnrecoverable(ex)
                        Settings.UseSpecialClassName = False
                    End Try
                End If
            End If

            ' Deserialize settings
            Dim SettingNodes As XmlNodeList = XmlDoc.SelectNodes("Settings:SettingsFile/Settings:Settings/Settings:Setting", XmlNamespaceManager)

            For Each SettingNode As XmlNode In SettingNodes
                Dim typeAttr As XmlAttribute = SettingNode.Attributes("Type")
                Dim scopeAttr As XmlAttribute = SettingNode.Attributes("Scope")
                Dim nameAttr As XmlAttribute = SettingNode.Attributes("Name")
                Dim generateDefaultValueAttribute As XmlAttribute = SettingNode.Attributes("GenerateDefaultValueInCode")
                Dim descriptionAttr As XmlAttribute = SettingNode.Attributes("Description")
                Dim providerAttr As XmlAttribute = SettingNode.Attributes("Provider")
                Dim RoamingAttr As XmlAttribute = SettingNode.Attributes("Roaming")

                If typeAttr Is Nothing OrElse scopeAttr Is Nothing OrElse nameAttr Is Nothing Then
                    Throw New SettingsSerializer.SettingsSerializerException(SR.GetString(SR.SD_Err_CantLoadSettingsFile))
                End If

                Dim newSettingName As String = Settings.CreateUniqueName(nameAttr.Value)
                If Not Settings.IsValidName(newSettingName) Then
                    Throw New SettingsSerializer.SettingsSerializerException(SR.GetString(SR.SD_ERR_InvalidIdentifier_1Arg, nameAttr.Value))
                End If
                Dim Instance As DesignTimeSettingInstance = Settings.AddNew(typeAttr.Value, _
                                                                            newSettingName, _
                                                                            True)
                If scopeAttr.Value.Equals(SettingsDesigner.ApplicationScopeName, System.StringComparison.Ordinal) Then
                    Instance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                Else
                    Instance.SetScope(DesignTimeSettingInstance.SettingScope.User)
                End If

                If descriptionAttr IsNot Nothing Then
                    Instance.SetDescription(descriptionAttr.Value)
                End If

                If providerAttr IsNot Nothing Then
                    Instance.SetProvider(providerAttr.Value)
                End If

                If RoamingAttr IsNot Nothing Then
                    Instance.SetRoaming(XmlConvert.ToBoolean(RoamingAttr.Value))
                End If

                If generateDefaultValueAttribute IsNot Nothing AndAlso generateDefaultValueAttribute.Value <> "" AndAlso Not XmlConvert.ToBoolean(generateDefaultValueAttribute.Value) Then
                    Instance.SetGenerateDefaultValueInCode(False)
                Else
                    Instance.SetGenerateDefaultValueInCode(True)
                End If

                '
                ' Deserialize the value
                '
                Dim ValueNode As XmlNode = Nothing
                ' First, unless explicitly told to only get runtime values, 
                ' let's check if we have design-time specific values for this guy...
                If Not getRuntimeValue Then
                    ValueNode = SettingNode.SelectSingleNode("./Settings:DesignTimeValue[@Profile=""(Default)""]", XmlNamespaceManager)
                End If
                If ValueNode Is Nothing Then
                    ' ...and if we didn't find any design-time specific info, let's check the "normal" value
                    ' element
                    ValueNode = SettingNode.SelectSingleNode("./Settings:Value[@Profile=""(Default)""]", XmlNamespaceManager)
                End If
                If ValueNode IsNot Nothing Then
                    Instance.SetSerializedValue(ValueNode.InnerText)
                End If
            Next SettingNode
            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Deserialized {0} settings", Settings.Count)
        End Sub

        ''' <summary>
        '''  Serialize design time settings instance
        ''' </summary>
        ''' <param name="Settings">Instance to serialize</param>
        ''' <param name="Writer">Text writer on stream to serialize settings to</param>
        ''' <remarks></remarks>
        Public Shared Sub Serialize(ByVal Settings As DesignTimeSettings, ByVal GeneratedClassNameSpace As String, ByVal ClassName As String, ByVal Writer As TextWriter, ByVal DeclareEncodingAs As Text.Encoding)
            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Serializing {0} settings", Settings.Count)

            ' Gotta store the namespace here in case it changes from under us!
            Settings.PersistedNamespace = GeneratedClassNameSpace

            Dim SettingsWriter As New XmlTextWriter(Writer)
            SettingsWriter.Formatting = Formatting.Indented
            SettingsWriter.Indentation = 2
            ' NOTE, VsWhidbey 294747, Can I assume UTF-8 encoding? Nope, it seems that the DocDataTextWriter uses
            ' Unicode.Default! Probably should file a bug / change request for this... gotta make 100%
            ' sure what encoding is actually used first!
            ' 
            If DeclareEncodingAs Is Nothing Then
                DeclareEncodingAs = System.Text.Encoding.UTF8
            End If
            Dim EncodingString As String = "encoding='" & DeclareEncodingAs.BodyName & "'"

            SettingsWriter.WriteProcessingInstruction("xml", "version='1.0' " & EncodingString)
            SettingsWriter.WriteStartElement("SettingsFile")
            SettingsWriter.WriteAttributeString("xmlns", Nothing, SettingsSerializer.SettingsSchemaUri)
            SettingsWriter.WriteAttributeString("CurrentProfile", SettingsDesigner.CultureInvariantDefaultProfileName)
            If Settings.Count > 0 Then
                ' We only want to scribble this into the file if we actually have some settings to generate.
                ' The main purpose for this is to be able to clean up any default values that we may have persisted
                ' in the app.config file (which includes the generated namespace) If we don't save anything, we
                ' know we don't have anything to clean up!
                SettingsWriter.WriteAttributeString("GeneratedClassNamespace", GeneratedClassNameSpace)
                SettingsWriter.WriteAttributeString("GeneratedClassName", ClassName)
            End If

            If Settings.UseSpecialClassName Then
                ' Make sure we persist the fact that we are using a special naming convention for this class  
                SettingsWriter.WriteAttributeString("UseMySettingsClassName", XmlConvert.ToString(True))
            End If
            '
            ' Write (empty) Profiles element - Settings profiles were cut for Whidbey (VsWhidbey 483350)
            '
            SettingsWriter.WriteStartElement("Profiles")
            Dim valueSerializer As New SettingsValueSerializer()
            SettingsWriter.WriteEndElement() ' End of Profiles element

            SettingsWriter.WriteStartElement("Settings")
            For Each Instance As DesignTimeSettingInstance In Settings
                SettingsWriter.WriteStartElement("Setting")
                SettingsWriter.WriteAttributeString("Name", Instance.Name)
                If Instance.Description <> "" Then
                    SettingsWriter.WriteAttributeString("Description", Instance.Description)
                End If
                If Instance.Provider <> "" Then
                    SettingsWriter.WriteAttributeString("Provider", Instance.Provider)
                End If

                If Instance.Roaming Then
                    SettingsWriter.WriteAttributeString("Roaming", XmlConvert.ToString(Instance.Roaming))
                End If

                If Not Instance.GenerateDefaultValueInCode Then
                    SettingsWriter.WriteAttributeString("GenerateDefaultValueInCode", XmlConvert.ToString(False))
                End If

                SettingsWriter.WriteAttributeString("Type", Instance.SettingTypeName)
                SettingsWriter.WriteAttributeString("Scope", Instance.Scope.ToString())
                Dim designTimeValue As String = Nothing
                Dim defaultValue As String

                ' If this is a connection string, we have different values at design time and runtim.
                ' We serialize the design time value in the DesignTimeValue node, and add a Value node
                ' that contain the value that's going to be used at runtime...
                If String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                    designTimeValue = Instance.SerializedValue
                    Dim scs As Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString
                    scs = DirectCast(valueSerializer.Deserialize(GetType(Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString), _
                                                                designTimeValue, _
                                                                Globalization.CultureInfo.InvariantCulture),  _
                                    Microsoft.VSDesigner.VSDesignerPackage.SerializableConnectionString)
                    If scs IsNot Nothing AndAlso scs.ConnectionString IsNot Nothing Then
                        defaultValue = scs.ConnectionString
                    Else
                        defaultValue = ""
                    End If
                Else
                    defaultValue = Instance.SerializedValue
                End If

                ' If we did find a design-time specific value, we better write it out...
                If designTimeValue IsNot Nothing Then
                    SettingsWriter.WriteStartElement("DesignTimeValue")
                    SettingsWriter.WriteAttributeString("Profile", SettingsDesigner.CultureInvariantDefaultProfileName)
                    SettingsWriter.WriteString(designTimeValue)
                    SettingsWriter.WriteEndElement() ' End of DesignTimeValue element
                End If
                ' And we should always have a "normal" value as well...
                SettingsWriter.WriteStartElement("Value")
                SettingsWriter.WriteAttributeString("Profile", SettingsDesigner.CultureInvariantDefaultProfileName)
                SettingsWriter.WriteString(defaultValue)
                SettingsWriter.WriteEndElement() ' End of Value element

                SettingsWriter.WriteEndElement() ' End of Setting element
            Next
            SettingsWriter.WriteEndElement() ' End of Settings element
            SettingsWriter.WriteEndElement() ' End of SettingsFile element
            SettingsWriter.Flush()
            SettingsWriter.Close()
        End Sub
    End Class
End Namespace
