Imports System

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Map between display names (the name that appear in the UI), the type name persisted in the .settings file
    ''' and the corresponding .NET FX type name.
    ''' 
    ''' There are three sets of names:
    ''' 1) Display names. This is the name that appears in the settings designer UI. This includes the language specific 
    '''    type names (i.e. String instead of System.String) and the localized name for the "virtual" types (i.e. connection string)
    ''' 2) Names as they are persisted in the .settings file. This is usually the same as the .NET FX type name for the setting
    '''    except in the case of connection strings and web service URLs, which use the culture invariant representation for these
    '''    types
    ''' 3) The .NET FX type name. This is what we present to the single file generator and the settings global object.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class SettingTypeNameResolutionService

        Private Enum Language
            UNKNOWN = -1
            CSharp = 0
            VB = 1
            JSharp = 2
        End Enum
#Region "Private fields"

        ' Map from language specific names to the corresponding .NET FX type name
        Private m_LanguageSpecificToFxTypeName As System.Collections.Generic.Dictionary(Of String, String)

        ' Map from .NET FX type names to language specific type names
        Private m_FxTypeNameToLanguageSpecific As System.Collections.Generic.Dictionary(Of String, String)

        ' Is the current language case-sensitive?
        Private m_caseSensitive As Boolean

#End Region

        Public Sub New(ByVal languageGuid As System.String, Optional ByVal caseSensitive As Boolean = True)
            Dim language As Language
            Select Case languageGuid
                Case EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp
                    language = language.CSharp
                Case EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB
                    language = language.VB
                Case EnvDTE80.CodeModelLanguageConstants2.vsCMLanguageJSharp
                    language = language.JSharp
                Case Else
                    language = language.UNKNOWN
            End Select

            m_caseSensitive = caseSensitive

            Dim comparer As System.Collections.Generic.IEqualityComparer(Of String)
            If caseSensitive Then
                comparer = System.StringComparer.Ordinal
            Else
                comparer = System.StringComparer.OrdinalIgnoreCase
            End If

            m_LanguageSpecificToFxTypeName = New System.Collections.Generic.Dictionary(Of String, String)(16, comparer)
            m_FxTypeNameToLanguageSpecific = New System.Collections.Generic.Dictionary(Of String, String)(16, comparer)
            If language <> language.UNKNOWN Then
                ' add language specific type names for C#, VB, J# respectively
                AddEntry((GetType(Boolean).FullName), New String() {"bool", "Boolean", "boolean"}(language))
                AddEntry((GetType(Byte).FullName), New String() {"byte", "Byte", "byte"}(language))
                AddEntry((GetType(Char).FullName), New String() {"char", "Char", "char"}(language))
                AddEntry((GetType(Decimal).FullName), New String() {"decimal", "Decimal", Nothing}(language))
                AddEntry((GetType(Double).FullName), New String() {"double", "Double", "double"}(language))
                AddEntry((GetType(Short).FullName), New String() {"short", "Short", "short"}(language))
                AddEntry((GetType(Integer).FullName), New String() {"int", "Integer", "int"}(language))
                AddEntry((GetType(Long).FullName), New String() {"long", "Long", "long"}(language))
                AddEntry((GetType(SByte).FullName), New String() {"sbyte", "SByte", Nothing}(language))
                AddEntry((GetType(Single).FullName), New String() {"float", "Single", "float"}(language))
                AddEntry((GetType(UShort).FullName), New String() {"ushort", "UShort", Nothing}(language))
                AddEntry((GetType(UInteger).FullName), New String() {"uint", "UInteger", Nothing}(language))
                AddEntry((GetType(ULong).FullName), New String() {"ulong", "ULong", Nothing}(language))
                AddEntry((GetType(String).FullName), New String() {"string", "String", "String"}(language))
                AddEntry((GetType(System.DateTime).FullName), New String() {Nothing, "Date", Nothing}(language))
            End If
        End Sub

        ''' <summary>
        ''' Is the current language case sensitive?
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property IsCaseSensitive() As Boolean
            Get
                Return m_caseSensitive
            End Get
        End Property
        ''' <summary>
        ''' Given the text persisted in the .settings file, return the string that we'll 
        ''' show in the UI
        ''' </summary>
        ''' <param name="typeName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function PersistedSettingTypeNameToTypeDisplayName(ByVal typeName As String) As String
            Dim displayName As String = Nothing
            If String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return DisplayTypeNameConnectionString
            ElseIf String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                Return DisplayTypeNameWebReference
            ElseIf m_FxTypeNameToLanguageSpecific.TryGetValue(typeName, displayName) Then
                Return displayName
            End If
            Return typeName
        End Function

        ''' <summary>
        ''' Given the string we persisted in the .settings file, return the .NET FX type name
        ''' that we'll use when building the CodeDom tree
        ''' </summary>
        ''' <param name="typeName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function PersistedSettingTypeNameToFxTypeName(ByVal typeName As String) As String
            If String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return GetType(String).FullName
            ElseIf String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                Return GetType(String).FullName
            Else
                Return typeName
            End If
        End Function

        ''' <summary>
        ''' Given the text showing in the UI, return the string that we'll actually persist in the
        ''' .settings file
        ''' </summary>
        ''' <param name="typeName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function TypeDisplayNameToPersistedSettingTypeName(ByVal typeName As String) As String
            Dim persistedTypeName As String = Nothing
            If String.Equals(typeName, DisplayTypeNameConnectionString, StringComparison.Ordinal) Then
                Return SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString
            ElseIf String.Equals(typeName, DisplayTypeNameWebReference, StringComparison.Ordinal) Then
                Return SettingsSerializer.CultureInvariantVirtualTypeNameWebReference
            ElseIf m_LanguageSpecificToFxTypeName.TryGetValue(typeName, persistedTypeName) Then
                Return persistedTypeName
            Else
                Return typeName
            End If
        End Function

#Region "Localized name of virtual types"
        Private Shared ReadOnly Property DisplayTypeNameConnectionString() As String
            Get
                Static VirtualTypeNameConnectionString As String = "(" & SR.GetString(SR.SD_ComboBoxItem_ConnectionStringType) & ")"
                Return VirtualTypeNameConnectionString
            End Get
        End Property

        Private Shared ReadOnly Property DisplayTypeNameWebReference() As String
            Get
                Static VirtualTypeNameWebReference As String = "(" & SR.GetString(SR.SD_ComboBoxItem_WebReferenceType) & ")"
                Return VirtualTypeNameWebReference
            End Get
        End Property
#End Region

#Region "Private implementation details"

        Private Sub AddEntry(ByVal FxName As String, ByVal languageSpecificName As String)
            If languageSpecificName <> "" Then
                m_LanguageSpecificToFxTypeName(languageSpecificName) = FxName
                m_FxTypeNameToLanguageSpecific(FxName) = languageSpecificName
            End If
        End Sub
#End Region

    End Class
End Namespace