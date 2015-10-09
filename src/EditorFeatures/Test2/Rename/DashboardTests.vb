' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Shared.TestHooks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
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
                                    public void $$foo()
                                    {
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="",
                    searchResultText:=EditorFeaturesResources.RenameWillUpdateReferenceInFile,
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
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
                                    public void $$foo()
                                    {
                                    }

                                    public void foo(int i)
                                    {
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 2),
                    hasRenameOverload:=True,
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
        End Function

        <WpfFact>
        <WorkItem(883263)>
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
                searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 2),
                changedOptionSet:=changingOptions,
                hasRenameOverload:=True,
                unresolvableConflictText:=String.Format(EditorFeaturesResources.UnresolvableConflicts, 1),
                severity:=DashboardSeverity.Error).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(853839)>
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
                    searchResultText:=EditorFeaturesResources.RenameWillUpdateReferenceInFile,
                    resolvableConflictText:=String.Format(EditorFeaturesResources.ConflictsWillBeResolved, 1),
                    severity:=DashboardSeverity.Info).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925)>
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
                                    public void $$foo()
                                    {
                                    }

                                    /// foo
                                    public void foo(int i)
                                    {
                                        // foo
                                        var a = "foo";
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 5),
                    hasRenameOverload:=True,
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925)>
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
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 6),
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925)>
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
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 2),
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <WorkItem(700923), WorkItem(700925)>
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
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 7),
                    changedOptionSet:=changingOptions).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function NonConflictingEditWithMultipleLocations() As Task
            Await VerifyDashboard(
                    (<Workspace>
                         <Project Language="C#" CommonReferences="true">
                             <Document>
                                class $$Foo
                                {
                                    void Blah()
                                    {
                                        Foo f = new Foo();
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="",
                    searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 3)).ConfigureAwait(True)
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
                                        Foo f = new Foo();
                                    }
                                }
                            </Document>
                         </Project>
                     </Workspace>),
                    newName:="",
                    searchResultText:=EditorFeaturesResources.RenameWillUpdateReferenceInFile).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceField() As Task
            Await VerifyDashboard(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Foo
                               {
                                   int foo;
                                   void Blah(int [|$$bar|])
                                   {
                                       foo = [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>),
                newName:="foo",
                searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources.ConflictsWillBeResolved, 1),
                severity:=DashboardSeverity.Info).ConfigureAwait(True)
        End Function

        <WorkItem(5923, "DevDiv_Projects/Roslyn")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithInstanceFieldMoreThanOnce() As Task
            Await VerifyDashboard(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Foo
                               {
                                   int foo;
                                   void Blah(int [|$$bar|])
                                   {
                                       foo = foo + [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>),
                newName:="foo",
                searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 2),
                resolvableConflictText:=String.Format(EditorFeaturesResources.ConflictsWillBeResolved, 2),
                severity:=DashboardSeverity.Info).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function ParameterConflictingWithLocal_Unresolvable() As Task
            Await VerifyDashboard(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Foo
                               {
                                   void Blah(int [|$$bar|])
                                   {
                                       int foo;
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>),
                newName:="foo",
                searchResultText:=EditorFeaturesResources.RenameWillUpdateReferenceInFile,
                unresolvableConflictText:=String.Format(EditorFeaturesResources.UnresolvableConflicts, 1),
                severity:=DashboardSeverity.Error).ConfigureAwait(True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Async Function MoreThanOneUnresolvableConflicts() As Task
            Await VerifyDashboard(
                (<Workspace>
                     <Project Language="C#">
                         <Document>
                               class Foo
                               {
                                   void Blah(int [|$$bar|])
                                   {
                                       int foo;
                                       foo = [|bar|];
                                       foo = [|bar|];
                                   }
                               }
                           </Document>
                     </Project>
                 </Workspace>),
                newName:="foo",
                searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 3),
                unresolvableConflictText:=String.Format(EditorFeaturesResources.UnresolvableConflicts, 3),
                severity:=DashboardSeverity.Error).ConfigureAwait(True)
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
                                    public class [|$$Foo|]
                                    {
                                        void Blah()
                                        {
                                            [|Foo|] f = new [|Foo|]();
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
                                      Dim f = new {|N.Foo:Foo|}()
                                   End Sub
                               End Class
                           </Document>
                     </Project>
                 </Workspace>),
                   newName:="Bar",
                   searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInMultipleFiles, 4, 2),
                   resolvableConflictText:=String.Format(EditorFeaturesResources.ConflictsWillBeResolved, 1),
                   severity:=DashboardSeverity.Info).ConfigureAwait(True)
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
                   searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferenceInFile),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=True).ConfigureAwait(True)
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
                   searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 3),
                   hasRenameOverload:=True,
                   isRenameOverloadsEditable:=False).ConfigureAwait(True)
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
                   searchResultText:=String.Format(EditorFeaturesResources.RenameWillUpdateReferencesInFile, 5),
                   changedOptionSet:=changingOptions,
                   hasRenameOverload:=True).ConfigureAwait(True)
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

                workspace.Services.GetService(Of IOptionService)().SetOptions(optionSet)

                Dim sessionInfo = renameService.StartInlineSession(
                    document, document.GetSyntaxTreeAsync().Result.GetRoot().FindToken(cursorPosition).Span, CancellationToken.None)

                ' Perform the edit in the buffer
                Using edit = cursorDocument.TextBuffer.CreateEdit()
                    edit.Replace(token.SpanStart, token.Span.Length, newName)
                    edit.Apply()
                End Using

                Dim listeners = DirectCast(workspace.ExportProvider.GetExports(Of IAsynchronousOperationListener, FeatureMetadata)(), IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata)))
                Dim renameListener = New AggregateAsynchronousOperationListener(listeners, FeatureAttribute.Rename)

                Using dashboard = New Dashboard(New DashboardViewModel(DirectCast(sessionInfo.Session, InlineRenameSession)), cursorDocument.GetTextView())
                    Await WaitForRename(workspace).ConfigureAwait(True)

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
