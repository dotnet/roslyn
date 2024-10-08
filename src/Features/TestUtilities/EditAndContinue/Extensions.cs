// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal static class Extensions
    {
        public static IEnumerable<RudeEditDiagnosticDescription> ToDescription(this IEnumerable<RudeEditDiagnostic> diagnostics, SourceText newSource, bool includeFirstLines)
        {
            return diagnostics.Select(d => new RudeEditDiagnosticDescription(
                d.Kind,
                d.Span == default ? null : newSource.ToString(d.Span),
                d.Arguments,
                firstLine: includeFirstLines ? newSource.Lines.GetLineFromPosition(d.Span.Start).ToString().Trim() : null));
        }

        private const string LineSeparator = "\r\n";

        public static IEnumerable<string> ToLines(this string str)
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

        public static Project AddTestProject(this Solution solution, string projectName, string language = LanguageNames.CSharp)
            => AddTestProject(solution, projectName, language, out _);

        public static Project AddTestProject(this Solution solution, string projectName, out ProjectId id)
            => AddTestProject(solution, projectName, LanguageNames.CSharp, out id);

        public static Project AddTestProject(this Solution solution, string projectName, string language, out ProjectId id)
        {
            var info = CreateProjectInfo(projectName, language);
            return solution.AddProject(info).GetRequiredProject(id = info.Id);
        }

        public static Guid CreateProjectTelemetryId(string projectName)
        {
            Assert.True(Encoding.UTF8.GetByteCount(projectName) <= 20, "Use shorter project names in tests");
            return BlobContentId.FromHash(Encoding.UTF8.GetBytes(projectName.PadRight(20, '\0'))).Guid;
        }

        public static ProjectInfo CreateProjectInfo(string projectName, string language = LanguageNames.CSharp)
            => ProjectInfo.Create(
                ProjectId.CreateNewId(),
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
                filePath: projectName + language switch
                {
                    LanguageNames.CSharp => ".csproj",
                    LanguageNames.VisualBasic => ".vbproj",
                    NoCompilationConstants.LanguageName => ".noproj",
                    _ => throw ExceptionUtilities.UnexpectedValue(language)
                })
                .WithTelemetryId(CreateProjectTelemetryId(projectName));

    }
}
