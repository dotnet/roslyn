// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Xunit;
using Xunit.Abstractions;

#if COHOSTING
namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
#else
namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
#endif

[Collection(HtmlFormattingCollection.Name)]
public class OnTypeFormattingTest(FormattingTestContext context, HtmlFormattingFixture fixture, ITestOutputHelper testOutput)
    : FormattingTestBase(context, fixture.Service, testOutput), IClassFixture<FormattingTestContext>
{
    [FormattingTestFact]
    public async Task FormatsElseCloseBrace()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @page "/"

                    @if (true)
                    {
                        <option value="@streetKind">@streetKind</option>
                    }
                    else {
                        <input />
                    }$$

                    @code {
                        private string? streetKind;
                    }
                    """,
            expected: """
                    @page "/"

                    @if (true)
                    {
                        <option value="@streetKind">@streetKind</option>
                    }
                    else
                    {
                        <input />
                    }

                    @code {
                        private string? streetKind;
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task FormatsIfStatementInComponent()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @using Microsoft.AspNetCore.Components.Forms

                    @if (true)
                    {
                        <InputSelect TValue="string" DisplayName="asdf">
                            <option>asdf</option>
                        </InputSelect>
                    }$$
                    """,
            expected: """
                    @using Microsoft.AspNetCore.Components.Forms
                    
                    @if (true)
                    {
                        <InputSelect TValue="string" DisplayName="asdf">
                            <option>asdf</option>
                        </InputSelect>
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task FormatsIfStatementInComponent2()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @using Microsoft.AspNetCore.Components.Forms

                    @if (true)
                    {<InputSelect TValue="string" DisplayName="asdf">
                            <option>asdf</option>
                        </InputSelect>
                    }$$
                    """,
            expected: """
                    @using Microsoft.AspNetCore.Components.Forms
                    
                    @if (true)
                    {
                        <InputSelect TValue="string" DisplayName="asdf">
                            <option>asdf</option>
                        </InputSelect>
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_Class_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public class Foo{}$$
                    }
                    """,
            expected: """
                    @code {
                        public class Foo { }
                    }
                    """,
            triggerCharacter: '}',
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    public async Task CloseCurly_Class_SingleLine_UseTabsAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public class Foo{}$$
                    }
                    """,
            expected: """
                    @code {
                    	public class Foo { }
                    }
                    """,
            triggerCharacter: '}',
            insertSpaces: false,
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    public async Task CloseCurly_Class_SingleLine_AdjustTabSizeAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public class Foo{}$$
                    }
                    """,
            expected: """
                    @code {
                          public class Foo { }
                    }
                    """,
            triggerCharacter: '}',
            tabSize: 6,
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    public async Task CloseCurly_Class_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public class Foo{
                    }$$
                    }
                    """,
            expected: """
                    @code {
                        public class Foo
                        {
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_Method_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public void Foo{}$$
                    }
                    """,
            expected: """
                    @code {
                        public void Foo { }
                    }
                    """,
            triggerCharacter: '}',
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    public async Task CloseCurly_Method_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public void Foo{
                    }$$
                    }
                    """,
            expected: """
                    @code {
                        public void Foo
                        {
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_Property_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public string Foo{ get;set;}$$
                    }
                    """,
            expected: """
                    @code {
                        public string Foo { get; set; }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_Property_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public string Foo{
                    get;set;}$$
                    }
                    """,
            expected: """
                    @code {
                        public string Foo
                        {
                            get; set;
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_Property_StartOfBlockAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code { public string Foo{ get;set;}$$
                    }
                    """,
            expected: """
                    @code {
                        public string Foo { get; set; }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task Semicolon_ClassField_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     public class Foo {private int _hello = 0;$$}
                    }
                    """,
            expected: """
                    @code {
                        public class Foo { private int _hello = 0; }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task Semicolon_ClassField_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                        public class Foo{
                    private int _hello = 0;$$ }
                    }
                    """,
            expected: """
                    @code {
                        public class Foo{
                            private int _hello = 0; }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task Semicolon_MethodVariableAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                        public void Foo()
                        {
                                                var hello = 0;$$
                        }
                    }
                    """,
            expected: """
                    @code {
                        public void Foo()
                        {
                            var hello = 0;
                        }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27135")]
    public async Task Semicolon_Fluent_CallAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @implements IDisposable

                    @code{
                        protected override async Task OnInitializedAsync()
                        {
                            hubConnection = new HubConnectionBuilder()
                                .WithUrl(NavigationManager.ToAbsoluteUri("/chathub"))
                                .Build();$$
                        }
                    }

                    """,
            expected: """
                    @implements IDisposable

                    @code{
                        protected override async Task OnInitializedAsync()
                        {
                            hubConnection = new HubConnectionBuilder()
                                .WithUrl(NavigationManager.ToAbsoluteUri("/chathub"))
                                .Build();
                        }
                    }

                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task ClosingBrace_MatchesCSharpIndentationAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @page "/counter"

                    <h1>Counter</h1>

                    <p>Current count: @currentCount</p>

                    <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

                    @code {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                            if (true){
                                }$$
                        }
                    }
                    """,
            expected: """
                    @page "/counter"

                    <h1>Counter</h1>

                    <p>Current count: @currentCount</p>

                    <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

                    @code {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                            if (true)
                            {
                            }
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task ClosingBrace_DoesntMatchCSharpIndentationAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @page "/counter"

                    <h1>Counter</h1>

                    <p>Current count: @currentCount</p>

                    <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

                    @code {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                            if (true){
                                    }$$
                        }
                    }
                    """,
            expected: """
                    @page "/counter"

                    <h1>Counter</h1>

                    <p>Current count: @currentCount</p>

                    <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

                    @code {
                        private int currentCount = 0;

                        private void IncrementCount()
                        {
                            currentCount++;
                            if (true)
                            {
                            }
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
    public async Task CodeBlock_SemiColon_SingleLine1Async()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div></div>
                    @{ Debugger.Launch();$$}
                    <div></div>
                    """,
            expected: """
                    <div></div>
                    @{
                        Debugger.Launch();
                    }
                    <div></div>
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
    public async Task CodeBlock_SemiColon_SingleLine2Async()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div></div>
                    @{     Debugger.Launch(   )     ;$$ }
                    <div></div>
                    """,
            expected: """
                    <div></div>
                    @{
                        Debugger.Launch(); 
                    }
                    <div></div>
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
    public async Task CodeBlock_SemiColon_SingleLine3Async()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>
                        @{     Debugger.Launch(   )     ;$$ }
                    </div>
                    """,
            expected: """
                    <div>
                        @{
                            Debugger.Launch(); 
                        }
                    </div>
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/27102")]
    public async Task CodeBlock_SemiColon_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div></div>
                    @{
                        var abc = 123;$$
                    }
                    <div></div>
                    """,
            expected: """
                    <div></div>
                    @{
                        var abc = 123;
                    }
                    <div></div>
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
    public async Task Switch_Statment_NestedHtml_NestedCodeBlock()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @switch ("asdf")
                    {
                        case "asdf":
                            <div>
                                @if (true)
                                {
                                    <strong></strong>
                                }
                                else if (false)
                                {
                    1.ToString();$$
                                }
                            </div>
                            break;
                    }
                    """,
            expected: """
                    @switch ("asdf")
                    {
                        case "asdf":
                            <div>
                                @if (true)
                                {
                                    <strong></strong>
                                }
                                else if (false)
                                {
                                    1.ToString();
                                }
                            </div>
                            break;
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
    public async Task NestedHtml_NestedCodeBlock()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                    1.ToString();$$
                            }
                        </div>
                    }
                    """,
            expected: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                                1.ToString();
                            }
                        </div>
                    }
                    """,
            triggerCharacter: ';');
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/36390")]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
    public async Task NestedHtml_NestedCodeBlock_EndingBrace()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                                }$$
                        </div>
                    }
                    """,
            expected: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                            }
                        </div>
                    }
                    """,
            triggerCharacter: '}');
    }

    [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/36390")]
    [WorkItem("https://github.com/dotnet/aspnetcore/issues/34319")]
    public async Task NestedHtml_NestedCodeBlock_EndingBrace_WithCode()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                                "asdf".ToString();
                                }$$
                        </div>
                    }
                    """,
            expected: """
                    @if (true)
                    {
                        <div>
                            @if (true)
                            {
                                <strong></strong>
                            }
                            else if (false)
                            {
                                "asdf".ToString();
                            }
                        </div>
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5698")]
    public async Task Semicolon_NoDocumentChanges()
    {
        var input = """
                @page "/"

                @code {
                    void Foo()
                    {
                        DateTime.Now;$$
                    }
                }
                """;

        await RunOnTypeFormattingTestAsync(input, input.Replace("$$", ""), triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5698")]
    public async Task Semicolon_NoDocumentChanges2()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>
                       <h5><a href="#"></a></h5>
                       <div class="collapse">
                          @ChildContent
                       </div>
                    </div>
                    @code {
                       private string ID { get; } = Guid.NewGuid().ToString();

                       [Parameter]
                       public string Title { get; set; }

                       public string Test {get;$$}
                    }
                    """,
            expected: """
                    <div>
                       <h5><a href="#"></a></h5>
                       <div class="collapse">
                          @ChildContent
                       </div>
                    </div>
                    @code {
                       private string ID { get; } = Guid.NewGuid().ToString();

                       [Parameter]
                       public string Title { get; set; }

                       public string Test { get; }
                    }
                    """,
            triggerCharacter: ';',
            tabSize: 3,
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5693")]
    public async Task IfStatementInsideLambda()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code
                    {
                        public RenderFragment RenderFoo()
                        {
                            return (__builder) =>
                            {
                                @if (true)
                                {

                                }$$
                            };
                        }
                    }
                    """,
            expected: """
                    @code
                    {
                        public RenderFragment RenderFoo()
                        {
                            return (__builder) =>
                            {
                                @if (true)
                                {

                                }
                            };
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task ComponentInCSharp1()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                        public void Sink(RenderFragment r)
                        {
                        }
                    
                        public void M()
                        {
                            Sink(@<PageTitle />);$$ // <-- this should not move
                    
                            Console.WriteLine("Hello");
                            Console.WriteLine("World"); // <-- type/replace semicolon here
                        }
                    }
                    """,
            expected: """
                    @code {
                        public void Sink(RenderFragment r)
                        {
                        }
                    
                        public void M()
                        {
                            Sink(@<PageTitle />); // <-- this should not move
                    
                            Console.WriteLine("Hello");
                            Console.WriteLine("World"); // <-- type/replace semicolon here
                        }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task ComponentInCSharp2()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                        public void Sink(RenderFragment r)
                        {
                        }
                    
                        public void M()
                        {
                            Sink(@<PageTitle />); // <-- this should not move
                    
                            Console.WriteLine("Hello");
                            Console.WriteLine("World");$$ // <-- type/replace semicolon here
                        }
                    }
                    """,
            expected: """
                    @code {
                        public void Sink(RenderFragment r)
                        {
                        }
                    
                        public void M()
                        {
                            Sink(@<PageTitle />); // <-- this should not move
                    
                            Console.WriteLine("Hello");
                            Console.WriteLine("World"); // <-- type/replace semicolon here
                        }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/6158")]
    public async Task Format_NestedLambdas()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {

                        protected Action Goo(string input)
                        {
                            return async () =>
                            {
                                foreach (var x in input)
                                {
                                    if (true)
                                    {
                                        await Task.Delay(1);

                                        if (true)
                                        {
                                            // do some stufff
                                            if (true)
                                            {}$$
                                        }
                                    }
                                }
                            };
                        }
                    }
                    """,
            expected: """
                    @code {

                        protected Action Goo(string input)
                        {
                            return async () =>
                            {
                                foreach (var x in input)
                                {
                                    if (true)
                                    {
                                        await Task.Delay(1);

                                        if (true)
                                        {
                                            // do some stufff
                                            if (true)
                                            { }
                                        }
                                    }
                                }
                            };
                        }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    public async Task CloseCurly_UnrelatedEdit_DoesNothing()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>}$$</div>

                    @{
                    	void Test()
                    	{
                    		<span>
                    			Test
                    		</span>
                    	}
                    }
                    """,
            expected: """
                    <div>}</div>
                    
                    @{
                    	void Test()
                    	{
                    		<span>
                    			Test
                    		</span>
                    	}
                    }
                    """,
            triggerCharacter: '}',
            insertSpaces: false);
    }

    [FormattingTestFact]
    public async Task CloseCurly_IfBlock_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @{
                     if(true){}$$
                    }
                    """,
            expected: """
                    @{
                        if (true) { }
                    }
                    """,
            triggerCharacter: '}');
    }

    [FormattingTestTheory]
    [CombinatorialData]
    public async Task CloseCurly_IfBlock_MultiLineAsync(bool inGlobalNamespace)
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @{
                     if(true)
                    {
                     }$$
                    }
                    """,
            expected: """
                    @{
                        if (true)
                        {
                        }
                    }
                    """,
            triggerCharacter: '}',
            inGlobalNamespace: inGlobalNamespace);
    }

    [FormattingTestTheory]
    [CombinatorialData]
    public async Task CloseCurly_MultipleStatementBlocksAsync(bool inGlobalNamespace)
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>
                        @{
                            if(true) { }
                        }
                    </div>

                    @{
                     if(true)
                    {
                     }$$
                    }
                    """,
            expected: """
                    <div>
                        @{
                            if(true) { }
                        }
                    </div>

                    @{
                        if (true)
                        {
                        }
                    }
                    """,
            triggerCharacter: '}',
            inGlobalNamespace: inGlobalNamespace);
    }

    [FormattingTestFact]
    public async Task Semicolon_Variable_SingleLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @{
                     var x = 'foo';$$
                    }
                    """,
            expected: """
                    @{
                        var x = 'foo';
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task Semicolon_Variable_MultiLineAsync()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @{
                     var x = @"
                    foo";$$
                    }
                    """,
            expected: """
                    @{
                        var x = @"
                    foo";
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task Semicolon_PropertyGet()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    @code {
                     private string Name {get;$$}
                    }
                    """,
            expected: """
                    @code {
                        private string Name { get; }
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task Semicolon_AddsLineAtEndOfDocument()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>
                    </div>
                    @{ var x = new HtmlString("sdf");$$ }
                    """,
            expected: """
                    <div>
                    </div>
                    @{
                        var x = new HtmlString("sdf"); 
                    }
                    """,
            triggerCharacter: ';');
    }

    [FormattingTestFact]
    public async Task FormatsSimpleHtmlTag_OnType()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <html>
                    <head>
                        <title>Hello</title>
                            <script>
                                var x = 2;$$
                            </script>
                    </head>
                    </html>
                    """,
            expected: """
                    <html>
                    <head>
                        <title>Hello</title>
                        <script>
                            var x = 2;
                        </script>
                    </head>
                    </html>
                    """,
            triggerCharacter: ';',
            fileKind: RazorFileKind.Legacy);
    }

    [FormattingTestFact]
    public async Task OnTypeFormatting_Enabled()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
            @functions {
            	private int currentCount = 0;
            
            	private void IncrementCount (){
            		currentCount++;
            	}$$
            }
            """,
            expected: """
            @functions {
                private int currentCount = 0;
            
                private void IncrementCount()
                {
                    currentCount++;
                }
            }
            """,
            triggerCharacter: '}');
    }

    [FormattingTestFact]
    [WorkItem("https://github.com/dotnet/razor/issues/11117")]
    public async Task SemiColon_DoesntBreakHtmlAttributes()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <PageTitle>AI!</PageTitle>

                    <div>
                        <div class="asdf"
                             style="text-color: black"
                             accesskey="GF">
                            Hello there
                        </div>
                    </div>

                    @code {
                     public class Foo{}$$
                    }
                    """,
            expected: """
                    <PageTitle>AI!</PageTitle>
                    
                    <div>
                        <div class="asdf"
                             style="text-color: black"
                             accesskey="GF">
                            Hello there
                        </div>
                    </div>

                    @code {
                        public class Foo { }
                    }
                    """,
            triggerCharacter: '}',
            expectedChangedLines: 1);
    }

    [FormattingTestFact]
    public async Task Semicolon_VariableDeclaration()
    {
        await RunOnTypeFormattingTestAsync(
            input: """
                    <div>
                        @if (true)
                        {


                        var a = 1;$$
                        }
                    </div>
                    """,
            expected: """
                    <div>
                        @if (true)
                        {
                    
                    
                            var a = 1;
                        }
                    </div>
                    """,
            triggerCharacter: ';');
    }
}
