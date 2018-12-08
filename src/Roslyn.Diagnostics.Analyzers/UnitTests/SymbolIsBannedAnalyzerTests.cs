// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    // For specification of document comment IDs see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#processing-the-documentation-file

    public class SymbolIsBannedAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        private void VerifyBasic(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(SymbolIsBannedAnalyzer.BannedSymbolsFileName, bannedApiText);
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(SymbolIsBannedAnalyzer.BannedSymbolsFileName, bannedApiText);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        #region Diagnostic tests

        [Fact]
        public void NoDiagnosticForNoBannedText()
        {
            Verify("class C { }", LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles: null, compilationOptions: null, parseOptions: null, expected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public void NoDiagnosticReportedForEmptyBannedText()
        {
            var source = @"";

            var bannedText = @"";

            VerifyCSharp(source, bannedText);
        }

        [Fact]
        public void NoDiagnosticForInvalidBannedText()
        {
            VerifyCSharp(source: "", bannedApiText: null);
        }

        [Fact]
        public void DiagnosticReportedForDuplicateBannedApiLines()
        {
            var source = @"";
            var bannedText = @"
T:System.Console
T:System.Console";

            VerifyCSharp(source, bannedText,
                GetResultAt(
                    SymbolIsBannedAnalyzer.BannedSymbolsFileName,
                    SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.Id,
                    string.Format(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.MessageFormat.ToString(), "T:System.Console"),
                    "(3,1)", "(2,1)"));
        }

        [Fact]
        public void CSharp_BannedApiFile_MessageIncludedInDiagnostic()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"T:N.Banned;Use NonBanned instead";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));
        }

        [Fact]
        public void CSharp_BannedApiFile_WhiteSpace()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"
  T:N.Banned  ";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public void CSharp_BannedApiFile_WhiteSpaceWithMessage()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"T:N.Banned ; Use NonBanned instead ";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));
        }

        [Fact]
        public void CSharp_BannedApiFile_EmptyMessage()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"T:N.Banned;";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public void CSharp_BannedType_Constructor()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"
