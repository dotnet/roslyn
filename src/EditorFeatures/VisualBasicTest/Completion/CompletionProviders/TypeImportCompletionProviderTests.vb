' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Experiments
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders

    <UseExportProvider>
    Public Class TypeImportCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Private Property ShowImportCompletionItemsOptionValue As Boolean = True

        ' -1 would disable timebox, whereas 0 means always timeout.
        Private Property TimeoutInMilliseconds As Integer = -1

        Protected Overrides Sub SetWorkspaceOptions(workspace As TestWorkspace)
            workspace.Options = workspace.Options
            .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, ShowImportCompletionItemsOptionValue)
            .WithChangedOption(CompletionServiceOptions.TimeoutInMillisecondsForImportCompletion, TimeoutInMilliseconds)
        End Sub

        Protected Overrides Function GetExportProvider() As ExportProvider
            Return ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(TestExperimentationService))).CreateExportProvider()
        End Function

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New TypeImportCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")>
        Public Async Function AttributeTypeInAttributeNameContext() As Task

            Dim file1 = <Text>
Namespace Foo
    Public Class MyAttribute
        Inherits System.Attribute
    End Class

    Public Class MyVBClass
    End Class

    Public Class MyAttributeWithoutSuffix
        Inherits System.Attribute
    End Class
End Namespace</Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    <$$
    Sub Main()

    End Sub
End Class]]></Text>.Value

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyItemExistsAsync(markup, "My", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.MyAttribute")
            Await VerifyItemIsAbsentAsync(markup, "MyAttributeWithoutSuffix", inlineDescription:="Foo") ' We intentionally ignore attribute types without proper suffix for perf reason
            Await VerifyItemIsAbsentAsync(markup, "MyAttribute", inlineDescription:="Foo")
            Await VerifyItemIsAbsentAsync(markup, "MyVBClass", inlineDescription:="Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")>
        Public Async Function AttributeTypeInNonAttributeNameContext() As Task

            Dim file1 = <Text>
Namespace Foo
    Public Class MyAttribute
        Inherits System.Attribute
    End Class

    Public Class MyVBClass
    End Class

    Public Class MyAttributeWithoutSuffix
        Inherits System.Attribute
End Namespace</Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    Sub Main()
        Dim x As $$
    End Sub
End Class]]></Text>.Value

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyItemExistsAsync(markup, "MyAttribute", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.MyAttribute")
            Await VerifyItemExistsAsync(markup, "MyAttributeWithoutSuffix", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.MyAttributeWithoutSuffix")
            Await VerifyItemExistsAsync(markup, "MyVBClass", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.MyVBClass")
            Await VerifyItemIsAbsentAsync(markup, "My", inlineDescription:="Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")>
        Public Async Function AttributeTypeInAttributeNameContext2() As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text>
Namespace Foo
    Public Class Myattribute
        Inherits System.Attribute
    End Class
End Namespace</Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    <$$
    Sub Main()

    End Sub
End Class]]></Text>.Value

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyItemExistsAsync(markup, "My", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.Myattribute")
            Await VerifyItemIsAbsentAsync(markup, "Myattribute", inlineDescription:="Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")>
        Public Async Function CSharpAttributeTypeWithoutSuffixInAttributeNameContext() As Task

            ' attribute suffix isn't capitalized
            Dim file1 = <Text>
namespace Foo
{
    public class Myattribute : System.Attribute { }
}</Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    <$$
    Sub Main()

    End Sub
End Class]]></Text>.Value

            Dim markup = CreateMarkupForProjecWithProjectReference(file2, file1, LanguageNames.VisualBasic, LanguageNames.CSharp)
            Await VerifyItemExistsAsync(markup, "My", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", expectedDescriptionOrNull:="Class Foo.Myattribute")
            Await VerifyItemIsAbsentAsync(markup, "Myattribute", inlineDescription:="Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(35124, "https://github.com/dotnet/roslyn/issues/35124")>
        Public Async Function GenericTypeShouldDisplayProperVBSyntax() As Task

            Dim file1 = <Text>
Namespace Foo
    Public Class MyGenericClass(Of T)
    End Class
End Namespace</Text>.Value

            Dim file2 = <Text><![CDATA[
Public Class Bar
    Sub Main()
        Dim x As $$
    End Sub
End Class]]></Text>.Value

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyItemExistsAsync(markup, "MyGenericClass", glyph:=Glyph.ClassPublic, inlineDescription:="Foo", displayTextSuffix:="(Of ...)", expectedDescriptionOrNull:="Class Foo.MyGenericClass(Of T)")
        End Function
    End Class
End Namespace
