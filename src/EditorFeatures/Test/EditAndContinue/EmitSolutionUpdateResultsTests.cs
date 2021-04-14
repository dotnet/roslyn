﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class EmitSolutionUpdateResultsTests
    {
        [Fact]
        public async Task GetHotReloadDiagnostics()
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);

            var sourcePath = Path.Combine(TempRoot.Root, "x", "a.cs");
            var razorPath = Path.Combine(TempRoot.Root, "a.razor");

            var document = workspace.CurrentSolution.
                AddProject("proj", "proj", LanguageNames.CSharp).
                WithMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).
                AddDocument(sourcePath, SourceText.From("class C {}", Encoding.UTF8), filePath: Path.Combine(TempRoot.Root, sourcePath));

            var solution = document.Project.Solution;

            var diagnosticData = ImmutableArray.Create(
                new DiagnosticData(
                    id: "CS0001",
                    category: "Test",
                    message: "warning",
                    enuMessageForBingSearch: "test2 message format",
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray.Create("Test2"),
                    properties: ImmutableDictionary<string, string?>.Empty,
                    document.Project.Id,
                    new DiagnosticDataLocation(document.Id, new TextSpan(1, 2), "a.cs", 0, 0, 0, 5, "a.razor", 10, 10, 10, 15),
                    language: "C#",
                    title: "title",
                    description: "description",
                    helpLink: "http://link"),
                new DiagnosticData(
                    id: "CS0012",
                    category: "Test",
                    message: "error",
                    enuMessageForBingSearch: "test2 message format",
                    severity: DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray.Create("Test2"),
                    properties: ImmutableDictionary<string, string?>.Empty,
                    document.Project.Id,
                    new DiagnosticDataLocation(document.Id, new TextSpan(1, 2), originalFilePath: sourcePath, 0, 0, 0, 5, mappedFilePath: @"..\a.razor", 10, 10, 10, 15),
                    language: "C#",
                    title: "title",
                    description: "description",
                    helpLink: "http://link"));

            var rudeEdits = ImmutableArray.Create(
                (document.Id, ImmutableArray.Create(new RudeEditDiagnostic(RudeEditKind.Insert, TextSpan.FromBounds(1, 10), 123, new[] { "a" }))),
                (document.Id, ImmutableArray.Create(new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(1, 10), 123, new[] { "b" }))));

            var actual = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, diagnosticData, rudeEdits, CancellationToken.None);

            AssertEx.Equal(new[]
            {
                $@"Error CS0012: {razorPath} (10,10)-(10,15): error",
                $@"Error ENC0021: {sourcePath} (0,1)-(0,10): {string.Format(FeaturesResources.Adding_0_will_prevent_the_debug_session_from_continuing, "a")}",
                $@"Error ENC0033: {sourcePath} (0,1)-(0,10): {string.Format(FeaturesResources.Deleting_0_will_prevent_the_debug_session_from_continuing, "b")}"
            }, actual.Select(d => $"{d.Severity} {d.Id}: {d.FilePath} {d.Span.GetDebuggerDisplay()}: {d.Message}"));
        }
    }
}
