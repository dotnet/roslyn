// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if !NET472
#pragma warning disable IDE0055 // Fix formatting
#endif

#if NET472

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Xunit;
using MemoryStream = System.IO.MemoryStream;
using System;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class NoPiaEmbedTypes : EmitMetadataTestBase
    {
        [Fact]
        public void EmbedClass1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public class Test
{ }
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
        Test x = null;
        System.Action<Test> y = null;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            DiagnosticDescription[] expected = {
                // (6,14): error CS1752: Interop type 'Test' cannot be embedded. Use the applicable interface instead.
                //         Test x = null;
    Diagnostic(ErrorCode.ERR_NewCoClassOnLink, "Test").WithArguments("Test"),
                // (7,29): error CS1752: Interop type 'Test' cannot be embedded. Use the applicable interface instead.
                //         System.Action<Test> y = null;
    Diagnostic(ErrorCode.ERR_NewCoClassOnLink, "Test").WithArguments("Test")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedClass2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public class Test
{ }
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
        System.Action<Test> y = null;
        System.Console.WriteLine(y);
    }
}";

            DiagnosticDescription[] expected = {
                // (6,29): error CS1752: Interop type 'Test' cannot be embedded. Use the applicable interface instead.
                //         System.Action<Test> y = null;
                Diagnostic(ErrorCode.ERR_NewCoClassOnLink, "Test").WithArguments("Test")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        // In VB use AssertTheseErrors format for expected diagnostics!
        private static void VerifyEmitDiagnostics(
            CSharpCompilation compilation,
            bool metadataOnlyShouldSucceed,
            DiagnosticDescription[] expectedFullBuildDiagnostics,
            DiagnosticDescription[] expectedMetadataOnlyDiagnostics = null)
        {
            using (var executableStream = new MemoryStream())
            {
                var result = compilation.Emit(executableStream);
                Assert.False(result.Success);
                result.Diagnostics.Verify(expectedFullBuildDiagnostics);
            }

            using (var executableStream = new MemoryStream())
            {
                var result = compilation.Emit(executableStream, options: new EmitOptions(metadataOnly: true));

                if (metadataOnlyShouldSucceed)
                {
                    Assert.True(result.Success);
                    result.Diagnostics.Verify();
                }
                else
                {
                    Assert.False(result.Success);
                    result.Diagnostics.Verify(expectedMetadataOnlyDiagnostics ?? expectedFullBuildDiagnostics);
                }
            }
        }

        [Fact]
        public void EmbedClass3()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public class Test
{ }
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    class test2 : Test
    {}
}";

            DiagnosticDescription[] expected = {
                // (8,19): error CS1752: Interop type 'Test' cannot be embedded. Use the applicable interface instead.
                //     class test2 : Test
                Diagnostic(ErrorCode.ERR_NewCoClassOnLink, "Test").WithArguments("Test")
            };

            var compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);
        }

        [Fact]
        public void EmbedNestedType1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest20
{
    Test21.Test22 M22();
}

public struct Test21
{
    public struct Test22
    {
    }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M13(ITest20 x)
    {
        Test21.Test22 y = x.M22();
        return y;
    }

    public object M14(ITest20 x)
    {
        var y = x.M22();
        return y;
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1754: Type 'Test21.Test22' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         Test21.Test22 y = x.M22();
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "Test22").WithArguments("Test21.Test22"),
                // (16,13): error CS1754: Type 'Test21.Test22' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         var y = x.M22();
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "y = x.M22()").WithArguments("Test21.Test22")
            };

            var compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedNestedType2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest20
{
    Test21.Test22 M22();
}

public struct Test21
{
    public struct Test22
    {
    }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
    class UsePia
    {
        public static void Main()
        {
        }

        public object M14(ITest20 x)
        {
            var y = x.M22();
            return y;
        }
    }";

            DiagnosticDescription[] expected = {
                // (10,13): error CS1754: Type 'Test21.Test22' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //         var y = x.M22();
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "y = x.M22()").WithArguments("Test21.Test22")
            };

            var compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedNestedType3()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
    public struct Test22
    {
    }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M14(Test21.Test22 x)
    {
    }
}";

            DiagnosticDescription[] expected = {
                // (8,28): error CS1754: Type 'Test21.Test22' cannot be embedded because it is a nested type. Consider setting the 'Embed Interop Types' property to false.
                //     public void M14(Test21.Test22 x)
                Diagnostic(ErrorCode.ERR_NoPIANestedType, "Test22").WithArguments("Test21.Test22")
            };

            var compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);
        }

        [Fact]
        public void EmbedGenericType1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest20<T>
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M13(ITest20<int> x)
    {
    }
}";

            DiagnosticDescription[] expected = {
                // (8,21): error CS1768: Type 'ITest20<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public void M13(ITest20<int> x)
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "ITest20<int>").WithArguments("ITest20<T>"),
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, false, expected);
        }

        [Fact]
        public void EmbedGenericType2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21<T>
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21<int>);
    }
}";

            DiagnosticDescription[] expected = {
                // (14,24): error CS1768: Type 'Test21<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //         return default(Test21<int>);
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Test21<int>").WithArguments("Test21<T>")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedStructWithPrivateField()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
   private int x;
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1757: Embedded interop struct 'Test21' can contain only public instance fields.
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "default(Test21)").WithArguments("Test21")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedStructWithStaticField()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
   static int x;
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1757: Embedded interop struct 'Test21' can contain only public instance fields.
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "default(Test21)").WithArguments("Test21")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedStructWithMethod()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
   public void M(){}
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1757: Embedded interop struct 'Test21' can contain only public instance fields.
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "default(Test21)").WithArguments("Test21")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedStructWithProperty()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
    int P4 { get {return 0;}  }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1757: Embedded interop struct 'Test21' can contain only public instance fields.
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "default(Test21)").WithArguments("Test21")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void EmbedStructWithEvent()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
    event System.Action E5;
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS1757: Embedded interop struct 'Test21' can contain only public instance fields.
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_InteropStructContainsMethods, "default(Test21)").WithArguments("Test21")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void CS1774ERR_InteropMethodWithBody()
        {
            string sources1 =
@".assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
.assembly A
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = {string('_.dll')}
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = {string('f9c2d51d-4f44-45f0-9eda-c9d599b58257')}
}
.class public sealed D extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor(object o, native int m) runtime { }
  .method public hidebysig instance void Invoke() runtime { }
  .method public hidebysig instance class [mscorlib]System.IAsyncResult BeginInvoke(class [mscorlib]System.AsyncCallback c, object o) runtime { }
  .method public hidebysig instance void EndInvoke(class [mscorlib]System.IAsyncResult r) runtime { }
  .method public static void M1() { ldnull throw }
  .method public static pinvokeimpl(""A.dll"" winapi) void M2() { }
  .method public instance void M3() { ldnull throw }
}";
            string sources2 =
@"class C
{
    static void M(D d)
    {
        D.M1();
        D.M2();
        d.M3();
    }
}";
            DiagnosticDescription[] expected =
            {
                // (5,9): error CS1774: Embedded interop method 'void D.M1()' contains a body.
                //         D.M1();
                Diagnostic(ErrorCode.ERR_InteropMethodWithBody, "D.M1()").WithArguments("void D.M1()"),
                // (5,9): error CS1774: Embedded interop method 'void D.M3()' contains a body.
                //         D.M1();
                Diagnostic(ErrorCode.ERR_InteropMethodWithBody, "D.M1()").WithArguments("void D.M3()")
            };
            DiagnosticDescription[] expectedMetadataOnly =
            {
                // (5,9): error CS1774: Embedded interop method 'void D.M1()' contains a body.
                Diagnostic(ErrorCode.ERR_InteropMethodWithBody).WithArguments("void D.M1()"),
                // (5,9): error CS1774: Embedded interop method 'void D.M3()' contains a body.
                Diagnostic(ErrorCode.ERR_InteropMethodWithBody).WithArguments("void D.M3()")
            };
            var reference1 = CompileIL(sources1, prependDefaultHeader: false, embedInteropTypes: true);
            var compilation2 = CreateCompilation(sources2, references: new MetadataReference[] { reference1 });
            VerifyEmitDiagnostics(compilation2, false, expected, expectedMetadataOnly);
        }

        [Fact]
        public void TypeIdentifierIsMissing1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test21
{
}
";

            var piaCompilation = CreateEmptyCompilation(pia, new MetadataReference[] { MscorlibRef_v20 }, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M14()
    {
        return default(Test21);
    }
}";

            DiagnosticDescription[] expected = {
                // (15,16): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.TypeIdentifierAttribute..ctor'
                //         return default(Test21);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "default(Test21)").WithArguments("System.Runtime.InteropServices.TypeIdentifierAttribute", ".ctor")
                                               };

            var compilation = CreateEmptyCompilation(consumer, new MetadataReference[] { MscorlibRef_v20, new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) }, options: TestOptions.ReleaseExe);

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateEmptyCompilation(consumer, references: new MetadataReference[] { MscorlibRef_v20, piaCompilation.EmitToImageReference(embedInteropTypes: true) }, options: TestOptions.ReleaseExe);

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void TypeIdentifierIsMissing2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest20
{
}
";

            var piaCompilation = CreateEmptyCompilation(pia, new MetadataReference[] { MscorlibRef_v20 }, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public object M13()
    {
        var x = (ITest20)null;
        return x;
    }
}";

            DiagnosticDescription[] expected = {
                // (10,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.TypeIdentifierAttribute..ctor'
                //         var x = (ITest20)null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x = (ITest20)null").WithArguments("System.Runtime.InteropServices.TypeIdentifierAttribute", ".ctor")
            };

            var compilation = CreateEmptyCompilation(consumer, new MetadataReference[] { MscorlibRef_v20, new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) }, options: TestOptions.DebugExe);

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateEmptyCompilation(consumer, references: new MetadataReference[] { MscorlibRef_v20, piaCompilation.EmitToImageReference(embedInteropTypes: true) }, options: TestOptions.DebugExe);

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void LocalTypeMetadata_Simple()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58258"")]
public interface ITest1
{ 
}

public struct Test2 : ITest1
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58259"")]
public interface ITest3 : ITest1
{
    void M2();
}

public interface ITest4
{ 
}

[System.Serializable()]
[StructLayout( LayoutKind.Explicit, CharSet =CharSet.Unicode, Pack = 16, Size = 64)]
public struct Test5
{
    [FieldOffset(2)]
    public int F5;
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58260"")]
public interface ITest6
{ 
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58261"")]
public interface ITest7
{ 
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58262"")]
public interface ITest8
{ 
}

public enum Test9
{
    F1 = 1,
    F2 = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct Test10
{
    [NonSerialized()]
    public int F3;

    [MarshalAs(UnmanagedType.U4)]
    public int F4;
}

public delegate void Test11();

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58264"")]
public interface ITest13
{
    void M13(int x, __arglist);
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58265"")]
public interface ITest14
{
    void M14();
    int P6 { set; }
    event System.Action E4; 
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58266"")]
public interface ITest15 : ITest14
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58267"")]
public interface ITest16
{
    void M16();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58268"")]
public interface ITest17
{
    void M17();
    void _VtblGap();
    void M18();
    void _VtblGap3_2();
    void M19();
    void _VtblGap4_2();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58269"")]
public interface ITest18
{
    void _VtblGap3_2();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58270"")]
public interface ITest19
{
    string M20(ref int x, out int y, [In()] ref int z, [In(), Out()] ref int u, [Optional()] int v, int w = 34);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string M21([MarshalAs(UnmanagedType.U4)] int x);
}

public struct Test20
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest21
{
    [SpecialName()]
    int P1 { get; set; }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58272"")]
public interface ITest22
{
    int P2 { get; set; }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58273"")]
public interface ITest23
{
    int P3 { get; }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58274"")]
public interface ITest24
{
    int P4 { set; }
    event System.Action E3; 
    void M27();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58275"")]
public interface ITest25
{
    [SpecialName()]
    event System.Action E1; 
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58276"")]
public interface ITest26
{
    event System.Action E2; 
    int P5 { set; }
    void M26();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
        Test2 x = new Test2();
        ITest3 y = null;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
        Test5 x5 = new Test5();
        System.Console.WriteLine(x5);
    }

    [MyAttribute(typeof(ITest7))]
    void M2(ITest6 x)
    {}
}

class UsePia1 : ITest8
{}

class MyAttribute : System.Attribute 
{
    public MyAttribute(System.Type type)
    { }
}

class UsePia2
{
    void Test(Test10 x, Test11 x11)
    {
        System.Console.WriteLine(Test9.F1);
        System.Console.WriteLine(x.F4);
        ITest17 y = null;
        y.M17();
        y.M19();
    }
}

class UsePia3 : ITest13
{
    public void M13(int x, __arglist)
    {
    }

    public void M14(ITest13 x)
    {
        x.M13(1, __arglist(2,3,4));
        x.M13(1, __arglist((Test20[])null));
    }
}

