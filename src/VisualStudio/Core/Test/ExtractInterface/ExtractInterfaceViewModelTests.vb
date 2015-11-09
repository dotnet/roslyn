' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ExtractInterface
    Public Class ExtractInterfaceViewModelTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_InterfaceNameIsSameAsPassedIn()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass", viewModel.InterfaceName)

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.GeneratedName)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.InterfaceName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", viewModel.FileName)
            Assert.Equal("IMyClassChanged", viewModel.GeneratedName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FileNameHasExpectedExtension()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass.cs", viewModel.FileName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameInGlobalNamespace()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("IMyClass", viewModel.GeneratedName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameInNestedNamespaces()
            Dim markup = <Text><![CDATA[
namespace Outer
{
    namespace Inner
    {
        class $$MyClass
        {
            public void Foo()
            {
            }
        }
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass", defaultNamespace:="Outer.Inner")
            Assert.Equal("Outer.Inner.IMyClass", viewModel.GeneratedName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameWithTypeParameters()
            Dim markup = <Text><![CDATA[
namespace Outer
{
    namespace Inner
    {
        class $$MyClass<X, Y>
        {
            public void Foo(X x, Y y)
            {
            }
        }
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass", defaultNamespace:="Outer.Inner", generatedNameTypeParameterSuffix:="<X, Y>")
            Assert.Equal("Outer.Inner.IMyClass<X, Y>", viewModel.GeneratedName)

            viewModel.InterfaceName = "IMyClassChanged"
            Assert.Equal("Outer.Inner.IMyClassChanged<X, Y>", viewModel.GeneratedName)
        End Sub

        <WpfFact>
        <WorkItem(716122), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_GeneratedNameIsGeneratedFromTrimmedInterfaceName()
            Dim markup = <Text><![CDATA[
namespace Ns
{
    class C$$
    {
        public void Foo()
        {
        }
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IC", defaultNamespace:="Ns")

            viewModel.InterfaceName = "     IC2       "
            Assert.Equal("Ns.IC2", viewModel.GeneratedName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_MembersCheckedByDefault()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.True(viewModel.MemberContainers.Single().IsChecked)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_InterfaceNameChangesUpdateGeneratedName()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.GeneratedName)

            viewModel.InterfaceName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged", viewModel.GeneratedName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_InterfaceNameChangesUpdateFileName()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.InterfaceName = "IMyClassChanged"
            Assert.Equal("IMyClassChanged.cs", viewModel.FileName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Sub

        <WpfFact>
        <WorkItem(716122), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FileNameIsGeneratedFromTrimmedInterfaceName()
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Foo() { }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IC")
            viewModel.InterfaceName = "                 IC2     "
            Assert.Equal("IC2.cs", viewModel.FileName)
        End Sub

        <WpfFact>
        <WorkItem(716122), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_InterfaceNameIsTrimmedOnSubmit()
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Foo() { }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IC")
            viewModel.InterfaceName = "                 IC2     "
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Sub

        <WpfFact>
        <WorkItem(716122), Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FileNameIsTrimmedOnSubmit()
            Dim markup = <Text><![CDATA[
public class C$$
{
    public void Foo() { }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IC")
            viewModel.FileName = "                 IC2.cs     "
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FileNameChangesDoNotUpdateInterfaceName()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Dim monitor = New PropertyChangedTestMonitor(viewModel, strict:=True)
            monitor.AddExpectation(Function() viewModel.FileName)

            viewModel.FileName = "IMyClassChanged.cs"
            Assert.Equal("IMyClass", viewModel.InterfaceName)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_SuccessfulCommit()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_SuccessfulCommit_NonemptyStrictSubsetOfMembersSelected()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }

    public void Bar()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.MemberContainers.First().IsChecked = False
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.True(submitSucceeded, String.Format("Submit failed unexpectedly."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_InterfaceNameConflict()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass", conflictingTypeNames:=New List(Of String) From {"IMyClass"})
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_InterfaceNameNotAnIdentifier()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.InterfaceName = "SomeNamespace.IMyClass"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_BadFileExtension()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.FileName = "FileName.vb"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_BadFileName()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.FileName = "Bad*FileName.cs"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_BadFileName2()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.FileName = "?BadFileName.cs"
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_FailedCommit_NoMembersSelected()
            Dim markup = <Text><![CDATA[
class $$MyClass
{
    public void Foo()
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            viewModel.MemberContainers.Single().IsChecked = False
            Dim submitSucceeded = viewModel.TrySubmit()
            Assert.False(submitSucceeded)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_MemberDisplay_Method()
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public void Foo<T>(T t, System.Diagnostics.CorrelationManager v, ref int w, Nullable<System.Int32> x = 7, string y = "hi", params int[] z)
    {
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("Foo<T>(T, CorrelationManager, ref int, [int?], [string], params int[])", viewModel.MemberContainers.Single().MemberName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_MemberDisplay_Property()
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public int Foo
    {
        get { return 5; }
        set { }
    }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("Foo", viewModel.MemberContainers.Where(Function(c) c.MemberSymbol.IsKind(SymbolKind.Property)).Single().MemberName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_MemberDisplay_Indexer()
            Dim markup = <Text><![CDATA[
using System;
class $$MyClass
{
    public int this[Nullable<Int32> x, string y = "hi"] { get { return 1; } set { } }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal("this[int?, [string]]", viewModel.MemberContainers.Where(Function(c) c.MemberSymbol.IsKind(SymbolKind.Property)).Single().MemberName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractInterface)>
        Public Sub ExtractInterface_MembersSorted()
            Dim markup = <Text><![CDATA[
public class $$MyClass
{
    public void Foo(string s) { }
    public void Foo(int i) { }
    public void Foo(int i, string s) { }
    public void Foo() { }
    public void Foo(int i, int i2) { }
}"]]></Text>

            Dim viewModel = GetViewModel(markup, LanguageNames.CSharp, "IMyClass")
            Assert.Equal(5, viewModel.MemberContainers.Count)
            Assert.Equal("Foo()", viewModel.MemberContainers.ElementAt(0).MemberName)
            Assert.Equal("Foo(int)", viewModel.MemberContainers.ElementAt(1).MemberName)
            Assert.Equal("Foo(int, int)", viewModel.MemberContainers.ElementAt(2).MemberName)
            Assert.Equal("Foo(int, string)", viewModel.MemberContainers.ElementAt(3).MemberName)
            Assert.Equal("Foo(string)", viewModel.MemberContainers.ElementAt(4).MemberName)
        End Sub

        Private Function GetViewModel(markup As XElement,
                              languageName As String,
                              defaultInterfaceName As String,
                              Optional defaultNamespace As String = "",
                              Optional generatedNameTypeParameterSuffix As String = "",
                              Optional conflictingTypeNames As List(Of String) = Nothing,
                              Optional isValidIdentifier As Boolean = True) As ExtractInterfaceDialogViewModel

            Dim workspaceXml =
            <Workspace>
                <Project Language=<%= languageName %> CommonReferences="true">
                    <Document><%= markup.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                </Project>
            </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If (Not doc.CursorPosition.HasValue) Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim token = workspaceDoc.GetSyntaxTreeAsync().Result.GetTouchingWord(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim symbol = workspaceDoc.GetSemanticModelAsync().Result.GetDeclaredSymbol(token.Parent)
                Dim extractableMembers = DirectCast(symbol, INamedTypeSymbol).GetMembers().Where(Function(s) Not (TypeOf s Is IMethodSymbol) OrElse DirectCast(s, IMethodSymbol).MethodKind <> MethodKind.Constructor)

                Return New ExtractInterfaceDialogViewModel(
                    workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(),
                    glyphService:=Nothing,
                    notificationService:=New TestNotificationService(),
                    defaultInterfaceName:=defaultInterfaceName,
                    extractableMembers:=extractableMembers.ToList(),
                    conflictingTypeNames:=If(conflictingTypeNames, New List(Of String)),
                    defaultNamespace:=defaultNamespace,
                    generatedNameTypeParameterSuffix:=generatedNameTypeParameterSuffix,
                    languageName:=doc.Project.Language,
                    fileExtension:=If(languageName = LanguageNames.CSharp, ".cs", ".vb"))
            End Using
        End Function
    End Class
End Namespace
