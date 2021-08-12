' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PullMemberUp
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
Imports Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveStaticMembers
    <UseExportProvider>
    Public Class MoveStaticMembersViewModelTest
        Private Async Function GetViewModelAsync(xmlElement As XElement) As Task(Of MoveStaticMembersDialogViewModel)
            Dim workspaceXml = xmlElement.Value
            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.ElementAt(0)
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If Not doc.CursorPosition.HasValue Then
                    Throw New ArgumentException("Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync().ConfigureAwait(False)
                Dim syntaxFacts = workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, syntaxFacts, CancellationToken.None).ConfigureAwait(False)
                Dim memberSymbol = (Await workspaceDoc.GetRequiredSemanticModelAsync(CancellationToken.None)).GetDeclaredSymbol(token.Parent)
                Dim existingNames = memberSymbol.ContainingNamespace.GetAllTypes(CancellationToken.None).Select(Function(type) type.Name).ToImmutableArray()
                Dim membersInType = memberSymbol.ContainingType.GetMembers().WhereAsArray(Function(member) MemberAndDestinationValidator.IsMemberValid(member))
                Dim membersViewModel = membersInType.SelectAsArray(
                    Function(member) New SymbolViewModel(Of ISymbol)(member, glyphService:=Nothing) With {.IsChecked = member.Equals(memberSymbol)})
                Dim memberToDependents = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, workspaceDoc.Project, CancellationToken.None)
                Dim memberSelectionViewModel = New StaticMemberSelectionViewModel(
                    workspace.GetService(Of IUIThreadOperationExecutor),
                    membersViewModel,
                    memberToDependents)
                Return New MoveStaticMembersDialogViewModel(
                    memberSelectionViewModel,
                    "TestDefaultType",
                    existingNames,
                    syntaxFacts)
            End Using
        End Function

        Private Function FindMemberByName(name As String, memberArray As ImmutableArray(Of SymbolViewModel(Of ISymbol))) As SymbolViewModel(Of ISymbol)
            Dim member = memberArray.FirstOrDefault(Function(memberViewModel) memberViewModel.Symbol.Name.Equals(name))
            Assert.NotNull(member)
            Return member
        End Function

        Private Sub SelectMember(name As String, viewModel As StaticMemberSelectionViewModel)
            Dim member = FindMemberByName(name, viewModel.Members)
            member.IsChecked = True
            viewModel.Members.Replace(FindMemberByName(name, viewModel.Members), member)
            Assert.True(FindMemberByName(name, viewModel.Members).IsChecked)
        End Sub

        Private Sub DeselectMember(name As String, viewModel As StaticMemberSelectionViewModel)
            Dim member = FindMemberByName(name, viewModel.Members)
            member.IsChecked = False
            viewModel.Members.Replace(FindMemberByName(name, viewModel.Members), member)
            Assert.False(FindMemberByName(name, viewModel.Members).IsChecked)
        End Sub

#Region "C#"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function CSTestNameConflicts() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
        <Document>
            namespace TestNs
            {
                public class TestClass
                {
                    public static int Bar$$bar()
                    {
                        return 12345;
                    }
                }
            }
        </Document>
        <Document>
            public class NoNsClass
            {
            }
        </Document>
        <Document>
            namespace TestNs
            {
                public interface ITestInterface
                {
                }
            }
        </Document>
        <Document>
            namespace TestNs 
            {
                public class ConflictingClassName
                {
                }
            }
        </Document>
        <Document>
            namespace TestNs2 
            {
                public class ConflictingClassName2
                {
                }
            }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
        <Document>
            namespace TestNs 
            {
                public class ConflictingClassName3
                {
                }
            }
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            Assert.Equal(viewModel.DestinationName, "TestDefaultType")

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            viewModel.DestinationName = "ConflictingClassName"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            viewModel.DestinationName = "ValidName"
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            ' spaces are not allowed as types
            viewModel.DestinationName = "asd "
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            ' different project
            viewModel.DestinationName = "ConflictingClassName3"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "ITestInterface"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            viewModel.DestinationName = "NoNsClass"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            ' different namespace
            viewModel.DestinationName = "ConflictingClassName2"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "TestClass"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function CSTestMemberSelection() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="VBAssembly1" CommonReferences="true">
        <Document>
            namespace TestNs 
            {
                public class TestClass
                {
                    public static int Bar$$bar()
                    {
                        return 12345;
                    }

                    public static int TestField;

                    public static int TestField2 = 0;

                    public static void DoSomething()
                    {
                    }

                    public static int TestProperty { get; set; }

                    private static int _private = 0

                    public static bool Dependent()
                    {
                        return Barbar() == 0;
                    }

                    static TestClass()
                    {
                    }

                    public static TestClass operator +(TestClass a, TestClass b)
                    {
                        return new TestClass()
                    }
                }
            }
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            Dim selectionVm = viewModel.MemberSelectionViewModel

            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            For Each member In selectionVm.Members
                If member.Symbol.Name <> "Barbar" Then
                    Assert.False(member.IsChecked)
                End If
            Next

            SelectMember("TestField", selectionVm)
            SelectMember("TestField2", selectionVm)
            SelectMember("DoSomething", selectionVm)
            SelectMember("TestProperty", selectionVm)
            SelectMember("_private", selectionVm)
            SelectMember("Dependent", selectionVm)

            Assert.Equal(7, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)

            DeselectMember("Barbar", selectionVm)
            DeselectMember("TestField", selectionVm)
            DeselectMember("TestField2", selectionVm)
            DeselectMember("DoSomething", selectionVm)
            DeselectMember("TestProperty", selectionVm)
            DeselectMember("_private", selectionVm)
            DeselectMember("Dependent", selectionVm)

            Assert.True(selectionVm.CheckedMembers.IsEmpty)
            Assert.False(viewModel.CanSubmit)

            selectionVm.SelectAll()
            ' If constructor and operators are able to be selected, this would be a higher number
            Assert.Equal(7, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)

            selectionVm.DeselectAll()
            Assert.True(selectionVm.CheckedMembers.IsEmpty)
            Assert.False(viewModel.CanSubmit)

            SelectMember("Dependent", selectionVm)
            selectionVm.SelectDependents()
            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            Assert.Equal(2, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)
        End Function
