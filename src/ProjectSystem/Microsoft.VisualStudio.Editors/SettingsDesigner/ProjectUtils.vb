Imports Microsoft.VisualStudio.Shell.Interop

Imports System
Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.Diagnostics
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner.ProjectUtils
    Friend Module ProjectUtils

        ''' <summary>
        ''' Get the file name from a project item. 
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <returns></returns>
        ''' <remarks>If the item contains of multiple files, the first one is returned</remarks>
        Friend Function FileName(ByVal ProjectItem As EnvDTE.ProjectItem) As String
            If ProjectItem Is Nothing Then
                System.Diagnostics.Debug.Fail("Can't get file name for NULL project item!")
                Throw New System.ArgumentNullException()
            End If

            If ProjectItem.FileCount <= 0 Then
                Debug.Fail("No file associated with ProjectItem (filecount <= 0)")
                Return Nothing
            End If

            ' The ProjectItem.FileNames collection is 1 based...
            Return ProjectItem.FileNames(1)
        End Function

        ''' <summary>
        ''' From a hierarchy and projectitem, return the item id
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ProjectItem"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function ItemId(ByVal Hierarchy As IVsHierarchy, ByVal ProjectItem As EnvDTE.ProjectItem) As UInteger
            Dim FoundItemId As UInteger
            VSErrorHandler.ThrowOnFailure(Hierarchy.ParseCanonicalName(FileName(ProjectItem), FoundItemId))
            Return FoundItemId
        End Function

        ''' <summary>
        ''' Is the file pointed to by FullPath included in the project?
        ''' </summary>
        ''' <param name="project"></param>
        ''' <param name="FullFilePath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function IsFileInProject(ByVal project As IVsProject, ByVal FullFilePath As String) As Boolean
            Dim found As Integer
            Dim prio(0) As Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY
            prio(0) = Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY.DP_Standard
            Dim itemId As UInteger

            VSErrorHandler.ThrowOnFailure(project.IsDocumentInProject(FullFilePath, found, prio, itemId))
            Return found <> 0
        End Function

        ''' <summary>
        ''' VB projects don't store the root namespace as part of the generated
        ''' namespace in the .settings file.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function PersistedNamespaceIncludesRootNamespace(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger) As Boolean
            If Common.Utils.IsVbProject(Hierarchy) Then
                Return False
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' From an (optionally empty) namespace and a class name, return the fully qualified classname
        ''' </summary>
        ''' <param name="Namespace"></param>
        ''' <param name="ClassName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function FullyQualifiedClassName(ByVal [Namespace] As String, ByVal ClassName As String) As String
            Dim sectionName As String

            If [Namespace] = "" Then
                sectionName = ClassName
            Else
                sectionName = String.Format(Globalization.CultureInfo.InvariantCulture, "{0}.{1}", [Namespace], ClassName)
            End If

            Return sectionName
        End Function


        ''' <summary>
        ''' Get the namespace for the generated file...
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GeneratedSettingsClassNamespace(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger) As String
            Dim IncludeRootNamespace As Boolean = PersistedNamespaceIncludesRootNamespace(Hierarchy, ItemId)
            Return GeneratedSettingsClassNamespace(Hierarchy, ItemId, IncludeRootNamespace)
        End Function

        ''' <summary>
        ''' Get the namespace for the generated file...
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="IncludeRootNamespace"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GeneratedSettingsClassNamespace(ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal IncludeRootNamespace As Boolean) As String
            Return Common.Utils.GeneratedCodeNamespace(Hierarchy, ItemId, IncludeRootNamespace, True)
        End Function

        ''' <summary>
        ''' Is the specified ProjectItem the default settings file for the project?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="Item"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function IsDefaultSettingsFile(ByVal Hierarchy As IVsHierarchy, ByVal Item As EnvDTE.ProjectItem) As Boolean
            If Hierarchy Is Nothing Then
                Debug.Fail("Can't get the special files from a NULL Hierarchy!")
                Throw New ArgumentNullException()
            End If

            If Item Is Nothing Then
                Debug.Fail("Shouldn't pass in NULL as Item to check if it is the default settings file!")
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from IVsHierarchy")
                Return False
            End If

            Try
                Dim DefaultSettingsItemId As UInteger
                Dim DontCarePath As String = Nothing
                VSErrorHandler.ThrowOnFailure(SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), DefaultSettingsItemId, DontCarePath))
                Return DefaultSettingsItemId = ItemId(Hierarchy, Item)
            Catch ex As System.Runtime.InteropServices.COMException
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End Try
            Return False

        End Function


        ''' <summary>
        ''' Open a document that contains a class that expands the generated settings class, creating a new
        ''' document if one doesn't already exist!
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ProjectItem"></param>
        ''' <param name="CodeProvider"></param>
        ''' <remarks></remarks>
        Friend Sub OpenAndMaybeAddExtendingFile(ByVal ClassName As String, ByVal SuggestedFileName As String, ByVal sp As IServiceProvider, ByVal Hierarchy As IVsHierarchy, ByVal ProjectItem As EnvDTE.ProjectItem, ByVal CodeProvider As System.CodeDom.Compiler.CodeDomProvider, ByVal View As DesignerFramework.BaseDesignerView)
            Dim SettingClassElement As EnvDTE.CodeElement = FindElement(ProjectItem, False, True, New KnownClassName(ClassName))

            Dim cc2 As EnvDTE80.CodeClass2 = TryCast(SettingClassElement, EnvDTE80.CodeClass2)
            If cc2 Is Nothing Then
                Debug.Fail("Failed to get CodeClass2 to extend!")
                Return
            End If

            ' Find all classes that extend this class
            Dim ExtendingItem As EnvDTE.ProjectItem = Nothing
            Dim MainSettingsItemId As UInteger = ItemId(Hierarchy, cc2.ProjectItem)
            Try
                Dim pcs As EnvDTE.CodeElements = cc2.Parts()
                For ItemNo As Integer = 1 To pcs.Count
                    Dim ExpandingClass As EnvDTE80.CodeClass2 = TryCast(pcs.Item(ItemNo), EnvDTE80.CodeClass2)
                    If ExpandingClass IsNot Nothing Then
                        If ItemId(Hierarchy, ExpandingClass.ProjectItem) <> MainSettingsItemId Then
                            ExtendingItem = ExpandingClass.ProjectItem
                            Exit For
                        End If
                    End If
                Next
            Catch ex As NotImplementedException
                ' BUG VsWhidbey 204348 - PartialClasses property not implemented for VB CodeModel!
            End Try

            ' "Manually" find the classes that extend this class
            If ExtendingItem Is Nothing Then
                Dim ExpandingClass As EnvDTE.CodeElement
                ExpandingClass = FindElement(Common.DTEUtils.EnvDTEProject(Hierarchy), False, True, New ExpandsKnownClass(cc2))
                If ExpandingClass IsNot Nothing Then
                    ExtendingItem = ExpandingClass.ProjectItem
                End If
            End If

            If ExtendingItem Is Nothing Then
                ' Since we didn't find an existing item that extends the specified class, we
                ' better create a new item...

                ' But before adding a new item, we need to make sure that the project file is editable...
                If View IsNot Nothing AndAlso ProjectItem IsNot Nothing AndAlso ProjectItem.ContainingProject IsNot Nothing Then
                    View.EnterProjectCheckoutSection()
                    Try
                        Dim sccmgr As New DesignerFramework.SourceCodeControlManager(sp, Hierarchy)
                        sccmgr.ManageFile(ProjectItem.ContainingProject.FullName)
                        sccmgr.EnsureFilesEditable()

                        If View.ProjectReloadedDuringCheckout Then
                            ' We need to bail ASAP if the project was reloaded during checkout - this will have brought down a new version of
                            ' the project file and potentially a file containing the user part of the settings class....
                            '
                            ' The user will see this as if nothing happened the first time they clicked on ViewCode, and (s)he will hopefully 
                            ' try again...
                            Return
                        End If
                    Finally
                        View.LeaveProjectCheckoutSection()
                    End Try
                End If

                Dim vsproj As IVsProject = TryCast(Hierarchy, IVsProject)
                Dim ParentId As UInteger
                Dim CollectionToAddTo As EnvDTE.ProjectItems
                Dim NewFilePath As String

                If IsDefaultSettingsFile(Hierarchy, ProjectItem) Then
                    ParentId = VSITEMID.ROOT
                    CollectionToAddTo = ProjectItem.ContainingProject.ProjectItems
                    NewFilePath = DirectCast(ProjectItem.ContainingProject.Properties.Item("FullPath").Value, String)
                Else
                    ParentId = ItemId(Hierarchy, ProjectItem)
                    CollectionToAddTo = ProjectItem.Collection
                    NewFilePath = IO.Path.GetDirectoryName(FileName(ProjectItem))
                End If

                If Not (NewFilePath.EndsWith(IO.Path.DirectorySeparatorChar) OrElse NewFilePath.EndsWith(IO.Path.AltDirectorySeparatorChar)) Then
                    NewFilePath &= IO.Path.DirectorySeparatorChar
                End If

                Dim NewItemName As String
                If SuggestedFileName <> "" Then
                    NewItemName = SuggestedFileName & "." & CodeProvider.FileExtension
                Else
                    NewItemName = cc2.Name & "." & CodeProvider.FileExtension
                End If

                If IsFileInProject(vsproj, NewFilePath & NewItemName) Then
                    VSErrorHandler.ThrowOnFailure(vsproj.GenerateUniqueItemName(ParentId, "." & CodeProvider.FileExtension, System.IO.Path.GetFileNameWithoutExtension(NewItemName), NewItemName))
                End If
                ' CONSIDER: Using different mechanism to figure out if this is VB than checking the file extension...
                Dim supportsDeclarativeEventHandlers As Boolean = (CodeProvider.FileExtension.Equals("vb", StringComparison.OrdinalIgnoreCase))
                ExtendingItem = AddNewProjectItemExtendingClass(cc2, NewFilePath & NewItemName, CodeProvider, supportsDeclarativeEventHandlers, CollectionToAddTo)
            End If

            Debug.Assert(ExtendingItem IsNot Nothing, "Couldn't find/create a class that extends the generated settings class")

            If ExtendingItem IsNot Nothing Then
                If ExtendingItem.IsOpen AndAlso ExtendingItem.Document IsNot Nothing Then
                    ExtendingItem.Document.Activate()
                Else
                    Dim Win As EnvDTE.Window = ExtendingItem.Open()
                    If Win IsNot Nothing Then
                        Win.SetFocus()
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Create a new file, adding a class that extends the generated settings class
        ''' </summary>
        ''' <param name="cc2">CodeClass2 to extend</param>
        ''' <param name="NewFilePath">Fully specified name and path for new file</param>
        ''' <param name="Generator">Code generator to use to generate the code</param>
        ''' <param name="CollectionToAddTo"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function AddNewProjectItemExtendingClass(ByVal cc2 As EnvDTE80.CodeClass2, ByVal NewFilePath As String, ByVal Generator As CodeDomProvider, ByVal supportsDeclarativeEventHandlers As Boolean, Optional ByVal CollectionToAddTo As EnvDTE.ProjectItems = Nothing) As EnvDTE.ProjectItem
            If cc2 Is Nothing Then
                Debug.Fail("CodeClass2 isntance to extend can't be NULL!")
                Throw New ArgumentNullException()
            End If

            If NewFilePath Is Nothing Then
                Debug.Fail("NewFilePath can't be nothing!")
                Throw New ArgumentNullException()
            End If

            If Generator Is Nothing Then
                Debug.Fail("Can't create a new file with a NULL CodeDomProvider")
                Throw New ArgumentNullException()
            End If

            Dim AddTo As EnvDTE.ProjectItems
            If CollectionToAddTo IsNot Nothing Then
                ' If we are given a specific ProjectItems collection to add the new
                ' item to, make sure we do so!
                AddTo = CollectionToAddTo
            Else
                ' Otherwise, we'll add the item to the class to expand's containing
                ' project (at the root level)
                AddTo = cc2.ProjectItem.ContainingProject.ProjectItems
            End If

            Debug.Assert(AddTo IsNot Nothing, "Must have a project items collection to add new item to!")

            ' Create new document...
            Using Writer As New System.IO.StreamWriter(NewFilePath, False, System.Text.Encoding.UTF8)
                Dim ExtendingNamespace As CodeNamespace = Nothing
                If cc2.Namespace IsNot Nothing Then
                    Debug.Assert(cc2.Namespace.FullName IsNot Nothing, "Couldn't get a FullName from the CodeClass2.Namespace!?")
                    Dim NamespaceName As String = ""
                    If String.Equals(cc2.Language, EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB, StringComparison.OrdinalIgnoreCase) Then
                        Dim rootNamespace As String = ""
                        Try
                            Dim projProp As EnvDTE.Property = cc2.ProjectItem.ContainingProject.Properties.Item("RootNamespace")
                            If projProp IsNot Nothing Then
                                rootNamespace = CStr(projProp.Value)
                            End If
                        Catch ex As Exception
                            Debug.Fail(String.Format("Failed to get root namespace to remove from class name: {0}", ex))
                        End Try
                        ExtendingNamespace = New CodeNamespace(Common.Utils.RemoveRootNamespace(cc2.Namespace.FullName, rootNamespace))
                    Else
                        ExtendingNamespace = New CodeNamespace(cc2.Namespace.FullName)
                    End If
                End If

                Dim ExtendingType As New CodeTypeDeclaration(cc2.Name)

                ExtendingType.TypeAttributes = CodeModelToCodeDomTypeAttributes(cc2)
                ExtendingType.IsPartial = True

                ExtendingType.Comments.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_COMMON1)))
                ExtendingType.Comments.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_COMMON2)))
                ExtendingType.Comments.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_COMMON3)))
                ExtendingType.Comments.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_COMMON4)))
                ExtendingType.Comments.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_COMMON5)))
                If Not supportsDeclarativeEventHandlers Then
                    GenerateExtendingClassInstructions(ExtendingType, Generator)
                End If

                If ExtendingNamespace IsNot Nothing Then
                    ExtendingNamespace.Types.Add(ExtendingType)
                    Generator.GenerateCodeFromNamespace(ExtendingNamespace, Writer, Nothing)
                Else
                    Generator.GenerateCodeFromType(ExtendingType, Writer, Nothing)
                End If
                Writer.Flush()
                Writer.Close()
            End Using

            Return AddTo.AddFromFileCopy(NewFilePath)
        End Function


        Friend Interface IFindFilter
            Function IsMatch(ByVal Element As EnvDTE.CodeElement) As Boolean
        End Interface

        ''' <summary>
        ''' Indicates whether to search for a class or a module or either
        ''' </summary>
        ''' <remarks></remarks>
        Friend Enum ClassOrModule
            ClassOnly
            ModuleOnly
            Either
        End Enum

        ''' <summary>
        ''' Look for a CodeClass with a known name in the project
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class KnownClassName
            Implements IFindFilter

            Private m_ClassName As String
            Private m_ClassOrModule As ClassOrModule

            Friend Sub New(ByVal ClassName As String, Optional ByVal ClassOrModule As ClassOrModule = ClassOrModule.ClassOnly)
                m_ClassName = ClassName
                m_ClassOrModule = ClassOrModule
            End Sub

            Public Function IsMatch(ByVal Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                Select Case m_ClassOrModule
                    Case ClassOrModule.ClassOnly
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementClass Then
                            Return False
                        End If
                    Case ClassOrModule.Either
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementClass AndAlso Element.Kind <> EnvDTE.vsCMElement.vsCMElementModule Then
                            Return False
                        End If
                    Case ClassOrModule.ModuleOnly
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementModule Then
                            Return False
                        End If
                    Case Else
                        Debug.Fail("Unexpected case")
                End Select

                Return m_ClassName.Equals(Element.FullName, StringComparison.Ordinal)
            End Function
        End Class

        ''' <summary>
        ''' Filter that finds a class expanding a known class specified by a CodeClass2 instance
        ''' </summary>
        ''' <remarks></remarks>
        Private Class ExpandsKnownClass
            Implements IFindFilter

            ''' <summary>
            ''' The class to expand
            ''' </summary>
            ''' <remarks></remarks>
            Private m_ClassToExpand As EnvDTE80.CodeClass2

            Friend Sub New(ByVal ClassToExpand As EnvDTE80.CodeClass2)
                If ClassToExpand Is Nothing Then
                    Debug.Fail("Can't find a class that expands a NULL class...")
                    Throw New System.ArgumentNullException()
                End If
                m_ClassToExpand = ClassToExpand
            End Sub

            Public Function IsMatch(ByVal Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind = EnvDTE.vsCMElement.vsCMElementClass AndAlso _
                    (Not FileName(m_ClassToExpand.ProjectItem).Equals(FileName(Element.ProjectItem), System.StringComparison.Ordinal)) AndAlso _
                    m_ClassToExpand.FullName.Equals(Element.FullName) _
                Then
                    Dim cc2 As EnvDTE80.CodeClass2 = TryCast(Element, EnvDTE80.CodeClass2)
                    If cc2 IsNot Nothing AndAlso cc2.DataTypeKind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial Then
                        Return True
                    End If
                End If
                Return False
            End Function
        End Class

        ''' <summary>
        ''' Find a CodeElement representing a property with a known name in a known class
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class FindPropertyFilter
            Implements IFindFilter

            Private m_ContaintingClass As EnvDTE.CodeElement
            Private m_PropertyName As String

            Public Sub New(ByVal ContainingClass As EnvDTE.CodeElement, ByVal PropertyName As String)
                If ContainingClass Is Nothing Then
                    Debug.Fail("Can't find property in unknown class!")
                    Throw New ArgumentNullException()
                End If

                If PropertyName Is Nothing Then
                    Debug.Fail("Can't find property without a property name!")
                    Throw New ArgumentNullException()
                End If

                m_ContaintingClass = ContainingClass
                m_PropertyName = PropertyName
            End Sub

            Public Function IsMatch(ByVal Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind <> EnvDTE.vsCMElement.vsCMElementProperty Then
                    Return False
                End If

                Dim comparisonType As System.StringComparison
                If Element.ProjectItem IsNot Nothing AndAlso _
                    Element.ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso Not Element.ProjectItem.ContainingProject.CodeModel.IsCaseSensitive Then
                    'BEGIN
                    comparisonType = StringComparison.OrdinalIgnoreCase
                Else
                    comparisonType = StringComparison.Ordinal
                End If

                If Not Element.Name.Equals(m_PropertyName, comparisonType) Then
                    Return False
                End If

                Dim Prop As EnvDTE.CodeProperty = TryCast(Element, EnvDTE.CodeProperty)
                Debug.Assert(Prop IsNot Nothing, "Failed to get EnvDTE.CodeProperty from element with kind = vsCMElementProperty!?")
                If Prop.Parent Is Nothing Then
                    Return False
                End If

                If Prop.Parent.FullName.Equals(m_ContaintingClass.FullName, comparisonType) Then
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class

        ''' <summary>
        ''' Find a CodeElement representing a method with a known name in a known class
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class FindFunctionFilter
            Implements IFindFilter

            Private m_ContaintingClass As EnvDTE.CodeElement
            Private m_FunctionName As String

            Public Sub New(ByVal ContainingClass As EnvDTE.CodeElement, ByVal FunctionName As String)
                If ContainingClass Is Nothing Then
                    Debug.Fail("Can't find property in unknown class!")
                    Throw New ArgumentNullException()
                End If

                If FunctionName Is Nothing Then
                    Debug.Fail("Can't find property without a property name!")
                    Throw New ArgumentNullException()
                End If

                m_ContaintingClass = ContainingClass
                m_FunctionName = FunctionName
            End Sub

            ''' <summary>
            ''' Check whether a code element meets our requirement.
            ''' </summary>
            Public Function IsMatch(ByVal Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind <> EnvDTE.vsCMElement.vsCMElementFunction Then
                    Return False
                End If

                ' Check name first...
                Dim comparisonType As System.StringComparison
                If Element.ProjectItem IsNot Nothing AndAlso _
                    Element.ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso Not Element.ProjectItem.ContainingProject.CodeModel.IsCaseSensitive Then
                    'BEGIN
                    comparisonType = StringComparison.OrdinalIgnoreCase
                Else
                    comparisonType = StringComparison.Ordinal
                End If

                If Not Element.Name.Equals(m_FunctionName, comparisonType) Then
                    Return False
                End If

                ' check containing class...
                Dim Func As EnvDTE.CodeFunction = TryCast(Element, EnvDTE.CodeFunction)
                Debug.Assert(Func IsNot Nothing, "Failed to get EnvDTE.CodeFunction from element with kind = vsCMElementFunction!?")
                If Func.Parent Is Nothing Then
                    Return False
                End If

                Dim ContaintingClass As EnvDTE.CodeClass = TryCast(Func.Parent, EnvDTE.CodeClass)
                If ContaintingClass IsNot Nothing AndAlso ContaintingClass.FullName.Equals(m_ContaintingClass.FullName, comparisonType) Then
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class

        ''' <summary>
        ''' Find the first CodeElement in the project that satisfies the given filter
        ''' </summary>
        ''' <param name="Project">The project to search in</param>
        ''' <param name="ExpandChildElements">If we should loop through the child CodeElements of types</param>
        ''' <param name="ExpandChildItems">If we should recurse to ProjectItem children</param>
        ''' <param name="Filter">The IFilter to satisfy</param>
        ''' <returns>The found element, NULL if no matching element found</returns>
        ''' <remarks></remarks>
        Friend Function FindElement(ByVal Project As EnvDTE.Project, ByVal ExpandChildElements As Boolean, ByVal ExpandChildItems As Boolean, ByVal Filter As IFindFilter) As EnvDTE.CodeElement
            For Each Item As EnvDTE.ProjectItem In Project.ProjectItems
                Dim Result As EnvDTE.CodeElement = FindElement(Item, ExpandChildElements, ExpandChildItems, Filter)
                If Result IsNot Nothing Then
                    Return Result
                End If
            Next
            Return Nothing
        End Function


        ''' <summary>
        ''' Find the first CodeElement int the ProjectItem's FileCodeModel that satisfies the given filter
        ''' </summary>
        ''' <param name="ProjectItem">The project to search in</param>
        ''' <param name="ExpandChildElements">If we should loop through the child CodeElements of types</param>
        ''' <param name="ExpandChildItems">If we should recurse to ProjectItem children</param>
        ''' <param name="Filter">The IFilter to satisfy</param>
        ''' <returns>The found element, NULL if no matching element found</returns>
        ''' <remarks></remarks>
        Friend Function FindElement(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal ExpandChildElements As Boolean, ByVal ExpandChildItems As Boolean, ByVal Filter As IFindFilter) As EnvDTE.CodeElement
            If ProjectItem.FileCodeModel IsNot Nothing Then
                For Each Element As EnvDTE.CodeElement In ProjectItem.FileCodeModel.CodeElements
                    Dim Result As EnvDTE.CodeElement = FindElement(Element, ExpandChildElements, Filter)
                    If Result IsNot Nothing Then
                        Return Result
                    End If
                Next
            End If

            If ExpandChildItems AndAlso ProjectItem.ProjectItems IsNot Nothing Then
                For Each ChildItem As EnvDTE.ProjectItem In ProjectItem.ProjectItems
                    Dim Result As EnvDTE.CodeElement = FindElement(ChildItem, ExpandChildElements, ExpandChildItems, Filter)
                    If Result IsNot Nothing Then
                        Return Result
                    End If
                Next
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Find the first element that satisfies the Filters IsMatch function
        ''' </summary>
        ''' <param name="Element">The element to check</param>
        ''' <param name="ExpandChildren">If we want to recurse through this elements children</param>
        ''' <param name="Filter">The filter to satisfy</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindElement(ByVal Element As EnvDTE.CodeElement, ByVal ExpandChildren As Boolean, ByVal Filter As IFindFilter) As EnvDTE.CodeElement
            If Filter.IsMatch(Element) Then
                Return Element
            End If

            ' We only expand code elements if it is a Namespace OR we are explicitly told to do so...
            Dim ShouldExpand As Boolean = ExpandChildren OrElse Element.Kind = EnvDTE.vsCMElement.vsCMElementNamespace
            If ShouldExpand Then
                Dim Children As EnvDTE.CodeElements = Nothing
                If Element.IsCodeType Then
                    If Element.Kind <> EnvDTE.vsCMElement.vsCMElementDelegate Then
                        Children = CType(Element, EnvDTE.CodeType).Members
                    End If
                ElseIf Element.Kind = EnvDTE.vsCMElement.vsCMElementNamespace Then
                    Children = CType(Element, EnvDTE.CodeNamespace).Members
                End If

                ' If we found children, let's iterate through these as well to find 
                ' any potential matches...
                If Children IsNot Nothing Then
                    For Each ChildElement As EnvDTE.CodeElement In Children
                        Dim Result As EnvDTE.CodeElement = FindElement(ChildElement, ExpandChildren, Filter)
                        If Result IsNot Nothing Then
                            Return Result
                        End If
                    Next
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' If the generated language doesn't support declarative event handlers, we
        ''' add a stub to make sure the user isn't totally lost when (s)he is presented
        ''' with the user code...
        ''' </summary>
        ''' <param name="ct"></param>
        ''' <param name="generator"></param>
        ''' <remarks></remarks>
        Private Sub GenerateExtendingClassInstructions(ByVal ct As CodeTypeDeclaration, ByVal generator As CodeDomProvider)
            Const SettingChangingEventName As String = "SettingChanging"
            Const SettingsSavingEventName As String = "SettingsSaving"

            Const SettingChangingEventHandlerName As String = "SettingChangingEventHandler"
            Const SettingsSavingEventHandlerName As String = "SettingsSavingEventHandler"

            ' Add constructor
            Dim constr As New CodeConstructor()
            constr.Attributes = MemberAttributes.Public

            ' Generate a series of statements to add to the constructor
            Dim thisExpr As New CodeThisReferenceExpression()
            Dim stmts As New CodeStatementCollection()
            stmts.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_HOWTO_ATTACHEVTS)))
            stmts.Add(New CodeAttachEventStatement(thisExpr, SettingChangingEventName, New CodeMethodReferenceExpression(thisExpr, SettingChangingEventHandlerName)))
            stmts.Add(New CodeAttachEventStatement(thisExpr, SettingsSavingEventName, New CodeMethodReferenceExpression(thisExpr, SettingsSavingEventHandlerName)))

            For Each stmt As CodeStatement In stmts
                constr.Statements.Add(CommentStatement(stmt, generator, True))
            Next

            ' Add stubs for settingschanging/settingssaving event handlers
            Dim senderParam As New CodeParameterDeclarationExpression(GetType(Object), "sender")
            Dim changingStub As New CodeMemberMethod()
            changingStub.Name = SettingChangingEventHandlerName
            changingStub.ReturnType = Nothing
            changingStub.Parameters.Add(senderParam)
            changingStub.Parameters.Add(New CodeParameterDeclarationExpression(GetType(System.Configuration.SettingChangingEventArgs), "e"))
            changingStub.Statements.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_HANDLE_CHANGING)))

            Dim savingStub As New CodeMemberMethod()
            savingStub.Name = SettingsSavingEventHandlerName
            savingStub.ReturnType = Nothing
            savingStub.Parameters.Add(senderParam)
            savingStub.Parameters.Add(New CodeParameterDeclarationExpression(GetType(System.ComponentModel.CancelEventArgs), "e"))
            savingStub.Statements.Add(New CodeCommentStatement(SR.GetString(SR.SD_CODEGENCMT_HANDLE_SAVING)))

            ct.Members.Add(constr)
            ct.Members.Add(changingStub)
            ct.Members.Add(savingStub)
        End Sub


        ''' <summary>
        ''' Create a comment statement from a "normal" code statement
        ''' </summary>
        ''' <param name="statement">The statement to comment out</param>
        ''' <param name="generator"></param>
        ''' <param name="doubleCommentComments">
        ''' If "statement" is already a comment, we can choose to add another level of comments by
        ''' settings this guy to true
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CommentStatement(ByVal statement As CodeStatement, ByVal generator As CodeDomProvider, ByVal doubleCommentComments As Boolean) As CodeCommentStatement
            ' If this is already a comment and we don't want to double comment it, just return the statement...
            If TypeOf statement Is CodeCommentStatement AndAlso Not doubleCommentComments Then
                Return DirectCast(statement, CodeCommentStatement)
            End If

            Dim sb As New System.Text.StringBuilder
            Dim sw As New System.IO.StringWriter(sb)

            generator.GenerateCodeFromStatement(statement, sw, New CodeGeneratorOptions())
            sw.Flush()

            Return New CodeCommentStatement(sb.ToString())
        End Function

        ''' <summary>
        ''' Get either the app.config file name or the project file name if the app.config file isn't included in the 
        ''' project. Used to check out the right set of files... 
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <param name="VsHierarchy"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function AppConfigOrProjectFileNameForCheckout(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal VsHierarchy As IVsHierarchy) As String
            ' We also want to check out the app.config and possibly the project file(s)...
            If ProjectItem IsNot Nothing Then
                ' We try to check out the app.config file...
                Dim projSpecialFiles As IVsProjectSpecialFiles = TryCast(VsHierarchy, IVsProjectSpecialFiles)
                If projSpecialFiles IsNot Nothing Then
                    Dim appConfigFileName As String = ""
                    Dim appConfigItemId As UInteger
                    Dim hr As Integer = projSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, CUInt(__PSFFLAGS.PSFF_FullPath), appConfigItemId, appConfigFileName)
                    If VSErrorHandler.Succeeded(hr) AndAlso appConfigFileName <> "" Then
                        If appConfigItemId <> VSITEMID.NIL Then
                            Return appConfigFileName
                        Else
                            ' Not app.config file in the project - we need to check out the project file!
                            If ProjectItem.ContainingProject IsNot Nothing AndAlso ProjectItem.ContainingProject.FullName <> "" Then
                                Return ProjectItem.ContainingProject.FullName
                            End If
                        End If
                    End If
                End If
            End If
            Return String.Empty
        End Function

        ''' <summary>
        ''' Translate the visibility (friend/public) and other type attributes (i.e. sealed) from CodeModel lingo
        ''' to what CodeDom expects
        ''' </summary>
        ''' <param name="cc2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function CodeModelToCodeDomTypeAttributes(ByVal cc2 As EnvDTE80.CodeClass2) As System.Reflection.TypeAttributes
            If cc2 Is Nothing Then
                Throw New ArgumentNullException("cc2")
            End If

            Dim returnValue As System.Reflection.TypeAttributes = 0

            Select Case cc2.Access
                Case EnvDTE.vsCMAccess.vsCMAccessProject
                    returnValue = TypeAttributes.NestedAssembly
                Case EnvDTE.vsCMAccess.vsCMAccessPublic
                    returnValue = TypeAttributes.Public
                Case Else
                    System.Diagnostics.Debug.Fail("Unexpected access for settings class: " & cc2.Access.ToString())
                    returnValue = TypeAttributes.NestedAssembly
            End Select

            If cc2.InheritanceKind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Then
                returnValue = returnValue Or TypeAttributes.Sealed
            End If

            Return returnValue
        End Function

    End Module
End Namespace
