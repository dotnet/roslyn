' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.InlineRename
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    Public Class DashboardTests
        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNoOverload(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithOverload(host As RenameTestHost) As Task
            Await VerifyDashboard(
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

        <WpfTheory>
        <WorkItem(883263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883263")>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithInvalidOverload(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                severity:=DashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(853839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853839")>
        Public Async Function RenameAttributeAlias(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                    severity:=DashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameWithOverloadAndInStringsAndComments(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInComments(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInStrings(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInCommentsAndStrings(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function NonConflictingEditWithMultipleLocations(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function NonConflictingEditWithSingleLocation(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceField(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                severity:=DashboardSeverity.Info)
        End Function

        <WorkItem(5923, "DevDiv_Projects/Roslyn")>
        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceFieldMoreThanOnce(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                severity:=DashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithLocal_Unresolvable(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                severity:=DashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function MoreThanOneUnresolvableConflicts(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                severity:=DashboardSeverity.Error)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ConflictsAcrossLanguages_Resolvable(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
                   severity:=DashboardSeverity.Info)
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromDefinition_DoesNotForceRenameOverloadsOption(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromReference_DoesForceRenameOverloadsOption(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromDefinition_WithRenameOverloads_Cascading(host As RenameTestHost) As Task
            Await VerifyDashboard(
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

        Friend Shared Async Function VerifyDashboard(
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
            Optional severity As DashboardSeverity = DashboardSeverity.None
        ) As Tasks.Task

            Using workspace = CreateWorkspaceWithWaiter(test, host)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)()
                globalOptions.SetGlobalOption(New OptionKey(InlineRenameSessionOptionsStorage.RenameOverloads), renameOverloads)
                globalOptions.SetGlobalOption(New OptionKey(InlineRenameSessionOptionsStorage.RenameInStrings), renameInStrings)
                globalOptions.SetGlobalOption(New OptionKey(InlineRenameSessionOptionsStorage.RenameInComments), renameInComments)
                globalOptions.SetGlobalOption(New OptionKey(InlineRenameSessionOptionsStorage.RenameFile), renameFile)

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

                Using dashboard = New Dashboard(
                    New DashboardViewModel(DirectCast(sessionInfo.Session, InlineRenameSession)),
                    editorFormatMapService:=Nothing,
                    textView:=cursorDocument.GetTextView())

                    Await WaitForRename(workspace)

                    Dim model = DirectCast(dashboard.DataContext, DashboardViewModel)

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

                sessionInfo.Session.Cancel()
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithReferenceInUnchangeableDocument(host As RenameTestHost) As Task
            Await VerifyDashboard(
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
    End Class
End Namespace
