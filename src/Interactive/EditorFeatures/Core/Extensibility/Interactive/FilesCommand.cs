// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias Scripting;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.Editor.Interactive;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using ScriptingCommandHelpers = Scripting::Microsoft.CodeAnalysis.Scripting.ScriptingCommandHelpers;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    /// <summary>
    /// Represents a files command which can be run from a REPL window.
    /// </summary>
    [Export(typeof(IInteractiveWindowCommand))]
    [ContentType(InteractiveCommandsContentTypes.CSharpInteractiveCommandContentTypeName)]
    [ContentType(InteractiveCommandsContentTypes.VisualBasicInteractiveCommandContentTypeName)]
    internal sealed class FilesCommand : IInteractiveWindowCommand
    {
        private const string CommandName = "files";
        private readonly IStandardClassificationService _registry;

        [ImportingConstructor]
        public FilesCommand(IStandardClassificationService registry)
        {
            _registry = registry;
        }

        string IInteractiveWindowCommand.Description
        {
            get { return InteractiveEditorFeaturesResources.FilesCommandDescription; }
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

            var filePaths = ArrayBuilder<string>.GetInstance();

            var solution = evaluator.CurrentSubmissionProject.Solution;
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null)
                {
                    continue;
                }

                foreach (var tree in compilation.SyntaxTrees)
                {
                    var filePath = tree.FilePath;
                    if (filePath.Length > 0)
                    {
                        filePaths.Add(filePath);
                    }
                }
            }

            await ScriptingCommandHelpers.WriteDistinctFilesAsync(window.OutputWriter, filePaths);

            filePaths.Free();

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

