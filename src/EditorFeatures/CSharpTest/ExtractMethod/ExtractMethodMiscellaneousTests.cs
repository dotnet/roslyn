// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod;

using static CSharpSyntaxTokens;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public sealed class ExtractMethodMiscellaneousTests
{
    [Fact]
    public void ServiceTest1()
    {
        var markupCode = """
            class A
            {
                /* test */ [|public|] void Test(int i, int b, int c)
                {

                }
            }
            """;
        MarkupTestFile.GetSpan(markupCode, out var code, out var span);

        var root = SyntaxFactory.ParseCompilationUnit(code);
        var result = CSharpSyntaxTriviaService.Instance.SaveTriviaAroundSelection(root, span);

        var rootWithAnnotation = result.Root;

        // find token to replace
        var publicToken = rootWithAnnotation.DescendantTokens().First(t => t.Kind() == SyntaxKind.PublicKeyword);

        // replace the token with new one
        var newRoot = rootWithAnnotation.ReplaceToken(publicToken, PrivateKeyword);

        // restore trivia around it
        var rootWithTriviaRestored = result.RestoreTrivia(newRoot);

        var expected = """
            class A
            {
                /* test */ private void Test(int i, int b, int c)
                {

                }
            }
            """;

        Assert.Equal(expected, rootWithTriviaRestored.ToFullString());
    }

    [Fact]
    public void ServiceTest2()
    {
        var markupCode = """
            class A
            {

            #if true
                [|/* test */ public|] void Test(int i, int b, int c)
                {

                }
            #endif

            }
            """;
        MarkupTestFile.GetSpan(markupCode, out var code, out var span);

        var root = SyntaxFactory.ParseCompilationUnit(code);
        var result = CSharpSyntaxTriviaService.Instance.SaveTriviaAroundSelection(root, span);

        var rootWithAnnotation = result.Root;

        // find token to replace
        var publicToken = rootWithAnnotation.DescendantTokens().First(t => t.Kind() == SyntaxKind.PublicKeyword);

        // replace the token with new one
        var newRoot = rootWithAnnotation.ReplaceToken(publicToken, PrivateKeyword);

        // restore trivia around it
        var rootWithTriviaRestored = result.RestoreTrivia(newRoot);

        var expected = """
            class A
            {

            #if true
                private void Test(int i, int b, int c)
                {

                }
            #endif

            }
            """;

        Assert.Equal(expected, rootWithTriviaRestored.ToFullString());
    }

    [WpfFact]
    public async Task TestExtractMethodCommandHandlerErrorMessage()
    {
        var markupCode = """
            class A
            {
                [|void Method() {}|]
            }
            """;

        await TestCommandHandler(markupCode, result: null, expectNotification: true);
    }

    private static async Task TestCommandHandler(string markupCode, string? result, bool expectNotification)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(markupCode, composition: EditorTestCompositions.EditorFeatures);
        var testDocument = workspace.Documents.Single();

        var view = testDocument.GetTextView();
        view.Selection.Select(new SnapshotSpan(
            view.TextBuffer.CurrentSnapshot, testDocument.SelectedSpans[0].Start, testDocument.SelectedSpans[0].Length), isReversed: false);

        result ??= view.TextBuffer.CurrentSnapshot.GetText();

        var callBackService = (INotificationServiceCallback)workspace.Services.GetRequiredService<INotificationService>();
        var gotNotification = false;
        callBackService.NotificationCallback = (_, _, _) => gotNotification = true;

        var handler = workspace.ExportProvider.GetCommandHandler<ExtractMethodCommandHandler>(PredefinedCommandHandlerNames.ExtractMethod, ContentTypeNames.CSharpContentType);

        handler.ExecuteCommand(new ExtractMethodCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create());

        var waiter = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.ExtractMethod);
        await waiter.ExpeditedWaitAsync();

        Assert.Equal(expectNotification, gotNotification);
        Assert.Equal(result, view.TextBuffer.CurrentSnapshot.GetText());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/65465")]
    public async Task TestExtractLocalFunctionInTopLevelFromCommandHandler()
    {
        var markupCode = """
            System.Console.WriteLine([|"string"|]);
            """;

        await TestCommandHandler(markupCode, """
            System.Console.WriteLine(NewMethod());
            
            static string NewMethod()
            {
                return "string";
            }
            """, expectNotification: false);
    }
}
