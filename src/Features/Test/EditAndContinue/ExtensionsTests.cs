// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

[UseExportProvider]
public sealed class ExtensionsTests
{
    /// <summary>
    /// A project named {positive integer}_{relative URL} with a design-time document whose name ends with
    /// the language projection suffix is a Venus design-time project. The display name is the document name
    /// without that suffix. Suffix matching is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("1_Default.aspx", LanguageNames.CSharp, "Default.aspx.__projection.g.cs", "Default.aspx")]
    [InlineData("42_Folder_My_Page.ascx", LanguageNames.CSharp, "Folder_My_Page.ascx.__PROJECTION.G.CS", "Folder_My_Page.ascx")]
    [InlineData("2147483647_Default.aspx", LanguageNames.VisualBasic, "Default.aspx.__projection.g.vb", "Default.aspx")]
    [InlineData("1_Pages_Index.aspx", LanguageNames.CSharp, "Index.aspx.__projection.g.cs", "Index.aspx")]
    public void IsVenusDesignTimeProject_RecognizesDesignTimeProjection(string projectName, string language, string documentName, string displayName)
    {
        var project = CreateProject(projectName, language, documentName);

        Assert.True(EditAndContinue.Extensions.IsVenusDesignTimeProject(project, out var actualDisplayName));
        Assert.Equal(displayName, actualDisplayName);
    }

    /// <summary>
    /// Project names that are not {positive integer}_{relative URL} are not Venus design-time projects,
    /// even with a design-time projection document.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("Default.aspx")]
    [InlineData("_Default.aspx")]
    [InlineData("1_")]
    [InlineData("a_Default.aspx")]
    [InlineData("1a_Default.aspx")]
    [InlineData("0_Default.aspx")]
    [InlineData("-1_Default.aspx")]
    [InlineData("+1_Default.aspx")]
    [InlineData(" 1_Default.aspx")]
    [InlineData("1 _Default.aspx")]
    [InlineData("2147483648_Default.aspx")]
    public void IsVenusDesignTimeProject_RejectsInvalidProjectName(string projectName)
    {
        var project = CreateProject(projectName, LanguageNames.CSharp, "Default.aspx.__projection.g.cs");

        Assert.False(EditAndContinue.Extensions.IsVenusDesignTimeProject(project, out var webPageName));
        Assert.Null(webPageName);
    }

    /// <summary>
    /// A Venus-named project is not recognized unless it has a design-time document ending with the language-specific projection suffix.
    /// </summary>
    [Theory]
    [InlineData(LanguageNames.CSharp, null)]
    [InlineData(LanguageNames.CSharp, "Default.aspx.cs")]
    [InlineData(LanguageNames.CSharp, "Default.aspx.__projection.g.vb")]
    [InlineData(LanguageNames.VisualBasic, "Default.aspx.__projection.g.cs")]
    [InlineData("F#", "Default.aspx.__projection.g.cs")]
    public void IsVenusDesignTimeProject_RejectsMissingOrMismatchedProjection(string language, string? documentName)
    {
        var project = CreateProject("1_Default.aspx", language, documentName);

        Assert.False(EditAndContinue.Extensions.IsVenusDesignTimeProject(project, out var webPageName));
        Assert.Null(webPageName);
    }

    /// <summary>
    /// A projection document that is not design-time-only does not make the project a Venus design-time project.
    /// </summary>
    [Fact]
    public void IsVenusDesignTimeProject_RejectsNonDesignTimeDocument()
    {
        var project = CreateProject("1_Default.aspx", LanguageNames.CSharp, "Default.aspx.__projection.g.cs", designTimeOnly: false);

        Assert.False(EditAndContinue.Extensions.IsVenusDesignTimeProject(project, out var webPageName));
        Assert.Null(webPageName);
    }

    /// <summary>
    /// A Venus design-time project with an absolute path and no effective generated output directory is ignored
    /// and logs, under the page display name, that changes are applied by the ASP.NET runtime. An ordinary project
    /// in the same state is ignored and logs the missing directory.
    /// </summary>
    [Theory]
    [InlineData(LanguageNames.CSharp, ".__projection.g.cs", ".cs")]
    [InlineData(LanguageNames.VisualBasic, ".__projection.g.vb", ".vb")]
    public void IgnoreForEditAndContinue_LogsRuntimeSaveMessageForVenusProject(
        string language,
        string projectionSuffix,
        string sourceExtension)
    {
        using var workspace = new AdhocWorkspace();
        var venusPath = Path.Combine(Path.GetTempPath(), "1_Default.aspx" + sourceExtension + "proj");
        var ordinaryPath = Path.Combine(Path.GetTempPath(), "ClassLibrary" + sourceExtension + "proj");

        var venus = CreateProject(
            "1_Default.aspx",
            language,
            "Default.aspx" + projectionSuffix,
            filePath: venusPath,
            workspace: workspace);
        var ordinary = CreateProject(
            "ClassLibrary",
            language,
            "Class1" + sourceExtension,
            designTimeOnly: false,
            filePath: ordinaryPath,
            workspace: workspace);

        var venusLog = new TraceLog("Venus");
        Assert.True(venus.IgnoreForEditAndContinue(venusLog));
        Assert.Equal(
            "'Default.aspx' ignored for EnC: changes applied by ASP.NET runtime on save",
            Assert.Single(venusLog.GetTestAccessor().Entries, static entry => entry is not null));

        var ordinaryLog = new TraceLog("Ordinary");
        Assert.True(ordinary.IgnoreForEditAndContinue(ordinaryLog));
        Assert.Equal(
            $"Project '{ordinaryPath}' ignored for EnC: no generated files output directory",
            Assert.Single(ordinaryLog.GetTestAccessor().Entries, static entry => entry is not null));
    }

    private static Project CreateProject(
        string name,
        string language,
        string? documentName,
        bool designTimeOnly = true,
        string? filePath = null,
        AdhocWorkspace? workspace = null)
    {
        var ownsWorkspace = workspace is null;
        workspace ??= new AdhocWorkspace();

        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution.AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, name, name, language, filePath: filePath));

        if (documentName != null)
        {
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), documentName).WithDesignTimeOnly(designTimeOnly);
            solution = solution.AddDocument(documentInfo);
        }

        var project = solution.GetProject(projectId)!;
        if (ownsWorkspace)
        {
            workspace.Dispose();
        }

        return project;
    }
}
