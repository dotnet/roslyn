// Licensed to the .NET Foundation under one or more agreements.
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

        public static IEnumerable<string[]> QualifiedAttributeNames { get; } = new[] {
            new[] { "System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" },
            new[] { "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute" },
        };

        public static IEnumerable<string[]> SimpleAttributeNames { get; } = new[] {
            new[] { "SuppressMessage" },
            new[] { "UnconditionalSuppressMessage" }
        };

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task LocalSuppressionOnType(string attrName)
        {
            await VerifyCSharpAsync(@"
[" + attrName + @"(""Test"", ""Declaration"")]
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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task MultipleLocalSuppressionsOnSingleSymbol(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[" + attrName + @"(""Test"", ""Declaration"")]
[" + attrName + @"(""Test"", ""TypeDeclaration"")]
public class C
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("C"), new WarningOnTypeDeclarationAnalyzer() });
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task DuplicateLocalSuppressions(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[" + attrName + @"(""Test"", ""Declaration"")]
[" + attrName + @"(""Test"", ""Declaration"")]
public class C
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("C") });
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task LocalSuppressionOnMember(string attrName)
        {
            await VerifyCSharpAsync(@"
public class C
{
    [" + attrName + @"(""Test"", ""Declaration"")]
    public void Goo() {}
    public void Goo1() {}
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Goo") },
                Diagnostic("Declaration", "Goo1"));
        }

        #endregion

        #region Global Suppression

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnNamespaces(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N.N1"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Namespace"", Target=""N4"")]

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

        [Theory, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnNamespaces_NamespaceAndDescendants(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N.N1"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""namespaceanddescendants"", Target=""N4"")]

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

        [Theory, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnTypesAndNamespaces_NamespaceAndDescendants(string attrName)
        {
            var source = @"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N.N1.N2"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""NamespaceAndDescendants"", Target=""N4"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""C2"")]

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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnTypes(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""Ef"")]
[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""Egg"")]
[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""Ele`2"")]

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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnNestedTypes(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""type"", Target=""C.A1"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""type"", Target=""C+A2"")]
[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""member"", Target=""C+A3"")]
[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""member"", Target=""C.A4"")]

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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task GlobalSuppressionOnBasicModule(string attrName)
        {
            await VerifyBasicAsync(@"
<assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""type"", Target=""M"")>

Module M
    Class C
    End Class
End Module
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") });
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnMembers(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M1"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Member"", Target=""C.#M3`1()"")]

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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task GlobalSuppressionOnValueTupleMemberWithDocId(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Member"", Target=""~M:C.M~System.Threading.Tasks.Task{System.ValueTuple{System.Boolean,ErrorCode}}"")]

enum ErrorCode {}

class C
{
    Task<(bool status, ErrorCode errorCode)> M() => null;
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("M") });
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task MultipleGlobalSuppressionsOnSingleSymbol(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[assembly: " + attrName + @"(""Test"", ""TypeDeclaration"", Scope=""Type"", Target=""E"")]

public class E
{
}
",
                new DiagnosticAnalyzer[] { new WarningOnNamePrefixDeclarationAnalyzer("E"), new WarningOnTypeDeclarationAnalyzer() });
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task DuplicateGlobalSuppressions(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[assembly: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]

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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task GloballySuppressSyntaxDiagnosticsCSharp(string attrName)
        {
            await VerifyCSharpAsync(@"
// before module attributes
[module: " + attrName + @"(""Test"", ""Comment"")]
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task GloballySuppressSyntaxDiagnosticsBasic(string attrName)
        {
            await VerifyBasicAsync(@"
' before module attributes
<Module: " + attrName + @"(""Test"", ""Comment"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task GloballySuppressSyntaxDiagnosticsOnTargetCSharp(string attrName)
        {
            await VerifyCSharpAsync(@"
// before module attributes
[module: " + attrName + @"(""Test"", ""Comment"", Scope=""Member"" Target=""C.Goo():System.Void"")]
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task GloballySuppressSyntaxDiagnosticsOnTargetBasic(string attrName)
        {
            await VerifyBasicAsync(@"
' before module attributes
<Module: " + attrName + @"(""Test"", ""Comment"", Scope:=""Member"", Target:=""C.Goo():System.Void"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceDeclarationCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
[assembly: " + attrName + @"(""Test"", ""Token"", Scope=""namespace"", Target=""A.B"")]
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

        [Theory, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceAndChildDeclarationCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
[assembly: " + attrName + @"(""Test"", ""Token"", Scope=""NamespaceAndDescendants"", Target=""A.B"")]
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceDeclarationBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
<assembly: " + attrName + @"(""Test"", ""Token"", Scope:=""Namespace"", Target:=""A.B"")>
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

        [Theory, WorkItem(486, "https://github.com/dotnet/roslyn/issues/486")]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNamespaceAndDescendantsDeclarationBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
<assembly: " + attrName + @"(""Test"", ""Token"", Scope:=""NamespaceAndDescendants"", Target:=""A.B"")>
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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnTypesCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

namespace N
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    class C<T> {}

    [" + attrName + @"(""Test"", ""Token"")]
    struct S<T> {}

    [" + attrName + @"(""Test"", ""Token"")]
    interface I<T>{}

    [" + attrName + @"(""Test"", ""Token"")]
    enum E {}

    [" + attrName + @"(""Test"", ""Token"")]
    delegate void D();
}|]
",
                Diagnostic("Token", "{").WithLocation(5, 1),
                Diagnostic("Token", "}").WithLocation(20, 1));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnTypesBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

Namespace [|N
    <" + attrName + @"(""Test"", ""Token"")>
    Module M
    End Module

    <" + attrName + @"(""Test"", ""Token"")>
    Class C
    End Class

    <" + attrName + @"(""Test"", ""Token"")>
    Structure S
    End Structure

    <" + attrName + @"(""Test"", ""Token"")>
    Interface I
    End Interface

    <" + attrName + @"(""Test"", ""Token"")>
    Enum E
        None
    End Enum

    <" + attrName + @"(""Test"", ""Token"")>
    Delegate Sub D()
End|] Namespace
",
                Diagnostic("Token", "N").WithLocation(4, 11),
                Diagnostic("Token", "End").WithLocation(28, 1));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnFieldsCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    int field1 = 1, field2 = 2;

    [" + attrName + @"(""Test"", ""Token"")]
    int field3 = 3;
}|]
",
                Diagnostic("Token", "{"),
                Diagnostic("Token", "}"));
        }

        [Theory]
        [WorkItem(6379, "https://github.com/dotnet/roslyn/issues/6379")]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEnumFieldsCSharp(string attrName)
        {
            await VerifyCSharpAsync(@"
// before module attributes
[module: " + attrName + @"(""Test"", ""Comment"", Scope=""Member"" Target=""E.Field1"")]
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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnFieldsBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Public field1 As Integer = 1,
           field2 As Double = 2.0

    <" + attrName + @"(""Test"", ""Token"")>
    Public field3 As Integer = 3
End|] Class
",
                Diagnostic("Token", "C"),
                Diagnostic("Token", "End"));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventsCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    public event System.Action<int> E1;

    [" + attrName + @"(""Test"", ""Token"")]
    public event System.Action<int> E2, E3;
}|]
",
                Diagnostic("Token", "{"),
                Diagnostic("Token", "}"));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventsBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Public Event E1 As System.Action(Of Integer)

    <" + attrName + @"(""Test"", ""Token"")>
    Public Event E2(ByVal arg As Integer)
End|] Class
",
                Diagnostic("Token", "C"),
                Diagnostic("Token", "End"));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventAddAccessorCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    public event System.Action<int> E
    [|{
        [" + attrName + @"(""Test"", ""Token"")]
        add {}
        remove|] {}
    }
}
",
                Diagnostic("Token", "{").WithLocation(5, 5),
                Diagnostic("Token", "remove").WithLocation(8, 9));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventAddAccessorBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class C
    Public Custom Event E As System.Action(Of Integer[|)
        <" + attrName + @"(""Test"", ""Token"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventRemoveAccessorCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    public event System.Action<int> E
    {
        add {[|}
        [" + attrName + @"(""Test"", ""Token"")]
        remove {}
    }|]
}
",
                Diagnostic("Token", "}").WithLocation(6, 14),
                Diagnostic("Token", "}").WithLocation(9, 5));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnEventRemoveAccessorBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class C
    Public Custom Event E As System.Action(Of Integer)
        AddHandler(value As Action(Of Integer))
        End [|AddHandler
        <" + attrName + @"(""Test"", ""Token"")>
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
        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnRaiseEventAccessorBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class C
    Public Custom Event E As System.Action(Of Integer)
        AddHandler(value As Action(Of Integer))
        End AddHandler
        RemoveHandler(value As Action(Of Integer))
        End [|RemoveHandler
        <" + attrName + @"(""Test"", ""Token"")>
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End|] Event
End Class
",
                Diagnostic("Token", "RemoveHandler"),
                Diagnostic("Token", "End"));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertyCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    int Property1 { get; set; }

    [" + attrName + @"(""Test"", ""Token"")]
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

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertyBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Property Property1 As Integer

    <" + attrName + @"(""Test"", ""Token"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertyGetterCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    int x;
    int Property
    [|{
        [" + attrName + @"(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertyGetterBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class C
    Private x As Integer
    Property [Property] As [|Integer
        <" + attrName + @"(""Test"", ""Token"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertySetterCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    int x;
    int Property
    [|{
        [" + attrName + @"(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnPropertySetterBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class C
    Private x As Integer
    Property [Property] As Integer
        Get
            Return 2
        End [|Get
        <" + attrName + @"(""Test"", ""Token"")>
        Set(value As Integer)
            x = value
        End Set
    End|] Property
End Class
",
                Diagnostic("Token", "Get").WithLocation(7, 13),
                Diagnostic("Token", "End").WithLocation(12, 5));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnIndexerCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    int x[|;
    [" + attrName + @"(""Test"", ""Token"")]
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnIndexerGetterCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    int x;
    int this[int i]
    [|{
        [" + attrName + @"(""Test"", ""Token"")]
        get { return 2; }
        set|] { x = 2; }
    }
}
",
                Diagnostic("Token", "{").WithLocation(6, 5),
                Diagnostic("Token", "set").WithLocation(9, 9));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnIndexerSetterCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
{
    int x;
    int this[int i]
    {
        get { return 2; [|}
        [" + attrName + @"(""Test"", ""Token"")]
        set { x = 2; }
    }|]
}
",
                Diagnostic("Token", "}").WithLocation(7, 25),
                Diagnostic("Token", "}").WithLocation(10, 5));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnMethodCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

abstract class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    public void M1<T>() {}

    [" + attrName + @"(""Test"", ""Token"")]
    public abstract void M2();
}|]
",
                Diagnostic("Token", "{").WithLocation(5, 1),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnMethodBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

Public MustInherit Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Public Function M2(Of T)() As Integer
        Return 0
    End Function

    <" + attrName + @"(""Test"", ""Token"")>
    Public MustOverride Sub M3()
End|] Class
",
                Diagnostic("Token", "C").WithLocation(4, 26),
                Diagnostic("Token", "End").WithLocation(12, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnOperatorCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    public static C operator +(C a, C b)
    {
        return null;
    }
}|]
",
                Diagnostic("Token", "{").WithLocation(3, 1),
                Diagnostic("Token", "}").WithLocation(9, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnOperatorBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Public Shared Operator +(ByVal a As C, ByVal b As C) As C
        Return Nothing
    End Operator
End|] Class
",
                Diagnostic("Token", "C").WithLocation(2, 7),
                Diagnostic("Token", "End").WithLocation(7, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnConstructorCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class Base
{
    public Base(int x) {}
}

class C : Base
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    public C() : base(0) {}
}|]
",
                Diagnostic("Token", "{").WithLocation(8, 1),
                Diagnostic("Token", "}").WithLocation(11, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnConstructorBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
    Public Sub New()
    End Sub
End|] Class
",
                Diagnostic("Token", "C").WithLocation(2, 7),
                Diagnostic("Token", "End").WithLocation(6, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnDestructorCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
    ~C() {}
}|]
",
                Diagnostic("Token", "{").WithLocation(3, 1),
                Diagnostic("Token", "}").WithLocation(6, 1));
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNestedTypeCSharp(string attrName)
        {
            await VerifyTokenDiagnosticsCSharpAsync(@"
class C
[|{
    [" + attrName + @"(""Test"", ""Token"")]
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressSyntaxDiagnosticsOnNestedTypeBasic(string attrName)
        {
            await VerifyTokenDiagnosticsBasicAsync(@"
Class [|C
    <" + attrName + @"(""Test"", ""Token"")>
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

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressMessageCompilationEnded(string attrName)
        {
            await VerifyCSharpAsync(
                @"[module: " + attrName + @"(""Test"", ""CompilationEnded"")]",
                new[] { new WarningOnCompilationEndedAnalyzer() });
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressMessageOnPropertyAccessor(string attrName)
        {
            await VerifyCSharpAsync(@"
public class C
{
    [" + attrName + @"(""Test"", ""Declaration"")]
    public string P { get; private set; }
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("get_") });
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressMessageOnDelegateInvoke(string attrName)
        {
            await VerifyCSharpAsync(@"
public class C
{
    [" + attrName + @"(""Test"", ""Declaration"")]
    delegate void D();
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("Invoke") });
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressMessageOnCodeBodyCSharp(string attrName)
        {
            await VerifyCSharpAsync(
                @"
public class C
{
    [" + attrName + @"(""Test"", ""CodeBody"")]
    void Goo()
    {
        Goo();
    }
}
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.CSharp) });
        }

        [Theory]
        [MemberData(nameof(QualifiedAttributeNames))]
        public async Task SuppressMessageOnCodeBodyBasic(string attrName)
        {
            await VerifyBasicAsync(
                @"
Public Class C
    <" + attrName + @"(""Test"", ""CodeBody"")>
    Sub Goo()
        Goo()
    End Sub
End Class
",
                new[] { new WarningOnCodeBodyAnalyzer(LanguageNames.VisualBasic) });
        }

        #endregion

        #region Attribute Decoding

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task UnnecessaryScopeAndTarget(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[" + attrName + @"(""Test"", ""Declaration"", Scope=""Type"")]
public class C1
{
}

[" + attrName + @"(""Test"", ""Declaration"", Target=""C"")]
public class C2
{
}

[" + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""C"")]
public class C3
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") });
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task InvalidScopeOrTarget(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Class"", Target=""C"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"", Target=""E"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Class"", Target=""E"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C"));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task MissingScopeOrTarget(string attrName)
        {
            await VerifyCSharpAsync(@"
using System.Diagnostics.CodeAnalysis;

[module: " + attrName + @"(""Test"", ""Declaration"", Target=""C"")]
[module: " + attrName + @"(""Test"", ""Declaration"", Scope=""Type"")]

public class C
{
}
",
                new[] { new WarningOnNamePrefixDeclarationAnalyzer("C") },
                Diagnostic("Declaration", "C"));
        }

        [Theory]
        [MemberData(nameof(SimpleAttributeNames))]
        public async Task InvalidAttributeConstructorParameters(string attrName)
        {
            await VerifyBasicAsync(@"
Imports System.Diagnostics.CodeAnalysis

<module: " + attrName + @"UndeclaredIdentifier, ""Comment"")>
<module: " + attrName + @"(""Test"", UndeclaredIdentifier)>
<module: " + attrName + @"(""Test"", ""Comment"", Scope:=UndeclaredIdentifier, Target:=""C"")>
<module: " + attrName + @"(""Test"", ""Comment"", Scope:=""Type"", Target:=UndeclaredIdentifier)>

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
