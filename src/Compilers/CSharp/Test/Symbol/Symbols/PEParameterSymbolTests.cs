// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class PEParameterSymbolTests : CSharpTestBase
    {
#if !NETCOREAPP
        [Fact]
        public void NoParameterNames()
        {
            // Create simple interface where method parameters have no names.
            // interface I
            // {
            //   void M(object, object);
            // }
            var reference = Roslyn.Test.Utilities.Desktop.DesktopRuntimeUtil.CreateReflectionEmitAssembly(moduleBuilder =>
                {
                    var typeBuilder = moduleBuilder.DefineType(
                        "I",
                        TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract);
                    var methodBuilder = typeBuilder.DefineMethod(
                        "M",
                        MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
                        typeof(void),
                        new Type[] { typeof(object), typeof(object) });
                    methodBuilder.DefineParameter(1, ParameterAttributes.None, null);
                    methodBuilder.DefineParameter(2, ParameterAttributes.None, null);
                    typeBuilder.CreateType();
                });
            var source =
@"class C
{
    static void M(I o)
    {
        o.M(0, value: 2);
    }
}";
            var compilation = CreateCompilation(source, new[] { reference });
            compilation.VerifyDiagnostics(
                // (5,16): error CS1744: Named argument 'value' specifies a parameter for which a positional argument has already been given
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "value").WithArguments("value").WithLocation(5, 16));
        }
#endif

        [Fact]
        [WorkItem(8018, "https://github.com/dotnet/roslyn/issues/8018")]
        public void IsOptional()
        {
            var vbComp = CreateVisualBasicCompilation(@"
Public Class Class1
    Public Shared Sub Test(<System.Runtime.InteropServices.Out> Optional ByRef x As Object = Nothing,
                           Optional ByRef y As Object = Nothing, Optional z As Integer = -1)

    End Sub
End Class


<System.Runtime.InteropServices.ComImport>
<System.Runtime.InteropServices.Guid(""00C7DAA6-9F86-4F05-9876-D8136B2D2503"")>
Public Interface I1
    Sub M1(<System.Runtime.InteropServices.Out> Optional ByRef x1 As Object = Nothing)
    Sub M2(Optional ByRef y2 As Object = Nothing)
End Interface
").EmitToImageReference();

            var source =
@"
public class X
{
    public static void Main()
    {
        Class1.Test();

        I1 i1 = null;
        i1.M1();
        i1.M2();
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, new[] { vbComp }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (6,16): error CS7036: There is no argument given that corresponds to the required parameter 'x' of 'Class1.Test(out object, ref object, int)'
                //         Class1.Test();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Test").WithArguments("x", "Class1.Test(out object, ref object, int)").WithLocation(6, 16),
                // (9,12): error CS7036: There is no argument given that corresponds to the required parameter 'x1' of 'I1.M1(out object)'
                //         i1.M1();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M1").WithArguments("x1", "I1.M1(out object)").WithLocation(9, 12)
                );

            var m = compilation.GetMember<MethodSymbol>("Class1.Test");

            Assert.Equal("void Class1.Test(out System.Object x, ref System.Object y, [System.Int32 z = -1])", m.ToTestDisplayString());

            var x = m.Parameters[0];
            var y = m.Parameters[1];
            var z = m.Parameters[2];

            Assert.False(x.IsOptional);
            Assert.True(x.IsMetadataOptional);
            Assert.Equal(RefKind.Out, x.RefKind);

            Assert.False(y.IsOptional);
            Assert.True(y.IsMetadataOptional);
            Assert.Equal(RefKind.Ref, y.RefKind);

            Assert.True(z.IsOptional);
            Assert.True(z.IsMetadataOptional);
            Assert.Equal(RefKind.None, z.RefKind);

            var m1 = compilation.GetMember<MethodSymbol>("I1.M1");
            Assert.Equal("void I1.M1(out System.Object x1)", m1.ToTestDisplayString());
            var x1 = m1.Parameters[0];
            Assert.False(x1.IsOptional);
            Assert.True(x1.IsMetadataOptional);
            Assert.Equal(RefKind.Out, x1.RefKind);

            var m2 = compilation.GetMember<MethodSymbol>("I1.M2");
            Assert.Equal("void I1.M2([ref System.Object y2 = null])", m2.ToTestDisplayString());
            var y2 = m2.Parameters[0];
            Assert.True(y2.IsOptional);
            Assert.True(y2.IsMetadataOptional);
            Assert.Equal(RefKind.Ref, y2.RefKind);
        }
    }
}