#End Region

#Region "VB"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function VBTestNameConflicts() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
        <Document>
            Namespace TestNs
                Public Class TestClass
                    Public Shared Function Bar$$bar() As Integer
                        Return 12345;
                    End Function
                End Class
            End Namespace
        </Document>
        <Document>
            Public Class NoNsClass
            End Class
        </Document>
        <Document>
            Namespace TestNs
                Public Interface ITestInterface
                End Interface
            End Namespace
        </Document>
        <Document>
            Namespace TestNs
                Public Class ConflictingClassName
                End Class
            End Namespace
        </Document>
        <Document>
            Namespace TestNs2
                Public Class ConflictingClassName2
                End Class
            End Namespace
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSAssembly2" CommonReferences="true">
        <Document>
            Namespace TestNs
                Public Class ConflictingClassName3
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            Assert.Equal(viewModel.DestinationName, "TestDefaultType")

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            viewModel.DestinationName = "ConflictingClassName"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            viewModel.DestinationName = "asd "
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            viewModel.DestinationName = "ValidName"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "ConflictingClassName3"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "ITestInterface"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            viewModel.DestinationName = "NoNsClass"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "ConflictingClassName2"
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.A_new_type_will_be_created, viewModel.Message)

            viewModel.DestinationName = "TestClass"
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
        Public Async Function VBTestMemberSelection() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
        <Document>
            Namespace TestNs
                Public Class TestClass
                    Public Shared Function Bar$$bar() As Integer
                        Return 12345
                    End Function

                    Public Shared TestField As Integer

                    Public Shared TestField2 As Integer = 0

                    Public Shared Sub DoSomething()
                    End Sub

                    Public Shared Property TestProperty As Integer

                    Private Shared _private As Integer = 0

                    Public Shared Function Dependent() As Boolean
                        Return Barbar() = 0
                    End Function

                    Shared Sub New()
                    End Sub

                    Public Shared Operator +(a As TestClass, b As TestClass)
                        Return New TestClass()
                    End Operator
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            Dim selectionVm = viewModel.MemberSelectionViewModel

            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            For Each member In selectionVm.Members
                If member.Symbol.Name <> "Barbar" Then
                    Assert.False(member.IsChecked)
                End If
            Next

            SelectMember("TestField", selectionVm)
            SelectMember("TestField2", selectionVm)
            SelectMember("DoSomething", selectionVm)
            SelectMember("TestProperty", selectionVm)
            SelectMember("_private", selectionVm)
            SelectMember("Dependent", selectionVm)

            Assert.Equal(7, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)

            DeselectMember("Barbar", selectionVm)
            DeselectMember("TestField", selectionVm)
            DeselectMember("TestField2", selectionVm)
            DeselectMember("DoSomething", selectionVm)
            DeselectMember("TestProperty", selectionVm)
            DeselectMember("_private", selectionVm)
            DeselectMember("Dependent", selectionVm)

            Assert.True(selectionVm.CheckedMembers.IsEmpty)
            Assert.False(viewModel.CanSubmit)

            selectionVm.SelectAll()
            ' If constructor and operators are able to be selected, this would be a higher number
            Assert.Equal(7, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)

            selectionVm.DeselectAll()
            Assert.True(selectionVm.CheckedMembers.IsEmpty)
            Assert.False(viewModel.CanSubmit)

            SelectMember("Dependent", selectionVm)
            selectionVm.SelectDependents()
            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            Assert.Equal(2, selectionVm.CheckedMembers.Length)
            Assert.True(viewModel.CanSubmit)
        End Function
#End Region
    End Class
End Namespace
