// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS;

using static CSharpHelpers;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
public sealed class SourceFileHandlingTests
{
    [WpfFact]
    public async Task AddRemoveSourceFile_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
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

    [WpfFact]
    public async Task AddRemoveAdditionalFile_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
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

    [WpfFact]
    public async Task ReorderSourceFiles_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
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

        project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1]);

        var newVersion = GetVersion();

        project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1]);

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

    [WpfFact]
    public async Task ReorderSourceFilesBatch_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());

        var sourceFileFullPath1 = @"c:\source1.cs";
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";

        // Add a file outside the batch.
        project.AddSourceFile(sourceFileFullPath2);

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using (disposableBatchScope.ConfigureAwait(false))
        {
            project.AddSourceFile(sourceFileFullPath1);
            project.AddSourceFile(sourceFileFullPath3);
            project.AddSourceFile(sourceFileFullPath4);
            project.AddSourceFile(sourceFileFullPath5);

            // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
            project.RemoveSourceFile(sourceFileFullPath2);

            // Removing path4 to test remove of a file when it was also added in a batch.
            project.RemoveSourceFile(sourceFileFullPath4);

            project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1]);
        }

        var documents = GetCurrentDocuments().ToArray();

        Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[1].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[2].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
    }

    [WpfFact]
    public async Task ReorderSourceFilesBatchWithReAdding_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());

        var sourceFileFullPath1 = @"c:\source1.cs";
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";

        // Add a file outside the batch.
        project.AddSourceFile(sourceFileFullPath2);

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using (disposableBatchScope.ConfigureAwait(false))
        {
            project.AddSourceFile(sourceFileFullPath1);
            project.AddSourceFile(sourceFileFullPath3);
            project.AddSourceFile(sourceFileFullPath4);
            project.AddSourceFile(sourceFileFullPath5);

            // Removing path2 to test removal of a file the actual internal project state has changed outside of the batch.
            project.RemoveSourceFile(sourceFileFullPath2);

            // Removing path4 to test remove of a file when it was also added in a batch.
            project.RemoveSourceFile(sourceFileFullPath4);

            project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath3, sourceFileFullPath1]);

            // Re-adding / re-removing / re-adding again.
            project.AddSourceFile(sourceFileFullPath2);
            project.AddSourceFile(sourceFileFullPath4);
            project.RemoveSourceFile(sourceFileFullPath2);
            project.RemoveSourceFile(sourceFileFullPath4);
            project.AddSourceFile(sourceFileFullPath2);
            project.AddSourceFile(sourceFileFullPath4);

            project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1]);
        }

        var documents = GetCurrentDocuments().ToArray();

        Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
    }

    [WpfFact]
    public async Task ReorderSourceFilesBatchAddAfterReorder_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());

        var sourceFileFullPath1 = @"c:\source1.cs";
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using (disposableBatchScope.ConfigureAwait(false))
        {
            project.AddSourceFile(sourceFileFullPath1);
            project.AddSourceFile(sourceFileFullPath2);

            project.ReorderSourceFiles([sourceFileFullPath2, sourceFileFullPath1]);

            project.AddSourceFile(sourceFileFullPath3);
            project.AddSourceFile(sourceFileFullPath4);
            project.AddSourceFile(sourceFileFullPath5);
        }

        project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1]);

        var documents = GetCurrentDocuments().ToArray();

        Assert.Equal(documents[0].FilePath, sourceFileFullPath5, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[1].FilePath, sourceFileFullPath4, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[2].FilePath, sourceFileFullPath3, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[3].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[4].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
    }

    [WpfFact]
    public async Task ReorderSourceFilesBatchRemoveAfterReorder_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());

        var sourceFileFullPath1 = @"c:\source1.cs";
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using (disposableBatchScope.ConfigureAwait(false))
        {
            project.AddSourceFile(sourceFileFullPath1);
            project.AddSourceFile(sourceFileFullPath2);
            project.AddSourceFile(sourceFileFullPath3);
            project.AddSourceFile(sourceFileFullPath4);
            project.AddSourceFile(sourceFileFullPath5);

            project.ReorderSourceFiles([sourceFileFullPath5, sourceFileFullPath4, sourceFileFullPath3, sourceFileFullPath2, sourceFileFullPath1]);

            project.RemoveSourceFile(sourceFileFullPath3);
            project.RemoveSourceFile(sourceFileFullPath4);
            project.RemoveSourceFile(sourceFileFullPath5);
        }

        project.ReorderSourceFiles([sourceFileFullPath2, sourceFileFullPath1]);

        var documents = GetCurrentDocuments().ToArray();

        Assert.Equal(documents[0].FilePath, sourceFileFullPath2, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(documents[1].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
    }

    [WpfFact]
    public async Task ReorderSourceFilesExceptions_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";
        project.AddSourceFile(@"c:\source1.cs");
        project.AddSourceFile(sourceFileFullPath2);
        project.AddSourceFile(sourceFileFullPath3);
        project.AddSourceFile(sourceFileFullPath4);
        project.AddSourceFile(sourceFileFullPath5);

        project.RemoveSourceFile(sourceFileFullPath2);
        project.AddSourceFile(sourceFileFullPath2);

        project.RemoveSourceFile(sourceFileFullPath4);
        project.AddSourceFile(sourceFileFullPath4);

        // This should throw due to not passing all of the files.
        Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles([sourceFileFullPath4, sourceFileFullPath5]));

        // This should throw because the path does not exist in the project.
        Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles([@"C:\invalid source file", sourceFileFullPath2, sourceFileFullPath3, sourceFileFullPath4, sourceFileFullPath5]));

        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));
    }

    [WpfFact]
    public async Task ReorderSourceFilesBatchExceptions_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());
        var sourceFileFullPath2 = @"c:\source2.cs";
        var sourceFileFullPath3 = @"c:\source3.cs";
        var sourceFileFullPath4 = @"c:\source4.cs";
        var sourceFileFullPath5 = @"c:\source5.cs";

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using var _ = disposableBatchScope.ConfigureAwait(false);

        Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles([sourceFileFullPath4, sourceFileFullPath5]));
        Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles([@"C:\invalid source file"])); // no files were added, therefore we should get an argument exception
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

        project.AddSourceFile(@"c:\source1.cs");

        // Test before we add/remove the rest of source files in the batch.
        Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles([sourceFileFullPath4, sourceFileFullPath5]));
        Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles([@"C:\invalid source file"]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));

        project.AddSourceFile(sourceFileFullPath2);
        project.AddSourceFile(sourceFileFullPath3);
        project.AddSourceFile(sourceFileFullPath4);
        project.AddSourceFile(sourceFileFullPath5);

        project.RemoveSourceFile(sourceFileFullPath2);
        project.AddSourceFile(sourceFileFullPath2);

        project.RemoveSourceFile(sourceFileFullPath4);
        project.AddSourceFile(sourceFileFullPath4);

        Assert.Throws<ArgumentException>(() => project.ReorderSourceFiles([sourceFileFullPath4, sourceFileFullPath5]));
        Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles([@"C:\invalid source file", sourceFileFullPath2, sourceFileFullPath3, sourceFileFullPath4, sourceFileFullPath5]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => project.ReorderSourceFiles(null));
    }

    [WpfFact]
    public async Task ReorderSourceFilesBatchExceptionRemoveFile_CPS()
    {
        using var environment = new TestEnvironment();
        using var project = await CreateCSharpCPSProjectAsync(environment, "project1");
        IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

        Assert.Empty(GetCurrentDocuments());

        var sourceFileFullPath1 = @"c:\source1.cs";
        var sourceFileFullPath2 = @"c:\source2.cs";

        project.AddSourceFile(sourceFileFullPath1);
        project.AddSourceFile(sourceFileFullPath2);

        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using (disposableBatchScope.ConfigureAwait(false))
        {
            project.RemoveSourceFile(sourceFileFullPath2);
            Assert.Throws<InvalidOperationException>(() => project.ReorderSourceFiles([sourceFileFullPath2]));
        }

        var documents = GetCurrentDocuments().ToArray();

        Assert.Equal(documents[0].FilePath, sourceFileFullPath1, StringComparer.OrdinalIgnoreCase);
    }
}
