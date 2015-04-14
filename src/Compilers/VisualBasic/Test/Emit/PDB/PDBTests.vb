' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub TestBasic()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        System.Console.WriteLine("Hello, world.")
    End Sub
End Class
]]></file>
</compilation>

            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll.WithParseOptions(parseOptions))

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="C1" name="Method">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="0"/>
                <entry offset="0x1" startLine="3" startColumn="9" endLine="3" endColumn="50" document="0"/>
                <entry offset="0xc" startLine="4" startColumn="5" endLine="4" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="My.MyComputer" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="107" startColumn="9" endLine="107" endColumn="25" document="0"/>
                <entry offset="0x1" startLine="108" startColumn="13" endLine="108" endColumn="25" document="0"/>
                <entry offset="0x8" startLine="109" startColumn="9" endLine="109" endColumn="16" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="121" startColumn="13" endLine="121" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="122" startColumn="17" endLine="122" endColumn="62" document="0"/>
                <entry offset="0xe" startLine="123" startColumn="13" endLine="123" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="133" startColumn="13" endLine="133" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="134" startColumn="17" endLine="134" endColumn="57" document="0"/>
                <entry offset="0xe" startLine="135" startColumn="13" endLine="135" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="144" startColumn="13" endLine="144" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="145" startColumn="17" endLine="145" endColumn="58" document="0"/>
                <entry offset="0xe" startLine="146" startColumn="13" endLine="146" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="237" startColumn="14" endLine="237" endColumn="17" document="0"/>
                <entry offset="0x1" startLine="238" startColumn="17" endLine="238" endColumn="67" document="0"/>
                <entry offset="0xe" startLine="239" startColumn="13" endLine="239" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor">
            <sequencePoints>
                <entry offset="0x0" startLine="126" startColumn="26" endLine="126" endColumn="97" document="0"/>
                <entry offset="0xa" startLine="137" startColumn="26" endLine="137" endColumn="95" document="0"/>
                <entry offset="0x14" startLine="148" startColumn="26" endLine="148" endColumn="136" document="0"/>
                <entry offset="0x1e" startLine="284" startColumn="26" endLine="284" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="247" startColumn="13" endLine="247" endColumn="75" document="0"/>
                <entry offset="0x1" startLine="248" startColumn="17" endLine="248" endColumn="40" document="0"/>
                <entry offset="0x10" startLine="249" startColumn="13" endLine="249" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="251" startColumn="13" endLine="251" endColumn="63" document="0"/>
                <entry offset="0x1" startLine="252" startColumn="17" endLine="252" endColumn="42" document="0"/>
                <entry offset="0xa" startLine="253" startColumn="13" endLine="253" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="255" startColumn="13" endLine="255" endColumn="72" document="0"/>
                <entry offset="0x1" startLine="256" startColumn="17" endLine="256" endColumn="46" document="0"/>
                <entry offset="0xe" startLine="257" startColumn="13" endLine="257" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="259" startColumn="13" endLine="259" endColumn="59" document="0"/>
                <entry offset="0x1" startLine="260" startColumn="17" endLine="260" endColumn="39" document="0"/>
                <entry offset="0xa" startLine="261" startColumn="13" endLine="261" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="264" startColumn="12" endLine="264" endColumn="95" document="0"/>
                <entry offset="0x1" startLine="265" startColumn="17" endLine="265" endColumn="44" document="0"/>
                <entry offset="0xe" startLine="266" startColumn="21" endLine="266" endColumn="35" document="0"/>
                <entry offset="0x16" startLine="267" startColumn="17" endLine="267" endColumn="21" document="0"/>
                <entry offset="0x17" startLine="268" startColumn="21" endLine="268" endColumn="36" document="0"/>
                <entry offset="0x1b" startLine="270" startColumn="13" endLine="270" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencePoints>
                <entry offset="0x0" startLine="273" startColumn="13" endLine="273" endColumn="71" document="0"/>
                <entry offset="0x1" startLine="274" startColumn="17" endLine="274" endColumn="35" document="0"/>
                <entry offset="0x8" startLine="275" startColumn="13" endLine="275" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="279" startColumn="13" endLine="279" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="280" startColumn="16" endLine="280" endColumn="28" document="0"/>
                <entry offset="0x8" startLine="281" startColumn="13" endLine="281" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="341" startColumn="17" endLine="341" endColumn="20" document="0"/>
                <entry offset="0x1" startLine="342" startColumn="21" endLine="342" endColumn="59" document="0"/>
                <entry offset="0x12" startLine="342" startColumn="60" endLine="342" endColumn="87" document="0"/>
                <entry offset="0x1c" startLine="343" startColumn="21" endLine="343" endColumn="47" document="0"/>
                <entry offset="0x24" startLine="344" startColumn="17" endLine="344" endColumn="24" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="350" startColumn="13" endLine="350" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="351" startColumn="17" endLine="351" endColumn="29" document="0"/>
                <entry offset="0x8" startLine="352" startColumn="13" endLine="352" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TryCatchFinally()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict On
Imports System

Module M1
    Public Sub Main()
        Dim x As Integer = 0
        Try
            Dim y As String = "y"
