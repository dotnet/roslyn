// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractive : AbstractInteractiveWindowTest
    {
        public CSharpInteractive(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void BclMathCall()
        {
            this.SubmitText("Math.Sin(1)");
            this.WaitForLastReplOutput("0.8414709848078965");
        }

        [Fact]
        public void BclConsoleCall()
        {
            this.SubmitText(@"Console.WriteLine(""Hello, World!"");");
            this.WaitForLastReplOutput("Hello, World!");
        }

        [Fact]
        public void ForStatement()
        {
            this.SubmitText("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            this.WaitForLastReplOutputContains($"{81}");
        }

        [Fact]
        public void ForEachStatement()
        {
            this.SubmitText(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            this.WaitForLastReplOutputContains(@"c:\windows\win.ini");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17634")]
        public void TopLevelMethod()
        {
            this.SubmitText(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            this.WaitForLastReplOutput($"{24}");
        }

        [Fact]
        public async Task WpfInteractionAsync()
        {
            this.SubmitText(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            this.SubmitText(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            this.SubmitText(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            this.SubmitText($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            await AutomationElementHelper.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            this.WaitForLastReplOutput("Hello, World!");
            this.SubmitText("b = null; w.Close(); w = null;");
        }

        [Fact]
        public void TypingHelpDirectiveWorks()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
            InteractiveWindow.ShowWindow(waitForPrompt: true);

            // Directly type #help, rather than sending it through this.SubmitText. We want to actually test
            // that completion doesn't interfere and there aren't problems with the content-type switching.
            VisualStudio.Instance.SendKeys.Send("#help");

            Assert.EndsWith("#help", InteractiveWindow.GetReplText());

            VisualStudio.Instance.SendKeys.Send("\n");
            this.WaitForLastReplOutputContains("REPL commands");
        }
    }
}