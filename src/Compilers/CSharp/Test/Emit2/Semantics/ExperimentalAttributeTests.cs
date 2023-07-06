// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// These tests cover the handling of System.Diagnostics.CodeAnalysis.ExperimentalAttribute.
public class ExperimentalAttributeTests : CSharpTestBase
{
    private const string experimentalAttributeSrc = """
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string? UrlFormat { get; set; }
    }
}
""";

    private const string DefaultHelpLinkUri = "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS8305)";

    [Theory, CombinatorialData]
    public void Simple(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void OnAssembly()
    {
        // Ignored on assemblies
        var libSrc = """
[assembly: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void OnModule()
    {
        // Ignored on modules
        var libSrc = """
[module: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnStruct(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public struct S
{
    public static void M() { }
}
""";

        var src = """
S.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'S' is for evaluation purposes only and is subject to change or removal in future updates.
            // S.M();
            Diagnostic("DiagID1", "S").WithArguments("S").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnEnum(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public enum E { }
""";

        var src = """
E e = default;
e.ToString();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning DiagID1: 'E' is for evaluation purposes only and is subject to change or removal in future updates.
            // E e = default;
            Diagnostic("DiagID1", "E").WithArguments("E").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnConstructor(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public C() { }
}
""";

        var src = """
_ = new C();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,5): warning DiagID1: 'C.C()' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = new C();
            Diagnostic("DiagID1", "new C()").WithArguments("C.C()").WithLocation(1, 5)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnMethod(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnProperty(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int P => 0;
}
""";

        var src = """
_ = C.P;
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,5): warning DiagID1: 'C.P' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = C.P;
            Diagnostic("DiagID1", "C.P").WithArguments("C.P").WithLocation(1, 5)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void OnField(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int field = 0;
}
""";

        var src = """
_ = C.field;
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,5): warning DiagID1: 'C.field' is for evaluation purposes only and is subject to change or removal in future updates.
            // _ = C.field;
            Diagnostic("DiagID1", "C.field").WithArguments("C.field").WithLocation(1, 5)
            );
    }

    [Theory, CombinatorialData]
    public void OnEvent(bool inSource)
    {
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static event System.Action Event;

    static void M()
    {
        Event();
    }
}
""";

        var src = """
C.Event += () => { };
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'C.Event' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.Event += () => { };
            Diagnostic("DiagID1", "C.Event").WithArguments("C.Event").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void OnInterface(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public interface I
{
    void M();
}
""";

        var src = """
I i = null;
i.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'I' is for evaluation purposes only and is subject to change or removal in future updates.
            // I i = null;
            Diagnostic("DiagID1", "I").WithArguments("I").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void OnDelegate(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public delegate void D();
""";

        var src = """
D d = null;
d();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'D' is for evaluation purposes only and is subject to change or removal in future updates.
            // D d = null;
            Diagnostic("DiagID1", "D").WithArguments("D").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void OnParameter(bool inSource)
    {
        // Ignored on parameters
        var libSrc = """
public class C
{
    public static void M([System.Diagnostics.CodeAnalysis.Experimental("DiagID1")] int i) { }
}
""";

        var src = """
C.M(42);
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnReturnValue(bool inSource)
    {
        // Ignored on return value
        var libSrc = """
public class C
{
    [return: System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static int M() => 0;
}
""";

        var src = """
_ = C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OnTypeParameter(bool inSource)
    {
        // Ignored on type parameters
        var libSrc = """
public class C<[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")] T> { }
""";

        var src = """
C<int> c = null;
c.ToString();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void NullDiagnosticId(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental(null)]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning CS8305: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void DiagnosticIdWithTrailingNewline(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("Diag\n")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning Diag
            // : 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag\n", "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void DiagnosticIdWithNewline(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("Diag\n01")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";
        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning Diag
            // 01: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag\n01", "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void WhitespaceDiagnosticId(bool inSource,
        [CombinatorialValues("\"\"", "\" \"", "\"\\n\"")] string whitespace)
    {
        var libSrc = $$"""
[System.Diagnostics.CodeAnalysis.Experimental({{whitespace}})]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new CSharpTestSource[] { (src, "0.cs"), libSrc, experimentalAttributeSrc })
            : CreateCompilation((src, "0.cs"), references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning CS8305: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Fact]
    public void SpacedDiagnosticId()
    {
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("Diag 01")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning Diag 01: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("Diag 01", "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Fact]
    public void BadAttribute_IntParameter()
    {
        // In source, if the attribute is improperly declared, but with the right number of parameters, we still recognize it
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental(42)]
class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(int diagnosticId)
        {
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics(
            // (1,1): warning CS8305: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic(ErrorCode.WRN_Experimental, "C").WithArguments("C").WithLocation(1, 1)
            );
    }

    [Fact]
    public void BadAttribute_IntParameter_Metadata()
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental(42)]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(int diagnosticId)
        {
        }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BadAttribute_TwoStringParameters()
    {
        // If the attribute is improperly declared, with a wrong number of parameters, we ignore it
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("ignored", "ignored")]
class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId, string urlFormat)
        {
        }
    }
}
""";
        var comp = CreateCompilation(src);
        comp.VerifyDiagnostics();
    }

    [Fact]
    public void BadAttribute_IntUrlFormatProperty_Metadata()
    {
        // A "UrlFormat" property with a type other than 'string' is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", UrlFormat = 42)]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public int UrlFormat { get; set; }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): warning DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadAttribute_UrlFormatField_Metadata()
    {
        // A field named "UrlFormat" is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", UrlFormat = "hello")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public string UrlFormat = "hello";
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): warning DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadAttribute_OtherProperty_Metadata()
    {
        // A property that isn't named "UrlFormat" is ignored
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID", NotUrlFormat = "hello")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
        public string NotUrlFormat { get; set; }
    }
}
""";

        var libComp = CreateCompilation(libSrc);

        var src = """
C.M();
""";

        var comp = CreateCompilation(src, references: new[] { libComp.EmitToImageReference() });
        comp.VerifyDiagnostics(
            // (1,1): warning DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void UrlFormat()
    {
        // Combine the DiagnosticId with the UrlFormat if present
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "https://example.org/{0}")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates. (https://example.org/DiagID1)
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal("https://example.org/DiagID1", diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void BadUrlFormat()
    {
        // We use a default help URL if the UrlFormat is improper
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "https://example.org/{0}{1}")]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Theory, CombinatorialData]
    public void EmptyUrlFormat(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = "")]
public class C
{
    public static void M() { }
}
""";

        var src = """
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (1,1): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal("", diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void NullUrlFormat()
    {
        // We use a default help URL if the UrlFormat is improper
        var src = """
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", UrlFormat = null)]
class C
{
    public static void M() { }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
            // C.M();
            Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(1, 1)
            );

        var diag = comp.GetDiagnostics().Single();
        Assert.Equal("DiagID1", diag.Id);
        Assert.Equal(ErrorCode.WRN_Experimental, (ErrorCode)diag.Code);
        Assert.Equal(DefaultHelpLinkUri, diag.Descriptor.HelpLinkUri);
    }

    [Fact]
    public void FullyQualified()
    {
        var src = """
N.C.M();

namespace N
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    class C
    {
        public static void M() { }
    }
}
""";
        var comp = CreateCompilation(new[] { src, experimentalAttributeSrc });
        comp.VerifyDiagnostics(
            // 0.cs(1,1): warning DiagID1: 'N.C' is for evaluation purposes only and is subject to change or removal in future updates.
            // N.C.M();
            Diagnostic("DiagID1", "N.C").WithArguments("N.C").WithLocation(1, 1)
            );
    }

    [Theory, CombinatorialData]
    public void Suppressed(bool inSource)
    {
        var libSrc = """
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

        var src = """
#pragma warning disable DiagID1
C.M();
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void InObsoleteMethod(bool inSource)
    {
        // Diagnostics for [Experimental] are not suppressed in [Obsolete] members
        var libSrc = """
public class C
{
    [System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
    public static void M() { }
}
""";

        var src = """
class D
{
    [System.Obsolete("obsolete", true)]
    void M2()
    {
        C.M();
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        comp.VerifyDiagnostics(
            // (6,9): warning DiagID1: 'C.M()' is for evaluation purposes only and is subject to change or removal in future updates.
            //         C.M();
            Diagnostic("DiagID1", "C.M()").WithArguments("C.M()").WithLocation(6, 9)
            );
    }

    [Theory, CombinatorialData]
    public void WithObsolete(bool inSource)
    {
        var libSrc = """
[System.Obsolete("error", true)]
[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
}
""";

        var src = """
class D
{
    void M(C c)
    {
    }
}
""";

        var comp = inSource
            ? CreateCompilation(new[] { src, libSrc, experimentalAttributeSrc })
            : CreateCompilation(src, references: new[] { CreateCompilation(new[] { libSrc, experimentalAttributeSrc }).EmitToImageReference() });

        if (inSource)
        {
            comp.VerifyDiagnostics(
                // 0.cs(3,12): warning DiagID1: 'C' is for evaluation purposes only and is subject to change or removal in future updates.
                //     void M(C c)
                Diagnostic("DiagID1", "C").WithArguments("C").WithLocation(3, 12)
                );
        }
        else
        {
            comp.VerifyDiagnostics(
                // (3,12): error CS0619: 'C' is obsolete: 'error'
                //     void M(C c)
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "error").WithLocation(3, 12)
                );
        }
    }
}
