'------------------------------------------------------------------------------
' <copyright company='Microsoft Corporation'>
'    Copyright (c) Microsoft Corporation. All Rights Reserved.
'    Information Contained Herein is Proprietary and Confidential.
' </copyright>
'------------------------------------------------------------------------------
'

Imports System
Imports System.Diagnostics
Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.IO

Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VSDesigner.VSDesignerPackage
Imports Microsoft.VSDesigner.Common
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    Public Class SettingsSingleFileGeneratorBase
        Implements IVsSingleFileGenerator, IObjectWithSite, System.IServiceProvider, IVsRefactorNotify

        Private m_Site As Object
        Private m_CodeDomProvider As CodeDomProvider
        Private m_ServiceProvider As ServiceProvider


        Private Const AddedHandlerFieldName As String = "addedHandler"
        Private Const AddedHandlerLockObjectFieldName As String = "addedHandlerLockObject"
        Private Const AutoSaveSubName As String = "AutoSaveSettings"
        Friend Const DefaultInstanceFieldName As String = "defaultInstance"
        Friend Const DefaultInstancePropertyName As String = "Default"

        Friend Const MyNamespaceName As String = "My"
        Private Const MySettingsModuleName As String = "MySettingsProperty"
        Private Const MySettingsPropertyName As String = "Settings"

        Private Const MyTypeWinFormsDefineConstant_If As String = "#If _MyType = ""WindowsForms"" Then"
        Private Const MyTypeWinFormsDefineConstant_EndIf As String = "#End If"

        Private Const HideAutoSaveRegionBegin As String = "#Region ""{0}"""
        Private Const HideAutoSaveRegionEnd As String = "#End Region"

        Private Const DocCommentSummaryStart As String = "<summary>"
        Private Const DocCommentSummaryEnd As String = "</summary>"

        Friend Const DesignerGeneratedFileSuffix As String = ".Designer"


        ''' <summary>
        ''' If set to true, tells the shell that symbolic renames are OK. 
        ''' </summary>
        ''' <remarks>
        ''' Normally, we can't handle symbolic renames since we don't update the contents of the .settings
        ''' file (which means that we overwrite the changes the next time the file is generated. 
        ''' In the special case where the designer invokes the symbolic rename, we should allow it.
        ''' 
        ''' Since all the file generation should happen on the main thread, it is OK to have this member shared...
        ''' </remarks>
        Friend Shared AllowSymbolRename As Boolean = False

        ''' <summary>
        ''' Returns the default visibility of this properties
        ''' </summary>
        ''' <value></value>
        ''' <remarks>MemberAttributes indicating what visibility to make the generated properties.</remarks>
        Friend Shared ReadOnly Property SettingsPropertyVisibility() As MemberAttributes
            Get
                Return MemberAttributes.Public Or MemberAttributes.Final
            End Get
        End Property

        '@ <summary>
        '@ Returns the default visibility of this properties
        '@ </summary>
        '@ <value>MemberAttributes indicating what visibility to make the generated properties.</value>
        Friend Overridable ReadOnly Property SettingsClassVisibility() As System.Reflection.TypeAttributes
            Get
                Return TypeAttributes.Sealed Or TypeAttributes.NestedAssembly
            End Get
        End Property

        ''' <summary>
        ''' Allow derived classes to modify the generated settings class and/or compile unit
        ''' </summary>
        ''' <param name="compileUnit">The full compile unit that we are to generate code from</param>
        ''' <param name="generatedClass">The generated settings class</param>
        ''' <remarks></remarks>
        Protected Overridable Sub OnCompileUnitCreated(ByVal compileUnit As CodeCompileUnit, ByVal generatedClass As CodeTypeDeclaration)
            ' By default, we don't want to make any modifications...
        End Sub

#Region "IVsSingleFileGenerator implementation"
        '@ <summary>
        '@ Get the default extension for the generated class.
        '@ </summary>
        '@ <param name="pbstrDefaultExtension"></param>
        '@ <remarks></remarks>
        Private Function DefaultExtension(ByRef pbstrDefaultExtension As String) As Integer Implements Shell.Interop.IVsSingleFileGenerator.DefaultExtension
            If CodeDomProvider IsNot Nothing Then
                ' For some reason some the code providers seem to be inconsistent in the way that they 
                ' return the extension - some have a leading "." and some do not...
                If CodeDomProvider.FileExtension.StartsWith(".") Then
                    pbstrDefaultExtension = DesignerGeneratedFileSuffix & CodeDomProvider.FileExtension
                Else
                    pbstrDefaultExtension = DesignerGeneratedFileSuffix & "." & CodeDomProvider.FileExtension
                End If
            Else
                Debug.Fail("We failed to get a CodeDom provider - defaulting file extension to 'Designer.vb'")
                pbstrDefaultExtension = DesignerGeneratedFileSuffix & ".vb"
            End If
        End Function

        '@ <summary>
        '@ Generate a strongly typed wrapper for the contents of the setting path
        '@ </summary>
        '@ <param name="wszInputFilePath"></param>
        '@ <param name="bstrInputFileContents"></param>
        '@ <param name="wszDefaultNamespace"></param>
        '@ <param name="rgbOutputFileContents"></param>
        '@ <param name="pcbOutput"></param>
        '@ <param name="pGenerateProgress"></param>
        '@ <remarks></remarks>
        Private Function Generate(ByVal wszInputFilePath As String, ByVal bstrInputFileContents As String, ByVal wszDefaultNamespace As String, ByVal rgbOutputFileContents() As System.IntPtr, ByRef pcbOutput As UInteger, ByVal pGenerateProgress As Shell.Interop.IVsGeneratorProgress) As Integer Implements Shell.Interop.IVsSingleFileGenerator.Generate


            Dim BufPtr As IntPtr = IntPtr.Zero
            Try
                ' get the DesignTimeSettings from the file content
                '
                Dim Settings As DesignTimeSettings = DeserializeSettings(bstrInputFileContents, pGenerateProgress)

                ' Add appropriate references to the project
                '
                AddRequiredReferences(pGenerateProgress)

                ' We have special handling for VB
                '
                Dim isVB As Boolean = CodeDomProvider.FileExtension.Equals("vb", StringComparison.Ordinal)

                ' And even more special handling for the default VB file...
                '
                Dim shouldGenerateMyStuff As Boolean = (isVB AndAlso IsDefaultSettingsFile(wszInputFilePath))

                Dim typeAttrs As TypeAttributes

                If CodeDomProvider.FileExtension.Equals(".jsl", StringComparison.OrdinalIgnoreCase) Then
                    ' VsWhidbey 302842, J# doesn't have assembly-only scoped types.... gotta generate the type
                    ' as Public for now - hopefully we'll have a better solution post beta1
                    typeAttrs = TypeAttributes.Public Or TypeAttributes.Sealed
                Else
                    typeAttrs = SettingsClassVisibility
                End If

                ' for VB, we need to generate some code that is fully-qualified, but our generator is always invoked
                '   without the project's root namespace due to VB convention. If this is VB, then we need to look
                '   up the project's root namespace and pass that in to Create in order to be able to generate the
                '   appropriate code.
                '
                Dim projectRootNamespace As String = String.Empty
                If (isVB) Then
                    projectRootNamespace = GetProjectRootNamespace()
                End If

                ' then get the CodeCompileUnit for this .settings file
                '
                Dim generatedClass As CodeTypeDeclaration = Nothing
                Dim CompileUnit As CodeCompileUnit = Create(DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy), _
                                                            Settings, _
                                                            wszDefaultNamespace, _
                                                            wszInputFilePath, _
                                                            False, _
                                                            pGenerateProgress, _
                                                            typeAttrs, _
                                                            CodeDomProvider.Supports(GeneratorSupport.TryCatchStatements), _
                                                            shouldGenerateMyStuff, _
                                                            projectRootNamespace, _
                                                            generatedClass)

                ' For VB, we need to add Option Strict ON, Option Explicit ON plus check whether or not we
                '   should add the My module
                '
                If isVB Then

                    CompileUnit.UserData("AllowLateBound") = False
                    CompileUnit.UserData("RequireVariableDeclaration") = True

                    ' If this is the "default" settings file, we add the "My" module as well...
                    '
                    If shouldGenerateMyStuff Then
                        AddMyModule(CompileUnit, projectRootNamespace, wszDefaultNamespace)
                    End If
                End If

                OnCompileUnitCreated(CompileUnit, generatedClass)

                Try
                    CodeGenerator.ValidateIdentifiers(CompileUnit)
                Catch argEx As ArgumentException
                    ' We have an invalid identifier here...
                    If pGenerateProgress IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0, 1, SR.GetString(SR.SingleFileGenerator_FailedToGenerateFile_1Arg, argEx.Message), 0, 0))
                        Return NativeMethods.E_FAIL
                    Else
                        Throw
                    End If
                End Try

                ' Let's start writing to a stream...
                Dim OutputStream As New MemoryStream
                Dim OutputWriter As New StreamWriter(OutputStream, System.Text.Encoding.UTF8)
                CodeDomProvider.GenerateCodeFromCompileUnit(CompileUnit, OutputWriter, New CodeGeneratorOptions())
                OutputWriter.Flush()

                Dim BufLen As Integer = CInt(OutputStream.Length)

                BufPtr = Marshal.AllocCoTaskMem(BufLen)
                Marshal.Copy(OutputStream.ToArray(), 0, BufPtr, BufLen)
                rgbOutputFileContents(0) = BufPtr
                pcbOutput = CUInt(BufLen)

                OutputWriter.Close()
                OutputStream.Close()

                If pGenerateProgress IsNot Nothing Then
                    ' We are done!
                    VSErrorHandler.ThrowOnFailure(pGenerateProgress.Progress(100, 100))
                End If
                BufPtr = IntPtr.Zero
                Return NativeMethods.S_OK
            Catch e As Exception
                If pGenerateProgress IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0, 1, SR.GetString(SR.SingleFileGenerator_FailedToGenerateFile_1Arg, e.Message), 0, 0))
                End If
            Finally
                If Not BufPtr.Equals(IntPtr.Zero) Then
                    Marshal.FreeCoTaskMem(BufPtr)
                End If
            End Try
            Return NativeMethods.E_FAIL
        End Function

        ''' <summary>
        ''' Creates the CodeCompileUnit for the given DesignTimeSettings using the given file-path to determine the class name.
        ''' </summary>
        ''' <param name="Hierarchy">Hierarchy that contains the settings file</param>
        ''' <param name="Settings">DesignTimeSettings class to generate a CodeCompileUnit from</param>
        ''' <param name="DefaultNamespace">namespace to generate code within</param>
        ''' <param name="FilePath">path to the file this Settings object is (used to create the class name)</param>
        ''' <param name="IsDesignTime">flag to tell whether we are generating for design-time consumers like SettingsGlobalObjectProvider users or not.</param>
        ''' <param name="GenerateProgress">optional reporting mechanism</param>
        ''' <param name="GeneratedClassVisibility"></param>
        ''' <param name="GeneratorSupportsTryCatch">Does the CodeDom generator support try/catch statements?</param>
        ''' <param name="GenerateVBMyAutoSave"></param>
        ''' <returns>CodeCompileUnit of the given DesignTimeSettings object</returns>
        ''' <remarks></remarks>
        Friend Shared Function Create(ByVal Hierarchy As IVsHierarchy,
                                      ByVal Settings As DesignTimeSettings,
                                      ByVal DefaultNamespace As String,
                                      ByVal FilePath As String,
                                      ByVal IsDesignTime As Boolean,
                                      ByVal GenerateProgress As Shell.Interop.IVsGeneratorProgress,
                                      ByVal GeneratedClassVisibility As TypeAttributes,
                                      Optional ByVal GeneratorSupportsTryCatch As Boolean = True,
                                      Optional ByVal GenerateVBMyAutoSave As Boolean = False,
                                      Optional ByVal ProjectRootNamespace As String = "",
                                      <Out()> Optional ByRef generatedType As CodeTypeDeclaration = Nothing) As CodeCompileUnit

            Dim CompileUnit As New CodeCompileUnit

            ' make sure the compile-unit references System to get the base-class definition
            '
            CompileUnit.ReferencedAssemblies.Add("System")

            ' Create a new namespace to put our class in
            '
            Dim ns As New CodeNamespace(DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(DefaultNamespace))
            CompileUnit.Namespaces.Add(ns)

            ' Create the strongly typed settings class
            ' VsWhidbey 234144, Make sure this is a valid class name
            '
            generatedType = New CodeTypeDeclaration(SettingsDesigner.GeneratedClassName(Hierarchy, VSITEMID.NIL, Settings, FilePath))

            ' pick up the default visibility
            '
            generatedType.TypeAttributes = GeneratedClassVisibility

            ' Set the base class
            '
            generatedType.BaseTypes.Add(CreateGlobalCodeTypeReference(SettingsBaseClass))

            ' This is the "main" partial class - there may be others that expand this class
            ' that contain user code...
            '
            generatedType.IsPartial = True

            ' add the CompilerGeneratedAttribute in order to support deploying VB apps in Yukon (where
            '   our shared/static fields make the code not "safe" according to Yukon's measure of what
            '   it means to deploy a safe assembly. VSWhidbey 320692.
            '
            Dim CompilerGeneratedAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
            generatedType.CustomAttributes.Add(CompilerGeneratedAttribute)

            ' Tell FXCop that we are compiler generated stuff...
            Static toolName As String = GetType(SettingsSingleFileGenerator).FullName
            Static toolVersion As String = GetType(SettingsSingleFileGenerator).Assembly.GetName().Version.ToString()
            Dim GeneratedCodeAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.CodeDom.Compiler.GeneratedCodeAttribute)),
                                                                       New CodeAttributeArgument() {New CodeAttributeArgument(New CodePrimitiveExpression(toolName)),
                                                                                                     New CodeAttributeArgument(New CodePrimitiveExpression(toolVersion))})
            generatedType.CustomAttributes.Add(GeneratedCodeAttribute)

            ' add the shared getter that fetches the default instance
            '
            AddDefaultInstance(generatedType, GenerateProgress, GeneratorSupportsTryCatch, GenerateVBMyAutoSave)

            ' and then add each setting as a property
            '

            ' We don't really care about the current language since we only want to translate the virtual type names
            ' into .NET FX type names...
            Dim typeNameResolver As New SettingTypeNameResolutionService("")
            For Each Instance As DesignTimeSettingInstance In Settings
                generatedType.Members.Add(CodeDomPropertyFromSettingInstance(typeNameResolver, Instance, IsDesignTime, GenerateProgress))
            Next

            ' Add our class to the namespace...
            '
            ns.Types.Add(generatedType)

            Return CompileUnit

        End Function

        ''' <summary>
        ''' Deserialize contents of XML input string into a DesignTimeSettings object
        ''' </summary>
        ''' <param name="InputString"></param>
        ''' <param name="GenerateProgress"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function DeserializeSettings(ByVal InputString As String, ByVal GenerateProgress As Shell.Interop.IVsGeneratorProgress) As DesignTimeSettings
            Dim Settings As New DesignTimeSettings()
            If InputString <> "" Then
                ' We actually have some contents to deserialize.... 
                Dim SettingsReader As New StringReader(InputString)
                Try
                    SettingsSerializer.Deserialize(Settings, SettingsReader, True)
                Catch ex As Xml.XmlException
                    If GenerateProgress IsNot Nothing Then
                        GenerateProgress.GeneratorError(0, 1, ex.Message, &HFFFFFFFFUL, &HFFFFFFFFUL)
                    Else
                        Throw
                    End If
                End Try
            End If
            Return Settings
        End Function

        ''' <summary>
        ''' Generate a CodeDomProperty to be the shared accessor
        ''' </summary>
        ''' <param name="GeneratedType"></param>
        ''' <param name="SupportsTryCatch"></param>
        ''' <param name="GenerateVBMyAutoSave"></param>
        ''' <remarks></remarks>
        Private Shared Sub AddDefaultInstance(ByVal GeneratedType As CodeTypeDeclaration, ByVal GenerateProgress As Shell.Interop.IVsGeneratorProgress, Optional ByVal SupportsTryCatch As Boolean = True, Optional ByVal GenerateVBMyAutoSave As Boolean = False)

            ' type-reference that both the default-instance field and the property will be
            '
            Dim SettingsClassTypeReference As New CodeTypeReference(GeneratedType.Name)

            ' Emit default instance field.
            '
            '     Private Shared defaultInstance As Settings = CType(Global.System.Configuration.ApplicationSettingsBase.Synchronized(New Settings),Settings)
            '
            Dim Field As New CodeMemberField(SettingsClassTypeReference, DefaultInstanceFieldName)
            Dim NewInstanceExpression As New CodeObjectCreateExpression(SettingsClassTypeReference)
            Dim SynchronizedExpression As New CodeMethodInvokeExpression(New CodeTypeReferenceExpression(New CodeTypeReference(SettingsBaseClass, CodeTypeReferenceOptions.GlobalReference)), _
                                                                         "Synchronized", _
                                                                         New CodeExpression() {NewInstanceExpression})
            Dim InitExpression As New CodeCastExpression(GeneratedType.Name, SynchronizedExpression)

            Field.Attributes = MemberAttributes.Private Or MemberAttributes.Static
            Field.InitExpression = InitExpression

            GeneratedType.Members.Add(Field)

            ' Emit the property that returns the default-instance field
            '
            '   Public Shared ReadOnly Property [Default]() As Settings
            '
            Dim CodeProperty As New CodeMemberProperty
            CodeProperty.Attributes = MemberAttributes.Public Or MemberAttributes.Static
            CodeProperty.Name = DefaultInstancePropertyName
            CodeProperty.Type = SettingsClassTypeReference
            CodeProperty.HasGet = True
            CodeProperty.HasSet = False

            ' We should hook up the My.Application.Shutdown event if told to auto save the 
            ' settings (only applicable for the main settings file & only applicable for VB)
            '
            If GenerateVBMyAutoSave Then
                ' if we need to generate the My.Settings module + AutoSave functionality, we should mark the class itself
                '   as advanced so it doesn't clutter IntelliSense because users will access this class via My.Settings, not
                '   Settings.Default
                '
                '   <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
                '   Class <settings-name-goes-here>
                '
                Dim browsableStateTypeReference As CodeTypeReference = CreateGlobalCodeTypeReference(GetType(System.ComponentModel.EditorBrowsableState))
                Dim browsableAttributeTypeReference As CodeTypeReference = CreateGlobalCodeTypeReference(GetType(System.ComponentModel.EditorBrowsableAttribute))

                Dim browsableAdvancedFieldReference As New CodeFieldReferenceExpression(New CodeTypeReferenceExpression(browsableStateTypeReference), "Advanced")
                Dim parameters() As CodeAttributeArgument = {New CodeAttributeArgument(browsableAdvancedFieldReference)}

                Dim browsableAdvancedAttribute As New CodeAttributeDeclaration(browsableAttributeTypeReference, parameters)

                GeneratedType.CustomAttributes.Add(browsableAdvancedAttribute)

                ' Add the AddHandler call that hooks My.Application.Shutdown inside the default-instance getter
                '
                Dim AutoSaveSnippet As New CodeSnippetExpression()

                AutoSaveSnippet.Value = _
                    Environment.NewLine & _
                    MyTypeWinFormsDefineConstant_If & Environment.NewLine & _
                    "               If Not " & AddedHandlerFieldName & " Then" & Environment.NewLine & _
                    "                    SyncLock " & AddedHandlerLockObjectFieldName & Environment.NewLine & _
                    "                        If Not " & AddedHandlerFieldName & " Then" & Environment.NewLine & _
                    "                            AddHandler My.Application.Shutdown, AddressOf " & AutoSaveSubName & Environment.NewLine & _
                    "                            " & AddedHandlerFieldName & " = True" & Environment.NewLine & _
                    "                        End If" & Environment.NewLine & _
                    "                    End SyncLock" & Environment.NewLine & _
                    "                End If" & Environment.NewLine & _
                    MyTypeWinFormsDefineConstant_EndIf

                CodeProperty.GetStatements.Add(AutoSaveSnippet)
            End If

            ' Emit return line
            '
            '   Return defaultInstance
            '
            Dim ValueReference As New CodeFieldReferenceExpression()
            ValueReference.FieldName = DefaultInstanceFieldName
            CodeProperty.GetStatements.Add(New CodeMethodReturnStatement(ValueReference))

            ' And last, add the property to the class we're generating
            '
            GeneratedType.Members.Add(CodeProperty)
        End Sub

        '@ <summary>
        '@ Given a setting instance, generate a CodeDomProperty
        '@ </summary>
        '@ <param name="Instance"></param>
        '@ <param name="GenerateProgress"></param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Shared Function CodeDomPropertyFromSettingInstance(ByVal TypeNameResolver As SettingTypeNameResolutionService, ByVal Instance As DesignTimeSettingInstance, ByVal IsDesignTime As Boolean, ByVal GenerateProgress As Shell.Interop.IVsGeneratorProgress) As CodeMemberProperty
            Dim CodeProperty As New CodeMemberProperty
            CodeProperty.Attributes = SettingsPropertyVisibility
            CodeProperty.Name = Instance.Name
            Dim fxTypeName As String = TypeNameResolver.PersistedSettingTypeNameToFxTypeName(Instance.SettingTypeName)

            CodeProperty.Type = New CodeTypeReference(fxTypeName)
            CodeProperty.Type.Options = CodeTypeReferenceOptions.GlobalReference

            CodeProperty.HasGet = True
            CodeProperty.GetStatements.AddRange(GenerateGetterStatements(Instance, CodeProperty.Type))

            ' At runtime, we currently only generate setters for User scoped settings.
            ' At designtime, however, consumers of the global settings class may have to set application
            ' scoped settings (i.e. connection strings, settings bound to properties on user controls and so on)
            If IsDesignTime OrElse Instance.Scope <> DesignTimeSettingInstance.SettingScope.Application Then
                CodeProperty.HasSet = True
                CodeProperty.SetStatements.AddRange(GenerateSetterStatements(Instance))
            End If

            ' Make sure we have a CustomAttributes collection for this guy!
            CodeProperty.CustomAttributes = New CodeAttributeDeclarationCollection
            ' Add scope attribute
            Dim ScopeAttribute As CodeAttributeDeclaration
            If Instance.Scope = DesignTimeSettingInstance.SettingScope.User Then
                ScopeAttribute = New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.UserScopedSettingAttribute)))
            Else
                ScopeAttribute = New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.ApplicationScopedSettingAttribute)))
            End If
            CodeProperty.CustomAttributes.Add(ScopeAttribute)

            If Instance.Provider <> "" Then
                Dim attr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.SettingsProviderAttribute)))
                attr.Arguments.Add(New CodeAttributeArgument(New CodeTypeOfExpression(Instance.Provider)))
                CodeProperty.CustomAttributes.Add(attr)
            End If

            If Instance.Description <> "" Then
                Dim attr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.SettingsDescriptionAttribute)))
                attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpression(Instance.Description)))
                CodeProperty.CustomAttributes.Add(attr)

                CodeProperty.Comments.Add(New CodeCommentStatement(DocCommentSummaryStart, True))
                CodeProperty.Comments.Add(New CodeCommentStatement(System.Security.SecurityElement.Escape(Instance.Description), True))
                CodeProperty.Comments.Add(New CodeCommentStatement(DocCommentSummaryEnd, True))
            End If

            ' Add DebuggerNonUserCode attribute
            CodeProperty.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Diagnostics.DebuggerNonUserCodeAttribute))))

            If String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                ' Add connection string attribute if this is a connection string...
                Dim SpecialSettingRefExp As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(System.Configuration.SpecialSetting)))
                Dim FieldExp As New CodeFieldReferenceExpression(SpecialSettingRefExp, System.Configuration.SpecialSetting.ConnectionString.ToString())
                Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
                Dim ConnectionStringAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.SpecialSettingAttribute)), Parameters)
                CodeProperty.CustomAttributes.Add(ConnectionStringAttribute)
            ElseIf String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                ' Add web reference attribute if this is a web reference...
                Dim SpecialSettingRefExp As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(System.Configuration.SpecialSetting)))
                Dim FieldExp As New CodeFieldReferenceExpression(SpecialSettingRefExp, System.Configuration.SpecialSetting.WebServiceUrl.ToString())
                Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
                Dim WebReferenceAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.SpecialSettingAttribute)), Parameters)
                CodeProperty.CustomAttributes.Add(WebReferenceAttribute)
            End If

            If Instance.GenerateDefaultValueInCode AndAlso (Instance.SerializedValue <> "" OrElse String.Equals(Instance.SettingTypeName, GetType(String).FullName, StringComparison.Ordinal)) Then
                ' Only add default value attributes for settings that actually have a value (Special-casing strings - 
                ' treat an empty serialized value as an empty string...)
                Debug.Assert(Instance.SerializedValue IsNot Nothing, "Why do we have a NULL serialized value!?")
                AddDefaultValueAttribute(CodeProperty, Instance.SerializedValue)
            End If

            ' Add SettingsManageabilityAttribute if this setting is roaming (but only if this is a USER scoped setting...
            If Instance.Roaming AndAlso Instance.Scope = DesignTimeSettingInstance.SettingScope.User Then
                AddManagebilityAttribue(CodeProperty, Configuration.SettingsManageability.Roaming)
            End If

            Return CodeProperty
        End Function

        Private Shared Sub AddDefaultValueAttribute(ByVal CodeProperty As CodeMemberProperty, ByVal Value As String)
            ' Add default value attribute
            Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(New CodePrimitiveExpression(Value))}
            Dim DefaultValueAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.DefaultSettingValueAttribute)), Parameters)
            CodeProperty.CustomAttributes.Add(DefaultValueAttribute)
        End Sub

        Private Shared Sub AddManagebilityAttribue(ByVal CodeProperty As CodeMemberProperty, ByVal Value As System.Configuration.SettingsManageability)
            Dim SettingsManageability As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(System.Configuration.SettingsManageability)))
            Dim FieldExp As New CodeFieldReferenceExpression(SettingsManageability, Value.ToString)
            Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
            Dim SettingsManageabilityAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Configuration.SettingsManageabilityAttribute)), Parameters)
            CodeProperty.CustomAttributes.Add(SettingsManageabilityAttribute)
        End Sub

        '@ <summary>
        '@ Get the type of the class that our strongly typed wrapper class is supposed to inherit from
        '@ </summary>
        '@ <value></value>
        '@ <remarks></remarks>
        Friend Shared ReadOnly Property SettingsBaseClass() As Type
            Get
                Return GetType(System.Configuration.ApplicationSettingsBase)
            End Get
        End Property

        '@ <summary>
        '@ Generate CodeDomStatements to get a setting from our base class
        '@ </summary>
        '@ <param name="Instance"></param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Shared Function GenerateGetterStatements(ByVal Instance As DesignTimeSettingInstance, ByVal SettingType As CodeTypeReference) As CodeStatementCollection
            Dim Statements As New CodeStatementCollection
            Dim Parameters() As CodeExpression = {New CodePrimitiveExpression(Instance.Name)}
            Dim IndexerStatement As New CodeIndexerExpression(New CodeThisReferenceExpression(), Parameters)
            ' Make sure we case this value to the correct type

            Dim TypeConversionStatement As New CodeCastExpression(SettingType, IndexerStatement)
            Dim ReturnStatement As New CodeMethodReturnStatement(TypeConversionStatement)

            Statements.Add(ReturnStatement)

            Return Statements
        End Function

        '@ <summary>
        '@ Generate statements to set a settings value
        '@ </summary>
        '@ <param name="Instance"></param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Shared Function GenerateSetterStatements(ByVal Instance As DesignTimeSettingInstance) As CodeStatementCollection
            Dim Statements As New CodeStatementCollection
            Dim Parameters() As CodeExpression = {New CodePrimitiveExpression(Instance.Name)}
            Dim IndexerStatement As New CodeIndexerExpression(New CodeThisReferenceExpression(), Parameters)
            ' Make sure we case this value to the correct type
            Dim AssignmentStatment As New CodeAssignStatement(IndexerStatement, New CodePropertySetValueReferenceExpression)

            Statements.Add(AssignmentStatment)
            Return Statements
        End Function

        ''' <summary>
        ''' Creates a string representation of the full-type name give the project's root-namespace, the default namespace
        ''' into which we are generating, and the name of the class
        ''' </summary>
        ''' <param name="projectRootNamespace">project's root namespace (may be String.Empty)</param>
        ''' <param name="defaultNamespace">namespace into which we are generating (may be String.Empty)</param>
        ''' <param name="typeName">the type of the settings-class we are generating</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GetFullTypeName(ByVal projectRootNamespace As String, ByVal defaultNamespace As String, ByVal typeName As String) As String

            Dim fullTypeName As String = String.Empty

            If (projectRootNamespace <> "") Then
                fullTypeName = projectRootNamespace & "."
            End If

            If (defaultNamespace <> "") Then
                fullTypeName &= defaultNamespace & "."
            End If

            Debug.Assert(typeName <> "", "we shouldn't have an empty type-name when generating a Settings class")
            fullTypeName &= typeName

            Return fullTypeName

        End Function

        ''' <summary>
        ''' Generates a SuppressMessageAttribute for the given memberName
        ''' </summary>
        ''' <param name="memberName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function GenerateSuppressMessageAttribute(ByVal memberName As String) As CodeAttributeDeclaration

            Dim suppressMessageAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Diagnostics.CodeAnalysis.SuppressMessageAttribute)))

            Dim categoryArgument As New CodeAttributeArgument(New CodePrimitiveExpression("Microsoft.Performance"))
            Dim checkIdArgument As New CodeAttributeArgument(New CodePrimitiveExpression("CA1811:AvoidUncalledPrivateCode"))
            Dim scopeArgument As New CodeAttributeArgument("Scope", New CodePrimitiveExpression("member"))
            Dim targetArgument As New CodeAttributeArgument("Target", New CodePrimitiveExpression(memberName))

            suppressMessageAttribute.Arguments.AddRange(New CodeAttributeArgument() {categoryArgument, checkIdArgument, scopeArgument, targetArgument})

            Return suppressMessageAttribute

        End Function

        '@ <summary>
        '@ Add required references to the project - currently only adding a reference to the settings base class assembly
        '@ </summary>
        '@ <param name="GenerateProgress"></param>
        '@ <remarks></remarks>
        Protected Overridable Sub AddRequiredReferences(ByVal GenerateProgress As Shell.Interop.IVsGeneratorProgress)
            Dim CurrentProjectItem As EnvDTE.ProjectItem = CType(GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
            If CurrentProjectItem Is Nothing Then
                Debug.Fail("Failed to get EnvDTE.ProjectItem service")
                Return
            End If

            Dim CurrentProject As VSLangProj.VSProject = CType(CurrentProjectItem.ContainingProject.Object, VSLangProj.VSProject)
            If CurrentProject Is Nothing Then
                Debug.Fail("Failed to get containing project")
                Return
            End If

            CurrentProject.References.Add(SettingsBaseClass.Assembly.GetName().Name)
        End Sub

        ''' <summary>
        ''' Adds the Module that lives in the My namespace and proffers up a Settings property which implements
        ''' My.Settings for easy access to typed-settings.
        ''' </summary>
        ''' <param name="Unit"></param>
        ''' <remarks></remarks>
        Private Shared Sub AddMyModule(ByVal Unit As CodeCompileUnit, ByVal projectRootNamespace As String, ByVal defaultNamespace As String)

            Debug.Assert(Unit IsNot Nothing AndAlso Unit.Namespaces.Count = 1 AndAlso Unit.Namespaces(0).Types.Count = 1, "Expected a compile unit with a single namespace containing a single type!")

            Dim GeneratedType As CodeTypeDeclaration = Unit.Namespaces(0).Types(0)

            ' Create a field to capture whether or not we've already done the AddHandler. We can't use a
            '   CodeMemberField to output this b/c that doesn't have a way of doing #If/#End If around
            '   the field declaration. Since adding My goo is VB-specific, we don't really need to bother too
            '   much about being language-agnostic.
            '
            ' #If _MyType = "WindowsForms" Then
            '    Private Shared addedHandler As Boolean
            ' #End If
            '
            Dim AutoSaveCode As New CodeSnippetTypeMember()
            AutoSaveCode.Text = _
                String.Format(HideAutoSaveRegionBegin, SR.GetString(SR.SD_SFG_AutoSaveRegionText)) & Environment.NewLine & _
                MyTypeWinFormsDefineConstant_If & Environment.NewLine & _
                "    Private Shared " & AddedHandlerFieldName & " As Boolean" & Environment.NewLine & _
                Environment.NewLine & _
                "    Private Shared " & AddedHandlerLockObjectFieldName & " As New Object" & Environment.NewLine & _
                Environment.NewLine & _
                "    <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _" & Environment.NewLine & _
                "    Private Shared Sub " & AutoSaveSubName & "(ByVal sender As Global.System.Object, ByVal e As Global.System.EventArgs)" & Environment.NewLine & _
                "        If My.Application.SaveMySettingsOnExit Then" & Environment.NewLine & _
                "            " & MyNamespaceName & "." & MySettingsPropertyName & ".Save()" & Environment.NewLine & _
                "        End If" & Environment.NewLine & _
                "    End Sub" & Environment.NewLine & _
                MyTypeWinFormsDefineConstant_EndIf & Environment.NewLine & _
                HideAutoSaveRegionEnd

            GeneratedType.Members.Add(AutoSaveCode)

            ' Create a namespace named My
            '
            Dim MyNamespace As New CodeNamespace(MyNamespaceName)

            ' Create a property named Settings
            '
            Dim SettingProperty As New CodeMemberProperty
            SettingProperty.Name = MySettingsPropertyName
            SettingProperty.HasGet = True
            SettingProperty.HasSet = False

            Dim fullTypeReference As CodeTypeReference = New CodeTypeReference(GetFullTypeName(projectRootNamespace, defaultNamespace, GeneratedType.Name))
            fullTypeReference.Options = CodeTypeReferenceOptions.GlobalReference
            SettingProperty.Type = fullTypeReference
            SettingProperty.Attributes = MemberAttributes.Assembly Or MemberAttributes.Final

            'TODO: Once CodeDom supports putting attributes on the individual Getter and Setter, we should
            '   mark this property as DebuggerNonUserCode. In Whidbey, CodeDom doesn't offer a way to put
            '   attributes on the getter and setter. It only offers attributes on the property itself which
            '   doesn't work for the debugger.
            '
            'SettingProperty.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Diagnostics.DebuggerNonUserCodeAttribute))))

            ' Also add the help keyword attribute
            '
            Dim helpKeywordAttr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.ComponentModel.Design.HelpKeywordAttribute)))
            helpKeywordAttr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpression(HelpIDs.MySettingsHelpKeyword)))
            SettingProperty.CustomAttributes.Add(helpKeywordAttr)

            Dim MethodInvokeExpr As New CodeMethodInvokeExpression(New CodeTypeReferenceExpression(SettingProperty.Type), DefaultInstancePropertyName, New CodeExpression() {})
            SettingProperty.GetStatements.Add(New CodeMethodReturnStatement(MethodInvokeExpr))

            ' Create a Module
            '
            '   <Global.Microsoft.VisualBasic.HideModuleNameAttribute(),  _
            '    Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), _
            '    Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
            '   Module MySettingsProperty
            '        
            Dim ModuleDecl As New CodeTypeDeclaration(MySettingsModuleName)
            ModuleDecl.UserData("Module") = True
            ModuleDecl.TypeAttributes = TypeAttributes.Sealed Or TypeAttributes.NestedAssembly
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Microsoft.VisualBasic.HideModuleNameAttribute))))
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Diagnostics.DebuggerNonUserCodeAttribute))))
            ' add the CompilerGeneratedAttribute in order to support deploying VB apps in Yukon (where
            '   our shared/static fields make the code not "safe" according to Yukon's measure of what
            '   it means to deploy a safe assembly. VSWhidbey 320692.
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute))))

            ModuleDecl.Members.Add(SettingProperty)

            ' add the Module to the My namespace
            '
            MyNamespace.Types.Add(ModuleDecl)

            ' Add the My namespace to the CodeCompileUnit
            '
            Unit.Namespaces.Add(MyNamespace)
        End Sub
