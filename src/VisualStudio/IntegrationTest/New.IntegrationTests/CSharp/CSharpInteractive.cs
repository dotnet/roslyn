// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInteractive : AbstractInteractiveWindowTest
    {
        [IdeFact]
        public async Task BclMathCall()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("Math.Sin(1)", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("0.8414709848078965", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task BclConsoleCall()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"Console.WriteLine(""Hello, World!"");", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("Hello, World!", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ForStatement()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync($"{81}", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ForEachStatement()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync(@"c:\windows\win.ini", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TopLevelMethod()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync($"{24}", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task WpfInteractionAsync()
        {
            await TestServices.InteractiveWindow.SubmitTextAsync(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;", HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.SubmitTextAsync(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;", HangMitigatingCancellationToken);

            var testValue = Guid.NewGuid();

            await TestServices.InteractiveWindow.SubmitTextAsync($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;", HangMitigatingCancellationToken);

            await AutomationElementHelper.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            await TestServices.InteractiveWindow.WaitForLastReplOutputAsync("Hello, World!", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.SubmitTextAsync("b = null; w.Close(); w = null;", HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TypingHelpDirectiveWorks()
        {
            await TestServices.InteractiveWindow.ShowWindowAsync(waitForPrompt: true, HangMitigatingCancellationToken);

            // Directly type #help, rather than sending it through VisualStudio.InteractiveWindow.SubmitText. We want to actually test
            // that completion doesn't interfere and there aren't problems with the content-type switching.
            await TestServices.Input.SendWithoutActivateAsync("#help", HangMitigatingCancellationToken);

            Assert.EndsWith("#help", await TestServices.InteractiveWindow.GetReplTextAsync(HangMitigatingCancellationToken));

            await TestServices.Input.SendWithoutActivateAsync("\n", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.WaitForLastReplOutputContainsAsync("REPL commands", HangMitigatingCancellationToken);
        }
    }
}