interface IUsePia4 : ITest15, ITest16, ITest18, ITest19
{
}

class UsePia4 
{
    public int M1(ITest21 x)
    {
	return x.P1;
    }

    public void M2(ITest22 x)
    {
	x.P2 = 1;
    }

    public int M3(ITest23 x)
    {
	return x.P3;
    }

    public void M4(ITest24 x)
    {
	x.P4 = 1;
    }

    public void M5(ITest25 x)
    {
	x.E1 += null;
    }

    public void M6(ITest26 x)
    {
	x.E2 -= null;
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest1 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest1").Single();
                    Assert.Equal(TypeKind.Interface, itest1.TypeKind);
                    Assert.Null(itest1.BaseType());
                    Assert.Equal(0, itest1.Interfaces().Length);
                    Assert.True(itest1.IsComImport);
                    Assert.False(itest1.IsSerializable);
                    Assert.False(itest1.IsSealed);
                    Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, itest1.MarshallingCharSet);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, itest1.Layout.Kind);
                    Assert.Equal(0, itest1.Layout.Alignment);
                    Assert.Equal(0, itest1.Layout.Size);

                    var attributes = itest1.GetAttributes();
                    Assert.Equal(3, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.GuidAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58258"")", attributes[1].ToString());
                    Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute", attributes[2].ToString());

                    // TypDefName: ITest1  (02000018)
                    // Flags     : [Public] [AutoLayout] [Interface] [Abstract] [Import] [AnsiClass]  (000010a1)
                    Assert.Equal(TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Import | TypeAttributes.AnsiClass, itest1.Flags);

                    var test2 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("Test2").Single();
                    Assert.Equal(TypeKind.Struct, test2.TypeKind);
                    Assert.Equal(SpecialType.System_ValueType, test2.BaseType().SpecialType);
                    Assert.Same(itest1, test2.Interfaces().Single());
                    Assert.False(test2.IsComImport);
                    Assert.False(test2.IsSerializable);
                    Assert.True(test2.IsSealed);
                    Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, test2.MarshallingCharSet);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Sequential, test2.Layout.Kind);
                    Assert.Equal(0, test2.Layout.Alignment);
                    Assert.Equal(1, test2.Layout.Size);

                    // TypDefName: Test2  (02000013)
                    // Flags     : [Public] [SequentialLayout] [Class] [Sealed] [AnsiClass] [BeforeFieldInit]  (00100109)
                    Assert.Equal(TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, test2.Flags);

                    attributes = test2.GetAttributes();
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test2"")", attributes[1].ToString());

                    var itest3 = module.GlobalNamespace.GetTypeMembers("ITest3").Single();
                    Assert.Equal(TypeKind.Interface, itest3.TypeKind);
                    Assert.Same(itest1, itest3.Interfaces().Single());
                    Assert.True(itest3.IsComImport);
                    Assert.False(itest3.IsSerializable);
                    Assert.False(itest3.IsSealed);
                    Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, itest3.MarshallingCharSet);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, itest3.Layout.Kind);
                    Assert.Equal(0, itest3.Layout.Alignment);
                    Assert.Equal(0, itest3.Layout.Size);

                    Assert.Equal(0, module.GlobalNamespace.GetTypeMembers("ITest4").Length);

                    var test5 = module.GlobalNamespace.GetTypeMembers("Test5").Single();
                    Assert.Equal(TypeKind.Struct, test5.TypeKind);
                    Assert.False(test5.IsComImport);
                    Assert.True(test5.IsSerializable);
                    Assert.True(test5.IsSealed);
                    Assert.Equal(System.Runtime.InteropServices.CharSet.Unicode, test5.MarshallingCharSet);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Explicit, test5.Layout.Kind);
                    Assert.Equal(16, test5.Layout.Alignment);
                    Assert.Equal(64, test5.Layout.Size);

                    var f5 = (PEFieldSymbol)test5.GetMembers()[0];
                    Assert.Equal("System.Int32 Test5.F5", f5.ToTestDisplayString());
                    Assert.Equal(2, f5.TypeLayoutOffset.Value);

                    // Field Name: F5 (04000003)
                    // Flags     : [Public]  (00000006)
                    Assert.Equal(FieldAttributes.Public, f5.Flags);

                    var itest6 = module.GlobalNamespace.GetTypeMembers("ITest6").Single();
                    Assert.Equal(TypeKind.Interface, itest6.TypeKind);

                    var itest7 = module.GlobalNamespace.GetTypeMembers("ITest7").Single();
                    Assert.Equal(TypeKind.Interface, itest7.TypeKind);

                    var itest8 = module.GlobalNamespace.GetTypeMembers("ITest8").Single();
                    Assert.Equal(TypeKind.Interface, itest8.TypeKind);
                    Assert.Same(itest8, module.GlobalNamespace.GetTypeMembers("UsePia1").Single().Interfaces().Single());

                    var test9 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("Test9").Single();
                    Assert.Equal(TypeKind.Enum, test9.TypeKind);
                    Assert.False(test9.IsComImport);
                    Assert.False(test9.IsSerializable);
                    Assert.True(test9.IsSealed);
                    Assert.Equal(System.Runtime.InteropServices.CharSet.Ansi, test9.MarshallingCharSet);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Auto, test9.Layout.Kind);

                    Assert.Equal(SpecialType.System_Int32, test9.EnumUnderlyingType.SpecialType);

                    // TypDefName: Test9  (02000016)
                    // Flags     : [Public] [AutoLayout] [Class] [Sealed] [AnsiClass]  (00000101)
                    Assert.Equal(TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass, test9.Flags);

                    attributes = test9.GetAttributes();
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test9"")", attributes[1].ToString());

                    var fieldToEmit = test9.GetFieldsToEmit().ToArray().AsImmutableOrNull();
                    Assert.Equal(3, fieldToEmit.Length);

                    var value__ = (PEFieldSymbol)fieldToEmit[0];
                    Assert.Equal(Accessibility.Public, value__.DeclaredAccessibility);
                    Assert.Equal("System.Int32 Test9.value__", value__.ToTestDisplayString());
                    Assert.False(value__.IsStatic);
                    Assert.True(value__.HasSpecialName);
                    Assert.True(value__.HasRuntimeSpecialName);
                    Assert.Null(value__.ConstantValue);

                    // Field Name: value__ (04000004)
                    // Flags     : [Public] [SpecialName] [RTSpecialName]  (00000606)
                    Assert.Equal(FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, value__.Flags);

                    var f1 = (PEFieldSymbol)fieldToEmit[1];
                    Assert.Equal(Accessibility.Public, f1.DeclaredAccessibility);
                    Assert.Equal("Test9.F1", f1.ToTestDisplayString());
                    Assert.True(f1.IsStatic);
                    Assert.False(f1.HasSpecialName);
                    Assert.False(f1.HasRuntimeSpecialName);
                    Assert.Equal(1, f1.ConstantValue);

                    // Field Name: F1 (04000005)
                    // Flags     : [Public] [Static] [Literal] [HasDefault]  (00008056)
                    Assert.Equal(FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, f1.Flags);

                    var f2 = (FieldSymbol)fieldToEmit[2];
                    Assert.Equal("Test9.F2", f2.ToTestDisplayString());
                    Assert.Equal(2, f2.ConstantValue);

                    Assert.Equal(3, test9.GetMembers().Length);
                    Assert.Same(f1, test9.GetMembers()[0]);
                    Assert.Same(f2, test9.GetMembers()[1]);
                    Assert.True(((MethodSymbol)test9.GetMembers()[2]).IsDefaultValueTypeConstructor());

                    var test10 = module.GlobalNamespace.GetTypeMembers("Test10").Single();
                    Assert.Equal(TypeKind.Struct, test10.TypeKind);
                    Assert.Equal(System.Runtime.InteropServices.LayoutKind.Sequential, test10.Layout.Kind);

                    Assert.Equal(3, test10.GetMembers().Length);

                    var f3 = (FieldSymbol)test10.GetMembers()[0];
                    Assert.Equal(Accessibility.Public, f3.DeclaredAccessibility);
                    Assert.Equal("System.Int32 Test10.F3", f3.ToTestDisplayString());
                    Assert.False(f3.IsStatic);
                    Assert.False(f3.HasSpecialName);
                    Assert.False(f3.HasRuntimeSpecialName);
                    Assert.Null(f3.ConstantValue);
                    Assert.Equal((System.Runtime.InteropServices.UnmanagedType)0, f3.MarshallingType);
                    Assert.False(f3.TypeLayoutOffset.HasValue);
                    Assert.True(f3.IsNotSerialized);

                    var f4 = (FieldSymbol)test10.GetMembers()[1];
                    Assert.Equal("System.Int32 Test10.F4", f4.ToTestDisplayString());
                    Assert.Equal(System.Runtime.InteropServices.UnmanagedType.U4, f4.MarshallingType);
                    Assert.False(f4.IsNotSerialized);

                    Assert.True(((MethodSymbol)test10.GetMembers()[2]).IsDefaultValueTypeConstructor());

                    var test11 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("Test11").Single();
                    Assert.Equal(TypeKind.Delegate, test11.TypeKind);
                    Assert.Equal(SpecialType.System_MulticastDelegate, test11.BaseType().SpecialType);

                    // TypDefName: Test11  (02000012)
                    // Flags     : [Public] [AutoLayout] [Class] [Sealed] [AnsiClass]  (00000101)
                    Assert.Equal(TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass, test11.Flags);

                    attributes = test11.GetAttributes();
                    Assert.Equal(2, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Test11"")", attributes[1].ToString());

                    Assert.Equal(4, test11.GetMembers().Length);

                    var ctor = (PEMethodSymbol)test11.GetMembers(".ctor").Single();

                    // MethodName: .ctor (0600000F)
                    // Flags     : [Public] [HideBySig] [ReuseSlot] [SpecialName] [RTSpecialName] [.ctor]  (00001886)
                    // ImplFlags : [Runtime] [Managed]  (00000003)
                    // CallCnvntn: [DEFAULT]
                    // hasThis 
                    // ReturnType: Void
                    // 2 Arguments
                    //     Argument #1:  Object
                    //     Argument #2:  I

                    Assert.Equal(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, ctor.Flags);
                    Assert.Equal(MethodImplAttributes.Runtime, (MethodImplAttributes)ctor.ImplementationAttributes);
                    Assert.Equal(CallingConvention.Default | CallingConvention.HasThis, ctor.CallingConvention);
                    Assert.Equal("Test11..ctor(System.Object @object, System.IntPtr method)", ctor.ToTestDisplayString());

                    var begin = (PEMethodSymbol)test11.GetMembers("BeginInvoke").Single();

                    // MethodName: BeginInvoke (06000011)
                    // Flags     : [Public] [Virtual] [HideBySig] [NewSlot]  (000001c6)
                    // ImplFlags : [Runtime] [Managed]  (00000003)
                    // CallCnvntn: [DEFAULT]
                    // hasThis 
                    // ReturnType: Class System.IAsyncResult
                    // 2 Arguments
                    //     Argument #1:  Class System.AsyncCallback
                    //     Argument #2:  Object
                    Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot, begin.Flags);
                    Assert.Equal(MethodImplAttributes.Runtime, (MethodImplAttributes)begin.ImplementationAttributes);
                    Assert.Equal(CallingConvention.Default | CallingConvention.HasThis, begin.CallingConvention);
                    Assert.Equal("System.IAsyncResult Test11.BeginInvoke(System.AsyncCallback callback, System.Object @object)", begin.ToTestDisplayString());

                    var end = (PEMethodSymbol)test11.GetMembers("EndInvoke").Single();

                    // MethodName: EndInvoke (06000012)
                    // Flags     : [Public] [Virtual] [HideBySig] [NewSlot]  (000001c6)
                    // ImplFlags : [Runtime] [Managed]  (00000003)
                    // CallCnvntn: [DEFAULT]
                    // hasThis 
                    // ReturnType: Void
                    // 1 Arguments
                    //     Argument #1:  Class System.IAsyncResult

                    Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot, end.Flags);
                    Assert.Equal(MethodImplAttributes.Runtime, (MethodImplAttributes)end.ImplementationAttributes);
                    Assert.Equal(CallingConvention.Default | CallingConvention.HasThis, end.CallingConvention);
                    Assert.Equal("void Test11.EndInvoke(System.IAsyncResult result)", end.ToTestDisplayString());

                    var invoke = (PEMethodSymbol)test11.GetMembers("Invoke").Single();

                    // MethodName: Invoke (06000010)
                    // Flags     : [Public] [Virtual] [HideBySig] [NewSlot]  (000001c6)
                    // ImplFlags : [Runtime] [Managed]  (00000003)
                    // CallCnvntn: [DEFAULT]
                    // hasThis 
                    // ReturnType: Void
                    // No arguments.

                    Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot, invoke.Flags);
                    Assert.Equal(MethodImplAttributes.Runtime, (MethodImplAttributes)invoke.ImplementationAttributes);
                    Assert.Equal(CallingConvention.Default | CallingConvention.HasThis, invoke.CallingConvention);
                    Assert.Equal("void Test11.Invoke()", invoke.ToTestDisplayString());

                    var itest13 = module.GlobalNamespace.GetTypeMembers("ITest13").Single();
                    Assert.Equal(TypeKind.Interface, itest13.TypeKind);

                    var m13 = (PEMethodSymbol)itest13.GetMembers()[0];

                    // MethodName: M13 (06000001)
                    // Flags     : [Public] [Virtual] [HideBySig] [NewSlot] [Abstract]  (000005c6)
                    // ImplFlags : [IL] [Managed]  (00000000)
                    // CallCnvntn: [VARARG]
                    // hasThis 
                    // ReturnType: Void
                    // 1 Arguments
                    //     Argument #1:  I4
                    // 1 Parameters
                    //     (1) ParamToken : (08000001) Name : x flags: [none] (00000000)

                    Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Abstract, m13.Flags);
                    Assert.Equal(MethodImplAttributes.IL, (MethodImplAttributes)m13.ImplementationAttributes);
                    Assert.Equal(CallingConvention.ExtraArguments | CallingConvention.HasThis, m13.CallingConvention);
                    Assert.Equal("void ITest13.M13(System.Int32 x, __arglist)", m13.ToTestDisplayString());

                    var itest14 = module.GlobalNamespace.GetTypeMembers("ITest14").Single();
                    Assert.Equal(TypeKind.Interface, itest14.TypeKind);
                    Assert.Equal(6, itest14.GetMembers().Length);
                    Assert.Equal("void ITest14.M14()", itest14.GetMembers()[0].ToTestDisplayString());
                    Assert.Equal("void ITest14.P6.set", itest14.GetMembers()[1].ToTestDisplayString());
                    Assert.Equal("void ITest14.E4.add", itest14.GetMembers()[2].ToTestDisplayString());
                    Assert.Equal("void ITest14.E4.remove", itest14.GetMembers()[3].ToTestDisplayString());
                    Assert.Equal("System.Int32 ITest14.P6 { set; }", itest14.GetMembers()[4].ToTestDisplayString());
                    Assert.Equal("event System.Action ITest14.E4", itest14.GetMembers()[5].ToTestDisplayString());

                    var itest16 = module.GlobalNamespace.GetTypeMembers("ITest16").Single();
                    Assert.Equal(TypeKind.Interface, itest16.TypeKind);
                    Assert.Equal("void ITest16.M16()", itest16.GetMembers()[0].ToTestDisplayString());

                    var itest17 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest17").Single();
                    Assert.Equal(TypeKind.Interface, itest17.TypeKind);

                    var metadata = ((PEModuleSymbol)module).Module;

                    var methodNames = metadata.GetMethodsOfTypeOrThrow(itest17.Handle).AsEnumerable().Select(rid => metadata.GetMethodDefNameOrThrow(rid)).ToArray();

                    Assert.Equal(3, methodNames.Length);
                    Assert.Equal("M17", methodNames[0]);
                    Assert.Equal("_VtblGap1_4", methodNames[1]);
                    Assert.Equal("M19", methodNames[2]);

                    MethodDefinitionHandle gapMethodDef = metadata.GetMethodsOfTypeOrThrow(itest17.Handle).AsEnumerable().ElementAt(1);
                    string name;
                    MethodImplAttributes implFlags;
                    MethodAttributes flags;
                    int rva;

                    metadata.GetMethodDefPropsOrThrow(gapMethodDef, out name, out implFlags, out flags, out rva);

                    Assert.Equal(MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, flags);
                    Assert.Equal(MethodImplAttributes.IL | MethodImplAttributes.Runtime, implFlags);

                    SignatureHeader signatureHeader;
                    BadImageFormatException mrEx;
                    ParamInfo<TypeSymbol>[] paramInfo = new MetadataDecoder((PEModuleSymbol)module, itest17).GetSignatureForMethod(gapMethodDef, out signatureHeader, out mrEx);
                    Assert.Null(mrEx);
                    Assert.Equal((byte)SignatureCallingConvention.Default | (byte)SignatureAttributes.Instance, signatureHeader.RawValue);
                    Assert.Equal(1, paramInfo.Length);
                    Assert.Equal(SpecialType.System_Void, paramInfo[0].Type.SpecialType);
                    Assert.False(paramInfo[0].IsByRef);
                    Assert.True(paramInfo[0].CustomModifiers.IsDefault);

                    Assert.Equal(2, itest17.GetMembers().Length);
                    var m17 = (PEMethodSymbol)itest17.GetMembers("M17").Single();

                    // MethodName: M17 (06000013)
                    // Flags     : [Public] [Virtual] [HideBySig] [NewSlot] [Abstract]  (000005c6)
                    // ImplFlags : [IL] [Managed]  (00000000)
                    // CallCnvntn: [DEFAULT]
                    // hasThis 
                    // ReturnType: Void
                    // No arguments.
                    Assert.Equal(MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Abstract, m17.Flags);
                    Assert.Equal(MethodImplAttributes.IL, (MethodImplAttributes)m17.ImplementationAttributes);
                    Assert.Equal(CallingConvention.Default | CallingConvention.HasThis, m17.CallingConvention);
                    Assert.Equal("void ITest17.M17()", m17.ToTestDisplayString());

                    var itest18 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest18").Single();
                    Assert.Equal(TypeKind.Interface, itest18.TypeKind);
                    Assert.False(metadata.GetMethodsOfTypeOrThrow(itest18.Handle).AsEnumerable().Any());

                    var itest19 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest19").Single();
                    var m20 = (PEMethodSymbol)itest19.GetMembers("M20").Single();

                    // 6 Arguments
                    //     Argument #1:  ByRef I4
                    //     Argument #2:  ByRef I4
                    //     Argument #3:  ByRef I4
                    //     Argument #4:  ByRef I4
                    //     Argument #5:  I4
                    //     Argument #6:  I4
                    // 6 Parameters
                    //     (1) ParamToken : (08000008) Name : x flags: [none] (00000000)
                    //     (2) ParamToken : (08000009) Name : y flags: [Out]  (00000002)
                    //     (3) ParamToken : (0800000a) Name : z flags: [In]  (00000001)
                    //     (4) ParamToken : (0800000b) Name : u flags: [In] [Out]  (00000003)
                    //     (5) ParamToken : (0800000c) Name : v flags: [Optional]  (00000010)
                    //     (6) ParamToken : (0800000d) Name : w flags: [Optional] [HasDefault]  (00001010) Default: (I4) 34

                    var param = (PEParameterSymbol)m20.Parameters[0];
                    Assert.Equal(RefKind.Ref, param.RefKind);
                    Assert.Equal((ParameterAttributes)0, param.Flags);
                    Assert.Equal(0, param.Ordinal);

                    param = (PEParameterSymbol)m20.Parameters[1];
                    Assert.Equal(RefKind.Out, param.RefKind);
                    Assert.Equal(ParameterAttributes.Out, param.Flags);
                    Assert.Equal(1, param.Ordinal);

                    param = (PEParameterSymbol)m20.Parameters[2];
                    Assert.Equal(RefKind.Ref, param.RefKind);
                    Assert.Equal(ParameterAttributes.In, param.Flags);
                    Assert.Equal(2, param.Ordinal);

                    param = (PEParameterSymbol)m20.Parameters[3];
                    Assert.Equal(RefKind.Ref, param.RefKind);
                    Assert.Equal(ParameterAttributes.In | ParameterAttributes.Out, param.Flags);
                    Assert.Equal(3, param.Ordinal);

                    param = (PEParameterSymbol)m20.Parameters[4];
                    Assert.Equal(RefKind.None, param.RefKind);
                    Assert.Equal(ParameterAttributes.Optional, param.Flags);
                    Assert.Null(param.ExplicitDefaultConstantValue);
                    Assert.Equal(4, param.Ordinal);

                    param = (PEParameterSymbol)m20.Parameters[5];
                    Assert.Equal(RefKind.None, param.RefKind);
                    Assert.Equal(ParameterAttributes.Optional | ParameterAttributes.HasDefault, param.Flags);
                    Assert.Equal(34, param.ExplicitDefaultValue);
                    Assert.Equal(5, param.Ordinal);

                    param = m20.ReturnTypeParameter;
                    Assert.Equal((ParameterAttributes)0, param.Flags);

                    var m21 = (PEMethodSymbol)itest19.GetMembers("M21").Single();

                    // 1 Arguments
                    //     Argument #1:  I4
                    // 2 Parameters
                    //     (0) ParamToken : (0800000e) Name :  flags: [HasFieldMarshal]  (00002000)
                    //         NATIVE_TYPE_LPWSTR 
                    //     (1) ParamToken : (0800000f) Name : x flags: [HasFieldMarshal]  (00002000)
                    //         NATIVE_TYPE_U4 

                    param = (PEParameterSymbol)m21.Parameters[0];
                    Assert.Equal(ParameterAttributes.HasFieldMarshal, param.Flags);
                    Assert.Equal(System.Runtime.InteropServices.UnmanagedType.U4, (System.Runtime.InteropServices.UnmanagedType)param.MarshallingDescriptor[0]);

                    param = m21.ReturnTypeParameter;
                    Assert.Equal(ParameterAttributes.HasFieldMarshal, param.Flags);
                    Assert.Equal(System.Runtime.InteropServices.UnmanagedType.LPWStr, (System.Runtime.InteropServices.UnmanagedType)param.MarshallingDescriptor[0]);

                    var itest21 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest21").Single();
                    var p1 = (PEPropertySymbol)itest21.GetMembers("P1").Single();

                    Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                    Assert.True(p1.HasSpecialName);
                    Assert.False(p1.HasRuntimeSpecialName);

                    var get_P1 = (PEMethodSymbol)itest21.GetMembers("get_P1").Single();
                    var set_P1 = (PEMethodSymbol)itest21.GetMembers("set_P1").Single();

                    Assert.Same(p1.GetMethod, get_P1);
                    Assert.Same(p1.SetMethod, set_P1);

                    var itest22 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest22").Single();
                    var p2 = (PEPropertySymbol)itest22.GetMembers("P2").Single();

                    var get_P2 = (PEMethodSymbol)itest22.GetMembers("get_P2").Single();
                    var set_P2 = (PEMethodSymbol)itest22.GetMembers("set_P2").Single();

                    Assert.Same(p2.GetMethod, get_P2);
                    Assert.Same(p2.SetMethod, set_P2);

                    var itest23 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest23").Single();
                    var p3 = (PEPropertySymbol)itest23.GetMembers("P3").Single();

                    var get_P3 = (PEMethodSymbol)itest23.GetMembers("get_P3").Single();

                    Assert.Same(p3.GetMethod, get_P3);
                    Assert.Null(p3.SetMethod);

                    var itest24 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest24").Single();
                    var p4 = (PEPropertySymbol)itest24.GetMembers("P4").Single();

                    Assert.Equal(2, itest24.GetMembers().Length);
                    Assert.False(p4.HasSpecialName);
                    Assert.False(p4.HasRuntimeSpecialName);
                    Assert.Equal((byte)SignatureKind.Property | (byte)SignatureAttributes.Instance, (byte)p4.CallingConvention);

                    var set_P4 = (PEMethodSymbol)itest24.GetMembers("set_P4").Single();

                    Assert.Null(p4.GetMethod);
                    Assert.Same(p4.SetMethod, set_P4);

                    var itest25 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest25").Single();
                    var e1 = (PEEventSymbol)itest25.GetMembers("E1").Single();

                    Assert.True(e1.HasSpecialName);
                    Assert.False(e1.HasRuntimeSpecialName);

                    var add_E1 = (PEMethodSymbol)itest25.GetMembers("add_E1").Single();
                    var remove_E1 = (PEMethodSymbol)itest25.GetMembers("remove_E1").Single();

                    Assert.Same(e1.AddMethod, add_E1);
                    Assert.Same(e1.RemoveMethod, remove_E1);

                    var itest26 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest26").Single();
                    var e2 = (PEEventSymbol)itest26.GetMembers("E2").Single();

                    Assert.Equal(3, itest26.GetMembers().Length);
                    Assert.False(e2.HasSpecialName);
                    Assert.False(e2.HasRuntimeSpecialName);

                    var add_E2 = (PEMethodSymbol)itest26.GetMembers("add_E2").Single();
                    var remove_E2 = (PEMethodSymbol)itest26.GetMembers("remove_E2").Single();

                    Assert.Same(e2.AddMethod, add_E2);
                    Assert.Same(e2.RemoveMethod, remove_E2);
                };

            var expected_M5 =
            @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldnull
  IL_0002:  callvirt   ""void ITest25.E1.add""
  IL_0007:  ret
}
";

            var expected_M6 =
            @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldnull
  IL_0002:  callvirt   ""void ITest26.E2.remove""
  IL_0007:  ret
}
";

            var verifier = CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia4.M5", expected_M5);
            verifier.VerifyIL("UsePia4.M6", expected_M6);

            verifier = CompileAndVerify(compilation2, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia4.M5", expected_M5);
            verifier.VerifyIL("UsePia4.M6", expected_M6);
        }

        [Fact]
        public void LocalTypeMetadata_GenericParameters()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
