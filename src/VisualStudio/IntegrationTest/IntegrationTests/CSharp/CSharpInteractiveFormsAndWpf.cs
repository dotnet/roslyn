// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpInteractiveFormsAndWpf : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveFormsAndWpf( )
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            VisualStudioInstance.InteractiveWindow.SubmitText(@"#r ""System.Windows.Forms""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""
#r ""System.Xaml""");

            VisualStudioInstance.InteractiveWindow.SubmitText(@"using System.Windows;
using System.Windows.Forms;
using Wpf = System.Windows.Controls;");
        }

        [TestMethod]
        public void InteractiveWithDisplayFormAndWpfWindow()
        {
            // 1) Create and display form and WPF window
            VisualStudioInstance.InteractiveWindow.SubmitText(@"Form form = new Form();
form.Text = ""win form text"";
form.Show();
Window wind = new Window();
wind.Title = ""wpf window text"";
wind.Show();");

            var form = AutomationElementHelper.FindAutomationElementAsync("win form text").Result;
            var wpf = AutomationElementHelper.FindAutomationElementAsync("wpf window text").Result;

            // 3) Add UI elements to windows and verify
            VisualStudioInstance.InteractiveWindow.SubmitText(@"// add a label to the form
Label l = new Label();
l.Text = ""forms label text"";
form.Controls.Add(l);
// set simple text as the body of the wpf window
Wpf.TextBlock t = new Wpf.TextBlock();
t.Text = ""wpf body text"";
wind.Content = t;");

            var formLabel = form.FindDescendantByPath("text");
            Assert.AreEqual("forms label text", formLabel.CurrentName);

            var wpfContent = wpf.FindDescendantByPath("text");
            Assert.AreEqual("wpf body text", wpfContent.CurrentName);

            // 4) Close windows
            VisualStudioInstance.InteractiveWindow.SubmitText(@"form.Close();
wind.Close();");
        }
    }
}
