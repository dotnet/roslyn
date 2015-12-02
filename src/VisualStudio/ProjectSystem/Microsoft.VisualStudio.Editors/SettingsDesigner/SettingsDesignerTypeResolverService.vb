'------------------------------------------------------------------------------
' <copyright from='2003' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.design
Imports System.Diagnostics

Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Interop

Imports Microsoft.VisualStudio.Editors.Interop

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    '@ <summary>
    '@ Resolve types from (potentially) partially specified names.
    '@ Will use the project level imports to try and resolve a partially specified class name
    '@ </summary>
    '@ <remarks></remarks>
    Friend NotInheritable Class SettingsDesignerTypeResolverService
        Implements ISettingsDesignerTypeResolverService

        Private m_Language As LanguageSpecificTypeName.Language

        Private m_WellKnownTypes As New Dictionary(Of String, Type)

        ' List of types we can't use in the designer...
        Private Shared ReadOnly s_blackList As New List(Of Type)

        Private Sub AddWellKnownType(ByVal NewType As Type)
            m_WellKnownTypes.Add(LanguageSpecificTypeName.GetDisplayName(NewType, m_Language), NewType)
        End Sub

        ''' <summary>
        ''' Type constructor
        ''' </summary>
        ''' <remarks></remarks>
        Shared Sub New()
            s_blackList.Add(GetType(System.Void))
        End Sub

        '@ <summary>
        '@ Create a new instance
        '@ </summary>
        '@ <param name="Hierarchy">The Hierarchy used to find assembly references</param>
        '@ <remarks></remarks>
        Friend Sub New(ByVal Hierarchy As IVsHierarchy, ByVal itemId As UInteger)
            If Hierarchy Is Nothing Then
                Debug.Fail("Can't resolve types without a Hierarcy to work from!")
                Throw New ArgumentNullException()
            End If
            m_VSHierarchy = Hierarchy
            m_itemId = itemId

            m_Language = LanguageSpecificTypeName.Language.UNKNOWN
            Dim proj As EnvDTE.Project = ProjectUtils.EnvDTEProject(Hierarchy)
            If proj IsNot Nothing Then
                Select Case proj.Kind
                    Case VSLangProj.PrjKind.prjKindVBProject
                        m_Language = LanguageSpecificTypeName.Language.VB
                    Case VSLangProj.PrjKind.prjKindCSharpProject
                        m_Language = LanguageSpecificTypeName.Language.CSharp
                    Case VSLangProj2.PrjKind2.prjKindVJSharpProject
                        m_Language = LanguageSpecificTypeName.Language.JSharp
                End Select
            End If

            AddWellKnownType(GetType(Boolean))
            AddWellKnownType(GetType(Byte))
            AddWellKnownType(GetType(Char))
            AddWellKnownType(GetType(DateTime))
            AddWellKnownType(GetType(Decimal))
            AddWellKnownType(GetType(Double))
            AddWellKnownType(GetType(Guid))
            AddWellKnownType(GetType(Short))
            AddWellKnownType(GetType(Integer))
            AddWellKnownType(GetType(Long))
            AddWellKnownType(GetType(SByte))
            AddWellKnownType(GetType(Single))
            AddWellKnownType(GetType(TimeSpan))
            AddWellKnownType(GetType(UShort))
            AddWellKnownType(GetType(UInteger))
            AddWellKnownType(GetType(ULong))
            AddWellKnownType(GetType(System.Drawing.Color))
            AddWellKnownType(GetType(System.Drawing.Font))
            AddWellKnownType(GetType(System.Drawing.Point))
            AddWellKnownType(GetType(System.Drawing.Size))
            AddWellKnownType(GetType(String))
            AddWellKnownType(GetType(Collections.Specialized.StringCollection))
        End Sub

        Public Function GetWellKnownTypes() As System.Collections.Generic.ICollection(Of System.Type) Implements ISettingsDesignerTypeResolverService.GetWellKnownTypes
            Return m_WellKnownTypes.Values
        End Function

        Private m_VSHierarchy As IVsHierarchy
        Friend ReadOnly Property VSHierarchy() As IVsHierarchy
            Get
                Debug.Assert(m_VSHierarchy IsNot Nothing, "returning NULL VsHierarchy?")
                Return m_VSHierarchy
            End Get
        End Property

        Private m_itemId As UInteger
        Friend ReadOnly Property ItemId() As UInteger
            Get
                Return m_itemId
            End Get
        End Property
        Private m_ServiceProvider As Microsoft.VisualStudio.Shell.ServiceProvider
        Friend ReadOnly Property ServiceProvider() As Microsoft.VisualStudio.Shell.ServiceProvider
            Get
                If m_ServiceProvider Is Nothing Then
                    Dim OLEServiceProvider As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = Nothing
                    VSErrorHandler.ThrowOnFailure(VSHierarchy.GetSite(OLEServiceProvider))
                    m_ServiceProvider = New Microsoft.VisualStudio.Shell.ServiceProvider(OLEServiceProvider)
                End If
                Debug.Assert(m_ServiceProvider IsNot Nothing, "returning NULL ServiceProvider?")
                Return m_ServiceProvider
            End Get
        End Property

