// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.CodingConventions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Tools.CodeFormatter
{
    internal static class CodeFormatter
    {
        const int MaxLoggedWorkspaceWarnings = 5;

        public static async Task<int> FormatWorkspaceAsync(ILogger logger, string solutionOrProjectPath, bool isSolution, bool logAllWorkspaceWarnings, CancellationToken cancellationToken)
        {
            logger.LogInformation(string.Format(Resources.Formatting_code_files_in_workspace_0, solutionOrProjectPath));

            logger.LogTrace(Resources.Loading_workspace);

            var loggedWarningCount = 0;
            var exitCode = 1;
            var workspaceStopwatch = Stopwatch.StartNew();

            var properties = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // This property ensures that XAML files will be compiled in the current AppDomain
                // rather than a separate one. Any tasks isolated in AppDomains or tasks that create
                // AppDomains will likely not work due to https://github.com/Microsoft/MSBuildLocator/issues/16.
                { "AlwaysCompileMarkupFilesInSeparateDomain", bool.FalseString },
            };

            var codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

            using (var workspace = MSBuildWorkspace.Create(properties))
            {
                workspace.WorkspaceFailed += LogWorkspaceWarnings;

                var projectPath = string.Empty;
                if (isSolution)
                {
                    await workspace.OpenSolutionAsync(solutionOrProjectPath, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await workspace.OpenProjectAsync(solutionOrProjectPath, cancellationToken: cancellationToken).ConfigureAwait(false);
                    projectPath = solutionOrProjectPath;
                }

                logger.LogTrace(Resources.Workspace_loaded_in_0_ms, workspaceStopwatch.ElapsedMilliseconds);
                workspaceStopwatch.Restart();

                exitCode = await FormatFilesInWorkspaceAsync(logger, workspace, projectPath, codingConventionsManager, cancellationToken).ConfigureAwait(false);

                logger.LogDebug(Resources.Format_complete_in_0_ms, workspaceStopwatch.ElapsedMilliseconds);
            }

            logger.LogInformation(Resources.Format_complete);

            return exitCode;

            void LogWorkspaceWarnings(object sender, WorkspaceDiagnosticEventArgs args)
            {
                if (args.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    return;
                }

                logger.LogWarning(args.Diagnostic.Message);

                if (!logAllWorkspaceWarnings)
                {
                    loggedWarningCount++;

                    if (loggedWarningCount == MaxLoggedWorkspaceWarnings)
                    {
                        logger.LogWarning(Resources.Maximum_number_of_workspace_warnings_to_log_has_been_reached_Set_the_verbosity_option_to_the_diagnostic_level_to_see_all_warnings);
                        ((MSBuildWorkspace)sender).WorkspaceFailed -= LogWorkspaceWarnings;
                    }
                }
            }
        }

        private static async Task<int> FormatFilesInWorkspaceAsync(ILogger logger, Workspace workspace, string projectPath, ICodingConventionsManager codingConventionsManager, CancellationToken cancellationToken)
        {
            var projectIds = workspace.CurrentSolution.ProjectIds.ToImmutableArray();
            var optionsApplier = new EditorConfigOptionsApplier();

            foreach (var projectId in projectIds)
            {
                var project = workspace.CurrentSolution.GetProject(projectId);
                if (!string.IsNullOrEmpty(projectPath) && !project.FilePath.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug(Resources.Skipping_referenced_project_0, project.Name);
                    continue;
                }

                if (project.Language != LanguageNames.CSharp && project.Language != LanguageNames.VisualBasic)
                {
                    logger.LogWarning(Resources.Could_not_format_0_Format_currently_supports_only_CSharp_and_Visual_Basic_projects, project.FilePath);
                    continue;
                }

                logger.LogInformation(Resources.Formatting_code_files_in_project_0, project.Name);

                var formattedSolution = await FormatFilesInProjectAsync(logger, project, codingConventionsManager, optionsApplier, cancellationToken).ConfigureAwait(false);
                if (!workspace.TryApplyChanges(formattedSolution))
                {
                    logger.LogError(Resources.Failed_to_save_formatting_changes);
                    return 1;
                }
            }

            return 0;
        }

        private static async Task<Solution> FormatFilesInProjectAsync(ILogger logger, Project project, ICodingConventionsManager codingConventionsManager, EditorConfigOptionsApplier optionsApplier, CancellationToken cancellationToken)
        {
            var isCommentTrivia = project.Language == LanguageNames.CSharp
                ? IsCSharpCommentTrivia
                : IsVisualBasicCommentTrivia;

            var formattedDocuments = new List<(DocumentId documentId, Task<SourceText> formatTask)>();
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.Solution.GetDocument(documentId);
                if (!document.SupportsSyntaxTree)
                {
                    continue;
                }

                var formatTask = Task.Run(async () =>
                {
                    var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    if (GeneratedCodeUtilities.IsGeneratedCode(syntaxTree, isCommentTrivia, cancellationToken))
                    {
                        return null;
                    }

                    logger.LogTrace(Resources.Formatting_code_file_0, Path.GetFileName(document.FilePath));

                    OptionSet documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

                    var codingConventionsContext = await codingConventionsManager.GetConventionContextAsync(document.FilePath, cancellationToken).ConfigureAwait(false);
                    if (codingConventionsContext?.CurrentConventions != null)
                    {
                        documentOptions = optionsApplier.ApplyConventions(documentOptions, codingConventionsContext.CurrentConventions, project.Language);
                    }

                    var formattedDocument = await Formatter.FormatAsync(document, documentOptions, cancellationToken).ConfigureAwait(false);
                    return await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                }, cancellationToken);

                formattedDocuments.Add((documentId, formatTask));
            }

            var formattedSolution = project.Solution;
            foreach (var (documentId, formatTask) in formattedDocuments)
            {
                var text = await formatTask.ConfigureAwait(false);
                if (text is null)
                {
                    continue;
                }

                formattedSolution = formattedSolution.WithDocumentText(documentId, text);
            }

            return formattedSolution;
        }

        private static Func<SyntaxTrivia, bool> IsCSharpCommentTrivia =
            (syntaxTrivia) => syntaxTrivia.IsKind(CSharp.SyntaxKind.SingleLineCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.MultiLineCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia)
                || syntaxTrivia.IsKind(CSharp.SyntaxKind.MultiLineDocumentationCommentTrivia);

        private static Func<SyntaxTrivia, bool> IsVisualBasicCommentTrivia =
            (syntaxTrivia) => syntaxTrivia.IsKind(VisualBasic.SyntaxKind.CommentTrivia)
                || syntaxTrivia.IsKind(VisualBasic.SyntaxKind.DocumentationCommentTrivia);
    }
}
