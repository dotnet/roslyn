' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CommonControls

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.ExtractInterface)>
    Public Class NewTypeDestinationSelectionViewModelTests

        <Fact>
        Public Async Function TypeNameIsSameAsPassedIn() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass", viewModel.TypeName)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.GeneratedName)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.TypeName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", viewModel.FileName)
            Assert.Equal("IMyClassChanged", viewModel.GeneratedName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact>
        Public Async Function FileNameHasExpectedExtension() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass.cs", viewModel.FileName)
        End Function

        <Fact>
        Public Async Function GeneratedNameInGlobalNamespace() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass", viewModel.GeneratedName)
        End Function

        <Fact>
        Public Async Function GeneratedNameInNestedNamespaces() As Task
            Dim markup = <Text><![CDATA[
namespace Outer
{
    namespace Inner
    {
        class $$MyClass
        {
            public void Goo()
            {
            }
        }
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass", defaultNamespace:="Outer.Inner")
            Assert.Equal("Outer.Inner.IMyClass", viewModel.GeneratedName)
        End Function

        <Fact>
        Public Async Function GeneratedNameWithTypeParameters() As Task
            Dim markup = <Text><![CDATA[
namespace Outer
{
    namespace Inner
    {
        class $$MyClass<X, Y>
        {
            public void Goo(X x, Y y)
            {
            }
        }
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass", defaultNamespace:="Outer.Inner", generatedNameTypeParameterSuffix:="<X, Y>")
            Assert.Equal("Outer.Inner.IMyClass<X, Y>", viewModel.GeneratedName)

            viewModel.TypeName = "IMyClassChanged"
            Assert.Equal("Outer.Inner.IMyClassChanged<X, Y>", viewModel.GeneratedName)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122")>
        Public Async Function GeneratedNameIsGeneratedFromTrimmedTypeName() As Task
            Dim markup = <Text><![CDATA[
namespace Ns
{
    class C$$
    {
        public void Goo()
        {
        }
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IC", defaultNamespace:="Ns")

            viewModel.TypeName = "     IC2       "
            Assert.Equal("Ns.IC2", viewModel.GeneratedName)
        End Function

        <Fact>
        Public Async Function TypeNameChangesUpdateGeneratedName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.GeneratedName)

            viewModel.TypeName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged", viewModel.GeneratedName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact>
        Public Async Function TypeNameChangesUpdateFileName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.TypeName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", viewModel.FileName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716122")>
        Public Async Function FileNameIsGeneratedFromTrimmedTypeName() As Task
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Goo() { }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IC")
            viewModel.TypeName = "                 IC2     "
            Assert.Equal("IC2.cs", viewModel.FileName)
        End Function

        <Fact>
        Public Async Function FileNameChangesDoNotUpdateTypeName() As Task
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Goo()
    {
    }
}"]]></Text>

            Dim viewModel = Await GetViewModelAsync(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel, strict:=True)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.FileName = "IMyClassChanged.cs"
            Assert.Equal("IMyClass", viewModel.TypeName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        Private Shared Async Function GetViewModelAsync(markup As XElement,
                              languageName As String,
                              defaultTypeName As String,
                              Optional defaultNamespace As String = "",
                              Optional generatedNameTypeParameterSuffix As String = "",
                              Optional conflictingTypeNames As List(Of String) = Nothing,
                              Optional isValidIdentifier As Boolean = True) As Tasks.Task(Of NewTypeDestinationSelectionViewModel)

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue().Replace(vbCrLf, vbLf) %></Document>
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

                Return New NewTypeDestinationSelectionViewModel(
                    defaultName:=defaultTypeName,
                    defaultNamespace:=defaultNamespace,
                    languageName:=languageName,
                    generatedNameTypeParameterSuffix:=generatedNameTypeParameterSuffix,
                    conflictingNames:=symbol.ContainingNamespace.GetAllTypes(CancellationToken.None).SelectAsArray(Function(t) t.Name),
                    syntaxFactsService:=workspaceDoc.GetRequiredLanguageService(Of ISyntaxFactsService),
                    globalOptionService:=workspace.GetService(Of IGlobalOptionService))
            End Using
        End Function
    End Class
End Namespace
