// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics;

public class UninitializedNonNullableFieldSuggestionTests : CSharpTestBase
{
    [Fact]
    public void StaticPropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public static string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,26): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public static string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 26)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void InstancePropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 19)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void InstanceReadonlyPropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public string Text { get; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public string Text { get; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 19)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void InstanceReadonlyFieldWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public readonly string Text;
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,28): warning CS8618: Non-nullable field 'Text' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.
            //     public readonly string Text;
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("field", "Text").WithLocation(3, 28)
        );
        Assert.Equal("Non-nullable field 'Text' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void EventWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           #pragma warning disable CS0067 // Event is never used
                           public class C {
                               public event System.Action E;
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (4,32): warning CS8618: Non-nullable event 'E' must contain a non-null value when exiting constructor. Consider declaring the event as nullable.
            //     public event System.Action E;
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "E").WithArguments("event", "E").WithLocation(4, 32)
        );
        Assert.Equal("Non-nullable event 'E' must contain a non-null value when exiting constructor. Consider declaring the event as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void PrivatePropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               private string Text { get; set; }
                               public C() { }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (4,12): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Text").WithLocation(4, 12)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void PublicPropertyPrivateSetWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public string Text { get; private set; }
                               public C() { }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (4,12): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Text").WithLocation(4, 12)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void OverridePropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public abstract class Base {
                               public abstract string Text { get; set; }
                           }
                           public class Derived : Base {
                               public override string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (6,28): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public override string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(6, 28)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void InternalPropertyInInternalClassWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           internal class C {
                               internal string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,21): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     internal string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 21)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void PrivateNestedClassPublicPropertyWarningMessage()
    {
        const string Src = """
                           #nullable enable
                           public class Outer {
                               private class Inner {
                                   public string Text { get; set; }
                               }
                           }
                           """;

        var comp = CreateCompilation(Src);

        comp.VerifyDiagnostics(
            // (4,23): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(4, 23)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void PrivatePropertyInPublicClassNoSuggestion()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               private string Text { get; set; }
                               public C() { }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (4,12): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public C() { }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C").WithArguments("property", "Text").WithLocation(4, 12)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void PublicPropertyInInternalClassSuggestion()
    {
        const string Src = """
                           #nullable enable
                           internal class C {
                               public string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(Src);
        comp.VerifyDiagnostics(
            // (3,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
            //     public string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 19)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }

    [Fact]
    public void StructConstructorRequiredMemberWarning()
    {
        const string Src = """
                           #nullable enable
                           public struct S {
                               public required string Text;
                               public S() { }
                           }
                           """;

        var comp = CreateCompilation(new[] { Src, RequiredMemberAttribute, CompilerFeatureRequiredAttribute });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void InstancePropertyWarningMessage_RequiredSuggestion()
    {
        const string Src = """
                           #nullable enable
                           public class C {
                               public string Text { get; set; }
                           }
                           """;
        var comp = CreateCompilation(new[] { Src, RequiredMemberAttribute, CompilerFeatureRequiredAttribute }, parseOptions: TestOptions.Regular11);
        comp.VerifyDiagnostics(
            // (3,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
            //     public string Text { get; set; }
            Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(3, 19)
        );
        Assert.Equal("Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.", comp.GetDiagnostics().Single().GetMessage());
    }
}