// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveFormsAndWpf : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveFormsAndWpf(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.InteractiveWindow.SubmitText(@"#r ""System.Windows.Forms""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            VisualStudio.InteractiveWindow.SubmitText(@"using System.Windows;
using System.Windows.Forms;
using Wpf = System.Windows.Controls;");
        }

        [WpfFact]
        public void InteractiveWithDisplayFormAndWpfWindow()
        {
            // 1) Create and display form and WPF window
            VisualStudio.InteractiveWindow.SubmitText(@"Form form = new Form();
form.Text = ""win form text"";
form.Show();
Window wind = new Window();
wind.Title = ""wpf window text"";
wind.Show();");

            var form = AutomationElementHelper.FindAutomationElementAsync("win form text").Result;
            var wpf = AutomationElementHelper.FindAutomationElementAsync("wpf window text").Result;

            // 3) Add UI elements to windows and verify
            VisualStudio.InteractiveWindow.SubmitText(@"// add a label to the form
Label l = new Label();
l.Text = ""forms label text"";
form.Controls.Add(l);
// set simple text as the body of the wpf window
Wpf.TextBlock t = new Wpf.TextBlock();
t.Text = ""wpf body text"";
wind.Content = t;");

            var formLabel = form.FindDescendantByPath("text");
            Assert.Equal("forms label text", formLabel.CurrentName);

            var wpfContent = wpf.FindDescendantByPath("text");
            Assert.Equal("wpf body text", wpfContent.CurrentName);

            // 4) Close windows
            VisualStudio.InteractiveWindow.SubmitText(@"form.Close();
wind.Close();");
        }
    }
}
