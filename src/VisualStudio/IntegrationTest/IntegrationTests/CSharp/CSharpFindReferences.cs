// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFindReferences : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFindReferences()
            : base(nameof(CSharpFindReferences))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task FindReferencesToCtorAsync()
        {
            await SetUpEditorAsync(@"
class Program
{
}$$
");
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "File2.cs");
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "File2.cs");

            await SetUpEditorAsync(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Progr$$am();
    }
}
");

            await VisualStudio.Editor.SendKeysAsync(Shift(VirtualKey.F12));

            const string programReferencesCaption = "'Program' references";
            var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(programReferencesCaption);

            var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "class Program", actual: reference.Code);
                        Assert.Equal(expected: 1, actual: reference.Line);
                        Assert.Equal(expected: 6, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Program p = new Program();", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task FindReferencesToLocalsAsync()
        {
            using (var telemetry = await VisualStudio.VisualStudio.EnableTestTelemetryChannelAsync())
            {
                await SetUpEditorAsync(@"
class Program
{
    static void Main()
    {
        int local = 1;
        Console.WriteLine(local$$);
    }
}
");

                await VisualStudio.Editor.SendKeysAsync(Shift(VirtualKey.F12));

                const string localReferencesCaption = "'local' references";
                var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(localReferencesCaption);

                var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
                Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

                Assert.Collection(
                    results,
                    new Action<Reference>[]
                    {
                    reference =>
                    {
                        Assert.Equal(expected: "int local = 1;", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 12, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.Code);
                        Assert.Equal(expected: 6, actual: reference.Line);
                        Assert.Equal(expected: 26, actual: reference.Column);
                    }
                    });

                await telemetry.VerifyFiredAsync("vs/platform/findallreferences/search", "vs/ide/vbcs/commandhandler/findallreference");
            }
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public async Task FindReferencesToStringAsync()
        {
            await SetUpEditorAsync(@"
class Program
{
    static void Main()
    {
         string local = ""1""$$;
    }
}
");

            await VisualStudio.Editor.SendKeysAsync(Shift(VirtualKey.F12));

            const string findReferencesCaption = "'\"1\"' references";
            var results = await VisualStudio.FindReferencesWindow.GetContentsAsync(findReferencesCaption);

            var activeWindowCaption = await VisualStudio.Shell.GetActiveWindowCaptionAsync();
            Assert.Equal(expected: findReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "string local = \"1\";", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }
    }
}
