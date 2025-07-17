// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public sealed class CSharpInheritanceMarginTests : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpInheritanceMarginTests()
        : base(nameof(CSharpInheritanceMarginTests))
    {
    }

    [IdeFact]
    public async Task TestNavigateInSource()
    {
        var project = ProjectName;
        await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
            """

            interface IBar
            {
            }

            class Implementation : IBar
            {
            }
            """, expectedGlyphsNumberInMargin: 2, HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.ClickTheGlyphOnLine(2, HangMitigatingCancellationToken);

        // Move focus to menu item of 'IBar', the destination is targeting 'class Implementation'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        // Navigate to the destination
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.InheritanceMargin], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"class $$Implementation", assertCaretPosition: true);
    }

    [IdeFact]
    public async Task TestMultipleItemsOnSameLine()
    {
        var project = ProjectName;
        await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
            """

            using System;
            interface IBar
            {
                event EventHandler e1, e2;
            }

            class Implementation : IBar
            {
                public event EventHandler e1, e2;
            }
            """, expectedGlyphsNumberInMargin: 4, HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.ClickTheGlyphOnLine(5, HangMitigatingCancellationToken);

        // The context menu contains two members, e1 and e2.
        // Move focus to menu item of 'event e1'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        // Expand the submenu
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        // Navigate to the implemention
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.InheritanceMargin], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"public event EventHandler $$e1, e2;", assertCaretPosition: true);
    }

    [IdeFact]
    public async Task TestNavigateToMetadata()
    {
        var project = ProjectName;
        await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
            """

            using System.Collections;

            class Implementation : IEnumerable
            {
                public IEnumerator GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            }
            """, expectedGlyphsNumberInMargin: 2, HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.ClickTheGlyphOnLine(4, HangMitigatingCancellationToken);

        // Move focus to menu item of 'class Implementation'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        // Navigate to 'IEnumerable'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.InheritanceMargin], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"public interface $$IEnumerable", assertCaretPosition: true);

        var document = await TestServices.Editor.GetActiveDocumentAsync(HangMitigatingCancellationToken);
        RoslynDebug.AssertNotNull(document);
        Assert.Equal(WorkspaceKind.MetadataAsSource, document.Project.Solution.WorkspaceKind);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/62286")]
    public async Task TestNavigateToDifferentProjects()
    {
        await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageNames.CSharp, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageNames.VisualBasic, cancellationToken: HangMitigatingCancellationToken);

        var csharpProjectName = ProjectName;
        var vbProjectName = "TestVBProject";
        await TestServices.SolutionExplorer.AddProjectAsync(
            vbProjectName, WellKnownProjectTemplates.VisualBasicNetStandardClassLibrary, LanguageNames.VisualBasic, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(vbProjectName, "Test.vb", """

            Namespace MyNs
                Public Interface IBar
                End Interface
            End Namespace
            """);

        await TestServices.SolutionExplorer.AddFileAsync(csharpProjectName, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectReferenceAsync(csharpProjectName, vbProjectName, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(csharpProjectName, "Test.cs", HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
            """

            using TestVBProject.MyNs;

            class Implementation : IBar
            {
            }
            """, expectedGlyphsNumberInMargin: 1, HangMitigatingCancellationToken);

        await TestServices.InheritanceMargin.ClickTheGlyphOnLine(4, HangMitigatingCancellationToken);

        // Move focus to menu item of 'class Implementation'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        // Navigate to 'IBar'
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.InheritanceMargin], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"Public Interface $$IBar", assertCaretPosition: true);

        var document = await TestServices.Editor.GetActiveDocumentAsync(HangMitigatingCancellationToken);
        RoslynDebug.AssertNotNull(document);
        Assert.NotEqual(WorkspaceKind.MetadataAsSource, document.Project.Solution.WorkspaceKind);
    }
}
