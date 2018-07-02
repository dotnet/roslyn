// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractive : AbstractIdeInteractiveWindowTest
    {
        [IdeFact]
        public async Task BclMathCallAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Math.Sin(1)");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("0.8414709848078965");
        }

        [IdeFact]
        public async Task BclConsoleCallAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"Console.WriteLine(""Hello, World!"");");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("Hello, World!");
        }

        [IdeFact]
        public async Task ForStatementAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync($"{81}");
        }

        [IdeFact]
        public async Task ForEachStatementAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync(@"c:\windows\win.ini");
        }

        [IdeFact]
        public async Task TopLevelMethodAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync($"{24}");
        }

        [IdeFact]
        public async Task WpfInteractionAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            await VisualStudio.InteractiveWindow.SubmitTextAsync($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            await AutomationElementHelper.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("Hello, World!");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("b = null; w.Close(); w = null;");
        }

        [IdeFact]
        public async Task TypingHelpDirectiveWorksAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(true);
            await VisualStudio.InteractiveWindow.ShowWindowAsync(waitForPrompt: true);

            // Directly type #help, rather than sending it through VisualStudio.InteractiveWindow.SubmitText. We want to actually test
            // that completion doesn't interfere and there aren't problems with the content-type switching.
            await VisualStudio.SendKeys.SendAsync("#help");

            Assert.EndsWith("#help", VisualStudio.InteractiveWindow.GetReplText());

            await VisualStudio.SendKeys.SendAsync("\n");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("REPL commands");
        }
    }
}
