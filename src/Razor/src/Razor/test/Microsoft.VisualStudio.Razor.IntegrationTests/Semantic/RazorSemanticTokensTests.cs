// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public class RazorSemanticTokensTests(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    private static string? s_projectPath;

    // WARNING: If you leave this as "true" it will cause the semantic tokens tests to change their expected values.
    // Do NOT check in set to true.
    protected bool GenerateBaselines { get; set; } = false;

    [IdeFact]
    public void GenerateBaselines_MustBeFalse()
    {
        Assert.False(GenerateBaselines, "Don't forget to set this back to false before you open a PR :)");
    }

    [IdeFact]
    public async Task GenericTypeParameters_Work()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.MainLayoutFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync($@"@page ""/counter""
@using Microsoft.AspNetCore.Components.Forms

<ValidationMessage For=""() => input.Hashers"" />
<h1></h1>", ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 1);

        // Assert
        var expectedClassifications = await GetExpectedClassificationSpansAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyGetClassificationsAsync(expectedClassifications, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/razor/issues/5595")] // Broken in FUSE due to @inherits bug
    public async Task Components_AreColored()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.MainLayoutFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(RazorProjectConstants.MainLayoutContent, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);

        // Assert
        var expectedClassifications = await GetExpectedClassificationSpansAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyGetClassificationsAsync(expectedClassifications, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Edits_UpdateColors()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.MainLayoutFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(RazorProjectConstants.MainLayoutContent, ControlledHangMitigatingCancellationToken);

        // Act
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);

        await TestServices.Editor.SetTextAsync(RazorProjectConstants.IndexPageContent, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken, count: 3);

        // Assert
        var expectedClassifications = await GetExpectedClassificationSpansAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyGetClassificationsAsync(expectedClassifications, ControlledHangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task Directives_AreColored()
    {
        // Arrange
        await TestServices.SolutionExplorer.OpenFileAsync(RazorProjectConstants.BlazorProjectName, RazorProjectConstants.CounterRazorFile, ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.WaitForComponentClassificationAsync(ControlledHangMitigatingCancellationToken);

        // Act and Assert
        var expectedClassifications = await GetExpectedClassificationSpansAsync(ControlledHangMitigatingCancellationToken);
        await TestServices.Editor.VerifyGetClassificationsAsync(expectedClassifications, ControlledHangMitigatingCancellationToken);
    }

    private async Task<IEnumerable<ClassificationSpan>> GetExpectedClassificationSpansAsync(CancellationToken cancellationToken, [CallerMemberName] string? testName = null)
    {
        var snapshot = await TestServices.Editor.GetActiveSnapshotAsync(ControlledHangMitigatingCancellationToken);

        if (GenerateBaselines)
        {
            var actual = await TestServices.Editor.GetClassificationsAsync(cancellationToken);
            GenerateSemanticBaseline(actual, testName.AssumeNotNull());
        }

        var expectedClassifications = await ReadSemanticBaselineAsync(snapshot, testName.AssumeNotNull(), cancellationToken);

        return expectedClassifications;
    }

    private async Task<IEnumerable<ClassificationSpan>> ReadSemanticBaselineAsync(ITextSnapshot snapshot, string testName, CancellationToken cancellationToken)
    {
        var baselinePath = $"Semantic/TestFiles/{nameof(RazorSemanticTokensTests)}/{testName}.txt";
        var assembly = GetType().GetTypeInfo().Assembly;
        var semanticFile = TestFile.Create(baselinePath, assembly);

        var semanticStr = await semanticFile.ReadAllTextAsync(cancellationToken);

        return ParseSemanticBaseline(semanticStr, snapshot);

        static IEnumerable<ClassificationSpan> ParseSemanticBaseline(string semanticStr, ITextSnapshot snapshot)
        {
            var result = new List<ClassificationSpan>();
            var strArray = semanticStr.Split(new[] { Separator.ToString(), Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < strArray.Length; i += 3)
            {
                if (!int.TryParse(strArray[i], out var position))
                {
                    throw new InvalidOperationException($"{strArray[i]} was not an int {i}");
                }

                if (!int.TryParse(strArray[i + 1], out var length))
                {
                    throw new InvalidOperationException($"{strArray[i + 1]} was not an int {i}");
                }

                var snapshotSpan = new SnapshotSpan(snapshot, position, length);

                var classification = strArray[i + 2];
                var classificationType = new ClassificationType(classification);

                result.Add(new ClassificationSpan(snapshotSpan, classificationType));
            }

            return result;
        }
    }

    private const char Separator = ',';

    private static void GenerateSemanticBaseline(IEnumerable<ClassificationSpan> actual, string baselineFileName)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var baseline in actual)
        {
            builder.Append(baseline.Span.Start.Position).Append(Separator);
            builder.Append(baseline.Span.Length).Append(Separator);

            var classification = baseline.ClassificationType;
            string? classificationStr = null;
            if (classification.BaseTypes.Count() > 1)
            {
                foreach (ILayeredClassificationType baseType in classification.BaseTypes)
                {
                    if (baseType.Layer == ClassificationLayer.Semantic)
                    {
                        classificationStr = baseType.Classification;
                        break;
                    }
                }

                if (classificationStr is null)
                {
                    Assert.Fail("Tried to write layered classifications without Semantic layer");
                    throw new Exception();
                }
            }
            else
            {
                classificationStr = classification.Classification;
            }

            builder.Append(classificationStr).Append(Separator);
            builder.AppendLine();
        }

        var semanticBaselinePath = GetBaselineFileName(baselineFileName);
        File.WriteAllText(semanticBaselinePath, builder.ToString());
    }

    private static string GetBaselineFileName(string testName)
    {
        s_projectPath ??= TestProject.GetProjectDirectory(typeof(RazorSemanticTokensTests), layer: TestProject.Layer.Tooling, useCurrentDirectory: true);
        var semanticBaselinePath = Path.Combine(s_projectPath, "Semantic", "TestFiles", nameof(RazorSemanticTokensTests), testName + ".txt");
        return semanticBaselinePath;
    }

    private class ClassificationComparer : IEqualityComparer<ClassificationSpan>
    {
        public static ClassificationComparer Instance = new();

        public bool Equals(ClassificationSpan x, ClassificationSpan y)
        {
            var spanEquals = x.Span.Equals(y.Span);
            var classificationEquals = ClassificationTypeComparer.Instance.Equals(x.ClassificationType, y.ClassificationType);
            return classificationEquals && spanEquals;
        }

        public int GetHashCode(ClassificationSpan obj)
        {
            throw new NotImplementedException();
        }
    }

    private class ClassificationTypeComparer : IEqualityComparer<IClassificationType>
    {
        public static ClassificationTypeComparer Instance = new();

        public bool Equals(IClassificationType x, IClassificationType y)
        {
            string xString;
            string yString;

            if (x is ILayeredClassificationType xLayered)
            {
                var baseType = xLayered.BaseTypes.Single(b => b is ILayeredClassificationType bLayered && bLayered.Layer == ClassificationLayer.Semantic);
                xString = baseType.Classification;
            }
            else
            {
                xString = x.Classification;
            }

            if (y is ILayeredClassificationType yLayered)
            {
                var baseType = yLayered.BaseTypes.Single(b => b is ILayeredClassificationType bLayered && bLayered.Layer == ClassificationLayer.Semantic);
                yString = baseType.Classification;
            }
            else
            {
                yString = y.Classification;
            }

            return xString.Equals(yString);
        }

        public int GetHashCode(IClassificationType obj)
        {
            throw new NotImplementedException();
        }
    }

    private class ClassificationType : IClassificationType
    {
        public ClassificationType(string classification)
        {
            Classification = classification;
        }

        public string Classification { get; }

        public IEnumerable<IClassificationType> BaseTypes => Array.Empty<IClassificationType>();

        public bool IsOfType(string type)
        {
            throw new NotImplementedException();
        }
    }
}
