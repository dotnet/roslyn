' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.MoveStaticMembers
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
Imports Microsoft.VisualStudio.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveStaticMembers
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsMoveStaticMembers)>
    Public Class MoveStaticMembersViewModelTest
        Private Shared Async Function GetViewModelAsync(xmlElement As XElement) As Task(Of MoveStaticMembersDialogViewModel)
            Dim workspaceXml = xmlElement.Value
            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.ElementAt(0)
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If Not doc.CursorPosition.HasValue Then
                    Throw New ArgumentException("Missing caret location in document.")
                End If

                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync().ConfigureAwait(False)
                Dim syntaxFacts = workspaceDoc.Project.Services.GetService(Of ISyntaxFactsService)()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, syntaxFacts, CancellationToken.None).ConfigureAwait(False)
                Dim memberSymbol = (Await workspaceDoc.GetRequiredSemanticModelAsync(CancellationToken.None)).GetDeclaredSymbol(token.Parent)
                Return VisualStudioMoveStaticMembersOptionsService.GetViewModel(
                    workspaceDoc,
                    memberSymbol.ContainingType,
                    ImmutableArray.Create(memberSymbol),
                    New LinkedList(Of INamedTypeSymbol),
                    Nothing,
                    workspace.GetService(Of IUIThreadOperationExecutor))
            End Using
        End Function

        Private Shared Function Submit(viewModel As MoveStaticMembersDialogViewModel, cSharp As Boolean) As MoveStaticMembersOptions
            Assert.True(viewModel.CanSubmit)
            Dim language = If(cSharp, LanguageNames.CSharp, LanguageNames.VisualBasic)

            Return VisualStudioMoveStaticMembersOptionsService.GenerateOptions(language, viewModel, True)
        End Function

        Private Shared Function FindMemberByName(name As String, memberArray As ImmutableArray(Of SymbolViewModel(Of ISymbol))) As SymbolViewModel(Of ISymbol)
            Dim member = memberArray.FirstOrDefault(Function(memberViewModel) memberViewModel.Symbol.Name.Equals(name))
            Assert.NotNull(member)
            Return member
        End Function

        Private Shared Sub SelectMember(name As String, viewModel As StaticMemberSelectionViewModel)
            Dim member = FindMemberByName(name, viewModel.Members)
            member.IsChecked = True
            viewModel.Members.Replace(FindMemberByName(name, viewModel.Members), member)
            Assert.True(FindMemberByName(name, viewModel.Members).IsChecked)
        End Sub

        Private Shared Sub DeselectMember(name As String, viewModel As StaticMemberSelectionViewModel)
            Dim member = FindMemberByName(name, viewModel.Members)
            member.IsChecked = False
            viewModel.Members.Replace(FindMemberByName(name, viewModel.Members), member)
            Assert.False(FindMemberByName(name, viewModel.Members).IsChecked)
        End Sub

#Region "C#"
        <Fact>
        Public Async Function CSTestBasicSubmit() As Task
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
    </Project>
