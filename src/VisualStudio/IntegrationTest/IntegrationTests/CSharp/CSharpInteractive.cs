// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpInteractive : AbstractInteractiveWindowTest
    {
        public CSharpInteractive( )
            : base()
        {
        }

        [TestMethod]
        public void BclMathCall()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("Math.Sin(1)");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("0.8414709848078965");
        }

        [TestMethod]
        public void BclConsoleCall()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"Console.WriteLine(""Hello, World!"");");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("Hello, World!");
        }

        [TestMethod]
        public void ForStatement()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains($"{81}");
        }

        [TestMethod]
        public void ForEachStatement()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains(@"c:\windows\win.ini");
        }

        [TestMethod]
        public void TopLevelMethod()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput($"{24}");
        }

        [TestMethod]
        public async Task WpfInteractionAsync()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            VisualStudioInstance.InteractiveWindow.SubmitText(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            VisualStudioInstance.InteractiveWindow.SubmitText(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            VisualStudioInstance.InteractiveWindow.SubmitText($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            await AutomationElementHelper.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("Hello, World!");
            VisualStudioInstance.InteractiveWindow.SubmitText("b = null; w.Close(); w = null;");
        }

        [TestMethod]
        public void TypingHelpDirectiveWorks()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(true);
            VisualStudioInstance.InteractiveWindow.ShowWindow(waitForPrompt: true);

            // Directly type #help, rather than sending it through VisualStudio.InteractiveWindow.SubmitText. We want to actually test
            // that completion doesn't interfere and there aren't problems with the content-type switching.
            VisualStudioInstance.SendKeys.Send("#help");

            ExtendedAssert.EndsWith("#help", VisualStudioInstance.InteractiveWindow.GetReplText());

            VisualStudioInstance.SendKeys.Send("\n");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("REPL commands");
        }
    }
}