public interface ITest28
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public interface ITest29
{
    void M21<T1, T2, T5, T6, T7>() where T2 : ITest28 where T5 : new() where T6 : struct where T7 : class;
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            //CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

interface UsePia5 : ITest29
{
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest28 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest28").Single();
                    Assert.Equal(TypeKind.Interface, itest28.TypeKind);

                    var itest29 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest29").Single();
                    Assert.Equal(TypeKind.Interface, itest29.TypeKind);

                    var m21 = (PEMethodSymbol)itest29.GetMembers("M21").Single();

                    Assert.Equal(5, m21.TypeParameters.Length);
                    var t1 = m21.TypeParameters[0];
                    Assert.Equal("T1", t1.Name);
                    Assert.False(t1.HasConstructorConstraint);
                    Assert.False(t1.HasValueTypeConstraint);
                    Assert.False(t1.HasReferenceTypeConstraint);
                    Assert.Equal(0, t1.ConstraintTypes().Length);
                    Assert.Equal(VarianceKind.None, t1.Variance);

                    var t2 = m21.TypeParameters[1];
                    Assert.False(t2.HasConstructorConstraint);
                    Assert.False(t2.HasValueTypeConstraint);
                    Assert.False(t2.HasReferenceTypeConstraint);
                    Assert.Equal(1, t2.ConstraintTypes().Length);
                    Assert.Same(itest28, t2.ConstraintTypes()[0]);
                    Assert.Equal(VarianceKind.None, t2.Variance);

                    var t5 = m21.TypeParameters[2];
                    Assert.True(t5.HasConstructorConstraint);
                    Assert.False(t5.HasValueTypeConstraint);
                    Assert.False(t5.HasReferenceTypeConstraint);
                    Assert.Equal(0, t5.ConstraintTypes().Length);
                    Assert.Equal(VarianceKind.None, t5.Variance);

                    var t6 = m21.TypeParameters[3];
                    Assert.False(t6.HasConstructorConstraint);
                    Assert.True(t6.HasValueTypeConstraint);
                    Assert.False(t6.HasReferenceTypeConstraint);
                    Assert.Equal(0, t6.ConstraintTypes().Length);
                    Assert.Equal(VarianceKind.None, t6.Variance);

                    var t7 = m21.TypeParameters[4];
                    Assert.False(t7.HasConstructorConstraint);
                    Assert.False(t7.HasValueTypeConstraint);
                    Assert.True(t7.HasReferenceTypeConstraint);
                    Assert.Equal(0, t7.ConstraintTypes().Length);
                    Assert.Equal(VarianceKind.None, t7.Variance);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator, verify: Verification.FailsPEVerify);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator, verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void NewWithoutCoClass()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58271"")]
