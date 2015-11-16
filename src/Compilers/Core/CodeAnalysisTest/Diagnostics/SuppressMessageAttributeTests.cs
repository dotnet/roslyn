// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class SuppressMessageAttributeTests
    {
        #region Local Suppression

        [Fact]
        public void LocalSuppressionOnType()
        {
            VerifyCSharp(@"
[System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
public class C
{
}
public class C1
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C1"));
        }

        [Fact]
        public void MultipleLocalSuppressionsOnSingleSymbol()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(""Test"", ""Declaration"")]
[SuppressMessage(""Test"", ""TypeDeclaration"")]
public class C
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("C"), new WarningOnTypeDeclarationAnalyzer() });
        }

        [Fact]
        public void DuplicateLocalSuppressions()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(""Test"", ""Declaration"")]
[SuppressMessage(""Test"", ""Declaration"")]
public class C
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("C") });
        }

        [Fact]
        public void LocalSuppressionOnMember()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public void Foo() {}
    public void Foo1() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Foo") },
                Diagnostic("Declaration", "Foo1"));
        }

        #endregion

        #region Global Suppression

        [Fact]
        public void GlobalSuppressionOnNamespaces()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N.N1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N4"")]

namespace N
{
    namespace N1
    {
        namespace N2.N3
        {
        }
    }
}

namespace N4
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("N") },
                Diagnostic("Declaration", "N2"),
                Diagnostic("Declaration", "N3"));
        }

        [Fact]
        public void GlobalSuppressionOnTypes()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""Ef"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""Egg"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""Ele`2"")]

public class E
{
}
public interface Ef
{
}
public struct Egg
{
}
public delegate void Ele<T1,T2>(T1 x, T2 y);
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("E") });
        }

        [Fact]
        public void GlobalSuppressionOnNestedTypes()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""C.A1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""C+A2"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""member"", Target=""C+A3"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""member"", Target=""C.A4"")]

public class C
{
    public class A1 { }
    public class A2 { }
    public class A3 { }
    public delegate void A4();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("A") },
                Diagnostic("Declaration", "A1"),
                Diagnostic("Declaration", "A3"),
                Diagnostic("Declaration", "A4"));
        }

        [Fact]
        public void GlobalSuppressionOnBasicModule()
        {
            VerifyBasic(@"
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""M"")>

Module M
    Class C
    End Class
End Module
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") });
        }

        [Fact]
        public void GlobalSuppressionOnMembers()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M3`1()"")]

public class C
{
    int M1;
    public void M2() {}
    public static void M3<T>() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") },
                new[] { Diagnostic("Declaration", "M2") });
        }

        [Fact]
        public void MultipleGlobalSuppressionsOnSingleSymbol()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[assembly: SuppressMessage(""Test"", ""TypeDeclaration"", Scope=""Type"", Target=""E"")]

public class E
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("E"), new WarningOnTypeDeclarationAnalyzer() });
        }

        [Fact]
        public void DuplicateGlobalSuppressions()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]

