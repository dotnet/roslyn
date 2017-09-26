using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicProjectFile : ProjectFile
    {
        public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject, string errorMessage)
            : base(loader, loadedProject, errorMessage)
        {
        }

        public override SourceCodeKind GetSourceCodeKind(string documentFileName)
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

        public override async Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken)
        {
            var buildInfo = await BuildAsync(cancellationToken).ConfigureAwait(false);

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
            return executedProject.GetItems("VbcCommandLineArgs");
        }

        private ProjectFileInfo CreateProjectFileInfo(BuildInfo buildInfo)
        {
            var project = buildInfo.Project;

            var projectDirectory = GetProjectDirectory(project);

            var commandLineArgs = this.GetCommandLineArgsFromModel(project)
                .Select(item => item.ItemSpec)
                .Where(item => !item.StartsWith("/"))
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
                this.GetProjectReferences(buildInfo.Project),
                buildInfo.ErrorMessage);
        }
    }
}
