// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public class MvcImportProjectFeatureTest
{
    [Fact]
    public void AddDefaultDirectivesImport_AddsSingleDynamicImport()
    {
        // Arrange
        using var imports = new PooledArrayBuilder<RazorProjectItem>();

        // Act
        MvcImportProjectFeature.AddDefaultDirectivesImport(ref imports.AsRef());

        // Assert
        var import = Assert.Single(imports.ToImmutable());
        Assert.Null(import.FilePath);
    }

    [Fact]
    public void AddHierarchicalImports_AddsViewImportSourceDocumentsOnDisk()
    {
        // Arrange
        using var imports = new PooledArrayBuilder<RazorProjectItem>();
        var projectItem = new TestRazorProjectItem("/Contact/Index.cshtml");
        var testFileSystem = new TestRazorProjectFileSystem(new[]
        {
            new TestRazorProjectItem("/Index.cshtml"),
            new TestRazorProjectItem("/_ViewImports.cshtml"),
            new TestRazorProjectItem("/Contact/_ViewImports.cshtml"),
            projectItem,
        });
        var mvcImportFeature = new MvcImportProjectFeature()
        {
            ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, testFileSystem)
        };

        // Act
        mvcImportFeature.AddHierarchicalImports(projectItem, ref imports.AsRef());

        // Assert
        Assert.Collection(imports.ToImmutable(),
            import => Assert.Equal("/_ViewImports.cshtml", import.FilePath),
            import => Assert.Equal("/Contact/_ViewImports.cshtml", import.FilePath));
    }

    [Fact]
    public void AddHierarchicalImports_AddsViewImportSourceDocumentsNotOnDisk()
    {
        // Arrange
        using var imports = new PooledArrayBuilder<RazorProjectItem>();
        var projectItem = new TestRazorProjectItem("/Pages/Contact/Index.cshtml");
        var testFileSystem = new TestRazorProjectFileSystem(new[] { projectItem });
        var mvcImportFeature = new MvcImportProjectFeature()
        {
            ProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, testFileSystem)
        };

        // Act
        mvcImportFeature.AddHierarchicalImports(projectItem, ref imports.AsRef());

        // Assert
        Assert.Collection(imports.ToImmutable(),
            import => Assert.Equal("/_ViewImports.cshtml", import.FilePath),
            import => Assert.Equal("/Pages/_ViewImports.cshtml", import.FilePath),
            import => Assert.Equal("/Pages/Contact/_ViewImports.cshtml", import.FilePath));
    }
}
