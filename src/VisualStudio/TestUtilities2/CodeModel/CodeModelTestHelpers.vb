' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Runtime.ExceptionServices
Imports System.Runtime.InteropServices
Imports EnvDTE
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Friend Module CodeModelTestHelpers

        Public ReadOnly Composition As TestComposition = VisualStudioTestCompositions.LanguageServices.AddParts(
            GetType(MockServiceProvider),
            GetType(MockVisualStudioWorkspace),
            GetType(ProjectCodeModelFactory))

        Public SystemWindowsFormsPath As String
        Public SystemDrawingPath As String

#Disable Warning IDE0040 ' Add accessibility modifiers - https://github.com/dotnet/roslyn/issues/45962
        Sub New()
#Enable Warning IDE0040 ' Add accessibility modifiers
            SystemWindowsFormsPath = GetType(System.Windows.Forms.Form).Assembly.Location
            SystemDrawingPath = GetType(System.Drawing.Point).Assembly.Location
        End Sub

        ' If something is *really* wrong with our COM marshalling stuff, the creation of the CodeModel will probably
        ' throw some sort of AV or other Very Bad exception. We still want to be able to catch them, so we can clean up
        ' the workspace. If we don't, we leak the workspace and it'll take down the process when it throws in a
        ' finalizer complaining we didn't clean it up. Catching AVs is of course not safe, but this is balancing
        ' "probably not crash" as an improvement over "will crash when the finalizer throws."

        <HandleProcessCorruptedStateExceptions()>
        Public Function CreateCodeModelTestState(definition As XElement) As CodeModelTestState
            Dim workspace = TestWorkspace.Create(definition, composition:=Composition)

            Dim result As CodeModelTestState = Nothing
            Try
                WrapperPolicy.s_ComWrapperFactory = MockComWrapperFactory.Instance

                ' The Code Model test infrastructure assumes that a test workspace only ever contains a single project.
                ' If tests are written that require multiple projects, additional support will need to be added.
                Dim project = workspace.CurrentSolution.Projects.Single()

                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
                Dim visualStudioWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                visualStudioWorkspace.SetWorkspace(workspace)

                Dim state = New CodeModelState(
                    threadingContext,
                    workspace.ExportProvider.GetExportedValue(Of MockServiceProvider),
                    project.LanguageServices,
                    visualStudioWorkspace,
                    workspace.ExportProvider.GetExportedValue(Of ProjectCodeModelFactory))

                Dim projectCodeModel = DirectCast(state.ProjectCodeModelFactory.CreateProjectCodeModel(project.Id, Nothing), ProjectCodeModel)

                Dim firstFileCodeModel As ComHandle(Of EnvDTE80.FileCodeModel2, Implementation.CodeModel.FileCodeModel)? = Nothing

                For Each document In project.Documents
                    ' Note that a parent is not specified below. In Visual Studio, this would normally be an EnvDTE.Project instance.
                    Dim fcm = projectCodeModel.GetOrCreateFileCodeModel(document.FilePath, parent:=Nothing)
                    fcm.Object.TextManagerAdapter = New MockTextManagerAdapter()

                    If Not firstFileCodeModel.HasValue Then
                        firstFileCodeModel = fcm
                    End If
                Next

                Dim root = New ComHandle(Of EnvDTE.CodeModel, RootCodeModel)(RootCodeModel.Create(state, Nothing, project.Id))

                result = New CodeModelTestState(workspace, state.Workspace, root, firstFileCodeModel, state.CodeModelService)
            Finally
                If result Is Nothing Then
                    workspace.Dispose()
                End If
            End Try

            Return result
        End Function

        Friend Class MockComWrapperFactory
            Implements IComWrapperFactory

            Public Shared ReadOnly Instance As IComWrapperFactory = New MockComWrapperFactory

            Public Function CreateAggregatedObject(managedObject As Object) As Object Implements IComWrapperFactory.CreateAggregatedObject
                Dim wrapperUnknown = BlindAggregatorFactory.CreateWrapper()
                Try
                    Dim innerUnknown = Marshal.CreateAggregatedObject(wrapperUnknown, managedObject)
                    Try
                        Dim handle = GCHandle.Alloc(managedObject, GCHandleType.Normal)
                        Dim freeHandle = True
                        Try
