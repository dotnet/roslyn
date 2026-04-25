// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;
using AssertEx = Roslyn.Test.Utilities.AssertEx;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Cohost.Formatting;

public class HtmlFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    public async Task FormatsComponentTags()
    {
        await RunFormattingTestAsync(
            input: """
                       <PageTitle>
                        @if(true){
                            <p>@DateTime.Now</p>
                    }
                    </PageTitle>

                        <GridTable>
                        @foreach (var row in rows){
                            <GridRow @onclick="SelectRow(row)">
                            @foreach (var cell in row){
                        <GridCell>@cell</GridCell>}</GridRow>
                        }
                    </GridTable>
                    """,
            htmlFormatted: """
                    <PageTitle>
                        @if(true){
                        <p>@DateTime.Now</p>
                        }
                    </PageTitle>

                    <GridTable>
                        @foreach (var row in rows){
                        <GridRow @onclick="SelectRow(row)">
                            @foreach (var cell in row){
                            <GridCell>@cell</GridCell>}
                        </GridRow>
                        }
                    </GridTable>
                    """,
            expected: """
                    <PageTitle>
                        @if (true)
                        {
                            <p>@DateTime.Now</p>
                        }
                    </PageTitle>

                    <GridTable>
                        @foreach (var row in rows)
                        {
                            <GridRow @onclick="SelectRow(row)">
                                @foreach (var cell in row)
                                {
                                    <GridCell>@cell</GridCell>
                                }
                            </GridRow>
                        }
                    </GridTable>
                    """);
    }

    [Fact]
    public async Task FormatsComponentTag_WithImplicitExpression()
    {
        await RunFormattingTestAsync(
            input: """
                        <GridTable>
                            <GridRow >
                        <GridCell>@cell</GridCell>
                    <GridCell>cell</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@cell</GridCell>
                            <GridCell>cell</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@cell</GridCell>
                            <GridCell>cell</GridCell>
                        </GridRow>
                    </GridTable>
                    """);
    }

    [Fact]
    public async Task FormatsComponentTag_WithExplicitExpression()
    {
        await RunFormattingTestAsync(
            input: """
                        <GridTable>
                            <GridRow >
                        <GridCell>@(cell)</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@(cell)</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@(cell)</GridCell>
                        </GridRow>
                    </GridTable>
                    """);
    }

    [Fact]
    public async Task FormatsComponentTag_WithExplicitExpression_FormatsInside()
    {
        await RunFormattingTestAsync(
            input: """
                        <GridTable>
                            <GridRow >
                        <GridCell>@(""  +    "")</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@(""  +    "")</GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                        <GridRow>
                            <GridCell>@("" + "")</GridCell>
                        </GridRow>
                    </GridTable>
                    """);
    }

    [Fact]
    public async Task FormatsComponentTag_WithExplicitExpression_MovesStart()
    {
        await RunFormattingTestAsync(
            input: """
                        <GridTable>
                            <GridRow >
                        <GridCell>
                        @(""  +    "")
                        </GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <GridRow>
                            <GridCell>
                                @(""  +    "")
                            </GridCell>
                        </GridRow>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                        <GridRow>
                            <GridCell>
                                @("" + "")
                            </GridCell>
                        </GridRow>
                    </GridTable>
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/30382")]
    public async Task FormatNestedComponents2()
    {
        await RunFormattingTestAsync(
            input: """
                    <GridTable>
                    <ChildContent>
                    <GridRow>
                    <ChildContent>
                    <GridCell>
                    <ChildContent>
                    <strong></strong>
                    @if (true)
                    {
                    <strong></strong>
                    }
                    <strong></strong>
                    </ChildContent>
                    </GridCell>
                    </ChildContent>
                    </GridRow>
                    </ChildContent>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <ChildContent>
                            <GridRow>
                                <ChildContent>
                                    <GridCell>
                                        <ChildContent>
                                            <strong></strong>
                                            @if (true)
                                            {
                                            <strong></strong>
                                            }
                                            <strong></strong>
                                        </ChildContent>
                                    </GridCell>
                                </ChildContent>
                            </GridRow>
                        </ChildContent>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                        <ChildContent>
                            <GridRow>
                                <ChildContent>
                                    <GridCell>
                                        <ChildContent>
                                            <strong></strong>
                                            @if (true)
                                            {
                                                <strong></strong>
                                            }
                                            <strong></strong>
                                        </ChildContent>
                                    </GridCell>
                                </ChildContent>
                            </GridRow>
                        </ChildContent>
                    </GridTable>
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8227")]
    public async Task FormatNestedComponents3()
    {
        await RunFormattingTestAsync(
            input: """
                    @if (true)
                    {
                        <Component1 Id="comp1"
                                Caption="Title" />
                    <Component1 Id="comp2"
                                Caption="Title">
                                <Frag>
                    <Component1 Id="comp3"
                                Caption="Title" />
                                </Frag>
                                </Component1>
                    }

                    @if (true)
                    {
                        <a_really_long_tag_name Id="comp1"
                                Caption="Title" />
                    <a_really_long_tag_name Id="comp2"
                                Caption="Title">
                                <a_really_long_tag_name>
                    <a_really_long_tag_name Id="comp3"
                                Caption="Title" />
                                </a_really_long_tag_name>
                                </a_really_long_tag_name>
                    }
                    """,
            htmlFormatted: """
                    @if (true)
                    {
                    <Component1 Id="comp1"
                                Caption="Title" />
                    <Component1 Id="comp2"
                                Caption="Title">
                        <Frag>
                            <Component1 Id="comp3"
                                        Caption="Title" />
                        </Frag>
                    </Component1>
                    }

                    @if (true)
                    {
                    <a_really_long_tag_name Id="comp1"
                                            Caption="Title" />
                    <a_really_long_tag_name Id="comp2"
                                            Caption="Title">
                        <a_really_long_tag_name>
                            <a_really_long_tag_name Id="comp3"
                                                    Caption="Title" />
                        </a_really_long_tag_name>
                    </a_really_long_tag_name>
                    }
                    """,
            expected: """
                    @if (true)
                    {
                        <Component1 Id="comp1"
                                    Caption="Title" />
                        <Component1 Id="comp2"
                                    Caption="Title">
                            <Frag>
                                <Component1 Id="comp3"
                                            Caption="Title" />
                            </Frag>
                        </Component1>
                    }

                    @if (true)
                    {
                        <a_really_long_tag_name Id="comp1"
                                                Caption="Title" />
                        <a_really_long_tag_name Id="comp2"
                                                Caption="Title">
                            <a_really_long_tag_name>
                                <a_really_long_tag_name Id="comp3"
                                                        Caption="Title" />
                            </a_really_long_tag_name>
                        </a_really_long_tag_name>
                    }
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8228")]
    public async Task FormatNestedComponents4()
    {
        await RunFormattingTestAsync(
            input: """
                    @{
                        RenderFragment fragment =
                          @<Component1 Id="Comp1"
                                     Caption="Title">
                        </Component1>;
                    }
                    """,
            htmlFormatted: """
                    @{
                        RenderFragment fragment =
                          @<Component1 Id="Comp1"
                                       Caption="Title">
                    </Component1>;
                    }
                    """,
            expected: """
                    @{
                        RenderFragment fragment =
                          @<Component1 Id="Comp1"
                                       Caption="Title">
                          </Component1>;
                    }
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/8229")]
    public async Task FormatNestedComponents5()
    {
        await RunFormattingTestAsync(
            input: """
                    <Component1>
                        @{
                            RenderFragment fragment =
                            @<Component1 Id="Comp1"
                                     Caption="Title">
                            </Component1>;
                        }
                    </Component1>
                    """,
            htmlFormatted: """
                    <Component1>
                        @{
                        RenderFragment fragment =
                        @<Component1 Id="Comp1"
                                     Caption="Title">
                        </Component1>;
                        }
                    </Component1>
                    """,
            expected: """
                    <Component1>
                        @{
                            RenderFragment fragment =
                            @<Component1 Id="Comp1"
                                         Caption="Title">
                            </Component1>;
                        }
                    </Component1>
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/30382")]
    public async Task FormatNestedComponents2_Range()
    {
        await RunFormattingTestAsync(
            input: """
                    <GridTable>
                    <ChildContent>
                    <GridRow>
                    <ChildContent>
                    <GridCell>
                    <ChildContent>
                    <strong></strong>
                    @if (true)
                    {
                    [|<strong></strong>|]
                    }
                    <strong></strong>
                    </ChildContent>
                    </GridCell>
                    </ChildContent>
                    </GridRow>
                    </ChildContent>
                    </GridTable>
                    """,
            htmlFormatted: """
                    <GridTable>
                        <ChildContent>
                            <GridRow>
                                <ChildContent>
                                    <GridCell>
                                        <ChildContent>
                                            <strong></strong>
                                            @if (true)
                                            {
                                            <strong></strong>
                                            }
                                            <strong></strong>
                                        </ChildContent>
                                    </GridCell>
                                </ChildContent>
                            </GridRow>
                        </ChildContent>
                    </GridTable>
                    """,
            expected: """
                    <GridTable>
                    <ChildContent>
                    <GridRow>
                    <ChildContent>
                    <GridCell>
                    <ChildContent>
                    <strong></strong>
                    @if (true)
                    {
                                                <strong></strong>
                    }
                    <strong></strong>
                    </ChildContent>
                    </GridCell>
                    </ChildContent>
                    </GridRow>
                    </ChildContent>
                    </GridTable>
                    """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/6211")]
    public async Task FormatCascadingValueWithCascadingTypeParameter()
    {
        await RunFormattingTestAsync(
            input: """

                    <div>
                        @foreach ( var i in new int[] { 1, 23 } )
                        {
                            <div></div>
                        }
                    </div>
                    <Select TValue="string">
                        @foreach ( var i in new int[] { 1, 23 } )
                        {
                            <SelectItem Value="@i">@i</SelectItem>
                        }
                    </Select>
                    """,
            htmlFormatted: """

                    <div>
                        @foreach ( var i in new int[] { 1, 23 } )
                        {
                        <div></div>
                        }
                    </div>
                    <Select TValue="string">
                        @foreach ( var i in new int[] { 1, 23 } )
                        {
                        <SelectItem Value="@i">@i</SelectItem>
                        }
                    </Select>
                    """,
            expected: """

                    <div>
                        @foreach (var i in new int[] { 1, 23 })
                        {
                            <div></div>
                        }
                    </div>
                    <Select TValue="string">
                        @foreach (var i in new int[] { 1, 23 })
                        {
                            <SelectItem Value="@i">@i</SelectItem>
                        }
                    </Select>
                    """);
    }

    [Fact]
    public async Task PreprocessorDirectives()
    {
        await RunFormattingTestAsync(
            input: """
                <div Model="SomeModel">
                <div />
                @{
                #if DEBUG
                }
                 <div />
                @{
                #endif
                }
                </div>

                @code {
                    private object SomeModel {get;set;}
                }
                """,
            htmlFormatted: """
                <div Model="SomeModel">
                    <div />
                    @{
                    #if DEBUG
                    }
                    <div />
                    @{
                    #endif
                    }
                </div>

                @code {
                    private object SomeModel {get;set;}
                }
                """,
            expected: """
                    <div Model="SomeModel">
                        <div />
                        @{
                    #if DEBUG
                    }
                     <div />
                    @{
                    #endif

                        }
                    </div>

                    @code {
                        private object SomeModel { get; set; }
                    }
                    """,
            allowDiagnostics: true);
    }

    [Theory]
    [InlineData(AttributeIndentStyle.AlignWithFirst)]
    [InlineData(AttributeIndentStyle.IndentByOne)]
    internal Task HtmlAttributes_FirstNotOnSameLine(AttributeIndentStyle attributeIndentStyle)
        => RunAttributeIndentStyleTestAsync(
            input: """
                <div
                                class="my-class"
                    id="my-id">
                    Content
                    </div>
                        <div
                class="my-class"
                    id="my-id">
                    Content
                    </div>
                """,
            expected: """
                <div
                    class="my-class"
                    id="my-id">
                    Content
                </div>
                <div
                    class="my-class"
                    id="my-id">
                    Content
                </div>
                """,
            attributeIndentStyle);

    [Fact]
    internal Task HtmlAttributes_FirstNotOnSameLine_IndentByTwo()
    => RunAttributeIndentStyleTestAsync(
        input: """
                <div
                                class="my-class"
                    id="my-id">
                    Content
                    </div>
                        <div
                class="my-class"
                    id="my-id">
                    Content
                    </div>
                """,
        expected: """
                <div
                        class="my-class"
                        id="my-id">
                    Content
                </div>
                <div
                        class="my-class"
                        id="my-id">
                    Content
                </div>
                """,
        AttributeIndentStyle.IndentByTwo);

    [Fact]
    public Task VoidTagHelpers_NotIndented()
        => base.RunFormattingTestAsync(
            input: """
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                """,
            htmlFormatted: """
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                """,
            expected: """
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                <input type="checkbox" asp-for="AllowAuthorizationCodeFlow">
                """,
            fileKind: RazorFileKind.Legacy,
            allowDiagnostics: true);

    private async Task RunAttributeIndentStyleTestAsync(string input, string expected, AttributeIndentStyle attributeIndentStyle)
    {
        // This helper method specifically doesn't call the Html formatter, to validate behaviour when it
        // doesn't "fix" the first attribute placement, and put it on the same line as the start tag.

        var document = CreateProjectAndRazorDocument(input);
        var options = ClientSettingsManager.GetClientSettings().ToRazorFormattingOptions();

        var formattingService = (RazorFormattingService)OOPExportProvider.GetExportedValue<IRazorFormattingService>();
        formattingService.GetTestAccessor().SetFormattingLoggerFactory(new TestFormattingLoggerFactory(TestOutputHelper));

        var htmlEdits = new TextEdit[0];
        var edits = await GetFormattingEditsAsync(document, htmlEdits, span: default, options.CodeBlockBraceOnNextLine, attributeIndentStyle, options.InsertSpaces, options.TabSize, RazorCSharpSyntaxFormattingOptions.Default);

        Assert.NotNull(edits);

        var inputText = await document.GetTextAsync(DisposalToken);
        var changes = edits.Select(inputText.GetTextChange);
        var finalText = inputText.WithChanges(changes);

        AssertEx.EqualOrDiff(expected, finalText.ToString());
    }

    private Task RunFormattingTestAsync(
       TestCode input,
       string htmlFormatted,
       string expected)
    {
        return base.RunFormattingTestAsync(
            input,
            htmlFormatted,
            expected,
            additionalFiles: [
                (FilePath("Components.cs"),  """
                    using Microsoft.AspNetCore.Components;
                    namespace Test
                    {
                        public class GridTable : ComponentBase
                        {
                            [Parameter]
                            public RenderFragment ChildContent { get; set; }
                        }
                    
                        public class GridRow : ComponentBase
                        {
                            [Parameter]
                            public RenderFragment ChildContent { get; set; }
                        }
                    
                        public class GridCell : ComponentBase
                        {
                            [Parameter]
                            public RenderFragment ChildContent { get; set; }
                        }
                    
                        public class Component1 : ComponentBase
                        {
                            [Parameter]
                            public string Id { get; set; }
                    
                            [Parameter]
                            public string Caption { get; set; }
                    
                            [Parameter]
                            public RenderFragment Frag {get;set;}
                        }
                    }
                    """),
                (FilePath("Select.razor"), """
                    @typeparam TValue
                    @attribute [CascadingTypeParameter(nameof(TValue))]
                    <CascadingValue Value="@this" IsFixed>
                        <select>
                            @ChildContent
                        </select>
                    </CascadingValue>
                    
                    @code
                    {
                        [Parameter] public TValue SelectedValue { get; set; }
                    }
                    """),
                (FilePath("SelectItem.razor"), """
                    @typeparam TValue
                    <option value="@StringValue">@ChildContent</option>
                    
                    @code
                    {
                        [Parameter] public TValue Value { get; set; }
                        [Parameter] public RenderFragment ChildContent { get; set; }
                    
                        protected string StringValue => Value?.ToString();
                    }
                    """)]);

    }
}
