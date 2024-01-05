' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PullMemberUp
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
Imports Microsoft.VisualStudio.LanguageServices.Utilities
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CommonControls
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.CodeActionsPullMemberUp)>
    Public Class MemberSelectionViewModelTests
        <Fact>
        Public Async Function SelectPublicMembers() As Task
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

            viewModel.SelectPublic()

            For Each member In viewModel.Members.Where(Function(memberViewModel) memberViewModel.Symbol.DeclaredAccessibility = Microsoft.CodeAnalysis.Accessibility.Public)
                Assert.True(member.IsChecked)
            Next
        End Function

        <Fact>
        Public Async Function TestMemberSelectionViewModelDoNot_PullDisableItem() As Task
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
            viewModel.SelectAll()

            ' select an interface, all checkbox of field will be disable
            viewModel.UpdateMembersBasedOnDestinationKind(TypeKind.Interface)

            ' Make sure fields are not pulled to interface
            Dim checkedMembers = viewModel.CheckedMembers()
            Assert.Empty(checkedMembers.WhereAsArray(Function(analysisResult) analysisResult.Symbol.IsKind(SymbolKind.Field)))
        End Function

        <Fact>
        Public Async Function SelectDependents() As Task
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

        Private Shared Function FindMemberByName(name As String, memberArray As ImmutableArray(Of MemberSymbolViewModel)) As MemberSymbolViewModel
            Dim member = memberArray.FirstOrDefault(Function(memberViewModel) memberViewModel.SymbolName.Equals(name))
            If (member Is Nothing) Then
                Assert.True(False, $"No member called {name} found")
            End If

            Return member
        End Function

        Private Shared Async Function GetViewModelAsync(markup As XElement, languageName As String) As Task(Of MemberSelectionViewModel)
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
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.Services.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim memberSymbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                Dim membersInType = memberSymbol.ContainingType.GetMembers().WhereAsArray(Function(member) MemberAndDestinationValidator.IsMemberValid(member))
                Dim membersViewModel = membersInType.SelectAsArray(
                    Function(member) New MemberSymbolViewModel(member, glyphService:=Nothing) With {.IsChecked = member.Equals(memberSymbol), .IsCheckable = True, .MakeAbstract = False})
                Dim memberToDependents = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, workspaceDoc.Project, CancellationToken.None)
                Return New MemberSelectionViewModel(
                    workspace.GetService(Of IUIThreadOperationExecutor),
                    membersViewModel,
                    memberToDependents)
            End Using
        End Function
    End Class
End Namespace
