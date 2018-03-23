// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicProjectFile : ProjectFile
    {
        public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject, ProjectBuildManager buildManager, DiagnosticLog log)
            : base(loader, loadedProject, buildManager, log)
        {
        }

        protected override SourceCodeKind GetSourceCodeKind(string documentFileName)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return documentFileName.EndsWith(".vbx", StringComparison.OrdinalIgnoreCase)
            //    ? SourceCodeKind.Script
            //    : SourceCodeKind.Regular;
            return SourceCodeKind.Regular;
        }

        public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return (sourceCodeKind != SourceCodeKind.Script) ? ".vb" : ".vbx";
            return ".vb";
        }

        protected override IEnumerable<MSB.Framework.ITaskItem> GetCommandLineArgsFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems(ItemNames.VbcCommandLineArgs);
        }

        protected override ProjectFileInfo CreateProjectFileInfo(MSB.Execution.ProjectInstance project)
        {
            var projectDirectory = GetProjectDirectory(project);

            var commandLineArgs = this.GetCommandLineArgsFromModel(project)
                .Select(item => item.ItemSpec)
                .Where(item => item.StartsWith("/"))
                .ToImmutableArray();

            if (commandLineArgs.Length == 0)
            {
                // We didn't get any command-line arguments. Try to read them directly from the project.
                commandLineArgs = ReadCommandLineArguments(project);
            }

            commandLineArgs = FixPlatform(commandLineArgs);

            var outputFilePath = project.ReadPropertyString(PropertyNames.TargetPath);
            if (!string.IsNullOrWhiteSpace(outputFilePath))
            {
                outputFilePath = this.GetAbsolutePath(outputFilePath);
            }

            var outputRefFilePath = project.ReadPropertyString(PropertyNames.TargetRefPath);
            if (!string.IsNullOrWhiteSpace(outputRefFilePath))
            {
                outputRefFilePath = this.GetAbsolutePath(outputRefFilePath);
            }

            var targetFramework = project.ReadPropertyString(PropertyNames.TargetFramework);
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                targetFramework = null;
            }

            var docs = project.GetDocumentsFromModel()
                .Where(s => !IsTemporaryGeneratedFile(s.ItemSpec))
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            var additionalDocs = project.GetAdditionalFiles()
                .Select(s => MakeAdditionalDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            return ProjectFileInfo.Create(
                this.Language,
                project.FullPath,
                outputFilePath,
                outputRefFilePath,
                targetFramework,
                commandLineArgs,
                docs,
                additionalDocs,
                this.GetProjectReferences(project),
                this.Log);
        }

        private ImmutableArray<string> FixPlatform(ImmutableArray<string> commandLineArgs)
        {
            string platform = null, target = null;
            var platformIndex = -1;

            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];

                if (platform == null && arg.StartsWith("/platform:", StringComparison.OrdinalIgnoreCase))
                {
                    platform = arg.Substring("/platform:".Length);
                    platformIndex = i;
                }
                else if (target == null && arg.StartsWith("/target:", StringComparison.OrdinalIgnoreCase))
                {
                    target = arg.Substring("/target:".Length);
                }

                if (platform != null && target != null)
                {
                    break;
                }
            }

            if (string.Equals("anycpu32bitpreferred", platform, StringComparison.OrdinalIgnoreCase)
                && (string.Equals("library", target, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("module", target, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("winmdobj", target, StringComparison.OrdinalIgnoreCase)))
            {
                return commandLineArgs.SetItem(platformIndex, "/platform:anycpu");
            }

            return commandLineArgs;
        }

        private ImmutableArray<string> ReadCommandLineArguments(MSB.Execution.ProjectInstance project)
        {
            return VisualBasicCommandLineArgumentReader.Read(project);
        }
    }
}
