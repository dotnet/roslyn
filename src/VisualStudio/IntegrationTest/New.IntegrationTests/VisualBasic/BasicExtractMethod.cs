// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public class BasicExtractMethod : AbstractEditorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    private const string TestSource = """

        Imports System
        Imports System.Collections.Generic
        Imports System.Linq

        Module Program
            Sub Main(args As String())
                Console.WriteLine("Hello VB!")
            End Sub

            Function F() As Integer
                Dim a As Integer
                Dim b As Integer
                a = 5
                b = 5
                Dim result = a * b
                Return result
            End Function
        End Module
        """;

    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicExtractMethod(ITestOutputHelper testOutputHelper)
        : base(nameof(BasicExtractMethod))
    {
        _testOutputHelper = testOutputHelper;
    }

    private void Log(string message)
        => _testOutputHelper.WriteLine(message);

    [IdeFact]
    public async Task SimpleExtractMethod()
    {
        Log("Setting the editor text to the test source");
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        Log("Placing the caret before 'Console'");
        await TestServices.Editor.PlaceCaretAsync("Console", charsOffset: -1, HangMitigatingCancellationToken);
        Log("Extending the selection to include the WriteLine statement");
        await TestServices.Editor.PlaceCaretAsync("Hello VB!", charsOffset: 3, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        Log("Executing Extract Method");
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Refactor.ExtractMethod, HangMitigatingCancellationToken);
        Log("Waiting for the Extract Method operation to complete");
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ExtractMethod, HangMitigatingCancellationToken);
        MarkupTestFile.GetSpans("""

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    [|NewMethod|]()
                End Sub

                Private Sub [|NewMethod|]()
                    Console.WriteLine("Hello VB!")
                End Sub

                Function F() As Integer
                    Dim a As Integer
                    Dim b As Integer
                    a = 5
                    b = 5
                    Dim result = a * b
                    Return result
                End Function
            End Module
            """, out var expectedText, out var spans);
        Log("Verifying the extracted method appears in the editor text");
        await TestServices.EditorVerifier.TextContainsAsync(expectedText, cancellationToken: HangMitigatingCancellationToken);
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        AssertEx.SetEqual(spans, tags);

        Log("Typing the new method name 'SayHello' and committing the rename");
        await TestServices.Input.SendAsync(["SayHello", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        Log("Verifying the renamed method appears in the editor text");
        await TestServices.EditorVerifier.TextContainsAsync("""
                Private Sub SayHello()
                    Console.WriteLine("Hello VB!")
                End Sub
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ExtractViaCodeAction()
    {
        Log("Setting the editor text to the test source");
        await TestServices.Editor.SetTextAsync(TestSource, HangMitigatingCancellationToken);
        Log("Placing the caret before 'a = 5'");
        await TestServices.Editor.PlaceCaretAsync("a = 5", charsOffset: -1, HangMitigatingCancellationToken);
        Log("Extending the selection through 'a * b'");
        await TestServices.Editor.PlaceCaretAsync("a * b", charsOffset: 1, occurrence: 0, extendSelection: true, selectBlock: false, HangMitigatingCancellationToken);
        Log("Applying the 'Extract method' code action");
        await TestServices.EditorVerifier.CodeActionAsync("Extract method", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        MarkupTestFile.GetSpans("""

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    Console.WriteLine("Hello VB!")
                End Sub

                Function F() As Integer
                    Dim a As Integer
                    Dim b As Integer
                    Dim result As Integer = Nothing
                    [|NewMethod|](a, b, result)
                    Return result
                End Function

                Private Sub [|NewMethod|](ByRef a As Integer, ByRef b As Integer, ByRef result As Integer)
                    a = 5
                    b = 5
                    result = a * b
                End Sub
            End Module
            """, out var expectedText, out var spans);
        Log("Verifying the resulting editor text matches the expected extraction");
        Assert.Equal(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        var tags = (await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken)).SelectAsArray(tag => tag.Span.Span.ToTextSpan());
        AssertEx.SetEqual(spans, tags);
    }
}
