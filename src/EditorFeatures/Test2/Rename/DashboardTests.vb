' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <[UseExportProvider]>
    Public Class DashboardTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNoOverload() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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
                     </Workspace>),
                    newName:="",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithOverload() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                    hasRenameOverload:=True,
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <WorkItem(883263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883263")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithInvalidOverload() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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
                newName:="Bar",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                changedOptionSet:=changingOptions,
                hasRenameOverload:=True,
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 1),
                severity:=DashboardSeverity.Error)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(853839, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/853839")>
        Public Async Function RenameAttributeAlias() As Task
            Await VerifyDashboard(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
using $$Evil = AttributeAttribute; 
[AttributeAttribute] 
class AttributeAttribute : System.Attribute { }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="AttributeAttributeAttribute",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                    resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                    severity:=DashboardSeverity.Info)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameWithOverloadAndInStringsAndComments() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
            changingOptions.Add(RenameOptions.RenameInStrings, True)
            changingOptions.Add(RenameOptions.RenameInComments, True)
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
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 5),
                    hasRenameOverload:=True,
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInComments() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameInComments, True)
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
                     </Workspace>),
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 6),
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInStrings() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameInStrings, True)
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
                     </Workspace>),
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700923"), WorkItem(700925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/700925")>
        Public Async Function RenameInCommentsAndStrings() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameInComments, True)
            changingOptions.Add(RenameOptions.RenameInStrings, True)
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
                     </Workspace>),
                    newName:="P",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 7),
                    changedOptionSet:=changingOptions)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function NonConflictingEditWithMultipleLocations() As Task
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
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3))
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function NonConflictingEditWithSingleLocation() As Task
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
                     </Workspace>),
                    newName:="",
                    searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceField() As Task
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
                 </Workspace>),
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                severity:=DashboardSeverity.Info)
        End Function

        <WorkItem(5923, "DevDiv_Projects/Roslyn")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceFieldMoreThanOnce() As Task
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
                 </Workspace>),
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 2),
                severity:=DashboardSeverity.Info)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithLocal_Unresolvable() As Task
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
                 </Workspace>),
                newName:="goo",
                searchResultText:=EditorFeaturesResources.Rename_will_update_1_reference_in_1_file,
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 1),
                severity:=DashboardSeverity.Error)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function MoreThanOneUnresolvableConflicts() As Task
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
                 </Workspace>),
                newName:="goo",
                searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3),
                unresolvableConflictText:=String.Format(EditorFeaturesResources._0_unresolvable_conflict_s, 3),
                severity:=DashboardSeverity.Error)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ConflictsAcrossLanguages_Resolvable() As Task
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
                 </Workspace>),
                   newName:="Bar",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_files, 4, 2),
                   resolvableConflictText:=String.Format(EditorFeaturesResources._0_conflict_s_will_be_resolved, 1),
                   severity:=DashboardSeverity.Info)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromDefinition_DoesNotForceRenameOverloadsOption() As Task
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
                 </Workspace>),
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_1_reference_in_1_file),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromReference_DoesForceRenameOverloadsOption() As Task
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
                 </Workspace>),
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 3),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function RenameWithNameof_FromDefinition_WithRenameOverloads_Cascading() As Task
            Dim changingOptions = New Dictionary(Of OptionKey, Object)()
            changingOptions.Add(RenameOptions.RenameOverloads, True)
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
                 </Workspace>),
                   newName:="Mo",
                   searchResultText:=String.Format(EditorFeaturesResources.Rename_will_update_0_references_in_1_file, 5),
                   changedOptionSet:=changingOptions,
                   hasRenameOverload:=True)
        End Function

        Friend Shared Async Function VerifyDashboard(
            test As XElement,
            newName As String,
            searchResultText As String,
            Optional hasRenameOverload As Boolean = False,
            Optional isRenameOverloadsEditable As Boolean = True,
            Optional changedOptionSet As Dictionary(Of OptionKey, Object) = Nothing,
            Optional resolvableConflictText As String = Nothing,
            Optional unresolvableConflictText As String = Nothing,
            Optional severity As DashboardSeverity = DashboardSeverity.None
        ) As Tasks.Task

            Using workspace = CreateWorkspaceWithWaiter(test)
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

                Dim optionSet = workspace.Options

                If changedOptionSet IsNot Nothing Then
                    For Each entry In changedOptionSet
                        optionSet = optionSet.WithChangedOption(entry.Key, entry.Value)
                    Next
                End If

                workspace.Options = optionSet

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

    End Class
End Namespace
