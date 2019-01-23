// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static CSharpHelpers;

    [UseExportProvider]
    public class SourceFileHandlingTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveSourceFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                // Add source file
                var sourceFileFullPath = @"c:\source.cs";
                project.AddSourceFile(sourceFileFullPath);
                Assert.True(GetCurrentDocuments().Any(s => s.FilePath == sourceFileFullPath));

                // Remove source file
                project.RemoveSourceFile(sourceFileFullPath);
                Assert.Empty(GetCurrentDocuments());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveAdditionalFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<TextDocument> GetCurrentAdditionalDocuments() => environment.Workspace.CurrentSolution.Projects.Single().AdditionalDocuments;
                Assert.Empty(GetCurrentAdditionalDocuments());

                // Add additional file
                var additionalFileFullPath = @"c:\source.cs";
                project.AddAdditionalFile(additionalFileFullPath);
                Assert.True(GetCurrentAdditionalDocuments().Any(s => s.FilePath == additionalFileFullPath));

                // Remove additional file
                project.RemoveAdditionalFile(additionalFileFullPath);
                Assert.Empty(GetCurrentAdditionalDocuments());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFiles_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;
                VersionStamp GetVersion() => environment.Workspace.CurrentSolution.Projects.Single().Version;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";
                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                project.RemoveSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath2);

                project.RemoveSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath4);

                var oldVersion = GetVersion();

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var newVersion = GetVersion();

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var newSameVersion = GetVersion();

                // Reordering should result in a new version if the order is different. If it's the same, the version should stay the same.
                Assert.NotEqual(oldVersion, newVersion);
                Assert.Equal(newVersion, newSameVersion);

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatch_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                // Add a file outside the batch.
                project.AddSourceFile(sourceFileFullPath2);

                project.StartBatch();
                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
                project.RemoveSourceFile(sourceFileFullPath2);

                // Removing path4 to test remove of a file when it was also added in a batch.
                project.RemoveSourceFile(sourceFileFullPath4);

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1 });

                project.EndBatch();

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatchWithReAdding_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                // Add a file outside the batch.
                project.AddSourceFile(sourceFileFullPath2);

                project.StartBatch();
                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
                project.RemoveSourceFile(sourceFileFullPath2);

                // Removing path4 to test remove of a file when it was also added in a batch.
                project.RemoveSourceFile(sourceFileFullPath4);

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1 });

                // Re-adding / re-removing / re-adding again.
                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath4);
                project.RemoveSourceFile(sourceFileFullPath2);
                project.RemoveSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath4);

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                project.EndBatch();

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatchAddAfterReorder_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                project.StartBatch();

                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath2);

                project.ReorderSourceFiles(new[] { sourceFileFullPath2, sourceFileFullPath1 });

                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                project.EndBatch();

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatchRemoveAfterReorder_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                project.StartBatch();

                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                project.RemoveSourceFile(sourceFileFullPath3);
                project.RemoveSourceFile(sourceFileFullPath4);
                project.RemoveSourceFile(sourceFileFullPath5);

                project.EndBatch();

                project.ReorderSourceFiles(new[] { sourceFileFullPath2, sourceFileFullPath1 });

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesExceptions_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";
                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                project.RemoveSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath2);

                project.RemoveSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath4);

                // This should throw due to not passing all of the files.
                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));

                // This should throw because the path does not exist in the project.
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file", sourceFileFullPath2, sourceFileFullPath3, sourceFileFullPath4, sourceFileFullPath5 }));

                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatchExceptions_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                project.StartBatch();

                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file" })); // no files were added, therefore we should get an argument exception
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                project.AddSourceFile(sourceFileFullPath1);

                // Test before we add/remove the rest of source files in the batch.
                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file" }));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                project.AddSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath3);
                project.AddSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath5);

                project.RemoveSourceFile(sourceFileFullPath2);
                project.AddSourceFile(sourceFileFullPath2);

                project.RemoveSourceFile(sourceFileFullPath4);
                project.AddSourceFile(sourceFileFullPath4);

                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file", sourceFileFullPath2, sourceFileFullPath3, sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                project.EndBatch();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void ReorderSourceFilesBatchExceptionRemoveFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";

                project.AddSourceFile(sourceFileFullPath1);
                project.AddSourceFile(sourceFileFullPath2);

                project.StartBatch();

                project.RemoveSourceFile(sourceFileFullPath2);
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath2 }));

                project.EndBatch();

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task AddRemoveSourceFileAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                // Add source file
                var sourceFileFullPath = @"c:\source.cs";
                await project.AddSourceFileAsync(sourceFileFullPath);
                Assert.True(GetCurrentDocuments().Any(s => s.FilePath == sourceFileFullPath));

                // Remove source file
                await project.RemoveSourceFileAsync(sourceFileFullPath);
                Assert.Empty(GetCurrentDocuments());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;
                VersionStamp GetVersion() => environment.Workspace.CurrentSolution.Projects.Single().Version;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";
                await project.AddSourceFileAsync(sourceFileFullPath1);
                await project.AddSourceFileAsync(sourceFileFullPath2);
                await project.AddSourceFileAsync(sourceFileFullPath3);
                await project.AddSourceFileAsync(sourceFileFullPath4);
                await project.AddSourceFileAsync(sourceFileFullPath5);

                await project.RemoveSourceFileAsync(sourceFileFullPath2);
                await project.AddSourceFileAsync(sourceFileFullPath2);

                await project.RemoveSourceFileAsync(sourceFileFullPath4);
                await project.AddSourceFileAsync(sourceFileFullPath4);

                var oldVersion = GetVersion();

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var newVersion = GetVersion();

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var newSameVersion = GetVersion();

                // Reordering should result in a new version if the order is different. If it's the same, the version should stay the same.
                Assert.NotEqual(oldVersion, newVersion);
                Assert.Equal(newVersion, newSameVersion);

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                var tasks = new List<Task>();

                // Add a file outside the batch.
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));

                project.StartBatch();
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath5));

                // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath2));

                // Removing path4 to test remove of a file when it was also added in a batch.
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath4));

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1 });

                project.EndBatch();

                await Task.WhenAll(tasks);

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchWithReAddingAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                var tasks = new List<Task>();

                // Add a file outside the batch.
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));

                project.StartBatch();
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath5));

                // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath2));

                // Removing path4 to test remove of a file when it was also added in a batch.
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath4));

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1 });

                // Re-adding / re-removing / re-adding again.
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                project.EndBatch();

                await Task.WhenAll(tasks);

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchAddAfterReorderAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                project.StartBatch();

                var tasks = new List<Task>();

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));

                project.ReorderSourceFiles(new[] { sourceFileFullPath2, sourceFileFullPath1 });

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath5));

                project.EndBatch();

                await Task.WhenAll(tasks);

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchRemoveAfterReorderAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                project.StartBatch();
                var tasks = new List<Task>();

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath5));

                project.ReorderSourceFiles(new[] { sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1 });

                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath5));

                project.EndBatch();

                await Task.WhenAll(tasks);

                project.ReorderSourceFiles(new[] { sourceFileFullPath2, sourceFileFullPath1 });

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(documents[1].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchExceptionsAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";
                var sourceFileFullPath3 = @"c:\source3.cs";
                var sourceFileFullPath4 = @"c:\source4.cs";
                var sourceFileFullPath5 = @"c:\source5.cs";

                var tasks = new List<Task>();

                project.StartBatch();

                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file" })); // no files were added, therefore we should get an argument exception
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));

                // Test before we add/remove the rest of source files in the batch.
                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file" }));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath3));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath5));

                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath2));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));

                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath4));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath4));

                Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { @"C:\invalid source file", sourceFileFullPath2, sourceFileFullPath3, sourceFileFullPath4, sourceFileFullPath5 }));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(new List<string>()));
                Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

                project.EndBatch();

                await Task.WhenAll(tasks);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public async Task ReorderSourceFilesBatchExceptionRemoveFileAsync_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                var sourceFileFullPath1 = @"c:\source1.cs";
                var sourceFileFullPath2 = @"c:\source2.cs";

                var tasks = new List<Task>();

                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath1));
                tasks.Add(project.AddSourceFileAsync(sourceFileFullPath2));

                project.StartBatch();

                tasks.Add(project.RemoveSourceFileAsync(sourceFileFullPath2));
                Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles(new[] { sourceFileFullPath2 }));

                project.EndBatch();

                await Task.WhenAll(tasks);

                var documents = GetCurrentDocuments().ToArray();

                Assert.Equal(documents[0].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