label1:
label2:
            If x = 0 Then
                Throw New Exception()
            End If
        Catch ex As Exception
            Dim z As String = "z"
            Console.WriteLine(x)
            x = 1
            GoTo label1
        Finally
            Dim q As String = "q"
            Console.WriteLine(x)
        End Try

        Console.WriteLine(x)

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("M1.Main",
<symbols>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="51"/>
                    <slot kind="temp"/>
                    <slot kind="0" offset="182"/>
                    <slot kind="0" offset="221"/>
                    <slot kind="0" offset="351"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" startLine="7" startColumn="9" endLine="7" endColumn="12" document="0"/>
                <entry offset="0x4" startLine="8" startColumn="17" endLine="8" endColumn="34" document="0"/>
                <entry offset="0xa" startLine="9" startColumn="1" endLine="9" endColumn="8" document="0"/>
                <entry offset="0xb" startLine="10" startColumn="1" endLine="10" endColumn="8" document="0"/>
                <entry offset="0xc" startLine="11" startColumn="13" endLine="11" endColumn="26" document="0"/>
                <entry offset="0x14" startLine="12" startColumn="17" endLine="12" endColumn="38" document="0"/>
                <entry offset="0x1a" startLine="13" startColumn="13" endLine="13" endColumn="19" document="0"/>
                <entry offset="0x1d" hidden="true" document="0"/>
                <entry offset="0x24" startLine="14" startColumn="9" endLine="14" endColumn="30" document="0"/>
                <entry offset="0x25" startLine="15" startColumn="17" endLine="15" endColumn="34" document="0"/>
                <entry offset="0x2c" startLine="16" startColumn="13" endLine="16" endColumn="33" document="0"/>
                <entry offset="0x33" startLine="17" startColumn="13" endLine="17" endColumn="18" document="0"/>
                <entry offset="0x35" startLine="18" startColumn="13" endLine="18" endColumn="24" document="0"/>
                <entry offset="0x3c" hidden="true" document="0"/>
                <entry offset="0x3e" startLine="19" startColumn="9" endLine="19" endColumn="16" document="0"/>
                <entry offset="0x3f" startLine="20" startColumn="17" endLine="20" endColumn="34" document="0"/>
                <entry offset="0x46" startLine="21" startColumn="13" endLine="21" endColumn="33" document="0"/>
                <entry offset="0x4e" startLine="22" startColumn="9" endLine="22" endColumn="16" document="0"/>
                <entry offset="0x4f" startLine="24" startColumn="9" endLine="24" endColumn="29" document="0"/>
                <entry offset="0x56" startLine="26" startColumn="5" endLine="26" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x57">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
                <scope startOffset="0x4" endOffset="0x1a">
                    <local name="y" il_index="1" il_start="0x4" il_end="0x1a" attributes="0"/>
                </scope>
                <scope startOffset="0x1d" endOffset="0x3b">
                    <local name="ex" il_index="3" il_start="0x1d" il_end="0x3b" attributes="0"/>
                    <scope startOffset="0x25" endOffset="0x3b">
                        <local name="z" il_index="4" il_start="0x25" il_end="0x3b" attributes="0"/>
                    </scope>
                </scope>
                <scope startOffset="0x3f" endOffset="0x4c">
                    <local name="q" il_index="5" il_start="0x3f" il_end="0x4c" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TryCatchWhen()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict On
Imports System

Module M1
    Public Sub Main()
        Dim x As Integer = 0
        Try
            Dim y As String = "y"
label1:
label2:
            x = x \ x
        Catch ex As Exception When ex.Message IsNot Nothing
            Dim z As String = "z"
            Console.WriteLine(x)
            x = 1
            GoTo label1
        Finally
            Dim q As String = "q"
            Console.WriteLine(x)
        End Try

        Console.WriteLine(x)

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("M1.Main",
<symbols>
    <entryPoint declaringType="M1" methodName="Main"/>
    <methods>
        <method containingType="M1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="51"/>
                    <slot kind="0" offset="119"/>
                    <slot kind="0" offset="188"/>
                    <slot kind="0" offset="318"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" startLine="7" startColumn="9" endLine="7" endColumn="12" document="0"/>
                <entry offset="0x4" startLine="8" startColumn="17" endLine="8" endColumn="34" document="0"/>
                <entry offset="0xa" startLine="9" startColumn="1" endLine="9" endColumn="8" document="0"/>
                <entry offset="0xb" startLine="10" startColumn="1" endLine="10" endColumn="8" document="0"/>
                <entry offset="0xc" startLine="11" startColumn="13" endLine="11" endColumn="22" document="0"/>
                <entry offset="0x12" hidden="true" document="0"/>
                <entry offset="0x25" startLine="12" startColumn="9" endLine="12" endColumn="60" document="0"/>
                <entry offset="0x33" hidden="true" document="0"/>
                <entry offset="0x34" startLine="13" startColumn="17" endLine="13" endColumn="34" document="0"/>
                <entry offset="0x3a" startLine="14" startColumn="13" endLine="14" endColumn="33" document="0"/>
                <entry offset="0x41" startLine="15" startColumn="13" endLine="15" endColumn="18" document="0"/>
                <entry offset="0x43" startLine="16" startColumn="13" endLine="16" endColumn="24" document="0"/>
                <entry offset="0x4a" hidden="true" document="0"/>
                <entry offset="0x4c" startLine="17" startColumn="9" endLine="17" endColumn="16" document="0"/>
                <entry offset="0x4d" startLine="18" startColumn="17" endLine="18" endColumn="34" document="0"/>
                <entry offset="0x54" startLine="19" startColumn="13" endLine="19" endColumn="33" document="0"/>
                <entry offset="0x5c" startLine="20" startColumn="9" endLine="20" endColumn="16" document="0"/>
                <entry offset="0x5d" startLine="22" startColumn="9" endLine="22" endColumn="29" document="0"/>
                <entry offset="0x64" startLine="24" startColumn="5" endLine="24" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x65">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x65" attributes="0"/>
                <scope startOffset="0x4" endOffset="0xf">
                    <local name="y" il_index="1" il_start="0x4" il_end="0xf" attributes="0"/>
                </scope>
                <scope startOffset="0x12" endOffset="0x49">
                    <local name="ex" il_index="2" il_start="0x12" il_end="0x49" attributes="0"/>
                    <scope startOffset="0x34" endOffset="0x49">
                        <local name="z" il_index="3" il_start="0x34" il_end="0x49" attributes="0"/>
                    </scope>
                </scope>
                <scope startOffset="0x4d" endOffset="0x5a">
                    <local name="q" il_index="4" il_start="0x4d" il_end="0x5a" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestBasic1()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict On

Module Module1
    Sub Main()
        Dim x As Integer = 3
        Do While (x <= 3)
            Dim y As Integer = x + 1
            x = y
        Loop
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="65"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="5" startColumn="13" endLine="5" endColumn="29" document="0"/>
                <entry offset="0x3" hidden="true" document="0"/>
                <entry offset="0x5" startLine="7" startColumn="17" endLine="7" endColumn="37" document="0"/>
                <entry offset="0x9" startLine="8" startColumn="13" endLine="8" endColumn="18" document="0"/>
                <entry offset="0xb" startLine="9" startColumn="9" endLine="9" endColumn="13" document="0"/>
                <entry offset="0xc" startLine="6" startColumn="9" endLine="6" endColumn="26" document="0"/>
                <entry offset="0x17" startLine="10" startColumn="5" endLine="10" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
                <scope startOffset="0x5" endOffset="0xb">
                    <local name="y" il_index="1" il_start="0x5" il_end="0xb" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestBasicCtor()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub New()
        System.Console.WriteLine("Hello, world.")
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C1..ctor",
<symbols>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="14" document="0"/>
                <entry offset="0x7" startLine="3" startColumn="9" endLine="3" endColumn="50" document="0"/>
                <entry offset="0x12" startLine="4" startColumn="5" endLine="4" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x13">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestLabels()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub New()
        label1:
        label2:
        label3:

        goto label2:
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C1..ctor",
<symbols>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="14" document="0"/>
                <entry offset="0x7" startLine="3" startColumn="9" endLine="3" endColumn="16" document="0"/>
                <entry offset="0x8" startLine="4" startColumn="9" endLine="4" endColumn="16" document="0"/>
                <entry offset="0x9" startLine="5" startColumn="9" endLine="5" endColumn="16" document="0"/>
                <entry offset="0xa" startLine="7" startColumn="9" endLine="7" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestIfThenAndBlocks()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0, xx = New Integer()

        If x < 10 Then Dim s As String = "hi" : Console.WriteLine(s) Else Console.WriteLine("bye") : Console.WriteLine("bye1")
        If x > 10 Then Console.WriteLine("hi") : Console.WriteLine("hi1") Else Dim s As String = "bye" : Console.WriteLine(s)

        Do While x < 5
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="22"/>
                    <slot kind="temp"/>
                    <slot kind="0" offset="71"/>
                    <slot kind="0" offset="255"/>
                    <slot kind="0" offset="753"/>
                    <slot kind="0" offset="444"/>
                    <slot kind="0" offset="555"/>
                    <slot kind="0" offset="653"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" startLine="6" startColumn="31" endLine="6" endColumn="49" document="0"/>
                <entry offset="0xb" startLine="8" startColumn="9" endLine="8" endColumn="23" document="0"/>
                <entry offset="0x17" startLine="8" startColumn="28" endLine="8" endColumn="46" document="0"/>
                <entry offset="0x1d" startLine="8" startColumn="49" endLine="8" endColumn="69" document="0"/>
                <entry offset="0x26" startLine="8" startColumn="70" endLine="8" endColumn="74" document="0"/>
                <entry offset="0x27" startLine="8" startColumn="75" endLine="8" endColumn="99" document="0"/>
                <entry offset="0x32" startLine="8" startColumn="102" endLine="8" endColumn="127" document="0"/>
                <entry offset="0x3d" startLine="9" startColumn="9" endLine="9" endColumn="23" document="0"/>
                <entry offset="0x49" startLine="9" startColumn="24" endLine="9" endColumn="47" document="0"/>
                <entry offset="0x54" startLine="9" startColumn="50" endLine="9" endColumn="74" document="0"/>
                <entry offset="0x61" startLine="9" startColumn="75" endLine="9" endColumn="79" document="0"/>
                <entry offset="0x62" startLine="9" startColumn="84" endLine="9" endColumn="103" document="0"/>
                <entry offset="0x69" startLine="9" startColumn="106" endLine="9" endColumn="126" document="0"/>
                <entry offset="0x71" hidden="true" document="0"/>
                <entry offset="0x73" startLine="12" startColumn="13" endLine="12" endColumn="26" document="0"/>
                <entry offset="0x7e" startLine="13" startColumn="17" endLine="13" endColumn="40" document="0"/>
                <entry offset="0x89" startLine="23" startColumn="13" endLine="23" endColumn="19" document="0"/>
                <entry offset="0x8c" startLine="14" startColumn="13" endLine="14" endColumn="30" document="0"/>
                <entry offset="0x97" startLine="15" startColumn="21" endLine="15" endColumn="40" document="0"/>
                <entry offset="0x9e" startLine="16" startColumn="17" endLine="16" endColumn="38" document="0"/>
                <entry offset="0xa6" startLine="23" startColumn="13" endLine="23" endColumn="19" document="0"/>
                <entry offset="0xa9" startLine="17" startColumn="13" endLine="17" endColumn="30" document="0"/>
                <entry offset="0xb4" startLine="18" startColumn="21" endLine="18" endColumn="40" document="0"/>
                <entry offset="0xbb" startLine="19" startColumn="17" endLine="19" endColumn="38" document="0"/>
                <entry offset="0xc3" startLine="23" startColumn="13" endLine="23" endColumn="19" document="0"/>
                <entry offset="0xc6" startLine="20" startColumn="13" endLine="20" endColumn="17" document="0"/>
                <entry offset="0xc7" startLine="21" startColumn="21" endLine="21" endColumn="42" document="0"/>
                <entry offset="0xce" startLine="22" startColumn="17" endLine="22" endColumn="38" document="0"/>
                <entry offset="0xd6" startLine="23" startColumn="13" endLine="23" endColumn="19" document="0"/>
                <entry offset="0xd7" startLine="25" startColumn="17" endLine="25" endColumn="40" document="0"/>
                <entry offset="0xdc" startLine="26" startColumn="13" endLine="26" endColumn="21" document="0"/>
                <entry offset="0xdf" startLine="27" startColumn="9" endLine="27" endColumn="13" document="0"/>
                <entry offset="0xe0" startLine="11" startColumn="9" endLine="11" endColumn="23" document="0"/>
                <entry offset="0xe8" startLine="29" startColumn="5" endLine="29" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe9">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xe9" attributes="0"/>
                <local name="xx" il_index="1" il_start="0x0" il_end="0xe9" attributes="0"/>
                <scope startOffset="0x17" endOffset="0x23">
                    <local name="s" il_index="3" il_start="0x17" il_end="0x23" attributes="0"/>
                </scope>
                <scope startOffset="0x62" endOffset="0x70">
                    <local name="s" il_index="4" il_start="0x62" il_end="0x70" attributes="0"/>
                </scope>
                <scope startOffset="0x73" endOffset="0xdf">
                    <local name="newX" il_index="5" il_start="0x73" il_end="0xdf" attributes="0"/>
                    <scope startOffset="0x97" endOffset="0xa6">
                        <local name="s2" il_index="6" il_start="0x97" il_end="0xa6" attributes="0"/>
                    </scope>
                    <scope startOffset="0xb4" endOffset="0xc3">
                        <local name="s3" il_index="7" il_start="0xb4" il_end="0xc3" attributes="0"/>
                    </scope>
                    <scope startOffset="0xc7" endOffset="0xd6">
                        <local name="e1" il_index="8" il_start="0xc7" il_end="0xd6" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestTopConditionDoLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0
        Do While x < 5
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="474"/>
                    <slot kind="temp"/>
                    <slot kind="0" offset="165"/>
                    <slot kind="0" offset="276"/>
                    <slot kind="0" offset="374"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" hidden="true" document="0"/>
                <entry offset="0x5" startLine="8" startColumn="13" endLine="8" endColumn="26" document="0"/>
                <entry offset="0x10" startLine="9" startColumn="17" endLine="9" endColumn="40" document="0"/>
                <entry offset="0x1b" startLine="19" startColumn="13" endLine="19" endColumn="19" document="0"/>
                <entry offset="0x1e" startLine="10" startColumn="13" endLine="10" endColumn="30" document="0"/>
                <entry offset="0x29" startLine="11" startColumn="21" endLine="11" endColumn="40" document="0"/>
                <entry offset="0x2f" startLine="12" startColumn="17" endLine="12" endColumn="38" document="0"/>
                <entry offset="0x36" startLine="19" startColumn="13" endLine="19" endColumn="19" document="0"/>
                <entry offset="0x39" startLine="13" startColumn="13" endLine="13" endColumn="30" document="0"/>
                <entry offset="0x44" startLine="14" startColumn="21" endLine="14" endColumn="40" document="0"/>
                <entry offset="0x4b" startLine="15" startColumn="17" endLine="15" endColumn="38" document="0"/>
                <entry offset="0x53" startLine="19" startColumn="13" endLine="19" endColumn="19" document="0"/>
                <entry offset="0x56" startLine="16" startColumn="13" endLine="16" endColumn="17" document="0"/>
                <entry offset="0x57" startLine="17" startColumn="21" endLine="17" endColumn="42" document="0"/>
                <entry offset="0x5e" startLine="18" startColumn="17" endLine="18" endColumn="38" document="0"/>
                <entry offset="0x66" startLine="19" startColumn="13" endLine="19" endColumn="19" document="0"/>
                <entry offset="0x67" startLine="21" startColumn="17" endLine="21" endColumn="40" document="0"/>
                <entry offset="0x6b" startLine="22" startColumn="13" endLine="22" endColumn="21" document="0"/>
                <entry offset="0x6d" startLine="23" startColumn="9" endLine="23" endColumn="13" document="0"/>
                <entry offset="0x6e" startLine="7" startColumn="9" endLine="7" endColumn="23" document="0"/>
                <entry offset="0x76" startLine="25" startColumn="5" endLine="25" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x77">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x77" attributes="0"/>
                <scope startOffset="0x5" endOffset="0x6d">
                    <local name="newX" il_index="1" il_start="0x5" il_end="0x6d" attributes="0"/>
                    <scope startOffset="0x29" endOffset="0x36">
                        <local name="s2" il_index="3" il_start="0x29" il_end="0x36" attributes="0"/>
                    </scope>
                    <scope startOffset="0x44" endOffset="0x53">
                        <local name="s3" il_index="4" il_start="0x44" il_end="0x53" attributes="0"/>
                    </scope>
                    <scope startOffset="0x57" endOffset="0x66">
                        <local name="e1" il_index="5" il_start="0x57" il_end="0x66" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestBottomConditionDoLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0

        Do
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop While x < 5

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="464"/>
                    <slot kind="temp"/>
                    <slot kind="0" offset="155"/>
                    <slot kind="0" offset="266"/>
                    <slot kind="0" offset="364"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" startLine="8" startColumn="9" endLine="8" endColumn="11" document="0"/>
                <entry offset="0x4" startLine="9" startColumn="13" endLine="9" endColumn="26" document="0"/>
                <entry offset="0xf" startLine="10" startColumn="17" endLine="10" endColumn="40" document="0"/>
                <entry offset="0x1a" startLine="20" startColumn="13" endLine="20" endColumn="19" document="0"/>
                <entry offset="0x1d" startLine="11" startColumn="13" endLine="11" endColumn="30" document="0"/>
                <entry offset="0x28" startLine="12" startColumn="21" endLine="12" endColumn="40" document="0"/>
                <entry offset="0x2e" startLine="13" startColumn="17" endLine="13" endColumn="38" document="0"/>
                <entry offset="0x35" startLine="20" startColumn="13" endLine="20" endColumn="19" document="0"/>
                <entry offset="0x38" startLine="14" startColumn="13" endLine="14" endColumn="30" document="0"/>
                <entry offset="0x43" startLine="15" startColumn="21" endLine="15" endColumn="40" document="0"/>
                <entry offset="0x4a" startLine="16" startColumn="17" endLine="16" endColumn="38" document="0"/>
                <entry offset="0x52" startLine="20" startColumn="13" endLine="20" endColumn="19" document="0"/>
                <entry offset="0x55" startLine="17" startColumn="13" endLine="17" endColumn="17" document="0"/>
                <entry offset="0x56" startLine="18" startColumn="21" endLine="18" endColumn="42" document="0"/>
                <entry offset="0x5d" startLine="19" startColumn="17" endLine="19" endColumn="38" document="0"/>
                <entry offset="0x65" startLine="20" startColumn="13" endLine="20" endColumn="19" document="0"/>
                <entry offset="0x66" startLine="22" startColumn="17" endLine="22" endColumn="40" document="0"/>
                <entry offset="0x6a" startLine="23" startColumn="13" endLine="23" endColumn="21" document="0"/>
                <entry offset="0x6c" startLine="24" startColumn="9" endLine="24" endColumn="25" document="0"/>
                <entry offset="0x75" startLine="26" startColumn="5" endLine="26" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x76">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x76" attributes="0"/>
                <scope startOffset="0x4" endOffset="0x6c">
                    <local name="newX" il_index="1" il_start="0x4" il_end="0x6c" attributes="0"/>
                    <scope startOffset="0x28" endOffset="0x35">
                        <local name="s2" il_index="3" il_start="0x28" il_end="0x35" attributes="0"/>
                    </scope>
                    <scope startOffset="0x43" endOffset="0x52">
                        <local name="s3" il_index="4" il_start="0x43" il_end="0x52" attributes="0"/>
                    </scope>
                    <scope startOffset="0x56" endOffset="0x65">
                        <local name="e1" il_index="5" il_start="0x56" il_end="0x65" attributes="0"/>
                    </scope>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestInfiniteLoop()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0

        Do
            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="52"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="13" endLine="6" endColumn="29" document="0"/>
                <entry offset="0x3" startLine="8" startColumn="9" endLine="8" endColumn="11" document="0"/>
                <entry offset="0x4" startLine="9" startColumn="17" endLine="9" endColumn="40" document="0"/>
                <entry offset="0x8" startLine="10" startColumn="13" endLine="10" endColumn="21" document="0"/>
                <entry offset="0xa" startLine="11" startColumn="9" endLine="11" endColumn="13" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xd" attributes="0"/>
                <scope startOffset="0x4" endOffset="0xa">
                    <local name="newX" il_index="1" il_start="0x4" il_end="0xa" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(527647, "DevDiv")>
        <Fact()>
        Public Sub ExtraSequencePointForEndIf()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Module MyMod

    Public Sub Main(args As String())
        If (args IsNot Nothing) Then
            Console.WriteLine("Then")
        Else
            Console.WriteLine("Else")
        End If
    End Sub

End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' By Design (better than Dev10): <entry offset="0x19" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="38" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="37" document="0"/>
                <entry offset="0x9" startLine="7" startColumn="13" endLine="7" endColumn="38" document="0"/>
                <entry offset="0x14" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x17" startLine="8" startColumn="9" endLine="8" endColumn="13" document="0"/>
                <entry offset="0x18" startLine="9" startColumn="13" endLine="9" endColumn="38" document="0"/>
                <entry offset="0x23" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x24" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x25">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(538821, "DevDiv")>
        <Fact()>
        Public Sub MissingSequencePointForOptimizedIfThen()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Public Module MyMod

    Public Sub Main()
        Console.WriteLine("B")

        If "x"c = "X"c Then
            Console.WriteLine("=")
        End If

        If "z"c <> "z"c Then
            Console.WriteLine("<>")
        End If

        Console.WriteLine("E")
    End Sub

End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)


            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main"/>
    <methods>
        <method containingType="MyMod" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="22" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="31" document="0"/>
                <entry offset="0xc" startLine="8" startColumn="9" endLine="8" endColumn="28" document="0"/>
                <entry offset="0x14" startLine="9" startColumn="13" endLine="9" endColumn="35" document="0"/>
                <entry offset="0x1f" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x20" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x21" startLine="12" startColumn="9" endLine="12" endColumn="29" document="0"/>
                <entry offset="0x29" startLine="13" startColumn="13" endLine="13" endColumn="36" document="0"/>
                <entry offset="0x34" startLine="14" startColumn="9" endLine="14" endColumn="15" document="0"/>
                <entry offset="0x35" startLine="14" startColumn="9" endLine="14" endColumn="15" document="0"/>
                <entry offset="0x36" startLine="16" startColumn="9" endLine="16" endColumn="31" document="0"/>
                <entry offset="0x41" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x42">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub MissingSequencePointForTrivialIfThen()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1

    Sub Main()

        ' one
        If (False) Then
            Dim x As String = "hello"
            Show(x)
        End If

        ' two
        If (False) Then Show("hello")

        Try
        Catch ex As Exception
        Finally
            ' three
            If (False) Then Show("hello")
        End Try

    End Sub


    Function Show(s As String) As Integer
        Console.WriteLine(s)

        Return 1
    End Function

End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)


            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="temp"/>
                    <slot kind="0" offset="33"/>
                    <slot kind="0" offset="172"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="24" document="0"/>
                <entry offset="0x9" startLine="9" startColumn="17" endLine="9" endColumn="38" document="0"/>
                <entry offset="0xf" startLine="10" startColumn="13" endLine="10" endColumn="20" document="0"/>
                <entry offset="0x16" startLine="11" startColumn="9" endLine="11" endColumn="15" document="0"/>
                <entry offset="0x17" startLine="11" startColumn="9" endLine="11" endColumn="15" document="0"/>
                <entry offset="0x18" startLine="14" startColumn="9" endLine="14" endColumn="24" document="0"/>
                <entry offset="0x20" startLine="14" startColumn="25" endLine="14" endColumn="38" document="0"/>
                <entry offset="0x2b" hidden="true" document="0"/>
                <entry offset="0x2c" startLine="16" startColumn="9" endLine="16" endColumn="12" document="0"/>
                <entry offset="0x2f" hidden="true" document="0"/>
                <entry offset="0x36" startLine="17" startColumn="9" endLine="17" endColumn="30" document="0"/>
                <entry offset="0x3e" hidden="true" document="0"/>
                <entry offset="0x40" startLine="18" startColumn="9" endLine="18" endColumn="16" document="0"/>
                <entry offset="0x41" startLine="20" startColumn="13" endLine="20" endColumn="28" document="0"/>
                <entry offset="0x49" startLine="20" startColumn="29" endLine="20" endColumn="42" document="0"/>
                <entry offset="0x54" hidden="true" document="0"/>
                <entry offset="0x55" startLine="21" startColumn="9" endLine="21" endColumn="16" document="0"/>
                <entry offset="0x56" startLine="23" startColumn="5" endLine="23" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x57">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <scope startOffset="0x9" endOffset="0x16">
                    <local name="x" il_index="1" il_start="0x9" il_end="0x16" attributes="0"/>
                </scope>
                <scope startOffset="0x2f" endOffset="0x3d">
                    <local name="ex" il_index="2" il_start="0x2f" il_end="0x3d" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(538944, "DevDiv")>
        <Fact()>
        Public Sub MissingEndWhileSequencePoint()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System


