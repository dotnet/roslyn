' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports System.Xml.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBLambdaTests
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleLambda()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Delegate Function D() As Object

    Public Sub Main()
        Dim d as D = Function() 1
        d()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="E5, 7C, 24, B4, CD, 54, 7D, DA, 7A, 48, 2F, D1, A4, B6, D2, EB, 5C, 95, CA, B4, "/>
    </files>
    <methods>
        <method containingType="C" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>2</methodOrdinal>
                    <lambda offset="13"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="22" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="13" endLine="5" endColumn="34" document="1"/>
                <entry offset="0x26" startLine="6" startColumn="9" endLine="6" endColumn="12" document="1"/>
                <entry offset="0x2d" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2e">
                <currentnamespace name=""/>
                <local name="d" il_index="0" il_start="0x0" il_end="0x2e" attributes="0"/>
            </scope>
        </method>
        <method containingType="C+_Closure$__" name="_Lambda$__2-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="22" endLine="5" endColumn="32" document="1"/>
                <entry offset="0x1" startLine="5" startColumn="33" endLine="5" endColumn="34" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="C" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub LambdaMethod()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module M1
    Class C1(Of G)
        Public Sub Print(Of TPrint)(x As TPrint)
            Console.Write(x.ToString())
        End Sub

        Public Shared Sub PrintShared(Of TPrint)(x As TPrint, y As G)
            Console.Write(x.ToString())
            Console.Write(y.ToString())
        End Sub

        Public Sub Foo(Of TFun1, TFun2)(p As TFun1, p1 As TFun2, p3 As Integer)
            Dim d1 As Action(Of Integer, Integer) =
                Sub(lifted As Integer, notLifted As Integer)
                    Dim iii As Integer = lifted + notlifted
                    Console.WriteLine(iii)

                    Dim d2 As Action(Of TFun1) =
                        Sub(X As TFun1)
                            lifted = lifted + 1
                            C1(Of TFun2).PrintShared(Of TFun1)(X, p1)
                        End Sub

                    d2.Invoke(p)
                End Sub
            d1.Invoke(5, 5)
        End Sub
    End Class

    Public Sub Main()
        Dim inst As New C1(Of Integer)
        inst.Foo(Of Integer, Integer)(42, 333, 432)
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("M1+C1`1+_Closure$__3-1`2._Lambda$__0",
<symbols>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1+C1`1+_Closure$__3-1`2" name="_Lambda$__0" parameterNames="lifted, notLifted">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="30" offset="-1"/>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="111"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="17" endLine="16" endColumn="61" document="0"/>
                <entry offset="0x1" hidden="true" document="0"/>
                <entry offset="0x15" startLine="17" startColumn="25" endLine="17" endColumn="60" document="0"/>
                <entry offset="0x1e" startLine="18" startColumn="21" endLine="18" endColumn="43" document="0"/>
                <entry offset="0x25" startLine="20" startColumn="25" endLine="24" endColumn="32" document="0"/>
                <entry offset="0x32" startLine="26" startColumn="21" endLine="26" endColumn="33" document="0"/>
                <entry offset="0x3f" startLine="27" startColumn="17" endLine="27" endColumn="24" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x40">
                <importsforward declaringType="M1" methodName="Main"/>
                <local name="$VB$Closure_0" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="iii" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="d2" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        <WorkItem(544000, "DevDiv")>
        Public Sub TestLambdaNameStability()
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            Dim actual1 As XElement = GetPdbXml(compilation)
            Dim actual2 As XElement = GetPdbXml(compilation)
            AssertXml.Equal(actual1, actual2)
        End Sub

        <Fact>
        Public Sub TestFunctionValueLocalOfLambdas()
            Dim source =
            <compilation>
                <file>
Module Module1

    Sub Main()

        Dim x = Function()
                    dim r = 23
                    Return r
           End Function
    End Sub
End Module
</file>
            </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1+_Closure$__._Lambda$__0-0",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="17" endLine="5" endColumn="27" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="25" endLine="6" endColumn="31" document="0"/>
                <entry offset="0x4" startLine="7" startColumn="21" endLine="7" endColumn="29" document="0"/>
                <entry offset="0x8" startLine="8" startColumn="12" endLine="8" endColumn="24" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Main"/>
                <local name="r" il_index="1" il_start="0x0" il_end="0xa" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub PartiallydefinedClass_1()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Partial Class C
    Public m1 As Func(Of Integer) = Function() 1

    Sub Main()
    End Sub
End Class
    </file>
    <file name="b.vb">
Imports System
Partial Class C
    Public m2 As Func(Of Integer) = Function() 2
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            ' Check two distinct lambda offets for m1 and m2
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="E9, 8A, 62, CA, DC, E3, 2B, C4, 4B,  6, D5, 97, 3C, 77, 18, 2E, 6F, 67, EE, 15, "/>
        <file id="2" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="A1, 36, 22, 63, B1, FC, DD, 52, E1, 86, 92, E9, 1A, 7D, 68, 5A, C5, 74, 27, 69, "/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="-26"/>
                    <lambda offset="-12"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="3" startColumn="12" endLine="3" endColumn="49" document="1"/>
                <entry offset="0x30" startLine="3" startColumn="12" endLine="3" endColumn="49" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5b">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="C" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="5" endLine="6" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="37" endLine="3" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="48" endLine="3" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C+_Closure$__" name="_Lambda$__0-1">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="37" endLine="3" endColumn="47" document="2"/>
                <entry offset="0x1" startLine="3" startColumn="48" endLine="3" endColumn="49" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub PartiallydefinedClass_2()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Partial Class C
    Public m1 As Func(Of Integer) = Function() 1

    Sub Main()
    End Sub
End Class

Partial Class C
    Public m2 As Func(Of Integer) = Function() 2
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            ' Check two distinct lambda offets for m1 and m2
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="CC,  4, 2E, 86, CE, 51, 76, 57, 53, 27, C4, A0, 42, 3C, DA, FC, 6A, 91, 4A, 39, "/>
    </files>
    <methods>
        <method containingType="C" name=".ctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="-26"/>
                    <lambda offset="-12"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="3" startColumn="12" endLine="3" endColumn="49" document="1"/>
                <entry offset="0x30" startLine="10" startColumn="12" endLine="10" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5b">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="C" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="5" endLine="6" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="37" endLine="3" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="48" endLine="3" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C+_Closure$__" name="_Lambda$__0-1">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="37" endLine="10" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="10" startColumn="48" endLine="10" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="C" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub PartiallydefinedClass_3()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Partial Class C2
    Public Shared m1 As Func(Of Integer) = Function() 1
End Class
    </file>
    <file name="b.vb">
Imports System
Partial Class C2
    Shared Sub New()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="37, E0,  6, E1,  3,  9, 97, 5A, F5, 8F, 79, EE, 92, BC, 7C, 63, A6, EB, FF, D4, "/>
        <file id="2" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="D2, 29, EA, DE, F7, E6, E9, BC, A0, CE, E4, FB, 93, 74,  5, 37, 16, D8, 89, F1, "/>
    </files>
    <methods>
        <method containingType="C2" name=".cctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>2</methodOrdinal>
                    <lambda offset="-12"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="21" document="1"/>
                <entry offset="0x1" startLine="3" startColumn="19" endLine="3" endColumn="56" document="2"/>
                <entry offset="0x16" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x17">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="C2+_Closure$__" name="_Lambda$__2-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="44" endLine="3" endColumn="54" document="2"/>
                <entry offset="0x1" startLine="3" startColumn="55" endLine="3" endColumn="56" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="C2" methodName=".cctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class
End Namespace