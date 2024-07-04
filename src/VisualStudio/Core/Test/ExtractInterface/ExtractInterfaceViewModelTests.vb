' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Immutable
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Utilities
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ExtractInterface
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.ExtractInterface)>
    Public Class ExtractInterfaceViewModelTests
        <Fact>
        Public Async Function TestExtractInterface_MembersCheckedByDefault() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.True(viewModel.MemberContainers.Single().IsChecked)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122")>
        Public Async Function TestExtractInterface_InterfaceNameIsTrimmedOnSubmit() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IC")
            viewModel.DestinationViewModel.TypeName = "                 IC2     "
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122")>
        Public Async Function TestExtractInterface_FileNameIsTrimmedOnSubmit() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IC")
            viewModel.DestinationViewModel.FileName = "                 IC2.cs     "
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact>
        Public Async Function TestExtractInterface_SuccessfulCommit() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact>
        Public Async Function TestExtractInterface_SuccessfulCommit_NonemptyStrictSubsetOfMembersSelected() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }

    public void Bar()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.MemberContainers.First().IsChecked = False
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_InterfaceNameConflict() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass", conflictingTypeNames:=New List(Of String) From {"IMyClass"})
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_InterfaceNameNotAnIdentifier() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.DestinationViewModel.TypeName = "SomeNamespace.IMyClass"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_BadFileExtension() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.DestinationViewModel.FileName = "FileName.vb"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_BadFileName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.DestinationViewModel.FileName = "Bad*FileName.cs"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_BadFileName2() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.DestinationViewModel.FileName = "?BadFileName.cs"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_FailedCommit_NoMembersSelected() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.MemberContainers.Single().IsChecked = False
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_MemberDisplay_Method() As Task
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public void Goo<T>(T t, System.Diagnostics.CorrelationManager v, ref int w, Nullable<System.Int32> x = 7, string y = "hi", params int[] z)
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("Goo<T>(T, CorrelationManager, ref int, [int?], [string], params int[])", viewModel.MemberContainers.Single().SymbolName)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_MemberDisplay_Property() As Task
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public int Goo
    {
        get { return 5; }
        set { }
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("Goo", viewModel.MemberContainers.Where(Function(c) c.Symbol.IsKind(SymbolKind.Property)).Single().SymbolName)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_MemberDisplay_Indexer() As Task
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public int this[Nullable<Int32> x, string y = "hi"] { get { return 1; } set { } }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("this[int?, [string]]", viewModel.MemberContainers.Where(Function(c) c.Symbol.IsKind(SymbolKind.Property)).Single().SymbolName)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37176")>
        Public Async Function TestExtractInterface_MemberDisplay_NullableReferenceType() As Task
            Dim markup = <Text><![CDATA[
#nullable enable
using System.Collections.Generic;
class $$MyClass
{
    public void M(string? s, IEnumerable<string?> e) { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("M(string?, IEnumerable<string?>)", viewModel.MemberContainers.Single(Function(c) c.Symbol.IsKind(SymbolKind.Method)).SymbolName)
        End Function

        <Fact>
        Public Async Function TestExtractInterface_MembersSorted() As Task
            Dim markup = <Text><![CDATA[
public class $$MyClass
{
    public void Goo(string s) { }
    public void Goo(int i) { }
    public void Goo(int i, string s) { }
    public void Goo() { }
    public void Goo(int i, int i2) { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal(5, viewModel.MemberContainers.Count)
            Assert.Equal("Goo()", viewModel.MemberContainers.ElementAt(0).SymbolName)
            Assert.Equal("Goo(int)", viewModel.MemberContainers.ElementAt(1).SymbolName)
            Assert.Equal("Goo(int, int)", viewModel.MemberContainers.ElementAt(2).SymbolName)
            Assert.Equal("Goo(int, string)", viewModel.MemberContainers.ElementAt(3).SymbolName)
            Assert.Equal("Goo(string)", viewModel.MemberContainers.ElementAt(4).SymbolName)
        End Function

        <Fact>
        Public Async Function TestDestinationChanged() As Task
            Dim markup = <Text><![CDATA[
public class $$MyClass
{
    public void Goo(string s) { }
    public void Goo(int i) { }
    public void Goo(int i, string s) { }
    public void Goo() { }
    public void Goo(int i, int i2) { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal(NewTypeDestination.NewFile, viewModel.DestinationViewModel.Destination)
            viewModel.DestinationViewModel.Destination = NewTypeDestination.CurrentFile
            Dim newViewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal(viewModel.DestinationViewModel.Destination, newViewModel.DestinationViewModel.Destination)
        End Function

        Private Shared Async Function GetViewModelAsync(markup As XElement,
                              languageName As String,
                              defaultInterfaceName As String,
                              Optional defaultNamespace As String = "",
                              Optional generatedNameTypeParameterSuffix As String = "",
                              Optional conflictingTypeNames As List(Of String) = Nothing,
                              Optional isValidIdentifier As Boolean = True) As Tasks.Task(Of ExtractInterfaceDialogViewModel)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.Services.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim symbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                Dim extractableMembers = DirectCast(symbol, INamedTypeSymbol).GetMembers().Where(Function(s) Not (TypeOf s Is IMethodSymbol) OrElse DirectCast(s, IMethodSymbol).MethodKind <> MethodKind.Constructor)

                Dim memberViewModels = extractableMembers.Select(Function(member As ISymbol)
                                                                     Return New MemberSymbolViewModel(member, Nothing)
                                                                 End Function)

                Return New ExtractInterfaceDialogViewModel(
                    workspaceDoc.Project.Services.GetService(Of ISyntaxFactsService)(),
                    notificationService:=New TestNotificationService(),
                    uiThreadOperationExecutor:=Nothing,
                    defaultInterfaceName:=defaultInterfaceName,
                    conflictingTypeNames:=If(conflictingTypeNames, New List(Of String)),
                    memberViewModels:=memberViewModels.ToImmutableArray(),
                    defaultNamespace:=defaultNamespace,
                    generatedNameTypeParameterSuffix:=generatedNameTypeParameterSuffix,
                    languageName:=doc.Project.Language,
                    globalOptionService:=workspace.GetService(Of IGlobalOptionService))
            End Using
        End Function
    End Class
End Namespace