public interface ITest28
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public ITest28 Test()
    {
        return new ITest28();
    }
}";

            DiagnosticDescription[] expected = {
                // (10,16): error CS0144: Cannot create an instance of the abstract type or interface 'ITest28'
                //         return new ITest28();
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new ITest28()").WithArguments("ITest28")
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void NewCoClassWithoutGiud()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
    int P1 { get; set; }
}

public class ClassITest28 //: ITest28
{
    private ClassITest28(){} 
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public ITest28 Test()
    {
        return new ITest28 {P1 = 2};
    }
}";

            var expected =
@"
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (ITest28 V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""00000000-0000-0000-0000-000000000000""
  IL_0006:  newobj     ""System.Guid..ctor(string)""
  IL_000b:  call       ""System.Type System.Type.GetTypeFromCLSID(System.Guid)""
  IL_0010:  call       ""object System.Activator.CreateInstance(System.Type)""
  IL_0015:  castclass  ""ITest28""
  IL_001a:  dup
  IL_001b:  ldc.i4.2
  IL_001c:  callvirt   ""void ITest28.P1.set""
  IL_0021:  nop
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_0025
  IL_0025:  ldloc.0
  IL_0026:  ret
}
";

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol module) =>
            {
                ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                var itest28 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest28").Single();

                var interfaceType = itest28.GetAttributes("System.Runtime.InteropServices", "CoClassAttribute").Single();
                Assert.Equal("System.Runtime.InteropServices.CoClassAttribute(typeof(object))", interfaceType.ToString());
            };

            var compilation = CreateCompilationWithMscorlib40(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            var verifier = CompileAndVerify(compilation, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);

            compilation = CreateCompilationWithMscorlib40(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            verifier = CompileAndVerify(compilation, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);
        }

        [Fact]
        public void NewCoClassWithGiud()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest28 //: ITest28
{
    public ClassITest28(int x){} 
}
";

            var piaCompilation = CreateEmptyCompilation(pia, new MetadataReference[] { MscorlibRef_v4_0_30316_17626 }, options: TestOptions.DebugDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public ITest28 Test()
    {
        return new ITest28();
    }
}";

            var expected =
@"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (ITest28 V_0)
  IL_0000:  nop
  IL_0001:  ldstr      ""f9c2d51d-4f44-45f0-9eda-c9d599b58278""
  IL_0006:  newobj     ""System.Guid..ctor(string)""
  IL_000b:  call       ""System.Type System.Runtime.InteropServices.Marshal.GetTypeFromCLSID(System.Guid)""
  IL_0010:  call       ""object System.Activator.CreateInstance(System.Type)""
  IL_0015:  castclass  ""ITest28""
  IL_001a:  stloc.0
  IL_001b:  br.s       IL_001d
  IL_001d:  ldloc.0
  IL_001e:  ret
}
";

            Action<ModuleSymbol> metadataValidator = (ModuleSymbol module) =>
            {
                ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                var itest28 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest28").Single();

                var interfaceType = itest28.GetAttributes("System.Runtime.InteropServices", "CoClassAttribute").Single();
                Assert.Equal("System.Runtime.InteropServices.CoClassAttribute(typeof(object))", interfaceType.ToString());
            };

            var compilation = CreateEmptyCompilation(consumer,
                                                new MetadataReference[] { MscorlibRef_v4_0_30316_17626, new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) },
                                                options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(compilation, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);

            compilation = CreateEmptyCompilation(consumer,
                                                        new MetadataReference[] { MscorlibRef_v4_0_30316_17626, piaCompilation.EmitToImageReference(embedInteropTypes: true) },
                                                        options: TestOptions.DebugExe);

            verifier = CompileAndVerify(compilation, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);
        }

        [Fact]
        public void NewCoClassWithArguments()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
    int P1 { get; set; }
}

public class ClassITest28 //: ITest28
{
    private ClassITest28(int x){} 
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public ITest28 Test()
    {
        return new ITest28(1);
    }
}";


            DiagnosticDescription[] expected = {
                // (10,20): error CS1729: 'ITest28' does not contain a constructor that takes 1 arguments
                //         return new ITest28(1);
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "ITest28").WithArguments("ITest28", "1").WithLocation(10, 20)
            };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void NewCoClassMissingWellKnownMembers()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58277"")]
[CoClass(typeof(ClassITest28))]
public interface ITest28
{
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public class ClassITest28 : ITest28
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);

            CompileAndVerify(piaCompilation);

            string consumer = @"
namespace System
{
    public class Guid
    {
        private Guid() { }
    }
    public class Activator
    {
    }
}
class UsePia
{
    public static void Main()
    {
    }

    public ITest28 Test()
    {
        return new ITest28();
    }
}";


            DiagnosticDescription[] expected = {
                // (20,16): error CS0656: Missing compiler required member 'System.Guid..ctor'
                //         return new ITest28();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "new ITest28()").WithArguments("System.Guid", ".ctor").WithLocation(20, 16)
                                               };

            var compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);

            compilation = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation, true, expected);
        }

        [Fact]
        public void AddHandler_Simple()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[ComEventInterface(typeof(InterfaceEvents), typeof(int))]
public interface Interface1_Event
{
    event MyDelegate Goo;
}

[ComImport(), Guid(""84374891-a3b1-4f8f-8310-99ea58059b10"")]
public interface InterfaceEvents
{
    void Goo();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1_Event x)
    {
    	x.Goo += Handler;	
    }

    void Handler()
    {}
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(2, module.GetReferencedAssemblySymbols().Length);
                    Assert.Equal("mscorlib", module.GetReferencedAssemblySymbols()[0].Name);
                    Assert.Equal("System.Core", module.GetReferencedAssemblySymbols()[1].Name);

                    var interface1_Event = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("Interface1_Event").Single();

                    var attributes = interface1_Event.GetAttributes();
                    Assert.Equal(3, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal("System.Runtime.InteropServices.ComEventInterfaceAttribute(typeof(InterfaceEvents), typeof(InterfaceEvents))", attributes[1].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.TypeIdentifierAttribute(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""Interface1_Event"")", attributes[2].ToString());

                    var goo = (PEEventSymbol)interface1_Event.GetMembers("Goo").Single();

                    var interfaceEvents = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("InterfaceEvents").Single();

                    attributes = interfaceEvents.GetAttributes();
                    Assert.Equal(3, attributes.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attributes[0].ToString());
                    Assert.Equal(@"System.Runtime.InteropServices.GuidAttribute(""84374891-a3b1-4f8f-8310-99ea58059b10"")", attributes[1].ToString());
                    Assert.Equal("System.Runtime.InteropServices.TypeIdentifierAttribute", attributes[2].ToString());

                    var goo1 = (PEMethodSymbol)interfaceEvents.GetMembers("Goo").Single();
                };

            var expected =
@"
{
  // Code size       39 (0x27)
  .maxstack  4
  IL_0000:  ldtoken    ""Interface1_Event""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""Goo""
  IL_000f:  newobj     ""System.Runtime.InteropServices.ComAwareEventInfo..ctor(System.Type, string)""
  IL_0014:  ldarg.1
  IL_0015:  ldarg.0
  IL_0016:  ldftn      ""void UsePia.Handler()""
  IL_001c:  newobj     ""MyDelegate..ctor(object, System.IntPtr)""
  IL_0021:  callvirt   ""void System.Reflection.EventInfo.AddEventHandler(object, System.Delegate)""
  IL_0026:  ret
}
";

            var verifier = CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);

            verifier = CompileAndVerify(compilation2, symbolValidator: metadataValidator);

            verifier.VerifyIL("UsePia.Test", expected);
        }

        [Fact]
        public void RemoveHandler_Simple()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[ComEventInterface(typeof(InterfaceEvents), typeof(int))]
public interface Interface1_Event
{
    event MyDelegate Goo;
}

[ComImport(), Guid(""84374891-a3b1-4f8f-8310-99ea58059b10"")]
public interface InterfaceEvents
{
    void Goo(int x);
}

[ComImport(), Guid(""84374c91-a3b1-4f8f-8310-99ea58059b10"")]
public interface Interface1 : Interface1_Event
{
    void Raise();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1 x)
    {
    	x.Goo -= Handler;	
    }

    void Handler()
    {}
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            var expected =
@"
{
  // Code size       39 (0x27)
  .maxstack  4
  IL_0000:  ldtoken    ""Interface1_Event""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ldstr      ""Goo""
  IL_000f:  newobj     ""System.Runtime.InteropServices.ComAwareEventInfo..ctor(System.Type, string)""
  IL_0014:  ldarg.1
  IL_0015:  ldarg.0
  IL_0016:  ldftn      ""void UsePia.Handler()""
  IL_001c:  newobj     ""MyDelegate..ctor(object, System.IntPtr)""
  IL_0021:  callvirt   ""void System.Reflection.EventInfo.RemoveEventHandler(object, System.Delegate)""
  IL_0026:  ret
}
";

            var verifier = CompileAndVerify(compilation1);

            verifier.VerifyIL("UsePia.Test", expected);

            verifier = CompileAndVerify(compilation2);

            verifier.VerifyIL("UsePia.Test", expected);
        }

        [Fact]
        public void CS1766ERR_MissingMethodOnSourceInterface()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[ComEventInterface(typeof(InterfaceEvents), typeof(int))]
public interface Interface1_Event
{
    event MyDelegate Goo;
}

