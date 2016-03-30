// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicBuild
    {
        private readonly VisualStudioInstanceContext _visualStudio;

        public BasicBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            var solution = _visualStudio.Instance.SolutionExplorer.CreateSolution(nameof(BasicBuild));
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

            _visualStudio.Instance.EditorWindow.Text = editorText;

            // TODO: Validate build works as expected
        }
    }
}
