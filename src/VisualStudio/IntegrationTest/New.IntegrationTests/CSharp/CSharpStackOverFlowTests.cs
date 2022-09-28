// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpStackOverFlowTests : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpStackOverFlowTests()
            : base(nameof(CSharpStackOverFlowTests))
        {
        }

        [IdeFact]
        public async Task TestDevenvDoNotCrash()
        {
            var sampleCode = await GetSampleCodeAsync();
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);
            await SetUpEditorAsync(sampleCode, HangMitigatingCancellationToken);

            // Try to compute the light bulb. The content of the light bulb is not important because here we want to make sure
            // the speical crafted code don't crash VS.
            await TestServices.Editor.ShowLightBulbAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TestSyntaxIndex()
        {
            var sampleCode = await GetSampleCodeAsync();
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "Test.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "Test.cs", HangMitigatingCancellationToken);
            await SetUpEditorAsync(sampleCode, HangMitigatingCancellationToken);

            // Call FAR to create syntax index. The goal is to verify we don't hit StackOverFlow during the creation.
            await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
            var contents = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            Assert.Single(contents);
            var content = contents.Single();
            content.TryGetValue(StandardTableKeyNames.Text, out string code);
            Assert.Equal("int Tree82(int i)", code);
        }

        private static async Task<string> GetSampleCodeAsync()
        {
            var resourceStream = typeof(CSharpStackOverFlowTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.NewIntegrationTests.Resources.LongClass.txt");
            using var reader = new StreamReader(resourceStream);
            // This is a special crafted code which many Roslyn functions won't work.
            var sampleCode = await reader.ReadToEndAsync();
            return sampleCode;
        }
    }
}
