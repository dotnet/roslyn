' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
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

        Private Property IsExpandedCompletion As Boolean = True

        Protected Overrides Function WithChangedOptions(options As OptionSet) As OptionSet
            Return options _
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, ShowImportCompletionItemsOptionValue) _
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, IsExpandedCompletion)
        End Function

        Protected Overrides Function GetComposition() As TestComposition
            Return MyBase.GetComposition().AddParts(GetType(TestExperimentationService))
        End Function

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(TypeImportCompletionProvider)
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

            Dim markup = CreateMarkupForProjectWithProjectReference(file2, file1, LanguageNames.VisualBasic, LanguageNames.CSharp)
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

        <InlineData(SourceCodeKind.Regular)>
        <InlineData(SourceCodeKind.Script)>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(37038, "https://github.com/dotnet/roslyn/issues/37038")>
        Public Async Function CommitTypeInImportAliasContextShouldUseFullyQualifiedName(kind As SourceCodeKind) As Task

            Dim file1 = <Text>
Namespace Foo
    Public Class Bar
    End Class
End Namespace</Text>.Value

            Dim file2 = "Imports BarAlias = $$"

            Dim expectedCodeAfterCommit = "Imports BarAlias = Foo.Bar$$"

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyCustomCommitProviderAsync(markup, "Bar", expectedCodeAfterCommit, sourceCodeKind:=kind)
        End Function

        <InlineData(SourceCodeKind.Regular)>
        <InlineData(SourceCodeKind.Script)>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(37038, "https://github.com/dotnet/roslyn/issues/37038")>
        Public Async Function CommitGenericTypeParameterInImportAliasContextShouldUseFullyQualifiedName(kind As SourceCodeKind) As Task

            Dim file1 = <Text>
Namespace Foo
    Public Class Bar
    End Class
End Namespace</Text>.Value

            Dim file2 = "Imports BarAlias = System.Collections.Generic.List(Of $$)"

            Dim expectedCodeAfterCommit = "Imports BarAlias = System.Collections.Generic.List(Of Foo.Bar$$)"

            Dim markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.VisualBasic)
            Await VerifyCustomCommitProviderAsync(markup, "Bar", expectedCodeAfterCommit, sourceCodeKind:=kind)
        End Function
    End Class
End Namespace
