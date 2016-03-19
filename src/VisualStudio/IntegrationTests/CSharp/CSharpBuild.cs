// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild
    {
        private readonly IntegrationHost _host;

        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _host = instanceFactory.GetNewOrUsedInstance();

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

            // TODO: Validate build works as expected
        }
    }
}
