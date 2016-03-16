// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHost))]
    public class CSharpBuild
    {
        private IntegrationHost _host;

        public CSharpBuild(IntegrationHost host)
        {
            _host = host;
            _host.Initialize();

            var solution = _host.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            var project = solution.AddProject("TestProj", ProjectTemplate.ConsoleApplication, ProjectLanguage.CSharp);
        }

        [Fact]
        public void BuildProject()
        {
            var editorText = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

            _host.EditorWindow.Text = editorText;
            Assert.Equal(editorText, _host.EditorWindow.Text);

            // TODO: Validate build works as expected
        }
    }
}