[ComImport(), Guid(""84374891-a3b1-4f8f-8310-99ea58059b10"")]
public interface InterfaceEvents
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1_Event x)
    {
    	x.Goo -= Handler;	
    }

    void Handler()
    {}
}
";

            DiagnosticDescription[] expected = {
                // (10,6): error CS1766: Source interface 'InterfaceEvents' is missing method 'Goo' which is required to embed event 'Interface1_Event.Goo'.
                //     	x.Goo -= Handler;	
                Diagnostic(ErrorCode.ERR_MissingMethodOnSourceInterface, "x.Goo -= Handler").WithArguments("InterfaceEvents", "Goo", "Interface1_Event.Goo")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation1, true, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation2, true, expected);
        }

        [Fact]
        public void CS1767ERR_MissingSourceInterface()
        {
            string pia = @"
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[ComEventInterface(typeof(object[]), typeof(object))]
public interface Interface1_Event
{
    event MyDelegate E;
}

[ComEventInterface(typeof(object[]), typeof(object))]
public interface Interface2_Event
{
    event MyDelegate E;
}

[ComEventInterface(null, null)]
public interface Interface3_Event
{
    event MyDelegate E;
}

[ComEventInterface(null, null)]
public interface Interface4_Event
{
    event MyDelegate E;
}";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    void M1(Interface1_Event x1)
    {
    	x1.E += Handler;
    }
    void M2(Interface2_Event x2)
    {
    }
    void M3(Interface3_Event x3)
    {
    }
    void M4(Interface4_Event x4)
    {
    	x4.E += Handler;
    }
    void Handler()
    {
    }
}";

            DiagnosticDescription[] expected = {
                // (6,6): error CS1767: Interface 'Interface1_Event' has an invalid source interface which is required to embed event 'Interface1_Event.E'.
                //     	x1.E += Handler;
                Diagnostic(ErrorCode.ERR_MissingSourceInterface, "x1.E += Handler").WithArguments("Interface1_Event", "Interface1_Event.E").WithLocation(6, 6),
                // (16,6): error CS1767: Interface 'Interface4_Event' has an invalid source interface which is required to embed event 'Interface4_Event.E'.
                //     	x4.E += Handler;
                Diagnostic(ErrorCode.ERR_MissingSourceInterface, "x4.E += Handler").WithArguments("Interface4_Event", "Interface4_Event.E").WithLocation(16, 6)
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation1, true, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation2, true, expected);
        }

        [Fact]
        public void MissingComImport()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[Guid(""84374891-a3b1-4f8f-8310-99ea58059b10"")]
public interface Interface1_Event
{
    event MyDelegate Goo;
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1_Event x)
    {
    	x.Goo -= Handler;	
    }

    void Handler()
    {}
}
";

            DiagnosticDescription[] expected = {
                // (10,6): error CS1756: Interop type 'Interface1_Event' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
                //     	x.Goo -= Handler;	
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute, "x.Goo -= Handler").WithArguments("Interface1_Event", "System.Runtime.InteropServices.ComImportAttribute")
                                               };

            DiagnosticDescription[] expectedMetadataOnly = {
                // error CS1756: Interop type 'Interface1_Event' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute).WithArguments("Interface1_Event", "System.Runtime.InteropServices.ComImportAttribute")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation1, false, expected, expectedMetadataOnly);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            VerifyEmitDiagnostics(compilation2, false, expected, expectedMetadataOnly);
        }

        [Fact]
        public void MissingGuid()
        {
            var iLSource = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


.class public auto ansi sealed MyDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method MyDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke() runtime managed
  {
  } // end of method MyDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method MyDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method MyDelegate::EndInvoke

} // end of class MyDelegate

.class interface public abstract auto ansi import Interface1_Event
{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_Goo(class MyDelegate 'value') cil managed
  {
  } // end of method Interface1_Event::add_Goo

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_Goo(class MyDelegate 'value') cil managed
  {
  } // end of method Interface1_Event::remove_Goo

  .event MyDelegate Goo
  {
    .removeon instance void Interface1_Event::remove_Goo(class MyDelegate)
    .addon instance void Interface1_Event::add_Goo(class MyDelegate)
  } // end of event Interface1_Event::Goo
} // end of class Interface1_Event
";

            MetadataReference piaReference = CompileIL(iLSource, prependDefaultHeader: false, embedInteropTypes: true);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1_Event x)
    {
    	x.Goo -= Handler;	
    }

    void Handler()
    {}
}
";

            DiagnosticDescription[] expected = {
                // (10,6): error CS1756: Interop type 'Interface1_Event' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.GuidAttribute' attribute.
                //     	x.Goo -= Handler;	
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute, "x.Goo -= Handler").WithArguments("Interface1_Event", "System.Runtime.InteropServices.GuidAttribute")
                                               };

            DiagnosticDescription[] expectedMetadataOnly = {
                // error CS1756: Interop type 'Interface1_Event' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.GuidAttribute' attribute.
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute).WithArguments("Interface1_Event", "System.Runtime.InteropServices.GuidAttribute")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaReference });

            VerifyEmitDiagnostics(compilation1, false, expected, expectedMetadataOnly);
        }

        [Fact]
        public void InterfaceTypeAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITest29
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[InterfaceType((short)ComInterfaceType.InterfaceIsIUnknown)]
public interface ITest30
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest29, ITest30 
{
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest29 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest29").Single();

                    var interfaceType = itest29.GetAttributes("System.Runtime.InteropServices", "InterfaceTypeAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)", interfaceType.ToString());

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    interfaceType = itest30.GetAttributes("System.Runtime.InteropServices", "InterfaceTypeAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.InterfaceTypeAttribute(1)", interfaceType.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void BestFitMappingAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
[BestFitMapping(true)]
public interface ITest29
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[BestFitMapping(false, ThrowOnUnmappableChar=true)]
public interface ITest30
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest29, ITest30 
{
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest29 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest29").Single();

                    var interfaceType = itest29.GetAttributes("System.Runtime.InteropServices", "BestFitMappingAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.BestFitMappingAttribute(true)", interfaceType.ToString());

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    interfaceType = itest30.GetAttributes("System.Runtime.InteropServices", "BestFitMappingAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.BestFitMappingAttribute(false, ThrowOnUnmappableChar = true)", interfaceType.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void FlagsAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[Flags()]
public enum Test31
{
    a = 0
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M6(Test31 x)
    {
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var test31 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("Test31").Single();

                    var interfaceType = test31.GetAttributes("System", "FlagsAttribute").Single();
                    Assert.Equal("System.FlagsAttribute", interfaceType.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DefaultMemberAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[System.Reflection.DefaultMember(""M1"")]
public interface ITest30
{
    int[] M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M6(ITest30 x)
    {
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var interfaceType = itest30.GetAttributes("System.Reflection", "DefaultMemberAttribute").Single();
                    Assert.Equal(@"System.Reflection.DefaultMemberAttribute(""M1"")", interfaceType.ToString());

                    Assert.Equal("System.Int32[] ITest30.M1()", itest30.GetMembers("M1").Single().ToTestDisplayString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void LCIDConversionAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    [LCIDConversionAttribute(123)]
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1()
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    var attr = m1.GetAttributes("System.Runtime.InteropServices", "LCIDConversionAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.LCIDConversionAttribute(123)", attr.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DispIdAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    [DispIdAttribute(124)]
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1()
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    var attr = m1.GetAttributes("System.Runtime.InteropServices", "DispIdAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.DispIdAttribute(124)", attr.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void ParamArrayAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    void M1(params int[] x);
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1(params int[] x)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    Assert.True(m1.Parameters[0].IsParams);
                    Assert.Equal(0, m1.Parameters[0].GetAttributes().Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DateTimeConstantAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    void M1([Optional()][DateTimeConstantAttribute(987654321)] DateTime x);
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1(System.DateTime x)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    Assert.Equal(new System.DateTime(987654321), m1.Parameters[0].ExplicitDefaultValue);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DecimalConstantAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    void M1([Optional()][DecimalConstantAttribute(0,0,int.MinValue,-2, -3)] decimal x);
    void M2([Optional()][DecimalConstantAttribute(0,0,uint.MaxValue,2, 3)] decimal x);
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1(decimal x)
    {
    }
    public void M2(decimal x)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    Assert.Equal(39614081275578912866186559485m, m1.Parameters[0].ExplicitDefaultValue);

                    var m2 = (PEMethodSymbol)itest30.GetMembers("M2").Single();

                    Assert.Equal(79228162495817593528424333315m, m2.Parameters[0].ExplicitDefaultValue);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DefaultParameterValueAttribute()
        {
            var iLSource = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.class interface public abstract auto ansi import ITest30
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 37 39 00 00 )                      // 9b58279..
  .method public newslot abstract strict virtual 
          instance void  M1([opt] valuetype [mscorlib]System.Decimal x) cil managed
  {
    .param [1]
    .custom instance void [System]System.Runtime.InteropServices.DefaultParameterValueAttribute::.ctor(object) = ( 01 00 0D 10 58 39 B4 C8 D6 5E 40 00 00 )          // ....X9...^@..
  } // end of method ITest30::M1

} // end of class ITest30
";

            MetadataReference piaReference = CompileIL(iLSource, prependDefaultHeader: false, embedInteropTypes: true);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 : ITest30
{
    public void M1(decimal x)
    {
    }
} 
";

            var compilation1 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaReference, SystemRef });


            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(2, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    var attr = m1.Parameters[0].GetAttributes("System.Runtime.InteropServices", "DefaultParameterValueAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.DefaultParameterValueAttribute(123.356)", attr.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);
        }

        [Fact]
        public void UnmanagedFunctionPointerAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, SetLastError=true)]
public delegate void MyDelegate();
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5
{
    public void M1(MyDelegate x)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var myDelegate = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("MyDelegate").Single();

                    var attr = myDelegate.GetAttributes("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute").Single();
                    Assert.Equal("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.StdCall, SetLastError = true)", attr.ToString());
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void PreserveSigAttribute()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    [PreserveSigAttribute()]
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia5 
{
    public void M1(ITest30 x)
    {
         x.M1();
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest30 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest30").Single();

                    var m1 = (PEMethodSymbol)itest30.GetMembers("M1").Single();

                    Assert.Equal(MethodImplAttributes.IL | MethodImplAttributes.PreserveSig, (MethodImplAttributes)m1.ImplementationAttributes);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void PiaWithoutGuid()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
//[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            DiagnosticDescription[] expected = {
                // error CS1747: Cannot embed interop types from assembly 'Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing the 'System.Runtime.InteropServices.GuidAttribute' attribute.
                Diagnostic(ErrorCode.ERR_NoPIAAssemblyMissingAttribute).WithArguments("Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Runtime.InteropServices.GuidAttribute")
                                               };

            VerifyEmitDiagnostics(compilation1, false, expected);
            VerifyEmitDiagnostics(compilation2, false, expected);
        }

        [Fact]
        public void NotAPia()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

//[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            DiagnosticDescription[] expected = {
                // error CS1759: Cannot embed interop types from assembly 'Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because it is missing either the 'System.Runtime.InteropServices.ImportedFromTypeLibAttribute' attribute or the 'System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute' attribute.
                Diagnostic(ErrorCode.ERR_NoPIAAssemblyMissingAttributes).WithArguments("Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "System.Runtime.InteropServices.ImportedFromTypeLibAttribute", "System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute")
                                               };

            VerifyEmitDiagnostics(compilation1, false, expected);
            VerifyEmitDiagnostics(compilation2, false, expected);
        }

        [Fact]
        public void TypeNameConflict1()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest32
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest321 : ITest32
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");

            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58256"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58281"")]
public interface ITest32
{
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58282"")]
public interface ITest322 : ITest32
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2");

            CompileAndVerify(piaCompilation2);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest321 x, ITest322 y)
    {
    }
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true),
                                                      new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation1.EmitToImageReference(embedInteropTypes: true),
                                                      piaCompilation2.EmitToImageReference(embedInteropTypes: true)});

            DiagnosticDescription[] expected = {
                // error CS1758: Cannot embed interop type 'ITest32' found in both assembly 'Pia1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' and 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_InteropTypesWithSameNameAndGuid).WithArguments("ITest32", "Pia1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                               };

            VerifyEmitDiagnostics(compilation1, false, expected);
            VerifyEmitDiagnostics(compilation2, false, expected);
        }

        [Fact]
        public void TypeNameConflict2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1(ITest34 x);
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34
{
    void M2();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia1");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest33 y)
    {
	y.M1(null);
    }
} 

class ITest34
{
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            DiagnosticDescription[] expected = {
                // error CS1761: Embedding the interop type 'ITest34' from assembly 'Pia1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' causes a name clash in the current assembly. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_LocalTypeNameClash).WithArguments("ITest34", "Pia1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                               };

            VerifyEmitDiagnostics(compilation1, true, expected);
            VerifyEmitDiagnostics(compilation2, true, expected);
        }

        [Fact]
        public void NoIndirectReference()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    [System.Security.SecurityCriticalAttribute()]
    void M1();
}
";

            string consumer1 = @"
public class UsePia6
{
    public static void Main()
    {
    }

    public void M1(ITest30 x)
    {
    }
} 
";

            string consumer2 = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest30 x)
    {
    }
}
";

            DiagnosticDescription[] expected = {
                                               };

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);
                };

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);
            var piaMetadata = AssemblyMetadata.CreateFromImage(piaCompilation.EmitToArray());

            var compilation1 = CreateCompilation(consumer1, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: false) }, assemblyName: "Consumer1");

            CompileAndVerify(compilation1);
            var metadata1 = AssemblyMetadata.CreateFromImage(compilation1.EmitToArray());

            var compilation2 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true),
                                                      new CSharpCompilationReference(compilation1, embedInteropTypes: false) });

            CompileAndVerify(compilation2, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation3 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true),
                                                      metadata1.GetReference(embedInteropTypes: false) });

            CompileAndVerify(compilation3, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation4 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaMetadata.GetReference(embedInteropTypes: true),
                                                      new CSharpCompilationReference(compilation1, embedInteropTypes: false) });

            CompileAndVerify(compilation4, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation5 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaMetadata.GetReference(embedInteropTypes: true),
                                                      metadata1.GetReference(embedInteropTypes: false) });

            CompileAndVerify(compilation5, symbolValidator: metadataValidator).VerifyDiagnostics(expected);
        }

        [Fact]
        public void IndirectReference()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest30
{
    [System.Security.SecurityCriticalAttribute()]
    void M1();
}
";

            string consumer1 = @"
