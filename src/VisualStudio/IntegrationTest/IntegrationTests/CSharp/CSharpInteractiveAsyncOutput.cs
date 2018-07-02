// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveAsyncOutput : AbstractIdeInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyPreviousAndNextHistoryAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"#cls");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Threading;
var t1 = new Thread(() => { for (int i = 0; ; i++) { Console.WriteLine('$'); Thread.Sleep(500); } });
var t2 = new Thread(() => { for (int i = 0; ; i++) { Console.Write('$'); Thread.Sleep(101); } });
var t3 = new Thread(() => { while (true) { Console.Write('\r'); Thread.Sleep(1200); } });
t1.Start();
t2.Start();
t3.Start();");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"#help");
            await Task.Delay(TimeSpan.FromSeconds(1));

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"1+1");
            await Task.Delay(TimeSpan.FromSeconds(1));

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"1+2");
            await Task.Delay(TimeSpan.FromSeconds(1));

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            await Task.Delay(TimeSpan.FromSeconds(1));

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"1+4");
            await Task.Delay(TimeSpan.FromSeconds(1));

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"1+5");
            await Task.Delay(TimeSpan.FromSeconds(1));

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"#cls");
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"1+5");
            await Task.Delay(TimeSpan.FromSeconds(1));

            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency(prompt: "....", output: "$");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"t1.Abort();
t1.Join();
t2.Abort();
t2.Join();
t3.Abort();
t3.Join();");

            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.ResetAsync();
        }
    }
}
