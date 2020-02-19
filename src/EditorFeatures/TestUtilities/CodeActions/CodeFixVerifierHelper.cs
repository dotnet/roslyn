// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using Xunit;

#if CODE_STYLE
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Internal.Options;
using Microsoft.CodeAnalysis.Text;
#else
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal static class CodeFixVerifierHelper
    {
        public const string DefaultRootFilePath = @"z:\";

        public static void VerifyStandardProperties(DiagnosticAnalyzer analyzer, bool verifyHelpLink = false)
        {
            VerifyMessageTitle(analyzer);
            VerifyMessageDescription(analyzer);

            if (verifyHelpLink)
            {
                VerifyMessageHelpLinkUri(analyzer);
            }
        }

        private static void VerifyMessageTitle(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    // The title only displayed for rule configuration
                    continue;
                }

                Assert.NotEqual("", descriptor.Title?.ToString() ?? "");
            }
        }

        private static void VerifyMessageDescription(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (ShouldSkipMessageDescriptionVerification(descriptor))
                {
                    continue;
                }

                Assert.NotEqual("", descriptor.MessageFormat?.ToString() ?? "");
            }

            return;

            // Local function
            static bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    if (!descriptor.IsEnabledByDefault || descriptor.DefaultSeverity == DiagnosticSeverity.Hidden)
                    {
                        // The message only displayed if either enabled and not hidden, or configurable
                        return true;
                    }
                }

                return false;
            }
        }

        private static void VerifyMessageHelpLinkUri(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                Assert.NotEqual("", descriptor.HelpLinkUri ?? "");
            }
        }

        public static Solution ApplyOptions(Solution solution, OptionsCollection options)
        {
            // For CodeStyle layer testing, we create an .editorconfig at project root
            // to apply the options as workspace options are not available in CodeStyle layer.
            // Otherwise, we apply the options directly to the workspace.

#if CODE_STYLE
            // We need to ensure that our projects/documents are rooted for
            // execution from CodeStyle layer as we will be adding a rooted .editorconfig to each project
            // to apply the options.
            if (!options.IsEmpty)
            {
                solution = MakeProjectsAndDocumentsRooted(solution);
                solution = AddAnalyzerConfigDocumentWithOptions(solution, options);
            }
#else
            var optionSet = solution.Options;
            foreach (var (key, value) in options)
            {
                optionSet = optionSet.WithChangedOption(key, value);
            }

            solution = solution.WithOptions(optionSet);
#endif

            return solution;
        }

#if CODE_STYLE
        private static Solution MakeProjectsAndDocumentsRooted(Solution solution)
        {
            var newSolution = solution;
            var projectNameSuffix = 0;
            var documentNameSuffix = 0;
            foreach (var projectId in solution.ProjectIds)
            {
                var project = newSolution.GetProject(projectId);

                string projectRootFilePath;
                if (!PathUtilities.IsAbsolute(project.FilePath))
                {
                    projectRootFilePath = DefaultRootFilePath;
                    var projectFilePath = project.FilePath;
                    if (projectFilePath == null)
                    {
                        projectFilePath = project.Language == LanguageNames.CSharp ? $"project{projectNameSuffix}.csproj" : $"project{projectNameSuffix}.vbproj";
                        projectNameSuffix++;
                    }

                    newSolution = newSolution.WithProjectFilePath(projectId, Path.Combine(projectRootFilePath, projectFilePath));
                }
                else
                {
                    projectRootFilePath = PathUtilities.GetPathRoot(project.FilePath);
                }

                foreach (var documentId in project.DocumentIds)
                {
                    var document = newSolution.GetDocument(documentId);
                    if (!PathUtilities.IsAbsolute(document.FilePath))
                    {
                        var documentFilePath = document.FilePath;
                        if (documentFilePath == null)
                        {
                            documentFilePath = project.Language == LanguageNames.CSharp ? $"Test{documentNameSuffix}.cs" : $"Test{documentNameSuffix}.vb";
                            documentNameSuffix++;
                        }

                        newSolution = newSolution.WithDocumentFilePath(documentId, Path.Combine(projectRootFilePath, documentFilePath));
                    }
                    else
                    {
                        Assert.Equal(projectRootFilePath, PathUtilities.GetPathRoot(document.FilePath));
                    }
                }
            }

            return newSolution;
        }

        private static Solution AddAnalyzerConfigDocumentWithOptions(Solution solution, OptionsCollection options)
        {
            Debug.Assert(options != null);
            var analyzerConfigText = GenerateAnalyzerConfigText(options);

            var newSolution = solution;
            foreach (var project in solution.Projects)
            {
                Assert.True(PathUtilities.IsAbsolute(project.FilePath));
                var projectRootFilePath = PathUtilities.GetPathRoot(project.FilePath);
                var documentId = DocumentId.CreateNewId(project.Id);
                newSolution = newSolution.AddAnalyzerConfigDocument(
                    documentId,
                    ".editorconfig",
                    SourceText.From(analyzerConfigText),
                    filePath: Path.Combine(projectRootFilePath, ".editorconfig"));
            }

            return newSolution;

            static string GenerateAnalyzerConfigText(OptionsCollection options)
            {
                var textBuilder = new StringBuilder();

                foreach (var (optionKey, value) in options)
                {
                    foreach (var location in optionKey.Option.StorageLocations)
                    {
                        if (location is IEditorConfigStorageLocation2 editorConfigStorageLocation)
                        {
                            var editorConfigString = editorConfigStorageLocation.GetEditorConfigString(value, default);
                            if (editorConfigString != null)
                            {
                                textBuilder.AppendLine(GetSectionHeader(optionKey));
                                textBuilder.AppendLine(editorConfigString);
                                textBuilder.AppendLine();
                                break;
                            }

                            Assert.False(true, "Unexpected non-editorconfig option");
                        }
                    }
                }

                return textBuilder.ToString();

                static string GetSectionHeader(OptionKey optionKey)
                {
                    if (optionKey.Option.IsPerLanguage)
                    {
                        switch (optionKey.Language)
                        {
                            case LanguageNames.CSharp:
                                return "[*.cs]";
                            case LanguageNames.VisualBasic:
                                return "[*.vb]";
                        }
                    }

                    return "[*]";
                }
            }
        }
#endif
    }
}
