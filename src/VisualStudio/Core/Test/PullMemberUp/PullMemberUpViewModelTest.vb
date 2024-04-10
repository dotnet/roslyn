' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PullMemberUp
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
Imports Microsoft.VisualStudio.LanguageServices.Utilities
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.PullMemberUp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
    Public Class PullMemberUpViewModelTest
        <Fact>
        Public Async Function TestPullMemberUp_VerifySameBaseTypeAppearMultipleTimes() As Task
            Dim markUp = <Text><![CDATA[
interface Level2Interface
{
}

interface Level1Interface : Level2Interface
{
}

class Level1BaseClass: Level2Interface
{
}

class MyClass : Level1BaseClass, Level1Interface
{
    public void G$$oo()
    {
    }
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            Dim baseTypeTree = viewModel.DestinationTreeNodeViewModel.BaseTypeNodes
            Assert.Equal("Level1BaseClass", baseTypeTree(0).SymbolName)
            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            Assert.Equal("Level2Interface", baseTypeTree(0).BaseTypeNodes(0).SymbolName)
            Assert.Equal("Level2Interface", baseTypeTree(1).BaseTypeNodes(0).SymbolName)
            Assert.Empty(baseTypeTree(0).BaseTypeNodes(0).BaseTypeNodes)
            Assert.Empty(baseTypeTree(1).BaseTypeNodes(0).BaseTypeNodes)

            Assert.False(viewModel.OkButtonEnabled)
            viewModel.SelectedDestination = baseTypeTree(0)
            Assert.True(viewModel.OkButtonEnabled)
        End Function

        <Fact>
        Public Async Function TestPullMemberUp_NoVBDestinationAppearInCSharpProject() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
            using VBAssembly;
            public interface ITestInterface
            {
            }

            public class TestClass : VBClass, ITestInterface
            {
                public int Bar$$bar()
                {
                    return 12345;
                }
            }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
            Public Class VBClass
            End Class
        </Document>
    </Project>
</Workspace>]]></Text>

            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            Dim baseTypeTree = viewModel.DestinationTreeNodeViewModel.BaseTypeNodes

            ' C# types will be showed
            Assert.Equal("ITestInterface", baseTypeTree(0).SymbolName)
            ' Make sure Visual basic types are not showed since we are not ready to support cross language scenario
            Assert.Single(baseTypeTree)
        End Function

        <Fact>
        Public Async Function TestPullMemberUp_SelectInterfaceDisableMakeAbstractCheckbox() As Task
            Dim markUp = <Text><![CDATA[
interface Level2Interface
{
}

interface Level1Interface : Level2Interface
{
}

class Level1BaseClass: Level2Interface
{
}

class MyClass : Level1BaseClass, Level1Interface
{
    public void G$$oo()
    {
    }

    public double e => 2.717;

    public const days = 365;

    private double pi => 3.1416;

    protected float goldenRadio = 0.618;

    internal float gravitational = 6.67e-11;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            Dim baseTypeTree = viewModel.DestinationTreeNodeViewModel.BaseTypeNodes

            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            For Each member In viewModel.MemberSelectionViewModel.Members.WhereAsArray(
                Function(memberViewModel)
                    Return Not memberViewModel.Symbol.IsKind(SymbolKind.Field) And Not memberViewModel.Symbol.IsAbstract
                End Function)
                Assert.False(member.IsMakeAbstractCheckable)
            Next
        End Function

        <Fact>
        Public Async Function TestPullMemberUp_SelectInterfaceDisableFieldCheckbox() As Task
            Dim markUp = <Text><![CDATA[
interface Level2Interface
{
}

interface Level1Interface : Level2Interface
{
}

class Level1BaseClass: Level2Interface
{
}

class MyClass : Level1BaseClass, Level1Interface
{
    public void G$$oo()
    {
    }

    public double e => 2.717;

    public const days = 365;

    private double pi => 3.1416;

    protected float goldenRadio = 0.618;

    internal float gravitational = 6.67e-11;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            Dim baseTypeTree = viewModel.DestinationTreeNodeViewModel.BaseTypeNodes

            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            For Each member In viewModel.MemberSelectionViewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.IsKind(SymbolKind.Field))
                Assert.False(member.IsCheckable)
                Assert.False(String.IsNullOrEmpty(member.TooltipText))
            Next
        End Function

        <Fact>
        Public Async Function TestPullMemberUp_SelectClassEnableFieldCheckbox() As Task
            Dim markUp = <Text><![CDATA[
interface Level2Interface
{
}

interface Level1Interface : Level2Interface
{
}

class Level1BaseClass: Level2Interface
{
}

class MyClass : Level1BaseClass, Level1Interface
{
    public void G$$oo()
    {
    }

    public double e => 2.717;

    public const days = 365;

    private double pi => 3.1416;

    protected float goldenRadio = 0.618;

    internal float gravitational = 6.67e-11;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            Dim baseTypeTree = viewModel.DestinationTreeNodeViewModel.BaseTypeNodes

            ' First select an interface, all checkbox will be disable as the previous test.
            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            ' Second select a class, check all checkboxs will be resumed.
            Assert.Equal("Level1BaseClass", baseTypeTree(0).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(0)
            For Each member In viewModel.MemberSelectionViewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.IsKind(SymbolKind.Field))
                Assert.True(member.IsCheckable)
                Assert.True(String.IsNullOrEmpty(member.TooltipText))
            Next
        End Function

        Private Shared Function FindMemberByName(name As String, memberArray As ImmutableArray(Of MemberSymbolViewModel)) As MemberSymbolViewModel
            Dim member = memberArray.FirstOrDefault(Function(memberViewModel) memberViewModel.SymbolName.Equals(name))
            If (member Is Nothing) Then
                Assert.True(False, $"No member called {name} found")
            End If

            Return member
        End Function

        Private Shared Async Function GetViewModelAsync(markup As XElement, languageName As String) As Task(Of PullMemberUpDialogViewModel)
            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.Value %></Document>
                </Project>
            </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Throw New ArgumentException("Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.Services.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim memberSymbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                Dim baseTypeTree = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(glyphService:=Nothing, workspaceDoc.Project.Solution, memberSymbol.ContainingType, CancellationToken.None)
                Dim membersInType = memberSymbol.ContainingType.GetMembers().WhereAsArray(Function(member) MemberAndDestinationValidator.IsMemberValid(member))
                Dim membersViewModel = membersInType.SelectAsArray(
                    Function(member) New MemberSymbolViewModel(member, glyphService:=Nothing) With {.IsChecked = member.Equals(memberSymbol), .IsCheckable = True, .MakeAbstract = False})
                Dim memberToDependents = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, workspaceDoc.Project, CancellationToken.None)
                Return New PullMemberUpDialogViewModel(
                    workspace.GetService(Of IUIThreadOperationExecutor),
                    membersViewModel,
                    baseTypeTree,
                    memberToDependents)
            End Using
        End Function
    End Class
End Namespace
