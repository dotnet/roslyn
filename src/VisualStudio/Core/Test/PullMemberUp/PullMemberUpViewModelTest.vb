' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PullMemberUp
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.PullMemberUp
    <[UseExportProvider]>
    Public Class PullMemberUpViewModelTest
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
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
            Dim baseTypeTree = viewModel.Destinations()
            Assert.Equal("Level1BaseClass", baseTypeTree(0).SymbolName)
            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            Assert.Equal("Level2Interface", baseTypeTree(0).BaseTypeNodes(0).SymbolName)
            Assert.Equal("Level2Interface", baseTypeTree(1).BaseTypeNodes(0).SymbolName)
            Assert.Empty(baseTypeTree(0).BaseTypeNodes(0).BaseTypeNodes)
            Assert.Empty(baseTypeTree(1).BaseTypeNodes(0).BaseTypeNodes)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
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
            Dim baseTypeTree = viewModel.Destinations

            ' C# types will be showed
            Assert.Equal("ITestInterface", baseTypeTree(0).SymbolName)
            ' Make sure Visual basic types are not showed since we are not ready to support cross language scenario
            Assert.Single(baseTypeTree)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
        Public Async Function TestPullMemberUp_SelectAllMemberMakeSelectAllBecomeChecked() As Task
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

    private double pi => 3.1416;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            viewModel.SelectAllMembers()

            Assert.True(viewModel.SelectAllCheckBoxState)
            Assert.False(viewModel.SelectAllCheckBoxThreeStateEnable)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
        Public Async Function TestPullMemberUp_DeSelectAllMemberMakeSelectAllBecomeEmpty() As Task
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

    private double pi => 3.1416;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            viewModel.DeSelectAllMembers()

            Assert.False(viewModel.SelectAllCheckBoxState)
            Assert.False(viewModel.SelectAllCheckBoxThreeStateEnable)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
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
            Dim baseTypeTree = viewModel.Destinations()

            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            For Each member In viewModel.Members.WhereAsArray(
                Function(memberViewModel)
                    Return Not memberViewModel.Symbol.IsKind(SymbolKind.Field) And Not memberViewModel.Symbol.IsAbstract
                End Function)
                Assert.False(member.IsMakeAbstractCheckable)
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
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
            Dim baseTypeTree = viewModel.Destinations()

            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            For Each member In viewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.IsKind(SymbolKind.Field))
                Assert.False(member.IsCheckable)
                Assert.False(String.IsNullOrEmpty(member.TooltipText))
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
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
            Dim baseTypeTree = viewModel.Destinations()

            ' First select an interface, all checkbox will be disable as the previous test.
            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            ' Second select a class, check all checkboxs will be resumed.
            Assert.Equal("Level1BaseClass", baseTypeTree(0).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(0)
            For Each member In viewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.IsKind(SymbolKind.Field))
                Assert.True(member.IsCheckable)
                Assert.True(String.IsNullOrEmpty(member.TooltipText))
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
        Public Async Function TestPullMemberUp_SelectPublicMembers() As Task
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
            Dim baseTypeTree = viewModel.Destinations()
            viewModel.SelectPublicMembers()

            For Each member In viewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.DeclaredAccessibility = Microsoft.CodeAnalysis.Accessibility.Public)
                Assert.True(member.IsChecked)
            Next
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
        Public Async Function TestPullMemberUpDont_PullDisableItem() As Task
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
            Dim baseTypeTree = viewModel.Destinations()
            viewModel.SelectAllMembers()
            ' select an interface, all checkbox of field will be disable
            Assert.Equal("Level1Interface", baseTypeTree(1).SymbolName)
            viewModel.SelectedDestination = baseTypeTree(1)

            Dim options = viewModel.CreatePullMemberUpOptions()
            ' Make sure fields are pulled to interface
            Assert.Empty(options.MemberAnalysisResults.WhereAsArray(Function(analysisResult) analysisResult.Member.IsKind(SymbolKind.Field)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
        Public Async Function TestPullMemberUp_SelectDependents() As Task
            Dim markUp = <Text><![CDATA[
using System;

class Level1BaseClass
{
}

class MyClass : Level1BaseClass
{
    private int i = 100;
    
    private event EventHandler FooEvent;

    public void G$$oo()
    {
        int i = BarBar(e);
        What = 1000;
    }

    public int BarBar(double e)
    {
        Nested1();
        return 1000;
    }

    private void Nested1()
    {
        int i = 1000;
        gravitational == 1.0;
    }

    internal float gravitational = 6.67e-11;
    private int What {get; set; }
    public double e => 2.717;
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp, LanguageNames.CSharp)
            viewModel.SelectDependents()

            ' Dependents of Goo
            Assert.True(FindMemberByName("Goo()", viewModel.Members).IsChecked)
            Assert.True(FindMemberByName("e", viewModel.Members).IsChecked)
            Assert.True(FindMemberByName("What", viewModel.Members).IsChecked)
            Assert.True(FindMemberByName("BarBar(double)", viewModel.Members).IsChecked)
            Assert.True(FindMemberByName("Nested1()", viewModel.Members).IsChecked)
            Assert.True(FindMemberByName("gravitational", viewModel.Members).IsChecked)

            ' Not the depenents of Goo
            Assert.False(FindMemberByName("i", viewModel.Members).IsChecked)
            Assert.False(FindMemberByName("FooEvent", viewModel.Members).IsChecked)
        End Function

        Private Function FindMemberByName(name As String, memberArray As ImmutableArray(Of PullMemberUpSymbolViewModel)) As PullMemberUpSymbolViewModel
            Dim member = memberArray.FirstOrDefault(Function(memberViewModel) memberViewModel.SymbolName.Equals(name))
            If (member Is Nothing) Then
                Assert.True(False, $"No member called {name} found")
            End If
            Return member
        End Function

        Private Async Function GetViewModelAsync(markup As XElement, languageName As String) As Task(Of PullMemberUpDialogViewModel)
            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.Value %></Document>
                </Project>
            </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Throw New ArgumentException("Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim memberSymbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                Dim baseTypeTree = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(glyphService:=Nothing, workspaceDoc.Project.Solution, memberSymbol.ContainingType, CancellationToken.None)
                Dim membersInType = memberSymbol.ContainingType.GetMembers().WhereAsArray(Function(member) MemberAndDestinationValidator.IsMemberValid(member))
                Dim membersViewModel = membersInType.SelectAsArray(
                    Function(member) New PullMemberUpSymbolViewModel(member, glyphService:=Nothing) With {.IsChecked = member.Equals(memberSymbol), .IsCheckable = True, .MakeAbstract = False})
                Dim memberToDependents = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, workspaceDoc.Project, CancellationToken.None)
                Return New PullMemberUpDialogViewModel(
                    workspace.GetService(Of IWaitIndicator),
                    membersViewModel,
                    baseTypeTree.BaseTypeNodes,
                    memberToDependents)
            End Using
        End Function
    End Class
End Namespace