</Workspace>]]></Text>

            ' We can call the method, but we need the document still to test submission
            Dim viewModel = Await GetViewModelAsync(markUp).ConfigureAwait(False)

            Assert.Equal("TestNs.TestClassHelpers", viewModel.DestinationName.FullyQualifiedTypeName)
            SetSearchText(viewModel, "ExtraNs.TestClassHelpers")
            Assert.Equal("TestNs.ExtraNs.TestClassHelpers", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.Equal("TestNs.ExtraNs.", viewModel.TypeName_NamespaceOnly)
            Assert.True(viewModel.CanSubmit)

            Dim cancelledOptions = VisualStudioMoveStaticMembersOptionsService.GenerateOptions(LanguageNames.CSharp, viewModel, False)
            Assert.True(cancelledOptions.IsCancelled)

            Dim options = VisualStudioMoveStaticMembersOptionsService.GenerateOptions(LanguageNames.CSharp, viewModel, True)
            Assert.False(options.IsCancelled)
            Assert.Equal("TestClassHelpers.cs", options.FilePath)
            Assert.Equal("TestClassHelpers", options.TypeName)
            Assert.Equal("TestNs.ExtraNs", options.NamespaceDisplay)
        End Function

        <Fact>
        Public Async Function CSTestInvalidNames() As Task
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
        <Document>
            namespace TestNs.ExtraNs
            {
                public class ConflictingNsClassName
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

            Assert.Equal(viewModel.DestinationName.FullyQualifiedTypeName, "TestNs.TestClassHelpers")
            Assert.Equal("TestNs.", viewModel.TypeName_NamespaceOnly)

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            SetSearchText(viewModel, "ValidName")
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            ' spaces are not allowed as types
            SetSearchText(viewModel, "asd ")
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            ' different project
            SetSearchText(viewModel, "ConflictingClassName3")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            SetSearchText(viewModel, "NoNsClass")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            ' different namespace
            SetSearchText(viewModel, "ConflictingClassName2")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            SetSearchText(viewModel, "ExtraNamespace.ValidName")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)
        End Function

        Private Shared Sub SetSearchText(viewModel As MoveStaticMembersDialogViewModel, destinationName As String)
            viewModel.SearchText = destinationName
        End Sub

        <Fact>
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

            selectionVm.SelectAll()
            ' If constructor and operators are able to be selected, this would be a higher number
            Assert.Equal(7, selectionVm.CheckedMembers.Length)

            selectionVm.DeselectAll()
            Assert.True(selectionVm.CheckedMembers.IsEmpty)

            SelectMember("Dependent", selectionVm)
            selectionVm.SelectDependents()
            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            Assert.Equal(2, selectionVm.CheckedMembers.Length)
        End Function

        <Fact>
        Public Async Function CSTestTypeSelection() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
        <Document>
            namespace TestNs
            {
                public static class TestClass
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
        <Document FilePath="TestFile.cs">
            namespace TestNs 
            {
                public static class ConflictingClassName
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
        <Document>
            namespace TestNs.ExtraNs
            {
                public static class ConflictingNsClassName
                {
                }
            }
        </Document>
        <Document>
            namespace TestNs
            {
                public class NonStaticConflictingName
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

            Assert.Equal(viewModel.SearchText, "TestClassHelpers")

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            ' there should only be 2 available types that are
            ' a) the same kind
            ' b) static (if the current type is static)
            ' b) in the same or nested namespace
            ' c) in the same project
            Assert.Equal(2, viewModel.AvailableTypes.Length)
            Assert.Equal(1, viewModel.MemberSelectionViewModel.CheckedMembers.Length)

            viewModel.SearchText = viewModel.AvailableTypes.ElementAt(1).FullyQualifiedTypeName
            Assert.Equal("TestNs.ExtraNs.ConflictingNsClassName", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)

            viewModel.SearchText = viewModel.AvailableTypes.ElementAt(0).FullyQualifiedTypeName
            Assert.Equal("TestNs.ConflictingClassName", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)

            Dim options = Submit(viewModel, cSharp:=True)
            Assert.False(options.IsNewType)
            Assert.False(options.IsCancelled)
            Assert.NotNull(options.Destination)
            Assert.Equal("TestNs.ConflictingClassName", options.Destination.ToDisplayString())
            Assert.Equal(Path.Combine(TestWorkspace.RootDirectory, "TestFile.cs"), options.FilePath)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70896")>
        Public Async Function CSTestTypeSelection2() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
        <Document>
            namespace TestNs
            {
                public class FromClass
                {
                    public static int Bar$$bar()
                    {
                        return 12345;
                    }
                }

                public struct TestStruct
                {
                }

                public interface ITestInterface
                {
                }
            }
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            ' Should Not have the interface in the list as we started from a class.  Should still have the struct through.
            Assert.Equal(1, viewModel.AvailableTypes.Length)
            Assert.Equal(1, viewModel.MemberSelectionViewModel.CheckedMembers.Length)

            viewModel.SearchText = viewModel.AvailableTypes.ElementAt(0).FullyQualifiedTypeName
            Assert.Equal("TestNs.TestStruct", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70896")>
        Public Async Function CSTestTypeSelection3() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
        <Document>
            namespace TestNs
            {
                public interface FromInterface
                {
                    public static int Bar$$bar()
                    {
                        return 12345;
                    }
                }

                public struct TestStruct
                {
                }

                public interface ITestInterface
                {
                }
            }
        </Document>
    </Project>
</Workspace>]]></Text>
            Dim viewModel = Await GetViewModelAsync(markUp)

            Assert.Equal(2, viewModel.AvailableTypes.Length)
            Assert.Equal(1, viewModel.MemberSelectionViewModel.CheckedMembers.Length)

            ' Should have the interface and the struct in the list as we started from an interface.
            viewModel.SearchText = "TestNs.TestStruct"
            Assert.Equal("TestNs.TestStruct", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)

            viewModel.SearchText = "TestNs.ITestInterface"
            Assert.Equal("TestNs.ITestInterface", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)
        End Function
#End Region

#Region "VB"
        <Fact>
        Public Async Function VBTestBasicSubmit() As Task
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
    </Project>
</Workspace>]]></Text>

            ' We can call the method, but we need the document still to test submission
            Dim viewModel = Await GetViewModelAsync(markUp).ConfigureAwait(False)

            Assert.Equal("TestNs.TestClassHelpers", viewModel.DestinationName.FullyQualifiedTypeName)
            SetSearchText(viewModel, "ExtraNs.TestClassHelpers")
            Assert.Equal("TestNs.ExtraNs.TestClassHelpers", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.Equal("TestNs.ExtraNs.", viewModel.TypeName_NamespaceOnly)
            Assert.True(viewModel.CanSubmit)

            Dim cancelledOptions = VisualStudioMoveStaticMembersOptionsService.GenerateOptions(LanguageNames.VisualBasic, viewModel, False)
            Assert.True(cancelledOptions.IsCancelled)

            Dim options = VisualStudioMoveStaticMembersOptionsService.GenerateOptions(LanguageNames.VisualBasic, viewModel, True)
            Assert.False(options.IsCancelled)
            Assert.Equal("TestClassHelpers.vb", options.FilePath)
            Assert.Equal("TestClassHelpers", options.TypeName)
            Assert.Equal("TestNs.ExtraNs", options.NamespaceDisplay)
        End Function

        <Fact>
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
        <Document>
            Namespace TestNs.ExtraNs
                Public Class ConflictingNsClassName
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

            Assert.Equal(viewModel.DestinationName.FullyQualifiedTypeName, "TestNs.TestClassHelpers")
            Assert.Equal("TestNs.", viewModel.TypeName_NamespaceOnly)

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            SetSearchText(viewModel, "TestNs.ConflictingClassName")
            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)

            SetSearchText(viewModel, "ValidName")
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            ' spaces are not allowed as types
            SetSearchText(viewModel, "asd ")
            Assert.False(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.Invalid_type_name, viewModel.Message)

            ' different project
            SetSearchText(viewModel, "ConflictingClassName3")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            SetSearchText(viewModel, "NoNsClass")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            ' different namespace
            SetSearchText(viewModel, "ConflictingClassName2")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            SetSearchText(viewModel, "ExtraNamespace.ValidName")
            Assert.True(viewModel.CanSubmit)
            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)
        End Function

        <Fact>
        Public Async Function VBTestRootNamespace() As Task
            Dim markUp = <Text><![CDATA[
<Workspace>
    <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
        <CompilationOptions RootNamespace="RootNs"/>
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
            Namespace TestNs
                Public Class ConflictingClassName
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>]]></Text>

            Dim viewModel = Await GetViewModelAsync(markUp)

            Assert.Equal(viewModel.DestinationName.FullyQualifiedTypeName, "RootNs.TestNs.TestClassHelpers")
            Assert.Equal("RootNs.TestNs.", viewModel.TypeName_NamespaceOnly)

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            SetSearchText(viewModel, "RootNs.TestNs.ConflictingClassName")
            Assert.True(viewModel.CanSubmit)
            Assert.False(viewModel.ShowMessage)
        End Function

        <Fact>
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

            selectionVm.SelectAll()
            ' If constructor and operators are able to be selected, this would be a higher number
            Assert.Equal(7, selectionVm.CheckedMembers.Length)

            selectionVm.DeselectAll()
            Assert.True(selectionVm.CheckedMembers.IsEmpty)

            SelectMember("Dependent", selectionVm)
            selectionVm.SelectDependents()
            Assert.True(FindMemberByName("Barbar", selectionVm.Members).IsChecked)
            Assert.Equal(2, selectionVm.CheckedMembers.Length)
        End Function

        <Fact>
        Public Async Function VBTestTypeSelection() As Task
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
        <Document FilePath="TestFile.vb">
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
        <Document>
            Namespace TestNs.ExtraNs
                Public Class ConflictingNsClassName
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

            Assert.Equal(viewModel.SearchText, "TestClassHelpers")

            Assert.True(viewModel.ShowMessage)
            Assert.Equal(ServicesVSResources.New_Type_Name_colon, viewModel.Message)

            Assert.False(viewModel.MemberSelectionViewModel.CheckedMembers.IsEmpty)
            Assert.True(viewModel.CanSubmit)

            ' there should only be 2 available types that are
            ' a) the same kind
            ' b) in the same or nested namespace
            ' c) in the same project
            Assert.Equal(2, viewModel.AvailableTypes.Length)
            Assert.Equal(1, viewModel.MemberSelectionViewModel.CheckedMembers.Length)

            viewModel.SearchText = viewModel.AvailableTypes.ElementAt(1).FullyQualifiedTypeName
            Assert.Equal("TestNs.ExtraNs.ConflictingNsClassName", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)

            viewModel.SearchText = viewModel.AvailableTypes.ElementAt(0).FullyQualifiedTypeName
            Assert.Equal("TestNs.ConflictingClassName", viewModel.DestinationName.FullyQualifiedTypeName)
            Assert.NotNull(viewModel.DestinationName.NamedType)
            Assert.False(viewModel.DestinationName.IsNew)
            Assert.False(viewModel.ShowMessage)
            Assert.True(viewModel.CanSubmit)

            Dim options = Submit(viewModel, cSharp:=False)
            Assert.False(options.IsNewType)
            Assert.False(options.IsCancelled)
            Assert.NotNull(options.Destination)
            Assert.Equal("TestNs.ConflictingClassName", options.Destination.ToDisplayString())
            Assert.Equal(Path.Combine(TestWorkspace.RootDirectory, "TestFile.vb"), options.FilePath)
        End Function
#End Region
    End Class
End Namespace