public class UsePia6
{
    public static void Main()
    {
    }

    public void M1(ITest30 x)
    {
    }
} 
";

            string consumer2 = @"
class UsePia
{
    public static void Main()
    {
        UsePia6.Main();
    }

    public void M1(ITest30 x)
    {
    }
}
";

            DiagnosticDescription[] expected = {
                // warning CS1762: A reference was created to embedded interop assembly 'Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' because of an indirect reference to that assembly created by assembly 'Consumer1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Consider changing the 'Embed Interop Types' property on either assembly.
                Diagnostic(ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA).WithArguments("Pia, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "Consumer1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                               };

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(2, module.GetReferencedAssemblySymbols().Length);
                    Assert.Equal("Consumer1", module.GetReferencedAssemblySymbols()[1].Name);
                };

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);
            var piaMetadata = AssemblyMetadata.CreateFromImage(piaCompilation.EmitToArray());

            var compilation1 = CreateCompilation(consumer1, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: false) }, assemblyName: "Consumer1");

            CompileAndVerify(compilation1);
            var metadata1 = AssemblyMetadata.CreateFromImage(compilation1.EmitToArray());

            var compilation2 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true),
                                                      new CSharpCompilationReference(compilation1, embedInteropTypes: false) });

            CompileAndVerify(compilation2, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation3 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true),
                                                      metadata1.GetReference(embedInteropTypes: false) });

            CompileAndVerify(compilation3, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation4 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaMetadata.GetReference(embedInteropTypes: true),
                                                      new CSharpCompilationReference(compilation1, embedInteropTypes: false) });

            CompileAndVerify(compilation4, symbolValidator: metadataValidator).VerifyDiagnostics(expected);

            var compilation5 = CreateCompilation(consumer2, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaMetadata.GetReference(embedInteropTypes: true),
                                                      metadata1.GetReference(embedInteropTypes: false) });

            CompileAndVerify(compilation5, symbolValidator: metadataValidator).VerifyDiagnostics(expected);
        }

        [Fact]
        public void ImplementedInterfacesAndTheirMembers_1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 : ITest33
{
    void M2();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58281"")]
public interface ITest35 : ITest34
{
    void M3();
}";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

interface IUsePia6 : ITest35
{
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    var m1 = (PEMethodSymbol)itest33.GetMembers("M1").Single();

                    var itest34 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest34").Single();
                    var m2 = (PEMethodSymbol)itest34.GetMembers("M2").Single();

                    var itest35 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest35").Single();
                    var m3 = (PEMethodSymbol)itest35.GetMembers("M3").Single();
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void ImplementedInterfacesAndTheirMembers_2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 : ITest33
{
    void M2();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58281"")]
public interface ITest35 : ITest34
{
    void M3();
}";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
}

class UsePia6 
{
    public virtual void M1(){}
    public virtual void M2(){}
    public virtual void M3(){}
} 

class UsePia7 : UsePia6, ITest35
{
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    var m1 = (PEMethodSymbol)itest33.GetMembers("M1").Single();

                    var itest34 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest34").Single();
                    var m2 = (PEMethodSymbol)itest34.GetMembers("M2").Single();

                    var itest35 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest35").Single();
                    var m3 = (PEMethodSymbol)itest35.GetMembers("M3").Single();
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void ImplementedInterfacesAndTheirMembers_3()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 : ITest33
{
    void M2();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58281"")]
public interface ITest35 : ITest34
{
    void M3();
}";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest35 x)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    Assert.Equal(0, itest33.GetMembers().Length);

                    var itest34 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest34").Single();
                    Assert.Equal(0, itest34.GetMembers().Length);

                    var itest35 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest35").Single();
                    Assert.Equal(0, itest35.GetMembers().Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }
} 

class UsePia7 : ITest33
{
    void ITest33.M1(){}
}
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(1, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    var m1 = (PEMethodSymbol)itest33.GetMembers("M1").Single();

                    var usePia7 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("UsePia7").Single();
                    var m1Impl = (PEMethodSymbol)usePia7.GetMembers("ITest33.M1").Single();

                    Assert.Same(m1, m1Impl.ExplicitInterfaceImplementations[0]);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DynamicIndexing_1()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    int this[int x] { get; set; }
    int this[long x] { get; set; }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest33 x, dynamic y)
    {
	var z = x[y];
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true), CSharpRef });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true), CSharpRef });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(3, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    Assert.Equal(2, itest33.GetMembers("this[]").Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DynamicIndexing_2()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    int this[int x] { get; set; }
    int this[long x] { get; set; }
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest33 x, dynamic y)
    {
	x[y] = 1;
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true), CSharpRef });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true), CSharpRef });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(3, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    Assert.Equal(2, itest33.GetMembers("this[]").Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DynamicInvocation()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1(int x);
    void M1(long x);
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(ITest33 x, dynamic y)
    {
	x.M1(y);
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true), CSharpRef });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true), CSharpRef });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(3, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    Assert.Equal(2, itest33.GetMembers("M1").Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void DynamicCollectionInitializer()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
[CoClass(typeof(ClassITest33))]
public interface ITest33 : System.Collections.IEnumerable
{
    void Add(int x);
    void Add(long x);
}

[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58278"")]
public abstract class ClassITest33
{
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void M1(dynamic y)
    {
	var z = new ITest33  { y };
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true), CSharpRef });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true), CSharpRef });

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    Assert.Equal(3, module.GetReferencedAssemblySymbols().Length);

                    var itest33 = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("ITest33").Single();
                    Assert.Equal(2, itest33.GetMembers("Add").Length);
                };

            CompileAndVerify(compilation1, symbolValidator: metadataValidator);

            CompileAndVerify(compilation2, symbolValidator: metadataValidator);
        }

        [Fact]
        public void ErrorType1()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 : ITest33
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            CompileAndVerify(piaCompilation2);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
    }
} 
";

            DiagnosticDescription[] expected = {
                // error CS1748: Cannot find the interop type that matches the embedded interop type 'ITest33'. Are you missing an assembly reference?
                Diagnostic(ErrorCode.ERR_NoCanonicalView).WithArguments("ITest33")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, false, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, false, expected);
        }

        [Fact]
        public void ErrorType2()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(ITest33), typeof(int))]
public interface ITest34
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            CompileAndVerify(piaCompilation2);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
    }
} 
";
            DiagnosticDescription[] expected = {
                // error CS1748: Cannot find the interop type that matches the embedded interop type 'ITest33'. Are you missing an assembly reference?
                Diagnostic(ErrorCode.ERR_NoCanonicalView).WithArguments("ITest33")
                                               };

            var fullName = MetadataTypeName.FromFullName("ITest33");
            bool isNoPiaLocalType;

            var compilation1 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });

            Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(compilation1.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0].LookupTopLevelMetadataType(ref fullName));
            Assert.Null(compilation1.SourceModule.GetReferencedAssemblySymbols()[1].GetTypeByMetadataName(fullName.FullName));

            VerifyEmitDiagnostics(compilation1, false, expected);

            var compilation2 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });

            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(((PEModuleSymbol)compilation2.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0]).LookupTopLevelMetadataType(ref fullName, out isNoPiaLocalType));
            Assert.True(isNoPiaLocalType);
            Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(compilation2.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0].LookupTopLevelMetadataType(ref fullName));
            Assert.Null(compilation2.SourceModule.GetReferencedAssemblySymbols()[1].GetTypeByMetadataName(fullName.FullName));

            VerifyEmitDiagnostics(compilation2, false, expected);

            var compilation3 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2) });

            Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(compilation3.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0].LookupTopLevelMetadataType(ref fullName));
            Assert.Null(compilation3.SourceModule.GetReferencedAssemblySymbols()[1].GetTypeByMetadataName(fullName.FullName));

            CompileAndVerify(compilation3);

            var compilation4 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { MetadataReference.CreateFromStream(piaCompilation2.EmitToStream()) });

            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(((PEModuleSymbol)compilation4.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0]).LookupTopLevelMetadataType(ref fullName, out isNoPiaLocalType));
            Assert.True(isNoPiaLocalType);
            Assert.IsType<MissingMetadataTypeSymbol.TopLevel>(compilation4.SourceModule.GetReferencedAssemblySymbols()[1].Modules[0].LookupTopLevelMetadataType(ref fullName));
            Assert.Null(compilation4.SourceModule.GetReferencedAssemblySymbols()[1].GetTypeByMetadataName(fullName.FullName));

            CompileAndVerify(compilation4);
        }

        [Fact]
        public void ErrorType3()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.DebugDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(ITest33), typeof(int))]
public interface ITest34
{
    void M2();
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.DebugDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            //CompileAndVerify(piaCompilation2, emitOptions: EmitOptions.RefEmitBug);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1()
    {
        ITest34 y = null;
        y.M2();
    }
} 
";

            DiagnosticDescription[] expected = {
                // (10,17): error CS1748: Cannot find the interop type that matches the embedded interop type 'ITest33'. Are you missing an assembly reference?
                //         ITest34 y = null;
                Diagnostic(ErrorCode.ERR_NoCanonicalView, "y = null").WithArguments("ITest33")
            };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, true, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, true, expected);

            var compilation3 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2) });
            CompileAndVerify(compilation3, verify: Verification.FailsPEVerify);

            var compilation4 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { MetadataReference.CreateFromStream(piaCompilation2.EmitToStream()) });
            CompileAndVerify(compilation4, verify: Verification.FailsPEVerify);
        }

        [Fact]
        public void ErrorType4()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(IList<List<ITest33>>), typeof(int))]
public interface ITest34
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            CompileAndVerify(piaCompilation2);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
    }
} 
";

            DiagnosticDescription[] expected = {
                // error CS1769: Type 'System.Collections.Generic.List<ITest33>' from assembly 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies).WithArguments("System.Collections.Generic.List<ITest33>", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, false, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, false, expected);
        }

        [Fact]
        public void ErrorType5()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.DebugDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(IList<List<ITest33>>), typeof(int))]
public interface ITest34
{
    void M2();
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.DebugDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1()
    {
        ITest34 y = null;
        y.M2();
    }
} 
";

            DiagnosticDescription[] expected = {
                // (10,17): error CS1769: Type 'System.Collections.Generic.List<ITest33>' from assembly 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
                //         ITest34 y = null;
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "y = null").WithArguments("System.Collections.Generic.List<ITest33>", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, true, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, true, expected);
        }

        [Fact]
        public void ErrorType6()
        {
            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 : ITest33
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2");

            //CompileAndVerify(piaCompilation2, emitOptions: EmitOptions.RefEmitBug);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            compilation1.VerifyEmitDiagnostics(
                // error CS0246: The type or namespace name 'ITest33' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("ITest33").WithLocation(1, 1),
                // error CS0246: The type or namespace name 'ITest33' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("ITest33").WithLocation(1, 1),
                // error CS0246: The type or namespace name 'ITest33' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("ITest33").WithLocation(1, 1));
        }

        [Fact]
        public void ErrorType7()
        {
            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34 
{
    void M2(ITest33 x);
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2");

            //CompileAndVerify(piaCompilation2, emitOptions: EmitOptions.RefEmitBug);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
        y.M2(null);
    }
} 
";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });

            DiagnosticDescription[] expected =
            {
                // (10,9): error CS0246: The type or namespace name 'ITest33' could not be found (are you missing a using directive or an assembly reference?)
                //         y.M2(null);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "y.M2").WithArguments("ITest33").WithLocation(10, 9)
            };

            VerifyEmitDiagnostics(compilation1, true, expected);
        }

        [Fact]
        public void ErrorType8()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(List<ITest33>), typeof(int))]
