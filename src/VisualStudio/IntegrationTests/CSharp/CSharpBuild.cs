// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild
    {
        private readonly VisualStudioInstance _visualStudio;

        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            var solution = _visualStudio.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
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

            _visualStudio.EditorWindow.Text = editorText;

            // TODO: Validate build works as expected
        }
    }
}
