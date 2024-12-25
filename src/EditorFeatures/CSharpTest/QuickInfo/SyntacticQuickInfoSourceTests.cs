// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo;

[Trait(Traits.Feature, Traits.Features.QuickInfo)]
public class SyntacticQuickInfoSourceTests : AbstractQuickInfoSourceTests
{
    [WpfFact]
    public async Task Brackets_0()
    {
        await TestInMethodAndScriptAsync(
@"
switch (true)
{
}$$
",
@"switch (true)
{");
    }

    [WpfFact]
    public async Task Brackets_1()
        => await TestInClassAsync("int Property { get; }$$ ", "int Property {");

    [WpfFact]
    public async Task Brackets_2()
        => await TestInClassAsync("void M()\r\n{ }$$ ", "void M()\r\n{");

    [WpfFact]
    public async Task Brackets_3()
        => await TestInMethodAndScriptAsync("var a = new int[] { }$$ ", "new int[] {");

    [WpfFact]
    public async Task Brackets_4()
    {
        await TestInMethodAndScriptAsync(
@"
if (true)
{
}$$
",
@"if (true)
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_0()
    {
        await TestInMethodAndScriptAsync(
@"if (true)
            {
                {
                }$$
            }",
        "{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_1()
    {
        await TestInMethodAndScriptAsync(
@"while (true)
            {
                // some
                // comment
                {
                }$$
            }",
@"// some
// comment
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_2()
    {
        await TestInMethodAndScriptAsync(
@"do
            {
                /* comment */
                {
                }$$
            }
            while (true);",
@"/* comment */
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_3()
    {
        await TestInMethodAndScriptAsync(
@"if (true)
            {
            }
            else
            {
                {
                    // some
                    // comment
                }$$
            }",
@"{
    // some
    // comment");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_4()
    {
        await TestInMethodAndScriptAsync(
@"using (var x = new X())
            {
                {
                    /* comment */
                }$$
            }",
@"{
    /* comment */");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_5()
    {
        await TestInMethodAndScriptAsync(
@"foreach (var x in xs)
            {
                // above
                {
                    /* below */
                }$$
            }",
@"// above
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_6()
    {
        await TestInMethodAndScriptAsync(
@"for (;;)
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }",
@"/*************/

// part 1

// part 2
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_7()
    {
        await TestInMethodAndScriptAsync(
@"try
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }
            catch { throw; }",
@"/*************/

// part 1

// part 2
{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_8()
    {
        await TestInMethodAndScriptAsync(
@"
{
    /*************/

    // part 1

    // part 2
}$$
",
@"{
    /*************/

    // part 1

    // part 2");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_9()
    {
        await TestInClassAsync(
@"int Property
{
    set
    {
        {
        }$$
    }
}",
        "{");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/325")]
    public async Task ScopeBrackets_10()
    {
        await TestInMethodAndScriptAsync(
@"switch (true)
            {
                default:
                    // comment
                    {
                    }$$
                    break;
            }",
@"// comment
{");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndShowsStartRegionMessage()
    {
        await TestAsync(
@"
#region Start
#end$$region", "#region Start");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    [InlineData("$$#endregion")]
    [InlineData("#$$endregion")]
    [InlineData("#endregion$$ ")]
    [InlineData("#endregion$$\r\n")]
    [InlineData("#endregion$$ End")]
    public async Task RegionEndShowsStartRegionMessageAtDifferentPositions(string endRegion)
    {
        await TestAsync(
@$"
#region Start
{endRegion}", "#region Start");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    [InlineData("#endregion$$")]
    [InlineData("# $$ endregion")]
    [InlineData("#endregion $$End")]
    [InlineData("#endregion En$$d")]
    [InlineData("#endregion $$")]
    [InlineData("#endregion\r\n$$")]
    public async Task RegionEndQuickInfoIsNotOfferedAtDifferentPositions(string endRegion)
    {
        await TestAsync(
@$"
#region Start
{endRegion}", "");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndHasNoQuickinfo_MissingRegionStart_1()
    {
        await TestAsync(
@$"#end$$region", "");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndHasNoQuickinfo_MissingRegionStart_2()
    {
        await TestAsync(
@$"
#region Start
#endregion
#end$$region", "");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndShowsRegionStart_Nesting_1()
    {
        await TestAsync(
@$"
#region Start1
#region Start2
#endregion
#end$$region", "#region Start1");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndShowsRegionStart_Nesting_2()
    {
        await TestAsync(
@$"
#region Start1
#region Start2
#end$$region
#endregion", "#region Start2");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndShowsRegionStart_Blocks_1()
    {
        await TestAsync(
@$"
#region Start1
#end$$region
#region Start2
#endregion", "#region Start1");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task RegionEndShowsRegionStart_Blocks_2()
    {
        await TestAsync(
@$"
#region Start1
#endregion
#region Start2
#end$$region", "#region Start2");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfCondition_1()
    {
        await TestAsync(
@$"
#if DEBUG
#end$$if", "#if DEBUG");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfCondition_2()
    {
        await TestAsync(
@$"
#if DEBUG
#else
#end$$if", "#if DEBUG\r\n#else");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsElIfCondition()
    {
        await TestAsync(
@$"
#if DEBUG
#elif RELEASE
#end$$if", "#if DEBUG\r\n#elif RELEASE");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task ElseShowsIfCondition()
    {
        await TestAsync(
@$"
#if DEBUG
#el$$se
#endif", "#if DEBUG");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task ElseShowsElIfCondition_1()
    {
        await TestAsync(
@$"
#if DEBUG
#elif RELEASE
#el$$se
#endif", "#if DEBUG\r\n#elif RELEASE");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task ElseShowsElIfCondition_2()
    {
        await TestAsync(
@$"
#if DEBUG
#elif RELEASE
#elif DEMO
#el$$se
#endif", "#if DEBUG\r\n#elif RELEASE\r\n#elif DEMO");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task ElIfShowsIfCondition()
    {
        await TestAsync(
@$"
#if DEBUG
#el$$if RELEASE
#endif", "#if DEBUG");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfNested_1()
    {
        await TestAsync(
@$"
#if DEBUG
#if RELEASE
#end$$if
#endif", "#if RELEASE");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfNested_2()
    {
        await TestAsync(
@$"
#if DEBUG
#if RELEASE
#endif
#end$$if", "#if DEBUG");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfNested_3()
    {
        await TestAsync(
@$"
#if DEBUG
#elif RELEASE
#if DEMO
#end$$if
#endif", "#if DEMO");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfShowsIfNested_4()
    {
        await TestAsync(
@$"
#if DEBUG
#elif RELEASE
#if DEMO
#endif
#end$$if", "#if DEBUG\r\n#elif RELEASE");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfHasNoQuickinfo_MissingIf_1()
    {
        await TestAsync(
@$"
#end$$if", "");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    public async Task EndIfHasNoQuickinfo_MissingIf_2()
    {
        await TestAsync(
@$"
#if DEBUG
#endif
#end$$if", "");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    [InlineData("#$$elif RELEASE")]
    [InlineData("#elif$$ RELEASE")]
    [InlineData("#elif RELEASE$$")]
    public async Task ElifHasQuickinfoAtDifferentPositions(string elif)
    {
        await TestAsync(
@$"
#if DEBUG
{elif}
#endif", "#if DEBUG");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/56507")]
    [InlineData("#elif $$RELEASE")]
    [InlineData("#elif RELE$$ASE")]
    [InlineData("#elif (REL$$EASE == true)")]
    [InlineData("#elif (RELEASE =$$= true)")]
    [InlineData("#elif (RELEASE !$$= true)")]
    [InlineData("#elif (RELEASE == $$true)")]
    [InlineData("#elif (RELEASE == $$false)")]
    [InlineData("#elif RELEASE |$$| DEMO")]
    [InlineData("#elif RELEASE &$$& DEMO")]
    [InlineData("#elif ($$ RELEASE && DEMO)")]
    [InlineData("#elif (RELEASE && DEMO $$)")]
    public async Task ElifHasNoQuickinfoAtDifferentPositions(string elif)
    {
        await TestAsync(
@$"
#if DEBUG
{elif}
#endif", "");
    }

    private static QuickInfoProvider CreateProvider()
        => new CSharpSyntacticQuickInfoProvider();

    protected override async Task AssertNoContentAsync(
        EditorTestWorkspace workspace,
        Document document,
        int position)
    {
        var provider = CreateProvider();
        Assert.Null(await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, SymbolDescriptionOptions.Default, CancellationToken.None)));
    }

    protected override async Task AssertContentIsAsync(
        EditorTestWorkspace workspace,
        Document document,
        int position,
        string expectedContent,
        string expectedDocumentationComment = null)
    {
        var provider = CreateProvider();
        var info = await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, SymbolDescriptionOptions.Default, CancellationToken.None));
        Assert.NotNull(info);
        Assert.NotEqual(0, info.RelatedSpans.Length);

        var trackingSpan = new Mock<ITrackingSpan>(MockBehavior.Strict);

        var navigationActionFactory = new NavigationActionFactory(
            document,
            threadingContext: workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
            operationExecutor: workspace.ExportProvider.GetExportedValue<IUIThreadOperationExecutor>(),
            AsynchronousOperationListenerProvider.NullListener,
            streamingPresenter: workspace.ExportProvider.GetExport<IStreamingFindUsagesPresenter>());

        var quickInfoItem = await IntellisenseQuickInfoBuilder.BuildItemAsync(
            trackingSpan.Object, info, document,
            ClassificationOptions.Default, LineFormattingOptions.Default,
            navigationActionFactory, CancellationToken.None);

        var containerElement = quickInfoItem.Item as ContainerElement;

        var textElements = containerElement.Elements.OfType<ClassifiedTextElement>();
        Assert.NotEmpty(textElements);

        var textElement = textElements.First();
        var actualText = string.Concat(textElement.Runs.Select(r => r.Text));
        Assert.Equal(expectedContent, actualText);
    }

    protected override Task TestInMethodAsync(string code, string expectedContent, string expectedDocumentationComment = null)
    {
        return TestInClassAsync(
@"void M()
{" + code + "}", expectedContent, expectedDocumentationComment);
    }

    protected override Task TestInClassAsync(string code, string expectedContent, string expectedDocumentationComment = null)
    {
        return TestAsync(
@"class C
{" + code + "}", expectedContent, expectedDocumentationComment);
    }

    protected override Task TestInScriptAsync(string code, string expectedContent, string expectedDocumentationComment = null)
        => TestAsync(code, expectedContent, expectedContent, Options.Script);

    protected override async Task TestAsync(
        string code,
        string expectedContent,
        string expectedDocumentationComment = null,
        CSharpParseOptions parseOptions = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions);
        var testDocument = workspace.Documents.Single();
        var position = testDocument.CursorPosition.Value;
        var document = workspace.CurrentSolution.Projects.First().Documents.First();

        if (string.IsNullOrEmpty(expectedContent))
        {
            await AssertNoContentAsync(workspace, document, position);
        }
        else
        {
            await AssertContentIsAsync(workspace, document, position, expectedContent, expectedDocumentationComment);
        }
    }
}
