// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Integration.UnitTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveDemo
    {
        private readonly IntegrationHost _host;
        private readonly InteractiveWindow _interactiveWindow;

        public CSharpInteractiveDemo(SharedIntegrationHost sharedHost)
        {
            _host = sharedHost.GetHost();

            _interactiveWindow = _host.CSharpInteractiveWindow;

            _interactiveWindow.ShowAsync().GetAwaiter().GetResult();
            _interactiveWindow.ResetAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task BclMathCall()
        {
            await _interactiveWindow.SubmitTextToReplAsync("Math.Sin(1)").ConfigureAwait(continueOnCapturedContext: false);
            Assert.Equal("0.8414709848078965", _interactiveWindow.LastReplOutput);
        }

        [Fact]
        public async Task BclConsoleCall()
        {
            await _interactiveWindow.SubmitTextToReplAsync(@"Console.WriteLine(""Hello, World!"");").ConfigureAwait(continueOnCapturedContext: false);
            Assert.Equal("Hello, World!", _interactiveWindow.LastReplOutput);
        }

        [Fact]
        public async Task ForStatement()
        {
            await _interactiveWindow.SubmitTextToReplAsync("for (int i = 0; i < 10; i++) Console.WriteLine(i * i);").ConfigureAwait(continueOnCapturedContext: false);
            Assert.EndsWith($"{81}", _interactiveWindow.LastReplOutput);
        }

        [Fact]
        public async Task ForEachStatement()
        {
            await _interactiveWindow.SubmitTextToReplAsync(@"foreach (var f in System.IO.Directory.GetFiles(@""c:\windows"")) Console.WriteLine($""{f}"".ToLower());").ConfigureAwait(continueOnCapturedContext: false);
            Assert.Contains(@"c:\windows\win.ini", _interactiveWindow.LastReplOutput);
        }

        [Fact]
        public async Task TopLevelMethod()
        {
            await _interactiveWindow.SubmitTextToReplAsync(@"int Fac(int x)
{
    return x < 1 ? 1 : x * Fac(x - 1);
}
Fac(4)").ConfigureAwait(continueOnCapturedContext: false);
            Assert.Equal($"{24}", _interactiveWindow.LastReplOutput);
        }

        [Fact]
        public async Task WpfInteraction()
        {
            await _interactiveWindow.SubmitTextToReplAsync(@"#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""").ConfigureAwait(continueOnCapturedContext: false);

            await _interactiveWindow.SubmitTextToReplAsync(@"using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;").ConfigureAwait(continueOnCapturedContext: false);

            await _interactiveWindow.SubmitTextToReplAsync(@"var w = new Window();
w.Title = ""Hello World"";
w.FontFamily = new FontFamily(""Calibri"");
w.FontSize = 24;
w.Height = 300;
w.Width = 300;
w.Topmost = true;
w.Visibility = Visibility.Visible;").ConfigureAwait(continueOnCapturedContext: false);

            var testValue = Guid.NewGuid();

            await _interactiveWindow.SubmitTextToReplAsync($@"var b = new Button();
b.Content = ""{testValue}"";
b.Margin = new Thickness(40);
b.Click += (sender, e) => Console.WriteLine(""Hello, World!"");

var g = new Grid();
g.Children.Add(b);
w.Content = g;").ConfigureAwait(continueOnCapturedContext: false);

            await _host.ClickAutomationElementAsync(testValue.ToString(), recursive: true).ConfigureAwait(continueOnCapturedContext: false);
            await _interactiveWindow.WaitForReplPromptAsync().ConfigureAwait(continueOnCapturedContext: false);

            Assert.Equal("Hello, World!", _interactiveWindow.LastReplOutput);

            await _interactiveWindow.SubmitTextToReplAsync("b = null; w.Close(); w = null;").ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
