// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Generator;

public class RazorBenchmarks : AbstractBenchmark
{
    [Benchmark]
    public GeneratorDriver Razor_Add_Independent() => RunRazorBenchmark(Independent, "Independent.razor", replaceExisting: false);

    [Benchmark]
    public GeneratorDriver Razor_Edit_Independent() => RunRazorBenchmark(Independent, "\\0.razor");

    [Benchmark]
    public GeneratorDriver Razor_Remove_Independent() => RunRazorBenchmark(null, "\\0.razor");

    [Benchmark]
    public GeneratorDriver Razor_Edit_DependentIgnorable() => RunRazorBenchmark(DependentIgnorable, "Counter.razor");

    [Benchmark]
    public GeneratorDriver Razor_Edit_Dependent() => RunRazorBenchmark(Dependent, "Counter.razor");

    [Benchmark]
    public GeneratorDriver Razor_Remove_Dependent() => RunRazorBenchmark(null, "\\Counter.razor");


    private GeneratorDriver RunRazorBenchmark(string? AddedFileContent, string FilePath, bool replaceExisting = true) => RunBenchmark((ProjectSetup.RazorProject project) =>
    {
        var removedFile = replaceExisting
                            ? project.AdditionalTexts.Single(a => a.Path.EndsWith(FilePath, StringComparison.OrdinalIgnoreCase))
                            : null;

        var addedFile = AddedFileContent is not null
                            ? new ProjectSetup.InMemoryAdditionalText(AddedFileContent, replaceExisting ? removedFile!.Path : FilePath)
                            : null;

        if (addedFile is not null && removedFile is not null)
        {
            return project.GeneratorDriver.ReplaceAdditionalText(removedFile, addedFile);
        }
        else if (addedFile is not null)
        {
            return project.GeneratorDriver.AddAdditionalTexts(ImmutableArray.Create((AdditionalText)addedFile));
        }
        else if (removedFile is not null)
        {
            return project.GeneratorDriver.RemoveAdditionalTexts(ImmutableArray.Create(removedFile));
        }

        return project.GeneratorDriver;
    });


    private const string Independent = "<h1>Independent file</h1>";

    private const string DependentIgnorable = """
        @page "/counter"

        <PageTitle>Counter</PageTitle>

        <h1>Counter edited</h1>

        <p role="status">Current count: @currentCount</p>

        <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

        @code {
            [Parameter]
            public int IncrementAmount { get; set; } = 1; 

            private int currentCount = 0;

            private void IncrementCount()
            {
                currentCount += IncrementAmount;
            }
        }

        """;

    private const string Dependent = """
        @page "/counter"

        <PageTitle>Counter</PageTitle>

        <h1>Counter edited dependent</h1>

        <p role="status">Current count: @currentCount</p>

        <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

        @code {
            
            private int currentCount = 0;

            private void IncrementCount()
            {
                currentCount++;
            }
        }

        """;
}