#Region "Cached services"

        Private m_DynamicTypeService As DynamicTypeService
        Private ReadOnly Property DynamicTypeService() As DynamicTypeService
            Get
                If m_DynamicTypeService Is Nothing Then
                    m_DynamicTypeService = GetService(Of DynamicTypeService)()
                End If
                Return m_DynamicTypeService
            End Get
        End Property

        Private m_TypeResolutionService As ITypeResolutionService
        Friend ReadOnly Property TypeResolutionService() As ITypeResolutionService
            Get
                If m_TypeResolutionService Is Nothing Then
                    m_TypeResolutionService = DynamicTypeService.GetTypeResolutionService(VSHierarchy, ItemId)
                End If
                Return m_TypeResolutionService
            End Get
        End Property
#End Region

#Region "Type safe service accessor"
        Friend Function GetService(Of T)() As T
            Return DirectCast(ServiceProvider.GetService(GetType(T)), T)
        End Function
#End Region

        '@ <summary>
        '@ Resolve the given type.
        '@ </summary>
        '@ <param name="TypeName"></param>
        '@ <returns></returns>
        '@ <remarks>Do not require a fully qualified type name</remarks>
        Public Function ResolveType(ByVal TypeName As String) As Type Implements ISettingsDesignerTypeResolverService.ResolveType
            If TypeName Is Nothing Then
                Throw New ArgumentNullException("TypeName")
            End If

            If m_WellKnownTypes.ContainsKey(TypeName) Then
                Return m_WellKnownTypes.Item(TypeName)
            End If

            Dim ResolvedTypeName As String = ResolveTypeName(TypeName)
            If ResolvedTypeName <> "" Then
                Return CreateType(ResolvedTypeName)
            Else
                Return Nothing
            End If
        End Function

        '@ <summary>
        '@ Given a type name, try to find the fully namespace qualified name in any project references
        '@ The function resolves types in the following order:
        '@ - Exact match (using casing rules of the language)
        '@ - Match in referenced assembly when prepending one of the project level import namespaces to the typename
        '@ </summary>
        '@ <returns>
        '@  If found, name of resolved type, "" if not found!
        '@</returns>
        '@<remarks>
        '@  The resolved type name may not be create:able at design time
        '@</remarks>
        Public Function ResolveTypeName(ByVal TypeName As String) As String Implements ISettingsDesignerTypeResolverService.ResolveTypeName
            If TypeName Is Nothing Then
                Throw New ArgumentNullException("TypeName")
            End If

            ' First, let's check with our list of well known types...
            If m_WellKnownTypes.ContainsKey(TypeName) Then
                Return m_WellKnownTypes(TypeName).FullName
            End If

            ' If this is a virtual type, there is no reason to continue with the 
            ' type resolution...
            If IsVirtualType(TypeName) Then
                Return ""
            End If

            ' Next, let's make a wild stab in the dark to see if the typeresolution service
            ' can help us out here...
            If TypeResolutionService IsNot Nothing Then
                Dim TestResolveType As Type = CreateType(TypeName)
                If TestResolveType IsNot Nothing Then
                    Return TestResolveType.FullName
                End If
            End If

            Dim PotentialMatches As New Specialized.StringCollection

            ' Check if this is an exact match - if so, return it to the caller!
            If FindTypeByName(TypeName, PotentialMatches) Then
                Return TypeName
            End If

            ' Nope, no exact match... Now we should have a list of PotentialMatches that
            ' all end with the given type name... We just have to prepend the namespace to
            ' resolve the type - the problem is just that we don't know what namespace :(
            ' Well - let's just try 'em all!

            ' Check all project level import namespaces
            For Each ImportedNamespace As String In GetProjectImports()
                Dim TypeNameToTest As String
                TypeNameToTest = ImportedNamespace & "." & TypeName
                If PotentialMatches.Contains(NormalizeTypeName(TypeNameToTest)) Then
                    Return TypeNameToTest
                End If
            Next

            Return ""
        End Function

        Public Function GetTypeDisplayName(ByVal type As Type) As String Implements ISettingsDesignerTypeResolverService.GetTypeDisplayName
            Return LanguageSpecificTypeName.GetDisplayName(type, m_Language)
        End Function

        Private Function NormalizeTypeName(ByVal TypeName As String) As String
            If IsCaseSensitive Then
                Return TypeName
            Else
                Return TypeName.ToLower()
            End If
        End Function

        ''' <summary>
        ''' Is this a virtual type?
        ''' </summary>
        ''' <param name="typeName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Shared Function IsVirtualType(ByVal typeName As String) As Boolean
            If typeName Is Nothing Then
                Debug.Fail("Must pass in a valid typeName")
                Throw New ArgumentNullException("typeName")
            End If

            If String.Equals(SettingsDesigner.DisplayTypeNameConnectionString, typeName, StringComparison.Ordinal) Then
                Return True
            ElseIf String.Equals(SettingsDesigner.DisplayTypeNameWebReference, typeName, StringComparison.Ordinal) Then
                Return True
            Else
                Return False
            End If
        End Function

        '@ <summary>
        '@  Given a type name, try to find the fully namespace qualified name in any project references
        '@  If no exact match is found, the PotentialMatches is populated with all typenames that end
        '@  with the given type name...
        '@ </summary>
        '@ <param name="TypeName">
        '@  Name of type to find. The name may or may not be namespace qualified
        '@ </param>
        '@ <param name="PotentialMatches">
        '@  Collection of strings that end with the specified type name
        '@ </param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function FindTypeByName(ByVal TypeName As String, ByRef PotentialMatches As Specialized.StringCollection) As Boolean
            Dim StringCompareOptionType As StringComparison
            If IsCaseSensitive Then
                StringCompareOptionType = StringComparison.Ordinal
            Else
                StringCompareOptionType = StringComparison.OrdinalIgnoreCase
            End If

            Dim Result As New Specialized.StringCollection
            Dim AvailableTypes As System.Collections.Specialized.StringCollection = GetAllAvailableTypeNames(False, False)
            For Each AvailableType As String In AvailableTypes
                If TypeName.Equals(AvailableType, StringCompareOptionType) Then
                    PotentialMatches.Clear()
                    Return True
                ElseIf AvailableType.Length() > TypeName.Length() AndAlso TypeName.Equals(AvailableType.Substring(AvailableType.Length - TypeName.Length), StringCompareOptionType) Then
                    PotentialMatches.Add(NormalizeTypeName(AvailableType))
                End If
            Next
            Return False
        End Function
        '@ <summary>
        '@ Can we serialize the given type?
        '@ </summary>
        '@ <param name="Type"></param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Public Function CanSerializeType(ByVal Type As Type) As Boolean Implements ISettingsDesignerTypeResolverService.CanSerializeType
            Try
                If Type.IsAbstract() OrElse Not (Type.IsClass() OrElse Type.IsValueType()) Then
                    Return False
                End If

                Dim td As TypeConverter = TypeDescriptor.GetConverter(Type)

                If td IsNot Nothing AndAlso td.CanConvertFrom(GetType(String)) Then
                    Return True
                End If

                Dim Constr As System.Reflection.ConstructorInfo = Type.GetConstructor(System.Type.EmptyTypes)
                If Constr IsNot Nothing AndAlso Constr.IsPublic AndAlso Not Constr.IsStatic Then
                    Return True
                End If
            Catch ex As Exception
                ' Sometimes referenced types are not well-behaved and can trow (i.e. MissingMethodExceptions)
                '
                ' We simply Debug.Fail and then assume that we can't serialize this particular type... (if it wasn't
                ' a *really* nasty exception, cause then we rethrow!)
                Debug.Fail(String.Format("Failed to check if type {0} is serializable {1}", Type, ex))
                Common.Utils.RethrowIfUnrecoverable(ex)
            End Try

            Return False
        End Function

        '@ <summary>
        '@ Get a list of the available types
        '@ </summary>
        '@ <remarks></remarks>
        Public Function GetAllAvailableTypeNames(ByVal IncludeNonSerializable As Boolean, ByVal IncludeNonNamespaceQualifiedName As Boolean) As Specialized.StringCollection Implements ISettingsDesignerTypeResolverService.GetAllAvailableTypeNames
            Dim Result As New Specialized.StringCollection
            Try
                Dim ImportedNamespaces As New Generic.Dictionary(Of String, Boolean)
                If IncludeNonNamespaceQualifiedName Then
                    For Each Import As String In GetProjectImports()
                        ImportedNamespaces(Import) = True
                    Next
                End If

                For Each CurrentType As Type In GetAllAvailableTypesFromReferences()
                    If (Not CurrentType.IsPointer) AndAlso  CurrentType.FullName <> "" AndAlso CurrentType.IsPublic() AndAlso Not CurrentType.ContainsGenericParameters Then
                        If Not IsBlacklisted(CurrentType) Then
                            If IncludeNonSerializable OrElse CanSerializeType(CurrentType) Then
                                Result.Add(CurrentType.FullName)
                                If IncludeNonNamespaceQualifiedName AndAlso CurrentType.Namespace IsNot Nothing AndAlso ImportedNamespaces.ContainsKey(CurrentType.Namespace) Then
                                    Result.Add(CurrentType.Name)
                                End If
                            End If
                        End If
                    End If
                Next

            Catch ex As Exception
                Debug.Fail(String.Format("Failed to get available types: {0}", ex))
                Throw
            End Try
            Return Result
        End Function

        Private Function GetAllAvailableTypesFromReferences() As System.Collections.Generic.List(Of System.Type)
            Dim Result As New List(Of Type)

            ' Let's go through all project references..
            Dim SystemAdded As Boolean = False
            For Each StrAssemblyName As String In GetProjectReferences()
                Try
                    Dim CurrentAssemblyName As System.Reflection.AssemblyName = System.Reflection.AssemblyName.GetAssemblyName(StrAssemblyName)
                    If CurrentAssemblyName Is Nothing Then
                        Debug.Fail(String.Format("Failed to get AssemblyName from path {0}", StrAssemblyName))
                        Continue For
                    End If

                    Dim CurrentAssembly As System.Reflection.Assembly = TypeResolutionService.GetAssembly(CurrentAssemblyName)
                    If CurrentAssembly Is Nothing Then
                        Debug.Fail(String.Format("Failed to get Assembly from path {0}", StrAssemblyName))
                        Continue For
                    End If

                    For Each CurrentType As Type In CurrentAssembly.GetExportedTypes()
                        If Not SystemAdded AndAlso CurrentType Is GetType(Object) Then
                            SystemAdded = True
                        End If
                        If Not IsBlacklisted(CurrentType) Then
                            Result.Add(CurrentType)
                        End If
                    Next
                Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                    Debug.Fail(String.Format("Unknown error when trying to get type from assembly {0} {1}", StrAssemblyName, ex))
                End Try
            Next
            ' If we haven't added a reference to System.dll, we should do so...
            If Not SystemAdded Then
                For Each currenttype As Type In GetType(Object).Assembly.GetExportedTypes()
                    If Not IsBlacklisted(currenttype) Then
                        Result.Add(currenttype)
                    End If
                Next
            End If

            Return Result
        End Function

#Region "Private helper properties for getting VSLangproj/EnvDTE items from given hierarchy"
        '@ <summary>
        '@ Get the current VSLangProj.VSProject instance for the project containing the
        '@ .settings file
        '@ </summary>
        '@ <value></value>
        '@ <remarks></remarks>
        Private ReadOnly Property VSProject() As VSLangProj.VSProject
            Get
                If EnvDTEProject IsNot Nothing Then
                    Debug.Assert(TypeOf EnvDTEProject.Object Is VSLangProj.VSProject, "Uknown EnvDTEProject type encountered!")
                    Return DirectCast(EnvDTEProject.Object, VSLangProj.VSProject)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        '@ <summary>
        '@ Get the current EndDTE.Project instance for the project containing the .settings
        '@ file
        '@ </summary>
        '@ <remarks></remarks>
        Private m_EnvDTEProject As EnvDTE.Project
        Private ReadOnly Property EnvDTEProject() As EnvDTE.Project
            Get
                If m_EnvDTEProject Is Nothing Then
                    Dim ProjectObj As Object = Nothing
                    VSErrorHandler.ThrowOnFailure(VSHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ProjectObj))
                    m_EnvDTEProject = CType(ProjectObj, EnvDTE.Project)
                End If
                Return m_EnvDTEProject
            End Get
        End Property

#End Region

#Region "Private Type resolution helpers"

        '
        ' Consider: 
        ' Cache services
        ' Cache list of known types
        ' Listen to type/assembly change notifications from the dynamic type service
        '

        '@ <summary>
        '@ Given a fully qualified type name, create the type
        '@ </summary>
        '@ <param name="TypeName"></param>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function CreateType(ByVal TypeName As String) As Type
            Dim result As Type = TypeResolutionService.GetType(TypeName, False, Not IsCaseSensitive)
            If result IsNot Nothing AndAlso IsBlacklisted(result) Then
                Return Nothing
            Else
                Return result
            End If
        End Function

        '@ <summary>
        '@ Return list of paths to referenced assemblies
        '@ </summary>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function GetProjectReferences() As List(Of String)
            Dim References As VSLangProj.References
            Dim FoundProjectReferences As New List(Of String)

            If VSProject IsNot Nothing Then
                References = VSProject.References()
            Else
                Debug.Fail("No VsLangProj - can't get project references!")
                References = Nothing
            End If

            If References IsNot Nothing Then
                For ReferenceNo As Integer = 1 To References.Count()
                    Dim reference As VSLangProj.Reference = References.Item(ReferenceNo)
                    If reference.Type = VSLangProj.prjReferenceType.prjReferenceTypeAssembly AndAlso reference.SourceProject Is Nothing Then
                        FoundProjectReferences.Add(References.Item(ReferenceNo).Path())
                    End If
                Next
            End If
            Return FoundProjectReferences
        End Function

        '@ <summary>
        '@ Get a list of project level imports
        '@ </summary>
        '@ <returns></returns>
        '@ <remarks></remarks>
        Private Function GetProjectImports() As List(Of String)
            Dim Result As New List(Of String)
            If VSProject.Imports() IsNot Nothing Then
                For Index As Integer = 1 To VSProject.Imports.Count
                    Result.Add(VSProject.Imports.Item(Index))
                Next
            Else
                ' CONSIDER: add some default "imports" here (like "System")
            End If
            Return Result
        End Function

        Private Function IsBlacklisted(ByVal type As Type) As Boolean
            If type.IsPointer Then Return True
            If s_blackList.Contains(type) Then Return True

            Return False
        End Function

#End Region

        Private ReadOnly Property IsCaseSensitive() As Boolean
            Get
                If EnvDTEProject IsNot Nothing Then
                    Return EnvDTEProject.CodeModel.IsCaseSensitive
                Else
                    Return True
                End If
            End Get
        End Property

        Private Class LanguageSpecificTypeName
            Private Shared m_LanguageSpecificTypeNameDictionary As New Dictionary(Of Type, String())

            Friend Enum Language
                UNKNOWN = -1
                CSharp = 0
                VB = 1
                JSharp = 2
            End Enum

            Shared Sub New()
                ' add language specific type names for C#, VB, J#
                m_LanguageSpecificTypeNameDictionary(GetType(Boolean)) = New String() {"bool", "Boolean", "boolean"}
                m_LanguageSpecificTypeNameDictionary(GetType(Byte)) = New String() {"byte", "Byte", "byte"}
                m_LanguageSpecificTypeNameDictionary(GetType(Char)) = New String() {"char", "Char", "char"}
                m_LanguageSpecificTypeNameDictionary(GetType(Decimal)) = New String() {"decimal", "Decimal", Nothing}
                m_LanguageSpecificTypeNameDictionary(GetType(Double)) = New String() {"double", "Double", "double"}
                m_LanguageSpecificTypeNameDictionary(GetType(Short)) = New String() {"short", "Short", "short"}
                m_LanguageSpecificTypeNameDictionary(GetType(Integer)) = New String() {"int", "Integer", "int"}
                m_LanguageSpecificTypeNameDictionary(GetType(Long)) = New String() {"long", "Long", "long"}
                m_LanguageSpecificTypeNameDictionary(GetType(SByte)) = New String() {"sbyte", "SByte", Nothing}
                m_LanguageSpecificTypeNameDictionary(GetType(Single)) = New String() {"float", "Single", "float"}
                m_LanguageSpecificTypeNameDictionary(GetType(UShort)) = New String() {"ushort", "UShort", Nothing}
                m_LanguageSpecificTypeNameDictionary(GetType(UInteger)) = New String() {"uint", "UInteger", Nothing}
                m_LanguageSpecificTypeNameDictionary(GetType(ULong)) = New String() {"ulong", "ULong", Nothing}
                m_LanguageSpecificTypeNameDictionary(GetType(String)) = New String() {"string", "String", "String"}
                m_LanguageSpecificTypeNameDictionary(GetType(System.DateTime)) = New String() {Nothing, "Date", Nothing}
            End Sub

            Friend Shared Function GetDisplayName(ByVal type As Type, ByVal language As Language) As String
                If language <> LanguageSpecificTypeName.Language.UNKNOWN AndAlso m_LanguageSpecificTypeNameDictionary.ContainsKey(type) Then
                    Dim languageSpecificName As String = m_LanguageSpecificTypeNameDictionary(type)(language)
                    If languageSpecificName <> "" Then
                        Return languageSpecificName
                    End If
                End If
                Return type.FullName
            End Function
        End Class
    End Class
End Namespace
