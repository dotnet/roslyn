// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpProjectFile : ProjectFile
    {
        public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project, string errorMessage)
            : base(loader, project, errorMessage)
        {
        }

        public override SourceCodeKind GetSourceCodeKind(string documentFileName)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return documentFileName.EndsWith(".csx", StringComparison.OrdinalIgnoreCase)
            //    ? SourceCodeKind.Script
            //    : SourceCodeKind.Regular;
            return SourceCodeKind.Regular;
        }

        public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return (sourceCodeKind != SourceCodeKind.Script) ? ".cs" : ".csx";
            return ".cs";
        }

        public override async Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken)
        {
            var buildInfo = await this.BuildAsync(cancellationToken).ConfigureAwait(false);

            if (buildInfo.Project == null)
            {
                return new ProjectFileInfo(
                    commandLineArgs: SpecializedCollections.EmptyEnumerable<string>(),
                    documents: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectFileReference>(),
                    errorMessage: buildInfo.ErrorMessage);
            }

            return CreateProjectFileInfo(buildInfo);
        }

        protected override IEnumerable<MSB.Framework.ITaskItem> GetCommandLineArgsFromModel(MSB.Execution.ProjectInstance executedProject)
        {
            return executedProject.GetItems("CscCommandLineArgs");
        }

        protected override ProjectFileReference CreateProjectFileReference(MSB.Execution.ProjectItemInstance reference)
        {
            var filePath = reference.EvaluatedInclude;
            var aliases = GetAliases(reference);

            return new ProjectFileReference(filePath, aliases);
        }

        private ProjectFileInfo CreateProjectFileInfo(BuildInfo buildInfo)
        {
            var project = buildInfo.Project;
            var projectDirectory = GetProjectDirectory(project);

            var commandLineArgs = this.GetCommandLineArgsFromModel(project)
                .Select(item => item.ItemSpec)
                .Where(item => item.StartsWith("/"))
                .ToImmutableArray();

            if (commandLineArgs.Length == 0)
            {
                return new ProjectFileInfo(
                    commandLineArgs: SpecializedCollections.EmptyEnumerable<string>(),
                    documents: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    additionalDocuments: SpecializedCollections.EmptyEnumerable<DocumentFileInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectFileReference>(),
                    errorMessage: "MSBuild did not return any command-line arguments");
            }

            var docs = this.GetDocumentsFromModel(project)
                .Where(s => !IsTemporaryGeneratedFile(s.ItemSpec))
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            var additionalDocs = this.GetAdditionalFilesFromModel(project)
                .Select(s => MakeDocumentFileInfo(projectDirectory, s))
                .ToImmutableArray();

            return new ProjectFileInfo(
                commandLineArgs,
                docs,
                additionalDocs,
                this.GetProjectReferences(project),
                buildInfo.ErrorMessage);
        }

        private ImmutableArray<string> GetAliases(MSB.Framework.ITaskItem item)
        {
            var aliasesText = item.GetMetadata("Aliases");

            if (string.IsNullOrEmpty(aliasesText))
            {
                return ImmutableArray<string>.Empty;
            }

            return ImmutableArray.CreateRange(aliasesText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