public class E
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("E") });
        }

        #endregion

        #region Syntax Semantics

        [Fact]
        public void WarningOnCommentAnalyzerCSharp()
        {
            VerifyCSharp("// Comment\r\n /* Comment */",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "// Comment"),
                Diagnostic("Comment", "/* Comment */"));
        }

        [Fact]
        public void WarningOnCommentAnalyzerBasic()
        {
            VerifyBasic("' Comment",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "' Comment"));
        }

        [Fact]
        public void GloballySuppressSyntaxDiagnosticsCSharp()
        {
            VerifyCSharp(@"
// before module attributes
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"")]
// before class
public class C
{
    // before method
    public void Foo() // after method declaration
    {
        // inside method
    }
}
// after class
",
                new[] { new WarningOnCommentAnalyzer() });
        }

        [Fact]
        public void GloballySuppressSyntaxDiagnosticsBasic()
        {
            VerifyBasic(@"
' before module attributes
<Module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"")>
' before class
Public Class C
    ' before sub
    Public Sub Foo() ' after sub statement
        ' inside sub
    End Sub
End Class
' after class
",
                new[] { new WarningOnCommentAnalyzer() });
        }

        [Fact]
        public void GloballySuppressSyntaxDiagnosticsOnTargetCSharp()
        {
            VerifyCSharp(@"
// before module attributes
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"", Scope=""Member"" Target=""C.Foo():System.Void"")]
// before class
public class C
{
    // before method
    public void Foo() // after method declaration
    {
        // inside method
    }
}
// after class
",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "// before module attributes"),
                Diagnostic("Comment", "// before class"),
                Diagnostic("Comment", "// after class"));
        }

        [Fact]
        public void GloballySuppressSyntaxDiagnosticsOnTargetBasic()
        {
            VerifyBasic(@"
' before module attributes
<Module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"", Scope:=""Member"", Target:=""C.Foo():System.Void"")>
' before class
Public Class C
    ' before sub
    Public Sub Foo() ' after sub statement
        ' inside sub
    End Sub
End Class
' after class
",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "' before module attributes"),
                Diagnostic("Comment", "' before class"),
                Diagnostic("Comment", "' after class"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnNamespaceDeclarationCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"", Scope=""namespace"", Target=""A.B"")]
namespace A
[|{
    namespace B
    {
        class C {}
    }
}|]
",
                Diagnostic("Token", "{").WithLocation(4, 1),
                Diagnostic("Token", "class").WithLocation(7, 9),
                Diagnostic("Token", "C").WithLocation(7, 15),
                Diagnostic("Token", "{").WithLocation(7, 17),
                Diagnostic("Token", "}").WithLocation(7, 18),
                Diagnostic("Token", "}").WithLocation(9, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnNamespaceDeclarationBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"", Scope:=""Namespace"", Target:=""A.B"")>
