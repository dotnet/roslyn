// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal static class Extensions
{
    extension(IEnumerable<RudeEditDiagnostic> diagnostics)
    {
        public IEnumerable<RudeEditDiagnosticDescription> ToDescription(SourceText newSource, bool includeFirstLines)
        {
            return diagnostics.Select(d => new RudeEditDiagnosticDescription(
                d.Kind,
                d.Span == default ? null : newSource.ToString(d.Span),
                d.Arguments,
                firstLine: includeFirstLines ? newSource.Lines.GetLineFromPosition(d.Span.Start).ToString().Trim() : null));
        }
    }

    private const string LineSeparator = "\r\n";

    extension(string str)
    {
        public IEnumerable<string> ToLines()
        {
            var i = 0;
            while (true)
            {
                var eoln = str.IndexOf(LineSeparator, i, StringComparison.Ordinal);
                if (eoln < 0)
                {
                    yield return str[i..];
                    yield break;
                }

                yield return str[i..eoln];
                i = eoln + LineSeparator.Length;
            }
        }
    }

    extension(Solution solution)
    {
#nullable enable

        public Project AddTestProject(string projectName, out ProjectId id)
            => AddTestProject(solution, projectName, LanguageNames.CSharp, out id);

        public Project AddTestProject(string projectName, string language, out ProjectId id)
            => AddTestProject(solution, projectName, language, TargetFramework.NetLatest, id = ProjectId.CreateNewId(debugName: projectName));

        public Project AddTestProject(string projectName, string language, TargetFramework targetFramework, out ProjectId id)
        {
            var project = AddTestProject(solution, projectName, language, targetFramework, id: null);
            id = project.Id;
            return project;
        }

        public Project AddTestProject(string projectName, string language = LanguageNames.CSharp, TargetFramework targetFramework = TargetFramework.NetLatest, ProjectId? id = null)
        {
            id ??= ProjectId.CreateNewId(debugName: projectName);

            var info = CreateProjectInfo(projectName, id, language);
            return solution
                .AddProject(info)
                .WithProjectMetadataReferences(id, TargetFrameworkUtil.GetReferences(targetFramework))
                .GetRequiredProject(id);
        }

        public Document AddTestDocument(ProjectId projectId, string source, string path, out DocumentId id)
            => solution.AddDocument(
                id = DocumentId.CreateNewId(projectId),
                name: PathUtilities.GetFileName(path),
                SourceText.From(source, Encoding.UTF8, SourceHashAlgorithms.Default),
                filePath: PathUtilities.IsAbsolute(path)
                    ? path : Path.Combine(Path.GetDirectoryName(solution.GetRequiredProject(projectId).FilePath!)!, path))
            .GetRequiredDocument(id);
    }

    extension(Project project)
    {
        public Document AddTestDocument(string source, string path)
        => project.AddTestDocument(source, path, out _);

        public Document AddTestDocument(string source, string path, out DocumentId id)
            => project.Solution.AddTestDocument(project.Id, source, path, out id);

        public Project WithCompilationOptions<TCompilationOptions>(Func<TCompilationOptions, TCompilationOptions> transform)
            where TCompilationOptions : CompilationOptions
            => project.WithCompilationOptions(transform((TCompilationOptions)(project.CompilationOptions ?? throw ExceptionUtilities.Unreachable())));
    }

    public static Guid CreateProjectTelemetryId(string projectName)
    {
        Assert.True(Encoding.UTF8.GetByteCount(projectName) <= 20, "Use shorter project names in tests");
        return BlobContentId.FromHash(Encoding.UTF8.GetBytes(projectName.PadRight(20, '\0'))).Guid;
    }

    public static ProjectInfo CreateProjectInfo(string projectName, ProjectId id, string language = LanguageNames.CSharp)
        => ProjectInfo.Create(
            id,
            VersionStamp.Create(),
            name: projectName,
            assemblyName: projectName,
            language,
            parseOptions: language switch
            {
                LanguageNames.CSharp => CSharpParseOptions.Default.WithNoRefSafetyRulesAttribute(),
                LanguageNames.VisualBasic => VisualBasicParseOptions.Default,
                NoCompilationConstants.LanguageName => null,
                _ => throw ExceptionUtilities.UnexpectedValue(language)
            },
            compilationOptions: language switch
            {
                LanguageNames.CSharp => TestOptions.DebugDll,
                LanguageNames.VisualBasic => VisualBasic.UnitTests.TestOptions.DebugDll,
                NoCompilationConstants.LanguageName => null,
                _ => throw ExceptionUtilities.UnexpectedValue(language)
            },
            filePath: Path.Combine(TempRoot.Root, projectName, projectName + language switch
            {
                LanguageNames.CSharp => ".csproj",
                LanguageNames.VisualBasic => ".vbproj",
                NoCompilationConstants.LanguageName => ".noproj",
                _ => throw ExceptionUtilities.UnexpectedValue(language)
            }))
            .WithCompilationOutputInfo(new CompilationOutputInfo(
                assemblyPath: Path.Combine(TempRoot.Root, projectName + ".dll"),
                generatedFilesOutputDirectory: null))
            .WithTelemetryId(CreateProjectTelemetryId(projectName));
}
