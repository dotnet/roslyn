' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.InlineRename
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.InlineRename
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class RenameViewModelTests
        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithNoOverload(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                using System.Threading.Tasks;

                                class Program
                                {
                                    public void $$goo()
                                    {
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                    renameOverloads:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithOverload(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                using System.Threading.Tasks;

                                class Program
                                {
                                    public void $$goo()
                                    {
                                    }

                                    public void goo(int i)
                                    {
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                    hasRenameOverload:=True,
                    renameOverloads:=True)
        End Function

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883263")>
        <CombinatorialData>
        Public Async Function RenameWithInvalidOverload(host As RenameTestHost) As Task
            Await VerifyViewModels(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class Program
{
    void $$X(int x)
    {
        X();
    }
    void X(int x, int y)
    {
    }
}
                            </Document>
                    </Project>
                </Workspace>,
                host:=host,
                newName:="Bar",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                renameOverloads:=True,
                hasRenameOverload:=True,
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 1),
                severity:=RenameDashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853839")>
        Public Async Function RenameAttributeAlias(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
using $$Evil = AttributeAttribute; 
[AttributeAttribute] 
class AttributeAttribute : System.Attribute { }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="AttributeAttributeAttribute",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                    resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                    severity:=RenameDashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameWithOverloadAndInStringsAndComments(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                using System;
                                using System.Collections.Generic;
                                using System.Linq;
                                using System.Threading.Tasks;

                                class Program
                                {
                                    public void $$goo()
                                    {
                                    }

                                    /// goo
                                    public void goo(int i)
                                    {
                                        // goo
                                        var a = "goo";
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 5),
                    hasRenameOverload:=True,
                    renameOverloads:=True,
                    renameInStrings:=True,
                    renameInComments:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInComments(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                 <![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class $$Program
{
    /// <summary>
    /// <Program></Program>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        // Program
        /* Program!
            Program
        */
        var a = "Program";
    }
}
]]>
                             </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 6),
                    renameInComments:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInStrings(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                 <![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class $$Program
{
    /// <summary>
    /// <Program></Program>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        // Program
        /* Program!
            Program
        */
        var a = "Program";
    }
}
]]>
                             </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                    renameInStrings:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInCommentsAndStrings(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                 <![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class $$Program
{
    /// <summary>
    /// <Program></Program>
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        // Program
        /* Program!
            Program
        */
        var a = "Program";
    }
}
]]>
                             </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 7),
                    renameInStrings:=True,
                    renameInComments:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function NonConflictingEditWithMultipleLocations(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                class $$Goo
                                {
                                    void Blah()
                                    {
                                        Goo f = new Goo();
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3))
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function NonConflictingEditWithSingleLocation(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                class $$UniqueClassName
                                {
                                    void Blah()
                                    {
                                        Goo f = new Goo();
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function ParameterConflictingWithInstanceField(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Goo
                               {
                                   int goo;
                                   void Blah(int [|$$bar|])
                                   {
                                       goo = [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>), host:=host,
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                severity:=RenameDashboardSeverity.Info)
        End Function

        <WorkItem(5923, "DevDiv_Projects/Roslyn")>
        <WpfTheory>
        <CombinatorialData>
        Public Async Function ParameterConflictingWithInstanceFieldMoreThanOnce(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Goo
                               {
                                   int goo;
                                   void Blah(int [|$$bar|])
                                   {
                                       goo = goo + [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>), host:=host,
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 2),
                severity:=RenameDashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function ParameterConflictingWithLocal_Unresolvable(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Goo
                               {
                                   void Blah(int [|$$bar|])
                                   {
                                       int goo;
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>), host:=host,
                newName:="goo",
                searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 1),
                severity:=RenameDashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function MoreThanOneUnresolvableConflicts(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Goo
                               {
                                   void Blah(int [|$$bar|])
                                   {
                                       int goo;
                                       goo = [|bar|];
                                       goo = [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>), host:=host,
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3),
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 3),
                severity:=RenameDashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function ConflictsAcrossLanguages_Resolvable(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                         <Document>
                               namespace N
                               {
                                    public class [|$$Goo|]
                                    {
                                        void Blah()
                                        {
                                            [|Goo|] f = new [|Goo|]();
                                        }
                                    }
                               }
                           </Document>
                     </Project>
                     <Project Language="Visual Basic" CommonReferences="true">
                         <ProjectReference>CSharpAssembly</ProjectReference>
                         <Document>
                               Imports N
                               Class Bar
                                   Sub Blah()
                                      Dim f = new {|N.Goo:Goo|}()
                                   End Sub
                               End Class
                           </Document>
                     </Project>
                 </Workspace>), host:=host,
                   newName:="Bar",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_files, 4, 2),
                   resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                   severity:=RenameDashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithNameof_FromDefinition_DoesNotForceRenameOverloadsOption(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                         <Document>
class C
{
    void M$$()
    {
        nameof(M).ToString();
    }
    void M(int x) { }
}
                        </Document>
                     </Project>
                 </Workspace>), host:=host,
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_1_reference_in_1_file),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithNameof_FromReference_DoesForceRenameOverloadsOption(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                         <Document>
class C
{
    void M()
    {
        nameof(M$$).ToString();
    }
    void M(int x) { }
}
                        </Document>
                     </Project>
                 </Workspace>), host:=host,
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=False)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithNameof_FromDefinition_WithRenameOverloads_Cascading(host As RenameTestHost) As Task
            Await VerifyViewModels(
                (<Workspace>
                     <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                         <Document>
class B
{
    public virtual void [|M|](int x)
    {
        nameof([|M|]).ToString();
    }
}

class D : B
{
    public void $$[|M|]()
    {
        nameof([|M|]).ToString();
    }

    public override void [|M|](int x)
    {
    }
}
                        </Document>
                     </Project>
                 </Workspace>), host:=host,
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 5),
                   renameOverloads:=True,
                   hasRenameOverload:=True)
        End Function

        Friend Shared Async Function VerifyViewModels(
            test As XElement,
            newName As String,
            searchResultText As String,
            host As RenameTestHost,
            Optional hasRenameOverload As Boolean = False,
            Optional isRenameOverloadsEditable As Boolean = True,
            Optional renameOverloads As Boolean = False,
            Optional renameInStrings As Boolean = False,
            Optional renameInComments As Boolean = False,
            Optional renameFile As Boolean = False,
            Optional resolvableConflictText As String = Nothing,
            Optional unresolvableConflictText As String = Nothing,
            Optional severity As RenameDashboardSeverity = RenameDashboardSeverity.None,
            Optional executionPreference As SourceGeneratorExecutionPreference = SourceGeneratorExecutionPreference.Automatic) As Task

            Using workspace = CreateWorkspaceWithWaiter(test, host)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, renameOverloads)
                globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, renameInStrings)
                globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, renameInComments)
                globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, renameFile)

                Dim configService = workspace.ExportProvider.GetExportedValue(Of TestWorkspaceConfigurationService)
                configService.Options = New WorkspaceConfigurationOptions(SourceGeneratorExecution:=executionPreference)

                Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Assert.NotNull(document)

                Dim token = document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(cursorPosition)

                Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)

                ' Create views for all documents to ensure that undo is hooked up properly
                For Each d In workspace.Documents
                    d.GetTextView()
                Next

                Dim sessionInfo = renameService.StartInlineSession(
                    document, document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(cursorPosition).Span, CancellationToken.None)

                ' Perform the edit in the buffer
                Using edit = cursorDocument.GetTextBuffer().CreateEdit()
                    edit.Replace(token.SpanStart, token.Span.Length, newName)
                    edit.Apply()
                End Using

                Dim threadingContext = workspace.ExportProvider.GetExport(Of IThreadingContext)().Value

                Using dashboard = New RenameDashboard(
                    New RenameDashboardViewModel(DirectCast(sessionInfo.Session, InlineRenameSession), threadingContext),
                    editorFormatMapService:=Nothing,
                    textView:=cursorDocument.GetTextView())

                    Await WaitForRename(workspace)

                    Dim model = DirectCast(dashboard.DataContext, RenameDashboardViewModel)

                    Assert.Equal(searchResultText, model.SearchText)

                    If String.IsNullOrEmpty(resolvableConflictText) Then
                        Assert.False(model.HasResolvableConflicts, "Expected no resolvable conflicts")
                        Assert.Null(model.ResolvableConflictText)
                    Else
                        Assert.True(model.HasResolvableConflicts, "Expected resolvable conflicts")
                        Assert.Equal(resolvableConflictText, model.ResolvableConflictText)
                    End If

                    If String.IsNullOrEmpty(unresolvableConflictText) Then
                        Assert.False(model.HasUnresolvableConflicts, "Expected no unresolvable conflicts")
                        Assert.Null(model.UnresolvableConflictText)
                    Else
                        Assert.True(model.HasUnresolvableConflicts, "Expected unresolvable conflicts")
                        Assert.Equal(unresolvableConflictText, model.UnresolvableConflictText)
                    End If

                    Assert.Equal(hasRenameOverload, model.Session.HasRenameOverloads)
                    Assert.Equal(isRenameOverloadsEditable, model.IsRenameOverloadsEditable)
                    If Not isRenameOverloadsEditable Then
                        Assert.True(model.DefaultRenameOverloadFlag)
                    End If

                    Assert.Equal(severity, model.Severity)
                End Using

                Dim TestQuickInfoBroker = New TestQuickInfoBroker()
                Dim listenerProvider = workspace.ExportProvider.GetExport(Of IAsynchronousOperationListenerProvider)().Value
                Dim editorFormatMapService = workspace.ExportProvider.GetExport(Of IEditorFormatMapService)().Value

                Using flyout = New RenameFlyout(
                    New RenameFlyoutViewModel(DirectCast(sessionInfo.Session, InlineRenameSession), selectionSpan:=Nothing, registerOleComponent:=False, globalOptions, threadingContext, listenerProvider, Nothing), ' Don't registerOleComponent in tests, it requires OleComponentManagers that don't exist in our host
                    textView:=cursorDocument.GetTextView(),
                    themeService:=Nothing,
                    TestQuickInfoBroker,
                    editorFormatMapService,
                    threadingContext,
                    listenerProvider)

                    Await WaitForRename(workspace)

                    Dim model = DirectCast(flyout.DataContext, RenameFlyoutViewModel)

                    Assert.Equal(hasRenameOverload, model.Session.HasRenameOverloads)
                    Assert.Equal(hasRenameOverload, model.IsRenameOverloadsVisible)
                    Assert.Equal(isRenameOverloadsEditable, model.IsRenameOverloadsEditable)
                    If Not isRenameOverloadsEditable Then
                        Assert.True(model.RenameOverloadsFlag)
                    End If

                    Dim waiter = listenerProvider.GetWaiter(FeatureAttribute.InlineRenameFlyout)
                    Await waiter.ExpeditedWaitAsync()

                    Dim QuickInfoSession = DirectCast(TestQuickInfoBroker.GetSession(cursorDocument.GetTextView()), TestQuickInfoBroker.TestSession)
                    Assert.True(QuickInfoSession.Dismissed)
                End Using

                sessionInfo.Session.Cancel()
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function RenameWithReferenceInUnchangeableDocument(host As RenameTestHost) As Task
            Await VerifyViewModels(
                    (<Workspace>
                         <Project Language="C#">
                             <Document>
                            public class $$A
                            {
                            }
                        </Document>
                             <Document CanApplyChange="false">
                            class B
                            {
                                void M()
                                {
                                    A a;
                                }
                            }
                        </Document>
                         </Project>
                     </Workspace>), host:=host,
                    newName:="C",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                    renameOverloads:=True)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Sub RenameFlyoutRemembersCollapsedState(host As RenameTestHost)
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
                                class Program
                                {
                                    public void $$goo()
                                    {
                                    }
                                }
                            </Document>
                           </Project>
                       </Workspace>

            Using workspace = CreateWorkspaceWithWaiter(test, host)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.CollapseUI, False)

                Dim cursorDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Assert.NotNull(document)

                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim sessionInfo = renameService.StartInlineSession(
                    document, document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(cursorPosition).Span, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExport(Of IAsynchronousOperationListenerProvider)().Value
                Dim threadingContext = workspace.ExportProvider.GetExport(Of IThreadingContext)().Value

                Dim vm = New RenameFlyoutViewModel(DirectCast(sessionInfo.Session, InlineRenameSession), selectionSpan:=Nothing, registerOleComponent:=False, globalOptions, threadingContext, listenerProvider, Nothing) ' Don't registerOleComponent in tests, it requires OleComponentManagers that don't exist in our host
                Assert.False(vm.IsCollapsed)
                Assert.True(vm.IsExpanded)
                vm.IsCollapsed = True

                vm = New RenameFlyoutViewModel(DirectCast(sessionInfo.Session, InlineRenameSession), selectionSpan:=Nothing, registerOleComponent:=False, globalOptions, threadingContext, listenerProvider, Nothing) ' Don't registerOleComponent in tests, it requires OleComponentManagers that don't exist in our host
                Assert.True(vm.IsCollapsed)
                Assert.False(vm.IsExpanded)

            End Using
        End Sub
    End Class

    Friend Class TestQuickInfoBroker
        Implements IAsyncQuickInfoBroker

        Private ReadOnly Session As TestSession = New TestSession()

        Public Function IsQuickInfoActive(textView As VisualStudio.Text.Editor.ITextView) As Boolean Implements IAsyncQuickInfoBroker.IsQuickInfoActive
            Return False
        End Function

        Public Function TriggerQuickInfoAsync(textView As VisualStudio.Text.Editor.ITextView, Optional triggerPoint As VisualStudio.Text.ITrackingPoint = Nothing, Optional options As QuickInfoSessionOptions = QuickInfoSessionOptions.None, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IAsyncQuickInfoSession) Implements IAsyncQuickInfoBroker.TriggerQuickInfoAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetQuickInfoItemsAsync(textView As VisualStudio.Text.Editor.ITextView, triggerPoint As VisualStudio.Text.ITrackingPoint, cancellationToken As CancellationToken) As Task(Of QuickInfoItemsCollection) Implements IAsyncQuickInfoBroker.GetQuickInfoItemsAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetSession(textView As VisualStudio.Text.Editor.ITextView) As IAsyncQuickInfoSession Implements IAsyncQuickInfoBroker.GetSession
            Return Session
        End Function

        Public Class TestSession
            Implements IAsyncQuickInfoSession

            Public Dismissed As Boolean = False

            Public ReadOnly Property ApplicableToSpan As VisualStudio.Text.ITrackingSpan Implements IAsyncQuickInfoSession.ApplicableToSpan
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Content As IEnumerable(Of Object) Implements IAsyncQuickInfoSession.Content
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property HasInteractiveContent As Boolean Implements IAsyncQuickInfoSession.HasInteractiveContent
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Options As QuickInfoSessionOptions Implements IAsyncQuickInfoSession.Options
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property State As QuickInfoSessionState Implements IAsyncQuickInfoSession.State
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property TextView As VisualStudio.Text.Editor.ITextView Implements IAsyncQuickInfoSession.TextView
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Properties As PropertyCollection Implements IPropertyOwner.Properties
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Event StateChanged As EventHandler(Of QuickInfoSessionStateChangedEventArgs) Implements IAsyncQuickInfoSession.StateChanged

            Public Function GetTriggerPoint(textBuffer As VisualStudio.Text.ITextBuffer) As VisualStudio.Text.ITrackingPoint Implements IAsyncQuickInfoSession.GetTriggerPoint
                Throw New NotImplementedException()
            End Function

            Public Function GetTriggerPoint(snapshot As VisualStudio.Text.ITextSnapshot) As VisualStudio.Text.SnapshotPoint? Implements IAsyncQuickInfoSession.GetTriggerPoint
                Throw New NotImplementedException()
            End Function

            Public Function DismissAsync() As Task Implements IAsyncQuickInfoSession.DismissAsync
                Dismissed = True

                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