Namespace [|A
    Namespace B 
        Class C
        End Class
    End Namespace
End|] Namespace
",
                Diagnostic("Token", "A").WithLocation(3, 11),
                Diagnostic("Token", "Class").WithLocation(5, 9),
                Diagnostic("Token", "C").WithLocation(5, 15),
                Diagnostic("Token", "End").WithLocation(6, 9),
                Diagnostic("Token", "Class").WithLocation(6, 13),
                Diagnostic("Token", "End").WithLocation(8, 1));
        }

        [Fact]
        public void DontSuppressSyntaxDiagnosticsInRootNamespaceBasic()
        {
            VerifyBasic(@"
<module: System.Diagnostics.SuppressMessage(""Test"", ""Comment"", Scope:=""Namespace"", Target:=""RootNamespace"")>
' In root namespace
",
                rootNamespace: "RootNamespace",
                analyzers: new[] { new WarningOnCommentAnalyzer() },
                diagnostics: Diagnostic("Comment", "' In root namespace").WithLocation(3, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnTypesCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
using System.Diagnostics.CodeAnalysis;

namespace N
[|{
    [SuppressMessage(""Test"", ""Token"")]
    class C<T> {}

    [SuppressMessage(""Test"", ""Token"")]
    struct S<T> {}

    [SuppressMessage(""Test"", ""Token"")]
    interface I<T>{}

    [SuppressMessage(""Test"", ""Token"")]
    enum E {}

    [SuppressMessage(""Test"", ""Token"")]
    delegate void D();
}|]
",
                Diagnostic("Token", "{").WithLocation(5, 1),
                Diagnostic("Token", "}").WithLocation(20, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnTypesBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Imports System.Diagnostics.CodeAnalysis

Namespace [|N
    <SuppressMessage(""Test"", ""Token"")>
    Module M
    End Module

    <SuppressMessage(""Test"", ""Token"")>
    Class C
    End Class

    <SuppressMessage(""Test"", ""Token"")>
    Structure S
    End Structure

    <SuppressMessage(""Test"", ""Token"")>
    Interface I
    End Interface

    <SuppressMessage(""Test"", ""Token"")>
    Enum E
        None
    End Enum

    <SuppressMessage(""Test"", ""Token"")>
    Delegate Sub D()
End|] Namespace
",
                Diagnostic("Token", "N").WithLocation(4, 11),
                Diagnostic("Token", "End").WithLocation(28, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnFieldsCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
using System.Diagnostics.CodeAnalysis;

class C
[|{
    [SuppressMessage(""Test"", ""Token"")]
    int field1 = 1, field2 = 2;

    [SuppressMessage(""Test"", ""Token"")]
    int field3 = 3;
}|]
",
                Diagnostic("Token", "{"),
                Diagnostic("Token", "}"));
        }

        [Fact]
        [WorkItem(6379, "https://github.com/dotnet/roslyn/issues/6379")]
        public void SuppressSyntaxDiagnosticsOnEnumFieldsCSharp()
        {
            VerifyCSharp(@"
// before module attributes
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"", Scope=""Member"" Target=""E.Field1"")]
// before enum
public enum E
{
    // before Field1 declaration
    Field1, // after Field1 declaration
    Field2 // after Field2 declaration
}
// after enum
",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "// before module attributes"),
                Diagnostic("Comment", "// before enum"),
                Diagnostic("Comment", "// after Field1 declaration"),
                Diagnostic("Comment", "// after Field2 declaration"),
                Diagnostic("Comment", "// after enum"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnFieldsBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <SuppressMessage(""Test"", ""Token"")>
    Public field1 As Integer = 1,
           field2 As Double = 2.0

    <SuppressMessage(""Test"", ""Token"")>
    Public field3 As Integer = 3
End|] Class
",
                Diagnostic("Token", "C"),
                Diagnostic("Token", "End"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventsCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
[|{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    public event System.Action<int> E1;

    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    public event System.Action<int> E2, E3;
}|]
",
                Diagnostic("Token", "{"),
                Diagnostic("Token", "}"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventsBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <SuppressMessage(""Test"", ""Token"")>
    Public Event E1 As System.Action(Of Integer)

    <SuppressMessage(""Test"", ""Token"")>
    Public Event E2(ByVal arg As Integer)
End|] Class
",
                Diagnostic("Token", "C"),
                Diagnostic("Token", "End"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventAddAccessorCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    public event System.Action<int> E
    [|{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        add {}
        remove|] {}
    }
}
",
                Diagnostic("Token", "{").WithLocation(5, 5),
                Diagnostic("Token", "remove").WithLocation(8, 9));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventAddAccessorBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class C
    Public Custom Event E As System.Action(Of Integer[|)
        <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
        AddHandler(value As Action(Of Integer))
        End AddHandler
        RemoveHandler|](value As Action(Of Integer))
        End RemoveHandler
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End Event
End Class
",
                Diagnostic("Token", ")"),
                Diagnostic("Token", "RemoveHandler"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventRemoveAccessorCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    public event System.Action<int> E
    {
        add {[|}
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        remove {}
    }|]
}
",
                Diagnostic("Token", "}").WithLocation(6, 14),
                Diagnostic("Token", "}").WithLocation(9, 5));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnEventRemoveAccessorBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class C
    Public Custom Event E As System.Action(Of Integer)
        AddHandler(value As Action(Of Integer))
        End [|AddHandler
        <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
        RemoveHandler(value As Action(Of Integer))
        End RemoveHandler
        RaiseEvent|](obj As Integer)
        End RaiseEvent
    End Event
End Class
",
                Diagnostic("Token", "AddHandler"),
                Diagnostic("Token", "RaiseEvent"));
        }

        [WorkItem(1103442)]
        [Fact]
        public void SuppressSyntaxDiagnosticsOnRaiseEventAccessorBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class C
    Public Custom Event E As System.Action(Of Integer)
        AddHandler(value As Action(Of Integer))
        End AddHandler
        RemoveHandler(value As Action(Of Integer))
        End [|RemoveHandler
        <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End|] Event
End Class
",
                Diagnostic("Token", "RemoveHandler"),
                Diagnostic("Token", "End"));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertyCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
