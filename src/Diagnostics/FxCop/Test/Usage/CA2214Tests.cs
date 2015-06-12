// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AnalyzerPowerPack.CSharp.Usage;
using Microsoft.AnalyzerPowerPack.VisualBasic.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public partial class CA2214Tests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA2214DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA2214DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualMethodCSharp()
        {
            VerifyCSharp(@"
class C
{
    C()
    {
        Foo();
    }

    protected virtual Foo() { }
}
",
            GetCA2214CSharpResultAt(6, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualMethodCSharpWithScope()
        {
            VerifyCSharp(@"
class C
{
    C()
    {
        Foo();
    }

    [|protected virtual Foo() { }|]
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualMethodBasic()
        {
            VerifyBasic(@"
Class C
    Public Sub New()
        Foo()
    End Sub
    Overridable Sub Foo()
    End Sub
End Class
",
            GetCA2214BasicResultAt(4, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualMethodBasicwithScope()
        {
            VerifyBasic(@"
Class C
    Public Sub New()
        Foo()
    End Sub
    [|Overridable Sub Foo()
    End Sub|]
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214AbstractMethodCSharp()
        {
            VerifyCSharp(@"
class C
{
    C()
    {
        Foo();
    }

    protected abstract Foo();
}
",
            GetCA2214CSharpResultAt(6, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214AbstractMethodBasic()
        {
            VerifyBasic(@"
Class C
    Public Sub New()
        Foo()
    End Sub
    MustOverride Sub Foo()
End Class
",
            GetCA2214BasicResultAt(4, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214MultipleInstancesCSharp()
        {
            VerifyCSharp(@"
class C
{
    C()
    {
        Foo();
        Bar();
    }

    protected abstract Foo();
    protected virtual Bar() { }
}
",
            GetCA2214CSharpResultAt(6, 9),
            GetCA2214CSharpResultAt(7, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214MultipleInstancesBasic()
        {
            VerifyBasic(@"
Class C
    Public Sub New()
        Foo()
        Bar()
    End Sub
    MustOverride Sub Foo()
    Overridable Sub Bar()
    End Sub
End Class
",
           GetCA2214BasicResultAt(4, 9),
           GetCA2214BasicResultAt(5, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214NotTopLevelCSharp()
        {
            VerifyCSharp(@"
class C
{
    C()
    {
        if (true)
        {
            Foo();
        }

        if (false)
        {
            Foo(); // also check unreachable code
        }
    }

    protected abstract Foo();
}
",
            GetCA2214CSharpResultAt(8, 13),
            GetCA2214CSharpResultAt(13, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214NotTopLevelBasic()
        {
            VerifyBasic(@"
Class C
    Public Sub New()
        If True Then
            Foo()
        End If

        If False Then
            Foo() ' also check unreachable code
        End If
    End Sub
    MustOverride Sub Foo()
End Class
",
            GetCA2214BasicResultAt(5, 13),
            GetCA2214BasicResultAt(9, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214NoDiagnosticsOutsideConstructorCSharp()
        {
            VerifyCSharp(@"
class C
{
    protected abstract Foo();

    void Method()
    {
        Foo();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214NoDiagnosticsOutsideConstructorBasic()
        {
            VerifyBasic(@"
Class C
    MustOverride Sub Foo()

    Sub Method()
        Foo()
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214SpecialInheritanceCSharp()
        {
            var source = @"
class C : System.Web.UI.Control
{
    C()
    {
        // no diagnostics because we inherit from System.Web.UI.Control
        Foo();
        OnLoad(null);
    }

    protected abstract Foo();
}

class D : System.Windows.Forms.Control
{
    D()
    {
        // no diagnostics because we inherit from System.Web.UI.Control
        Foo();
        OnPaint(null);
    }

    protected abstract Foo();
}

class ControlBase : System.Windows.Forms.Control
{
}

class E : ControlBase
{
    E()
    {
        OnLoad(null); // no diagnostics when we're not an immediate descendant of a special class
    }
}
";
            var document = CreateDocument(source, LanguageNames.CSharp);
            var project = document.Project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Web.UI.Control).Assembly.Location));
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Windows.Forms.Control).Assembly.Location));
            var analyzer = GetCSharpDiagnosticAnalyzer();
            GetSortedDiagnostics(analyzer, project.Documents.Single()).Verify(analyzer);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214SpecialInheritanceBasic()
        {
            var source = @"
Class C
    Inherits System.Web.UI.Control
    Public Sub New()
        ' no diagnostics because we inherit from System.Web.UI.Control
        Foo()
        OnLoad(Nothing)
    End Sub
    MustInerit Sub Foo()
End Class

Class D
    Inherits System.Windows.Forms.Control
    Public Sub New()
        ' no diagnostics because we inherit from System.Web.UI.Control
        Foo()
        OnPaint(Nothing)
    End Sub
    MustInerit Sub Foo()
End Class

Class ControlBase
    Inherits System.Windows.Forms.Control
End Class

Class E
    Inherits ControlBase
    Public Sub New()
        OnLoad(Nothing) ' no diagnostics when we're not an immediate descendant of a special class
    End Sub
End Class
";
            var document = CreateDocument(source, LanguageNames.VisualBasic);
            var project = document.Project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Web.UI.Control).Assembly.Location));
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Windows.Forms.Control).Assembly.Location));
            var analyzer = GetBasicDiagnosticAnalyzer();
            GetSortedDiagnostics(analyzer, project.Documents.Single()).Verify(analyzer);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualOnOtherClassesCSharp()
        {
            VerifyCSharp(@"
class D
{
    public virtual void Foo() {}
}

class C
{
    public C(object obj, D d)
    {
        if (obj.Equals(d))
        {
            d.Foo();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2214VirtualOnOtherClassesBasic()
        {
            VerifyBasic(@"
Class D
    Public Overridable Sub Foo()
    End Sub
End Class

Class C
    Public Sub New(obj As Object, d As D)
        If obj.Equals(d) Then
            d.Foo()
        End If
    End Sub
End Class
");
        }

        internal static string CA2214Name = "CA2214";
        internal static string CA2214Message = "Do not call overridable methods in constructors";

        private static DiagnosticResult GetCA2214CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA2214Name, CA2214Message);
        }

        private static DiagnosticResult GetCA2214BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA2214Name, CA2214Message);
        }
    }
}
