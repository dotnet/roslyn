// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Debugging;

/// <summary>
/// Tests for <see cref="Handler.Debugging.DebuggerCompletionHandler"/>
/// </summary>
public sealed class DebuggerCompletionHandlerTests : AbstractLanguageServerProtocolTests
{
    public DebuggerCompletionHandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    private static async Task<LSP.VSInternalCompletionList?> GetDebuggerCompletionAsync(
        TestLspServer testLspServer,
        string expression,
        int cursorOffset,
        LSP.CompletionContext? context = null)
    {
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var completionParams = new LSP.DebuggerCompletionParams
        {
            TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = caretLocation.DocumentUri },
            StatementRange = caretLocation.Range,
            Expression = expression,
            CursorOffset = cursorOffset,
            Context = context
        };

        return await testLspServer.ExecuteRequestAsync<LSP.DebuggerCompletionParams, LSP.VSInternalCompletionList?>(
            LSP.Methods.RoslynDebuggerCompletionName,
            completionParams,
            CancellationToken.None);
    }

    private static string GetDocumentationText(LSP.SumType<string, LSP.MarkupContent>? documentation)
    {
        if (documentation == null)
            return string.Empty;

        return documentation.Value.Match(s => s, m => m.Value);
    }

    private static async Task<LSP.CompletionItem> ResolveCompletionItemAsync(
        TestLspServer testLspServer,
        LSP.VSInternalCompletionList completionList,
        LSP.CompletionItem item)
    {
        // Promote list-level Data to item (emulating client behavior)
        item.Data ??= completionList.Data;

        return (await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(
            LSP.Methods.TextDocumentCompletionResolveName,
            item,
            CancellationToken.None))!;
    }

    [Theory, CombinatorialData]
    public async Task ReturnsCompletionForLocalVariable(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    string myLocalVar = "hello";
                    System.Console.WriteLine(myLocalVar);{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "myLocalVar.", cursorOffset: 11);

        Assert.Contains(result!.Items, item => item.Label == "Length");
        Assert.Contains(result.Items, item => item.Label == "Substring");
    }

    [Theory, CombinatorialData]
    public async Task ReturnsCompletionForParameter(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M(string param)
                {
                    System.Console.WriteLine(param);{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "param.", cursorOffset: 6);

        Assert.Contains(result!.Items, item => item.Label == "Length");
    }

    [Theory, CombinatorialData]
    public async Task ReturnsCompletionForFieldAccess(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                private string _field = "hello";

                void M()
                {
                    System.Console.WriteLine(_field);{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "_field.", cursorOffset: 7);

        Assert.Contains(result!.Items, item => item.Label == "Length");
    }

    [Theory, CombinatorialData]
    public async Task ReturnsCompletionWithUsings(bool mutatingLspWorkspace)
    {
        var markup = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    int x = 1;{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "List", cursorOffset: 4);

        Assert.Contains(result!.Items, item => item.Label.StartsWith("List"));
    }

    [Theory, CombinatorialData]
    public async Task ReturnsNullForMissingDocument(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    int x = 1;
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        // Create a params with a non-existent document URI
        var completionParams = new LSP.DebuggerCompletionParams
        {
            TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = new LSP.DocumentUri("file:///nonexistent.cs") },
            StatementRange = new LSP.Range { Start = new LSP.Position(0, 0), End = new LSP.Position(0, 0) },
            Expression = "x",
            CursorOffset = 1
        };

        var result = await testLspServer.ExecuteRequestAsync<LSP.DebuggerCompletionParams, LSP.VSInternalCompletionList?>(
            LSP.Methods.RoslynDebuggerCompletionName,
            completionParams,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsNullForNegativeCursorOffset(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    int x = 1;{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "x", cursorOffset: -1);

        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsNullForCursorOffsetBeyondExpression(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    int x = 1;{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "x", cursorOffset: 5);

        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task HandlesEmptyExpression(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void Main()
                {
                    int x = 1;{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "", cursorOffset: 0);

        Assert.Contains(result!.Items, item => item.Label == "x");
        Assert.Contains(result.Items, item => item.Label == "Main");
    }

    [Theory, CombinatorialData]
    public async Task HandlesTriggerCharacter(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    string s = "hello";{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var result = await GetDebuggerCompletionAsync(
            testLspServer,
            expression: "s.",
            cursorOffset: 2,
            context: new LSP.CompletionContext
            {
                TriggerKind = LSP.CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = "."
            });

        Assert.Contains(result!.Items, item => item.Label == "Length");
    }

    [Theory, CombinatorialData]
    public async Task CompletionInForLoopScope(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    for (int i = 0; i < 10; i++)
                        System.Console.WriteLine(i);{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "i", cursorOffset: 1);

        Assert.Contains(result!.Items, item => item.Label == "i");
    }

    [Theory, CombinatorialData]
    public async Task CompletionWithGenericTypes(bool mutatingLspWorkspace)
    {
        var markup = """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<int>();
                    list.Add(1);{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "list.", cursorOffset: 5);

        Assert.Contains(result!.Items, item => item.Label == "Add");
        Assert.Contains(result.Items, item => item.Label == "Count");
    }

    [Theory, CombinatorialData]
    public async Task CompletionItemResolveAfterDebuggerCompletion(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    string myVar = "hello";{|caret:|}
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var completionList = await GetDebuggerCompletionAsync(testLspServer, expression: "myVar.", cursorOffset: 6);

        var lengthItem = completionList!.Items.FirstOrDefault(item => item.Label == "Length");
        Assert.NotNull(lengthItem);

        var resolvedItem = await ResolveCompletionItemAsync(testLspServer, completionList, lengthItem);
        Assert.Equal("Length", resolvedItem.Label);
        Assert.NotNull(resolvedItem.Documentation);
    }

    [Theory, CombinatorialData]
    public async Task CompletionForMemberInSameFile(bool mutatingLspWorkspace)
    {
        // Test completion for members declared in the same file as the breakpoint
        var markup = """
            class C
            {
                void M()
                {
                    int x = 1;{|caret:|}
                }

                /// <summary>Method documentation.</summary>
                void M2() { }

                /// <summary>Property documentation.</summary>
                int MyProperty { get; set; }

                /// <summary>Field documentation.</summary>
                int _field;
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "this.", cursorOffset: 5);

        var m2Item = Assert.Single(result!.Items, item => item.Label == "M2");
        Assert.Equal(LSP.CompletionItemKind.Method, m2Item.Kind);

        var propertyItem = Assert.Single(result.Items, item => item.Label == "MyProperty");
        Assert.Equal(LSP.CompletionItemKind.Property, propertyItem.Kind);

        var fieldItem = Assert.Single(result.Items, item => item.Label == "_field");
        Assert.Equal(LSP.CompletionItemKind.Field, fieldItem.Kind);

        Assert.Contains(result.Items, item => item.Label == "GetType");
        Assert.Contains(result.Items, item => item.Label == "ToString");

        var resolvedM2 = await ResolveCompletionItemAsync(testLspServer, result, m2Item);
        Assert.Contains("Method documentation", GetDocumentationText(resolvedM2.Documentation));

        var resolvedProperty = await ResolveCompletionItemAsync(testLspServer, result, propertyItem);
        Assert.Contains("Property documentation", GetDocumentationText(resolvedProperty.Documentation));

        var resolvedField = await ResolveCompletionItemAsync(testLspServer, result, fieldItem);
        Assert.Contains("Field documentation", GetDocumentationText(resolvedField.Documentation));
    }

    [Theory, CombinatorialData]
    public async Task CompletionItemResolveForMemberInSameFile(bool mutatingLspWorkspace)
    {
        // Test that resolve works for a member declared in the same file
        var markup = """
            class C
            {
                void M()
                {
                    int x = 1;{|caret:|}
                }

                /// <summary>
                /// Helper method for testing.
                /// </summary>
                void M2() { }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var completionList = await GetDebuggerCompletionAsync(testLspServer, expression: "this.", cursorOffset: 5);

        var m2Item = completionList!.Items.FirstOrDefault(item => item.Label == "M2");
        Assert.NotNull(m2Item);

        var resolvedItem = await ResolveCompletionItemAsync(testLspServer, completionList, m2Item);
        Assert.Equal("M2", resolvedItem.Label);
        Assert.Contains("Helper method", GetDocumentationText(resolvedItem.Documentation));
    }

    [Theory, CombinatorialData]
    public async Task DoesNotOfferExtensionMethodsFromUnimportedNamespaces(bool mutatingLspWorkspace)
    {
        // Extension methods from unimported namespaces should not appear in debugger completion
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
                        System.Console.WriteLine(o);{|caret:|}
                    }
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        // Enable import completion so extension methods from unimported namespaces would
        // normally be offered. ForceExpandedCompletionIndexCreation makes the test deterministic.
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, true);

        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "o.", cursorOffset: 2);
        Assert.Contains(result!.Items, item => item.Label == "ToString");
        Assert.DoesNotContain(result.Items, item => item.Label == "ExtensionMethod");
    }

    [Theory, CombinatorialData]
    public async Task ReturnsNullForUnsupportedLanguage(bool mutatingLspWorkspace)
    {
        var markup = """
            Class C
                Sub M()
                    Dim x As Integer = 1{|caret:|}
                End Sub
            End Class
            """;

        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        var result = await GetDebuggerCompletionAsync(testLspServer, expression: "x", cursorOffset: 1);

        Assert.Null(result);
    }
}
