// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHost))]
    public class BasicBuild
    {
        private IntegrationHost _host;

        public BasicBuild(IntegrationHost host)
        {
            _host = host;
            _host.Initialize();

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
            Assert.Equal(editorText, _host.EditorWindow.Text);

            // TODO: Validate build works as expected
        }
    }
}