using System.Diagnostics.CodeAnalysis;

class C
[|{
    [SuppressMessage(""Test"", ""Token"")]
    int Property1 { get; set; }

    [SuppressMessage(""Test"", ""Token"")]
    int Property2
    {
        get { return 2; }
        set { Property1 = 2; }
    }
}|]
",
                Diagnostic("Token", "{").WithLocation(5, 1),
                Diagnostic("Token", "}").WithLocation(15, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertyBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <SuppressMessage(""Test"", ""Token"")>
    Property Property1 As Integer

    <SuppressMessage(""Test"", ""Token"")>
    Property Property2 As Integer
        Get
            Return 2
        End Get
        Set(value As Integer)
            Property1 = value
        End Set
    End Property
End|] Class
",
                Diagnostic("Token", "C").WithLocation(4, 7),
                Diagnostic("Token", "End").WithLocation(17, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertyGetterCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    int x;
    int Property
    [|{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertyGetterBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class C
    Private x As Integer
    Property [Property] As [|Integer
        <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
        Get
            Return 2
        End Get
        Set|](value As Integer)
            x = value
        End Set
    End Property
End Class
",
                Diagnostic("Token", "Integer").WithLocation(4, 28),
                Diagnostic("Token", "Set").WithLocation(9, 9));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertySetterCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    int x;
    int Property
    [|{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnPropertySetterBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class C
    Private x As Integer
    Property [Property] As Integer
        Get
            Return 2
        End [|Get
        <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
        Set(value As Integer)
            x = value
        End Set
    End|] Property
End Class
",
                Diagnostic("Token", "Get").WithLocation(7, 13),
                Diagnostic("Token", "End").WithLocation(12, 5));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnIndexerCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    int x[|;
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    int this[int i]
    {
        get { return 2; }
        set { x = 2; }
    }
}|]
",
                Diagnostic("Token", ";").WithLocation(4, 10),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnIndexerGetterCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    int x;
    int this[int i]
    [|{
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnIndexerSetterCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
{
    int x;
    int this[int i]
    {
        get { return 2; [|}
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
        set { x = 2; }
    }|]
}
",
                Diagnostic("Token", "}").WithLocation(7, 25),
                Diagnostic("Token", "}").WithLocation(10, 5));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnMethodCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
using System.Diagnostics.CodeAnalysis;

abstract class C
[|{
    [SuppressMessage(""Test"", ""Token"")]
    public void M1<T>() {}

    [SuppressMessage(""Test"", ""Token"")]
    public abstract void M2();
}|]
",
                Diagnostic("Token", "{").WithLocation(5, 1),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnMethodBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Imports System.Diagnostics.CodeAnalysis

Public MustInherit Class [|C
    <SuppressMessage(""Test"", ""Token"")> 
    Public Function M2(Of T)() As Integer
        Return 0
    End Function 
    
    <SuppressMessage(""Test"", ""Token"")> 
    Public MustOverride Sub M3() 
End|] Class
",
                Diagnostic("Token", "C").WithLocation(4, 26),
                Diagnostic("Token", "End").WithLocation(12, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnOperatorCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
[|{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    public static C operator +(C a, C b) 
    {
        return null;
    } 
}|]
",
                Diagnostic("Token", "{").WithLocation(3, 1),
                Diagnostic("Token", "}").WithLocation(9, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnOperatorBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class [|C
    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")> 
    Public Shared Operator +(ByVal a As C, ByVal b As C) As C 
        Return Nothing
    End Operator 
End|] Class 
",
                Diagnostic("Token", "C").WithLocation(2, 7),
                Diagnostic("Token", "End").WithLocation(7, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnConstructorCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class Base
{
    public Base(int x) {}
}

class C : Base
[|{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    public C() : base(0) {} 
}|]
",
                Diagnostic("Token", "{").WithLocation(8, 1),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnConstructorBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class [|C
    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
    Public Sub New()
    End Sub
End|] Class
",
                Diagnostic("Token", "C").WithLocation(2, 7),
                Diagnostic("Token", "End").WithLocation(6, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnDestructorCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
[|{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    ~C() {}
}|]
",
                Diagnostic("Token", "{").WithLocation(3, 1),
                Diagnostic("Token", "}").WithLocation(6, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnNestedTypeCSharp()
        {
            VerifyTokenDiagnosticsCSharp(@"
class C
[|{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")]
    class D
    {
        class E
        {
        }
    }
}|]
",
                Diagnostic("Token", "{").WithLocation(3, 1),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Fact]
        public void SuppressSyntaxDiagnosticsOnNestedTypeBasic()
        {
            VerifyTokenDiagnosticsBasic(@"
Class [|C
    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"")>
    Class D
        Class E
        End Class
    End Class
End|] Class
",
                Diagnostic("Token", "C").WithLocation(2, 7),
                Diagnostic("Token", "End").WithLocation(8, 1));
        }

        #endregion

        #region Special Cases

        [Fact]
        public void SuppressMessageCompilationEnded()
        {
            VerifyCSharp(
                @"[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CompilationEnded"")]",
                new[] { new WarningOnCompilationEndedAnalyzer() });
        }

        [Fact]
        public void SuppressMessageOnPropertyAccessor()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public string P { get; private set; }
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("get_") });
        }

        [Fact]
        public void SuppressMessageOnDelegateInvoke()
        {
            VerifyCSharp(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    delegate void D();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Invoke") });
        }

        [Fact]
        public void SuppressMessageOnCodeBodyCSharp()
        {
            VerifyCSharp(
                @"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")]
    void Foo()
    {
        Foo();
    }
}
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.CSharp) });
        }

        [Fact]
        public void SuppressMessageOnCodeBodyBasic()
        {
            VerifyBasic(
                @"
Public Class C
    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")>
    Sub Foo()
        Foo()
    End Sub
End Class
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.VisualBasic) });
        }

        #endregion

        #region Attribute Decoding

        [Fact]
        public void UnnecessaryScopeAndTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(""Test"", ""Declaration"", Scope=""Type"")]
