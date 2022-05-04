// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.AddMissingImports)]
    public class CSharpAddMissingUsingsOnPaste : AbstractEditorTest
    {
        public CSharpAddMissingUsingsOnPaste()
            : base(nameof(CSharpAddMissingUsingsOnPaste))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact]
        public async Task VerifyDisabled()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "Example.cs", contents: @"
public class Example
{
}
");
            await SetUpEditorAsync(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    $$
}", HangMitigatingCancellationToken);

            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp), false);

            await PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    Task DoThingAsync() => Task.CompletedTask;
}", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task VerifyDisabledWithNull()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "Example.cs", contents: @"
public class Example
{
}
", cancellationToken: HangMitigatingCancellationToken);
            await SetUpEditorAsync(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    $$
}", HangMitigatingCancellationToken);

            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp), null);

            await PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    Task DoThingAsync() => Task.CompletedTask;
}", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task VerifyAddImportsOnPaste()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(
                project,
                "Example.cs",
                contents: @"
public class Example
{
}
",
                cancellationToken: HangMitigatingCancellationToken);
            await SetUpEditorAsync(
@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    $$
}
",
                HangMitigatingCancellationToken);

            await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);

            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.AddImportsOnPaste, LanguageNames.CSharp), true);

            await PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(
@"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    Task DoThingAsync() => Task.CompletedTask;
}
",
                await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
            await telemetry.VerifyFiredAsync(new[] { "vs/ide/vbcs/commandhandler/paste/importsonpaste" }, HangMitigatingCancellationToken);
        }

        private async Task PasteAsync(string text, CancellationToken cancellationToken)
        {
            var provider = await TestServices.Shell.GetComponentModelServiceAsync<IAsynchronousOperationListenerProvider>(HangMitigatingCancellationToken);
            var waiter = (IAsynchronousOperationWaiter)provider.GetListener(FeatureAttribute.AddImportsOnPaste);

            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawler }, cancellationToken);
            Clipboard.SetText(text);
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Paste, cancellationToken);

            await waiter.ExpeditedWaitAsync();
        }
    }
}