Module MyMod

    Sub Main(args As String())
        Dim x, y, z As ULong, a, b, c As SByte
        x = 10
        y = 20
        z = 30
        a = 1
        b = 2
        c = 3
        Dim ct As Integer = 100
        Do
            Console.WriteLine("Out={0}", y)
            y = y + 2
            While (x > a)
                Do While ct - 50 > a + b * 10
                    b = b + 1
                    Console.Write("b={0} | ", b)
                    Do Until z <= ct / 4
                        Console.Write("z={0} | ", z)
                        Do
                            Console.Write("c={0} | ", c)
                            c = c * 2
                        Loop Until c > ct / 10
                        z = z - 4
                    Loop
                Loop
                x = x - 5
                Console.WriteLine("x={0}", x)
            End While
        Loop While (y < 25)
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            ' startLine="33"
            compilation.VerifyPdb("MyMod.Main",
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="7"/>
                    <slot kind="0" offset="10"/>
                    <slot kind="0" offset="22"/>
                    <slot kind="0" offset="25"/>
                    <slot kind="0" offset="28"/>
                    <slot kind="0" offset="145"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="8" startColumn="9" endLine="8" endColumn="15" document="0"/>
                <entry offset="0x5" startLine="9" startColumn="9" endLine="9" endColumn="15" document="0"/>
                <entry offset="0x9" startLine="10" startColumn="9" endLine="10" endColumn="15" document="0"/>
                <entry offset="0xd" startLine="11" startColumn="9" endLine="11" endColumn="14" document="0"/>
                <entry offset="0xf" startLine="12" startColumn="9" endLine="12" endColumn="14" document="0"/>
                <entry offset="0x12" startLine="13" startColumn="9" endLine="13" endColumn="14" document="0"/>
                <entry offset="0x15" startLine="14" startColumn="13" endLine="14" endColumn="32" document="0"/>
                <entry offset="0x19" startLine="15" startColumn="9" endLine="15" endColumn="11" document="0"/>
                <entry offset="0x1a" startLine="16" startColumn="13" endLine="16" endColumn="44" document="0"/>
                <entry offset="0x2b" startLine="17" startColumn="13" endLine="17" endColumn="22" document="0"/>
                <entry offset="0x43" hidden="true" document="0"/>
                <entry offset="0x48" hidden="true" document="0"/>
                <entry offset="0x4d" startLine="20" startColumn="21" endLine="20" endColumn="30" document="0"/>
                <entry offset="0x54" startLine="21" startColumn="21" endLine="21" endColumn="49" document="0"/>
                <entry offset="0x66" hidden="true" document="0"/>
                <entry offset="0x68" startLine="23" startColumn="25" endLine="23" endColumn="53" document="0"/>
                <entry offset="0x79" startLine="24" startColumn="25" endLine="24" endColumn="27" document="0"/>
                <entry offset="0x7a" startLine="25" startColumn="29" endLine="25" endColumn="57" document="0"/>
                <entry offset="0x8c" startLine="26" startColumn="29" endLine="26" endColumn="38" document="0"/>
                <entry offset="0x93" startLine="27" startColumn="25" endLine="27" endColumn="47" document="0"/>
                <entry offset="0xaf" startLine="28" startColumn="25" endLine="28" endColumn="34" document="0"/>
                <entry offset="0xc7" startLine="29" startColumn="21" endLine="29" endColumn="25" document="0"/>
                <entry offset="0xc8" startLine="22" startColumn="21" endLine="22" endColumn="41" document="0"/>
                <entry offset="0xe0" startLine="30" startColumn="17" endLine="30" endColumn="21" document="0"/>
                <entry offset="0xe1" startLine="19" startColumn="17" endLine="19" endColumn="46" document="0"/>
                <entry offset="0xf8" startLine="31" startColumn="17" endLine="31" endColumn="26" document="0"/>
                <entry offset="0x110" startLine="32" startColumn="17" endLine="32" endColumn="46" document="0"/>
                <entry offset="0x121" startLine="33" startColumn="13" endLine="33" endColumn="22" document="0"/>
                <entry offset="0x122" startLine="18" startColumn="13" endLine="18" endColumn="26" document="0"/>
                <entry offset="0x13f" startLine="34" startColumn="9" endLine="34" endColumn="28" document="0"/>
                <entry offset="0x15f" startLine="35" startColumn="5" endLine="35" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x160">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="y" il_index="1" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="z" il_index="2" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="a" il_index="3" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="b" il_index="4" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="c" il_index="5" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="ct" il_index="6" il_start="0x0" il_end="0x160" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestImplicitLocals()
            Dim source =
<compilation>
    <file>
Option Explicit Off
Option Strict On
Imports System

Module Module1
    Sub Main()
        x = "Hello"
        dim y as string = "world"
        i% = 3
        While i &gt; 0
            Console.WriteLine("{0}, {1}", x, y)
            Console.WriteLine(i)
            q$ = "string"
            i = i% - 1
        End While
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="0"/>
                    <slot kind="0" offset="56"/>
                    <slot kind="0" offset="180"/>
                    <slot kind="0" offset="25"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="20" document="0"/>
                <entry offset="0x7" startLine="8" startColumn="13" endLine="8" endColumn="34" document="0"/>
                <entry offset="0xd" startLine="9" startColumn="9" endLine="9" endColumn="15" document="0"/>
                <entry offset="0xf" hidden="true" document="0"/>
                <entry offset="0x11" startLine="11" startColumn="13" endLine="11" endColumn="48" document="0"/>
                <entry offset="0x23" startLine="12" startColumn="13" endLine="12" endColumn="33" document="0"/>
                <entry offset="0x2a" startLine="13" startColumn="13" endLine="13" endColumn="26" document="0"/>
                <entry offset="0x30" startLine="14" startColumn="13" endLine="14" endColumn="23" document="0"/>
                <entry offset="0x34" startLine="15" startColumn="9" endLine="15" endColumn="18" document="0"/>
                <entry offset="0x35" startLine="10" startColumn="9" endLine="10" endColumn="20" document="0"/>
                <entry offset="0x3f" startLine="16" startColumn="5" endLine="16" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x40">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="i" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="y" il_index="3" il_start="0x0" il_end="0x40" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub AddRemoveHandler()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        AddHandler (v.DomainUnload), del
        RemoveHandler (v.DomainUnload), del

        AppDomain.Unload(v)    
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="123"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <lambda offset="46"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="5" startColumn="13" endLine="6" endColumn="74" document="0"/>
                <entry offset="0x26" startLine="8" startColumn="13" endLine="8" endColumn="45" document="0"/>
                <entry offset="0x31" startLine="10" startColumn="9" endLine="10" endColumn="41" document="0"/>
                <entry offset="0x39" startLine="11" startColumn="9" endLine="11" endColumn="44" document="0"/>
                <entry offset="0x41" startLine="13" startColumn="9" endLine="13" endColumn="28" document="0"/>
                <entry offset="0x48" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x49">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="del" il_index="0" il_start="0x0" il_end="0x49" attributes="0"/>
                <local name="v" il_index="1" il_start="0x0" il_end="0x49" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub SelectCase_NoCaseBlocks()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x4" startLine="6" startColumn="9" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x5" startLine="7" startColumn="5" endLine="7" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_SingleCaseStatement()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
        End Select

        Select Case num
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0xd" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="9" startColumn="9" endLine="9" endColumn="24" document="0"/>
                <entry offset="0x11" startLine="10" startColumn="13" endLine="10" endColumn="22" document="0"/>
                <entry offset="0x14" startLine="11" startColumn="9" endLine="11" endColumn="19" document="0"/>
                <entry offset="0x15" startLine="12" startColumn="5" endLine="12" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x16">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x16" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_OnlyCaseStatements()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
            Case 2
            Case 0, 3 To 8
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x6" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x13" startLine="7" startColumn="13" endLine="7" endColumn="19" document="0"/>
                <entry offset="0x20" startLine="8" startColumn="13" endLine="8" endColumn="27" document="0"/>
                <entry offset="0x37" startLine="9" startColumn="13" endLine="9" endColumn="22" document="0"/>
                <entry offset="0x38" startLine="10" startColumn="9" endLine="10" endColumn="19" document="0"/>
                <entry offset="0x39" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3a">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x3a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_SwitchTable()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
                Console.WriteLine("1")
            Case 2
                Console.WriteLine("2")
            Case 0, 3, 4, 5, 6, Is = 7, 8
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x30" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x31" startLine="7" startColumn="17" endLine="7" endColumn="39" document="0"/>
                <entry offset="0x3e" startLine="8" startColumn="13" endLine="8" endColumn="19" document="0"/>
                <entry offset="0x3f" startLine="9" startColumn="17" endLine="9" endColumn="39" document="0"/>
                <entry offset="0x4c" startLine="10" startColumn="13" endLine="10" endColumn="42" document="0"/>
                <entry offset="0x4f" startLine="11" startColumn="13" endLine="11" endColumn="22" document="0"/>
                <entry offset="0x50" startLine="12" startColumn="17" endLine="12" endColumn="42" document="0"/>
                <entry offset="0x5d" startLine="13" startColumn="9" endLine="13" endColumn="19" document="0"/>
                <entry offset="0x5e" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5f">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_SwitchTable_TempUsed()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num + 1
            Case 1
                Console.WriteLine("")
            Case 2
                Console.WriteLine("2")
            Case 0, 3, 4, 5, 6, Is = 7, 8
                Console.WriteLine("0")
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="28" document="0"/>
                <entry offset="0x34" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x35" startLine="7" startColumn="17" endLine="7" endColumn="38" document="0"/>
                <entry offset="0x42" startLine="8" startColumn="13" endLine="8" endColumn="19" document="0"/>
                <entry offset="0x43" startLine="9" startColumn="17" endLine="9" endColumn="39" document="0"/>
                <entry offset="0x50" startLine="10" startColumn="13" endLine="10" endColumn="42" document="0"/>
                <entry offset="0x51" startLine="11" startColumn="17" endLine="11" endColumn="39" document="0"/>
                <entry offset="0x5e" startLine="12" startColumn="13" endLine="12" endColumn="22" document="0"/>
                <entry offset="0x5f" startLine="13" startColumn="17" endLine="13" endColumn="42" document="0"/>
                <entry offset="0x6c" startLine="14" startColumn="9" endLine="14" endColumn="19" document="0"/>
                <entry offset="0x6d" startLine="15" startColumn="5" endLine="15" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6e">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6e" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_IfList()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num
            Case 1
                Console.WriteLine("1")
            Case 2
                Console.WriteLine("2")
            Case 0, >= 3, <= 8
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x6" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x11" startLine="7" startColumn="17" endLine="7" endColumn="39" document="0"/>
                <entry offset="0x1e" startLine="8" startColumn="13" endLine="8" endColumn="19" document="0"/>
                <entry offset="0x29" startLine="9" startColumn="17" endLine="9" endColumn="39" document="0"/>
                <entry offset="0x36" startLine="10" startColumn="13" endLine="10" endColumn="31" document="0"/>
                <entry offset="0x4a" startLine="12" startColumn="17" endLine="12" endColumn="42" document="0"/>
                <entry offset="0x55" startLine="13" startColumn="9" endLine="13" endColumn="19" document="0"/>
                <entry offset="0x56" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x57">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_IfList_TempUsed()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim num As Integer = 0
        Select Case num + 1
            Case 1
                Console.WriteLine("")
            Case 2
                Console.WriteLine("2")
            Case 0, >= 3, <= 8
                Console.WriteLine("0")
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="32"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x3" startLine="5" startColumn="9" endLine="5" endColumn="28" document="0"/>
                <entry offset="0x8" startLine="6" startColumn="13" endLine="6" endColumn="19" document="0"/>
                <entry offset="0x13" startLine="7" startColumn="17" endLine="7" endColumn="38" document="0"/>
                <entry offset="0x20" startLine="8" startColumn="13" endLine="8" endColumn="19" document="0"/>
                <entry offset="0x2b" startLine="9" startColumn="17" endLine="9" endColumn="39" document="0"/>
                <entry offset="0x38" startLine="10" startColumn="13" endLine="10" endColumn="31" document="0"/>
                <entry offset="0x4a" startLine="11" startColumn="17" endLine="11" endColumn="39" document="0"/>
                <entry offset="0x57" startLine="13" startColumn="17" endLine="13" endColumn="42" document="0"/>
                <entry offset="0x62" startLine="14" startColumn="9" endLine="14" endColumn="19" document="0"/>
                <entry offset="0x63" startLine="15" startColumn="5" endLine="15" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x64">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x64" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_String_SwitchTable_Hash()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02"
                Console.WriteLine("02")
            Case "00", "03", "04", "05", "06", "07", "08"
            Case Else
                Console.WriteLine("Else")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="0"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x135" startLine="6" startColumn="13" endLine="6" endColumn="22" document="0"/>
                <entry offset="0x136" startLine="7" startColumn="17" endLine="7" endColumn="40" document="0"/>
                <entry offset="0x143" startLine="8" startColumn="13" endLine="8" endColumn="22" document="0"/>
                <entry offset="0x144" startLine="9" startColumn="17" endLine="9" endColumn="40" document="0"/>
                <entry offset="0x151" startLine="10" startColumn="13" endLine="10" endColumn="58" document="0"/>
                <entry offset="0x154" startLine="11" startColumn="13" endLine="11" endColumn="22" document="0"/>
                <entry offset="0x155" startLine="12" startColumn="17" endLine="12" endColumn="42" document="0"/>
                <entry offset="0x162" startLine="13" startColumn="9" endLine="13" endColumn="19" document="0"/>
                <entry offset="0x163" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x164">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x164" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub SelectCase_String_SwitchTable_NonHash()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02"
            Case "00"
                Console.WriteLine("00")
            Case Else
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="0"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0x34" startLine="6" startColumn="13" endLine="6" endColumn="22" document="0"/>
                <entry offset="0x35" startLine="7" startColumn="17" endLine="7" endColumn="40" document="0"/>
                <entry offset="0x42" startLine="8" startColumn="13" endLine="8" endColumn="22" document="0"/>
                <entry offset="0x45" startLine="9" startColumn="13" endLine="9" endColumn="22" document="0"/>
                <entry offset="0x46" startLine="10" startColumn="17" endLine="10" endColumn="40" document="0"/>
                <entry offset="0x53" startLine="11" startColumn="13" endLine="11" endColumn="22" document="0"/>
                <entry offset="0x56" startLine="12" startColumn="9" endLine="12" endColumn="19" document="0"/>
                <entry offset="0x57" startLine="13" startColumn="5" endLine="13" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x58">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x58" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="00")
        End Sub

        <Fact()>
        Public Sub SelectCase_String_IfList()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module Module1
    Sub Main()
        Dim str As String = "00"
        Select Case str
            Case "01"
                Console.WriteLine("01")
            Case "02", 3.ToString()
            Case "00"
                Console.WriteLine("00")
        End Select
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            compilation.VerifyPdb("Module1.Main",
<symbols>
    <entryPoint declaringType="Module1" methodName="Main"/>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="15" offset="34"/>
                    <slot kind="temp"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="13" endLine="4" endColumn="33" document="0"/>
                <entry offset="0x7" startLine="5" startColumn="9" endLine="5" endColumn="24" document="0"/>
                <entry offset="0xa" startLine="6" startColumn="13" endLine="6" endColumn="22" document="0"/>
                <entry offset="0x1d" startLine="7" startColumn="17" endLine="7" endColumn="40" document="0"/>
                <entry offset="0x2a" startLine="8" startColumn="13" endLine="8" endColumn="36" document="0"/>
                <entry offset="0x54" startLine="9" startColumn="13" endLine="9" endColumn="22" document="0"/>
                <entry offset="0x67" startLine="10" startColumn="17" endLine="10" endColumn="40" document="0"/>
                <entry offset="0x72" startLine="11" startColumn="9" endLine="11" endColumn="19" document="0"/>
                <entry offset="0x73" startLine="12" startColumn="5" endLine="12" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x74">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x74" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)

            CompileAndVerify(compilation, expectedOutput:="00")
        End Sub

        <Fact()>
        Public Sub DontEmit_AnonymousType_NoKeys()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        Dim o = New With { .a = 1 }
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="C1" name="Method">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="0"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="36" document="0"/>
                <entry offset="0x8" startLine="4" startColumn="5" endLine="4" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub DontEmit_AnonymousType_WithKeys()
            Dim source =