public interface ITest34
{
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            CompileAndVerify(piaCompilation2);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1(ITest34 y)
    {
    }
} 
";

            DiagnosticDescription[] expected = {
                // error CS1769: Type 'System.Collections.Generic.List<ITest33>' from assembly 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies).WithArguments("System.Collections.Generic.List<ITest33>", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                                               };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, false, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, false, expected);

            var compilation3 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2) });
            CompileAndVerify(compilation3);

            var compilation4 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { MetadataReference.CreateFromStream(piaCompilation2.EmitToStream()) });
            CompileAndVerify(compilation4);
        }

        [Fact]
        public void ErrorType_Tuple()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilationWithMscorlib40(pia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest34
{
    List<(ITest33, ITest33)> M();
}
";

            var piaCompilation2 = CreateCompilationWithMscorlib40(pia2, options: TestOptions.ReleaseDll, assemblyName: "Pia2",
                references: new MetadataReference[] { piaCompilation1.EmitToImageReference(embedInteropTypes: true), SystemRuntimeFacadeRef, ValueTupleRef });

            CompileAndVerify(piaCompilation2);

            string consumer = @"
using System;
using System.Collections.Generic;

public class UsePia5 : ITest34
{
    public List<(ITest33, ITest33)> M()
    {
        throw new System.Exception();
    } 
}
";

            DiagnosticDescription[] expected = {
                // (5,24): error CS1769: Type 'List<ValueTuple<ITest33, ITest33>>' from assembly 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
                // public class UsePia5 : ITest34
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "ITest34").WithArguments("System.Collections.Generic.List<ValueTuple<ITest33, ITest33>>", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(5, 24)
            };

            var compilation1 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaCompilation2.ToMetadataReference(embedInteropTypes: true), piaCompilation1.ToMetadataReference(), ValueTupleRef, SystemRuntimeFacadeRef });
            VerifyEmitDiagnostics(compilation1, metadataOnlyShouldSucceed: false, expectedFullBuildDiagnostics: expected);

            var compilation2 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true), piaCompilation1.ToMetadataReference(), ValueTupleRef, SystemRuntimeFacadeRef });
            VerifyEmitDiagnostics(compilation2, metadataOnlyShouldSucceed: false, expectedFullBuildDiagnostics: expected);

            var compilation3 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaCompilation2.ToMetadataReference(), piaCompilation1.ToMetadataReference(), ValueTupleRef, SystemRuntimeFacadeRef });
            VerifyEmitDiagnostics(compilation3, metadataOnlyShouldSucceed: false, expectedFullBuildDiagnostics: expected);

            var compilation4 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(), piaCompilation1.ToMetadataReference(), ValueTupleRef, SystemRuntimeFacadeRef });
            VerifyEmitDiagnostics(compilation4, metadataOnlyShouldSucceed: false, expectedFullBuildDiagnostics: expected);
        }

        [Fact]
        public void ErrorType9()
        {
            string pia1 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA1.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
}
";

            var piaCompilation1 = CreateCompilation(pia1, options: TestOptions.DebugDll, assemblyName: "Pia1");
            CompileAndVerify(piaCompilation1);

            string pia2 = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

[assembly: ImportedFromTypeLib(""GeneralPIA2.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58290"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
[ComEventInterface(typeof(List<ITest33>), typeof(int))]
public interface ITest34
{
    void M2();
}
";

            var piaCompilation2 = CreateCompilation(pia2, options: TestOptions.DebugDll, assemblyName: "Pia2",
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation1, embedInteropTypes: true) });

            //CompileAndVerify(piaCompilation2, emitOptions: EmitOptions.RefEmitBug);

            string consumer = @"
class UsePia5 
{
    public static void Main()
    {
    }

    public void M1()
    {
        ITest34 y = null;
        y.M2();
    }
} 
";

            DiagnosticDescription[] expected = {
                // (10,17): error CS1769: Type 'System.Collections.Generic.List<ITest33>' from assembly 'Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
                //         ITest34 y = null;
                Diagnostic(ErrorCode.ERR_GenericsUsedAcrossAssemblies, "y = null").WithArguments("System.Collections.Generic.List<ITest33>", "Pia2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
            };

            var compilation1 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2, embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation1, true, expected);

            var compilation2 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { piaCompilation2.EmitToImageReference(embedInteropTypes: true) });
            VerifyEmitDiagnostics(compilation2, true, expected);

            var compilation3 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation2) });
            CompileAndVerify(compilation3, verify: Verification.FailsPEVerify);

            var compilation4 = CreateCompilation(consumer, options: TestOptions.DebugExe,
                references: new MetadataReference[] { MetadataReference.CreateFromStream(piaCompilation2.EmitToStream()) });
            CompileAndVerify(compilation4, verify: Verification.FailsPEVerify);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = ConditionalSkipReason.NoPiaNeedsDesktop)]
        [WorkItem(611578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611578")]
        public void Bug611578()
        {
            string IEvent_cs = @"
using System;
using System.Runtime.InteropServices;

[assembly: ImportedFromTypeLib(""NoPiaTest"")]
[assembly: Guid(""ECED788D-2448-447A-B786-1646D2DBEEEE"")]

public delegate void EventDelegate01(ref bool p);
public delegate string EventDelegate02(string p);

/// <summary>
/// Source Interface
/// </summary>
[ComImport, Guid(""904458F3-005B-4DFD-8581-E9832DDFA433"")]
public interface IEventsBase
{
    [DispId(101), PreserveSig]
    void MyEvent01(ref bool p);
}

[ComImport, Guid(""904458F3-005B-4DFD-8581-E9832D7DA433"")]
public interface IEventsDerived : IEventsBase
{
    [DispId(102), PreserveSig]
    string MyEvent02(string p);
}

/// <summary>
/// Event Interface
/// </summary>
[ComEventInterface(typeof(IEventsDerived), typeof(int))]
[ComVisible(false)]
public interface IEventsDerived_Event
{
    event EventDelegate01 MyEvent01;
    event EventDelegate02 MyEvent02;
}
";

            var IEvent_Compilation = CreateCompilation(IEvent_cs, options: TestOptions.ReleaseDll, assemblyName: "IEvent");

            CompileAndVerify(IEvent_Compilation);
            var IEvent_Metadata = AssemblyMetadata.CreateFromImage(IEvent_Compilation.EmitToArray());

            string NetImpl_cs = @"
using System;
using System.Collections;
using System.Collections.Generic;

public class NetImpl : IEventsDerived_Event
{
    // Unique keys for events
    static readonly object[] myEventKeyList = new object[] { new object(), new object() };
    Hashtable eventTable = new Hashtable();

    #region Shared Func
    // return event handle associated with the key
    protected Delegate GetEventHandlerDelegate(int index)
    {
        object key = myEventKeyList[index];
        return eventTable[key] as Delegate;
    }

    // add event handle associated with the key
    protected void AddEventHandlerDelegate(int index, Delegate handler)
    {
        lock (eventTable)
        {
            object key = myEventKeyList[index];
            switch (index)
            {
                case 0:
                    eventTable[key] = (EventDelegate01)eventTable[key] + (EventDelegate01)handler;
                    break;
                case 1:
                    eventTable[key] = (EventDelegate02)eventTable[key] + (EventDelegate02)handler;
                    break;
            }
        }
    }

    // remove event handle associated with the key
    protected void RemoveEventHandlerDelegate(int index, Delegate handler)
    {
        lock (eventTable)
        {
            object key = myEventKeyList[index];
            switch (index)
            {
                case 0:
                    eventTable[key] = (EventDelegate01)eventTable[key] - (EventDelegate01)handler;
                    break;
                case 1:
                    eventTable[key] = (EventDelegate02)eventTable[key] - (EventDelegate02)handler;
                    break;
            }
        }
    }
    #endregion

    #region Impl Event
    event EventDelegate01 IEventsDerived_Event.MyEvent01
    {
        add { AddEventHandlerDelegate(0, value); }
        remove { RemoveEventHandlerDelegate(0, value); }
    }

    event EventDelegate02 IEventsDerived_Event.MyEvent02
    {
        add { AddEventHandlerDelegate(1, value); }
        remove { RemoveEventHandlerDelegate(1, value); }
    }

    #endregion

    #region Fire Event

    public void Fire01(ref bool arg, int idx = 0)
    {
        EventDelegate01 e = GetEventHandlerDelegate(idx) as EventDelegate01;
        if (null != e)
            e(ref arg);
    }

    public string Fire02(string arg, int idx = 1)
    {
        EventDelegate02 e = GetEventHandlerDelegate(idx) as EventDelegate02;
        if (null != e)
            return e(arg);
        return String.Empty;
    }

    #endregion
}
";

            System.Action<ModuleSymbol> metadataValidator =
                delegate (ModuleSymbol module)
                {
                    ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();

                    var IEventsBase = (PENamedTypeSymbol)module.GlobalNamespace.GetTypeMembers("IEventsBase").Single();
                    Assert.Equal(1, IEventsBase.GetMembers("MyEvent01").Length);
                };

            var NetImpl_1_Compilation = CreateCompilation(NetImpl_cs, new[] { new CSharpCompilationReference(IEvent_Compilation, embedInteropTypes: true) }, options: TestOptions.ReleaseDll, assemblyName: "NetImpl");

            CompileAndVerify(NetImpl_1_Compilation, symbolValidator: metadataValidator);
            var NetImpl_1_Image = NetImpl_1_Compilation.EmitToStream();

            var NetImpl_2_Compilation = CreateCompilation(NetImpl_cs, new[] { IEvent_Metadata.GetReference(embedInteropTypes: true) }, options: TestOptions.ReleaseDll, assemblyName: "NetImpl");

            CompileAndVerify(NetImpl_2_Compilation, symbolValidator: metadataValidator);
            var NetImpl_2_Image = NetImpl_2_Compilation.EmitToStream();

            string App_cs = @"
using System;

class Test
{
    public static void Main()
    {
        var obj = new NetImpl();
        var d1 = false;
        dynamic d2 = ""123"";
        // cast to interface
        IEventsDerived_Event iobj = obj;

        // Event 1
        iobj.MyEvent01 += new EventDelegate01(MyEvent01Handler);
        obj.Fire01(ref d1);

        // Event 2
        iobj.MyEvent02 += new EventDelegate02(MyEvent02Handler);
        obj.Fire02(d2);
    }

    #region Event Handlers

    static void MyEvent01Handler(ref bool arg)
    {
        Console.WriteLine(""E01"");
        arg = true;
    }

    static string MyEvent02Handler(string arg)
    {
        Console.WriteLine(""E02"");
        return arg;
    }

    #endregion
}
";

            MetadataReference[] NetImpl_refs = new MetadataReference[] { new CSharpCompilationReference(NetImpl_1_Compilation),
                                                                         new CSharpCompilationReference(NetImpl_2_Compilation),
                                                                         MetadataReference.CreateFromStream(NetImpl_1_Image),
                                                                         MetadataReference.CreateFromStream(NetImpl_2_Image)};

            MetadataReference[] IEvent_refs = new MetadataReference[] { new CSharpCompilationReference(IEvent_Compilation),
                                                                        new CSharpCompilationReference(IEvent_Compilation, embedInteropTypes: true),
                                                                        IEvent_Metadata.GetReference(),
                                                                        IEvent_Metadata.GetReference(embedInteropTypes: true)};

            foreach (var NetImpl_ref in NetImpl_refs)
            {
                foreach (var IEvent_ref in IEvent_refs)
                {
                    var app_compilation = CreateCompilation(App_cs, new[] { NetImpl_ref, IEvent_ref, CSharpRef }, options: TestOptions.ReleaseExe, assemblyName: "App");

                    CompileAndVerify(app_compilation, symbolValidator: IEvent_ref.Properties.EmbedInteropTypes ? metadataValidator : null,
                        expectedOutput: @"E01
E02");
                }
            }
        }

        [Fact, WorkItem(651240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651240")]
        public void Bug651240()
        {
            string pia = @"
using System;using System.Runtime.InteropServices; 
[assembly: ImportedFromTypeLib(""NoPiaTest"")]
[assembly: Guid(""A55E0B17-2558-447D-B786-84682CBEF136"")]
[assembly: BestFitMapping(false)] 

public interface IMyInterface
{
    void Method(int n);
}
 
public delegate void DelegateWithInterface(IMyInterface value);
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);
            CompileAndVerify(piaCompilation);

            string consumer = @"
namespace NoPiaTestApp
{
    class Test
    {
        public event DelegateWithInterface e3;          
        static void Main()
        {
        }

    }
}";

            var compilation1 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });


            var compilation2 = CreateCompilation(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { piaCompilation.EmitToImageReference(embedInteropTypes: true) });

            DiagnosticDescription[] expected = {
                // (6,44): error CS1756: Interop type 'IMyInterface' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
                //         public event DelegateWithInterface e3;          
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute, "e3").WithArguments("IMyInterface", "System.Runtime.InteropServices.ComImportAttribute"),
                // (6,44): warning CS0067: The event 'NoPiaTestApp.Test.e3' is never used
                //         public event DelegateWithInterface e3;          
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e3").WithArguments("NoPiaTestApp.Test.e3")
                                               };

            DiagnosticDescription[] expectedMEtadataOnly = {
                // error CS1756: Interop type 'IMyInterface' cannot be embedded because it is missing the required 'System.Runtime.InteropServices.ComImportAttribute' attribute.
                Diagnostic(ErrorCode.ERR_InteropTypeMissingAttribute).WithArguments("IMyInterface", "System.Runtime.InteropServices.ComImportAttribute")
                                               };

            VerifyEmitDiagnostics(compilation1, false, expected, expectedMEtadataOnly);
            VerifyEmitDiagnostics(compilation2, false, expected, expectedMEtadataOnly);
        }

        [Fact, WorkItem(651408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651408")]
        public void Bug651408()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
 
[assembly: ImportedFromTypeLib(""NoPiaTest"")]
[assembly: Guid(""ECED788D-2448-447A-B786-64682CBECC40"")]
 
namespace EventNS
{
    /// <summary>
    /// Source Interface
    /// </summary>
    [ComImport, Guid(""904458F3-005B-4DFD-8581-E9832D7FA440"")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch), TypeLibType(TypeLibTypeFlags.FDual)]
    public interface IEvents
    {
        [DispId(101), PreserveSig]
        void OnEvent01();
    }
 
    /// <summary>
    /// Event Interface
    /// </summary>
    [ComEventInterface(typeof(IEvents), typeof(int))]
    [ComVisible(false), TypeLibType(TypeLibTypeFlags.FHidden)]
    public interface IEvents_Event
    {
        event OnEvent01EventHandler OnEvent01;
        event OnEvent02EventHandler OnEvent02;
    }
 
    /// <summary>
    /// delegate
    /// </summary>
    public delegate void OnEvent01EventHandler();
    public delegate void OnEvent02EventHandler(object i1);
}
";

            var piaCompilation = CreateCompilation(pia, options: TestOptions.ReleaseDll);
            CompileAndVerify(piaCompilation);

            var piaRef1 = new CSharpCompilationReference(piaCompilation, embedInteropTypes: true);
            var piaRef2 = piaCompilation.EmitToImageReference(embedInteropTypes: true);

            string consumer0 = @"
namespace NetImplNS
{
    public class NetImpl : EventNS.IEvents_Event
    {
        public event EventNS.OnEvent01EventHandler OnEvent01;
        public event EventNS.OnEvent02EventHandler OnEvent02;
 
        public void Fire1()
        {
            if (OnEvent01 != null) OnEvent01();
        }
        public void Fire2(object obj)
        {
            if (OnEvent02 != null) OnEvent02(obj);
        }
    }
}
";

            var compilation0 = CreateCompilation(consumer0, options: TestOptions.ReleaseDll, references: new MetadataReference[] { piaRef1 });

            System.Action<ModuleSymbol> symbolValidator = m =>
                {
                    Assert.Equal("void EventNS.IEvents.OnEvent01()", m.GlobalNamespace.GetMember<NamespaceSymbol>("EventNS").GetMember<NamedTypeSymbol>("IEvents").GetMember<MethodSymbol>("OnEvent01").ToTestDisplayString());
                };

            CompileAndVerify(compilation0, symbolValidator: symbolValidator);

            compilation0 = CreateCompilation(consumer0, options: TestOptions.ReleaseDll, references: new MetadataReference[] { piaRef2 });
            CompileAndVerify(compilation0, symbolValidator: symbolValidator);

            string consumer2 = consumer0 + @"
namespace NetImplNS2
{
    public class NetImpl
    {
        public void Fire1(EventNS.IEvents_Event x)
        {
            x.OnEvent02 += null;
        }

        public void Fire2(EventNS.IEvents_Event y)
        {
            y.OnEvent02 += null; 
        }
    }
}";

            var compilation1 = CreateCompilation(consumer2, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaRef1 });


            var compilation2 = CreateCompilation(consumer2, options: TestOptions.ReleaseDll,
                references: new MetadataReference[] { piaRef2 });

            DiagnosticDescription[] expected = {
                // (10,13): error CS1766: Source interface 'EventNS.IEvents' is missing method 'OnEvent02' which is required to embed event 'EventNS.IEvents_Event.OnEvent02'.
                //             x.OnEvent02 += null;
                Diagnostic(ErrorCode.ERR_MissingMethodOnSourceInterface, "x.OnEvent02 += null").WithArguments("EventNS.IEvents", "OnEvent02", "EventNS.IEvents_Event.OnEvent02"),
                                               };

            VerifyEmitDiagnostics(compilation1, true, expected);
            VerifyEmitDiagnostics(compilation2, true, expected);
        }

        [Fact, WorkItem(673546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/673546")]
        public void MissingComAwareEventInfo()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public delegate void MyDelegate();

[ComEventInterface(typeof(InterfaceEvents), typeof(int))]
public interface Interface1_Event
{
    event MyDelegate Goo;
}

[ComImport(), Guid(""84374891-a3b1-4f8f-8310-99ea58059b10"")]
public interface InterfaceEvents
{
    void Goo();
}
";

            var piaCompilation = CreateCompilationWithMscorlib40(pia, options: TestOptions.ReleaseDll, assemblyName: "Pia");

            CompileAndVerify(piaCompilation);

            string consumer = @"
class UsePia
{
    public static void Main()
    {
    }

    public void Test(Interface1_Event x)
    {
    	x.Goo += Handler;	
    }

    void Handler()
    {}
}
";

            var compilation1 = CreateCompilationWithMscorlib40(consumer, options: TestOptions.ReleaseExe,
                references: new MetadataReference[] { new CSharpCompilationReference(piaCompilation, embedInteropTypes: true) });

            DiagnosticDescription[] expected = {
                // (10,6): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.ComAwareEventInfo..ctor'
                //     	x.Goo += Handler;	
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.Goo += Handler").WithArguments("System.Runtime.InteropServices.ComAwareEventInfo", ".ctor").WithLocation(10, 6)
                                               };

            VerifyEmitDiagnostics(compilation1, true, expected);
        }

        [Fact, WorkItem(2793, "https://github.com/dotnet/roslyn/issues/2793")]
        public void DefaultValueWithoutOptional_01()
        {
            var il = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
.class interface public abstract auto ansi import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 45 41 44 42 45 45 46 2D 43 41 46 45   // ..$DEADBEEF-CAFE
                                                                                                  2D 42 41 42 45 2D 42 41 41 44 2D 44 45 41 44 43   // -BABE-BAAD-DEADC
                                                                                                  30 44 45 30 30 30 30 00 00 )                      // 0DE0000..
  .method public newslot abstract strict virtual 
          instance void  M(int32 x) cil managed
  {
    .param [1] = int32(0x0000000C)
  } // end of method IA::M
} // end of class IA
";
            MetadataReference piaReference = CompileIL(il, prependDefaultHeader: false, embedInteropTypes: true);
            var csharp = @"
