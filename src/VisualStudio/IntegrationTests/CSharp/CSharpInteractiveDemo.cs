// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveDemo : IDisposable
    {
        private readonly VisualStudioInstanceContext _visualStudio;
        private readonly CSharpInteractiveWindow_OutOfProc _interactiveWindow;

        public CSharpInteractiveDemo(VisualStudioInstanceFactory instanceFactory)
        {
            _visualStudio = instanceFactory.GetNewOrUsedInstance();

            _interactiveWindow = _visualStudio.Instance.CSharpInteractiveWindow;
            _interactiveWindow.Initialize();

            _interactiveWindow.ShowWindow();
            _interactiveWindow.Reset();
        }

        public void Dispose()
        {
            _visualStudio.Dispose();
        }

        [Fact]
        public void BclMathCall()
        {
            _interactiveWindow.SubmitText("Math.Sin(1)");
            Assert.Equal("0.8414709848078965", _interactiveWindow.GetLastReplOutput());
        }

        [Fact]
        public void BclConsoleCall()
        {
            _interactiveWindow.SubmitText(@"Console.WriteLine(""Hello, World!"");");
            Assert.Equal("Hello, World!", _interactiveWindow.GetLastReplOutput());
        }

        [Fact]
        public void ForStatement()
        {
            _interactiveWindow.SubmitText("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);");
            Assert.EndsWith($"{81}", _interactiveWindow.GetLastReplOutput());
        }

        [Fact]
        public void ForEachStatement()
        {
            _interactiveWindow.SubmitText(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());");
            Assert.Contains(@"c:\windows\win.ini", _interactiveWindow.GetLastReplOutput());
        }

        [Fact]
        public void TopLevelMethod()
        {
            _interactiveWindow.SubmitText(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)");
            Assert.Equal($"{24}", _interactiveWindow.GetLastReplOutput());
        }

        [Fact]
        public async Task WpfInteraction()
        {
            _interactiveWindow.SubmitText(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            _interactiveWindow.SubmitText(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;");

            _interactiveWindow.SubmitText(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;");

            var testValue = Guid.NewGuid();

            _interactiveWindow.SubmitText($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;");

            await _visualStudio.Instance.ClickAutomationElementAsync(testValue.ToString(), recursive: true);

            _interactiveWindow.WaitForReplOutput("Hello, World!");

            Assert.Equal("Hello, World!", _interactiveWindow.GetLastReplOutput());

            _interactiveWindow.SubmitText("b = null; w.Close(); w = null;");
        }
    }
}
