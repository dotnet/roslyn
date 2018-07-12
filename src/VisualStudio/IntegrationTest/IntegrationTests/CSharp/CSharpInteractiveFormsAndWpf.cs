// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveFormsAndWpf : AbstractIdeInteractiveWindowTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"#r ""System.Windows.Forms""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"using System.Windows;
using System.Windows.Forms;
using Wpf = System.Windows.Controls;");
        }

        [IdeFact]
        public async Task InteractiveWithDisplayFormAndWpfWindowAsync()
        {
            // 1) Create and display form and WPF window
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"Form form = new Form();
form.Text = ""win form text"";
form.Show();
Window wind = new Window();
wind.Title = ""wpf window text"";
wind.Show();");

            var form = await AutomationElementHelper.FindAutomationElementAsync("win form text");
            var  wpf = await AutomationElementHelper.FindAutomationElementAsync("wpf window text");

            // 3) Add UI elements to windows and verify
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"// add a label to the form
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
            await VisualStudio.InteractiveWindow.SubmitTextAsync(@"form.Close();
wind.Close();");
        }
    }
}
