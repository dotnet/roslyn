using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicAddMissingReference : AbstractEditorTest
    {
        private const string FileInLibraryProject1 = @"Public Class Class1
    Inherits System.Windows.Forms.Form
    Public Sub foo()

    End Sub
End Class

Public Class class2
    Public Sub foo(ByVal x As System.Windows.Forms.Form)

    End Sub

    Public Event ee As System.Windows.Forms.ColumnClickEventHandler
End Class

Public Class class3
    Implements System.Windows.Forms.IButtonControl

    Public Property DialogResult() As System.Windows.Forms.DialogResult Implements System.Windows.Forms.IButtonControl.DialogResult
        Get

        End Get
        Set(ByVal Value As System.Windows.Forms.DialogResult)

        End Set
    End Property

    Public Sub NotifyDefault(ByVal value As Boolean) Implements System.Windows.Forms.IButtonControl.NotifyDefault

    End Sub

    Public Sub PerformClick() Implements System.Windows.Forms.IButtonControl.PerformClick

    End Sub
End Class";
        private const string FileInLibraryProject2 = @"Public Class Class1
    Inherits System.Xml.XmlAttribute
    Sub New()
        MyBase.New(Nothing, Nothing, Nothing, Nothing)
    End Sub
    Sub foo()

    End Sub
    Public bar As ClassLibrary3.Class1
End Class";
        private const string FileInLibraryProject3 = @"Public Class Class1
    Public Enum E
        E1
        E2
    End Enum

    Public Function Foo() As ADODB.Recordset
        Dim x As ADODB.Recordset = Nothing
        Return x
    End Function


End Class";
        private const string FileInConsoleProject1 = @"Imports System.Data.XLinq

Module Module1

    Sub Main()
        'ERRID_UnreferencedAssembly3
        Dim y As New ClassLibrary1.class2
        y.foo(Nothing)

        'ERRID_UnreferencedAssemblyEvent3
        AddHandler y.ee, Nothing

        'ERRID_UnreferencedAssemblyBase3
        Dim x As New ClassLibrary1.Class1
        x.foo()
        'ERRID_UnreferencedAssemblyImplements3
        Dim z As New ClassLibrary1.class3
        Dim xxx = z.DialogResult
        'ERRID_SymbolFromUnreferencedProject3
        Dim a As New ClassLibrary2.Class1
        Dim c As Boolean = a.HasChildNodes()

        Dim d = a.bar
    End Sub

End Module";

        private const string ClassLibrary1Name = "ClassLibrary1";
        private const string ClassLibrary2Name = "ClassLibrary2";
        private const string ClassLibrary3Name = "ClassLibrary3";
        private const string ConsoleProjectName = "ConsoleApplication1";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicAddMissingReference(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution("ReferenceErrors", solutionElement: XElement.Parse(
                "<Solution>" +
               $"   <Project ProjectName=\"{ClassLibrary1Name}\" ProjectTemplate=\"{WellKnownProjectTemplates.WinFormsApplication}\" Language=\"{LanguageNames.VisualBasic}\">" +
                "       <Document FileName=\"Class1.vb\"><![CDATA[" +
                FileInLibraryProject1 +
                "]]>" +
                "       </Document>" +
                "   </Project>" +
               $"   <Project ProjectName=\"{ClassLibrary2Name}\" ProjectReferences=\"{ClassLibrary3Name}\" ProjectTemplate=\"{WellKnownProjectTemplates.ClassLibrary}\" Language=\"{LanguageNames.VisualBasic}\">" +
                "       <Document FileName=\"Class1.vb\"><![CDATA[" +
               FileInLibraryProject2 +
                "]]>" +
                "       </Document>" +
                "   </Project>" +
               $"   <Project ProjectName=\"{ClassLibrary3Name}\" ProjectTemplate=\"{WellKnownProjectTemplates.ClassLibrary}\" Language=\"{LanguageNames.VisualBasic}\">" +
                "       <Document FileName=\"Class1.vb\"><![CDATA[" +
               FileInLibraryProject3 +
                "]]>" +
                "       </Document>" +
                "   </Project>" +
               $"   <Project ProjectName=\"{ConsoleProjectName}\" ProjectReferences=\"{ClassLibrary1Name};{ClassLibrary2Name}\" ProjectTemplate=\"{WellKnownProjectTemplates.ConsoleApplication}\" Language=\"{LanguageNames.VisualBasic}\">" +
                "       <Document FileName=\"Module1.vb\"><![CDATA[" +
               FileInConsoleProject1 +
                "]]>" +
                "       </Document>" +
               "   </Project>" +
                "</Solution>"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AddMissingReference)]
        public void Verify_Available_Code_Actions()
        {
            OpenFile(ConsoleProjectName, "Module1.vb");
            PlaceCaret("y.foo", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add reference to 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.", applyFix: false);
            PlaceCaret("x.foo", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add reference to 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.", applyFix: false);
            PlaceCaret("z.DialogResult", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add reference to 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.", applyFix: false);
            PlaceCaret("a.bar", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add project reference to 'ClassLibrary3'.", applyFix: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AddMissingReference)]
        public void Invoke_Some_Fixes_In_VisualBasic_Then_Verify_References()
        {
            OpenFile(ConsoleProjectName, "Module1.vb");
            PlaceCaret("y.foo", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add reference to 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.", applyFix: true);
            VerifyAssemblyReferencePresent(
                projectName: ConsoleProjectName,
                assemblyName: "System.Windows.Forms",
                assemblyVersion: "4.0.0.0",
                assemblyPublicKeyToken: "b77a5c561934e089");
            PlaceCaret("a.bar", charsOffset: 1);
            InvokeCodeActionList();
            VerifyCodeAction("Add project reference to 'ClassLibrary3'.", applyFix: true);
            VerifyProjectReferencePresent(
                projectName: ConsoleProjectName,
                referencedProjectName: ClassLibrary3Name);
        }
    }
}
