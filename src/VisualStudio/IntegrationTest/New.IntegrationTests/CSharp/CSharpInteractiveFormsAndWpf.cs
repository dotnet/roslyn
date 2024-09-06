// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Threading;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpInteractiveFormsAndWpf : AbstractInteractiveWindowTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await TestServices.InteractiveWindow.SubmitTextAsync(@"#r ""System.Windows.Forms""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""", HangMitigatingCancellationToken);

        await TestServices.InteractiveWindow.SubmitTextAsync(@"using System.Windows;
using System.Windows.Forms;
using Wpf = System.Windows.Controls;", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task InteractiveWithDisplayFormAndWpfWindow()
    {
        // 1) Create and display form and WPF window
        await TestServices.InteractiveWindow.SubmitTextAsync(@"Form form = new Form();
form.Text = ""win form text"";
form.Show();
Window wind = new Window();
wind.Title = ""wpf window text"";
wind.Show();", HangMitigatingCancellationToken);

        var form = await AutomationElementHelper.FindAutomationElementAsync("win form text").WithCancellation(HangMitigatingCancellationToken);
        var wpf = await AutomationElementHelper.FindAutomationElementAsync("wpf window text").WithCancellation(HangMitigatingCancellationToken);

        // 3) Add UI elements to windows and verify
        await TestServices.InteractiveWindow.SubmitTextAsync(@"// add a label to the form
Label l = new Label();
l.Text = ""forms label text"";
form.Controls.Add(l);
// set simple text as the body of the wpf window
Wpf.TextBlock t = new Wpf.TextBlock();
t.Text = ""wpf body text"";
wind.Content = t;", HangMitigatingCancellationToken);

        var formLabel = form.FindDescendantByPath("text");
        Assert.Equal("forms label text", formLabel.CurrentName);

        var wpfContent = wpf.FindDescendantByPath("text");
        Assert.Equal("wpf body text", wpfContent.CurrentName);

        // 4) Close windows
        await TestServices.InteractiveWindow.SubmitTextAsync(@"form.Close();
wind.Close();", HangMitigatingCancellationToken);
    }
}
