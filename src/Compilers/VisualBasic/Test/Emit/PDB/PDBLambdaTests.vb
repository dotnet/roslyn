' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports System.Xml.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBLambdaTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="E5-7C-24-B4-CD-54-7D-DA-7A-48-2F-D1-A4-B6-D2-EB-5C-95-CA-B4"/>
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
                    <slot kind="21" offset="13"/>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

        Public Sub Goo(Of TFun1, TFun2)(p As TFun1, p1 As TFun2, p3 As Integer)
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
        inst.Goo(Of Integer, Integer)(42, 333, 432)
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("M1+C1`1+_Closure$__3-1`2._Lambda$__0",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1+C1`1+_Closure$__3-1`2" name="_Lambda$__0" parameterNames="lifted, notLifted">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="30" offset="57"/>
                    <slot kind="0" offset="127"/>
                    <slot kind="0" offset="234"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="17" endLine="16" endColumn="61" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x15" startLine="17" startColumn="25" endLine="17" endColumn="60" document="1"/>
                <entry offset="0x1e" startLine="18" startColumn="21" endLine="18" endColumn="43" document="1"/>
                <entry offset="0x25" startLine="20" startColumn="25" endLine="24" endColumn="32" document="1"/>
                <entry offset="0x32" startLine="26" startColumn="21" endLine="26" endColumn="33" document="1"/>
                <entry offset="0x3f" startLine="27" startColumn="17" endLine="27" endColumn="24" document="1"/>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub NestedLambdaFunction()
            Dim source = "
Class C
    Sub F()
        Dim f = Function(a) Function(b) b + 1
    End Sub
End Class"

            Dim compilation = CreateCompilation(source, options:=TestOptions.DebugDll)

            ' Notice the that breakpoint spans of the inner function overlap with the breakpoint span of the outer function body
            ' and that the two sequence points have the same start position.
            ' Dim f = Function(a) [|[|Function(b)|] b + 1|]

            compilation.VerifyPdb("C+_Closure$__._Lambda$__1-0",
 <symbols>
     <files>
         <file id="1" name="" language="VB"/>
     </files>
     <methods>
         <method containingType="C+_Closure$__" name="_Lambda$__1-0" parameterNames="a">
             <customDebugInfo>
                 <encLocalSlotMap>
                     <slot kind="21" offset="8"/>
                 </encLocalSlotMap>
             </customDebugInfo>
             <sequencePoints>
                 <entry offset="0x0" startLine="4" startColumn="17" endLine="4" endColumn="28" document="1"/>
                 <entry offset="0x1" startLine="4" startColumn="29" endLine="4" endColumn="46" document="1"/>
             </sequencePoints>
             <scope startOffset="0x0" endOffset="0x2a">
                 <importsforward declaringType="C" methodName="F"/>
             </scope>
         </method>
     </methods>
 </symbols>)

            compilation.VerifyPdb("C+_Closure$__._Lambda$__1-1",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="C+_Closure$__" name="_Lambda$__1-1" parameterNames="b">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="20"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="29" endLine="4" endColumn="40" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="41" endLine="4" endColumn="46" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <importsforward declaringType="C" methodName="F"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        <WorkItem(544000, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544000")>
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            Dim actual1 As XElement = GetPdbXml(compilation)
            Dim actual2 As XElement = GetPdbXml(compilation)
            AssertXml.Equal(actual1, actual2)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1+_Closure$__._Lambda$__0-0",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1+_Closure$__" name="_Lambda$__0-0">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="8"/>
                    <slot kind="0" offset="44"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="17" endLine="5" endColumn="27" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="25" endLine="6" endColumn="31" document="1"/>
                <entry offset="0x4" startLine="7" startColumn="21" endLine="7" endColumn="29" document="1"/>
                <entry offset="0x8" startLine="8" startColumn="12" endLine="8" endColumn="24" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Main"/>
                <local name="r" il_index="1" il_start="0x0" il_end="0xa" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub PartiallyDefinedClass_1()
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            ' Check two distinct lambda offsets for m1 and m2
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="E9-8A-62-CA-DC-E3-2B-C4-4B-06-D5-97-3C-77-18-2E-6F-67-EE-15"/>
        <file id="2" name="b.vb" language="VB" checksumAlgorithm="SHA1" checksum="A1-36-22-63-B1-FC-DD-52-E1-86-92-E9-1A-7D-68-5A-C5-74-27-69"/>
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
                <entry offset="0x7" startLine="3" startColumn="12" endLine="3" endColumn="49" document="1"/>
                <entry offset="0x31" startLine="3" startColumn="12" endLine="3" endColumn="49" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5c">
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
                    <slot kind="21" offset="-26"/>
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
                    <slot kind="21" offset="-12"/>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub PartiallyDefinedClass_2()
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            ' Check two distinct lambda offsets for m1 and m2
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="CC-04-2E-86-CE-51-76-57-53-27-C4-A0-42-3C-DA-FC-6A-91-4A-39"/>
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
                <entry offset="0x7" startLine="3" startColumn="12" endLine="3" endColumn="49" document="1"/>
                <entry offset="0x31" startLine="10" startColumn="12" endLine="10" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5c">
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
                    <slot kind="21" offset="-26"/>
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
                    <slot kind="21" offset="-12"/>
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

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub PartiallyDefinedClass_3()
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                    source,
                    TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="b.vb" language="VB" checksumAlgorithm="SHA1" checksum="37-E0-06-E1-03-09-97-5A-F5-8F-79-EE-92-BC-7C-63-A6-EB-FF-D4"/>
        <file id="2" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="D2-29-EA-DE-F7-E6-E9-BC-A0-CE-E4-FB-93-74-05-37-16-D8-89-F1"/>
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
                    <slot kind="21" offset="-12"/>
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

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1, 2, 3}
    End Function
 
    Sub Main()
	System.Diagnostics.Debug.Assert(False)

        Dim q = From x In Nums()
                Order By x Descending
                Group y = x * 10, z = x * 100 By evenOdd = x Mod 2
                    Into s = Sum(y + 12345), z = Sum(y + 56789)
 
        q.ToArray()

        Dim qq = From x As Long In Nums()
                Order By x Descending
 
        qq.ToArray()
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="25" document="1"/>
                <entry offset="0x15" startLine="7" startColumn="5" endLine="7" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x17">
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x17" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="54"/>
                    <slot kind="0" offset="286"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="101"/>
                    <lambda offset="174"/>
                    <lambda offset="141"/>
                    <lambda offset="131"/>
                    <lambda offset="216"/>
                    <lambda offset="236"/>
                    <lambda offset="298"/>
                    <lambda offset="342"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="12" startColumn="13" endLine="15" endColumn="64" document="1"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="20" document="1"/>
                <entry offset="0xa8" startLine="19" startColumn="13" endLine="20" endColumn="38" document="1"/>
                <entry offset="0x100" startLine="22" startColumn="9" endLine="22" endColumn="21" document="1"/>
                <entry offset="0x107" startLine="23" startColumn="5" endLine="23" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x108">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x108" attributes="0"/>
                <local name="qq" il_index="1" il_start="0x0" il_end="0x108" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="26" endLine="13" endColumn="27" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="60" endLine="14" endColumn="67" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="27" endLine="14" endColumn="33" document="1"/>
                <entry offset="0x4" startLine="14" startColumn="39" endLine="14" endColumn="46" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="evenOdd, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="15" startColumn="30" endLine="15" endColumn="44" document="1"/>
                <entry offset="0x2b" startLine="15" startColumn="50" endLine="15" endColumn="64" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5b">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="43" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="54" endLine="15" endColumn="63" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="19" startColumn="25" endLine="19" endColumn="32" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="20" startColumn="26" endLine="20" endColumn="27" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_01()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
        x = New List(Of Integer)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="61"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="14" endColumn="37" document="1"/>
                <entry offset="0x30" startLine="15" startColumn="9" endLine="15" endColumn="36" document="1"/>
                <entry offset="0x3c" startLine="16" startColumn="5" endLine="16" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_02()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 As Long In Nums(), rangeVar2 As Long In Nums()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="76"/>
                    <lambda offset="116"/>
                    <lambda offset="105"/>
                    <lambda offset="61"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="14" endColumn="74" document="1"/>
                <entry offset="0x7d" startLine="15" startColumn="9" endLine="15" endColumn="35" document="1"/>
                <entry offset="0x89" startLine="16" startColumn="5" endLine="16" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="28" endLine="14" endColumn="35" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="68" endLine="14" endColumn="74" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="57" endLine="14" endColumn="64" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_03()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Let rangeVar2 = rangeVar1 * 2
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="115"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="42" document="1"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="29" endLine="15" endColumn="42" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_04()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Let rangeVar2 As Long = rangeVar1 * 2, rangeVar3 = rangeVar1 + rangeVar2
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="123"/>
                    <lambda offset="150"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="85" document="1"/>
                <entry offset="0x59" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x65" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="37" endLine="15" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="64" endLine="15" endColumn="85" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x20">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_05()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select rangeVar2 = rangeVar1 * 2
        x = New List(Of Integer)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="118"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="45" document="1"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="36" document="1"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="32" endLine="15" endColumn="45" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_06()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select rangeVar1 * 2
        x = New List(Of Integer)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="106"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="33" document="1"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="36" document="1"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="33" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_07()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select rangeVar2 = rangeVar1 * 2, rangeVar3 = rangeVar1 / 2
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="118"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="72" document="1"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="32" endLine="15" endColumn="45" document="1"/>
                <entry offset="0x3" startLine="15" startColumn="59" endLine="15" endColumn="72" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x15">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_08()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums() Join rangeVar2 As Long In Nums()
                                     On rangeVar1 Equals rangeVar2
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="101"/>
                    <lambda offset="160"/>
                    <lambda offset="177"/>
                    <lambda offset="86"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="67" document="1"/>
                <entry offset="0xa6" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0xb2" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb3">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xb3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xb3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="53" endLine="14" endColumn="60" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_09()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums() Join rangeVar2 In Nums()
                                          Join rangeVar3 In Nums()
                                          On rangeVar3 Equals rangeVar2
                                     On rangeVar1 Equals rangeVar2 And rangeVar3 + 1 Equals rangeVar1 + 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="225"/>
                    <lambda offset="242"/>
                    <lambda offset="154"/>
                    <lambda offset="293"/>
                    <lambda offset="310"/>
                    <lambda offset="86"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="106" document="1"/>
                <entry offset="0xf3" startLine="18" startColumn="9" endLine="18" endColumn="35" document="1"/>
                <entry offset="0xff" startLine="19" startColumn="5" endLine="19" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x100">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x100" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x100" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="63" endLine="16" endColumn="72" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="46" endLine="16" endColumn="55" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="41" endLine="17" endColumn="50" document="1"/>
                <entry offset="0x1" startLine="17" startColumn="93" endLine="17" endColumn="106" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="58" endLine="17" endColumn="67" document="1"/>
                <entry offset="0x6" startLine="17" startColumn="72" endLine="17" endColumn="85" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x14">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_10()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums() Group Join rangeVar2 As Long In Nums()
                                     On rangeVar1 Equals rangeVar2
                                     Into Group
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="107"/>
                    <lambda offset="166"/>
                    <lambda offset="183"/>
                    <lambda offset="86"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="48" document="1"/>
                <entry offset="0xa6" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0xb2" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb3">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xb3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xb3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="59" endLine="14" endColumn="66" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_11()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums() Group Join rangeVar2 As Long In Nums()
                                         Group Join rangeVar3 As Long In Nums()
                                                On rangeVar3 Equals rangeVar2
                                         Into Sum(rangeVar3)
                                     On rangeVar1 Equals rangeVar2
                                     Into Sum(rangeVar2)
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="107"/>
                    <lambda offset="188"/>
                    <lambda offset="258"/>
                    <lambda offset="275"/>
                    <lambda offset="167"/>
                    <lambda offset="336"/>
                    <lambda offset="388"/>
                    <lambda offset="405"/>
                    <lambda offset="86"/>
                    <lambda offset="462"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="19" endColumn="57" document="1"/>
                <entry offset="0x145" startLine="20" startColumn="9" endLine="20" endColumn="35" document="1"/>
                <entry offset="0x151" startLine="21" startColumn="5" endLine="21" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x152">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x152" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x152" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="59" endLine="14" endColumn="66" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="63" endLine="15" endColumn="70" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="69" endLine="16" endColumn="78" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="52" endLine="16" endColumn="61" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="17" startColumn="47" endLine="17" endColumn="61" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="51" endLine="17" endColumn="60" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="41" endLine="18" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="58" endLine="18" endColumn="67" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-8" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="19" startColumn="43" endLine="19" endColumn="57" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-9" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="19" startColumn="47" endLine="19" endColumn="56" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_12()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums() Group Join rangeVar2 In Nums()
                                     On rangeVar1 Equals rangeVar2 And rangeVar2 + 1 Equals rangeVar1 + 1
                                     Into Group, Sum = Sum(rangeVar2), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="158"/>
                    <lambda offset="175"/>
                    <lambda offset="86"/>
                    <lambda offset="284"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="79" document="1"/>
                <entry offset="0x7d" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x89" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="1"/>
                <entry offset="0x1" startLine="15" startColumn="93" endLine="15" endColumn="106" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="1"/>
                <entry offset="0x1" startLine="15" startColumn="72" endLine="15" endColumn="85" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x2" startLine="16" startColumn="56" endLine="16" endColumn="70" document="1"/>
                <entry offset="0x2c" startLine="16" startColumn="72" endLine="16" endColumn="79" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x38">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="60" endLine="16" endColumn="69" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_13()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Where rangeVar1 = rangeVar2 OrElse rangeVar1 < rangeVar2 + 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="126"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="73" document="1"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="19" endLine="15" endColumn="73" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_14()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Skip While rangeVar1 = rangeVar2 OrElse rangeVar1 < rangeVar2 + 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="131"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="78" document="1"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="24" endLine="15" endColumn="78" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_15()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Take While rangeVar1 = rangeVar2 OrElse rangeVar1 < rangeVar2 + 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="131"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="78" document="1"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="24" endLine="15" endColumn="78" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_16()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Skip 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="19" document="1"/>
                <entry offset="0xd" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x19" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_17()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Take 1
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="19" document="1"/>
                <entry offset="0xd" startLine="16" startColumn="9" endLine="16" endColumn="35" document="1"/>
                <entry offset="0x19" startLine="17" startColumn="5" endLine="17" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_18()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Group By rangeVar1
            Into Group
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="129"/>
                    <lambda offset="120"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="23" document="1"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="22" endLine="15" endColumn="31" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_19()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Group By rangeVar2 = rangeVar1 * 2
            Into Sum(rangeVar2)
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="141"/>
                    <lambda offset="120"/>
                    <lambda offset="177"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="32" document="1"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="16" startColumn="18" endLine="16" endColumn="32" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="22" endLine="16" endColumn="31" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_20()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums(), rangeVar2 In Nums()
            Group By rangeVar2 = rangeVar1 * 2, rangeVar3 = rangeVar1 / 2
            Into Group, Sum = Sum(rangeVar2), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="100"/>
                    <lambda offset="61"/>
                    <lambda offset="141"/>
                    <lambda offset="120"/>
                    <lambda offset="217"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="54" document="1"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="47" document="1"/>
                <entry offset="0x8" startLine="15" startColumn="61" endLine="15" endColumn="74" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="$VB$It, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0xd" startLine="16" startColumn="31" endLine="16" endColumn="45" document="1"/>
                <entry offset="0x37" startLine="16" startColumn="47" endLine="16" endColumn="54" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x43">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="35" endLine="16" endColumn="44" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_21()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Aggregate rangeVar2 As Long In Nums()
            Into Sum(rangeVar2 / 3)
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="130"/>
                    <lambda offset="119"/>
                    <lambda offset="159"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="36" document="1"/>
                <entry offset="0x30" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x3c" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="16" endColumn="36" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="22" endLine="16" endColumn="35" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_22()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Aggregate rangeVar2 As Long In Nums(), rangeVar3 In Nums()
            Into Sum = Sum(rangeVar2 * rangeVar3)
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="130"/>
                    <lambda offset="119"/>
                    <lambda offset="151"/>
                    <lambda offset="99"/>
                    <lambda offset="186"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="50" document="1"/>
                <entry offset="0x30" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x3c" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="16" endColumn="50" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xab">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="65" endLine="15" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="49" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_23()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select 1
            Aggregate rangeVar2 As Long In Nums()
            Into Sum(rangeVar2 / 3)
        x = New List(Of Double)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="106"/>
                    <lambda offset="152"/>
                    <lambda offset="141"/>
                    <lambda offset="181"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="36" document="1"/>
                <entry offset="0x59" startLine="18" startColumn="9" endLine="18" endColumn="35" document="1"/>
                <entry offset="0x65" startLine="19" startColumn="5" endLine="19" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="17" endColumn="36" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x58">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="22" endLine="17" endColumn="35" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_24()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Aggregate rangeVar2 In Nums()
            Into Sum = Sum(rangeVar2), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="122"/>
                    <lambda offset="99"/>
                    <lambda offset="157"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="1"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="42" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="1"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_25()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Aggregate rangeVar2 As Long In Nums(), rangeVar3 In Nums()
            Into Sum = Sum(rangeVar3), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="130"/>
                    <lambda offset="119"/>
                    <lambda offset="151"/>
                    <lambda offset="99"/>
                    <lambda offset="99"/>
                    <lambda offset="186"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="1"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x82">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="65" endLine="15" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="1"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_26()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Aggregate rangeVar2 As Long In Nums() Join rangeVar3 In Nums() On rangeVar2 Equals rangeVar3
            Into Sum = Sum(rangeVar3), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="130"/>
                    <lambda offset="119"/>
                    <lambda offset="165"/>
                    <lambda offset="182"/>
                    <lambda offset="137"/>
                    <lambda offset="99"/>
                    <lambda offset="220"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="1"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="1"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xab">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="79" endLine="15" endColumn="88" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="96" endLine="15" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="1"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_27()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select 2
            Aggregate rangeVar2 In Nums()
            Into Sum = Sum(rangeVar2), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="106"/>
                    <lambda offset="144"/>
                    <lambda offset="121"/>
                    <lambda offset="179"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="1"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="1"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="42" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="1"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_28()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select 3
            Aggregate rangeVar2 As Long In Nums(), rangeVar3 In Nums()
            Into Sum = Sum(rangeVar3), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="106"/>
                    <lambda offset="152"/>
                    <lambda offset="141"/>
                    <lambda offset="173"/>
                    <lambda offset="121"/>
                    <lambda offset="121"/>
                    <lambda offset="208"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="1"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="1"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7c">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="65" endLine="16" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="1"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_29()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim q As IEnumerable
        Dim x As Object

        q = From rangeVar1 In Nums()
            Select 3
            Aggregate rangeVar2 As Long In Nums() Join rangeVar3 In Nums() On rangeVar2 Equals rangeVar3
            Into Sum = Sum(rangeVar3), Count()
        x = New List(Of Object)(q)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="34"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="106"/>
                    <lambda offset="152"/>
                    <lambda offset="141"/>
                    <lambda offset="187"/>
                    <lambda offset="204"/>
                    <lambda offset="159"/>
                    <lambda offset="121"/>
                    <lambda offset="242"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="1"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="1"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa5">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="79" endLine="16" endColumn="88" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="96" endLine="16" endColumn="105" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="1"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_30()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim x As Object
        x = Aggregate rangeVar1 As Long In Nums()
            Into Sum(rangeVar1 / 3)
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="49"/>
                    <lambda offset="89"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="12" startColumn="9" endLine="13" endColumn="36" document="1"/>
                <entry offset="0x5e" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="33" endLine="12" endColumn="40" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="22" endLine="13" endColumn="35" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824944")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_31()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Function Nums() As IEnumerable(Of Integer)
        Return {1}
    End Function
 
    Sub Main()
        Dim x As Object
        x = Aggregate rangeVar1 In Nums(), rangeVar2 As Long In Nums()
            Into Sum = Sum(rangeVar2), Count()
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="1"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="1"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <lambda offset="81"/>
                    <lambda offset="70"/>
                    <lambda offset="29"/>
                    <lambda offset="116"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="1"/>
                <entry offset="0x1" startLine="12" startColumn="9" endLine="13" endColumn="47" document="1"/>
                <entry offset="0x8a" startLine="14" startColumn="5" endLine="14" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8b">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x8b" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="65" endLine="12" endColumn="71" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="54" endLine="12" endColumn="61" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="28" endLine="13" endColumn="37" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(841361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/841361")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub SequencePointsInAQuery_32()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main()
        Dim x = From a in {1, 2, 3}
                Let b = a * a
                Select b
    End Sub
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb("Module1+_Closure$__._Lambda$__0-0",
<symbols>
    <files>
        <file id="1" name="" language="VB"/>
    </files>
    <methods>
        <method containingType="Module1+_Closure$__" name="_Lambda$__0-0" parameterNames="a">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="25" endLine="8" endColumn="30" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class
End Namespace
