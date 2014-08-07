' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities
Imports System.Xml.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class PDBConstLocalTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub TestSimpleLocalConstants()
            Dim source =
<compilation>
    <file>
Imports System                                 
Public Class C
    Public Sub M()
        const x as integer = 1
        const y as integer = 2
        Console.WriteLine(x + y)
    end sub
end class
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            Dim actual As XElement = GetPdbXml(compilation, "C.M")
            Dim expected = <symbols>
                               <methods>
                                   <method containingType="C" name="M" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="19" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="6" start_column="9" end_row="6" end_column="33" file_ref="0"/>
                                           <entry il_offset="0x8" start_row="7" start_column="5" end_row="7" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <constant name="x" value="1" type="Int32"/>
                                           <constant name="y" value="2" type="Int32"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x9">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <constant name="x" value="1" type="Int32"/>
                                           <constant name="y" value="2" type="Int32"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>
            AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestLambdaLocalConstants()
            Dim source =
<compilation>
    <file>
Imports System                                 
Public Class C
    Public Sub M(a as action)
        const x as integer = 1
        M(
            Sub()
                const y as integer = 2
                const z as integer = 3
                Console.WriteLine(x + y + z)
            end Sub
         )
    end sub
end class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll)

            Dim actual As XElement = GetPdbXml(compilation)
            Dim expected = <symbols>
                               <methods>
                                   <method containingType="C" name="M" parameterNames="a">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="30" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="5" start_column="9" end_row="11" end_column="11" file_ref="0"/>
                                           <entry il_offset="0x28" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <constant name="x" value="1" type="Int32"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x29">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <constant name="x" value="1" type="Int32"/>
                                       </scope>
                                   </method>
                                   <method containingType="C" name="_Lambda$__1" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" start_row="6" start_column="13" end_row="6" end_column="18" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="9" start_column="17" end_row="9" end_column="45" file_ref="0"/>
                                           <entry il_offset="0x8" start_row="10" start_column="13" end_row="10" end_column="20" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <constant name="y" value="2" type="Int32"/>
                                           <constant name="z" value="3" type="Int32"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x9">
                                           <importsforward declaringType="C" methodName="M" parameterNames="a"/>
                                           <constant name="y" value="2" type="Int32"/>
                                           <constant name="z" value="3" type="Int32"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>
            AssertXmlEqual(expected, actual)

            compilation.VerifyDiagnostics()
        End Sub

#If False Then
        <WorkItem(11017)>
        <Fact()>
        Public Sub TestIteratorLocalConstants()
            Dim text = <text>
using System.Collections.Generic;

class C
{
    IEnumerable&lt;int&gt; M()
    {
        const int x = 1;
        for (int i = 0; i &lt; 10; i++)
        {
            const int y = 2;
            yield return x + y + i;
        }
    }
}
</text>.Value

            AssertXmlEqual(expected, actual)
        End Sub
#End If

        <WorkItem(529101, "DevDiv")>
        <Fact()>
        Public Sub TestLocalConstantsTypes()
            Dim source = <compilation>
                             <file>
Imports System                                    
Public Class C
    Sub M()
        const o as object = nothing
        const s as string  = "hello"
        const f as single = single.MinValue
        const d as double = double.MaxValue
        const dec as decimal = 1.5D
        const dt as datetime = #2/29/2012#
    End Sub
End Class
</file>
                         </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll)

            compilation.VerifyDiagnostics(
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "o").WithArguments("o"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "s").WithArguments("s"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "f").WithArguments("f"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "d").WithArguments("d"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "dec").WithArguments("dec"),
                 Diagnostic(ERRID.WRN_UnusedLocalConst, "dt").WithArguments("dt"))

            Dim actual As XElement = GetPdbXml(compilation, "C.M")
            Dim invariantStr = actual.ToString()
            invariantStr = invariantStr.Replace("-3,402823E+38", "-3.402823E+38")
            invariantStr = invariantStr.Replace("1,79769313486232E+308", "1.79769313486232E+308")
            invariantStr = invariantStr.Replace("value=""1,5""", "value=""1.5""")
            Dim expected = <symbols>
                               <methods>
                                   <method containingType="C" name="M" parameterNames="">
                                       <sequencepoints total="2">
                                           <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="12" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="10" start_column="5" end_row="10" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <constant name="o" value="0" type="Int32"/>
                                           <constant name="s" value="hello" type="String"/>
                                           <constant name="f" value="-3.402823E+38" type="Single"/>
                                           <constant name="d" value="1.79769313486232E+308" type="Double"/>
                                           <constant name="dec" value="1.5" type="Decimal"/>
                                           <constant name="dt" value="40968" type="Double"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x2">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <constant name="o" value="0" type="Int32"/>
                                           <constant name="s" value="hello" type="String"/>
                                           <constant name="f" value="-3.402823E+38" type="Single"/>
                                           <constant name="d" value="1.79769313486232E+308" type="Double"/>
                                           <constant name="dec" value="1.5" type="Decimal"/>
                                           <constant name="dt" value="40968" type="Double"/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            'AssertXmlEqual(expected, actual)
            Assert.Equal(expected.ToString(), invariantStr)
        End Sub

    End Class
End Namespace


