' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ParamsCollectionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Consume_01()

            Dim csSource =
"
public class C1
{
    public static void M1(params System.Collections.Generic.IEnumerable<int> a) {}
    public static void M2(System.Collections.Generic.IEnumerable<int> a) {}
    public static void M3(params int[] a) {}
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        C1.M1(1, 2, 3)
        C1.M1({1, 2, 3})

        C1.M2({1, 2, 3})
        C1.M3(1, 2, 3)
        C1.M3({1, 2, 3})
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'IEnumerable(Of Integer)'.
        C1.M1(1, 2, 3)
              ~
BC30057: Too many arguments to 'Public Shared Overloads Sub M1(a As IEnumerable(Of Integer))'.
        C1.M1(1, 2, 3)
                 ~
</expected>
            )
        End Sub

        <Fact>
        Public Sub SymbolDisplay_01()

            Dim csSource =
"
public class C1
{
    public static void M1(params System.Collections.Generic.IEnumerable<int> a) {}
    public static void M2(params int[] a) {}
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard))

            Dim m1 = DirectCast(csCompilation, Compilation).GetTypeByMetadataName("C1").GetMembers().Where(Function(s) s.Name = "M1").Single()
            Dim m2 = DirectCast(csCompilation, Compilation).GetTypeByMetadataName("C1").GetMembers().Where(Function(s) s.Name = "M2").Single()

            AssertEx.Equal("Public Shared Sub M1(a As System.Collections.Generic.IEnumerable(Of Integer))", SymbolDisplay.ToDisplayString(m1))
            AssertEx.Equal("Public Shared Sub M2(ParamArray a As Integer())", SymbolDisplay.ToDisplayString(m2))

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation.EmitToImageReference()}, options:=TestOptions.DebugExe)

            AssertEx.Equal("Sub C1.M1(a As System.Collections.Generic.IEnumerable(Of System.Int32))", comp1.GetMember("C1.M1").ToTestDisplayString())
            AssertEx.Equal("Sub C1.M2(ParamArray a As System.Int32())", comp1.GetMember("C1.M2").ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub PublicApi_01()

            Dim csSource =
"
public class C1
{
    public static void M1(params System.Collections.Generic.IEnumerable<int> a) {}
    public static void M2(System.Collections.Generic.IEnumerable<int> a) {}
    public static void M3(params int[] a) {}
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C2
    Shared Sub M4(ParamArray b as Integer())
    End Sub
    Shared Sub M5(b as Integer())
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugDll)

            VerifyParams(comp1.GetMember("C1.M1").GetParameters().Last(), isParamArray:=False)
            VerifyParams(comp1.GetMember("C1.M2").GetParameters().Last(), isParamArray:=False)
            VerifyParams(comp1.GetMember("C1.M3").GetParameters().Last(), isParamArray:=True)
            VerifyParams(comp1.GetMember("C2.M4").GetParameters().Last(), isParamArray:=True)
            VerifyParams(comp1.GetMember("C2.M5").GetParameters().Last(), isParamArray:=False)
        End Sub

        Private Shared Sub VerifyParams(parameter As ParameterSymbol, isParamArray As Boolean)
            Assert.Equal(isParamArray, parameter.IsParamArray)

            Dim iParameter = DirectCast(parameter, IParameterSymbol)
            Assert.Equal(isParamArray, iParameter.IsParamsArray)
            Assert.False(iParameter.IsParamsCollection)
            Assert.Equal(isParamArray, iParameter.IsParams)
        End Sub

    End Class
End Namespace
