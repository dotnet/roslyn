﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.TestSourceGenerator;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSourceGenerators : AbstractEditorTest
    {
        public CSharpSourceGenerators(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpSourceGenerators), WellKnownProjectTemplates.ConsoleApplication)
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            VisualStudio.SolutionExplorer.AddAnalyzerReference(typeof(HelloWorldGenerator).Assembly.Location, new ProjectUtils.Project(ProjectName));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
        public void GoToDefinitionOpensGeneratedFile()
        {
            VisualStudio.Editor.SetText(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + @".GetMessage());
    }
}");

            VisualStudio.Editor.PlaceCaret(HelloWorldGenerator.GeneratedEnglishClassName);
            VisualStudio.Editor.GoToDefinition();
            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", VisualStudio.Shell.GetActiveWindowCaption());
            Assert.Equal(HelloWorldGenerator.GeneratedEnglishClassName, VisualStudio.Editor.GetSelectedText());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
        public void FindReferencesForFileWithDefinitionInSourceGeneratedFile()
        {
            VisualStudio.Editor.SetText(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + @".GetMessage());
    }
}");

            VisualStudio.Editor.PlaceCaret(HelloWorldGenerator.GeneratedEnglishClassName);
            VisualStudio.Editor.SendKeys(Shift(VirtualKey.F12));

            string programReferencesCaption = $"'{HelloWorldGenerator.GeneratedEnglishClassName}' references";
            var results = VisualStudio.FindReferencesWindow.GetContents(programReferencesCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "internal class HelloWorld", actual: reference.Code);
                        Assert.Equal(expected: 1, actual: reference.Line);
                        Assert.Equal(expected: 15, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + ".GetMessage());", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 26, actual: reference.Column);
                    }
                });
        }
    }
}
