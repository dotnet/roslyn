// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class TaskListTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    [IdeFact]
    public async Task ShowsTasks()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync("""
            <PageTitle>Title</PageTitle>

            @* TODO: Fill in more content *@

            @code
            {
                // TODO: Fill in more code
            }
            """, ControlledHangMitigatingCancellationToken);

        await TestServices.TaskList.WaitForTaskDescriptorsAsync(ControlledHangMitigatingCancellationToken);

        var tasks = await TestServices.TaskList.WaitForTasksAsync(expectedCount: 2, ControlledHangMitigatingCancellationToken);

        Assert.NotNull(tasks);
        Assert.Collection(tasks.OrderAsArray(),
            static task => Assert.Contains("TODO: Fill in more code", task),
            static task => Assert.Contains("TODO: Fill in more content", task));
    }
}