T:N.Banned";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public void CSharp_BannedGenericType_Constructor()
        {
            var source = @"
class C
{
    void M()
    {
        var c = new System.Collections.Generic.List<string>();
    }
}";

            var bannedText = @"
T:System.Collections.Generic.List`1";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(6, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List<T>", ""));
        }

        [Fact]
        public void CSharp_BannedNestedType_Constructor()
        {
            var source = @"
class C
{
    class Nested { }
    void M()
    {
        var n = new Nested();
    }
}";

            var bannedText = @"
T:C.Nested";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(7, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested", ""));
        }

        [Fact]
        public void CSharp_BannedType_MethodOnNestedType()
        {
            var source = @"
class C
{
    public static class Nested
    {
        public static void M() { }
    }
}

class D
{
    void M2()
    {
        C.Nested.M();
    }
}";
            var bannedText = @"
T:C";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(14, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedInterface_Method()
        {
            var source = @"
interface I
{
    void M();
}

class C
{
    void M()
    {
        I i = null;
        i.M();
    }
}";
            var bannedText = @"T:I";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(12, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I", ""));
        }

        [Fact]
        public void CSharp_BannedClass_Property()
        {
            var source = @"
class C
{
    public int P { get; set; }
    void M()
    {
        P = P;
    }
}";
            var bannedText = @"T:C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(7, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedClass_Field()
        {
            var source = @"
class C
{
    public int F;
    void M()
    {
        F = F;
    }
}";
            var bannedText = @"T:C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(7, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedClass_Event()
        {
            var source = @"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        E += null;
        E -= null;
        E(null, EventArgs.Empty);
    }
}";
            var bannedText = @"T:C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(9, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(10, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(11, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedClass_MethodGroup()
        {
            var source = @"
delegate void D();
class C
{
    void M()
    {
        D d = M;
    }
}
";
            var bannedText = @"T:C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 15, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedClass_DocumentationReference()
        {
            var source = @"
class C { }

/// <summary><see cref=""C"" /></summary>
class D { }
";
            var bannedText = @"T:C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(4, 25, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void CSharp_BannedAttribute_UsageOnType()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }

[Banned]
class C { }
class D : C { }
";
            var bannedText = @"T:BannedAttribute";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void CSharp_BannedAttribute_UsageOnMember()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }

class C 
{
    [Banned]
    public int Foo { get; }
}
";
            var bannedText = @"T:BannedAttribute";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(9, 6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void CSharp_BannedAttribute_UsageOnAssembly()
        {
            var source = @"
using System;

[assembly: BannedAttribute]

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }
";

            var bannedText = @"T:BannedAttribute";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(4, 12, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void CSharp_BannedAttribute_UsageOnModule()
        {
            var source = @"
using System;

[module: BannedAttribute]

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }
";

            var bannedText = @"T:BannedAttribute";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(4, 10, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void CSharp_BannedConstructor()
        {
            var source = @"
namespace N
{
    class Banned
    {
        public Banned() {}
        public Banned(int i) {}
    }
    class C
    {
        void M()
        {
            var c = new Banned();
            var d = new Banned(1);
        }
    }
}";

            var bannedText1 = @"M:N.Banned.#ctor";
            var bannedText2 = @"M:N.Banned.#ctor(System.Int32)";

            VerifyCSharp(
                source,
                bannedText1,
                GetCSharpResultAt(13, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned()", ""));

            VerifyCSharp(
                source,
                bannedText2,
                GetCSharpResultAt(14, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned(int)", ""));
        }

        [Fact]
        public void CSharp_BannedMethod()
        {
            var source = @"
namespace N
{
    class C
    {
        public void Banned() {}
        public void Banned(int i) {}

        void M()
        {
            Banned();
            Banned(1);
        }
    }
}";

            var bannedText1 = @"M:N.C.Banned";
            var bannedText2 = @"M:N.C.Banned(System.Int32)";

            VerifyCSharp(
                source,
                bannedText1,
                GetCSharpResultAt(11, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));

            VerifyCSharp(
                source,
                bannedText2,
                GetCSharpResultAt(12, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned(int)", ""));
        }

        [Fact]
        public void CSharp_BannedProperty()
        {
            var source = @"
namespace N
{
    class C
    {
        public int Banned { get; set; }

        void M()
        {
            Banned = Banned;
        }
    }
}";

            var bannedText = @"P:N.C.Banned";

            VerifyCSharp(
                source,
                bannedText,
                GetCSharpResultAt(10, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(10, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public void CSharp_BannedField()
        {
            var source = @"
namespace N
{
    class C
    {
        public int Banned;

        void M()
        {
            Banned = Banned;
        }
    }
}";

            var bannedText = @"F:N.C.Banned";

            VerifyCSharp(
                source,
                bannedText,
                GetCSharpResultAt(10, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(10, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public void CSharp_BannedEvent()
        {
            var source = @"
namespace N
{
    class C
    {
        public event System.Action Banned;

        void M()
        {
            Banned += null;
            Banned -= null;
            Banned();
        }
    }
}";

            var bannedText = @"E:N.C.Banned";

            VerifyCSharp(
                source,
                bannedText,
                GetCSharpResultAt(10, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(11, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(12, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public void CSharp_BannedMethodGroup()
        {
            var source = @"
namespace N
{
    class C
    {
        public void Banned() {}

        void M()
        {
            System.Action b = Banned;
        }
    }
}";

            var bannedText = @"M:N.C.Banned";

            VerifyCSharp(
                source,
                bannedText,
                GetCSharpResultAt(10, 31, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));
        }

        [Fact]
        public void VisualBasic_BannedType_Constructor()
        {
            var source = @"
Namespace N
    Class Banned : End Class
    Class C
        Sub M()
            Dim c As New Banned()
        End Sub
    End Class
End Namespace";

            var bannedText = @"T:N.Banned";

            VerifyBasic(source, bannedText, GetBasicResultAt(6, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public void VisualBasic_BannedGenericType_Constructor()
        {
            var source = @"
Class C
    Sub M()
        Dim c = New System.Collections.Generic.List(Of String)()
    End Sub
End Class";

            var bannedText = @"
T:System.Collections.Generic.List`1";

            VerifyBasic(source, bannedText, GetBasicResultAt(4, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List(Of T)", ""));
        }

        [Fact]
        public void VisualBasic_BannedNestedType_Constructor()
        {
            var source = @"
Class C
    Class Nested : End Class
    Sub M()
        Dim n As New Nested()
    End Sub
End Class";

            var bannedText = @"
T:C.Nested";

            VerifyBasic(source, bannedText, GetBasicResultAt(5, 18, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested", ""));
        }

        [Fact]
        public void VisualBasic_BannedType_MethodOnNestedType()
        {
            var source = @"
Class C
    Public Class Nested
        Public Shared Sub M() : End Sub
    End Class
End Class

Class D
    Sub M2()
        C.Nested.M()
    End Sub
End Class
";
            var bannedText = @"
T:C";

            VerifyBasic(source, bannedText, GetBasicResultAt(10, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void VisualBasic_BannedInterface_Method()
        {
            var source = @"
Interface I
    Sub M()
End Interface

Class C
    Sub M()
        Dim i As I = Nothing
        i.M()
    End Sub
End Class";
            var bannedText = @"T:I";

            VerifyBasic(source, bannedText, GetBasicResultAt(9, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I", ""));
        }

        [Fact]
        public void VisualBasic_BannedClass_Property()
        {
            var source = @"
Class C
    Public Property P As Integer
    Sub M()
        P = P
    End Sub
End Class";
            var bannedText = @"T:C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(5, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void VisualBasic_BannedClass_Field()
        {
            var source = @"
Class C
    Public F As Integer
    Sub M()
        F = F
    End Sub
End Class";
            var bannedText = @"T:C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(5, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void VisualBasic_BannedClass_Event()
        {
            var source = @"
Imports System

Class C
    public Event E As EventHandler
    Sub M()
        AddHandler E, Nothing
        RemoveHandler E, Nothing
        RaiseEvent E(Me, EventArgs.Empty)
    End Sub
End Class";
            var bannedText = @"T:C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(7, 20, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(8, 23, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetBasicResultAt(9, 20, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void VisualBasic_BannedClass_MethodGroup()
        {
            var source = @"
Delegate Sub D()
Class C
    Sub M()
        Dim d as D = AddressOf M
    End Sub
End Class";
            var bannedText = @"T:C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public void VisualBasic_BannedAttribute_UsageOnType()
        {
            var source = @"
Imports System

<AttributeUsage(AttributeTargets.All, Inherited:=true)>
Class BannedAttribute
    Inherits Attribute
End Class

<Banned>
Class C
End Class
Class D
    Inherits C
End Class
";
            var bannedText = @"T:BannedAttribute";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(9, 2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void VisualBasic_BannedAttribute_UsageOnMember()
        {
            var source = @"
Imports System

<AttributeUsage(System.AttributeTargets.All, Inherited:=True)>
Class BannedAttribute
    Inherits System.Attribute
End Class

Class C
    <Banned>
    Public ReadOnly Property Foo As Integer
End Class
";
            var bannedText = @"T:BannedAttribute";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(10, 6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void VisualBasic_BannedAttribute_UsageOnAssembly()
        {
            var source = @"
Imports System

<Assembly:BannedAttribute>

<AttributeUsage(AttributeTargets.All, Inherited:=True)>
Class BannedAttribute
    Inherits Attribute
End Class
";

            var bannedText = @"T:BannedAttribute";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(4, 2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void VisualBasic_BannedAttribute_UsageOnModule()
        {
            var source = @"
Imports System

<Module:BannedAttribute>

<AttributeUsage(AttributeTargets.All, Inherited:=True)>
Class BannedAttribute
    Inherits Attribute
End Class
";

            var bannedText = @"T:BannedAttribute";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(4, 2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public void VisualBasic_BannedConstructor()
        {
            var source = @"
Namespace N
    Class Banned
        Sub New : End Sub
        Sub New(ByVal I As Integer) : End Sub
    End Class
    Class C
        Sub M()
            Dim c As New Banned()
            Dim d As New Banned(1)
        End Sub
    End Class
End Namespace";

            var bannedText1 = @"M:N.Banned.#ctor";
            var bannedText2 = @"M:N.Banned.#ctor(System.Int32)";

            VerifyBasic(
                source,
                bannedText1,
                GetBasicResultAt(9, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New()", ""));

            VerifyBasic(
                source,
                bannedText2,
                GetBasicResultAt(10, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub New(I As Integer)", ""));
        }

        [Fact]
        public void VisualBasic_BannedMethod()
        {
            var source = @"
Namespace N
    Class C
        Sub Banned : End Sub
        Sub Banned(ByVal I As Integer) : End Sub
        Sub M()
            Me.Banned()
            Me.Banned(1)
        End Sub
    End Class
End Namespace";

            var bannedText1 = @"M:N.C.Banned";
            var bannedText2 = @"M:N.C.Banned(System.Int32)";

            VerifyBasic(
                source,
                bannedText1,
                GetBasicResultAt(7, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned()", ""));

            VerifyBasic(
                source,
                bannedText2,
                GetBasicResultAt(8, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned(I As Integer)", ""));
        }

        [Fact]
        public void VisualBasic_BannedProperty()
        {
            var source = @"
Namespace N
    Class C
        Public Property Banned As Integer
        Sub M()
            Banned = Banned
        End Sub
    End Class
End Namespace";

            var bannedText = @"P:N.C.Banned";

            VerifyBasic(
                source,
                bannedText,
                GetBasicResultAt(6, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Property Banned As Integer", ""),
                GetBasicResultAt(6, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Property Banned As Integer", ""));
        }

        [Fact]
        public void VisualBasic_BannedField()
        {
            var source = @"
Namespace N
    Class C
        Public Banned As Integer
        Sub M()
            Banned = Banned
        End Sub
    End Class
End Namespace";

            var bannedText = @"F:N.C.Banned";

            VerifyBasic(
                source,
                bannedText,
                GetBasicResultAt(6, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Banned As Integer", ""),
                GetBasicResultAt(6, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Banned As Integer", ""));
        }

        [Fact]
        public void VisualBasic_BannedEvent()
        {
            var source = @"
Namespace N
    Class C
        Public Event Banned As System.Action
        Sub M()
            AddHandler Banned, Nothing
            RemoveHandler Banned, Nothing
            RaiseEvent Banned()
        End Sub
    End Class
End Namespace";

            var bannedText = @"E:N.C.Banned";

            VerifyBasic(
                source,
                bannedText,
                GetBasicResultAt(6, 24, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""),
                GetBasicResultAt(7, 27, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""),
                GetBasicResultAt(8, 24, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Event Banned As Action", ""));
        }

        [Fact]
        public void VisualBasic_BannedMethodGroup()
        {
            var source = @"
Namespace N
    Class C
        Public Sub Banned() : End Sub
        Sub M()
            Dim b As System.Action = AddressOf Banned
        End Sub
    End Class
End Namespace";

            var bannedText = @"M:N.C.Banned";

            VerifyBasic(
                source,
                bannedText,
                GetBasicResultAt(6, 38, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Public Sub Banned()", ""));
        }

        [Fact]
        public void VisualBasic_BannedClass_DocumentationReference()
        {
            var source = @"
Class C : End Class

''' <summary><see cref=""C"" /></summary>
Class D : End Class
";
            var bannedText = @"T:C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(4, 25, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        #endregion
    }
}
