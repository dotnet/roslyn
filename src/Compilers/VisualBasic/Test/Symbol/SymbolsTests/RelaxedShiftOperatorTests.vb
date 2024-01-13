' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class RelaxedShiftOperatorTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Consumption_01()

            Dim csSource =
"
public class C1
{
    public static C1 operator <<(C1 x, C1 y)
    {
        System.Console.WriteLine(""<<"");
        return x;
    }
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
        Dim c1 As New C1()
        Dim x = c1 << c1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors><![CDATA[
BC30452: Operator '<<' is not defined for types 'C1' and 'C1'.
        Dim x = c1 << c1
                ~~~~~~~~
]]></errors>
            )

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim c1 As New C1()
        C1.op_LeftShift(c1, c1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, references:={csCompilation}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp2, expectedOutput:="<<").VerifyDiagnostics()
        End Sub

    End Class
End Namespace
