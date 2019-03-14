// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractive : AbstractInteractiveWindowTest
    {
        public CSharpInteractive(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        [WpfFact]
        public void BclMathCall()
        {
            VisualStudio.InteractiveWindow.SubmitText("Math.Sin(1)");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("0.8414709848078965");
        }

        [WpfFact]
        public void BclConsoleCall()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"Console.WriteLine(""Hello, World!"");");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("Hello, World!");
        }

        [WpfFact]
        public void ForStatement()
        {
            VisualStudio.InteractiveWindow.SubmitText("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains($"{81}");
        }

        [WpfFact]
        public void ForEachStatement()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains(@"c:\windows\win.ini");
        }

        [WpfFact]
        public void TopLevelMethod()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput($"{24}");
        }

        [WpfFact]
        public async Task WpfInteractionAsync()
        {
            VisualStudio.InteractiveWindow.SubmitText(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            VisualStudio.InteractiveWindow.SubmitText(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            VisualStudio.InteractiveWindow.SubmitText(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            VisualStudio.InteractiveWindow.SubmitText($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            await AutomationElementHelper.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            VisualStudio.InteractiveWindow.WaitForLastReplOutput("Hello, World!");
            VisualStudio.InteractiveWindow.SubmitText("b = null; w.Close(); w = null;");
        }

        [WpfFact]
        public void TypingHelpDirectiveWorks()
        {
            VisualStudio.InteractiveWindow.ShowWindow(waitForPrompt: true);

            // Directly type #help, rather than sending it through VisualStudio.InteractiveWindow.SubmitText. We want to actually test
            // that completion doesn't interfere and there aren't problems with the content-type switching.
            VisualStudio.SendKeys.Send("#help");

            Assert.EndsWith("#help", VisualStudio.InteractiveWindow.GetReplText());

            VisualStudio.SendKeys.Send("\n");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("REPL commands");
        }
    }
}
