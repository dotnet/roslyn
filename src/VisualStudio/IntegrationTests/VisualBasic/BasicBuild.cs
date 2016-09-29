// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicBuild
    {
        private readonly VisualStudioInstanceContext _visualStudio;

        public BasicBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _visualStudio.Instance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            _visualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [Fact]
        public void BuildProject()
        {
            var editorText = @"Module Program

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            _visualStudio.Instance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }
    }
}
