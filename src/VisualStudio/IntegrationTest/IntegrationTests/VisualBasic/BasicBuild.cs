// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicBuild : AbstractIntegrationTest
    {
        public BasicBuild(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, _=> null)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            VisualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Build)]
        public void BuildProject()
        {
            var editorText = @"Module Program

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            VisualStudio.Instance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }
    }
}
