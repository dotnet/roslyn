﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests.InProcess;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractEditorTest : AbstractIntegrationTest
    {
        private readonly string? _solutionName;
        private readonly string? _projectTemplate;

        protected AbstractEditorTest()
        {
        }

        protected AbstractEditorTest(string solutionName)
            : this(solutionName, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        protected AbstractEditorTest(string solutionName, string projectTemplate)
        {
            _solutionName = solutionName;
            _projectTemplate = projectTemplate;
        }

        protected abstract string LanguageName { get; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            if (_solutionName != null)
            {
                RoslynDebug.AssertNotNull(_projectTemplate);

                await TestServices.SolutionExplorer.CreateSolutionAsync(_solutionName, HangMitigatingCancellationToken);
                await TestServices.SolutionExplorer.AddProjectAsync(ProjectName, _projectTemplate, LanguageName, HangMitigatingCancellationToken);
                await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(ProjectName, HangMitigatingCancellationToken);

                // Winforms and XAML do not open text files on creation
                // so these editor tasks will not work if that is the project template being used.
                if (_projectTemplate is not WellKnownProjectTemplates.WinFormsApplication and
                    not WellKnownProjectTemplates.WpfApplication and
                    not WellKnownProjectTemplates.CSharpNetCoreClassLibrary and
                    not WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
                {
                    await TestServices.Editor.SetUseSuggestionModeAsync(false, HangMitigatingCancellationToken);
                    await ClearEditorAsync(HangMitigatingCancellationToken);
                }
            }
        }

        protected async Task ClearEditorAsync(CancellationToken cancellationToken)
            => await SetUpEditorAsync("$$", cancellationToken);

        protected async Task SetUpEditorAsync(string markupCode, CancellationToken cancellationToken)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out int caretPosition);

            await TestServices.Editor.DismissCompletionSessionsAsync(cancellationToken);
            await TestServices.Editor.DismissLightBulbSessionAsync(cancellationToken);

            var originalValue = await TestServices.Workspace.IsPrettyListingOnAsync(LanguageName, cancellationToken);

            await TestServices.Workspace.SetPrettyListingAsync(LanguageName, false, cancellationToken);
            try
            {
                await TestServices.Editor.SetTextAsync(code, cancellationToken);
                await TestServices.Editor.MoveCaretAsync(caretPosition, cancellationToken);
                await TestServices.Editor.ActivateAsync(cancellationToken);
            }
            finally
            {
                await TestServices.Workspace.SetPrettyListingAsync(LanguageName, originalValue, cancellationToken);
            }
        }
    }
}
