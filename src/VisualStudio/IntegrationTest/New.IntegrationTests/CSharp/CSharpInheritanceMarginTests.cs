// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInheritanceMarginTests : AbstractEditorTest
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
@"
interface IBar
{
}

class Implementation : IBar
{
}", expectedGlyphsNumberInMargin: 2, HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.ClickTheGlyphOnLine(2, HangMitigatingCancellationToken);

            // Move focus to menu item of 'IBar', the destination is targeting 'class Implementation'
            await TestServices.Input.SendAsync(VirtualKey.Tab);
            // Navigate to the destination
            await TestServices.Input.SendAsync(VirtualKey.Enter);
            await TestServices.EditorVerifier.TextContainsAsync(@"class Implementation$$", assertCaretPosition: true);
        }

        [IdeFact]
        public async Task TestMultipleItemsOnSameLine()
        {
            var project = ProjectName;
            await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
@"
interface IBar
{
    event EventHandler e1, e2;
}

class Implementation : IBar
{
    public event EventHandler e1, e2;
}", expectedGlyphsNumberInMargin: 4, HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.ClickTheGlyphOnLine(4, HangMitigatingCancellationToken);

            // The context menu contains two members, e1 and e2.
            // Move focus to menu item of 'event e1'
            await TestServices.Input.SendAsync(VirtualKey.Tab);
            // Expand the submenu
            await TestServices.Input.SendAsync(VirtualKey.Enter);
            // Navigate to the implemention
            await TestServices.Input.SendAsync(VirtualKey.Enter);
            await TestServices.EditorVerifier.TextContainsAsync(@"public event EventHandler e1$$, e2;", assertCaretPosition: true);
        }

        [IdeFact]
        public async Task TestNavigateToMetadata()
        {
            var project = ProjectName;
            await TestServices.InheritanceMargin.EnableOptionsAsync(LanguageName, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.SetTextAndEnsureGlyphsAppearAsync(
@"
using System.Collections

class Implementation : IEnumerable
{
}", expectedGlyphsNumberInMargin: 1, HangMitigatingCancellationToken);

            await TestServices.InheritanceMargin.ClickTheGlyphOnLine(4, HangMitigatingCancellationToken);

            // Move focus to menu item of 'class Implementation'
            await TestServices.Input.SendAsync(VirtualKey.Tab);
            // Navigate to 'IEnumerable'
            await TestServices.Input.SendAsync(VirtualKey.Enter);
            await TestServices.EditorVerifier.TextContainsAsync(@"public interface IEnumerable$$", assertCaretPosition: true);
        }
    }
}
