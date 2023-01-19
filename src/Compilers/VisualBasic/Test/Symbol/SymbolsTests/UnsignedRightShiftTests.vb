' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class UnsignedRightShiftTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Consume_01()

            Dim csSource =
"
public class C1
{
    public static C1 operator >>>(C1 x, int y)
    {
        System.Console.WriteLine("">>>"");
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
        Dim x = new C1() >> 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:=">>>").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Consume_02()

            Dim csSource =
"
public class C1
{
    public static C1 operator >>>(C1 x, int y)
    {
        System.Console.WriteLine("">>>"");
        return x;
    }
    public static C1 operator >>(C1 x, int y)
    {
        System.Console.WriteLine("">>"");
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
        Dim x = new C1() >> 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)
            CompileAndVerify(comp1, expectedOutput:=">>").VerifyDiagnostics()
        End Sub

    End Class
End Namespace
