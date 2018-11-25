﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveAsyncOutput : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveAsyncOutput(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [WpfFact]
        public void VerifyPreviousAndNextHistory()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"#cls");

            VisualStudio.InteractiveWindow.SubmitText(@"using System.Threading;
var t1 = new Thread(() => { for (int i = 0; ; i++) { Console.WriteLine('$'); Thread.Sleep(500); } });
var t2 = new Thread(() => { for (int i = 0; ; i++) { Console.Write('$'); Thread.Sleep(101); } });
var t3 = new Thread(() => { while (true) { Console.Write('\r'); Thread.Sleep(1200); } });
t1.Start();
t2.Start();
t3.Start();");

            VisualStudio.InteractiveWindow.SubmitText(@"#help");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.SubmitText(@"1+1");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.SubmitText(@"1+2");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.SubmitText(@"1+4");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.SubmitText(@"1+5");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            VisualStudio.InteractiveWindow.SubmitText(@"#cls");
            VisualStudio.InteractiveWindow.SubmitText(@"1+5");
            Wait(seconds: 1);

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            VisualStudio.InteractiveWindow.SubmitText(@"t1.Abort();
t1.Join();
t2.Abort();
t2.Join();
t3.Abort();
t3.Join();");

            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.Reset();
        }
    }
}