<compilation>
    <file><![CDATA[
Class C1
    Sub Method()
        Dim o = New With { Key .a = 1 }
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="C1" name="Method">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="17" document="0"/>
                <entry offset="0x1" startLine="3" startColumn="13" endLine="3" endColumn="40" document="0"/>
                <entry offset="0x8" startLine="4" startColumn="5" endLine="4" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(727419, "DevDiv")>
        <Fact()>
        Public Sub Bug727419()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict Off
Option Explicit Off
Imports System

Class FooDerived
    Public Sub ComputeMatrix(ByVal rank As Integer)
        Dim I As Integer
        Dim J As Long
        Dim q() As Long
        Dim count As Long
        Dim dims() As Long

        ' allocate space for arrays
        ReDim q(rank)
        ReDim dims(rank)

        ' create the dimensions
        count = 1
        For I = 0 To rank - 1
            q(I) = 0
            dims(I) = CLng(2 ^ I)
            count *= dims(I)
        Next I
    End Sub

End Class

Module Variety
    Sub Main()
        Dim a As New FooDerived()
        a.ComputeMatrix(2)
    End Sub
End Module
' End of File
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("FooDerived.ComputeMatrix",
<symbols>
    <entryPoint declaringType="Variety" methodName="Main"/>
    <methods>
        <method containingType="FooDerived" name="ComputeMatrix" parameterNames="rank">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="30"/>
                    <slot kind="0" offset="53"/>
                    <slot kind="0" offset="78"/>
                    <slot kind="0" offset="105"/>
                    <slot kind="13" offset="271"/>
                    <slot kind="11" offset="271"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="52" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="15" endLine="14" endColumn="22" document="0"/>
                <entry offset="0xa" startLine="15" startColumn="15" endLine="15" endColumn="25" document="0"/>
                <entry offset="0x14" startLine="18" startColumn="9" endLine="18" endColumn="18" document="0"/>
                <entry offset="0x17" startLine="19" startColumn="9" endLine="19" endColumn="30" document="0"/>
                <entry offset="0x1e" hidden="true" document="0"/>
                <entry offset="0x20" startLine="20" startColumn="13" endLine="20" endColumn="21" document="0"/>
                <entry offset="0x25" startLine="21" startColumn="13" endLine="21" endColumn="34" document="0"/>
                <entry offset="0x3f" startLine="22" startColumn="13" endLine="22" endColumn="29" document="0"/>
                <entry offset="0x46" startLine="23" startColumn="9" endLine="23" endColumn="15" document="0"/>
                <entry offset="0x4a" hidden="true" document="0"/>
                <entry offset="0x58" startLine="24" startColumn="5" endLine="24" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x59">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="I" il_index="0" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="J" il_index="1" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="count" il_index="3" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="dims" il_index="4" il_start="0x0" il_end="0x59" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(722627, "DevDiv")>
        <Fact()>
        Public Sub Bug722627()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Friend Module SubMod
    Sub Main()
L0:
        GoTo L2
L1:
        Exit Sub
L2:
        GoTo L1
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            compilation.VerifyPdb("SubMod.Main",
<symbols>
    <entryPoint declaringType="SubMod" methodName="Main"/>
    <methods>
        <method containingType="SubMod" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="1" endLine="4" endColumn="4" document="0"/>
                <entry offset="0x2" startLine="5" startColumn="9" endLine="5" endColumn="16" document="0"/>
                <entry offset="0x4" startLine="6" startColumn="1" endLine="6" endColumn="4" document="0"/>
                <entry offset="0x5" startLine="7" startColumn="9" endLine="7" endColumn="17" document="0"/>
                <entry offset="0x7" startLine="8" startColumn="1" endLine="8" endColumn="4" document="0"/>
                <entry offset="0x8" startLine="9" startColumn="9" endLine="9" endColumn="16" document="0"/>
                <entry offset="0xa" startLine="10" startColumn="5" endLine="10" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(543703, "DevDiv")>
        <Fact()>
        Public Sub DontIncludeMethodAttributesInSeqPoint()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module M1
    Sub Main()
        S()
    End Sub

    <System.Runtime.InteropServices.PreserveSigAttribute()>
    <CLSCompliantAttribute(False)>
    Public Sub S()

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="M1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="9" endLine="4" endColumn="12" document="0"/>
                <entry offset="0x7" startLine="5" startColumn="5" endLine="5" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="M1" name="S">
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="19" document="0"/>
                <entry offset="0x1" startLine="11" startColumn="5" endLine="11" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="M1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(529300, "DevDiv")>
        Public Sub DontShowOperatorNameCTypeInLocals()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1

    Class B2
        Public f As Integer

        Public Sub New(x As Integer)
            f = x
        End Sub

        Shared Widening Operator CType(x As Integer) As B2
            Return New B2(x)
        End Operator
    End Class

    Sub Main()
        Dim x As Integer = 11
        Dim b2 As B2 = x
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="35"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="5" endLine="17" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="18" startColumn="13" endLine="18" endColumn="30" document="0"/>
                <entry offset="0x4" startLine="19" startColumn="13" endLine="19" endColumn="25" document="0"/>
                <entry offset="0xb" startLine="20" startColumn="5" endLine="20" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                <local name="b2" il_index="1" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name=".ctor" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="9" endLine="8" endColumn="37" document="0"/>
                <entry offset="0x7" startLine="9" startColumn="13" endLine="9" endColumn="18" document="0"/>
                <entry offset="0xe" startLine="10" startColumn="9" endLine="10" endColumn="16" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name="op_Implicit" parameterNames="x">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="9" endLine="12" endColumn="59" document="0"/>
                <entry offset="0x1" startLine="13" startColumn="13" endLine="13" endColumn="29" document="0"/>
                <entry offset="0xa" startLine="14" startColumn="9" endLine="14" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(760994, "DevDiv")>
        <Fact()>
        Public Sub Bug760994()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class CLAZZ
    Public FLD1 As Integer = 1
    Public Event Load As Action
    Public FLD2 As Integer = 1

    Public Sub New()

    End Sub

    Private Sub frmMain_Load() Handles Me.Load
    End Sub
End Class


Module Program
    Sub Main(args As String())
        Dim c As New CLAZZ
    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("CLAZZ..ctor",
<symbols>
    <methods>
        <method containingType="CLAZZ" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="5" endLine="8" endColumn="21" document="0"/>
                <entry offset="0x1a" startLine="4" startColumn="12" endLine="4" endColumn="31" document="0"/>
                <entry offset="0x21" startLine="6" startColumn="12" endLine="6" endColumn="31" document="0"/>
                <entry offset="0x28" startLine="10" startColumn="5" endLine="10" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub WRN_PDBConstantStringValueTooLong()
            Dim longStringValue = New String("a"c, 2050)

            Dim source =
            <compilation>
                <file>
Imports System

Module Module1

    Sub Main()
        Const foo as String = "<%= longStringValue %>"

        Console.WriteLine("Hello Word.")
        Console.WriteLine(foo)
    End Sub
End Module
</file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            Dim exebits = New IO.MemoryStream()
            Dim pdbbits = New IO.MemoryStream()
            Dim result = compilation.Emit(exebits, pdbbits)
            result.Diagnostics.Verify()

            'this new warning was abandoned

            'result.Diagnostics.Verify(Diagnostic(ERRID.WRN_PDBConstantStringValueTooLong).WithArguments("foo", longStringValue.Substring(0, 20) & "..."))

            ''ensure that the warning is suppressable
            'compilation = CreateCompilationWithMscorlibAndVBRuntime(source, OptionsExe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(False).
            '    WithSpecificDiagnosticOptions(New Dictionary(Of Integer, ReportWarning) From {{CInt(ERRID.WRN_PDBConstantStringValueTooLong), ReportWarning.Suppress}}))
            'result = compilation.Emit(exebits, Nothing, "DontCare", pdbbits, Nothing)
            'result.Diagnostics.Verify()

            ''ensure that the warning can be turned into an error
            'compilation = CreateCompilationWithMscorlibAndVBRuntime(source, OptionsExe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(False).
            '    WithSpecificDiagnosticOptions(New Dictionary(Of Integer, ReportWarning) From {{CInt(ERRID.WRN_PDBConstantStringValueTooLong), ReportWarning.Error}}))
            'result = compilation.Emit(exebits, Nothing, "DontCare", pdbbits, Nothing)
            'Assert.False(result.Success)
            'result.Diagnostics.Verify(Diagnostic(ERRID.WRN_PDBConstantStringValueTooLong).WithArguments("foo", longStringValue.Substring(0, 20) & "...").WithWarningAsError(True),
            '                              Diagnostic(ERRID.ERR_WarningTreatedAsError).WithArguments("The value assigned to the constant string 'foo' is too long to be used in a PDB file. Consider shortening the value, otherwise the string's value will not be visible in the debugger. Only the debug experience is affected."))

        End Sub

        <Fact>
        Public Sub NoDebugInfoForEmbeddedSymbols()
            Dim source =
<compilation>
    <file>
Imports Microsoft.VisualBasic.Strings

Public Class C
    Public Shared Function F(z As Integer) As Char
        Return ChrW(z)
    End Function
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll.WithEmbedVbCoreRuntime(True))

            ' Dev11 generates debug info for embedded symbols. There is no reason to do so since the source code is not available to the user.

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="C" name="F" parameterNames="z">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="5" endLine="4" endColumn="51" document="0"/>
                <entry offset="0x1" startLine="5" startColumn="9" endLine="5" endColumn="23" document="0"/>
                <entry offset="0xa" startLine="6" startColumn="5" endLine="6" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <type name="Microsoft.VisualBasic.Strings" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="F" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact(), WorkItem(797482, "DevDiv")>
        Public Sub Bug797482()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Module Module1
    Sub Main()
        Console.WriteLine(MakeIncrementer(5)(2))
    End Sub
    Function MakeIncrementer(n As Integer) As Func(Of Integer, Integer)
        Return Function(i)
            Return i + n
        End Function
    End Function
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb("Module1.MakeIncrementer",
<symbols>
    <methods>
        <method containingType="Module1" name="MakeIncrementer" parameterNames="n">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="30" offset="-1"/>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
                <encLambdaMap>
                    <methodOrdinal>1</methodOrdinal>
                    <closure offset="-1"/>
                    <lambda offset="7" closure="0"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="72" document="0"/>
                <entry offset="0x1" hidden="true" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="9" endLine="10" endColumn="21" document="0"/>
                <entry offset="0x1d" startLine="11" startColumn="5" endLine="11" endColumn="17" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Main"/>
                <local name="$VB$Closure_0" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="MakeIncrementer" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' If a synthesized .ctor contains user code (field initializers),
        ''' the method must have a sequence point at
        ''' offset 0 for correct stepping behavior.
        ''' </summary>
        <WorkItem(804681, "DevDiv")>
        <Fact()>
        Public Sub DefaultConstructorWithInitializer()
            Dim source =
<compilation>
    <file><![CDATA[
Class C
    Private o As Object = New Object()
End Class
]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)

            compilation.VerifyPdb("C..ctor",
<symbols>
    <methods>
        <method containingType="C" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x6" startLine="2" startColumn="13" endLine="2" endColumn="39" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x17">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        ''' <summary>
        ''' If a synthesized method contains any user code,
        ''' the method must have a sequence point at
        ''' offset 0 for correct stepping behavior.
        ''' </summary>
        <Fact()>
        Public Sub SequencePointAtOffset0()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Module M
    Private Fn As Func(Of Object, Integer) = Function(x)
            Dim f As Func(Of Object, Integer) = Function(o) 1
            Dim g As Func(Of Func(Of Object, Integer), Func(Of Object, Integer)) = Function(h) Function(y) h(y)
            Return g(f)(Nothing)
        End Function
End Module
]]></file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="M" name=".cctor">
            <customDebugInfo>
                <encLambdaMap>
                    <methodOrdinal>0</methodOrdinal>
                    <closure offset="-84"/>
                    <lambda offset="-243"/>
                    <lambda offset="-182"/>
                    <lambda offset="-84"/>
                    <lambda offset="-72" closure="0"/>
                </encLambdaMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="13" endLine="7" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x16">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="M+_Closure$__0-0" name="_Lambda$__3" parameterNames="y">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="96" endLine="5" endColumn="107" document="0"/>
                <entry offset="0x1" startLine="5" startColumn="108" endLine="5" endColumn="112" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x17">
                <importsforward declaringType="M" methodName=".cctor"/>
            </scope>
        </method>
        <method containingType="M+_Closure$__" name="_Lambda$__0-0" parameterNames="x">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                    <slot kind="0" offset="4"/>
                    <slot kind="0" offset="67"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="46" endLine="3" endColumn="57" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="17" endLine="4" endColumn="62" document="0"/>
                <entry offset="0x26" startLine="5" startColumn="17" endLine="5" endColumn="112" document="0"/>
                <entry offset="0x4b" startLine="6" startColumn="13" endLine="6" endColumn="33" document="0"/>
                <entry offset="0x5b" startLine="7" startColumn="9" endLine="7" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5d">
                <importsforward declaringType="M" methodName=".cctor"/>
                <local name="f" il_index="1" il_start="0x0" il_end="0x5d" attributes="0"/>
                <local name="g" il_index="2" il_start="0x0" il_end="0x5d" attributes="0"/>
            </scope>
        </method>
        <method containingType="M+_Closure$__" name="_Lambda$__0-1" parameterNames="o">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="4" startColumn="49" endLine="4" endColumn="60" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="61" endLine="4" endColumn="62" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="M" methodName=".cctor"/>
            </scope>
        </method>
        <method containingType="M+_Closure$__" name="_Lambda$__0-2" parameterNames="h">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="30" offset="-1"/>
                    <slot kind="21" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="84" endLine="5" endColumn="95" document="0"/>
                <entry offset="0x1" hidden="true" document="0"/>
                <entry offset="0xe" startLine="5" startColumn="96" endLine="5" endColumn="112" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="M" methodName=".cctor"/>
                <local name="$VB$Closure_0" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="25" document="0"/>
                <entry offset="0x15" startLine="7" startColumn="5" endLine="7" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="12" startColumn="13" endLine="15" endColumn="64" document="0"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="20" document="0"/>
                <entry offset="0xa8" startLine="19" startColumn="13" endLine="20" endColumn="38" document="0"/>
                <entry offset="0x100" startLine="22" startColumn="9" endLine="22" endColumn="21" document="0"/>
                <entry offset="0x107" startLine="23" startColumn="5" endLine="23" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x108">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x108" attributes="0"/>
                <local name="qq" il_index="1" il_start="0x0" il_end="0x108" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="26" endLine="13" endColumn="27" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="60" endLine="14" endColumn="67" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="27" endLine="14" endColumn="33" document="0"/>
                <entry offset="0x4" startLine="14" startColumn="39" endLine="14" endColumn="46" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="evenOdd, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x1" startLine="15" startColumn="30" endLine="15" endColumn="44" document="0"/>
                <entry offset="0x2b" startLine="15" startColumn="50" endLine="15" endColumn="64" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5b">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="43" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="54" endLine="15" endColumn="63" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="19" startColumn="25" endLine="19" endColumn="32" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="x">
            <sequencePoints>
                <entry offset="0x0" startLine="20" startColumn="26" endLine="20" endColumn="27" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="14" endColumn="37" document="0"/>
                <entry offset="0x30" startLine="15" startColumn="9" endLine="15" endColumn="36" document="0"/>
                <entry offset="0x3c" startLine="16" startColumn="5" endLine="16" endColumn="12" document="0"/>
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

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="14" endColumn="74" document="0"/>
                <entry offset="0x7d" startLine="15" startColumn="9" endLine="15" endColumn="35" document="0"/>
                <entry offset="0x89" startLine="16" startColumn="5" endLine="16" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="28" endLine="14" endColumn="35" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="68" endLine="14" endColumn="74" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="57" endLine="14" endColumn="64" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="42" document="0"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="29" endLine="15" endColumn="42" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="85" document="0"/>
                <entry offset="0x59" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x65" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="37" endLine="15" endColumn="50" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="64" endLine="15" endColumn="85" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x20">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.VerifyDiagnostics()

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="45" document="0"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="36" document="0"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="32" endLine="15" endColumn="45" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="33" document="0"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="36" document="0"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="33" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="72" document="0"/>
                <entry offset="0x30" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x3c" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="32" endLine="15" endColumn="45" document="0"/>
                <entry offset="0x3" startLine="15" startColumn="59" endLine="15" endColumn="72" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x15">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="67" document="0"/>
                <entry offset="0xa6" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0xb2" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb3">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xb3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xb3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="53" endLine="14" endColumn="60" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="106" document="0"/>
                <entry offset="0xf3" startLine="18" startColumn="9" endLine="18" endColumn="35" document="0"/>
                <entry offset="0xff" startLine="19" startColumn="5" endLine="19" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x100">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x100" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x100" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="63" endLine="16" endColumn="72" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="46" endLine="16" endColumn="55" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="41" endLine="17" endColumn="50" document="0"/>
                <entry offset="0x1" startLine="17" startColumn="93" endLine="17" endColumn="106" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="58" endLine="17" endColumn="67" document="0"/>
                <entry offset="0x6" startLine="17" startColumn="72" endLine="17" endColumn="85" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x14">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="48" document="0"/>
                <entry offset="0xa6" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0xb2" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xb3">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xb3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xb3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="59" endLine="14" endColumn="66" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="19" endColumn="57" document="0"/>
                <entry offset="0x145" startLine="20" startColumn="9" endLine="20" endColumn="35" document="0"/>
                <entry offset="0x151" startLine="21" startColumn="5" endLine="21" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x152">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x152" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x152" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="59" endLine="14" endColumn="66" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="63" endLine="15" endColumn="70" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="69" endLine="16" endColumn="78" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="52" endLine="16" endColumn="61" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x1" startLine="17" startColumn="47" endLine="17" endColumn="61" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="51" endLine="17" endColumn="60" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="41" endLine="18" endColumn="50" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="58" endLine="18" endColumn="67" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-8" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x1" startLine="19" startColumn="43" endLine="19" endColumn="57" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-9" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="19" startColumn="47" endLine="19" endColumn="56" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="79" document="0"/>
                <entry offset="0x7d" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x89" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="41" endLine="15" endColumn="50" document="0"/>
                <entry offset="0x1" startLine="15" startColumn="93" endLine="15" endColumn="106" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="58" endLine="15" endColumn="67" document="0"/>
                <entry offset="0x1" startLine="15" startColumn="72" endLine="15" endColumn="85" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x2" startLine="16" startColumn="56" endLine="16" endColumn="70" document="0"/>
                <entry offset="0x2c" startLine="16" startColumn="72" endLine="16" endColumn="79" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x38">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="60" endLine="16" endColumn="69" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="73" document="0"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="19" endLine="15" endColumn="73" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="78" document="0"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="24" endLine="15" endColumn="78" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="78" document="0"/>
                <entry offset="0x7d" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x89" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8a">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8a" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="24" endLine="15" endColumn="78" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="19" document="0"/>
                <entry offset="0xd" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x19" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
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

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="15" endColumn="19" document="0"/>
                <entry offset="0xd" startLine="16" startColumn="9" endLine="16" endColumn="35" document="0"/>
                <entry offset="0x19" startLine="17" startColumn="5" endLine="17" endColumn="12" document="0"/>
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

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="23" document="0"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="22" endLine="15" endColumn="31" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="32" document="0"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x1" startLine="16" startColumn="18" endLine="16" endColumn="32" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x31">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="22" endLine="16" endColumn="31" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="54" document="0"/>
                <entry offset="0xa1" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0xad" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xae">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xae" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xae" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="52" endLine="14" endColumn="58" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="34" endLine="15" endColumn="47" document="0"/>
                <entry offset="0x8" startLine="15" startColumn="61" endLine="15" endColumn="74" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="$VB$It, $VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0xd" startLine="16" startColumn="31" endLine="16" endColumn="45" document="0"/>
                <entry offset="0x37" startLine="16" startColumn="47" endLine="16" endColumn="54" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x43">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="35" endLine="16" endColumn="44" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="36" document="0"/>
                <entry offset="0x30" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x3c" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="16" endColumn="36" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="22" endLine="16" endColumn="35" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="50" document="0"/>
                <entry offset="0x30" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x3c" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3d">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x3d" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x3d" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="16" endColumn="50" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xab">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="65" endLine="15" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="49" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="36" document="0"/>
                <entry offset="0x59" startLine="18" startColumn="9" endLine="18" endColumn="35" document="0"/>
                <entry offset="0x65" startLine="19" startColumn="5" endLine="19" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="17" endColumn="36" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x58">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="22" endLine="17" endColumn="35" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="0"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="42" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="0"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="0"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x82">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="65" endLine="15" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="0"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="16" endColumn="47" document="0"/>
                <entry offset="0x59" startLine="17" startColumn="9" endLine="17" endColumn="35" document="0"/>
                <entry offset="0x65" startLine="18" startColumn="5" endLine="18" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x66">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x66" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x66" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="13" endLine="15" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xab">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="33" endLine="15" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="79" endLine="15" endColumn="88" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="96" endLine="15" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="0"/>
                <entry offset="0x6" startLine="16" startColumn="24" endLine="16" endColumn="38" document="0"/>
                <entry offset="0x35" startLine="16" startColumn="40" endLine="16" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x46">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="28" endLine="16" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="0"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="0"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="42" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="0"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="0"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="0"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7c">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="65" endLine="16" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-5" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="0"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="14" startColumn="9" endLine="17" endColumn="47" document="0"/>
                <entry offset="0x82" startLine="18" startColumn="9" endLine="18" endColumn="35" document="0"/>
                <entry offset="0x8e" startLine="19" startColumn="5" endLine="19" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x8f" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x8f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="15" startColumn="20" endLine="15" endColumn="21" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="$VB$ItAnonymous">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="13" endLine="16" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa5">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-2" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="33" endLine="16" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="79" endLine="16" endColumn="88" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-4" parameterNames="rangeVar3">
            <sequencePoints>
                <entry offset="0x0" startLine="16" startColumn="96" endLine="16" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-6" parameterNames="$VB$Group">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="24" endLine="17" endColumn="38" document="0"/>
                <entry offset="0x2a" startLine="17" startColumn="40" endLine="17" endColumn="47" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x36">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-7" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="17" startColumn="28" endLine="17" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="12" startColumn="9" endLine="13" endColumn="36" document="0"/>
                <entry offset="0x5e" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5f">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="33" endLine="12" endColumn="40" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="22" endLine="13" endColumn="35" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(824944, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="47" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="19" document="0"/>
                <entry offset="0xe" startLine="8" startColumn="5" endLine="8" endColumn="17" document="0"/>
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
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="15" document="0"/>
                <entry offset="0x1" startLine="12" startColumn="9" endLine="13" endColumn="47" document="0"/>
                <entry offset="0x8a" startLine="14" startColumn="5" endLine="14" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8b">
                <importsforward declaringType="Module1" methodName="Nums"/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x8b" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-0" parameterNames="rangeVar1">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="65" endLine="12" endColumn="71" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2f">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-1" parameterNames="rangeVar2">
            <sequencePoints>
                <entry offset="0x0" startLine="12" startColumn="54" endLine="12" endColumn="61" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
        <method containingType="Module1+_Closure$__" name="_Lambda$__1-3" parameterNames="$VB$It">
            <sequencePoints>
                <entry offset="0x0" startLine="13" startColumn="28" endLine="13" endColumn="37" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(841361, "DevDiv")>
        <Fact()>
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
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected></expected>)

            compilation.VerifyPdb("Module1+_Closure$__._Lambda$__0-0",
<symbols>
    <methods>
        <method containingType="Module1+_Closure$__" name="_Lambda$__0-0" parameterNames="a">
            <sequencePoints>
                <entry offset="0x0" startLine="8" startColumn="25" endLine="8" endColumn="30" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Main"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(846228, "DevDiv")>
        <WorkItem(845078, "DevDiv")>
        <Fact()>
        Public Sub RaiseEvent001()
            Dim source =
<compilation>
    <file><![CDATA[
Public Class IntervalUpdate
    Public Shared Sub Update()
        RaiseEvent IntervalEllapsed()
    End Sub
    
    Shared Sub Main()
        Update()
    End Sub

    Public Shared Event IntervalEllapsed()
End Class
]]></file>
</compilation>

            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(OutputKind.ConsoleApplication)
            defines = defines.Add(KeyValuePair.Create("_MyType", CObj("Console")))

            Dim parseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugDll.WithParseOptions(parseOptions))

            compilation.VerifyPdb(
<symbols>
    <methods>
        <method containingType="IntervalUpdate" name="Update">
            <sequencePoints>
                <entry offset="0x0" startLine="2" startColumn="5" endLine="2" endColumn="31" document="0"/>
                <entry offset="0x1" startLine="3" startColumn="9" endLine="3" endColumn="38" document="0"/>
                <entry offset="0x15" startLine="4" startColumn="5" endLine="4" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x16">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="IntervalUpdate" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="22" document="0"/>
                <entry offset="0x1" startLine="7" startColumn="9" endLine="7" endColumn="17" document="0"/>
                <entry offset="0x7" startLine="8" startColumn="5" endLine="8" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8">
                <importsforward declaringType="IntervalUpdate" methodName="Update"/>
            </scope>
        </method>
        <method containingType="My.MyComputer" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="107" startColumn="9" endLine="107" endColumn="25" document="0"/>
                <entry offset="0x1" startLine="108" startColumn="13" endLine="108" endColumn="25" document="0"/>
                <entry offset="0x8" startLine="109" startColumn="9" endLine="109" endColumn="16" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="121" startColumn="13" endLine="121" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="122" startColumn="17" endLine="122" endColumn="62" document="0"/>
                <entry offset="0xe" startLine="123" startColumn="13" endLine="123" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="133" startColumn="13" endLine="133" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="134" startColumn="17" endLine="134" endColumn="57" document="0"/>
                <entry offset="0xe" startLine="135" startColumn="13" endLine="135" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="144" startColumn="13" endLine="144" endColumn="16" document="0"/>
                <entry offset="0x1" startLine="145" startColumn="17" endLine="145" endColumn="58" document="0"/>
                <entry offset="0xe" startLine="146" startColumn="13" endLine="146" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="237" startColumn="14" endLine="237" endColumn="17" document="0"/>
                <entry offset="0x1" startLine="238" startColumn="17" endLine="238" endColumn="67" document="0"/>
                <entry offset="0xe" startLine="239" startColumn="13" endLine="239" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor">
            <sequencePoints>
                <entry offset="0x0" startLine="126" startColumn="26" endLine="126" endColumn="97" document="0"/>
                <entry offset="0xa" startLine="137" startColumn="26" endLine="137" endColumn="95" document="0"/>
                <entry offset="0x14" startLine="148" startColumn="26" endLine="148" endColumn="136" document="0"/>
                <entry offset="0x1e" startLine="284" startColumn="26" endLine="284" endColumn="105" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="247" startColumn="13" endLine="247" endColumn="75" document="0"/>
                <entry offset="0x1" startLine="248" startColumn="17" endLine="248" endColumn="40" document="0"/>
                <entry offset="0x10" startLine="249" startColumn="13" endLine="249" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x12">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="251" startColumn="13" endLine="251" endColumn="63" document="0"/>
                <entry offset="0x1" startLine="252" startColumn="17" endLine="252" endColumn="42" document="0"/>
                <entry offset="0xa" startLine="253" startColumn="13" endLine="253" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="255" startColumn="13" endLine="255" endColumn="72" document="0"/>
                <entry offset="0x1" startLine="256" startColumn="17" endLine="256" endColumn="46" document="0"/>
                <entry offset="0xe" startLine="257" startColumn="13" endLine="257" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="259" startColumn="13" endLine="259" endColumn="59" document="0"/>
                <entry offset="0x1" startLine="260" startColumn="17" endLine="260" endColumn="39" document="0"/>
                <entry offset="0xa" startLine="261" startColumn="13" endLine="261" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="264" startColumn="12" endLine="264" endColumn="95" document="0"/>
                <entry offset="0x1" startLine="265" startColumn="17" endLine="265" endColumn="44" document="0"/>
                <entry offset="0xe" startLine="266" startColumn="21" endLine="266" endColumn="35" document="0"/>
                <entry offset="0x16" startLine="267" startColumn="17" endLine="267" endColumn="21" document="0"/>
                <entry offset="0x17" startLine="268" startColumn="21" endLine="268" endColumn="36" document="0"/>
                <entry offset="0x1b" startLine="270" startColumn="13" endLine="270" endColumn="25" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencePoints>
                <entry offset="0x0" startLine="273" startColumn="13" endLine="273" endColumn="71" document="0"/>
                <entry offset="0x1" startLine="274" startColumn="17" endLine="274" endColumn="35" document="0"/>
                <entry offset="0x8" startLine="275" startColumn="13" endLine="275" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="279" startColumn="13" endLine="279" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="280" startColumn="16" endLine="280" endColumn="28" document="0"/>
                <entry offset="0x8" startLine="281" startColumn="13" endLine="281" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="-1"/>
                    <slot kind="temp"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="341" startColumn="17" endLine="341" endColumn="20" document="0"/>
                <entry offset="0x1" startLine="342" startColumn="21" endLine="342" endColumn="59" document="0"/>
                <entry offset="0x12" startLine="342" startColumn="60" endLine="342" endColumn="87" document="0"/>
                <entry offset="0x1c" startLine="343" startColumn="21" endLine="343" endColumn="47" document="0"/>
                <entry offset="0x24" startLine="344" startColumn="17" endLine="344" endColumn="24" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" startLine="350" startColumn="13" endLine="350" endColumn="29" document="0"/>
                <entry offset="0x1" startLine="351" startColumn="17" endLine="351" endColumn="29" document="0"/>
                <entry offset="0x8" startLine="352" startColumn="13" endLine="352" endColumn="20" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(876518)>
        <Fact>
        Public Sub WinFormMain()
            Dim source =
<compilation>
    <file>
&lt;Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()&gt; _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    &lt;System.Diagnostics.DebuggerNonUserCode()&gt; _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    &lt;System.Diagnostics.DebuggerStepThrough()&gt; _
    Private Sub InitializeComponent()
        components = New System.ComponentModel.Container()
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Text = "Form1"
    End Sub

End Class
    </file>
</compilation>
            Dim defines = PredefinedPreprocessorSymbols.AddPredefinedPreprocessorSymbols(
                OutputKind.WindowsApplication,
                KeyValuePair.Create(Of String, Object)("_MyType", "WindowsForms"),
                KeyValuePair.Create(Of String, Object)("Config", "Debug"),
                KeyValuePair.Create(Of String, Object)("DEBUG", -1),
                KeyValuePair.Create(Of String, Object)("TRACE", -1),
                KeyValuePair.Create(Of String, Object)("PLATFORM", "AnyCPU"))

            Dim parseOptions As VisualBasicParseOptions = New VisualBasicParseOptions(preprocessorSymbols:=defines)
            Dim compOptions As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(
                OutputKind.WindowsApplication,
                optimizationLevel:=OptimizationLevel.Debug,
                parseOptions:=parseOptions,
                mainTypeName:="My.MyApplication")
            Dim comp = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemWindowsFormsRef}, compOptions)
            comp.VerifyDiagnostics()

            ' Just care that there's at least one non-hidden sequence point.
            comp.VerifyPdb("My.MyApplication.Main",
<symbols>
    <entryPoint declaringType="My.MyApplication" methodName="Main" parameterNames="Args"/>
    <methods>
        <method containingType="My.MyApplication" name="Main" parameterNames="Args">
            <sequencePoints>
                <entry offset="0x0" startLine="76" startColumn="9" endLine="76" endColumn="55" document="0"/>
                <entry offset="0x1" startLine="77" startColumn="13" endLine="77" endColumn="16" document="0"/>
                <entry offset="0x2" startLine="78" startColumn="16" endLine="78" endColumn="133" document="0"/>
                <entry offset="0xf" startLine="79" startColumn="13" endLine="79" endColumn="20" document="0"/>
                <entry offset="0x11" startLine="80" startColumn="13" endLine="80" endColumn="20" document="0"/>
                <entry offset="0x12" startLine="81" startColumn="13" endLine="81" endColumn="37" document="0"/>
                <entry offset="0x1e" startLine="82" startColumn="9" endLine="82" endColumn="16" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1f">
                <currentnamespace name="My"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub SynthesizedVariableForSelectCastValue()
            Dim source =
<compilation>
    <file>
Imports System
Class C
    Sub F(args As String())
        Select Case args(0)
            Case "a"
                Console.WriteLine(1)
            Case "b"
                Console.WriteLine(2)
            Case "c"
                Console.WriteLine(3)
        End Select
    End Sub
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.DebugDll)
            c.VerifyDiagnostics()
            c.VerifyPdb("C.F",
<symbols>
    <methods>
        <method containingType="C" name="F" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="15" offset="0"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="28" document="0"/>
                <entry offset="0x1" startLine="4" startColumn="9" endLine="4" endColumn="28" document="0"/>
                <entry offset="0x32" startLine="5" startColumn="13" endLine="5" endColumn="21" document="0"/>
                <entry offset="0x33" startLine="6" startColumn="17" endLine="6" endColumn="37" document="0"/>
                <entry offset="0x3c" startLine="7" startColumn="13" endLine="7" endColumn="21" document="0"/>
                <entry offset="0x3d" startLine="8" startColumn="17" endLine="8" endColumn="37" document="0"/>
                <entry offset="0x46" startLine="9" startColumn="13" endLine="9" endColumn="21" document="0"/>
                <entry offset="0x47" startLine="10" startColumn="17" endLine="10" endColumn="37" document="0"/>
                <entry offset="0x50" startLine="11" startColumn="9" endLine="11" endColumn="19" document="0"/>
                <entry offset="0x51" startLine="12" startColumn="5" endLine="12" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x52">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>

</symbols>)
        End Sub

        <Fact>
        Public Sub Constant_AllTypes()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Collections.Generic
'Imports Microsoft.VisualBasic.Strings

Class X 
End Class

Public Class C(Of S)
    Enum EnumI1 As SByte    : A : End Enum
    Enum EnumU1 As Byte     : A : End Enum 
    Enum EnumI2 As Short    : A : End Enum 
    Enum EnumU2 As UShort   : A : End Enum
    Enum EnumI4 As Integer  : A : End Enum
    Enum EnumU4 As UInteger : A : End Enum
    Enum EnumI8 As Long     : A : End Enum
    Enum EnumU8 As ULong    : A : End Enum

    Public Sub F(Of T)()
        Const B As Boolean = Nothing
        Const C As Char = Nothing
        Const I1 As SByte = 0
        Const U1 As Byte = 0
        Const I2 As Short = 0
        Const U2 As UShort = 0
        Const I4 As Integer = 0
        Const U4 As UInteger = 0
        Const I8 As Long = 0
        Const U8 As ULong = 0
        Const R4 As Single = 0
        Const R8 As Double = 0

        Const EI1 As C(Of Integer).EnumI1 = 0
        Const EU1 As C(Of Integer).EnumU1 = 0
        Const EI2 As C(Of Integer).EnumI2 = 0
        Const EU2 As C(Of Integer).EnumU2 = 0
        Const EI4 As C(Of Integer).EnumI4 = 0
        Const EU4 As C(Of Integer).EnumU4 = 0
        Const EI8 As C(Of Integer).EnumI8 = 0
        Const EU8 As C(Of Integer).EnumU8 = 0

        'Const StrWithNul As String = ChrW(0)
        Const EmptyStr As String = ""
        Const NullStr As String = Nothing
        Const NullObject As Object = Nothing
       
        Const D As Decimal = Nothing
        Const DT As DateTime = #1-1-2015#
    End Sub
End Class
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, options:=TestOptions.DebugDll.WithEmbedVbCoreRuntime(True))

            c.VerifyPdb("C`1.F",
<symbols>
    <methods>
        <method containingType="C`1" name="F">
            <sequencePoints>
                <entry offset="0x0" startLine="18" startColumn="5" endLine="18" endColumn="25" document="0"/>
                <entry offset="0x1" startLine="48" startColumn="5" endLine="48" endColumn="12" document="0"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <constant name="B" value="0" type="Boolean"/>
                <constant name="C" value="0" type="Char"/>
                <constant name="I1" value="0" type="SByte"/>
                <constant name="U1" value="0" type="Byte"/>
                <constant name="I2" value="0" type="Int16"/>
                <constant name="U2" value="0" type="UInt16"/>
                <constant name="I4" value="0" type="Int32"/>
                <constant name="U4" value="0" type="UInt32"/>
                <constant name="I8" value="0" type="Int64"/>
                <constant name="U8" value="0" type="UInt64"/>
                <constant name="R4" value="0" type="Single"/>
                <constant name="R8" value="0" type="Double"/>
                <constant name="EI1" value="0" signature="15-11-10-01-08"/>
                <constant name="EU1" value="0" signature="15-11-14-01-08"/>
                <constant name="EI2" value="0" signature="15-11-18-01-08"/>
                <constant name="EU2" value="0" signature="15-11-1C-01-08"/>
                <constant name="EI4" value="null" signature="15-11-20-01-08"/>
                <constant name="EU4" value="0" signature="15-11-24-01-08"/>
                <constant name="EI8" value="0" signature="15-11-28-01-08"/>
                <constant name="EU8" value="0" signature="15-11-2C-01-08"/>
                <constant name="EmptyStr" value="" type="String"/>
                <constant name="NullStr" value="null" type="String"/>
                <constant name="NullObject" value="null" type="Object"/>
                <constant name="D" value="0" type="Decimal"/>
                <constant name="DT" value="01/01/2015 00:00:00" type="DateTime"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class

End Namespace
