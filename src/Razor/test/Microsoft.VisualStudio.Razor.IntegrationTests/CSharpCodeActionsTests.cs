// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class CSharpCodeActionsTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task CSharpCodeActionsTests_MakeExpressionBodiedMethod()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("IncrementCount", charsOffset: 2, occurrence: 2, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions, "Use expression body for method");

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForCurrentLineTextAsync("private void IncrementCount() => currentCount++;", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CSharpCodeActionsTests_FullyQualify()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""

            @{
                var x = ConflictOption.CompareAllSearchableValues;
            }

            """, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("ConflictOption", charsOffset: 0, occurrence: 1, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions,
            "System.Data.ConflictOption",
            "@using System.Data");

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForCurrentLineTextAsync("var x = System.Data.ConflictOption.CompareAllSearchableValues;", ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CSharpCodeActionsTests_AddUsing()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""

            @{
                var x = ConflictOption.CompareAllSearchableValues;
            }

            """, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("ConflictOption", charsOffset: 0, occurrence: 1, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions,
            "@using System.Data",
            "System.Data.ConflictOption");

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForTextChangeAsync("""
            @using System.Data

            @{
                var x = ConflictOption.CompareAllSearchableValues;
            }

            """, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CSharpCodeActionsTests_AddUsing_WithTypo()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""

            @{
                var x = Conflictoption.CompareAllSearchableValues;
            }

            """, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("Conflictoption", charsOffset: 0, occurrence: 1, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions,
            "ConflictOption - @using System.Data");

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForTextChangeAsync("""
            @using System.Data

            @{
                var x = ConflictOption.CompareAllSearchableValues;
            }

            """, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "Failing in CI")]
    public async Task CSharpCodeActionsTests_IntroduceLocal()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""
            @code {
                void M(string[] args)
                {
                    if (args.First().Length == 0)
                    {
                    }

                    if (args.First().Length == 0)
                    {
                    }
                }
            }
            """, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("args.First()", charsOffset: 0, occurrence: 1, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions, "Introduce local");

        Assert.True(codeAction.HasActionSets);

        codeAction = (await codeAction.GetActionSetsAsync(ControlledHangMitigatingCancellationToken)).First().Actions.First();

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForTextChangeAsync("""
                @code {
                    void M(string[] args)
                    {
                        string v = args.First();
                        if (v.Length == 0)
                        {
                        }

                        if (args.First().Length == 0)
                        {
                        }
                    }
                }
                """, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "Failing in CI")]
    public async Task CSharpCodeActionsTests_IntroduceLocal_All()
    {
        // Open the file
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""
            @code {
                void M(string[] args)
                {
                    if (args.First().Length == 0)
                    {
                    }

                    if (args.First().Length == 0)
                    {
                    }
                }
            }
            """, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.PlaceCaretAsync("args.First()", charsOffset: 0, occurrence: 1, extendSelection: false, selectBlock: false, ControlledHangMitigatingCancellationToken);

        // Act
        var codeActions = await TestServices.Editor.InvokeCodeActionListAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var codeAction = VerifyAndGetFirst(codeActions, "Introduce local");

        Assert.True(codeAction.HasActionSets);

        codeAction = (await codeAction.GetActionSetsAsync(ControlledHangMitigatingCancellationToken)).First().Actions.Skip(1).First();

        await TestServices.Editor.InvokeCodeActionAsync(codeAction, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.WaitForTextChangeAsync("""
                @code {
                    void M(string[] args)
                    {
                        string v = args.First();
                        if (v.Length == 0)
                        {
                        }

                        if (v.Length == 0)
                        {
                        }
                    }
                }
                """, ControlledHangMitigatingCancellationToken);
    }

    private ISuggestedAction VerifyAndGetFirst(IEnumerable<SuggestedActionSet> codeActions, params string[] expected)
    {
        foreach (var title in expected)
        {
            Assert.Contains(codeActions, a => a.Actions.Single().DisplayText == title);
        }

        return codeActions.First(a => a.Actions.Single().DisplayText == expected[0]).Actions.Single();
    }
}
