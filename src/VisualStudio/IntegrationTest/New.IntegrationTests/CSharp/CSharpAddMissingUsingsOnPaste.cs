// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImportOnPaste;
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
            globalOptions.SetGlobalOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, LanguageNames.CSharp, false);

            await TestServices.Editor.PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

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
            globalOptions.SetGlobalOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, LanguageNames.CSharp, true);

            await TestServices.Editor.PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

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
            await telemetry.VerifyFiredAsync(["vs/ide/vbcs/commandhandler/paste/importsonpaste"], HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyIndentation()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "Example.cs", contents: @"
public class Example
{
}
");
            await SetUpEditorAsync(@"
namespace MyNs
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
        }

        $$
    }
}", HangMitigatingCancellationToken);

            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, LanguageNames.CSharp, true);

            await TestServices.Editor.PasteAsync(@"Task DoThingAsync() => Task.CompletedTask;", HangMitigatingCancellationToken);

            AssertEx.EqualOrDiff(@"
namespace MyNs
{
    using System;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
        }

        Task DoThingAsync() => Task.CompletedTask;
    }
}", await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }
    }
}
