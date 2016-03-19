// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicBuild
    {
        private readonly IntegrationHost _host;

        public BasicBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _host = instanceFactory.GetNewOrUsedInstance();

            var solution = _host.SolutionExplorer.CreateSolution(nameof(BasicBuild));
            var project = solution.AddProject("TestProj", ProjectTemplate.ConsoleApplication, ProjectLanguage.VisualBasic);
        }

        [Fact]
        public void BuildProject()
        {
            var editorText = @"Module Program

    Sub Main()
        Console.WriteLine(""Hello, World!"")
    End Sub

End Module";

            _host.EditorWindow.Text = editorText;

            // TODO: Validate build works as expected
        }
    }
}