class B : IA
{
    public void M(int x)
    {
    }
}
";
            CompileAndVerify(csharp, references: new MetadataReference[] { piaReference }, symbolValidator: module =>
            {
                ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();
                var ia = module.GlobalNamespace.GetMember<NamedTypeSymbol>("IA");
                var m = (MethodSymbol)ia.GetMember("M");
                var p = (PEParameterSymbol)m.Parameters[0];
                Assert.False(p.IsMetadataOptional);
                Assert.Equal(ParameterAttributes.HasDefault, p.Flags);
                Assert.Equal((object)0x0000000C, p.ExplicitDefaultConstantValue.Value);
                Assert.False(p.HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(delegate
                    {
                        var tmp = p.ExplicitDefaultValue;
                    });
            }).VerifyDiagnostics();
        }

        [Fact, WorkItem(2793, "https://github.com/dotnet/roslyn/issues/2793")]
        public void DefaultValueWithoutOptional_02()
        {
            var il = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly pia
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ImportedFromTypeLibAttribute::.ctor(string) = ( 01 00 0E 47 65 6E 65 72 61 6C 50 49 41 2E 64 6C   // ...GeneralPIA.dl
                                                                                                                 6C 00 00 )                                        // l..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 39 63 32 64 35 31 64 2D 34 66 34 34   // ..$f9c2d51d-4f44
                                                                                                  2D 34 35 66 30 2D 39 65 64 61 2D 63 39 64 35 39   // -45f0-9eda-c9d59
                                                                                                  39 62 35 38 32 35 37 00 00 )                      // 9b58257..
}
.module pia.dll
// MVID: {FDF1B1F7-A867-40B9-83CD-3F75B2D2B3C2}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
.class interface public abstract auto ansi import IA
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 45 41 44 42 45 45 46 2D 43 41 46 45   // ..$DEADBEEF-CAFE
                                                                                                  2D 42 41 42 45 2D 42 41 41 44 2D 44 45 41 44 43   // -BABE-BAAD-DEADC
                                                                                                  30 44 45 30 30 30 30 00 00 )                      // 0DE0000..
  .method public newslot abstract strict virtual 
          instance void  M(valuetype [mscorlib]System.DateTime x) cil managed
  {
  .param [1]
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 B1 68 DE 3A 00 00 00 00 00 00 )             // ...h.:......
  } // end of method IA::M
} // end of class IA
";
            MetadataReference piaReference = CompileIL(il, prependDefaultHeader: false, embedInteropTypes: true);
            var csharp = @"
class B : IA
{
    public void M(System.DateTime x)
    {
    }
}
";
            CompileAndVerify(csharp, references: new MetadataReference[] { piaReference }, symbolValidator: module =>
            {
                ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();
                var ia = module.GlobalNamespace.GetMember<NamedTypeSymbol>("IA");
                var m = (MethodSymbol)ia.GetMember("M");
                var p = (PEParameterSymbol)m.Parameters[0];
                Assert.False(p.IsMetadataOptional);
                Assert.Equal(ParameterAttributes.None, p.Flags);
                Assert.Equal("System.Runtime.CompilerServices.DateTimeConstantAttribute(987654321)", p.GetAttributes().Single().ToString());
                Assert.Null(p.ExplicitDefaultConstantValue);
                Assert.False(p.HasExplicitDefaultValue);
                Assert.Throws<InvalidOperationException>(delegate
                {
                    var tmp = p.ExplicitDefaultValue;
                });
            }).VerifyDiagnostics();
        }

        [Fact, WorkItem(8088, "https://github.com/dotnet/roslyn/issues/8088")]
        public void ParametersWithoutNames()
        {
            var source = @"
class Program
{
    public void M(I1 x)
    {
        x.M1(1, 2, 3);
    }

    public void M1(int value)
    {
    }

    public void M2(int Param)
    {
    }
}
";
            var compilation = CreateCompilation(source,
                             references: new MetadataReference[]
                                {
                                    AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.ParametersWithoutNames).
                                        GetReference(display: "ParametersWithoutNames.dll", embedInteropTypes:true)
                                },
                             options: TestOptions.ReleaseDll);

            AssertParametersWithoutNames(compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("I1").GetMember<MethodSymbol>("M1").Parameters, false);

            CompileAndVerify(compilation,
                             symbolValidator: module =>
                             {
                                 ((PEModuleSymbol)module).Module.PretendThereArentNoPiaLocalTypes();
                                 AssertParametersWithoutNames(module.GlobalNamespace.GetMember<NamedTypeSymbol>("I1").GetMember<MethodSymbol>("M1").Parameters, true);

                                 PEParameterSymbol p;
                                 p = (PEParameterSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M").Parameters[0];
                                 Assert.Equal("x", ((PEModuleSymbol)module).Module.GetParamNameOrThrow(p.Handle));
                                 Assert.Equal("x", p.Name);
                                 Assert.Equal("x", p.MetadataName);
                                 p = (PEParameterSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M1").Parameters[0];
                                 Assert.Equal("value", ((PEModuleSymbol)module).Module.GetParamNameOrThrow(p.Handle));
                                 Assert.Equal("value", p.Name);
                                 Assert.Equal("value", p.MetadataName);
                                 p = (PEParameterSymbol)module.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M2").Parameters[0];
                                 Assert.Equal("Param", ((PEModuleSymbol)module).Module.GetParamNameOrThrow(p.Handle));
                                 Assert.Equal("Param", p.Name);
                                 Assert.Equal("Param", p.MetadataName);
                             }).VerifyDiagnostics();
        }

        private static void AssertParametersWithoutNames(ImmutableArray<ParameterSymbol> parameters, bool isEmbedded)
        {
            Assert.True(((PEParameterSymbol)parameters[0]).Handle.IsNil);

            var p1 = (PEParameterSymbol)parameters[1];
            Assert.True(p1.IsMetadataOptional);
            Assert.False(p1.Handle.IsNil);
            Assert.True(((PEModuleSymbol)p1.ContainingModule).Module.MetadataReader.GetParameter(p1.Handle).Name.IsNil);

            var p2 = (PEParameterSymbol)parameters[2];
            if (isEmbedded)
            {
                Assert.True(p2.Handle.IsNil);
            }
            else
            {
                Assert.True(((PEModuleSymbol)p2.ContainingModule).Module.MetadataReader.GetParameter(p2.Handle).Name.IsNil);
            }

            foreach (var p in parameters)
            {
                Assert.Equal("value", p.Name);
                Assert.Equal("", p.MetadataName);
            }
        }
    }
}
#endif
