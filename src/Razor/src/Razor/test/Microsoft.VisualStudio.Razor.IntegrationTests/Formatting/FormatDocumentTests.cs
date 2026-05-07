// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class FormatDocumentTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    private static string? s_projectPath;

    // To add new formatting tests create a sample file of the "before" state
    // and place it in the TestFiles\Input folder.
    // If you know the "after" state then you can place that in a file of the same
    // name in the TestFiles\Expected folder, and run tests as normal.
    // If you want to generate the "after" state simple run the test without
    // creating the expected file, and it will be generated for you.
    //
    // NOTE: Formatting must change the input state or the test will hang
    // ie. these tests cannot be used for pure validation

    [IdeTheory]
    [MemberData(nameof(GetFormattingTestResourceNames))]
    public async Task FormattingDocument(string testFileName)
    {
        var inputResourceName = GetResourceName(testFileName, "Input");
        var expectedResourceName = GetResourceName(testFileName, "Expected");

        if (!TryGetResource(inputResourceName, out var input))
        {
            throw new Exception($"Could not get input resource data for '{inputResourceName}'");
        }

        // Open the file
        if (testFileName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
        {
            await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        }
        else
        {
            await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.ErrorCshtmlFile, ControlledHangMitigatingCancellationToken);
        }

        await TestServices.Editor.SetTextAsync(input, ControlledHangMitigatingCancellationToken);

        // Wait for the document to settle
        await TestServices.Editor.WaitForSemanticClassificationAsync("RazorTransition", ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.InvokeFormatDocumentAsync(ControlledHangMitigatingCancellationToken);

        // Assert
        var actual = await TestServices.Editor.WaitForTextChangeAsync(input, ControlledHangMitigatingCancellationToken);

        if (!TryGetResource(expectedResourceName, out var expected))
        {
            // If there was no expected results file, we generate one, but still fail
            // the test so that its impossible to forget to commit the results.
            s_projectPath ??= TestProject.GetProjectDirectory(typeof(FormatDocumentTests), layer: TestProject.Layer.Tooling, useCurrentDirectory: true);
            var path = Path.Combine(s_projectPath, "Formatting", "TestFiles", "Expected");
            var fileName = expectedResourceName.Split(new[] { '.' }, 8).Last();

            File.WriteAllText(Path.Combine(path, fileName), actual);

            throw new Exception("Test did not have expected results file so one has been generated. Running the test again should make it pass.");
        }

        AssertEx.EqualOrDiff(expected, actual);
    }

    private static string GetResourceBaseName()
        => $"{typeof(FormatDocumentTests).Namespace}.Formatting.TestFiles";

    private static string GetResourceName(string name, string suffix)
        => $"{GetResourceBaseName()}.{suffix}.{name}";

    private static bool TryGetResource(string name, [NotNullWhen(true)] out string? value)
    {
        try
        {
            using var expectedStream = typeof(FormatDocumentTests).Assembly.GetManifestResourceStream(name);
            using var sr = new StreamReader(expectedStream);

            value = sr.ReadToEnd();
        }
        catch
        {
            value = null;
            return false;
        }

        return true;
    }

    public static TheoryData<string> GetFormattingTestResourceNames()
    {
        var baseName = $"{GetResourceBaseName()}.Input.";
        var data = new TheoryData<string>();
        var names = typeof(FormatDocumentTests).Assembly.GetManifestResourceNames();

        foreach (var name in names)
        {
            if (name.StartsWith(baseName))
            {
                data.Add(Path.GetFileName(name)[baseName.Length..]);
            }
        }

        return data;
    }
}
