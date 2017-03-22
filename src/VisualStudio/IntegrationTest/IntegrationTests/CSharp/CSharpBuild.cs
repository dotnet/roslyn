// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild : AbstractIntegrationTest
    {
        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            VisualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
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

            VisualStudio.Instance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }

    }
}