public class C1
{
}

[SuppressMessage(""Test"", ""Declaration"", Target=""C"")]
public class C2
{
}

[SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""C"")]
public class C3
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") });
        }

        [Fact]
        public void InvalidScopeOrTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Class"", Target=""C"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Class"", Target=""E"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C"));
        }

        [Fact]
        public void MissingScopeOrTarget()
        {
            VerifyCSharp(@"
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage(""Test"", ""Declaration"", Target=""C"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C"));
        }

        [Fact]
        public void InvalidAttributeConstructorParameters()
        {
            VerifyBasic(@"
Imports System.Diagnostics.CodeAnalysis

<module: SuppressMessage(UndeclaredIdentifier, ""Comment"")>
<module: SuppressMessage(""Test"", UndeclaredIdentifier)>
<module: SuppressMessage(""Test"", ""Comment"", Scope:=UndeclaredIdentifier, Target:=""C"")>
<module: SuppressMessage(""Test"", ""Comment"", Scope:=""Type"", Target:=UndeclaredIdentifier)>

Class C
End Class
",
                new[] { new WarningOnTypeDeclarationAnalyzer() },
                Diagnostic("TypeDeclaration", "C").WithLocation(9, 7));
        }

        [Fact]
        public void SuppressDuplicateAnalyzerExceptionDiagnostics()
        {
            var exceptionDiagnostics = new HashSet<Diagnostic>();

            VerifyCSharp(@"
public class C
{
}
public class C1
{
}
public class C2
{
}
",
                new[] { new ThrowExceptionForEachNamedTypeAnalyzer() },
                onAnalyzerException: (ex, a, d) => exceptionDiagnostics.Add(d));

            exceptionDiagnostics.Verify(
                Diagnostic("AD0001", null)
                    .WithArguments(
                        "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionForEachNamedTypeAnalyzer",
                        "System.Exception",
                        "ThrowExceptionAnalyzer exception")
                    .WithLocation(1, 1));
        }

        #endregion

        protected void VerifyCSharp(string source, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.CSharp, analyzers, diagnostics);
        }

        protected void VerifyCSharp(string source, DiagnosticAnalyzer[] analyzers, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.CSharp, analyzers, diagnostics, onAnalyzerException);
        }

        protected void VerifyCSharp(string source, DiagnosticAnalyzer[] analyzers, bool logAnalyzerExceptionsAsDiagnostics, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.CSharp, analyzers, diagnostics, onAnalyzerException: null, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionsAsDiagnostics);
        }

        protected void VerifyTokenDiagnosticsCSharp(string markup, params DiagnosticDescription[] diagnostics)
        {
            VerifyTokenDiagnostics(markup, LanguageNames.CSharp, diagnostics);
        }

        protected void VerifyBasic(string source, string rootNamespace, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Assert.False(string.IsNullOrWhiteSpace(rootNamespace), string.Format("Invalid root namespace '{0}'", rootNamespace));
            Verify(source, LanguageNames.VisualBasic, analyzers, diagnostics, onAnalyzerException: null, rootNamespace: rootNamespace);
        }

        protected void VerifyBasic(string source, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.VisualBasic, analyzers, diagnostics);
        }

        protected void VerifyBasic(string source, DiagnosticAnalyzer[] analyzers, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.VisualBasic, analyzers, diagnostics, onAnalyzerException);
        }

        protected void VerifyBasic(string source, DiagnosticAnalyzer[] analyzers, bool logAnalyzerExceptionAsDiagnostics, params DiagnosticDescription[] diagnostics)
        {
            Verify(source, LanguageNames.VisualBasic, analyzers, diagnostics, onAnalyzerException: null, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics);
        }

        protected void VerifyTokenDiagnosticsBasic(string markup, params DiagnosticDescription[] diagnostics)
        {
            VerifyTokenDiagnostics(markup, LanguageNames.VisualBasic, diagnostics);
        }

        protected virtual void Verify(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] diagnostics, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null, bool logAnalyzerExceptionAsDiagnostics = false, string rootNamespace = null)
        {
            Assert.True(analyzers != null && analyzers.Length > 0, "Must specify at least one diagnostic analyzer to test suppression");
            var compilation = CreateCompilation(source, language, analyzers, rootNamespace);
            compilation.VerifyAnalyzerDiagnostics(analyzers, onAnalyzerException: onAnalyzerException, logAnalyzerExceptionAsDiagnostics: logAnalyzerExceptionAsDiagnostics, expected: diagnostics);
        }

        // Generate a diagnostic on every token in the specified spans, and verify that only the specified diagnostics are not suppressed
        private void VerifyTokenDiagnostics(string markup, string language, DiagnosticDescription[] diagnostics)
        {
            string source;
            IList<TextSpan> spans;
            MarkupTestFile.GetSpans(markup, out source, out spans);
            Assert.True(spans.Count > 0, "Must specify a span within which to generate diagnostics on each token");

            Verify(source, language, new DiagnosticAnalyzer[] { new WarningOnTokenAnalyzer(spans) }, diagnostics);
        }

        private static Compilation CreateCompilation(string source, string language, DiagnosticAnalyzer[] analyzers, string rootNamespace)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";
            string projectName = "TestProject";

            var syntaxTree = language == LanguageNames.CSharp ?
                CSharpSyntaxTree.ParseText(source, path: fileName) :
                VisualBasicSyntaxTree.ParseText(source, path: fileName);

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef },
                    options: new VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        rootNamespace: rootNamespace));
            }
        }

        protected virtual bool ConsiderArgumentsForComparingDiagnostics
        {
            get
            {
                return true;
            }
        }

        protected DiagnosticDescription Diagnostic(string id, string squiggledText)
        {
            var arguments = (this.ConsiderArgumentsForComparingDiagnostics && squiggledText != null) ? new[] { squiggledText } : null;
            return new DiagnosticDescription(id, false, squiggledText, arguments, null, null, false);
        }
    }
}
