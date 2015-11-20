// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.VisualStudio.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow.CSharp.Commands
{
    /// <summary>
    /// Represents a files command which can be run from a REPL window.
    /// </summary>
    [Export(typeof(IInteractiveWindowCommand))]
    [ContentType(InteractiveCommandsContentTypes.CSharpInteractiveCommandContentTypeName)]
    internal sealed class ImportsCommand : IInteractiveWindowCommand
    {
        private const string CommandName = "imports";
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public ImportsCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        string IInteractiveWindowCommand.Description
        {
            get { return InteractiveEditorFeaturesResources.ImportsCommandDescription; }
        }

        IEnumerable<string> IInteractiveWindowCommand.DetailedDescription
        {
            get { return null; }
        }

        IEnumerable<string> IInteractiveWindowCommand.Names
        {
            get { yield return CommandName; }
        }

        string IInteractiveWindowCommand.CommandLine
        {
            get { return null; }
        }

        IEnumerable<KeyValuePair<string, string>> IInteractiveWindowCommand.ParametersDescription
        {
            get { return null; }
        }

        async Task<ExecutionResult> IInteractiveWindowCommand.Execute(IInteractiveWindow window, string arguments)
        {
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failure;
            }

            var evaluator = window.Evaluator as InteractiveEvaluator;
            if (evaluator == null)
            {
                await window.ErrorOutputWriter.WriteLineAsync(string.Format(InteractiveEditorFeaturesResources.EvaluatorRequired, CommandName));
                return ExecutionResult.Failure;
            }

            var currentSubmissionProject = evaluator.CurrentSubmissionProject;
            var currentSubmission = (CSharpCompilation)await InteractiveCommandHelpers.FindSubmissionCompilationAsync(currentSubmissionProject);

            ImmutableArray<string> globalImportStrings;
            ImmutableArray<string> localImportStrings;

            if (currentSubmission != null)
            {
                globalImportStrings = CSharpScriptCompiler.GetImportStrings(currentSubmission.GlobalImports);
                localImportStrings = CSharpScriptCompiler.GetImportStrings(currentSubmission.GetPreviousSubmissionImports().Concat(currentSubmission.GetSubmissionImports()));
            }
            else
            {
                // Special case: before any expression has been compiled (a reasonable time to ask about imports),
                // there is no compilation from which to retrieve the bound imports.  Instead we print out the
                // imports from the CompilationOptions (verbatim).
                var compilationOptions = (CSharpCompilationOptions)currentSubmissionProject.CompilationOptions;
                globalImportStrings = compilationOptions.Usings.SelectAsArray(@using => $"{ScriptingResources.UnresolvedImport}: {@using}");
                localImportStrings = ImmutableArray<string>.Empty;
            }

            await ScriptingCommandHelpers.WriteImportsAsync(window.OutputWriter, globalImportStrings, localImportStrings);

            return ExecutionResult.Success;
        }

        IEnumerable<ClassificationSpan> IInteractiveWindowCommand.ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            return Enumerable.Empty<ClassificationSpan>();
        }

        private void ReportInvalidArguments(IInteractiveWindow window)
        {
            var commands = (IInteractiveWindowCommands)window.Properties[typeof(IInteractiveWindowCommands)];
            commands.DisplayCommandUsage(this, window.ErrorOutputWriter, displayDetails: false);
        }
    }
}