#Disable Warning RS0042 ' Do not copy value
                            BlindAggregatorFactory.SetInnerObject(wrapperUnknown, innerUnknown, GCHandle.ToIntPtr(handle))
#Enable Warning RS0042 ' Do not copy value
                            freeHandle = False
                        Finally
                            If freeHandle Then handle.Free()
                        End Try

                        Dim wrapperRCW = Marshal.GetObjectForIUnknown(wrapperUnknown)
                        Return CType(wrapperRCW, IComWrapperFixed)
                    Finally
                        Marshal.Release(innerUnknown)
                    End Try
                Finally
                    Marshal.Release(wrapperUnknown)
                End Try
            End Function

        End Class

        <Extension()>
        Public Function GetDocumentAtCursor(state As CodeModelTestState) As Microsoft.CodeAnalysis.Document
            Dim cursorDocument = state.Workspace.Documents.First(Function(d) d.CursorPosition.HasValue)

            Dim document = state.Workspace.CurrentSolution.GetDocument(cursorDocument.Id)
            Assert.NotNull(document)
            Return document
        End Function

        <Extension()>
        Public Function GetCodeElementAtCursor(Of T As Class)(state As CodeModelTestState, Optional scope As EnvDTE.vsCMElement = EnvDTE.vsCMElement.vsCMElementOther) As T
            Dim cursorPosition = state.Workspace.Documents.First(Function(d) d.CursorPosition.HasValue).CursorPosition.Value

            ' Here we use vsCMElementOther to mean "Figure out the scope from the type parameter".
            Dim candidateScopes = If(scope = EnvDTE.vsCMElement.vsCMElementOther,
                                     s_map(GetType(T)),
                                     {scope})

            Dim result As EnvDTE.CodeElement = Nothing

            For Each candidateScope In candidateScopes
                WpfTestRunner.RequireWpfFact($"{NameOf(GetCodeElementAtCursor)} creates {NameOf(EnvDTE.CodeElement)}s and thus uses the affinited {NameOf(CleanableWeakComHandleTable(Of SyntaxNodeKey, EnvDTE.CodeElement))}")

                Try
                    result = state.FileCodeModelObject.CodeElementFromPosition(cursorPosition, candidateScope)
                Catch ex As COMException
                    ' Loop around and try the next candidate scope
                    result = Nothing
                End Try

                If result IsNot Nothing Then
                    Exit For
                End If
            Next

            If result Is Nothing Then
                Assert.True(False, "Could not locate code element")
            End If

            Return CType(result, T)
        End Function

        ''' <summary>
        ''' Creates an "external" version of the given code element.
        ''' </summary>
        <Extension()>
        Public Function AsExternal(Of T As Class)(element As T) As T
            Dim codeElement = TryCast(element, EnvDTE.CodeElement)

            Assert.True(codeElement IsNot Nothing, "Expected code element")
            Assert.True(codeElement.InfoLocation = EnvDTE.vsCMInfoLocation.vsCMInfoLocationProject, "Expected internal code element")

            If TypeOf codeElement Is EnvDTE.CodeParameter Then
                Dim codeParameter = DirectCast(codeElement, EnvDTE.CodeParameter)
                Dim externalParentCodeElement = codeParameter.Parent.AsExternal()
                Dim externalParentCodeElementImpl = ComAggregate.GetManagedObject(Of AbstractExternalCodeMember)(externalParentCodeElement)

                Return DirectCast(externalParentCodeElementImpl.Parameters.Item(codeParameter.Name), T)
            End If

            Dim codeElementImpl = ComAggregate.GetManagedObject(Of AbstractCodeElement)(codeElement)
            Dim state = codeElementImpl.State
            Dim projectId = codeElementImpl.FileCodeModel.GetProjectId()
            Dim symbol = codeElementImpl.LookupSymbol()

            Dim externalCodeElement = codeElementImpl.CodeModelService.CreateExternalCodeElement(state, projectId, symbol)
            Assert.True(externalCodeElement IsNot Nothing, "Could not create external code element")

            Dim result = TryCast(externalCodeElement, T)
            Assert.True(result IsNot Nothing, $"Created external code element was not of type, {GetType(T).FullName}")

            Return result
        End Function

        <Extension()>
        Public Function GetMethodXML(func As EnvDTE.CodeFunction) As XElement
            Dim methodXml = TryCast(func, IMethodXML)
            Assert.NotNull(methodXml)

            Dim xml = methodXml.GetXML()
            Return XElement.Parse(xml)
        End Function

        Private ReadOnly s_map As New Dictionary(Of Type, EnvDTE.vsCMElement()) From
            {{GetType(EnvDTE.CodeAttribute), {EnvDTE.vsCMElement.vsCMElementAttribute}},
             {GetType(EnvDTE80.CodeAttribute2), {EnvDTE.vsCMElement.vsCMElementAttribute}},
             {GetType(EnvDTE.CodeClass), {EnvDTE.vsCMElement.vsCMElementClass, EnvDTE.vsCMElement.vsCMElementModule}},
             {GetType(EnvDTE80.CodeClass2), {EnvDTE.vsCMElement.vsCMElementClass, EnvDTE.vsCMElement.vsCMElementModule}},
             {GetType(EnvDTE.CodeDelegate), {EnvDTE.vsCMElement.vsCMElementDelegate}},
             {GetType(EnvDTE80.CodeDelegate2), {EnvDTE.vsCMElement.vsCMElementDelegate}},
             {GetType(EnvDTE80.CodeElement2), {EnvDTE.vsCMElement.vsCMElementOptionStmt, EnvDTE.vsCMElement.vsCMElementInheritsStmt, EnvDTE.vsCMElement.vsCMElementImplementsStmt}},
             {GetType(EnvDTE.CodeEnum), {EnvDTE.vsCMElement.vsCMElementEnum}},
             {GetType(EnvDTE80.CodeEvent), {EnvDTE.vsCMElement.vsCMElementEvent}},
             {GetType(EnvDTE.CodeFunction), {EnvDTE.vsCMElement.vsCMElementFunction, EnvDTE.vsCMElement.vsCMElementDeclareDecl}},
             {GetType(EnvDTE80.CodeFunction2), {EnvDTE.vsCMElement.vsCMElementFunction, EnvDTE.vsCMElement.vsCMElementDeclareDecl}},
             {GetType(EnvDTE80.CodeImport), {EnvDTE.vsCMElement.vsCMElementImportStmt}},
             {GetType(EnvDTE.CodeInterface), {EnvDTE.vsCMElement.vsCMElementInterface}},
             {GetType(EnvDTE80.CodeInterface2), {EnvDTE.vsCMElement.vsCMElementInterface}},
             {GetType(EnvDTE.CodeNamespace), {EnvDTE.vsCMElement.vsCMElementNamespace}},
             {GetType(EnvDTE.CodeParameter), {EnvDTE.vsCMElement.vsCMElementParameter}},
             {GetType(EnvDTE80.CodeParameter2), {EnvDTE.vsCMElement.vsCMElementParameter}},
             {GetType(EnvDTE.CodeProperty), {EnvDTE.vsCMElement.vsCMElementProperty}},
             {GetType(EnvDTE80.CodeProperty2), {EnvDTE.vsCMElement.vsCMElementProperty}},
             {GetType(EnvDTE.CodeStruct), {EnvDTE.vsCMElement.vsCMElementStruct}},
             {GetType(EnvDTE80.CodeStruct2), {EnvDTE.vsCMElement.vsCMElementStruct}},
             {GetType(EnvDTE.CodeVariable), {EnvDTE.vsCMElement.vsCMElementVariable}},
             {GetType(EnvDTE80.CodeVariable2), {EnvDTE.vsCMElement.vsCMElementVariable}}}

        <Extension>
        Public Function Find(Of T)(elements As EnvDTE.CodeElements, name As String) As T
            For Each element As EnvDTE.CodeElement In elements
                If element.Name = name Then
                    Return CType(element, T)
                End If
            Next

            Return Nothing
        End Function

    End Module
End Namespace
