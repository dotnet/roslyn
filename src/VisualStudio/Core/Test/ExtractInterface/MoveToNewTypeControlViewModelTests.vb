' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveToNewType
    <[UseExportProvider]>
    Public Class MoveToNewTypeControlViewModelTests
        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_InterfaceNameIsSameAsPassedIn() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            Assert.Equal("IMyClass", destinationViewModel.TypeName)

            Dim monitor = New PropertyChangedTestMonitor(destinationViewModel)
            monitor.AddExpectation(Function() destinationViewModel.FileName)

            destinationViewModel.TypeName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", destinationViewModel.FileName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FileNameHasExpectedExtension() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            Assert.Equal("IMyClass.cs", destinationViewModel.FileName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_MembersCheckedByDefault() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.True(viewModel.Members.Single().IsChecked)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_InterfaceNameChangesUpdateFileName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            Dim monitor = New PropertyChangedTestMonitor(destinationViewModel)
            monitor.AddExpectation(Function() destinationViewModel.FileName)

            destinationViewModel.TypeName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", destinationViewModel.FileName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact>
        <WorkItem(716122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FileNameIsGeneratedFromTrimmedInterfaceName() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.TypeName = "                 IC2     "
            Assert.Equal("IC2.cs", destinationViewModel.FileName)
        End Function

        <Fact>
        <WorkItem(716122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_InterfaceNameIsTrimmedOnSubmit() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.TypeName = "                 IC2     "
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.True(viewModel.OkButtonEnabled, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact>
        <WorkItem(716122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122"), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FileNameIsTrimmedOnSubmit() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            viewModel.SelectAllMembers()
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.FileName = "                 IC2.cs     "
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.True(viewModel.OkButtonEnabled, String.Format("Ok button is not enabled."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FileNameChangesDoNotUpdateInterfaceName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            Dim monitor = New PropertyChangedTestMonitor(destinationViewModel, strict:=True)
            monitor.AddExpectation(Function() destinationViewModel.FileName)

            destinationViewModel.FileName = "IMyClassChanged.cs"
            Assert.Equal("IMyClass", destinationViewModel.TypeName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_SuccessfulCommit() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.True(viewModel.OkButtonEnabled, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
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

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            viewModel.Members.First().IsChecked = False
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.True(viewModel.OkButtonEnabled, String.Format("Submit failed unexpectedly."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_InterfaceNameConflict() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}

interface IMyClass
{
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.TypeName = "IMyClass"
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_InterfaceNameNotAnIdentifier() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.FileName = "SomeNamespace.IMyClass"
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_BadFileExtension() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.FileName = "FileName.vb"
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_BadFileName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.FileName = "Bad*FileName.cs"
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_BadFileName2() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Dim destinationViewModel = CType(viewModel.SelectDestinationViewModel, MoveToNewTypeControlViewModel)
            destinationViewModel.FileName = "?BadFileName.cs"
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_FailedCommit_NoMembersSelected() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            viewModel.Members.Single().IsChecked = False
            viewModel.SetStatesOfOkButtonAndSelectAllCheckBox()
            Assert.False(viewModel.OkButtonEnabled)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_MemberDisplay_Method() As Task
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public void Goo<T>(T t, System.Diagnostics.CorrelationManager v, ref int w, Nullable<System.Int32> x = 7, string y = "hi", params int[] z)
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.Equal("Goo<T>(T, CorrelationManager, ref int, [int?], [string], params int[])", viewModel.Members.Single().SymbolName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
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

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.Equal("Goo", viewModel.Members.Where(Function(c) c.Symbol.IsKind(SymbolKind.Property)).Single().SymbolName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Async Function TestExtractInterface_MemberDisplay_Indexer() As Task
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public int this[Nullable<Int32> x, string y = "hi"] { get { return 1; } set { } }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.Equal("this[int?, [string]]", viewModel.Members.Where(Function(c) c.Symbol.IsKind(SymbolKind.Property)).Single().SymbolName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        <WorkItem(37176, "https://github.com/dotnet/roslyn/issues/37176")>
        Public Async Function TestExtractInterface_MemberDisplay_NullableReferenceType() As Task
            Dim markup = <Text><![CDATA[
#nullable enable
using System.Collections.Generic;
class $$MyClass
{
    public void M(string? s, IEnumerable<string?> e) { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.Equal("M(string?, IEnumerable<string?>)", viewModel.Members.Single(Function(c) c.Symbol.IsKind(SymbolKind.Method)).SymbolName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
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

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp)
            Assert.Equal(5, viewModel.Members.Count)
            Assert.Equal("Goo()", viewModel.Members.ElementAt(0).SymbolName)
            Assert.Equal("Goo(int)", viewModel.Members.ElementAt(1).SymbolName)
            Assert.Equal("Goo(int, int)", viewModel.Members.ElementAt(2).SymbolName)
            Assert.Equal("Goo(int, string)", viewModel.Members.ElementAt(3).SymbolName)
            Assert.Equal("Goo(string)", viewModel.Members.ElementAt(4).SymbolName)
        End Function

        Private Async Function GetViewModelAsync(markup As XElement,
                              languageName As String,
                              Optional defaultNamespace As String = "",
                              Optional generatedNameTypeParameterSuffix As String = "",
                              Optional isValidIdentifier As Boolean = True) As Tasks.Task(Of MoveMembersDialogViewModel)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim symbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                Dim extractableMembers = DirectCast(symbol, INamedTypeSymbol).GetMembers().Where(Function(s) Not (TypeOf s Is IMethodSymbol) OrElse DirectCast(s, IMethodSymbol).MethodKind <> MethodKind.Constructor)

                Return New MoveMembersDialogViewModel(
                    waitIndicator:=Nothing,
                    targetType:=CType(symbol, INamedTypeSymbol),
                    members:=extractableMembers.Select(
                        Function(m)
                            Dim newMember = New MoveMembersSymbolViewModel(m, glyphService:=Nothing)
                            newMember.IsCheckable = True
                            Return newMember
                        End Function).AsImmutable(),
                    dependentsMap:=Nothing,
                    fileExtension:=If(doc.Project.Language = LanguageNames.CSharp, ".cs", ".vb"))
            End Using
        End Function
    End Class
End Namespace
