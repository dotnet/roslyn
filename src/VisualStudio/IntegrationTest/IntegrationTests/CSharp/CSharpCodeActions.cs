// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCodeActions : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeActions(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpIntelliSense))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void GenerateMethodInClosedFile()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Foo.cs", contents: @"
public class Foo
{
}
");

            SetUpEditor(@"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Foo f = new Foo();
        f.Bar()$$
    }
}
");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate method 'Foo.Bar'", applyFix: true);
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "Foo.cs", @"
using System;

public class Foo
{
    internal void Bar()
    {
        throw new NotImplementedException();
    }
}
");
        }
    }
}