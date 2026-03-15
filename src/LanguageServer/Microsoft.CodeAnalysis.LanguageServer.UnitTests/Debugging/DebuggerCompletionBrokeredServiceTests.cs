// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.DebuggerCompletion;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Debugging;

public sealed class DebuggerCompletionBrokeredServiceTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    private const string MemberAccessExpression = "myLocalVar.";
    private const string LocalVariableExpression = "x";

    private static Task<DebuggerCompletionResult> GetDebuggerCompletionsAsync(
        TestContext context,
        string expression,
        int cursorOffset)
        => context.Service.GetDebuggerCompletionsAsync(
            context.SourceFilePath,
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression,
            cursorOffset,
            CancellationToken.None);

    [Fact]
    public async Task GetDebuggerCompletionsAsync_01()
    {
        // Returns completion items over JsonRpc for a member access expression.
        var markup = """
            class C
            {
                void M()
                {
                    System.String myLocalVar = "hello";/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await context.Service.GetDebuggerCompletionsAsync(
            context.SourceFilePath,
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression: MemberAccessExpression,
            cursorOffset: MemberAccessExpression.Length,
            CancellationToken.None);

        var item = Assert.Single(result.Items, static item => item.Label == "Length");
        Assert.Equal(item.Label, item.InsertText);
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_02()
    {
        // Returns top-level completions for an empty expression.
        var markup = """
            class C
            {
                void M()
                {
                    System.Int32 x = 1;/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await context.Service.GetDebuggerCompletionsAsync(
            context.SourceFilePath,
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression: string.Empty,
            cursorOffset: 0,
            CancellationToken.None);

        Assert.Contains(result.Items, static item => item.Label == "x");
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_03()
    {
        // Returns null when the requested document path is not in the workspace.
        var markup = """
            class C
            {
                void M()
                {
                    System.Int32 x = 1;/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await context.Service.GetDebuggerCompletionsAsync(
            Path.Combine(Path.GetDirectoryName(context.SourceFilePath)!, "Missing.cs"),
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression: LocalVariableExpression,
            cursorOffset: LocalVariableExpression.Length,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_04()
    {
        // Returns null when the cursor offset is negative.
        var markup = """
            class C
            {
                void M()
                {
                    System.Int32 x = 1;/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await context.Service.GetDebuggerCompletionsAsync(
            context.SourceFilePath,
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression: LocalVariableExpression,
            cursorOffset: -1,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_05()
    {
        // Returns null when the cursor offset is past the end of the expression.
        var markup = """
            class C
            {
                void M()
                {
                    System.Int32 x = 1;/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await context.Service.GetDebuggerCompletionsAsync(
            context.SourceFilePath,
            context.StatementEndLine,
            context.StatementEndCharacter,
            expression: LocalVariableExpression,
            cursorOffset: LocalVariableExpression.Length + 1,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_06()
    {
        // Returns completions for parameters in scope.
        var markup = """
            class C
            {
                void M(string param)
                {
                    System.Console.WriteLine(param);/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await GetDebuggerCompletionsAsync(context, "param.", cursorOffset: 6);

        Assert.Contains(result.Items, static item => item.Label == "Length");
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_07()
    {
        // Returns completions for fields in scope.
        var markup = """
            class C
            {
                private string _field = "hello";

                void M()
                {
                    System.Console.WriteLine(_field);/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await GetDebuggerCompletionsAsync(context, "_field.", cursorOffset: 7);

        Assert.Contains(result.Items, static item => item.Label == "Length");
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_08()
    {
        // Returns type completions that rely on existing using directives.
        var markup = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    int x = 1;/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await GetDebuggerCompletionsAsync(context, "List", cursorOffset: 4);

        Assert.Contains(result.Items, static item => item.Label.StartsWith("List", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_09()
    {
        // Returns completions for locals declared in a for-loop scope.
        var markup = """
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10; i++)
                        System.Console.WriteLine(i);/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await GetDebuggerCompletionsAsync(context, "i", cursorOffset: 1);

        Assert.Contains(result.Items, static item => item.Label == "i");
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_10()
    {
        // Returns completions for generic types and member access.
        var markup = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int>();
                    list.Add(1);/*caret*/
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);

        var result = await GetDebuggerCompletionsAsync(context, "list.", cursorOffset: 5);

        Assert.Contains(result.Items, static item => item.Label == "Add");
        Assert.Contains(result.Items, static item => item.Label == "Count");
    }

    [Fact]
    public async Task GetDebuggerCompletionsAsync_11()
    {
        // Debugger completion should not offer extension methods from unimported namespaces.
        var markup = """
            namespace NS2
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this object o) => true;
                }
            }

            namespace NS1
            {
                class C
                {
                    void M(object o)
                    {
                        System.Console.WriteLine(o);/*caret*/
                    }
                }
            }
            """;

        await using var context = await CreateTestContextAsync(markup);
        context.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true);
        context.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, true);

        var result = await GetDebuggerCompletionsAsync(context, "o.", cursorOffset: 2);

        Assert.Contains(result.Items, static item => item.Label == "ToString");
        Assert.DoesNotContain(result.Items, static item => item.Label == "ExtensionMethod");
    }

    private async Task<TestContext> CreateTestContextAsync(string markup)
    {
        const string caretMarker = "/*caret*/";
        var caretPosition = markup.IndexOf(caretMarker, StringComparison.Ordinal);
        Assert.True(caretPosition >= 0);

        var source = markup.Replace(caretMarker, string.Empty, StringComparison.Ordinal);
        var statementEndPosition = Text.SourceText.From(source).Lines.GetLinePosition(caretPosition);

        var directory = TempRoot.CreateDirectory();
        var projectFilePath = Path.Combine(directory.Path, "Test.csproj");
        var sourceFilePath = Path.Combine(directory.Path, "Test.cs");
        File.WriteAllText(sourceFilePath, source);

        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            LoggerFactory, includeDevKitComponents: false, MefCacheDirectory.Path, []);
        await exportProvider.GetExportedValue<BrokeredServices.ServiceBrokerFactory>().CreateAsync();

        var workspaceProjectFactoryServiceInstance = exportProvider.GetExportedValues<IExportedBrokeredService>()
            .OfType<WorkspaceProjectFactoryService>()
            .Single();
        var debuggerCompletionServiceInstance = exportProvider.GetExportedValues<IExportedBrokeredService>()
            .OfType<DebuggerCompletionBrokeredService>()
            .Single();

        var workspaceProjectFactoryProxy = new BrokeredServiceProxy<IWorkspaceProjectFactoryService>(workspaceProjectFactoryServiceInstance);
        var workspaceProjectFactoryService = await workspaceProjectFactoryProxy.GetServiceAsync();
        var workspaceProject = await workspaceProjectFactoryService.CreateAndAddProjectAsync(
            new WorkspaceProjectCreationInfo(LanguageNames.CSharp, "TestProject", projectFilePath, new Dictionary<string, string>()),
            CancellationToken.None);

        await workspaceProject.SetCommandLineArgumentsAsync(["/target:library"], CancellationToken.None);
        await workspaceProject.AddMetadataReferencesAsync(
            [new MetadataReferenceInfo(typeof(object).Assembly.Location, Aliases: null, EmbedInteropTypes: false)],
            CancellationToken.None);
        await workspaceProject.AddSourceFilesAsync([new SourceFileInfo(sourceFilePath, [])], CancellationToken.None);
        await workspaceProject.SetProjectHasAllInformationAsync(true, CancellationToken.None);

        var debuggerCompletionProxy = new BrokeredServiceProxy<IDebuggerCompletionService>(
            debuggerCompletionServiceInstance,
            BrokeredServiceProxyTransport.Json);
        var debuggerCompletionService = await debuggerCompletionProxy.GetServiceAsync();

        return new TestContext(
            exportProvider,
            workspaceProject,
            workspaceProjectFactoryProxy,
            debuggerCompletionProxy,
            debuggerCompletionService,
            sourceFilePath,
            statementEndPosition.Line,
            statementEndPosition.Character);
    }

    private sealed class TestContext(
        VisualStudio.Composition.ExportProvider exportProvider,
        IWorkspaceProject workspaceProject,
        BrokeredServiceProxy<IWorkspaceProjectFactoryService> workspaceProjectFactoryProxy,
        BrokeredServiceProxy<IDebuggerCompletionService> debuggerCompletionProxy,
        IDebuggerCompletionService service,
        string sourceFilePath,
        int statementEndLine,
        int statementEndCharacter) : IAsyncDisposable
    {
        public IDebuggerCompletionService Service { get; } = service;
        public IGlobalOptionService GlobalOptions { get; } = exportProvider.GetExportedValue<IGlobalOptionService>();
        public string SourceFilePath { get; } = sourceFilePath;
        public int StatementEndLine { get; } = statementEndLine;
        public int StatementEndCharacter { get; } = statementEndCharacter;

        public async ValueTask DisposeAsync()
        {
            workspaceProject.Dispose();
            await debuggerCompletionProxy.DisposeAsync();
            await workspaceProjectFactoryProxy.DisposeAsync();
            exportProvider.Dispose();
        }
    }
}
