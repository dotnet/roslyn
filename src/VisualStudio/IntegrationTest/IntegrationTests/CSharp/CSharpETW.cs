// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpETW : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpETW(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpETW))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        void ETW()
        {
            var text = @"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace F1TestNamespace
{
    #region TaoRegion
    abstract class ShapesClass { }

    class Program$$
    {
        public static void Main()
        {
        }

        public IEnumerable<int> Linq1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            int i = numbers.First();
            int j = Array.IndexOf(numbers, 1);

            var lowNums1 =
                from n in numbers
                orderby n ascending
                where n < 5
                select n;

            var numberGroups =
              from n in numbers
              let m = 1
              join p in numbers on i equals p
              group n by n % 5 into g
              select new { Remainder = g.Key, Numbers = g };

            foreach (int element in numbers) yield return i;
        }

    }
    #endregion TaoRegion
}";

            var type = Microsoft.Diagnostics.Tracing.Parsers.RoslynEventSource.FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem.ToString();
            var option = FunctionIdOptions.GetOption(FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem);
            VisualStudio.Workspace.SetOption(option.Name, option.Feature, true);
            TraceEventMonitor.StartListener(VisualStudio.HostProcess);
            TraceEventMonitor.StartListening(type);
            SetUpEditor(text);
            var span = TimeSpan.FromMinutes(5);
            TraceEventMonitor.WaitFor(type, span);

            VisualStudio.ExecuteCommand("Tools.ForceGC");
        }

        private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }
    }
}