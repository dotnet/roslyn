// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.SemanticTokens;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public abstract class CohostSemanticTokensEndpointTestBase(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    private protected abstract Task<SemanticTokens?> GetSemanticTokensAsync(TextDocument document, CancellationToken cancellationToken);

    private protected async Task VerifySemanticTokensAsync(
        string input,
        bool colorBackground,
        bool miscellaneousFile,
        RazorFileKind? fileKind = null,
        Action<RazorProjectBuilder>? projectConfigure = null,
        [CallerMemberName] string? testName = null)
    {
        var document = CreateProjectAndRazorDocument(input, fileKind, miscellaneousFile: miscellaneousFile, projectConfigure: projectConfigure);

        // We need to manually initialize the OOP service so we can get semantic token info later
        UpdateClientLSPInitializationOptions(options =>
        {
            return options with
            {
                TokenTypes = SemanticTokensLegendService.TokenTypes.All,
                TokenModifiers = SemanticTokensLegendService.TokenModifiers.All,
            };
        });

        ClientSettingsManager.Update(ClientAdvancedSettings.Default with { ColorBackground = colorBackground });

        var result = await GetSemanticTokensAsync(document, DisposalToken);

        var sourceText = await document.GetTextAsync(DisposalToken);
        var actualFileContents = GetTestOutput(sourceText, result?.Data, SemanticTokensLegendService);

        if (colorBackground)
        {
            testName += "_with_background";
        }

        if (miscellaneousFile)
        {
            testName += "_misc_file";
        }

        var baselineFileName = Path.Combine("TestFiles", this.GetType().Name, $"{testName}.txt");
        if (GenerateBaselines.ShouldGenerate)
        {
            WriteBaselineFile(actualFileContents, baselineFileName);
        }

        var expectedFileContents = GetBaselineFileContents(baselineFileName);
        AssertEx.EqualOrDiff(expectedFileContents, actualFileContents);
    }

    private string GetBaselineFileContents(string baselineFileName)
    {
        var semanticFile = TestFile.Create(baselineFileName, GetType().Assembly);
        if (!semanticFile.Exists())
        {
            return string.Empty;
        }

        return semanticFile.ReadAllText()
            // CI seems to not checkout with auto-crlf, so normalize to Environment.NewLine
            .Replace("\r\n", Environment.NewLine);
    }

    private void WriteBaselineFile(string fileContents, string baselineFileName)
    {
        var projectPath = TestProject.GetProjectDirectory(GetType(), layer: TestProject.Layer.Tooling);
        var baselineFileFullPath = Path.Combine(projectPath, baselineFileName);
        File.WriteAllText(baselineFileFullPath, fileContents);
    }

    private static string GetTestOutput(SourceText sourceText, int[]? data, ISemanticTokensLegendService legend)
    {
        if (data == null)
        {
            return string.Empty;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        builder.AppendLine("Line Δ, Char Δ, Length, Type, Modifier(s), Text");
        var tokenTypes = legend.TokenTypes.All;
        var prevLength = 0;
        var lineIndex = 0;
        var lineOffset = 0;
        for (var i = 0; i < data.Length; i += 5)
        {
            var lineDelta = data[i];
            var charDelta = data[i + 1];
            var length = data[i + 2];

            Assert.False(i != 0 && lineDelta == 0 && charDelta == 0, "line delta and character delta are both 0, which is invalid as we shouldn't be producing overlapping tokens");
            Assert.False(i != 0 && lineDelta == 0 && charDelta < prevLength, "Previous length is longer than char offset from previous start, meaning tokens will overlap");

            if (lineDelta != 0)
            {
                lineOffset = 0;
            }

            lineIndex += lineDelta;
            lineOffset += charDelta;

            var type = tokenTypes[data[i + 3]];
            var modifier = GetTokenModifierString(data[i + 4], legend);
            var text = sourceText.ToString(new TextSpan(sourceText.Lines[lineIndex].Start + lineOffset, length));
            builder.AppendLine($"{lineDelta} {charDelta} {length} {type} {modifier} [{text}]");

            prevLength = length;
        }

        return builder.ToString();
    }

    private static string GetTokenModifierString(int tokenModifiers, ISemanticTokensLegendService legend)
    {
        var modifiers = legend.TokenModifiers.All;

        var modifiersBuilder = ArrayBuilder<string>.GetInstance();
        for (var i = 0; i < modifiers.Length; i++)
        {
            if ((tokenModifiers & (1 << (i % 32))) != 0)
            {
                modifiersBuilder.Add(modifiers[i]);
            }
        }

        return $"[{string.Join(", ", modifiersBuilder.ToArrayAndFree())}]";
    }
}
