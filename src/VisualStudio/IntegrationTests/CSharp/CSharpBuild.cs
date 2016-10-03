// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;

        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);

            _visualStudio.Instance.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            _visualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
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

            _visualStudio.Instance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }

    }
}
