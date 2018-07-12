// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicBuild : AbstractIdeIntegrationTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(BasicBuild));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Build)]
        public async Task BuildProjectAsync()
        {
            var editorText = @"Module Program

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            await VisualStudio.Editor.SetTextAsync(editorText);

            // TODO: Validate build works as expected
        }
    }
}
