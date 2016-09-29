// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if false
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Services.Editor.Implementation.CodeActions;
using Roslyn.Services.Editor.Shared.Extensions;
using Roslyn.Services.Editor.Shared.TestHooks;
using Roslyn.Services.Editor.UnitTests.Utilities;
using Roslyn.Services.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.Services.Editor.UnitTests.CodeActions
{
    public class CodeActionSmartTagProducerTests
    {
        [WpfFact]
        public void TestWalker()
        {
            var text =
@"class C : System.Exception
{
    //Foo
    void Bar()
    {
        Console.WriteLine(1 + 1);
    }
}";

            using (var workspace = TestWorkspace.CreateWorkspaceFromFile(text))
            {
                var textBuffer = workspace.Documents.First().TextBuffer;
                var issueProducer = new CodeIssueTagProducer(
                    TestWaitIndicator.Default,
                    textBuffer,
                    workspace.ExportProvider.GetExportedValue<CodeActionProviderManager>());

                var snapshot = textBuffer.CurrentSnapshot;
                var tags1 = issueProducer.ProduceTagsAsync(snapshot.GetSpan(0, snapshot.Length), null, CancellationToken.None).PumpingWaitResult().ToList();

                var tagCount1 = tags1.Count;
                Assert.True(tagCount1 > 0, tagCount1.ToString());
            }
        }
    }
}
#endif
