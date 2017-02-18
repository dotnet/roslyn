using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpWinForms : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpWinForms(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpWinForms), WellKnownProjectTemplates.WinFormsApplication, clearEditor: false)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Add_Control()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            CloseFile(ProjectName, "Form1.cs", saveFile: true);
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Name = ""SomeButton""");
            VerifyTextContains(@"private System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Control_Property()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            EditWinFormButtonProperty(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Text", propertyValue: "NewButtonText");
            CloseFile(ProjectName, "Form1.cs", saveFile: true);
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Text = ""NewButtonText""");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Control_Property_In_Code()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            EditWinFormButtonProperty(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextGoesHere");
            VerifyWinFormButtonPropertySet(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Text", expectedPropertyValue: "ButtonTextGoesHere");
            CloseFile(ProjectName, "Form1.cs", saveFile: true);
            //  Change the control's text in designer.cs code 
            OpenFile(ProjectName, "Form1.Designer.cs");
            //  Verify that the control's property was set correctly. The following text should appear in InitializeComponent().
            VerifyTextContains(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            //  Replace text property with something else 
            SelectTextInCurrentDocument(@"this.SomeButton.Text = ""ButtonTextGoesHere"";");
            SendKeys(@"this.SomeButton.Text = ""GibberishText"";");
            CloseFile(ProjectName, "Form1.Designer.cs", saveFile: true);
            //  Verify that the control text has changed in the designer 
            OpenFileWithDesigner(ProjectName, "Form1.cs");
            VerifyWinFormButtonPropertySet(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Text", expectedPropertyValue: "GibberishText");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Add_Click_Handler()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            EditWinFormsButtonEvent(ProjectName, "Form1.cs", buttonName: "SomeButton", eventName: "Click", eventHandlerName: "ExecuteWhenButtonClicked");
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeButton.Click += new System.EventHandler(this.ExecuteWhenButtonClicked);");
            OpenFile(ProjectName, "Form1.cs");
            VerifyTextContains(@"    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ExecuteWhenButtonClicked(object sender, EventArgs e)
        {

        }
    }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Rename_Control()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            // Add some control properties and events 
            EditWinFormButtonProperty(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Text", propertyValue: "ButtonTextValue");
            EditWinFormsButtonEvent(ProjectName, "Form1.cs", buttonName: "SomeButton", eventName: "Click", eventHandlerName: "SomeButtonHandler");
            // Rename the control 
            EditWinFormButtonProperty(ProjectName, "Form1.cs", buttonName: "SomeButton", propertyName: "Name", propertyValue: "SomeNewButton");
            VerifyNoBuildErrors();
            // Verify that the rename propagated in designer code 
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"this.SomeNewButton.Name = ""SomeNewButton"";");
            VerifyTextContains(@"this.SomeNewButton.Text = ""ButtonTextValue"";");
            VerifyTextContains(@"this.SomeNewButton.Click += new System.EventHandler(this.SomeButtonHandler);");
            VerifyTextContains(@"private System.Windows.Forms.Button SomeNewButton;");
            // Verify that the old button name goes away 
            VerifyTextDoesNotContain(@"private System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Remove_Event_Handler()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            EditWinFormsButtonEvent(ProjectName, "Form1.cs", buttonName: "SomeButton", eventName: "Click", eventHandlerName: "FooHandler");
            //  Remove the event handler 
            EditWinFormsButtonEvent(ProjectName, "Form1.cs", buttonName: "SomeButton", eventName: "Click", eventHandlerName: "");
            VerifyNoBuildErrors();
            //  Verify that the handler is removed 
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextDoesNotContain(@"this.SomeButton.Click += new System.EventHandler(this.FooHandler);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Change_Accessibility()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            EditWinFormButtonProperty(ProjectName, "Form1.cs", 
                buttonName: "SomeButton",
                propertyName: "Modifiers",
                propertyTypeName: "System.CodeDom.MemberAttributes",
                propertyValue: "Public");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextContains(@"public System.Windows.Forms.Button SomeButton;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.WinForms)]
        public void Delete_Control()
        {
            AddWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            DeleteWinFormButton(ProjectName, "Form1.cs", "SomeButton");
            VerifyNoBuildErrors();
            OpenFile(ProjectName, "Form1.Designer.cs");
            VerifyTextDoesNotContain(@"this.SomeButton.Name = ""SomeButton"";");
            VerifyTextDoesNotContain(@"private System.Windows.Forms.Button SomeButton;");
        }
    }
}
