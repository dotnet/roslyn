// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAsyncOutput : AbstractInteractiveWindowTest
    {
        public CSharpAsyncOutput(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void VerifyPreviousAndNextHistory()
        {
            SubmitText(@"#prompt inline ""@@@"" ""   """, waitForPrompt: false);
            SubmitText(@"#cls", waitForPrompt: false);

            SubmitText(@"using System.Threading;
var t1 = new Thread(() => { for (int i = 0; ; i++) { Console.WriteLine('$'); Thread.Sleep(500); } });
var t2 = new Thread(() => { for (int i = 0; ; i++) { Console.Write('$'); Thread.Sleep(101); } });
var t3 = new Thread(() => { while (true) { Console.Write('\r'); Thread.Sleep(1200); } });
t1.Start();
t2.Start();
t3.Start();", waitForPrompt: false);

            SubmitText(@"#help", waitForPrompt: false);
            Wait(seconds: 1);

            SubmitText(@"1+1", waitForPrompt: false);
            Wait(seconds: 1);

            SubmitText(@"1+2", waitForPrompt: false);
            Wait(seconds: 1);

            VerifyReplPromptConsistency(prompt: "@@@", output: "$");

            SubmitText(@"#prompt margin", waitForPrompt: false);
            Wait(seconds: 1);

            SubmitText(@"1+4", waitForPrompt: false);
            SubmitText(@"#prompt inline", waitForPrompt: false);
            Wait(seconds: 1);

            SubmitText(@"1+5");
            Wait(seconds: 1);

            VerifyReplPromptConsistency(prompt: "@@@", output: "$");

            SubmitText(@"#cls", waitForPrompt: false);
            SubmitText(@"1+5", waitForPrompt: false);
            Wait(seconds: 1);

            VerifyReplPromptConsistency(prompt: "@@@", output: "$");

            SubmitText(@"#prompt inline "" > "" "". """, waitForPrompt: false);

            SubmitText(@"t1.Abort();
t1.Join();
t2.Abort();
t2.Join();
t3.Abort();
t3.Join();");

            ClearReplText();

            SubmitText(@"#prompt inline "" > "" "". """);
            Reset(waitForPrompt: true);
        }
    }
}
