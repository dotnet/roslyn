Option Strict On
Option Explicit On
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Xml
Imports System.Xml.Schema
Imports System.Threading
Imports System.Threading.Tasks
Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.XmlEditor
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.WCFReference.Interop

Imports NativeMethods = Microsoft.VisualStudio.ErrorHandler

Namespace Microsoft.VisualStudio.Editors.XmlIntellisense

    '--------------------------------------------------------------------------
    ' XmlIntellisenseService:
    '   XmlIntellisense Service Class. Implements the IXmlIntellisenseService
    '   interface.
    '
    '   This class is a factory for XmlIntellisenseSchemas objects, so that
    '   each VB project can have its own instance of XmlIntellisenseSchemas.
    '
    '   Methods on this class should only be called on the VS foreground
    '   thread.
    '--------------------------------------------------------------------------
    <ClassInterface(ClassInterfaceType.None)> _
    Friend NotInheritable Class XmlIntellisenseService
        Implements IXmlIntellisenseService

        Private m_Container As IServiceContainer
        Private m_SchemaService As XmlSchemaService

        '--------------------------------------------------------------------------
        ' New:
        '   Initialize the class.
        '--------------------------------------------------------------------------
        Friend Sub New(ByVal Container As IServiceContainer, ByVal SchemaService As XmlSchemaService)
            m_Container = Container
            m_SchemaService = SchemaService
        End Sub

        '--------------------------------------------------------------------------
        ' CreateSchemas:
        '   Implements IXmlIntellisenseService.CreateSchemas
        '
        '   Create an instance of XmlIntellisenseSchemas for the project with the
        '   specified GUID.
        '--------------------------------------------------------------------------
        Public Function CreateSchemas(ByVal ProjectGuid As Guid) As IXmlIntellisenseSchemas _
            Implements IXmlIntellisenseService.CreateSchemas

            Return New XmlIntellisenseSchemas(m_Container, m_SchemaService, ProjectGuid)
        End Function
    End Class

    ' This is the 'instance fields' for XmlIntellisenseSchemas.  It is in a separate class, so that GC-rooted callbacks can reference this without 
    ' also rooting the owner XmlIntellisenseSchemas instance object. As a result, we can witness the lifetime of the owner XmlIntellisenseSchemas 
    ' instance object via its finalizer, and leverage that lifetime information to prevent leaks.
    Class XmlIntellisenseSchemasData
        Public m_Container As IServiceContainer
        Public m_SchemaService As XmlSchemaService
        Public m_ProjectGuid As Guid
        Public m_Hierarchy As IVsHierarchy
        Public m_SchemasCompiledEvent As ManualResetEvent
        Public m_Builder As XmlSchemaSetBuilder
        Public m_TargetNamespaces() As String
        Public m_SchemaSet As XmlSchemaSet
        Public m_IndexedMembers As IXmlIntellisenseMemberList
        Public m_FirstErrorSource As String
        Public m_ExcludeDirectories As Dictionary(Of Uri, Uri)
        Public m_CompilationLevel As Integer
        Public m_SchemasFound As Integer
        Public m_SchemasCompilationCallBackDoneEvent As ManualResetEvent
        Private m_OwnerHasBeenFinalized As Boolean

        Sub New()
            m_OwnerHasBeenFinalized = False
        End Sub

        Sub NotifyOwnerIsFinalized()
            m_OwnerHasBeenFinalized = True
        End Sub

        Function OwnerHasBeenFinalized() As Boolean
            Return m_OwnerHasBeenFinalized
        End Function
    End Class

    '--------------------------------------------------------------------------
    ' XmlIntellisenseSchemas:
    '   XmlIntellisense Service Class. Implements the IXmlIntellisenseSchemas
    '   interface.
    '
    '   This class allows compilation of Xml schema information derived from
    '   schemas in the project.  Each time schema information is needed,
    '   AsyncCompile() should be called.  When compilation is complete, the
    '   CompiledEvent will be signaled (note that this is an OS Event, not a
    '   CLR Event).
    '
    '   Methods on this class should only be called on the VS foreground
    '   thread.
    '--------------------------------------------------------------------------
    <ClassInterface(ClassInterfaceType.None)> _
    Friend NotInheritable Class XmlIntellisenseSchemas
        Implements IXmlIntellisenseSchemas

        Private Const MaxPollInterval As Integer = 60 * 1000 ' 1 minutes
        Private Const MinPollInterval As Integer = 1000 ' 1 second

        ' Number of calls into AsyncCompile prior to terminating inital compilation loop
        Private Const CompilationLevel As Integer = 2

        Private m_Data As XmlIntellisenseSchemasData
        '--------------------------------------------------------------------------
        ' New:
        '   Initialize the class.
        '--------------------------------------------------------------------------
        Friend Sub New(ByVal Container As IServiceContainer, ByVal SchemaService As XmlSchemaService, ByVal ProjectGuid As Guid)
            m_Data = New XmlIntellisenseSchemasData()
            m_Data.m_Container = Container
            m_Data.m_SchemaService = SchemaService
            m_Data.m_ProjectGuid = ProjectGuid
            m_Data.m_CompilationLevel = CompilationLevel
            m_Data.m_SchemasFound = 0

            ' Get VS project
            Dim Solution As IVsSolution = DirectCast(Container.GetService(GetType(IVsSolution)), IVsSolution)
            Solution.GetProjectOfGuid(ProjectGuid, m_Data.m_Hierarchy)

            m_Data.m_SchemasCompiledEvent = New ManualResetEvent(True)
            m_Data.m_SchemasCompilationCallBackDoneEvent = New ManualResetEvent(True)
            m_Data.m_Builder = m_Data.m_SchemaService.CreateSchemaSetBuilder()
        End Sub

        Protected Overrides Sub Finalize()
            m_Data.NotifyOwnerIsFinalized()
        End Sub

        '--------------------------------------------------------------------------
        ' AsyncCompile:
        '   Implements IXmlIntellisenseSchemas.AsyncCompile
        '
        '   Recompiles all schemas in the specified project, ensuring that the most
        '   up-to-date files have been loaded.  Once compilation is complete, then
        '   the CompiledEvent will be signaled.
        '
        '   This method must be called on the VS foreground thread.  Also, in order
        '   for the background thread to make progress, the foreground thread must
        '   pump messages while waiting for any compilation results.
        '--------------------------------------------------------------------------
        Public Sub AsyncCompile() _
            Implements IXmlIntellisenseSchemas.AsyncCompile

            ' If event is signaled, then previous compilation is complete, so start another
            If m_Data.m_SchemasCompilationCallBackDoneEvent.WaitOne(0, True) Then

                ' Exclude schemas that were auto-generated by adding service references, as they can contain conflicts
                Dim ExcludeDirectories As Dictionary(Of Uri, Uri) = Nothing

                If TypeOf m_Data.m_Hierarchy Is IVsWCFMetadataStorageProvider Then
                    Dim Storages As IVsEnumWCFMetadataStorages = DirectCast(m_Data.m_Hierarchy, IVsWCFMetadataStorageProvider).GetStorages()
                    Dim Storage(1) As IVsWCFMetadataStorage
                    Dim ReturnCount As UInteger

                    While NativeMethods.Succeeded(Storages.Next(1, Storage, ReturnCount)) AndAlso ReturnCount > 0
                        Dim ResultUri As Uri = Nothing
                        If Storage(0) IsNot Nothing AndAlso Uri.TryCreate(Path.GetDirectoryName(Storage(0).GetMapFilePath()), UriKind.Absolute, ResultUri) Then
                            If ExcludeDirectories Is Nothing Then
                                ExcludeDirectories = New Dictionary(Of Uri, Uri)()
                            End If
                            ExcludeDirectories(ResultUri) = ResultUri
                        End If
                    End While
                End If

                ' Publish the excluded schemas to a member variable so that the background thread can safely access it
                ' It is important that this dictionary's content are immutable, since they will be accessed by the background thread
                m_Data.m_ExcludeDirectories = ExcludeDirectories

                ' Reset event and start background thread
                m_Data.m_SchemasCompilationCallBackDoneEvent.Reset()

                ThreadPool.QueueUserWorkItem(Sub() CompileCallback(m_Data))
            End If

            If m_Data.m_CompilationLevel > 0 Then
                m_Data.m_CompilationLevel -= 1
            End If
        End Sub

        '--------------------------------------------------------------------------
        ' CompiledEvent:
        '   Implements IXmlIntellisenseSchemas.CompiledEvent
        '
        '   Get a raw handle to the event which will be signaled when the
        '   background thread has completed compilation.
        '
        '   IMPORTANT: Do not attempt to use the event once this object has been
        '              released.
        '--------------------------------------------------------------------------
        Public ReadOnly Property CompiledEvent() As IntPtr _
            Implements IXmlIntellisenseSchemas.CompiledEvent

            Get
                Return m_Data.m_SchemasCompiledEvent.SafeWaitHandle.DangerousGetHandle()
            End Get
        End Property

        '--------------------------------------------------------------------------
        ' TargetNamespaces:
        '   Implements IXmlIntellisenseSchemas.TargetNamespaces
        '
        '   Gets array of all result target namespaces, which will form the basis
        '   of a dropdown list for the user to pick from.
        '--------------------------------------------------------------------------
        Public ReadOnly Property TargetNamespaces() As String() _
            Implements IXmlIntellisenseSchemas.TargetNamespaces

            Get
                Return m_Data.m_TargetNamespaces
            End Get
        End Property

        '--------------------------------------------------------------------------
        ' MemberList:
        '   Implements IXmlIntellisenseSchemas.MemberList
        '
        '   Returns a list of IXmlIntellisenseMember objects, one for each element
        '   and attribute declaration in the schema set.  This list can be filtered
        '   by calling the various filter methods on IXmlIntellisenseMember.
        '--------------------------------------------------------------------------
        Public ReadOnly Property MemberList() As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseSchemas.MemberList

            Get
                Return m_Data.m_IndexedMembers
            End Get
        End Property

        '--------------------------------------------------------------------------
        ' FirstErrorSource:
        '   Implements IXmlIntellisenseSchemas.FirstErrorSource
        '
        '   If non-null, contains the source uri of the file which triggered the
        '   first error (not warning) while compiling the schema set.
        '--------------------------------------------------------------------------
        Public ReadOnly Property FirstErrorSource() As String _
            Implements IXmlIntellisenseSchemas.FirstErrorSource

            Get
                Return m_Data.m_FirstErrorSource
            End Get
        End Property

        Private Shared Async Sub CompileCallBack(ByVal data As XmlIntellisenseSchemasData)
            Dim ProjectSchemas As IList(Of XmlSchemaReference)
            Dim pollInterval As Integer = MinPollInterval
            Dim iteration As Integer = 0

            If data.m_CompilationLevel > 0 OrElse iteration = 0 Then
                ' If owner was finalized, then end this polling loop, to prevent a leak
                While (Not data.OwnerHasBeenFinalized()) And (data.m_CompilationLevel > 0 OrElse iteration = 0)
                    ' Get all schemas in the current project
                    ProjectSchemas = data.m_SchemaService.GetKnownSchemas(data.m_ProjectGuid)

                    If ProjectSchemas.Count <> data.m_SchemasFound OrElse iteration = 0 Then
                        data.m_SchemasFound = ProjectSchemas.Count
                        Compile(ProjectSchemas, data)
                    Else
                        pollInterval = pollInterval * 2

                        If pollInterval > MaxPollInterval Then
                            pollInterval = MaxPollInterval
                        End If
                    End If

                    Await Task.Delay(pollInterval)
                    iteration += 1
                End While
            Else
                Compile(data.m_SchemaService.GetKnownSchemas(data.m_ProjectGuid), data)
            End If
            data.m_SchemasCompilationCallBackDoneEvent.Set()
        End Sub

        '--------------------------------------------------------------------------
        ' Compile:
        '   Called from CompileCallback when schemas need to be compiled
        '--------------------------------------------------------------------------
        Private Shared Sub Compile(ByVal ProjectSchemas As IList(Of XmlSchemaReference), ByVal data As XmlIntellisenseSchemasData)
            Dim ExcludeDirectories As Dictionary(Of Uri, Uri) = data.m_ExcludeDirectories

            ' Now signal that we are not done with compilation yet
            data.m_SchemasCompiledEvent.Reset()

            ' Include all schemas in the current project in the schema set except those in excluded directories
            data.m_Builder.Sources.Clear()
            For Each Source As XmlSchemaReference In ProjectSchemas
                If data.m_ExcludeDirectories IsNot Nothing AndAlso Source.Location.IsFile Then
                    Dim ResultUri As Uri = Nothing
                    If Uri.TryCreate(Path.GetDirectoryName(Source.Location.LocalPath), UriKind.Absolute, ResultUri) AndAlso _
                       data.m_ExcludeDirectories.ContainsKey(ResultUri) Then

                        Continue For
                    End If
                End If

                data.m_Builder.Sources.Add(Source)
            Next

            ' Add all schemas from the schema cache as candidates (i.e. schemas which will be used to resolve dangling Tns references in the sources)
            ' Enable next line once newest Xml Editor Schema Service reaches Main
            '            data.m_Builder.Candidates = data.m_SchemaService.GetKnownSchemas(XmlSchemaService.GUID_SchemaCache)

            ' Compile the schema set
            data.m_Builder.Compile()

            ' Save the first non-warning XmlSchemaReferenceException, if there is one
            Dim FirstErrorSource As String = Nothing
            For Each EachError As Exception In data.m_Builder.Errors

                ' Ignore warnings
                If EachError.Data("SchemaServices.Warning") Is Nothing Then
                    Dim SourceException As XmlSchemaReferenceException = TryCast(EachError, XmlSchemaReferenceException)

                    If SourceException IsNot Nothing AndAlso SourceException.InnerException Is Nothing Then
                        ' An XmlSchemaReferenceException will exist if an error loading one of the schemas occurred
                        FirstErrorSource = SourceException.Reference.Location.OriginalString
                        Exit For
                    ElseIf ProjectSchemas.Count > 0 Then
                        ' If no XmlSchemaReferenceException exists, then there must have been a set compilation error,
                        ' so get one of the source schemas, as it should be able to report the exact nature of the error
                        ' after the user navigates to it.
                        FirstErrorSource = ProjectSchemas(0).Location.OriginalString
                    End If
                End If
            Next
            data.m_FirstErrorSource = FirstErrorSource

            ' If the schema set is unchanged, then no need to rebuild index over it
            If data.m_SchemaSet IsNot data.m_Builder.CompiledSet Then
                data.m_SchemaSet = data.m_Builder.CompiledSet

                ' Do not build index or collect target namespaces if any errors occurred
                If FirstErrorSource Is Nothing Then
                    ' Save the target namespaces of all the result schemas
                    Dim UniqueNamespaces As Dictionary(Of String, String) = New Dictionary(Of String, String)
                    Dim ResolvedSet As IList(Of XmlSchemaReference) = data.m_Builder.ResolvedSet

                    If ResolvedSet.Count = 0 Then
                        Dim TargetNamespaces(-1) As String
                        data.m_TargetNamespaces = TargetNamespaces
                    Else
                        For Index As Integer = 0 To ResolvedSet.Count - 1
                            UniqueNamespaces(ResolvedSet(Index).TargetNamespace) = ResolvedSet(Index).TargetNamespace
                        Next

                        Dim TargetNamespaces(UniqueNamespaces.Count) As String
                        UniqueNamespaces.Values.CopyTo(TargetNamespaces, 0)

                        data.m_TargetNamespaces = TargetNamespaces
                    End If

                    ' Build the index
                    data.m_IndexedMembers = New IndexedMembers(data.m_SchemaSet, UniqueNamespaces).All
                Else
                    data.m_IndexedMembers = Nothing
                    data.m_TargetNamespaces = Nothing
                End If
            End If

            ' Now notify any listener that compilation is complete
            data.m_SchemasCompiledEvent.Set()

        End Sub

        '--------------------------------------------------------------------------
        ' ShowInXmlSchameExplorer:
        '   Implements IXmlIntellisenseSchemas.ShowInXmlSchemaExplorer
        '
        '   Shows element in XSD browser given namespace and local name.
        '--------------------------------------------------------------------------
        Public ReadOnly Property IsEmpty() As <MarshalAs(UnmanagedType.Bool)> Boolean _
            Implements IXmlIntellisenseSchemas.IsEmpty
            Get
                Return m_Data.m_SchemaSet Is Nothing OrElse m_Data.m_SchemaSet.Count = 0
            End Get
        End Property

        '--------------------------------------------------------------------------
        ' ShowInXmlSchameExplorer:
        '   Implements IXmlIntellisenseSchemas.ShowInXmlSchemaExplorer
        '
        '   Shows element in XSD browser given namespace and local name.
        '--------------------------------------------------------------------------
        Public Sub ShowInXmlSchemaExplorer( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String, _
            <MarshalAs(UnmanagedType.Bool)> ByRef ElementFound As Boolean, _
            <MarshalAs(UnmanagedType.Bool)> ByRef NamespaceFound As Boolean) _
            Implements IXmlIntellisenseSchemas.ShowInXmlSchemaExplorer

            ElementFound = False
            NamespaceFound = False

            If m_Data.m_SchemaSet Is Nothing OrElse m_Data.m_IndexedMembers Is Nothing Then Return

            ' Find the XmlSchemaElement based on NamespaceName and LocalName provided.
            Dim element As XmlSchemaElement = Nothing
            Dim ns As System.Xml.Linq.XNamespace = Nothing
            If NamespaceName Is Nothing Then
                NamespaceName = String.Empty
            End If
            If LocalName IsNot Nothing Then
                Dim elements As IXmlIntellisenseMemberList = m_Data.m_IndexedMembers.ElementsByName(NamespaceName, LocalName)
                If elements IsNot Nothing Then
                    Dim enumerator As IXmlIntellisenseMemberEnumerator = elements.GetEnumerator()
                    If enumerator IsNot Nothing Then
                        Dim member As XmlIntellisenseMember = TryCast(enumerator.GetNext(), XmlIntellisenseMember)
                        If member IsNot Nothing Then
                            element = member.Element
                        End If
                    End If
                End If
            End If
            ElementFound = element IsNot Nothing
            If NamespaceName IsNot Nothing Then
                Try
                    ' Get 'ns' even if 'element' is found. Testhook wants
                    ' to know if namespace can be found
                    ns = System.Xml.Linq.XNamespace.Get(NamespaceName)
                    NamespaceFound = ns IsNot Nothing
                Catch
                    ' ignore any exception coming from there
                End Try
                If element IsNot Nothing Then
                    ' If 'element' was found then set 'ns' to Nothing
                    ' This means 'ns' was get to support Testhook
                    ns = Nothing
                End If
            End If

            ' Get hold of VsShell service.
            Dim vsShell As IVsShell = TryCast(m_Data.m_Container.GetService(GetType(SVsShell)), IVsShell)
            If vsShell IsNot Nothing Then
                ' Make sure XSD designer package is loaded.
                Dim xsdDesignerPackageGuid As New Guid("20AAF8FA-14C0-4897-8CA0-4D861E2B1212")
                Dim package As IVsPackage = Nothing
                If VSErrorHandler.Succeeded(vsShell.LoadPackage(xsdDesignerPackageGuid, package)) Then
                    ' Get hold of IXmlSchemaDesignerService
                    Dim packageSP As IServiceProvider = TryCast(package, IServiceProvider)
                    If packageSP IsNot Nothing Then
                        Dim service As IXmlSchemaDesignerService = TryCast( _
                            packageSP.GetService(GetType(IXmlSchemaDesignerService)), _
                            IXmlSchemaDesignerService)
                        If service IsNot Nothing Then
                            ' Call the service to show the element or the namespace (whichever is not null).
                            If ns IsNot Nothing Then
                                service.AssociateSet(m_Data.m_SchemaSet, ns)
                            Else
                                service.AssociateSet(m_Data.m_SchemaSet, element)
                            End If
                        End If
                    End If
                End If
            End If
        End Sub
    End Class

    '--------------------------------------------------------------------------
    ' MemberSet:
    '   Defined for brevity.
    '--------------------------------------------------------------------------
    Friend Class MemberSet
        Inherits Dictionary(Of XmlIntellisenseMember, XmlIntellisenseMember)
    End Class

    '--------------------------------------------------------------------------
    ' FixupList:
    '   Defined for brevity.
    '--------------------------------------------------------------------------
    Friend Class FixupList
        Inherits List(Of KeyValuePair(Of XmlIntellisenseMember, XmlIntellisenseMember))
    End Class

    '--------------------------------------------------------------------------
    ' XmlIntellisenseMemberList:
    '   Class which represents a list of Xml intellisense member results, in
    '   the form of element and attribute declarations.
    '--------------------------------------------------------------------------
    <ClassInterface(ClassInterfaceType.None)> _
    Friend Class XmlIntellisenseMember
        Implements IXmlIntellisenseMember

        Private m_Name As XmlQualifiedName
        Private m_Children As XmlIntellisenseMember
        Private m_NextMember As XmlIntellisenseMember
        Private m_Flags As Flags
        Private m_Element As XmlSchemaElement

        Private Shared Any As XmlIntellisenseMember

        Private Enum Flags
            None = 0
            IsElement = 1
            IsRoot = 2
        End Enum

        Shared Sub New()
            ' Construct content model for xs:any, where any attribute and any element is allowed as content
            Any = AnyElement()
            Any.m_NextMember = AnyAttribute()
            Any.m_Children = Any
        End Sub

        Public Shared Function AnyElement() As XmlIntellisenseMember
            Dim Member As XmlIntellisenseMember = New XmlIntellisenseMember(XmlQualifiedName.Empty, Flags.IsElement)
            Member.Children = Any
            Return Member
        End Function

        Public Shared Function AnyAttribute() As XmlIntellisenseMember
            Return New XmlIntellisenseMember(XmlQualifiedName.Empty, Flags.None)
        End Function

        Private Sub New(ByVal Name As XmlQualifiedName, ByVal MemberFlags As Flags)
            m_Name = Name
            m_Flags = MemberFlags
        End Sub

        Sub New(ByVal Element As XmlSchemaElement)
            m_Name = Element.QualifiedName
            m_Flags = Flags.IsElement
            m_Element = Element
        End Sub

        Sub New(ByVal Attribute As XmlSchemaAttribute)
            m_Name = Attribute.QualifiedName
        End Sub

        Friend ReadOnly Property Element() As XmlSchemaElement
            Get
                Return m_Element
            End Get
        End Property

        Public Property Children() As XmlIntellisenseMember
            Get
                Return m_Children
            End Get
            Set(ByVal Value As XmlIntellisenseMember)
                m_Children = Value
            End Set
        End Property

        Public Property NextMember() As XmlIntellisenseMember
            Get
                Return m_NextMember
            End Get
            Set(ByVal Value As XmlIntellisenseMember)
                m_NextMember = Value
            End Set
        End Property

        Public Property IsRoot() As Boolean
            Get
                Return (m_Flags And Flags.IsRoot) <> 0
            End Get
            Set(ByVal Value As Boolean)
                Debug.Assert(Value, "IsRoot can only be set to true")
                m_Flags = m_Flags Or Flags.IsRoot
            End Set
        End Property

        Public ReadOnly Property IsAny() As Boolean
            Get
                Return m_Name.IsEmpty
            End Get
        End Property

        Public ReadOnly Property IsElement() As Boolean _
            Implements IXmlIntellisenseMember.IsElement
            Get
                Return (m_Flags And Flags.IsElement) <> 0
            End Get
        End Property

        Public ReadOnly Property NamespaceName() As String _
            Implements IXmlIntellisenseMember.NamespaceName
            Get
                Return m_Name.Namespace
            End Get
        End Property

        Public ReadOnly Property LocalName() As String _
            Implements IXmlIntellisenseMember.LocalName
            Get
                Return m_Name.Name
            End Get
        End Property

        Public ReadOnly Property Name() As XmlQualifiedName
            Get
                Return m_Name
            End Get
        End Property

    End Class

    '--------------------------------------------------------------------------
    ' IndexedMembers:
    '   Class which contains all element and attribute declarations discovered
    '   in a particular schema set, indexed by name and namespace for fast
    '   query access.
    '--------------------------------------------------------------------------
    <ClassInterface(ClassInterfaceType.None)> _
    Friend Class IndexedMembers
        Private m_TargetNamespaces As Dictionary(Of String, String)
        Private m_IndexedByNamespace As Dictionary(Of String, List(Of XmlIntellisenseMember))
        Private m_IndexedByName As Dictionary(Of XmlQualifiedName, Object)
        Private m_All As XmlIntellisenseMemberList
        Private m_Document As XmlIntellisenseMemberList
        Private m_Roots As XmlIntellisenseMemberList
        Private m_Elements As XmlIntellisenseMemberList

        Private Shared ReadOnly m_AnyElement As XmlIntellisenseMember = XmlIntellisenseMember.AnyElement()
        Private Shared ReadOnly m_AnyAttribute As XmlIntellisenseMember = XmlIntellisenseMember.AnyAttribute()

        Sub New(ByVal SchemaSet As XmlSchemaSet, ByVal TargetNamespaces As Dictionary(Of String, String))
            Dim TypeMap As Dictionary(Of XmlSchemaType, XmlIntellisenseMember) = New Dictionary(Of XmlSchemaType, XmlIntellisenseMember)()
            Dim ChildrenFixups As FixupList = New FixupList()
            Dim Roots As List(Of XmlIntellisenseMember) = New List(Of XmlIntellisenseMember)()

            m_TargetNamespaces = TargetNamespaces
            m_IndexedByNamespace = New Dictionary(Of String, List(Of XmlIntellisenseMember))()
            m_IndexedByName = New Dictionary(Of XmlQualifiedName, Object)()

            ' Start by adding all global element and attribute declarations to roots list
            For Each Element As XmlSchemaElement In SchemaSet.GlobalElements.Values
                Dim Root As XmlIntellisenseMember = CreateElementMember(Element, TypeMap, ChildrenFixups)
                Root.IsRoot = True
                Roots.Add(Root)
            Next

            ' AnyElement is always a root
            Roots.Add(m_AnyElement)

            ' Apply fixups for recursively defined types
            For Each Fixup As KeyValuePair(Of XmlIntellisenseMember, XmlIntellisenseMember) In ChildrenFixups
                Fixup.Key.Children = Fixup.Value.Children
            Next

            m_All = New XmlIntellisenseMemberList(Me)
            m_Document = New XmlIntellisenseMemberList(Me)
            m_Roots = New XmlIntellisenseMemberList(Me, Roots)
            m_Elements = New XmlIntellisenseMemberList(Me)

        End Sub

        Public ReadOnly Property All() As XmlIntellisenseMemberList
            Get
                Return m_All
            End Get
        End Property

        Public ReadOnly Property Document() As XmlIntellisenseMemberList
            Get
                Return m_Document
            End Get
        End Property

        Public ReadOnly Property Roots() As XmlIntellisenseMemberList
            Get
                Return m_Roots
            End Get
        End Property

        Public ReadOnly Property Elements() As XmlIntellisenseMemberList
            Get
                Return m_Elements
            End Get
        End Property

        ' Returns true if the namespace is defined by some schema in the set, or there exists an element or attribute declaration
        ' that defines this name in some schema in the set
        Public Function IsNamespaceDefined(ByVal Name As XmlQualifiedName) As Boolean
            ' If the namespace is defined, then type information is available for the name
            If m_TargetNamespaces.ContainsKey(Name.Namespace) Then
                Return True
            End If

            ' If the namespace is empty, then type information is available for the name only if the name is defined
            Return Name.Namespace.Length = 0 AndAlso m_IndexedByName.ContainsKey(Name)
        End Function

        Public Function FindRootsByNamespace(ByVal NamespaceName As String, ByVal Results As List(Of XmlIntellisenseMember)) As IEnumerable(Of XmlIntellisenseMember)
            For Each Root As XmlIntellisenseMember In m_Roots
                If Root.NamespaceName = NamespaceName Then
                    Results.Add(Root)
                End If
            Next

            Results.Add(m_AnyElement)

            Return Results
        End Function

        Public Function FindByNamespace(ByVal NamespaceName As String, ByVal Match As Predicate(Of XmlIntellisenseMember), ByVal Results As List(Of XmlIntellisenseMember)) As IEnumerable(Of XmlIntellisenseMember)
            Dim Members As List(Of XmlIntellisenseMember) = Nothing

            If m_IndexedByNamespace.TryGetValue(NamespaceName, Members) Then
                For Each Member As XmlIntellisenseMember In Members
                    If Match(Member) Then
                        Results.Add(Member)
                    End If
                Next
            End If

            AddAny(Match, Results)

            Return Results
        End Function

        Public Function FindByName(ByVal Name As XmlQualifiedName, ByVal Match As Predicate(Of XmlIntellisenseMember), ByVal Results As List(Of XmlIntellisenseMember)) As IEnumerable(Of XmlIntellisenseMember)
            Dim o As Object = Nothing
            Dim Member As XmlIntellisenseMember

            If m_IndexedByName.TryGetValue(Name, o) Then
                If TypeOf o Is XmlIntellisenseMember Then
                    Member = DirectCast(o, XmlIntellisenseMember)
                    If Match(Member) Then
                        Results.Add(Member)
                    End If
                Else
                    For Each ListMember As XmlIntellisenseMember In DirectCast(o, List(Of XmlIntellisenseMember))
                        If Match(ListMember) Then
                            Results.Add(ListMember)
                        End If
                    Next
                End If
            End If

            If Not IsNamespaceDefined(Name) Then
                AddAny(Match, Results)
            End If

            Return Results
        End Function

        Private Sub AddAny(ByVal Match As Predicate(Of XmlIntellisenseMember), ByVal Results As List(Of XmlIntellisenseMember))
            ' FindByNamespace and FindByName should always find Any members, as long as types match
            If Match(m_AnyElement) Then
                Results.Add(m_AnyElement)
            End If

            If Match(m_AnyAttribute) Then
                Results.Add(m_AnyAttribute)
            End If
        End Sub

        Private Function CreateElementMember(ByVal Element As XmlSchemaElement, ByVal TypeMap As Dictionary(Of XmlSchemaType, XmlIntellisenseMember), ByVal ChildrenFixups As FixupList) As XmlIntellisenseMember
            Dim NewMember As XmlIntellisenseMember = New XmlIntellisenseMember(Element)
            Dim ExistingMember As XmlIntellisenseMember = Nothing

            ' If the element's type has complex content,
            If TypeOf Element.ElementSchemaType Is XmlSchemaComplexType Then
                Dim ContentType As XmlSchemaComplexType = DirectCast(Element.ElementSchemaType, XmlSchemaComplexType)

                If ContentType IsNot Nothing Then

                    ' Has type been traversed yet?
                    If Not TypeMap.TryGetValue(ContentType, ExistingMember) Then
                        Dim ContentMembers As XmlIntellisenseMember = Nothing

                        ' No, so insert the type into the type map so that it can break cycles in the schema graph
                        TypeMap.Add(ContentType, NewMember)

                        ' Check for AnyAttribute property
                        If ContentType.AnyAttribute IsNot Nothing Then
                            ContentMembers = Concat(XmlIntellisenseMember.AnyAttribute(), ContentMembers)
                        End If

                        ' Yes, so create new content member linked list for the type
                        For Each Attribute As XmlSchemaAttribute In ContentType.AttributeUses.Values
                            ContentMembers = Concat(AddToIndexes(New XmlIntellisenseMember(Attribute)), ContentMembers)
                        Next

                        NewMember.Children = Concat(CreateContentMembers(ContentType.ContentTypeParticle, TypeMap, ChildrenFixups), ContentMembers)

                    ElseIf ExistingMember.Children Is Nothing Then
                        ' Type is recursively defined, so fixup content later
                        ChildrenFixups.Add(New KeyValuePair(Of XmlIntellisenseMember, XmlIntellisenseMember)(NewMember, ExistingMember))
                    Else
                        ' New element member has same children as existing element member
                        NewMember.Children = ExistingMember.Children
                    End If
                End If
            End If

            ' Add element member to indexes
            Return AddToIndexes(NewMember)
        End Function

        Private Function CreateContentMembers(ByVal Particle As XmlSchemaParticle, ByVal TypeMap As Dictionary(Of XmlSchemaType, XmlIntellisenseMember), ByVal ChildrenFixups As FixupList) As XmlIntellisenseMember
            ' Check for element declaration particle first
            If TypeOf Particle Is XmlSchemaElement Then
                Return CreateElementMember(DirectCast(Particle, XmlSchemaElement), TypeMap, ChildrenFixups)
            End If

            ' Recurse into particle group (choice, sequence, all)
            If TypeOf Particle Is XmlSchemaGroupBase Then
                Dim ContentMembers As XmlIntellisenseMember = Nothing

                For Each GroupParticle As XmlSchemaParticle In DirectCast(Particle, XmlSchemaGroupBase).Items
                    ContentMembers = Concat(CreateContentMembers(GroupParticle, TypeMap, ChildrenFixups), ContentMembers)
                Next

                Return ContentMembers
            End If

            ' Check for xs:any particle
            If TypeOf Particle Is XmlSchemaAny Then
                Return XmlIntellisenseMember.AnyElement()
            End If

            Return Nothing
        End Function

        Private Function Concat(ByVal Left As XmlIntellisenseMember, ByVal Right As XmlIntellisenseMember) As XmlIntellisenseMember
            Dim Current As XmlIntellisenseMember = Left

            If Current Is Nothing Then
                Return Right
            End If

            While Current.NextMember IsNot Nothing
                Current = Current.NextMember
            End While

            Current.NextMember = Right
            Return Left
        End Function

        Private Function AddToIndexes(ByVal Member As XmlIntellisenseMember) As XmlIntellisenseMember
            Dim MemberList As List(Of XmlIntellisenseMember) = Nothing
            Dim ByName As Object = Nothing

            ' Add member to namespace index
            If Not m_IndexedByNamespace.TryGetValue(Member.NamespaceName, MemberList) Then
                MemberList = New List(Of XmlIntellisenseMember)()
                m_IndexedByNamespace(Member.NamespaceName) = MemberList
            End If
            MemberList.Add(Member)

            ' Add member to name index
            If Not m_IndexedByName.TryGetValue(Member.Name, ByName) Then
                m_IndexedByName(Member.Name) = Member
            Else
                If TypeOf ByName Is List(Of XmlIntellisenseMember) Then
                    MemberList = DirectCast(ByName, List(Of XmlIntellisenseMember))
                Else
                    MemberList = New List(Of XmlIntellisenseMember)()
                    m_IndexedByName(Member.Name) = MemberList
                    MemberList.Add(DirectCast(ByName, XmlIntellisenseMember))
                End If

                MemberList.Add(Member)
            End If

            Return Member
        End Function
    End Class

    '--------------------------------------------------------------------------
    ' XmlIntellisenseMemberList:
    '   Class which represents a list of Xml intellisense member results, in
    '   the form of element and attribute declarations.
    '
    ' The Any rules:
    '
    '   1. Pretend there is an "Any" schema in the set, which allows any element
    '      and any attribute at any position in a document.  This means that
    '      the set of Roots contains an Any member.
    '
    '   2. An axis having an "exact" name (i.e. containing both a namespace part
    '      and a local name part) never propagates Any members in its input
    '      set, except in the case where the name is "untyped" (i.e.
    '      IsNamespaceDefined returns False).
    '
    '   3. An axis having only a namespace part always propagates Any members
    '      in its input set.
    '--------------------------------------------------------------------------
    <ClassInterface(ClassInterfaceType.None)> _
    Friend Class XmlIntellisenseMemberList
        Implements IXmlIntellisenseMemberList, IEnumerable(Of XmlIntellisenseMember)

        Private m_AllMembers As IndexedMembers
        Private m_PreviousStep As XmlIntellisenseMemberList
        Private m_Axis As Axis
        Private m_Name As XmlQualifiedName
        Private m_Members As IEnumerable(Of XmlIntellisenseMember)

        Private Enum Axis
            Elements
            Attributes
            Descendants
        End Enum

        '--------------------------------------------------------------------------
        ' New:
        '   Initialize a list containing all possible declarations.
        '--------------------------------------------------------------------------
        Sub New( _
            ByVal AllMembers As IndexedMembers _
            )

            m_AllMembers = AllMembers
        End Sub

        '--------------------------------------------------------------------------
        ' New:
        '   Initialize a list containing the specified members.
        '--------------------------------------------------------------------------
        Sub New( _
            ByVal AllMembers As IndexedMembers, _
            ByVal Members As IEnumerable(Of XmlIntellisenseMember) _
            )

            m_AllMembers = AllMembers
            m_Members = Members
        End Sub

        '--------------------------------------------------------------------------
        ' New:
        '   Initialize a list containing declarations found by starting with a
        '   previous list and following the specified axis from that point.
        '--------------------------------------------------------------------------
        Private Sub New( _
            ByVal AllMembers As IndexedMembers, _
            ByVal PreviousStep As XmlIntellisenseMemberList, _
            ByVal AxisOfStep As Axis, _
            ByVal NameOfStep As XmlQualifiedName _
            )

            m_AllMembers = AllMembers
            m_PreviousStep = PreviousStep
            m_Axis = AxisOfStep
            m_Name = NameOfStep
        End Sub

        '--------------------------------------------------------------------------
        ' MatchesNamedType:
        '   True if this list returns members that match a type defined in one of
        '   of the schemas in the set.  More than one type may be matched only if
        '   the other types share the same expanded name.
        '--------------------------------------------------------------------------
        Public ReadOnly Property MatchesNamedType() As Boolean _
            Implements IXmlIntellisenseMemberList.MatchesNamedType

            Get
                Return m_Name IsNot Nothing AndAlso m_Name.Name.Length <> 0 AndAlso m_AllMembers.IsNamespaceDefined(m_Name)
            End Get
        End Property

        '--------------------------------------------------------------------------
        ' GetEnumerator:
        '   Get enumerator over the list of members.
        '--------------------------------------------------------------------------
        Public Function GetEnumerator() As IEnumerator(Of XmlIntellisenseMember) _
            Implements IEnumerable(Of XmlIntellisenseMember).GetEnumerator

            If m_Members Is Nothing Then
                Dim Results As List(Of XmlIntellisenseMember) = New List(Of XmlIntellisenseMember)()

                If m_PreviousStep Is Nothing Then
                    Debug.Fail("GetEnumerator() should never be called on the Document or All member lists (it is not implemented).")
                Else If m_PreviousStep Is m_AllMembers.All Then
                    ' Applying a query to the document root and all elements yields all matching members at any level
                    If m_Name.Name.Length = 0 Then
                        m_AllMembers.FindByNamespace(m_Name.Namespace, AddressOf MatchesType, Results)
                    Else
                        m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                    End If
                ElseIf m_PreviousStep Is m_AllMembers.Roots Then
                    ' Applying a descendant query to all roots yields all matching members at any level except the root level
                    If m_Axis = Axis.Descendants Then
                        If m_Name.Name.Length = 0 Then
                            m_AllMembers.FindByNamespace(m_Name.Namespace, AddressOf MatchesTypeAndNonRoot, Results)
                        Else
                            m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                        End If
                    Else
                        FindChildMatches(m_PreviousStep.m_Members, false, Results)
                    End If
                ElseIf m_PreviousStep Is m_AllMembers.Document Then
                    ' Applying a descendant query to the document member yields all matching members at any level
                    If m_Axis = Axis.Descendants Then
                        If m_Name.Name.Length = 0 Then
                            m_AllMembers.FindByNamespace(m_Name.Namespace, AddressOf MatchesType, Results)
                        Else
                            m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                        End If
                    Else If m_Axis = Axis.Elements Then
                        If m_Name.Name.Length = 0 Then
                            m_AllMembers.FindRootsByNamespace(m_Name.Namespace, Results)
                        Else
                            m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                        End If
                    End If
                ElseIf m_PreviousStep Is m_AllMembers.Elements Then
                    ' Applying a query to all elements yields all matching members at any level except the root level
                    ' Note that refs to global elements are separate declarations, and will be found by MatchesTypeAndNonRoot
                    If m_Name.Name.Length = 0 Then
                        m_AllMembers.FindByNamespace(m_Name.Namespace, AddressOf MatchesTypeAndNonRoot, Results)
                    Else
                        m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                    End If
                Else
                    FindChildMatches(m_PreviousStep, m_Axis = Axis.Descendants, Results)
                End If

                m_Members = Results
            End If

            Return m_Members.GetEnumerator()
        End Function

        '--------------------------------------------------------------------------
        ' GetEnumerator:
        '   Get enumerator over the list of members.
        '--------------------------------------------------------------------------
        Public Function IEnumerable_GetEnumerator() As IEnumerator _
            Implements IEnumerable.GetEnumerator

            Return Me.GetEnumerator()
        End Function

        '--------------------------------------------------------------------------
        ' Document:
        '   Get member list containing single "document" declaration which acts
        '   as the root of queries.
        '--------------------------------------------------------------------------
        Public Function Document() As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.Document

            Return m_AllMembers.Document
        End Function

        '--------------------------------------------------------------------------
        ' Document:
        '   Get member list containing all element declarations.
        '--------------------------------------------------------------------------
        Public Function AllElements() As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.AllElements

            Return m_AllMembers.Elements
        End Function

        '--------------------------------------------------------------------------
        ' Document:
        '   Get member list containing all global element declarations.
        '--------------------------------------------------------------------------
        Public Function GlobalElements() As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.GlobalElements

            Return m_AllMembers.Roots
        End Function

        '--------------------------------------------------------------------------
        ' All:
        '   Get member list containing union of Document() and AllElements().
        '--------------------------------------------------------------------------
        Public Function All() As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.All

            Return m_AllMembers.All
        End Function

        '--------------------------------------------------------------------------
        ' ElementsByNamespace:
        '   Get child elements of this list having the specified namespace.
        '--------------------------------------------------------------------------
        Public Function ElementsByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.ElementsByNamespace

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Elements, New XmlQualifiedName(String.Empty, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' ElementsByName:
        '   Get child elements of this list having the specified name.
        '--------------------------------------------------------------------------
        Public Function ElementsByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.ElementsByName

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Elements, New XmlQualifiedName(LocalName, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' AttributesByNamespace:
        '   Get attributes of this list having the specified namespace.
        '--------------------------------------------------------------------------
        Public Function AttributesByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.AttributesByNamespace

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Attributes, New XmlQualifiedName(String.Empty, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' AttributesByName:
        '   Get attributes of this list having the specified name.
        '--------------------------------------------------------------------------
        Public Function AttributesByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.AttributesByName

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Attributes, New XmlQualifiedName(LocalName, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' DescendantsByNamespace:
        '   Get descendant elements of this list having the specified namespace.
        '--------------------------------------------------------------------------
        Public Function DescendantsByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.DescendantsByNamespace

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Descendants, New XmlQualifiedName(String.Empty, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' DescendantsByName:
        '   Get descendant elements of this list having the specified name.
        '--------------------------------------------------------------------------
        Public Function DescendantsByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList _
            Implements IXmlIntellisenseMemberList.DescendantsByName

            Return New XmlIntellisenseMemberList(m_AllMembers, Me, Axis.Descendants, New XmlQualifiedName(LocalName, NamespaceName))
        End Function

        '--------------------------------------------------------------------------
        ' GetEnumerator:
        '   Get an IXmlIntellisenseMemberEnumerator, which is more easily
        '   consumed by native code than IEnumerable/IEnumerator.
        '--------------------------------------------------------------------------
        Public Function IXmlIntellisenseMemberList_GetEnumerator() As IXmlIntellisenseMemberEnumerator _
            Implements IXmlIntellisenseMemberList.GetEnumerator

            Return New XmlIntellisenseMemberEnumerator(Me.GetEnumerator())
        End Function


        '--------------------------------------------------------------------------
        ' MatchesType:
        '   Returns True if the specified member's axis matches this list's axis.
        '--------------------------------------------------------------------------
        Private Function MatchesType(ByVal Member As XmlIntellisenseMember) As Boolean
            ' Attribute axis matches only attribute members, element/descendant axes only match element members
            Return Member.IsElement = (m_Axis <> Axis.Attributes)
        End Function

        '--------------------------------------------------------------------------
        ' MatchesTypeAndName:
        '   Returns True if the specified member's axis matches this list's axis,
        '   and the member's name matches this list's name.
        '--------------------------------------------------------------------------
        Private Function MatchesTypeAndName(ByVal Member As XmlIntellisenseMember) As Boolean
            ' Check type
            If Not MatchesType(Member) Then
                Return False
            End If

            ' xs:any matches any name
            If Member.IsAny Then
                Return True
            End If

            ' Check name
            Dim LocalName As String = m_Name.Name

            If LocalName.Length <> 0 AndAlso LocalName <> Member.LocalName Then
                Return False
            End If

            If m_Name.Namespace <> Member.NamespaceName Then
                Return False
            End If

            Return True
        End Function

        '--------------------------------------------------------------------------
        ' MatchesTypeAndNonRoot:
        '   Returns True if the specified member's axis matches this list's axis,
        '   and the member is not a global element or attribute declaration.
        '--------------------------------------------------------------------------
        Private Function MatchesTypeAndNonRoot(ByVal Member As XmlIntellisenseMember) As Boolean
            If Not MatchesType(Member) Then
                Return False
            End If

            Return Not Member.IsRoot
        End Function

        '--------------------------------------------------------------------------
        ' FindChildMatches:
        '   Iterate through the list of roots and find all matches.
        '--------------------------------------------------------------------------
        Private Function FindChildMatches(ByVal Roots As IEnumerable(Of XmlIntellisenseMember), ByVal Recurse As Boolean, ByVal Matches As List(Of XmlIntellisenseMember)) As IEnumerable(Of XmlIntellisenseMember)
            Dim CheckUnique As MemberSet = New MemberSet()

            For Each Root As XmlIntellisenseMember In Roots
                FindChildMatches(Root, Recurse, Matches, CheckUnique)
            Next

            Return Matches
        End Function

        Private Sub FindChildMatches(ByVal Root As XmlIntellisenseMember, ByVal Recurse As Boolean, ByVal Matches As List(Of XmlIntellisenseMember), ByVal CheckUnique As MemberSet)
            Dim Member As XmlIntellisenseMember = Root.Children

            If Member IsNot Nothing Then
                ' Insert first member into "checkUnique" set to ensure we don't cycle forever, and to ensure we don't add duplicate matches
                If Not AddUnique(Member, CheckUnique)
                    Return
                End If

                Do
                    ' If we found a match, add it to "matches" list
                    If MatchesTypeAndName(Member) Then
                        ' An exact name should match an xs:any particle only if it matches the name of an element declaration in some schema
                        If Member.IsAny AndAlso m_Name.Name.Length <> 0 AndAlso m_AllMembers.IsNamespaceDefined(m_Name) Then
                            ' Add all element declarations which match the name (will be empty set if none do, and no matches will be added)
                            Dim Results As List(Of XmlIntellisenseMember) = New List(Of XmlIntellisenseMember)

                            For Each Match As XmlIntellisenseMember In m_AllMembers.FindByName(m_Name, AddressOf MatchesType, Results)
                                If AddUnique(Match, CheckUnique) Then
                                    Matches.Add(Match)
                                End If
                            Next
                        Else
                            Matches.Add(Member)
                        End If
                    End If

                    ' Recurse()
                    If Recurse Then
                        FindChildMatches(Member, True, Matches, CheckUnique)
                    End If

                    ' Move to next child
                    Member = Member.NextMember

                Loop While Member IsNot Nothing
            End If
        End Sub

        Private Function AddUnique(ByVal Member As XmlIntellisenseMember, ByVal CheckUnique As MemberSet) As Boolean
            If CheckUnique.ContainsKey(Member) Then
                Return False
            End If

            CheckUnique(Member) = Member
            Return True
        End Function

    End Class

    <ClassInterface(ClassInterfaceType.None)> _
    Friend Class XmlIntellisenseMemberEnumerator
        Implements IXmlIntellisenseMemberEnumerator

        Private m_Enumerator As IEnumerator(Of XmlIntellisenseMember)

        Sub New(ByVal Enumerator As IEnumerator(Of XmlIntellisenseMember))
            m_Enumerator = Enumerator
        End Sub

        Public Function GetNext() As IXmlIntellisenseMember _
            Implements IXmlIntellisenseMemberEnumerator.GetNext

            If Not m_Enumerator.MoveNext() Then
                Return Nothing
            End If

            Return m_Enumerator.Current
        End Function

    End Class

End Namespace
