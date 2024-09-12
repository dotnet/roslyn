// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInteractiveAsyncOutput : AbstractInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyPreviousAndNextHistory()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"#cls", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Threading;
var t1 = new Thread(() => { for (int i = 0; ; i++) { Console.WriteLine('$'); Thread.Sleep(500); } });
var t2 = new Thread(() => { for (int i = 0; ; i++) { Console.Write('$'); Thread.Sleep(101); } });
var t3 = new Thread(() => { while (true) { Console.Write('\r'); Thread.Sleep(1200); } });
t1.Start();
t2.Start();
t3.Start();", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"#help", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindow.SubmitTextAsync(@"1+1", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindow.SubmitTextAsync(@"1+2", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindowVerifier.ReplPromptConsistencyAsync(prompt: "....", output: "$", HangMitigatingCancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindow.SubmitTextAsync(@"1+4", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindow.SubmitTextAsync(@"1+5", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindowVerifier.ReplPromptConsistencyAsync(prompt: "....", output: "$", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"#cls", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync(@"1+5", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));

            await TestServices.InteractiveWindowVerifier.ReplPromptConsistencyAsync(prompt: "....", output: "$", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"t1.Abort();
t1.Join();
t2.Abort();
t2.Join();
t3.Abort();
t3.Join();", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.ClearReplTextAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.ResetAsync(HangMitigatingCancellationToken);
        }
    }
}
