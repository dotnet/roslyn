using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceTests
{
    [UseExportProvider]
    public class CompilationTrackerTests
    {
        [Fact]
        public async Task CheckProjectWithDuplicateReferencesIsCompiledTest()
        {
            using (var workspace = new AdhocWorkspace())
            {
                var duplicateProjectId = ProjectId.CreateNewId();

                workspace.AddProject(ProjectInfo.Create(
                    duplicateProjectId,
                    VersionStamp.Create(),
                    "Duplicate",
                    "Duplicate",
                    LanguageNames.CSharp));

                var reference = new ProjectReference(duplicateProjectId);
                var duplicateReference = new ProjectReference(duplicateProjectId);

                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                     VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    projectReferences: ImmutableArray.Create(reference, duplicateReference));

                var projectWithDuplicateReferences = workspace.AddProject(projectInfo);

                var compilation = await projectWithDuplicateReferences.GetCompilationAsync();

                Assert.NotNull(compilation);
            }
        }
    }
}
