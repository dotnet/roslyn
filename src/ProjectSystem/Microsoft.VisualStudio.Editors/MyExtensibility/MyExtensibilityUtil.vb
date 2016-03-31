Option Strict On
Option Explicit On
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.CustomMarshalers
Imports System.Xml
Imports EnvDTE
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensibilityUtil
    ''' <summary>
    ''' Common utility methods for MyExtensibility.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class MyExtensibilityUtil

        ''' ;StringEquals
        ''' <summary>
        ''' Perform OrdinalIgnoreCase string comparison.
        ''' </summary>
        Public Shared Function StringEquals(ByVal s1 As String, ByVal s2 As String) As Boolean
            Return String.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' ;StringIsNullEmptyOrBlank
        ''' <summary>
        ''' Check if the given string is null, empty or all blank spaces.
        ''' </summary>
        Public Shared Function StringIsNullEmptyOrBlank(ByVal s As String) As Boolean
            Return String.IsNullOrEmpty(s) OrElse s.Trim().Length = 0
        End Function

        ''' ;GetAttributeValue
        ''' <summary>
        ''' Get the trimed attribute with the given name from the given xml element.
        ''' Return Nothing if such attributes don't exist.
        ''' </summary>
        Public Shared Function GetAttributeValue(ByVal xmlElement As XmlElement, ByVal attributeName As String) _
                As String
            Dim xmlAttribute As XmlAttribute = xmlElement.Attributes(attributeName)

            If xmlAttribute IsNot Nothing AndAlso xmlAttribute.Value IsNot Nothing Then
                Return xmlAttribute.Value.Trim()
            End If

            Return Nothing
        End Function

        ''' ;GerVersion
        ''' <summary>
        ''' Construct a Version instance from the given string, return Version(0.0.0.0)
        ''' if the string format is incorrect.
        ''' </summary>
        Public Shared Function GetVersion(ByVal versionString As String) As Version
            Dim result As New Version(0, 0, 0, 0)
            If Not String.IsNullOrEmpty(versionString) Then
                Try
                    result = New Version(versionString)
                Catch ex As ArgumentException ' Ignore exceptions from version constructor.
                Catch ex As FormatException
                Catch ex As OverflowException
                End Try
            End If
            Return result
        End Function

        ''' ;NormalizeAssemblyFullName
        ''' <summary>
        ''' Given an assembly full name, return a full name containing only name and version.
        ''' </summary>
        Public Shared Function NormalizeAssemblyFullName(ByVal assemblyFullName As String) As String
            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                Return Nothing
            End If
            Try
                Dim inputAsmName As New AssemblyName(assemblyFullName)
                Dim outputAsmName As New AssemblyName()
                outputAsmName.Name = inputAsmName.Name
                outputAsmName.Version = inputAsmName.Version
                Return outputAsmName.FullName
            Catch ex As Exception When Not IsUnrecoverable(ex)
                Return Nothing
            End Try
        End Function
    End Class

    Friend Enum AddRemoveAction As Byte
        Add = 1
        Remove = 0
    End Enum

    ''' ;AssemblyOption
    ''' <summary>
    ''' Auto-add or auto-remove option:
    ''' - Yes: Silently add or remove the extensions triggered by the assembly.
    ''' - No: Do not add or remove extensions triggered by the assembly. 
    ''' - Prompt: Prompt user.
    ''' </summary>
    Friend Enum AssemblyOption
        No = 0
        Yes = 1
        Prompt = 2
    End Enum

    ''' ;AssemblyDictionary
    ''' <summary>
    ''' A dictionary based on assembly full name. It containst a list of assembly independent items and
    ''' a dictionary of assembly name and (dictionary of version and item). When version with an assembly name
    ''' it will return the list of items corresponding to that name.
    ''' </summary>
    Friend Class AssemblyDictionary(Of T)

        Public Sub New()
        End Sub

        ''' <summary>
        ''' Add the given item with the given assemblyFullName to the dictionary.
        ''' </summary>
        Public Sub AddItem(ByVal assemblyFullName As String, ByVal item As T)
            If item Is Nothing Then
                Exit Sub
            End If

            Dim assemblyName As String = Nothing
            Dim assemblyVersion As Version = Nothing
            Me.ParseAssemblyFullName(assemblyFullName, assemblyName, assemblyVersion)

            If assemblyName Is Nothing Then
                If m_AssemblyIndependentList Is Nothing Then
                    m_AssemblyIndependentList = New List(Of T)()
                End If
                m_AssemblyIndependentList.Add(item)
            Else
                If m_AssemblyDictionary Is Nothing Then
                    m_AssemblyDictionary = New Dictionary(Of String, AssemblyVersionDictionary(Of T))( _
                        System.StringComparer.OrdinalIgnoreCase)
                End If
                Dim asmVersionDictionary As AssemblyVersionDictionary(Of T) = Nothing
                If m_AssemblyDictionary.ContainsKey(assemblyName) Then
                    asmVersionDictionary = m_AssemblyDictionary(assemblyName)
                Else
                    asmVersionDictionary = New AssemblyVersionDictionary(Of T)
                    m_AssemblyDictionary.Add(assemblyName, asmVersionDictionary)
                End If
                asmVersionDictionary.AddItem(assemblyVersion, item)
            End If
        End Sub

        ''' ;RemoveItem
        ''' <summary>
        ''' Remove the given item from the dictionary.
        ''' </summary>
        Public Sub RemoveItem(ByVal item As T)
            If item Is Nothing Then
                Exit Sub
            End If
            If m_AssemblyIndependentList IsNot Nothing AndAlso m_AssemblyIndependentList.Contains(item) Then
                m_AssemblyIndependentList.Remove(item)
            End If
            If m_AssemblyDictionary IsNot Nothing AndAlso m_AssemblyDictionary.Values.Count > 0 Then
                For Each versionDict As AssemblyVersionDictionary(Of T) In m_AssemblyDictionary.Values
                    versionDict.RemoveItem(item)
                Next
            End If
        End Sub

        ''' <summary>
        ''' Get a list of item with the given assembly full name from the dictionary.
        ''' </summary>
        Public Function GetItems(ByVal assemblyFullName As String) As List(Of T)
            Dim assemblyName As String = Nothing
            Dim assemblyVersion As Version = Nothing
            Me.ParseAssemblyFullName(assemblyFullName, assemblyName, assemblyVersion)

            If assemblyName Is Nothing Then
                Return m_AssemblyIndependentList
            Else
                If m_AssemblyDictionary IsNot Nothing AndAlso m_AssemblyDictionary.ContainsKey(assemblyName) Then
                    Return m_AssemblyDictionary(assemblyName).GetItems(assemblyVersion)
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Get all items contained in the dictionary. Return either NULL or a list with something.
        ''' </summary>
        Public Function GetAllItems() As List(Of T)
            Dim result As New List(Of T)

            If m_AssemblyIndependentList IsNot Nothing AndAlso m_AssemblyIndependentList.Count > 0 Then
                result.AddRange(m_AssemblyIndependentList)
            End If

            If m_AssemblyDictionary IsNot Nothing Then
                For Each asmVersionDictionary As AssemblyVersionDictionary(Of T) In m_AssemblyDictionary.Values
                    Dim versionDependentItems As List(Of T) = asmVersionDictionary.GetAllItems()
                    If versionDependentItems IsNot Nothing AndAlso versionDependentItems.Count > 0 Then
                        result.AddRange(versionDependentItems)
                    End If
                Next
            End If

            If result.Count <= 0 Then
                result = Nothing
            End If
            Return result
        End Function

        Public Sub Clear()
            If m_AssemblyIndependentList IsNot Nothing Then
                m_AssemblyIndependentList.Clear()
            End If
            If m_AssemblyDictionary IsNot Nothing Then
                m_AssemblyDictionary.Clear()
            End If
        End Sub

        Private Sub ParseAssemblyFullName(ByVal assemblyFullName As String, _
                ByRef assemblyName As String, ByRef assemblyVersion As Version)

            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                assemblyName = Nothing
                assemblyVersion = Nothing
            Else
                Try
                    Dim asmName As New AssemblyName(assemblyFullName)
                    assemblyName = asmName.Name
                    assemblyVersion = asmName.Version
                Catch ex As Exception When Not IsUnrecoverable(ex)
                    assemblyName = Nothing
                    assemblyVersion = Nothing
                End Try
            End If
        End Sub

        Private m_AssemblyIndependentList As List(Of T)
        Private m_AssemblyDictionary As Dictionary(Of String, AssemblyVersionDictionary(Of T))

        ''' <summary>
        ''' A dictionary based on assembly version. It contains a list of version independent items
        ''' and a dictionary of version dependent items. When query with a version it will return
        ''' a list of version independent items and items of the correct version if applicable.
        ''' </summary>
        Private Class AssemblyVersionDictionary(Of Y)

            ''' <summary>
            ''' Add an item with the given version to the dictionary.
            ''' </summary>
            Public Sub AddItem(ByVal version As Version, ByVal item As Y)
                If item Is Nothing Then
                    Exit Sub
                End If
                If version Is Nothing Then
                    If m_VersionIndependentList Is Nothing Then
                        m_VersionIndependentList = New List(Of Y)
                    End If
                    m_VersionIndependentList.Add(item)
                Else
                    Dim itemList As List(Of Y) = Nothing
                    If m_VersionDependentDictionary Is Nothing Then
                        m_VersionDependentDictionary = New Dictionary(Of Version, List(Of Y))()
                    End If
                    If m_VersionDependentDictionary.ContainsKey(version) Then
                        itemList = m_VersionDependentDictionary(version)
                    Else
                        itemList = New List(Of Y)
                        m_VersionDependentDictionary.Add(version, itemList)
                    End If
                    itemList.Add(item)
                End If
            End Sub

            ''' <summary>
            ''' Get a list of items for the given version.
            ''' </summary>
            Public Function GetItems(ByVal version As Version) As List(Of Y)
                Dim result As New List(Of Y)

                ' Always include version independent list.
                If m_VersionIndependentList IsNot Nothing AndAlso m_VersionIndependentList.Count > 0 Then
                    result.AddRange(m_VersionIndependentList)
                End If

                If version IsNot Nothing Then ' Include the version dependent list if applicable
                    If m_VersionDependentDictionary IsNot Nothing AndAlso m_VersionDependentDictionary.ContainsKey(version) Then
                        Dim itemList As List(Of Y) = m_VersionDependentDictionary(version)
                        If itemList IsNot Nothing AndAlso itemList.Count > 0 Then
                            result.AddRange(itemList)
                        End If
                    End If
                End If

                If result.Count <= 0 Then
                    result = Nothing
                End If
                Return result
            End Function

            ''' <summary>
            ''' Get all items available.
            ''' </summary>
            Public Function GetAllItems() As List(Of Y)
                Dim result As New List(Of Y)

                If m_VersionIndependentList IsNot Nothing AndAlso m_VersionIndependentList.Count > 0 Then
                    result.AddRange(m_VersionIndependentList)
                End If

                If m_VersionDependentDictionary IsNot Nothing Then
                    For Each itemList As List(Of Y) In m_VersionDependentDictionary.Values
                        result.AddRange(itemList)
                    Next
                End If

                If result.Count <= 0 Then
                    result = Nothing
                End If
                Return result
            End Function

            ''' ;RemoveItem
            ''' <summary>
            ''' Remove an item from the dictionary.
            ''' </summary>
            Public Sub RemoveItem(ByVal item As Y)
                If item Is Nothing Then
                    Exit Sub
                End If
                If m_VersionIndependentList IsNot Nothing AndAlso m_VersionIndependentList.Contains(item) Then
                    m_VersionIndependentList.Remove(item)
                End If
                If m_VersionDependentDictionary IsNot Nothing AndAlso _
                        m_VersionDependentDictionary.Values IsNot Nothing Then
                    For Each itemList As List(Of Y) In m_VersionDependentDictionary.Values
                        If itemList.Contains(item) Then
                            itemList.Remove(item)
                        End If
                    Next
                End If
            End Sub

            Public Sub Clear()
                If m_VersionIndependentList IsNot Nothing Then
                    m_VersionIndependentList.Clear()
                End If
                If m_VersionDependentDictionary IsNot Nothing Then
                    m_VersionDependentDictionary.Clear()
                End If
            End Sub

            Private m_VersionIndependentList As List(Of Y)
            Private m_VersionDependentDictionary As Dictionary(Of Version, List(Of Y))
        End Class
    End Class
End Namespace

' COM interop declaration to use Solution3.GetProjectItemTemplatesWithCustomData()
' CONSIDER: (HuyN) Replace with EnvDTE90.dll when this DLL is available.
Namespace Microsoft.VisualStudio.Editors.MyExtensibility.EnvDTE90Interop

    <ComImport()> _
    <Guid("76A0263C-083C-49F1-B312-9DB360FCC9F1")> _
    <TypeLibType(TypeLibTypeFlags.FDispatchable Or TypeLibTypeFlags.FDual)> _
    <DefaultMember("Name")> _
    Friend Interface Template
        <DispId(0)> _
        ReadOnly Property ID() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(10)> _
        ReadOnly Property Name() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(20)> _
        ReadOnly Property Description() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(30)> _
        ReadOnly Property FilePath() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(40)> _
        ReadOnly Property BaseName() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(50)> _
        ReadOnly Property CustomDataSignature() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(60)> _
        ReadOnly Property CustomData() As <MarshalAs(UnmanagedType.BStr)> String
    End Interface

    <ComImport()> _
    <Guid("30C96324-A117-4618-A9A9-0B06EC455121")> _
    <TypeLibType(TypeLibTypeFlags.FDispatchable Or TypeLibTypeFlags.FDual)> _
    <DefaultMember("Item")> _
    Friend Interface Templates
        Inherits System.Collections.IEnumerable

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime)> _
        <DispId(-4)> _
        Overloads Function GetEnumerator() As _
        <MarshalAs(UnmanagedType.CustomMarshaler, _
                MarshalTypeRef:=GetType(EnumeratorToEnumVariantMarshaler))> IEnumerator

        <DispId(0)> _
        ReadOnly Property Item(ByVal index As Integer) As <MarshalAs(UnmanagedType.Interface)> Template

        <DispId(10)> _
        ReadOnly Property Count() As Integer
    End Interface

    <ComImport()> _
    <TypeLibType(TypeLibTypeFlags.FDispatchable Or TypeLibTypeFlags.FDual)> _
    <Guid("DF23915F-FDA3-4DD5-9CAA-2E1372C2BB16")> _
    <DefaultMember("Item")> _
    Friend Interface Solution3
        Inherits EnvDTE80.Solution2

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(0)> _
        Overloads Function Item(<[In](), MarshalAs(UnmanagedType.Struct)> ByVal index As Object) As <MarshalAs(UnmanagedType.Interface)> Project

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), TypeLibFunc(CShort(1)), DispId(-4)> _
        Overloads Function GetEnumerator() As <MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef:=GetType(EnumeratorToEnumVariantMarshaler))> IEnumerator

        <DispId(10)> _
        Overloads ReadOnly Property DTE() As <MarshalAs(UnmanagedType.Interface)> DTE

        <DispId(11)> _
        Overloads ReadOnly Property Parent() As <MarshalAs(UnmanagedType.Interface)> DTE

        <DispId(12)> _
        Overloads ReadOnly Property Count() As Integer

        <DispId(13)> _
        Overloads ReadOnly Property FileName() As <MarshalAs(UnmanagedType.BStr)> String

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(14)> _
        Overloads Sub SaveAs(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal FileName As String)

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(15)> _
        Overloads Function AddFromTemplate(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal FileName As String, <[In](), MarshalAs(UnmanagedType.BStr)> ByVal Destination As String, <[In](), MarshalAs(UnmanagedType.BStr)> ByVal ProjectName As String, <[In]()> Optional ByVal Exclusive As Boolean = False) As <MarshalAs(UnmanagedType.Interface)> Project

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(16)> _
        Overloads Function AddFromFile(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal FileName As String, <[In]()> Optional ByVal Exclusive As Boolean = False) As <MarshalAs(UnmanagedType.Interface)> Project

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(17)> _
        Overloads Sub Open(<[In](), MarshalAs(UnmanagedType.BStr)> ByVal FileName As String)

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(18)> _
        Overloads Sub Close(<[In]()> Optional ByVal SaveFirst As Boolean = False)

        <DispId(19)> _
        Overloads ReadOnly Property Properties() As <MarshalAs(UnmanagedType.Interface)> Properties

        <DispId(22)> _
        Overloads Property IsDirty() As Boolean

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(25)> _
        Overloads Sub Remove(<[In](), MarshalAs(UnmanagedType.Interface)> ByVal proj As Project)

        <DispId(26)> _
        Overloads ReadOnly Property TemplatePath(ByVal ProjectType As String) As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(28)> _
        Overloads ReadOnly Property FullName() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(29)> _
        Overloads Property Saved() As Boolean

        <DispId(31)> _
        Overloads ReadOnly Property Globals() As <MarshalAs(UnmanagedType.Interface)> Globals

        <DispId(32)> _
        Overloads ReadOnly Property AddIns() As <MarshalAs(UnmanagedType.Interface)> AddIns

        <DispId(33)> _
        Overloads ReadOnly Property Extender(ByVal ExtenderName As String) As <MarshalAs(UnmanagedType.IDispatch)> Object

        <DispId(34)> _
        Overloads ReadOnly Property ExtenderNames() As <MarshalAs(UnmanagedType.Struct)> Object

        <DispId(35)> _
        Overloads ReadOnly Property ExtenderCATID() As <MarshalAs(UnmanagedType.BStr)> String

        <DispId(36)> _
        Overloads ReadOnly Property IsOpen() As Boolean

        <DispId(38)> _
        Overloads ReadOnly Property SolutionBuild() As <MarshalAs(UnmanagedType.Interface)> SolutionBuild

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(40)> _
        Overloads Sub Create(<MarshalAs(UnmanagedType.BStr)> ByVal Destination As String, <MarshalAs(UnmanagedType.BStr)> ByVal Name As String)

        <DispId(41)> _
        Overloads ReadOnly Property Projects() As <MarshalAs(UnmanagedType.Interface)> Projects

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(42)> _
        Overloads Function FindProjectItem(<MarshalAs(UnmanagedType.BStr)> ByVal FileName As String) As <MarshalAs(UnmanagedType.Interface)> ProjectItem

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(43)> _
        Overloads Function ProjectItemsTemplatePath(<MarshalAs(UnmanagedType.BStr)> ByVal ProjectKind As String) As <MarshalAs(UnmanagedType.BStr)> String

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(102)> _
        Overloads Function AddSolutionFolder(<MarshalAs(UnmanagedType.BStr)> ByVal Name As String) As <MarshalAs(UnmanagedType.Interface)> Project

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(103)> _
        Overloads Function GetProjectTemplate(<MarshalAs(UnmanagedType.BStr)> ByVal TemplateName As String, <MarshalAs(UnmanagedType.BStr)> ByVal Language As String) As <MarshalAs(UnmanagedType.BStr)> String

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(104)> _
        Overloads Function GetProjectItemTemplate(<MarshalAs(UnmanagedType.BStr)> ByVal TemplateName As String, <MarshalAs(UnmanagedType.BStr)> ByVal Language As String) As <MarshalAs(UnmanagedType.BStr)> String

        <MethodImpl(MethodImplOptions.InternalCall, MethodCodeType:=MethodCodeType.Runtime), DispId(205)> _
        Overloads Function GetProjectItemTemplates(<MarshalAs(UnmanagedType.BStr)> ByVal Language As String, <MarshalAs(UnmanagedType.BStr)> ByVal CustomDataSignature As String) As <MarshalAs(UnmanagedType.Interface)> Templates
    End Interface
End Namespace