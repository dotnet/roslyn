// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.Generator;

public class RazorTests
{
    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Project_Load_Cold()
    {
        // arrange
        var razorBenchmarks = new ColdBenchmarks();

        // act
        razorBenchmarks.Setup();

        // assert
        var project = razorBenchmarks.Project;
        Assert.NotNull(project);
        Assert.NotNull(project.GeneratorDriver);
        Assert.NotNull(project.OptionsProvider);
        Assert.NotNull(project.Compilation);
        Assert.NotNull(project.ParseOptions);

        Assert.Equal(110, project.AdditionalTexts.Length);
        Assert.Equal(8, project.Compilation.SyntaxTrees.Count());

        // Generator driver will throw if it's not been run yet. This checks we're in a cold state.
        Assert.Throws<NullReferenceException>(() => project.GeneratorDriver.GetRunResult());
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Project_Load_Warm()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();

        // act
        razorBenchmarks.Setup();

        // assert
        var project = razorBenchmarks.Project;
        Assert.NotNull(project);

        var results = project.GeneratorDriver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.Equal(110, results.GeneratedTrees.Length);
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Add_Independent()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // act
        var driver = razorBenchmarks.Razor_Add_Independent();

        // assert
        var results = driver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.Equal(111, results.GeneratedTrees.Length);
        Assert.Equal("Independent_razor.g.cs", results.Results[0].GeneratedSources.Last().HintName);
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Edit_Independent()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // check the contents of the generated 0 page
        var initialResults = razorBenchmarks.Project!.GeneratorDriver.GetRunResult();
        Assert.Contains("<h1>Page 0 </h1>", initialResults.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Generated_0_razor.g.cs").SourceText.ToString());

        // act
        var driver = razorBenchmarks.Razor_Edit_Independent();

        // assert
        var results = driver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.Contains("<h1>Independent file</h1>", results.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Generated_0_razor.g.cs").SourceText.ToString());
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Remove_Independent()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // act
        var driver = razorBenchmarks.Razor_Remove_Independent();

        // assert
        var results = driver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.Equal(109, results.GeneratedTrees.Length);
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Edit_DependentIgnorable()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // check the contents of the counter page
        var initialResults = razorBenchmarks.Project!.GeneratorDriver.GetRunResult();
        Assert.Contains("<h1>Counter</h1>", initialResults.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Counter_razor.g.cs").SourceText.ToString());

        // act
        var driver = razorBenchmarks.Razor_Edit_DependentIgnorable();

        // assert
        var results = driver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.Contains("<h1>Counter edited</h1>", results.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Counter_razor.g.cs").SourceText.ToString());
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Edit_Dependent()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // check the contents of the counter and index page
        var initialResults = razorBenchmarks.Project!.GeneratorDriver.GetRunResult();
        Assert.Contains("public int IncrementAmount", initialResults.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Counter_razor.g.cs").SourceText.ToString());
        Assert.Contains("__builder.AddAttribute(6, \"IncrementAmount\", (object)(global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<global::System.Int32>(", initialResults.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Index_razor.g.cs").SourceText.ToString());

        // act
        var driver = razorBenchmarks.Razor_Edit_Dependent();

        // assert
        var results = driver.GetRunResult();
        Assert.Empty(results.Diagnostics);
        Assert.DoesNotContain("public int IncrementAmount", results.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Counter_razor.g.cs").SourceText.ToString());
        Assert.Contains("__builder.AddAttribute(6, \"IncrementAmount\", (object)(\"5\"));", results.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Index_razor.g.cs").SourceText.ToString());
    }

    [Fact(Skip = "https://github.com/dotnet/razor/issues/7982")]
    public void Razor_Remove_Dependent()
    {
        // arrange
        var razorBenchmarks = new RazorBenchmarks();
        razorBenchmarks.Setup();

        // check the contents of the index page
        var initialResults = razorBenchmarks.Project!.GeneratorDriver.GetRunResult();
        Assert.Contains("__builder.OpenComponent<global::SampleApp.Pages.Counter>(5);", initialResults.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Index_razor.g.cs").SourceText.ToString());

        // act
        var driver = razorBenchmarks.Razor_Remove_Dependent();

        // assert
        var results = driver.GetRunResult();
        Assert.Equal(109, results.GeneratedTrees.Length);

        var diagnostic = Assert.Single(results.Diagnostics);
        Assert.Contains("RZ10012: Found markup element with unexpected name 'Counter'.", diagnostic.ToString());

        Assert.Contains("__builder.OpenElement(5, \"Counter\");", results.Results[0].GeneratedSources.Single(r => r.HintName == "Pages_Index_razor.g.cs").SourceText.ToString());
    }
}
