﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public abstract partial class SuppressMessageAttributeTests
    {
        #region Local Suppression

        [Fact]
        public async Task LocalSuppressionOnType()
        {
            await VerifyCSharpAsync(@"
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
        public async Task MultipleLocalSuppressionsOnSingleSymbol()
        {
            await VerifyCSharpAsync(@"
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
        public async Task DuplicateLocalSuppressions()
        {
            await VerifyCSharpAsync(@"
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
        public async Task LocalSuppressionOnMember()
        {
            await VerifyCSharpAsync(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public void Goo() {}
    public void Goo1() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Goo") },
                Diagnostic("Declaration", "Goo1"));
        }

        #endregion

        #region Global Suppression

        [Fact]
        public async Task GlobalSuppressionOnNamespaces()
        {
            await VerifyCSharpAsync(@"
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

        [Fact, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        public async Task GlobalSuppressionOnNamespaces_NamespaceAndDescendants()
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N.N1"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""namespaceanddescendants"", Target=""N4"")]

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
    namespace N5
    {
    }
}

namespace N.N1.N6.N7
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("N") },
                Diagnostic("Declaration", "N"));
        }

        [Fact, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        public async Task GlobalSuppressionOnTypesAndNamespaces_NamespaceAndDescendants()
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N.N1.N2"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N4"")]
[module: SuppressMessage(""Test"", ""Declaration"", Scope=""Type"", Target=""C2"")]

namespace N
{
    namespace N1
    {
        class C1
        {
        }

        namespace N2.N3
        {
            class C2
            {
            }

            class C3
            {
                class C4
                {
                }
            }
        }
    }
}

namespace N4
{
    namespace N5
    {
        class C5
        {
        }
    }

    class C6
    {
    }
}

namespace N.N1.N2.N7
{
    class C7
    {
    }
}
";

            await VerifyCSharpAsync(source,
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("N") },
                Diagnostic("Declaration", "N"),
                Diagnostic("Declaration", "N1"));

            await VerifyCSharpAsync(source,
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C1"));
        }

        [Fact]
        public async Task GlobalSuppressionOnTypes()
        {
            await VerifyCSharpAsync(@"
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
        public async Task GlobalSuppressionOnNestedTypes()
        {
            await VerifyCSharpAsync(@"
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
        public async Task GlobalSuppressionOnBasicModule()
        {
            await VerifyBasicAsync(@"
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"", Scope=""type"", Target=""M"")>

Module M
    Class C
    End Class
End Module
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") });
        }

        [Fact]
        public async Task GlobalSuppressionOnMembers()
        {
            await VerifyCSharpAsync(@"
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
        public async Task GlobalSuppressionOnValueTupleMemberWithDocId()
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

[assembly: SuppressMessage(""Test"", ""Declaration"", Scope=""Member"", Target=""~M:C.M~System.Threading.Tasks.Task{System.ValueTuple{System.Boolean,ErrorCode}}"")]

enum ErrorCode {}

class C
{
    Task<(bool status, ErrorCode errorCode)> M() => null;
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") });
        }

        [Fact]
        public async Task MultipleGlobalSuppressionsOnSingleSymbol()
        {
            await VerifyCSharpAsync(@"
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
        public async Task DuplicateGlobalSuppressions()
        {
            await VerifyCSharpAsync(@"
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
        public async Task WarningOnCommentAnalyzerCSharp()
        {
            await VerifyCSharpAsync("// Comment\r\n /* Comment */",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "// Comment"),
                Diagnostic("Comment", "/* Comment */"));
        }

        [Fact]
        public async Task WarningOnCommentAnalyzerBasic()
        {
            await VerifyBasicAsync("' Comment",
                new[] { new WarningOnCommentAnalyzer() },
                Diagnostic("Comment", "' Comment"));
        }

        [Fact]
        public async Task GloballySuppressSyntaxDiagnosticsCSharp()
        {
            await VerifyCSharpAsync(@"
// before module attributes
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"")]
// before class
public class C
{
    // before method
    public void Goo() // after method declaration
    {
        // inside method
    }
}
// after class
",
                new[] { new WarningOnCommentAnalyzer() });
        }

        [Fact]
        public async Task GloballySuppressSyntaxDiagnosticsBasic()
        {
            await VerifyBasicAsync(@"
' before module attributes
<Module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"")>
' before class
Public Class C
    ' before sub
    Public Sub Goo() ' after sub statement
        ' inside sub
    End Sub
End Class
' after class
",
                new[] { new WarningOnCommentAnalyzer() });
        }

        [Fact]
        public async Task GloballySuppressSyntaxDiagnosticsOnTargetCSharp()
        {
            await VerifyCSharpAsync(@"
// before module attributes
[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"", Scope=""Member"" Target=""C.Goo():System.Void"")]
// before class
public class C
{
    // before method
    public void Goo() // after method declaration
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
        public async Task GloballySuppressSyntaxDiagnosticsOnTargetBasic()
        {
            await VerifyBasicAsync(@"
' before module attributes
<Module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Comment"", Scope:=""Member"", Target:=""C.Goo():System.Void"")>
' before class
Public Class C
    ' before sub
    Public Sub Goo() ' after sub statement
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
        public async Task SuppressSyntaxDiagnosticsOnNamespaceDeclarationCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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

        [Fact, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceAndChildDeclarationCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"", Scope=""NamespaceAndDescendants"", Target=""A.B"")]
namespace A
[|{
    namespace B
    {
        class C {}
    }
}|]
",
                Diagnostic("Token", "{").WithLocation(4, 1),
                Diagnostic("Token", "}").WithLocation(9, 1));
        }

        [Fact]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceDeclarationBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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

        [Fact, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceAndDescendantsDeclarationBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
<assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Token"", Scope:=""NamespaceAndDescendants"", Target:=""A.B"")>
Namespace [|A
    Namespace B 
        Class C
        End Class
    End Namespace
End|] Namespace
",
                Diagnostic("Token", "A").WithLocation(3, 11),
                Diagnostic("Token", "End").WithLocation(8, 1));
        }

        [Theory, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        [InlineData("Namespace")]
        [InlineData("NamespaceAndDescendants")]
        public async Task DontSuppressSyntaxDiagnosticsInRootNamespaceBasic(string scope)
        {
            await VerifyBasicAsync($@"
<module: System.Diagnostics.SuppressMessage(""Test"", ""Comment"", Scope:=""{scope}"", Target:=""RootNamespace"")>
' In root namespace
",
                rootNamespace: "RootNamespace",
                analyzers: new[] { new WarningOnCommentAnalyzer() },
                diagnostics: Diagnostic("Comment", "' In root namespace").WithLocation(3, 1));
        }

        [Fact]
        public async Task SuppressSyntaxDiagnosticsOnTypesCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnTypesBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnFieldsCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEnumFieldsCSharp()
        {
            await VerifyCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnFieldsBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventsCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventsBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventAddAccessorCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventAddAccessorBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventRemoveAccessorCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnEventRemoveAccessorBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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

        [WorkItem(1103442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1103442")]
        [Fact]
        public async Task SuppressSyntaxDiagnosticsOnRaiseEventAccessorBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertyCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertyBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertyGetterCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertyGetterBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertySetterCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnPropertySetterBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnIndexerCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnIndexerGetterCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnIndexerSetterCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnMethodCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnMethodBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnOperatorCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnOperatorBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnConstructorCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnConstructorBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnDestructorCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnNestedTypeCSharp()
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
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
        public async Task SuppressSyntaxDiagnosticsOnNestedTypeBasic()
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
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
        public async Task SuppressMessageCompilationEnded()
        {
            await VerifyCSharpAsync(
                @"[module: System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CompilationEnded"")]",
                new[] { new WarningOnCompilationEndedAnalyzer() });
        }

        [Fact]
        public async Task SuppressMessageOnPropertyAccessor()
        {
            await VerifyCSharpAsync(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    public string P { get; private set; }
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("get_") });
        }

        [Fact]
        public async Task SuppressMessageOnDelegateInvoke()
        {
            await VerifyCSharpAsync(@"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""Declaration"")]
    delegate void D();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Invoke") });
        }

        [Fact]
        public async Task SuppressMessageOnCodeBodyCSharp()
        {
            await VerifyCSharpAsync(
                @"
public class C
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")]
    void Goo()
    {
        Goo();
    }
}
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.CSharp) });
        }

        [Fact]
        public async Task SuppressMessageOnCodeBodyBasic()
        {
            await VerifyBasicAsync(
                @"
Public Class C
    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Test"", ""CodeBody"")>
    Sub Goo()
        Goo()
    End Sub
End Class
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.VisualBasic) });
        }

        #endregion

        #region Attribute Decoding

        [Fact]
        public async Task UnnecessaryScopeAndTarget()
        {
            await VerifyCSharpAsync(@"
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
        public async Task InvalidScopeOrTarget()
        {
            await VerifyCSharpAsync(@"
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
        public async Task MissingScopeOrTarget()
        {
            await VerifyCSharpAsync(@"
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
        public async Task InvalidAttributeConstructorParameters()
        {
            await VerifyBasicAsync(@"
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

        #endregion

        protected async Task VerifyCSharpAsync(string source, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            await VerifyAsync(source, LanguageNames.CSharp, analyzers, diagnostics);
        }

        protected Task VerifyTokenDiagnosticsCSharpAsync(string markup, params DiagnosticDescription[] diagnostics)
        {
            return VerifyTokenDiagnosticsAsync(markup, LanguageNames.CSharp, diagnostics);
        }

        protected async Task VerifyBasicAsync(string source, string rootNamespace, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            Assert.False(string.IsNullOrWhiteSpace(rootNamespace), string.Format("Invalid root namespace '{0}'", rootNamespace));
            await VerifyAsync(source, LanguageNames.VisualBasic, analyzers, diagnostics, rootNamespace: rootNamespace);
        }

        protected async Task VerifyBasicAsync(string source, DiagnosticAnalyzer[] analyzers, params DiagnosticDescription[] diagnostics)
        {
            await VerifyAsync(source, LanguageNames.VisualBasic, analyzers, diagnostics);
        }

        protected Task VerifyTokenDiagnosticsBasicAsync(string markup, params DiagnosticDescription[] diagnostics)
        {
            return VerifyTokenDiagnosticsAsync(markup, LanguageNames.VisualBasic, diagnostics);
        }

        protected abstract Task VerifyAsync(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] diagnostics, string rootNamespace = null);

        // Generate a diagnostic on every token in the specified spans, and verify that only the specified diagnostics are not suppressed
        private Task VerifyTokenDiagnosticsAsync(string markup, string language, DiagnosticDescription[] diagnostics)
        {
            MarkupTestFile.GetSpans(markup, out var source, out ImmutableArray<TextSpan> spans);
            Assert.True(spans.Length > 0, "Must specify a span within which to generate diagnostics on each token");

            return VerifyAsync(source, language, new DiagnosticAnalyzer[] { new WarningOnTokenAnalyzer(spans) }, diagnostics);
        }

        protected abstract bool ConsiderArgumentsForComparingDiagnostics { get; }

        protected DiagnosticDescription Diagnostic(string id, string squiggledText)
        {
            var arguments = this.ConsiderArgumentsForComparingDiagnostics && squiggledText != null
                ? new[] { squiggledText }
                : null;
            return new DiagnosticDescription(id, false, squiggledText, arguments, null, null, false);
        }
    }
}