#End Region

        ''' <summary>
        ''' Gets the default-namespace for the project containing the .settings file for which
        ''' we are currently generating. This will include the root-namespace for VB even though
        ''' we would not have been passed in that namespace in the call to Generate.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetProjectRootNamespace() As String

            Dim rootNamespace As String = String.Empty

            Try
                Dim punkVsBrowseObject As IntPtr
                Dim vsBrowseObjectGuid As Guid = GetType(IVsBrowseObject).GUID

                ' first, we need to get IVsBrowseObject from our site
                '
                GetSite(vsBrowseObjectGuid, punkVsBrowseObject)

                Try
                    If (punkVsBrowseObject <> IntPtr.Zero) Then

                        Dim vsBrowseObject As IVsBrowseObject = TryCast(Marshal.GetObjectForIUnknown(punkVsBrowseObject), IVsBrowseObject)
                        Debug.Assert(vsBrowseObject IsNot Nothing, "Generator invoked by Site that is not IVsBrowseObject?")

                        If (vsBrowseObject IsNot Nothing) Then
                            Dim vsHierarchy As IVsHierarchy = Nothing
                            Dim itemid As UInteger = 0

                            ' use the IVsBrowseObject to get the hierarchy/itemid for the .settings file
                            '   from which are generating
                            '
                            VSErrorHandler.ThrowOnFailure(vsBrowseObject.GetProjectItem(vsHierarchy, itemid))

                            Debug.Assert(vsHierarchy IsNot Nothing, "GetProjectItem should have thrown or returned a valid IVsHierarchy")
                            Debug.Assert(itemid <> VSITEMID.NIL, "GetProjectItem should have thrown or returned a valid VSITEMID")

                            If ((vsHierarchy IsNot Nothing) AndAlso (itemid <> VSITEMID.NIL)) Then

                                Dim obj As Object = Nothing

                                ' get the default-namespace of the root node which will be the project's root namespace
                                '
                                VSErrorHandler.ThrowOnFailure(vsHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_DefaultNamespace, obj))

                                Debug.Assert(TypeOf obj Is String, "DefaultNamespace didn't return a string?")
                                If (TypeOf obj Is String) Then

                                    ' now we finally have the default-namespace
                                    '
                                    rootNamespace = CType(obj, String)
                                End If
                            End If
                        End If
                    End If
                Finally
                    If (punkVsBrowseObject <> IntPtr.Zero) Then
                        Marshal.Release(punkVsBrowseObject)
                    End If
                End Try
            Catch ex As Exception
                Debug.WriteLine(ex.ToString())
                Debug.Fail("Why did we fail to get the DefaultNamespace?" & Microsoft.VisualBasic.vbCrLf & ex.ToString())
            End Try

            Return rootNamespace
        End Function

        '@ <summary>
        '@ Is this the "default" settings file
        '@ </summary>
        '@ <param name="FilePath">Fully qualified path of file to check</param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function IsDefaultSettingsFile(ByVal FilePath As String) As Boolean
            Dim Hierarchy As IVsHierarchy = DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy)
            If Hierarchy Is Nothing Then
                Debug.Fail("Failed to get Hierarchy for file to generate code from...")
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return False
            End If

            Dim DefaultSettingsItemId As UInteger
            Dim DefaultSettingsFilePath As String = Nothing

            Dim hr As Integer = SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), DefaultSettingsItemId, DefaultSettingsFilePath)
            If NativeMethods.Succeeded(hr) Then
                If DefaultSettingsItemId <> VSITEMID.NIL Then
                    Dim NormalizedDefaultSettingFilePath As String = Path.GetFullPath(DefaultSettingsFilePath)
                    Dim NormalizedSettingFilePath As String = Path.GetFullPath(FilePath)
                    Return String.Equals(NormalizedDefaultSettingFilePath, NormalizedSettingFilePath, StringComparison.Ordinal)
                End If
            Else
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End If
            Return False
        End Function

        '@ <summary>
        '@ Demand-create a CodeDomProvider corresponding to my projects current language
        '@ </summary>
        '@ <value>A CodeDomProvider</value>
        '@ <remarks></remarks>
        Private Property CodeDomProvider() As CodeDomProvider
            Get
                If m_CodeDomProvider Is Nothing Then
                    Dim VSMDCodeDomProvider As IVSMDCodeDomProvider = CType(GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If VSMDCodeDomProvider IsNot Nothing Then
                        m_CodeDomProvider = CType(VSMDCodeDomProvider.CodeDomProvider, CodeDomProvider)
                    End If
                    Debug.Assert(m_CodeDomProvider IsNot Nothing, "Get CodeDomProvider Interface failed.  GetService(QueryService(CodeDomProvider) returned Null.")
                End If
                Return m_CodeDomProvider
            End Get
            Set(ByVal Value As CodeDomProvider)
                If Value Is Nothing Then
                    Throw New ArgumentNullException()
                End If
                m_CodeDomProvider = Value
            End Set
        End Property

        '@ <summary>
        '@ Demand-create service provider from my site
        '@ </summary>
        '@ <value></value>
        '@ <remarks></remarks>
        Private ReadOnly Property ServiceProvider() As ServiceProvider
            Get
                If m_ServiceProvider Is Nothing AndAlso m_Site IsNot Nothing Then
                    Dim OleSp As OLE.Interop.IServiceProvider = CType(m_Site, OLE.Interop.IServiceProvider)
                    m_ServiceProvider = New ServiceProvider(OleSp)
                End If
                Return m_ServiceProvider
            End Get
        End Property

        ''' <summary>
        ''' Create a CodeTypeReference instance with the GlobalReference option set.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function CreateGlobalCodeTypeReference(ByVal type As Type) As CodeTypeReference
            Dim ctr As New CodeTypeReference(type)
            ctr.Options = CodeTypeReferenceOptions.GlobalReference
            Return ctr
        End Function

#Region "IObjectWithSite implementation"
        Private Sub GetSite(ByRef riid As System.Guid, ByRef ppvSite As System.IntPtr) Implements OLE.Interop.IObjectWithSite.GetSite
            If m_Site Is Nothing Then
                ' Throw E_FAIL
                Throw New Win32Exception(NativeMethods.E_FAIL)
            End If

            Dim pUnknownPointer As IntPtr = Marshal.GetIUnknownForObject(m_Site)
            Try
                Marshal.QueryInterface(pUnknownPointer, riid, ppvSite)

                If ppvSite = IntPtr.Zero Then
                    ' throw E_NOINTERFACE
                    Throw New Win32Exception(NativeMethods.E_NOINTERFACE)
                End If
            Finally
                If (pUnknownPointer <> IntPtr.Zero) Then
                    Marshal.Release(pUnknownPointer)
                    pUnknownPointer = IntPtr.Zero
                End If
            End Try
        End Sub

        Private Sub SetSite(ByVal pUnkSite As Object) Implements OLE.Interop.IObjectWithSite.SetSite
            m_Site = pUnkSite
            ClearCachedServices()
        End Sub

        Private Sub ClearCachedServices()
            m_ServiceProvider = Nothing
            m_CodeDomProvider = Nothing
        End Sub
#End Region


#Region "IVsRefactorNotify Implementation"
        ' ******************* Implement IVsRefactorNotify *****************

        '@ <summary>
        '@ Called when a symbol is about to be renamed
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        '@ <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        '@ <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnBeforeGlobalSymbolRenamed(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal cRQNames As UInteger, ByVal rglpszRQName() As String, ByVal lpszNewName As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeGlobalSymbolRenamed
            prgAdditionalCheckoutVSITEMIDS = Nothing

            Dim isRootNamespaceRename As Boolean = RenamingHelper.IsRootNamespaceRename(phier, cRQNames, rglpszRQName, lpszNewName)

            If AllowSymbolRename Or isRootNamespaceRename Then
                If isRootNamespaceRename Then
                    ' We need to tell all settings global object to update the default namespace as well... 
                    ' if we don't do this, they will have the old namespace cached and anyone who asks for a 
                    ' virtual type will get a type with a bogus namespace...
                    Dim sp As Microsoft.VisualStudio.Shell.ServiceProvider = Common.ServiceProviderFromHierarchy(phier)
                    Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(phier)
                    Dim objectService As Shell.Design.GlobalObjectService = New Shell.Design.GlobalObjectService(sp, proj, GetType(System.ComponentModel.Design.Serialization.CodeDomSerializer))
                    If objectService IsNot Nothing Then
                        Dim objectCollection As Shell.Design.GlobalObjectCollection = objectService.GetGlobalObjects(GetType(System.Configuration.ApplicationSettingsBase))
                        If Not objectCollection Is Nothing Then
                            ' Note: We are currently calling refresh on all settings global objects for each
                            '   refactor notify, which effectively makes this an O(n^2) operation where n is the
                            '   number of .settings files in the project. We are OK with this because:
                            '   a) We don't expect users to have too many .settings files
                            '   b) Once we have retreived the settings global object, it will be cached
                            '      and the retreival is just a hash table lookup.
                            '   c) The Refresh is a cheap operation
                            For Each gob As Shell.Design.GlobalObject In objectCollection
                                Dim sgob As SettingsGlobalObjects.SettingsFileGlobalObject = TryCast(gob, SettingsGlobalObjects.SettingsFileGlobalObject)
                                If sgob IsNot Nothing Then
                                    sgob.Refresh()
                                End If
                            Next
                        End If
                    End If
                End If
                Return NativeMethods.S_OK
            Else
                Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_RenameNotSupported))
                ' Always return an error code to disable renaming of generated code
                Return NativeMethods.E_NOTIMPL
            End If
        End Function

        '@ <summary>
        '@ Called when a method is about to have its params reordered
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        '@ <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        '@ <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        '@ <returns>error code</returns>
        Private Function OnGlobalSymbolRenamed(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal cRQNames As UInteger, ByVal rglpszRQName() As String, ByVal lpszNewName As String) As Integer Implements IVsRefactorNotify.OnGlobalSymbolRenamed
            'VSWhidbey #452759: Always return S_OK in OnGlobalSymbolRenamed.
            Return NativeMethods.S_OK
        End Function

        '@ <summary>
        '@ Called when a method is about to have params added
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        '@ <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        '@ <param name="rgszParamIndexes">the indexes of the new parameters</param>
        '@ <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        '@ <param name="rgszParamNames">the names of the parameters</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnBeforeAddParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParams As UInteger, ByVal rgszParamIndexes() As UInteger, ByVal rgszRQTypeNames() As String, ByVal rgszParamNames() As String, ByRef prgAdditionalCheckoutVSITEMIDS As System.Array) As Integer Implements IVsRefactorNotify.OnBeforeAddParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        '@ <summary>
        '@ Called after a method has had params added
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        '@ <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        '@ <param name="rgszParamIndexes">the indexes of the new parameters</param>
        '@ <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        '@ <param name="rgszParamNames">the names of the parameters</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnAddParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParams As UInteger, ByVal rgszParamIndexes() As UInteger, ByVal rgszRQTypeNames() As String, ByVal rgszParamNames() As String) As Integer Implements IVsRefactorNotify.OnAddParams
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        '@ <summary>
        '@ Called when a method is about to have its params reordered
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        '@ <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        '@ <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnBeforeReorderParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParamIndexes As UInteger, ByVal rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeReorderParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        '@ <summary>
        '@ Called after a method has had its params reordered
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        '@ <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        '@ <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        '@ <returns>error code</returns>
        Private Function OnReorderParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParamIndexes As UInteger, ByVal rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnReorderParams
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        '@ <summary>
        '@ Called when a method is about to have some params removed
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        '@ <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        '@ <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnBeforeRemoveParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParamIndexes As UInteger, ByVal rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeRemoveParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        '@ <summary>
        '@ Called when a method is about to have some params removed
        '@ </summary>
        '@ <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        '@ <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        '@ <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        '@ <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        '@ <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        '@ <returns>error code</returns>
        Private Function OnRemoveParams(ByVal phier As IVsHierarchy, ByVal itemId As UInteger, ByVal lpszRQName As String, ByVal cParamIndexes As UInteger, ByVal rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnRemoveParams
            Common.Utils.SetErrorInfo(Common.Utils.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, SR.GetString(SR.SD_ERR_ModifyParamsNotSupported))
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

#End Region


#Region "IServiceProvider"

        '@ <summary>
        '@ I'm capable of providing services
        '@ </summary>
        '@ <param name="serviceType">The type of service requested</param>
        '@ <returns>An instance of the service, or nothing if service not found</returns>
        '@ <remarks></remarks>
        Private Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            If ServiceProvider IsNot Nothing Then
                Return ServiceProvider.GetService(serviceType)
            Else
                Return Nothing
            End If
        End Function
#End Region


    End Class
End Namespace
