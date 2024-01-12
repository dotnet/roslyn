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
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            ' PROTOTYPE(ParamsCollections): It looks like we will be much safer to use a different attribute for non-array params collections in metadata.
            '                               The old VB compiler will not be able to consume them decorated with ParamArrayAttribute neither in normal, nor in expanded form.
            '                               Therefore, an addition of 'params' modifier is likely to be break VB consumers, and very likely consumers from other languages.
            '                               We possibly could fix up the new version of VB compiler, but I am not sure it would be worth the effort since we can
            '                               simply use a different attribute, which is likely to work better for other languages too.
            comp1.AssertTheseDiagnostics(
<expected>
BC31092: ParamArray parameters must have an array type.
        C1.M1(1, 2, 3)
           ~~
BC31092: ParamArray parameters must have an array type.
        C1.M1({1, 2, 3})
           ~~
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
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard))

            Dim m1 = DirectCast(csCompilation, Compilation).GetTypeByMetadataName("C1").GetMembers().Where(Function(s) s.Name = "M1").Single()

            AssertEx.Equal("Public Shared Sub M1(ParamArray a As System.Collections.Generic.IEnumerable(Of Integer))", SymbolDisplay.ToDisplayString(m1))

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

            AssertEx.Equal("Sub C1.M1(ParamArray a As System.Collections.Generic.IEnumerable(Of System.Int32))", comp1.GetMember("C1.M1").ToTestDisplayString())
        End Sub

    End Class
End Namespace
