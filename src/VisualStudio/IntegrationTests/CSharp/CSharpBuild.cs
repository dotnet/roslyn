// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;

        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            var solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            var project = solution.AddProject("TestProj", ProjectTemplate.ConsoleApplication, ProjectLanguage.CSharp);
        }

        public void Dispose()
        {
            _visualStudio.Dispose();
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

            _visualStudio.Instance.EditorWindow.Text = editorText;

            // TODO: Validate build works as expected
        }

    }
}
