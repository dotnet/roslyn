// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHost))]
    public class CSharpInteractiveDemo
    {
        private IntegrationHost _host;
        private InteractiveWindow _interactiveWindow;

        public CSharpInteractiveDemo(IntegrationHost host)
        {
            _host = host;
            _host.Initialize();

            _interactiveWindow = _host.CSharpInteractiveWindow;

            _interactiveWindow.Show();
            _interactiveWindow.Reset();
        }

        [Fact]
        public void BclMathCall()
        {
            _interactiveWindow.SubmitTextToRepl("Math.Sin(1)");
            Assert.True(_interactiveWindow.CheckLastReplOutputEquals("0.8414709848078965"));
        }

        [Fact]
        public void BclConsoleCall()
        {
            _interactiveWindow.SubmitTextToRepl(@"Console.WriteLine(""Hello World"");");
            Assert.True(_interactiveWindow.CheckLastReplOutputEquals("Hello World"));
        }

        [Fact]
        public void ForStatement()
        {
            _interactiveWindow.SubmitTextToRepl("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            Assert.True(_interactiveWindow.CheckLastReplOutputEndsWith("81"));
        }

        [Fact]
        public void ForEachStatement()
        {
            _interactiveWindow.SubmitTextToRepl(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            Assert.True(_interactiveWindow.CheckLastReplOutputContains(@"c:\windows\win.ini"));
        }

        [Fact]
        public void TopLevelMethod()
        {
            _interactiveWindow.SubmitTextToRepl(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            Assert.True(_interactiveWindow.CheckLastReplOutputEquals("24"));
        }

        [Fact]
        public void WpfInteraction()
        {
            _interactiveWindow.SubmitTextToRepl(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            _interactiveWindow.SubmitTextToRepl(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            _interactiveWindow.SubmitTextToRepl(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            _interactiveWindow.SubmitTextToRepl($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            _host.ClickAutomationElement(testValue.ToString(), recursive: true);
            _interactiveWindow.WaitForReplPrompt();

            Assert.True(_interactiveWindow.CheckLastReplOutputEquals("Hello World!"));

            _interactiveWindow.SubmitTextToRepl("b = null; w.Close(); w = null;");
        }
    }
}
