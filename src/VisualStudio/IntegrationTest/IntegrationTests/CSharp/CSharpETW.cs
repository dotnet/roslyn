// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpETW : AbstractIntegrationTest
    {

        public CSharpETW(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        void ETWTyping()
        {
            VisualStudio.SolutionExplorer.OpenSolution(@"C:\rs\RoslynSolutions\Roslyn-CSharp.sln");
            ETWActions.ForceGC(VisualStudio);
            //VisualStudio.SolutionExplorer.OpenFile(new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(@"CSharpCompiler.mod"), "LanguageParser.cs");
            //VisualStudio.Editor.NavigateToSendKeys("LanguageParser.cs");
            VisualStudio.ExecuteCommand("File.OpenFile", @"C:\rs\RoslynSolutions\Source\Compilers\CSharp\Source\Parser\LanguageParser.cs");
            ETWActions.StartETWListener(VisualStudio);
            ETWActions.WaitForSolutionCrawler(VisualStudio);
            ETWActions.ForceGC(VisualStudio);
            VisualStudio.ExecuteCommand("Edit.GoTo", "9524");
            ETWActions.WaitForIdleCPU();
            //ETWActions.WaitForSolutionCrawler(VisualStudio);
            //System.Windows.Forms.MessageBox.Show("Attach debugger!");
            using (DelayTracker.Start(@"C:\typingResults\", @"C:\roslyn-internal\open\binaries\release\dlls\PerformanceTestUtilities\TypingDelayAnalyzer.exe", "typing"))
            {
            VisualStudio.Editor.PlayBackTyping(@"C:\roslyn-internal\Closed\Test\PerformanceTests\Perf\tests\CSharp\TypingInputs\CSharpGoldilocksInput-MultipliedDelay.txt");
            }
            //System.Windows.Forms.MessageBox.Show("Done waiting!");
            ETWActions.StopETWListener(VisualStudio);
            //System.Threading.Thread.Sleep(10000);
        }


            private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }
    }
}
