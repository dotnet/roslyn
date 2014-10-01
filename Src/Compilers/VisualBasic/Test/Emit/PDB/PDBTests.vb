' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports System.Xml.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    ' TODO: Verify the custom debug info - the current text is just based on the current output.
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

            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="C1" name="Method" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="17" file_ref="0"/>
                <entry il_offset="0x1" start_row="3" start_column="9" end_row="3" end_column="50" file_ref="0"/>
                <entry il_offset="0xc" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="My.MyComputer" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="107" start_column="9" end_row="107" end_column="25" file_ref="0"/>
                <entry il_offset="0x1" start_row="108" start_column="13" end_row="108" end_column="25" file_ref="0"/>
                <entry il_offset="0x8" start_row="109" start_column="9" end_row="109" end_column="16" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="121" start_column="13" end_row="121" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="122" start_column="17" end_row="122" end_column="62" file_ref="0"/>
                <entry il_offset="0xe" start_row="123" start_column="13" end_row="123" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="133" start_column="13" end_row="133" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="134" start_column="17" end_row="134" end_column="57" file_ref="0"/>
                <entry il_offset="0xe" start_row="135" start_column="13" end_row="135" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="144" start_column="13" end_row="144" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="145" start_column="17" end_row="145" end_column="58" file_ref="0"/>
                <entry il_offset="0xe" start_row="146" start_column="13" end_row="146" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="237" start_column="14" end_row="237" end_column="17" file_ref="0"/>
                <entry il_offset="0x1" start_row="238" start_column="17" end_row="238" end_column="67" file_ref="0"/>
                <entry il_offset="0xe" start_row="239" start_column="13" end_row="239" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="126" start_column="26" end_row="126" end_column="97" file_ref="0"/>
                <entry il_offset="0xa" start_row="137" start_column="26" end_row="137" end_column="95" file_ref="0"/>
                <entry il_offset="0x14" start_row="148" start_column="26" end_row="148" end_column="136" file_ref="0"/>
                <entry il_offset="0x1e" start_row="284" start_column="26" end_row="284" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="247" start_column="13" end_row="247" end_column="75" file_ref="0"/>
                <entry il_offset="0x1" start_row="248" start_column="17" end_row="248" end_column="40" file_ref="0"/>
                <entry il_offset="0x10" start_row="249" start_column="13" end_row="249" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x12">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="251" start_column="13" end_row="251" end_column="63" file_ref="0"/>
                <entry il_offset="0x1" start_row="252" start_column="17" end_row="252" end_column="42" file_ref="0"/>
                <entry il_offset="0xa" start_row="253" start_column="13" end_row="253" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="255" start_column="13" end_row="255" end_column="72" file_ref="0"/>
                <entry il_offset="0x1" start_row="256" start_column="17" end_row="256" end_column="46" file_ref="0"/>
                <entry il_offset="0xe" start_row="257" start_column="13" end_row="257" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="259" start_column="13" end_row="259" end_column="59" file_ref="0"/>
                <entry il_offset="0x1" start_row="260" start_column="17" end_row="260" end_column="39" file_ref="0"/>
                <entry il_offset="0xa" start_row="261" start_column="13" end_row="261" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <sequencepoints total="6">
                <entry il_offset="0x0" start_row="264" start_column="12" end_row="264" end_column="95" file_ref="0"/>
                <entry il_offset="0x1" start_row="265" start_column="17" end_row="265" end_column="44" file_ref="0"/>
                <entry il_offset="0xe" start_row="266" start_column="21" end_row="266" end_column="35" file_ref="0"/>
                <entry il_offset="0x16" start_row="267" start_column="17" end_row="267" end_column="21" file_ref="0"/>
                <entry il_offset="0x17" start_row="268" start_column="21" end_row="268" end_column="36" file_ref="0"/>
                <entry il_offset="0x1b" start_row="270" start_column="13" end_row="270" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="273" start_column="13" end_row="273" end_column="71" file_ref="0"/>
                <entry il_offset="0x1" start_row="274" start_column="17" end_row="274" end_column="35" file_ref="0"/>
                <entry il_offset="0x8" start_row="275" start_column="13" end_row="275" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="279" start_column="13" end_row="279" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="280" start_column="16" end_row="280" end_column="28" file_ref="0"/>
                <entry il_offset="0x8" start_row="281" start_column="13" end_row="281" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance" parameterNames="">
            <sequencepoints total="5">
                <entry il_offset="0x0" start_row="341" start_column="17" end_row="341" end_column="20" file_ref="0"/>
                <entry il_offset="0x1" start_row="342" start_column="21" end_row="342" end_column="59" file_ref="0"/>
                <entry il_offset="0x12" start_row="342" start_column="60" end_row="342" end_column="87" file_ref="0"/>
                <entry il_offset="0x1c" start_row="343" start_column="21" end_row="343" end_column="47" file_ref="0"/>
                <entry il_offset="0x24" start_row="344" start_column="17" end_row="344" end_column="24" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="350" start_column="13" end_row="350" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="351" start_column="17" end_row="351" end_column="29" file_ref="0"/>
                <entry il_offset="0x8" start_row="352" start_column="13" end_row="352" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = GetPdbXml(compilation, "M1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="M1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="M1" name="Main" parameterNames="">
            <sequencepoints total="22">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="22" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" start_row="7" start_column="9" end_row="7" end_column="12" file_ref="0"/>
                <entry il_offset="0x4" start_row="8" start_column="17" end_row="8" end_column="34" file_ref="0"/>
                <entry il_offset="0xa" start_row="9" start_column="1" end_row="9" end_column="8" file_ref="0"/>
                <entry il_offset="0xb" start_row="10" start_column="1" end_row="10" end_column="8" file_ref="0"/>
                <entry il_offset="0xc" start_row="11" start_column="13" end_row="11" end_column="26" file_ref="0"/>
                <entry il_offset="0x14" start_row="12" start_column="17" end_row="12" end_column="38" file_ref="0"/>
                <entry il_offset="0x1a" start_row="13" start_column="13" end_row="13" end_column="19" file_ref="0"/>
                <entry il_offset="0x1d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x24" start_row="14" start_column="9" end_row="14" end_column="30" file_ref="0"/>
                <entry il_offset="0x25" start_row="15" start_column="17" end_row="15" end_column="34" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="13" end_row="16" end_column="33" file_ref="0"/>
                <entry il_offset="0x33" start_row="17" start_column="13" end_row="17" end_column="18" file_ref="0"/>
                <entry il_offset="0x35" start_row="18" start_column="13" end_row="18" end_column="24" file_ref="0"/>
                <entry il_offset="0x3c" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x3e" start_row="19" start_column="9" end_row="19" end_column="16" file_ref="0"/>
                <entry il_offset="0x3f" start_row="20" start_column="17" end_row="20" end_column="34" file_ref="0"/>
                <entry il_offset="0x46" start_row="21" start_column="13" end_row="21" end_column="33" file_ref="0"/>
                <entry il_offset="0x4e" start_row="22" start_column="9" end_row="22" end_column="16" file_ref="0"/>
                <entry il_offset="0x4f" start_row="24" start_column="9" end_row="24" end_column="29" file_ref="0"/>
                <entry il_offset="0x56" start_row="26" start_column="5" end_row="26" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
                <local name="y" il_index="1" il_start="0x4" il_end="0x1a" attributes="0"/>
                <local name="ex" il_index="3" il_start="0x1d" il_end="0x3b" attributes="0"/>
                <local name="z" il_index="4" il_start="0x25" il_end="0x3b" attributes="0"/>
                <local name="q" il_index="5" il_start="0x3f" il_end="0x4c" attributes="0"/>
            </locals>
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
</symbols>
            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "M1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="M1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="M1" name="Main" parameterNames="">
            <sequencepoints total="21">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="22" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" start_row="7" start_column="9" end_row="7" end_column="12" file_ref="0"/>
                <entry il_offset="0x4" start_row="8" start_column="17" end_row="8" end_column="34" file_ref="0"/>
                <entry il_offset="0xa" start_row="9" start_column="1" end_row="9" end_column="8" file_ref="0"/>
                <entry il_offset="0xb" start_row="10" start_column="1" end_row="10" end_column="8" file_ref="0"/>
                <entry il_offset="0xc" start_row="11" start_column="13" end_row="11" end_column="22" file_ref="0"/>
                <entry il_offset="0x12" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x25" start_row="12" start_column="9" end_row="12" end_column="60" file_ref="0"/>
                <entry il_offset="0x33" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x34" start_row="13" start_column="17" end_row="13" end_column="34" file_ref="0"/>
                <entry il_offset="0x3a" start_row="14" start_column="13" end_row="14" end_column="33" file_ref="0"/>
                <entry il_offset="0x41" start_row="15" start_column="13" end_row="15" end_column="18" file_ref="0"/>
                <entry il_offset="0x43" start_row="16" start_column="13" end_row="16" end_column="24" file_ref="0"/>
                <entry il_offset="0x4a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x4c" start_row="17" start_column="9" end_row="17" end_column="16" file_ref="0"/>
                <entry il_offset="0x4d" start_row="18" start_column="17" end_row="18" end_column="34" file_ref="0"/>
                <entry il_offset="0x54" start_row="19" start_column="13" end_row="19" end_column="33" file_ref="0"/>
                <entry il_offset="0x5c" start_row="20" start_column="9" end_row="20" end_column="16" file_ref="0"/>
                <entry il_offset="0x5d" start_row="22" start_column="9" end_row="22" end_column="29" file_ref="0"/>
                <entry il_offset="0x64" start_row="24" start_column="5" end_row="24" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x65" attributes="0"/>
                <local name="y" il_index="1" il_start="0x4" il_end="0xf" attributes="0"/>
                <local name="ex" il_index="2" il_start="0x12" il_end="0x49" attributes="0"/>
                <local name="z" il_index="3" il_start="0x34" il_end="0x49" attributes="0"/>
                <local name="q" il_index="4" il_start="0x4d" il_end="0x5a" attributes="0"/>
            </locals>
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
</symbols>
            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="8">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="5" start_column="13" end_row="5" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x5" start_row="7" start_column="17" end_row="7" end_column="37" file_ref="0"/>
                <entry il_offset="0x9" start_row="8" start_column="13" end_row="8" end_column="18" file_ref="0"/>
                <entry il_offset="0xb" start_row="9" start_column="9" end_row="9" end_column="13" file_ref="0"/>
                <entry il_offset="0xc" start_row="6" start_column="9" end_row="6" end_column="26" file_ref="0"/>
                <entry il_offset="0x17" start_row="10" start_column="5" end_row="10" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
                <local name="y" il_index="1" il_start="0x5" il_end="0xb" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x18">
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x18" attributes="0"/>
                <scope startOffset="0x5" endOffset="0xb">
                    <local name="y" il_index="1" il_start="0x5" il_end="0xb" attributes="0"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "C1..ctor")

            Dim expected =
<symbols>
    <methods>
        <method containingType="C1" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="14" file_ref="0"/>
                <entry il_offset="0x7" start_row="3" start_column="9" end_row="3" end_column="50" file_ref="0"/>
                <entry il_offset="0x12" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x13">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "C1..ctor")

            Dim expected =
<symbols>
    <methods>
        <method containingType="C1" name=".ctor" parameterNames="">
            <sequencepoints total="5">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="14" file_ref="0"/>
                <entry il_offset="0x7" start_row="3" start_column="9" end_row="3" end_column="16" file_ref="0"/>
                <entry il_offset="0x8" start_row="4" start_column="9" end_row="4" end_column="16" file_ref="0"/>
                <entry il_offset="0x9" start_row="5" start_column="9" end_row="5" end_column="16" file_ref="0"/>
                <entry il_offset="0xa" start_row="7" start_column="9" end_row="7" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xc">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <sequencepoints total="36">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" start_row="6" start_column="31" end_row="6" end_column="49" file_ref="0"/>
                <entry il_offset="0xb" start_row="8" start_column="9" end_row="8" end_column="23" file_ref="0"/>
                <entry il_offset="0x17" start_row="8" start_column="28" end_row="8" end_column="46" file_ref="0"/>
                <entry il_offset="0x1d" start_row="8" start_column="49" end_row="8" end_column="69" file_ref="0"/>
                <entry il_offset="0x26" start_row="8" start_column="70" end_row="8" end_column="74" file_ref="0"/>
                <entry il_offset="0x27" start_row="8" start_column="75" end_row="8" end_column="99" file_ref="0"/>
                <entry il_offset="0x32" start_row="8" start_column="102" end_row="8" end_column="127" file_ref="0"/>
                <entry il_offset="0x3d" start_row="9" start_column="9" end_row="9" end_column="23" file_ref="0"/>
                <entry il_offset="0x49" start_row="9" start_column="24" end_row="9" end_column="47" file_ref="0"/>
                <entry il_offset="0x54" start_row="9" start_column="50" end_row="9" end_column="74" file_ref="0"/>
                <entry il_offset="0x61" start_row="9" start_column="75" end_row="9" end_column="79" file_ref="0"/>
                <entry il_offset="0x62" start_row="9" start_column="84" end_row="9" end_column="103" file_ref="0"/>
                <entry il_offset="0x69" start_row="9" start_column="106" end_row="9" end_column="126" file_ref="0"/>
                <entry il_offset="0x71" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x73" start_row="12" start_column="13" end_row="12" end_column="26" file_ref="0"/>
                <entry il_offset="0x7e" start_row="13" start_column="17" end_row="13" end_column="40" file_ref="0"/>
                <entry il_offset="0x89" start_row="23" start_column="13" end_row="23" end_column="19" file_ref="0"/>
                <entry il_offset="0x8c" start_row="14" start_column="13" end_row="14" end_column="30" file_ref="0"/>
                <entry il_offset="0x97" start_row="15" start_column="21" end_row="15" end_column="40" file_ref="0"/>
                <entry il_offset="0x9e" start_row="16" start_column="17" end_row="16" end_column="38" file_ref="0"/>
                <entry il_offset="0xa6" start_row="23" start_column="13" end_row="23" end_column="19" file_ref="0"/>
                <entry il_offset="0xa9" start_row="17" start_column="13" end_row="17" end_column="30" file_ref="0"/>
                <entry il_offset="0xb4" start_row="18" start_column="21" end_row="18" end_column="40" file_ref="0"/>
                <entry il_offset="0xbb" start_row="19" start_column="17" end_row="19" end_column="38" file_ref="0"/>
                <entry il_offset="0xc3" start_row="23" start_column="13" end_row="23" end_column="19" file_ref="0"/>
                <entry il_offset="0xc6" start_row="20" start_column="13" end_row="20" end_column="17" file_ref="0"/>
                <entry il_offset="0xc7" start_row="21" start_column="21" end_row="21" end_column="42" file_ref="0"/>
                <entry il_offset="0xce" start_row="22" start_column="17" end_row="22" end_column="38" file_ref="0"/>
                <entry il_offset="0xd6" start_row="23" start_column="13" end_row="23" end_column="19" file_ref="0"/>
                <entry il_offset="0xd7" start_row="25" start_column="17" end_row="25" end_column="40" file_ref="0"/>
                <entry il_offset="0xdc" start_row="26" start_column="13" end_row="26" end_column="21" file_ref="0"/>
                <entry il_offset="0xdf" start_row="27" start_column="9" end_row="27" end_column="13" file_ref="0"/>
                <entry il_offset="0xe0" start_row="11" start_column="9" end_row="11" end_column="23" file_ref="0"/>
                <entry il_offset="0xe8" start_row="29" start_column="5" end_row="29" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0xe9" attributes="0"/>
                <local name="xx" il_index="1" il_start="0x0" il_end="0xe9" attributes="0"/>
                <local name="s" il_index="3" il_start="0x17" il_end="0x23" attributes="0"/>
                <local name="s" il_index="4" il_start="0x62" il_end="0x70" attributes="0"/>
                <local name="newX" il_index="5" il_start="0x73" il_end="0xdf" attributes="0"/>
                <local name="s2" il_index="6" il_start="0x97" il_end="0xa6" attributes="0"/>
                <local name="s3" il_index="7" il_start="0xb4" il_end="0xc3" attributes="0"/>
                <local name="e1" il_index="8" il_start="0xc7" il_end="0xd6" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <sequencepoints total="23">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x5" start_row="8" start_column="13" end_row="8" end_column="26" file_ref="0"/>
                <entry il_offset="0x10" start_row="9" start_column="17" end_row="9" end_column="40" file_ref="0"/>
                <entry il_offset="0x1b" start_row="19" start_column="13" end_row="19" end_column="19" file_ref="0"/>
                <entry il_offset="0x1e" start_row="10" start_column="13" end_row="10" end_column="30" file_ref="0"/>
                <entry il_offset="0x29" start_row="11" start_column="21" end_row="11" end_column="40" file_ref="0"/>
                <entry il_offset="0x2f" start_row="12" start_column="17" end_row="12" end_column="38" file_ref="0"/>
                <entry il_offset="0x36" start_row="19" start_column="13" end_row="19" end_column="19" file_ref="0"/>
                <entry il_offset="0x39" start_row="13" start_column="13" end_row="13" end_column="30" file_ref="0"/>
                <entry il_offset="0x44" start_row="14" start_column="21" end_row="14" end_column="40" file_ref="0"/>
                <entry il_offset="0x4b" start_row="15" start_column="17" end_row="15" end_column="38" file_ref="0"/>
                <entry il_offset="0x53" start_row="19" start_column="13" end_row="19" end_column="19" file_ref="0"/>
                <entry il_offset="0x56" start_row="16" start_column="13" end_row="16" end_column="17" file_ref="0"/>
                <entry il_offset="0x57" start_row="17" start_column="21" end_row="17" end_column="42" file_ref="0"/>
                <entry il_offset="0x5e" start_row="18" start_column="17" end_row="18" end_column="38" file_ref="0"/>
                <entry il_offset="0x66" start_row="19" start_column="13" end_row="19" end_column="19" file_ref="0"/>
                <entry il_offset="0x67" start_row="21" start_column="17" end_row="21" end_column="40" file_ref="0"/>
                <entry il_offset="0x6b" start_row="22" start_column="13" end_row="22" end_column="21" file_ref="0"/>
                <entry il_offset="0x6d" start_row="23" start_column="9" end_row="23" end_column="13" file_ref="0"/>
                <entry il_offset="0x6e" start_row="7" start_column="9" end_row="7" end_column="23" file_ref="0"/>
                <entry il_offset="0x76" start_row="25" start_column="5" end_row="25" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x77" attributes="0"/>
                <local name="newX" il_index="1" il_start="0x5" il_end="0x6d" attributes="0"/>
                <local name="s2" il_index="3" il_start="0x29" il_end="0x36" attributes="0"/>
                <local name="s3" il_index="4" il_start="0x44" il_end="0x53" attributes="0"/>
                <local name="e1" il_index="5" il_start="0x57" il_end="0x66" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <sequencepoints total="22">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" start_row="8" start_column="9" end_row="8" end_column="11" file_ref="0"/>
                <entry il_offset="0x4" start_row="9" start_column="13" end_row="9" end_column="26" file_ref="0"/>
                <entry il_offset="0xf" start_row="10" start_column="17" end_row="10" end_column="40" file_ref="0"/>
                <entry il_offset="0x1a" start_row="20" start_column="13" end_row="20" end_column="19" file_ref="0"/>
                <entry il_offset="0x1d" start_row="11" start_column="13" end_row="11" end_column="30" file_ref="0"/>
                <entry il_offset="0x28" start_row="12" start_column="21" end_row="12" end_column="40" file_ref="0"/>
                <entry il_offset="0x2e" start_row="13" start_column="17" end_row="13" end_column="38" file_ref="0"/>
                <entry il_offset="0x35" start_row="20" start_column="13" end_row="20" end_column="19" file_ref="0"/>
                <entry il_offset="0x38" start_row="14" start_column="13" end_row="14" end_column="30" file_ref="0"/>
                <entry il_offset="0x43" start_row="15" start_column="21" end_row="15" end_column="40" file_ref="0"/>
                <entry il_offset="0x4a" start_row="16" start_column="17" end_row="16" end_column="38" file_ref="0"/>
                <entry il_offset="0x52" start_row="20" start_column="13" end_row="20" end_column="19" file_ref="0"/>
                <entry il_offset="0x55" start_row="17" start_column="13" end_row="17" end_column="17" file_ref="0"/>
                <entry il_offset="0x56" start_row="18" start_column="21" end_row="18" end_column="42" file_ref="0"/>
                <entry il_offset="0x5d" start_row="19" start_column="17" end_row="19" end_column="38" file_ref="0"/>
                <entry il_offset="0x65" start_row="20" start_column="13" end_row="20" end_column="19" file_ref="0"/>
                <entry il_offset="0x66" start_row="22" start_column="17" end_row="22" end_column="40" file_ref="0"/>
                <entry il_offset="0x6a" start_row="23" start_column="13" end_row="23" end_column="21" file_ref="0"/>
                <entry il_offset="0x6c" start_row="24" start_column="9" end_row="24" end_column="25" file_ref="0"/>
                <entry il_offset="0x75" start_row="26" start_column="5" end_row="26" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x76" attributes="0"/>
                <local name="newX" il_index="1" il_start="0x4" il_end="0x6c" attributes="0"/>
                <local name="s2" il_index="3" il_start="0x28" il_end="0x35" attributes="0"/>
                <local name="s3" il_index="4" il_start="0x43" il_end="0x52" attributes="0"/>
                <local name="e1" il_index="5" il_start="0x56" il_end="0x65" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact, WorkItem(651996, "DevDiv")>
        Public Sub TestAsync()
            Dim source =
<compilation>
    <file><![CDATA[
Option Strict Off
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Sub Main(args As String())
        Test().Wait()
    End Sub

    Async Function F(ParamArray a() As Integer) As Task(Of Integer)
        Await Task.Yield
        Return 0
    End Function

    Async Function Test() As Task
        Await F(Await F(
                    Await F(),
                    1,
                    Await F(12)),
                Await F(
                    Await F(Await F(Await F())),
                    Await F(12)))
    End Function

    Async Sub S()
        Await Task.Yield
    End Sub 
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestReferences, options:=TestOptions.DebugDll)

            ' kick-off method doesn't have any user code and hence doesn't need debug info
            AssertXmlEqual(
<symbols>
    <methods/>
</symbols>,
GetPdbXml(compilation, "Module1.F"))

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_0_F" name="MoveNext" parameterNames="">
            <sequencepoints total="9">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xf" start_row="11" start_column="5" end_row="11" end_column="68" file_ref="0"/>
                <entry il_offset="0x10" start_row="12" start_column="9" end_row="12" end_column="25" file_ref="0"/>
                <entry il_offset="0x7f" start_row="13" start_column="9" end_row="13" end_column="17" file_ref="0"/>
                <entry il_offset="0x83" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x8b" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xa8" start_row="14" start_column="5" end_row="14" end_column="17" file_ref="0"/>
                <entry il_offset="0xb2" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0xc0" attributes="0"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0xc0" attributes="0"/>
                <local name="VB$FRV" il_index="2" il_start="0xf" il_end="0x82" attributes="0"/>
                <local name="$ex" il_index="7" il_start="0x83" il_end="0xa7" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc0">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
                <local name="VB$returnTemp" il_index="0" il_start="0x0" il_end="0xc0" attributes="0"/>
                <local name="VB$cachedState" il_index="1" il_start="0x0" il_end="0xc0" attributes="0"/>
                <scope startOffset="0xf" endOffset="0x82">
                    <local name="VB$FRV" il_index="2" il_start="0xf" il_end="0x82" attributes="0"/>
                </scope>
                <scope startOffset="0x83" endOffset="0xa7">
                    <local name="$ex" il_index="7" il_start="0x83" il_end="0xa7" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="Module1" methodName="F" parameterNames="a"/>
                <await yield="0x36" resume="0x52" declaringType="Module1+VB$StateMachine_0_F" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>,
            GetPdbXml(compilation, "Module1+VB$StateMachine_0_F.MoveNext"))

            ' kick-off method doesn't have any user code and hence doesn't need debug info
            AssertXmlEqual(
<symbols>
    <methods/>
</symbols>,
GetPdbXml(compilation, "Module1.Test"))

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_1_Test" name="MoveNext" parameterNames="">
            <sequencepoints total="17">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x66" start_row="16" start_column="5" end_row="16" end_column="34" file_ref="0"/>
                <entry il_offset="0x67" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x68" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x69" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0xea" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x1f7" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x1f8" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x1f9" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x1fa" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x375" start_row="17" start_column="9" end_row="23" end_column="34" file_ref="0"/>
                <entry il_offset="0x4f5" start_row="24" start_column="5" end_row="24" end_column="17" file_ref="0"/>
                <entry il_offset="0x4f7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x4ff" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x51c" start_row="24" start_column="5" end_row="24" end_column="17" file_ref="0"/>
                <entry il_offset="0x526" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x533" attributes="0"/>
                <local name="$ex" il_index="13" il_start="0x4f7" il_end="0x51b" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x533">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x533" attributes="0"/>
                <scope startOffset="0x4f7" endOffset="0x51b">
                    <local name="$ex" il_index="13" il_start="0x4f7" il_end="0x51b" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="Module1" methodName="Test" parameterNames=""/>
                <await yield="0x92" resume="0xb2" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x118" resume="0x138" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x1a1" resume="0x1c0" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x223" resume="0x243" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x2a0" resume="0x2c0" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x31d" resume="0x33d" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x3a3" resume="0x3c3" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x428" resume="0x447" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
                <await yield="0x4ab" resume="0x4c7" declaringType="Module1+VB$StateMachine_1_Test" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>,
            GetPdbXml(compilation, "Module1+VB$StateMachine_1_Test.MoveNext"))

            ' kick-off method doesn't have any user code and hence doesn't need debug info
            AssertXmlEqual(
<symbols>
    <methods/>
</symbols>,
GetPdbXml(compilation, "Module1.S"))

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1+VB$StateMachine_2_S" name="MoveNext" parameterNames="">
            <sequencepoints total="9">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xf" start_row="26" start_column="5" end_row="26" end_column="18" file_ref="0"/>
                <entry il_offset="0x10" start_row="27" start_column="9" end_row="27" end_column="25" file_ref="0"/>
                <entry il_offset="0x7c" start_row="28" start_column="5" end_row="28" end_column="12" file_ref="0"/>
                <entry il_offset="0x7e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x86" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xa3" start_row="28" start_column="5" end_row="28" end_column="12" file_ref="0"/>
                <entry il_offset="0xad" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xba" attributes="0"/>
                <local name="$ex" il_index="5" il_start="0x7e" il_end="0xa2" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xba">
                <importsforward declaringType="Module1" methodName="Main" parameterNames="args"/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xba" attributes="0"/>
                <scope startOffset="0x7e" endOffset="0xa2">
                    <local name="$ex" il_index="5" il_start="0x7e" il_end="0xa2" attributes="0"/>
                </scope>
            </scope>
            <async-info catch-IL-offset="0x86">
                <kickoff-method declaringType="Module1" methodName="S" parameterNames=""/>
                <await yield="0x33" resume="0x4f" declaringType="Module1+VB$StateMachine_2_S" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation, "Module1+VB$StateMachine_2_S.MoveNext"))
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <sequencepoints total="6">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x3" start_row="8" start_column="9" end_row="8" end_column="11" file_ref="0"/>
                <entry il_offset="0x4" start_row="9" start_column="17" end_row="9" end_column="40" file_ref="0"/>
                <entry il_offset="0x8" start_row="10" start_column="13" end_row="10" end_column="21" file_ref="0"/>
                <entry il_offset="0xa" start_row="11" start_column="9" end_row="11" end_column="13" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0xd" attributes="0"/>
                <local name="newX" il_index="1" il_start="0x4" il_end="0xa" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
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

            ' By Design (better than Dev10): <entry il_offset="0x19" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
            Dim actual = GetPdbXml(compilation, "MyMod.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <sequencepoints total="8">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="38" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="9" end_row="6" end_column="37" file_ref="0"/>
                <entry il_offset="0x9" start_row="7" start_column="13" end_row="7" end_column="38" file_ref="0"/>
                <entry il_offset="0x14" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x17" start_row="8" start_column="9" end_row="8" end_column="13" file_ref="0"/>
                <entry il_offset="0x18" start_row="9" start_column="13" end_row="9" end_column="38" file_ref="0"/>
                <entry il_offset="0x23" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x24" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x25">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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


            Dim actual = GetPdbXml(compilation, "MyMod.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="22" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="9" end_row="6" end_column="31" file_ref="0"/>
                <entry il_offset="0xc" start_row="8" start_column="9" end_row="8" end_column="28" file_ref="0"/>
                <entry il_offset="0x14" start_row="9" start_column="13" end_row="9" end_column="35" file_ref="0"/>
                <entry il_offset="0x1f" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x20" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x21" start_row="12" start_column="9" end_row="12" end_column="29" file_ref="0"/>
                <entry il_offset="0x29" start_row="13" start_column="13" end_row="13" end_column="36" file_ref="0"/>
                <entry il_offset="0x34" start_row="14" start_column="9" end_row="14" end_column="15" file_ref="0"/>
                <entry il_offset="0x35" start_row="14" start_column="9" end_row="14" end_column="15" file_ref="0"/>
                <entry il_offset="0x36" start_row="16" start_column="9" end_row="16" end_column="31" file_ref="0"/>
                <entry il_offset="0x41" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x42">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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


            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="19">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="8" start_column="9" end_row="8" end_column="24" file_ref="0"/>
                <entry il_offset="0x9" start_row="9" start_column="17" end_row="9" end_column="38" file_ref="0"/>
                <entry il_offset="0xf" start_row="10" start_column="13" end_row="10" end_column="20" file_ref="0"/>
                <entry il_offset="0x16" start_row="11" start_column="9" end_row="11" end_column="15" file_ref="0"/>
                <entry il_offset="0x17" start_row="11" start_column="9" end_row="11" end_column="15" file_ref="0"/>
                <entry il_offset="0x18" start_row="14" start_column="9" end_row="14" end_column="24" file_ref="0"/>
                <entry il_offset="0x20" start_row="14" start_column="25" end_row="14" end_column="38" file_ref="0"/>
                <entry il_offset="0x2b" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="9" end_row="16" end_column="12" file_ref="0"/>
                <entry il_offset="0x2f" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x36" start_row="17" start_column="9" end_row="17" end_column="30" file_ref="0"/>
                <entry il_offset="0x3e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x40" start_row="18" start_column="9" end_row="18" end_column="16" file_ref="0"/>
                <entry il_offset="0x41" start_row="20" start_column="13" end_row="20" end_column="28" file_ref="0"/>
                <entry il_offset="0x49" start_row="20" start_column="29" end_row="20" end_column="42" file_ref="0"/>
                <entry il_offset="0x54" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x55" start_row="21" start_column="9" end_row="21" end_column="16" file_ref="0"/>
                <entry il_offset="0x56" start_row="23" start_column="5" end_row="23" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="1" il_start="0x9" il_end="0x16" attributes="0"/>
                <local name="ex" il_index="2" il_start="0x2f" il_end="0x3d" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "MyMod.Main")
            ' start_row="33"
            Dim expected =
<symbols>
    <entryPoint declaringType="MyMod" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="MyMod" name="Main" parameterNames="args">
            <sequencepoints total="32">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="8" start_column="9" end_row="8" end_column="15" file_ref="0"/>
                <entry il_offset="0x5" start_row="9" start_column="9" end_row="9" end_column="15" file_ref="0"/>
                <entry il_offset="0x9" start_row="10" start_column="9" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0xd" start_row="11" start_column="9" end_row="11" end_column="14" file_ref="0"/>
                <entry il_offset="0xf" start_row="12" start_column="9" end_row="12" end_column="14" file_ref="0"/>
                <entry il_offset="0x12" start_row="13" start_column="9" end_row="13" end_column="14" file_ref="0"/>
                <entry il_offset="0x15" start_row="14" start_column="13" end_row="14" end_column="32" file_ref="0"/>
                <entry il_offset="0x19" start_row="15" start_column="9" end_row="15" end_column="11" file_ref="0"/>
                <entry il_offset="0x1a" start_row="16" start_column="13" end_row="16" end_column="44" file_ref="0"/>
                <entry il_offset="0x2b" start_row="17" start_column="13" end_row="17" end_column="22" file_ref="0"/>
                <entry il_offset="0x43" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x48" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x4d" start_row="20" start_column="21" end_row="20" end_column="30" file_ref="0"/>
                <entry il_offset="0x54" start_row="21" start_column="21" end_row="21" end_column="49" file_ref="0"/>
                <entry il_offset="0x66" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x68" start_row="23" start_column="25" end_row="23" end_column="53" file_ref="0"/>
                <entry il_offset="0x79" start_row="24" start_column="25" end_row="24" end_column="27" file_ref="0"/>
                <entry il_offset="0x7a" start_row="25" start_column="29" end_row="25" end_column="57" file_ref="0"/>
                <entry il_offset="0x8c" start_row="26" start_column="29" end_row="26" end_column="38" file_ref="0"/>
                <entry il_offset="0x93" start_row="27" start_column="25" end_row="27" end_column="47" file_ref="0"/>
                <entry il_offset="0xaf" start_row="28" start_column="25" end_row="28" end_column="34" file_ref="0"/>
                <entry il_offset="0xc7" start_row="29" start_column="21" end_row="29" end_column="25" file_ref="0"/>
                <entry il_offset="0xc8" start_row="22" start_column="21" end_row="22" end_column="41" file_ref="0"/>
                <entry il_offset="0xe0" start_row="30" start_column="17" end_row="30" end_column="21" file_ref="0"/>
                <entry il_offset="0xe1" start_row="19" start_column="17" end_row="19" end_column="46" file_ref="0"/>
                <entry il_offset="0xf8" start_row="31" start_column="17" end_row="31" end_column="26" file_ref="0"/>
                <entry il_offset="0x110" start_row="32" start_column="17" end_row="32" end_column="46" file_ref="0"/>
                <entry il_offset="0x121" start_row="33" start_column="13" end_row="33" end_column="22" file_ref="0"/>
                <entry il_offset="0x122" start_row="18" start_column="13" end_row="18" end_column="26" file_ref="0"/>
                <entry il_offset="0x13f" start_row="34" start_column="9" end_row="34" end_column="28" file_ref="0"/>
                <entry il_offset="0x15f" start_row="35" start_column="5" end_row="35" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="y" il_index="1" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="z" il_index="2" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="a" il_index="3" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="b" il_index="4" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="c" il_index="5" il_start="0x0" il_end="0x160" attributes="0"/>
                <local name="ct" il_index="6" il_start="0x0" il_end="0x160" attributes="0"/>
            </locals>
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
</symbols>
            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "M1+C1`1+_Closure$__2`2._Lambda$__3")

            Dim expected =
<symbols>
    <entryPoint declaringType="M1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="M1+C1`1+_Closure$__2`2" name="_Lambda$__3" parameterNames="lifted, notLifted">
            <sequencepoints total="7">
                <entry il_offset="0x0" start_row="16" start_column="17" end_row="16" end_column="61" file_ref="0"/>
                <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x15" start_row="17" start_column="25" end_row="17" end_column="60" file_ref="0"/>
                <entry il_offset="0x1e" start_row="18" start_column="21" end_row="18" end_column="43" file_ref="0"/>
                <entry il_offset="0x25" start_row="20" start_column="25" end_row="24" end_column="32" file_ref="0"/>
                <entry il_offset="0x32" start_row="26" start_column="21" end_row="26" end_column="33" file_ref="0"/>
                <entry il_offset="0x3f" start_row="27" start_column="17" end_row="27" end_column="24" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="iii" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="d2" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x40">
                <importsforward declaringType="M1" methodName="Main" parameterNames=""/>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="iii" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="d2" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="20" file_ref="0"/>
                <entry il_offset="0x7" start_row="8" start_column="13" end_row="8" end_column="34" file_ref="0"/>
                <entry il_offset="0xd" start_row="9" start_column="9" end_row="9" end_column="15" file_ref="0"/>
                <entry il_offset="0xf" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x11" start_row="11" start_column="13" end_row="11" end_column="48" file_ref="0"/>
                <entry il_offset="0x23" start_row="12" start_column="13" end_row="12" end_column="33" file_ref="0"/>
                <entry il_offset="0x2a" start_row="13" start_column="13" end_row="13" end_column="26" file_ref="0"/>
                <entry il_offset="0x30" start_row="14" start_column="13" end_row="14" end_column="23" file_ref="0"/>
                <entry il_offset="0x34" start_row="15" start_column="9" end_row="15" end_column="18" file_ref="0"/>
                <entry il_offset="0x35" start_row="10" start_column="9" end_row="10" end_column="20" file_ref="0"/>
                <entry il_offset="0x3f" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="i" il_index="1" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x40" attributes="0"/>
                <local name="y" il_index="3" il_start="0x0" il_end="0x40" attributes="0"/>
            </locals>
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
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="args">
            <sequencepoints total="7">
                <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="5" start_column="13" end_row="6" end_column="74" file_ref="0"/>
                <entry il_offset="0x22" start_row="8" start_column="13" end_row="8" end_column="45" file_ref="0"/>
                <entry il_offset="0x2d" start_row="10" start_column="9" end_row="10" end_column="41" file_ref="0"/>
                <entry il_offset="0x35" start_row="11" start_column="9" end_row="11" end_column="44" file_ref="0"/>
                <entry il_offset="0x3d" start_row="13" start_column="9" end_row="13" end_column="28" file_ref="0"/>
                <entry il_offset="0x44" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="del" il_index="0" il_start="0x0" il_end="0x45" attributes="0"/>
                <local name="v" il_index="1" il_start="0x0" il_end="0x45" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x45">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="del" il_index="0" il_start="0x0" il_end="0x45" attributes="0"/>
                <local name="v" il_index="1" il_start="0x0" il_end="0x45" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="5">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x4" start_row="6" start_column="9" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x5" start_row="7" start_column="5" end_row="7" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x6">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="9">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0xa" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0xd" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="9" start_column="9" end_row="9" end_column="24" file_ref="0"/>
                <entry il_offset="0x11" start_row="10" start_column="13" end_row="10" end_column="22" file_ref="0"/>
                <entry il_offset="0x14" start_row="11" start_column="9" end_row="11" end_column="19" file_ref="0"/>
                <entry il_offset="0x15" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x16" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x16">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x16" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="9">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x6" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x13" start_row="7" start_column="13" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0x20" start_row="8" start_column="13" end_row="8" end_column="27" file_ref="0"/>
                <entry il_offset="0x37" start_row="9" start_column="13" end_row="9" end_column="22" file_ref="0"/>
                <entry il_offset="0x38" start_row="10" start_column="9" end_row="10" end_column="19" file_ref="0"/>
                <entry il_offset="0x39" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x3a" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x3a">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x3a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x30" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x31" start_row="7" start_column="17" end_row="7" end_column="39" file_ref="0"/>
                <entry il_offset="0x3e" start_row="8" start_column="13" end_row="8" end_column="19" file_ref="0"/>
                <entry il_offset="0x3f" start_row="9" start_column="17" end_row="9" end_column="39" file_ref="0"/>
                <entry il_offset="0x4c" start_row="10" start_column="13" end_row="10" end_column="42" file_ref="0"/>
                <entry il_offset="0x4f" start_row="11" start_column="13" end_row="11" end_column="22" file_ref="0"/>
                <entry il_offset="0x50" start_row="12" start_column="17" end_row="12" end_column="42" file_ref="0"/>
                <entry il_offset="0x5d" start_row="13" start_column="9" end_row="13" end_column="19" file_ref="0"/>
                <entry il_offset="0x5e" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5f">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x5f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="13">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="28" file_ref="0"/>
                <entry il_offset="0x34" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x35" start_row="7" start_column="17" end_row="7" end_column="38" file_ref="0"/>
                <entry il_offset="0x42" start_row="8" start_column="13" end_row="8" end_column="19" file_ref="0"/>
                <entry il_offset="0x43" start_row="9" start_column="17" end_row="9" end_column="39" file_ref="0"/>
                <entry il_offset="0x50" start_row="10" start_column="13" end_row="10" end_column="42" file_ref="0"/>
                <entry il_offset="0x51" start_row="11" start_column="17" end_row="11" end_column="39" file_ref="0"/>
                <entry il_offset="0x5e" start_row="12" start_column="13" end_row="12" end_column="22" file_ref="0"/>
                <entry il_offset="0x5f" start_row="13" start_column="17" end_row="13" end_column="42" file_ref="0"/>
                <entry il_offset="0x6c" start_row="14" start_column="9" end_row="14" end_column="19" file_ref="0"/>
                <entry il_offset="0x6d" start_row="15" start_column="5" end_row="15" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x6e">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x6e" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="11">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x6" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x11" start_row="7" start_column="17" end_row="7" end_column="39" file_ref="0"/>
                <entry il_offset="0x1e" start_row="8" start_column="13" end_row="8" end_column="19" file_ref="0"/>
                <entry il_offset="0x29" start_row="9" start_column="17" end_row="9" end_column="39" file_ref="0"/>
                <entry il_offset="0x36" start_row="10" start_column="13" end_row="10" end_column="31" file_ref="0"/>
                <entry il_offset="0x4a" start_row="12" start_column="17" end_row="12" end_column="42" file_ref="0"/>
                <entry il_offset="0x55" start_row="13" start_column="9" end_row="13" end_column="19" file_ref="0"/>
                <entry il_offset="0x56" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x57">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x3" start_row="5" start_column="9" end_row="5" end_column="28" file_ref="0"/>
                <entry il_offset="0x8" start_row="6" start_column="13" end_row="6" end_column="19" file_ref="0"/>
                <entry il_offset="0x13" start_row="7" start_column="17" end_row="7" end_column="38" file_ref="0"/>
                <entry il_offset="0x20" start_row="8" start_column="13" end_row="8" end_column="19" file_ref="0"/>
                <entry il_offset="0x2b" start_row="9" start_column="17" end_row="9" end_column="39" file_ref="0"/>
                <entry il_offset="0x38" start_row="10" start_column="13" end_row="10" end_column="31" file_ref="0"/>
                <entry il_offset="0x4a" start_row="11" start_column="17" end_row="11" end_column="39" file_ref="0"/>
                <entry il_offset="0x57" start_row="13" start_column="17" end_row="13" end_column="42" file_ref="0"/>
                <entry il_offset="0x62" start_row="14" start_column="9" end_row="14" end_column="19" file_ref="0"/>
                <entry il_offset="0x63" start_row="15" start_column="5" end_row="15" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="num" il_index="0" il_start="0x0" il_end="0x64" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x64">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="num" il_index="0" il_start="0x0" il_end="0x64" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="33" file_ref="0"/>
                <entry il_offset="0x7" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x135" start_row="6" start_column="13" end_row="6" end_column="22" file_ref="0"/>
                <entry il_offset="0x136" start_row="7" start_column="17" end_row="7" end_column="40" file_ref="0"/>
                <entry il_offset="0x143" start_row="8" start_column="13" end_row="8" end_column="22" file_ref="0"/>
                <entry il_offset="0x144" start_row="9" start_column="17" end_row="9" end_column="40" file_ref="0"/>
                <entry il_offset="0x151" start_row="10" start_column="13" end_row="10" end_column="58" file_ref="0"/>
                <entry il_offset="0x154" start_row="11" start_column="13" end_row="11" end_column="22" file_ref="0"/>
                <entry il_offset="0x155" start_row="12" start_column="17" end_row="12" end_column="42" file_ref="0"/>
                <entry il_offset="0x162" start_row="13" start_column="9" end_row="13" end_column="19" file_ref="0"/>
                <entry il_offset="0x163" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="str" il_index="0" il_start="0x0" il_end="0x164" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x164">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x164" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="11">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="33" file_ref="0"/>
                <entry il_offset="0x7" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0x34" start_row="6" start_column="13" end_row="6" end_column="22" file_ref="0"/>
                <entry il_offset="0x35" start_row="7" start_column="17" end_row="7" end_column="40" file_ref="0"/>
                <entry il_offset="0x42" start_row="8" start_column="13" end_row="8" end_column="22" file_ref="0"/>
                <entry il_offset="0x45" start_row="9" start_column="13" end_row="9" end_column="22" file_ref="0"/>
                <entry il_offset="0x46" start_row="10" start_column="17" end_row="10" end_column="40" file_ref="0"/>
                <entry il_offset="0x53" start_row="11" start_column="13" end_row="11" end_column="22" file_ref="0"/>
                <entry il_offset="0x56" start_row="12" start_column="9" end_row="12" end_column="19" file_ref="0"/>
                <entry il_offset="0x57" start_row="13" start_column="5" end_row="13" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="str" il_index="0" il_start="0x0" il_end="0x58" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x58">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x58" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="10">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="13" end_row="4" end_column="33" file_ref="0"/>
                <entry il_offset="0x7" start_row="5" start_column="9" end_row="5" end_column="24" file_ref="0"/>
                <entry il_offset="0xa" start_row="6" start_column="13" end_row="6" end_column="22" file_ref="0"/>
                <entry il_offset="0x1d" start_row="7" start_column="17" end_row="7" end_column="40" file_ref="0"/>
                <entry il_offset="0x2a" start_row="8" start_column="13" end_row="8" end_column="36" file_ref="0"/>
                <entry il_offset="0x54" start_row="9" start_column="13" end_row="9" end_column="22" file_ref="0"/>
                <entry il_offset="0x67" start_row="10" start_column="17" end_row="10" end_column="40" file_ref="0"/>
                <entry il_offset="0x72" start_row="11" start_column="9" end_row="11" end_column="19" file_ref="0"/>
                <entry il_offset="0x73" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="str" il_index="0" il_start="0x0" il_end="0x74" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x74">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x74" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="C1" name="Method" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="17" file_ref="0"/>
                <entry il_offset="0x1" start_row="3" start_column="13" end_row="3" end_column="36" file_ref="0"/>
                <entry il_offset="0x8" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="C1" name="Method" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="17" file_ref="0"/>
                <entry il_offset="0x1" start_row="3" start_column="13" end_row="3" end_column="40" file_ref="0"/>
                <entry il_offset="0x8" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
                <local name="o" il_index="0" il_start="0x0" il_end="0x9" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "FooDerived.ComputeMatrix")

            Dim expected =
<symbols>
    <entryPoint declaringType="Variety" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="FooDerived" name="ComputeMatrix" parameterNames="rank">
            <sequencepoints total="12">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="52" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="15" end_row="14" end_column="22" file_ref="0"/>
                <entry il_offset="0xa" start_row="15" start_column="15" end_row="15" end_column="25" file_ref="0"/>
                <entry il_offset="0x14" start_row="18" start_column="9" end_row="18" end_column="18" file_ref="0"/>
                <entry il_offset="0x17" start_row="19" start_column="9" end_row="19" end_column="30" file_ref="0"/>
                <entry il_offset="0x1e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x20" start_row="20" start_column="13" end_row="20" end_column="21" file_ref="0"/>
                <entry il_offset="0x25" start_row="21" start_column="13" end_row="21" end_column="34" file_ref="0"/>
                <entry il_offset="0x3f" start_row="22" start_column="13" end_row="22" end_column="29" file_ref="0"/>
                <entry il_offset="0x46" start_row="23" start_column="9" end_row="23" end_column="15" file_ref="0"/>
                <entry il_offset="0x4a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x58" start_row="24" start_column="5" end_row="24" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="I" il_index="0" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="J" il_index="1" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="count" il_index="3" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="dims" il_index="4" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="VB$LoopObject" il_index="5" il_start="0x17" il_end="0x57" attributes="1"/>
                <local name="VB$ForLimit" il_index="6" il_start="0x17" il_end="0x57" attributes="1"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x59">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="I" il_index="0" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="J" il_index="1" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="q" il_index="2" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="count" il_index="3" il_start="0x0" il_end="0x59" attributes="0"/>
                <local name="dims" il_index="4" il_start="0x0" il_end="0x59" attributes="0"/>
                <scope startOffset="0x17" endOffset="0x57">
                    <local name="VB$LoopObject" il_index="5" il_start="0x17" il_end="0x57" attributes="1"/>
                    <local name="VB$ForLimit" il_index="6" il_start="0x17" il_end="0x57" attributes="1"/>
                </scope>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "SubMod.Main")

            Dim expected =
<symbols>
    <entryPoint declaringType="SubMod" methodName="Main" parameterNames=""/>
    <methods>
        <method containingType="SubMod" name="Main" parameterNames="">
            <sequencepoints total="8">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="1" end_row="4" end_column="4" file_ref="0"/>
                <entry il_offset="0x2" start_row="5" start_column="9" end_row="5" end_column="16" file_ref="0"/>
                <entry il_offset="0x4" start_row="6" start_column="1" end_row="6" end_column="4" file_ref="0"/>
                <entry il_offset="0x5" start_row="7" start_column="9" end_row="7" end_column="17" file_ref="0"/>
                <entry il_offset="0x7" start_row="8" start_column="1" end_row="8" end_column="4" file_ref="0"/>
                <entry il_offset="0x8" start_row="9" start_column="9" end_row="9" end_column="16" file_ref="0"/>
                <entry il_offset="0xa" start_row="10" start_column="5" end_row="10" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xb">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>


            AssertXmlEqual(expected, actual)
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
            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="M1" name="Main" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="3" start_column="5" end_row="3" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="9" end_row="4" end_column="12" file_ref="0"/>
                <entry il_offset="0x7" start_row="5" start_column="5" end_row="5" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x8">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="M1" name="S" parameterNames="">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="19" file_ref="0"/>
                <entry il_offset="0x1" start_row="11" start_column="5" end_row="11" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="M1" methodName="Main" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>


            AssertXmlEqual(expected, actual)
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
            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="17" start_column="5" end_row="17" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="18" start_column="13" end_row="18" end_column="30" file_ref="0"/>
                <entry il_offset="0x4" start_row="19" start_column="13" end_row="19" end_column="25" file_ref="0"/>
                <entry il_offset="0xb" start_row="20" start_column="5" end_row="20" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                <local name="b2" il_index="1" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                <local name="b2" il_index="1" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name="op_Implicit" parameterNames="x">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="12" start_column="9" end_row="12" end_column="59" file_ref="0"/>
                <entry il_offset="0x1" start_row="13" start_column="13" end_row="13" end_column="29" file_ref="0"/>
                <entry il_offset="0xa" start_row="14" start_column="9" end_row="14" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1+B2" name=".ctor" parameterNames="x">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="8" start_column="9" end_row="8" end_column="37" file_ref="0"/>
                <entry il_offset="0x7" start_row="9" start_column="13" end_row="9" end_column="18" file_ref="0"/>
                <entry il_offset="0xe" start_row="10" start_column="9" end_row="10" end_column="16" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>


            AssertXmlEqual(expected, actual)
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
            AssertXmlEqual(actual1, actual2)
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
            Dim actual As XElement = GetPdbXml(compilation, "Module1._Lambda$__1")

            Dim expected =
                <symbols>
                    <entryPoint declaringType="Module1" methodName="Main" parameterNames=""/>
                    <methods>
                        <method containingType="Module1" name="_Lambda$__1" parameterNames="">
                            <sequencepoints total="4">
                                <entry il_offset="0x0" start_row="5" start_column="17" end_row="5" end_column="27" file_ref="0"/>
                                <entry il_offset="0x1" start_row="6" start_column="25" end_row="6" end_column="31" file_ref="0"/>
                                <entry il_offset="0x4" start_row="7" start_column="21" end_row="7" end_column="29" file_ref="0"/>
                                <entry il_offset="0x8" start_row="8" start_column="12" end_row="8" end_column="24" file_ref="0"/>
                            </sequencepoints>
                            <locals>
                                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0xa" attributes="0"/>
                                <local name="r" il_index="1" il_start="0x0" il_end="0xa" attributes="0"/>
                            </locals>
                            <scope startOffset="0x0" endOffset="0xa">
                                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
                                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0xa" attributes="0"/>
                                <local name="r" il_index="1" il_start="0x0" il_end="0xa" attributes="0"/>
                            </scope>
                        </method>
                    </methods>
                </symbols>

            AssertXmlEqual(expected, actual)
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

            Dim actual = GetPdbXml(compilation, "CLAZZ..ctor")

            Dim expected =
<symbols>
    <methods>
        <method containingType="CLAZZ" name=".ctor" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="8" start_column="5" end_row="8" end_column="21" file_ref="0"/>
                <entry il_offset="0x1a" start_row="4" start_column="12" end_row="4" end_column="31" file_ref="0"/>
                <entry il_offset="0x21" start_row="6" start_column="12" end_row="6" end_column="31" file_ref="0"/>
                <entry il_offset="0x28" start_row="10" start_column="5" end_row="10" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x29">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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
            Dim result = compilation.Emit(exebits, Nothing, "DontCare", pdbbits, Nothing)
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
            Dim actual As XElement = GetPdbXml(compilation)

            ' Dev11 generates debug info for embedded symbols. There is no reason to do so since the source code is not available to the user.

            Dim expected =
               <symbols>
                   <methods>
                       <method containingType="C" name="F" parameterNames="z">
                           <sequencepoints total="3">
                               <entry il_offset="0x0" start_row="4" start_column="5" end_row="4" end_column="51" file_ref="0"/>
                               <entry il_offset="0x1" start_row="5" start_column="9" end_row="5" end_column="23" file_ref="0"/>
                               <entry il_offset="0xa" start_row="6" start_column="5" end_row="6" end_column="17" file_ref="0"/>
                           </sequencepoints>
                           <locals>
                               <local name="F" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                           </locals>
                           <scope startOffset="0x0" endOffset="0xc">
                               <type name="Microsoft.VisualBasic.Strings" importlevel="file"/>
                               <currentnamespace name=""/>
                               <local name="F" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
                           </scope>
                       </method>
                   </methods>
               </symbols>

            AssertXmlEqual(expected, actual)

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

            Dim actual = GetPdbXml(compilation, "Module1.MakeIncrementer")

            Dim expected =
<symbols>
    <methods>
        <method containingType="Module1" name="MakeIncrementer" parameterNames="n">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="7" start_column="5" end_row="7" end_column="72" file_ref="0"/>
                <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="9" end_row="10" end_column="21" file_ref="0"/>
                <entry il_offset="0x1d" start_row="11" start_column="5" end_row="11" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="MakeIncrementer" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Main" parameterNames=""/>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="MakeIncrementer" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
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
            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="C" name=".ctor" parameterNames="">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x6" start_row="2" start_column="13" end_row="2" end_column="39" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x17">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation, "C..ctor"))
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
            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="M" name=".cctor" parameterNames="">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="3" start_column="13" end_row="7" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x12">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="M" name="_Lambda$__2" parameterNames="x">
            <sequencepoints total="5">
                <entry il_offset="0x0" start_row="3" start_column="46" end_row="3" end_column="57" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="17" end_row="4" end_column="62" file_ref="0"/>
                <entry il_offset="0x22" start_row="5" start_column="17" end_row="5" end_column="112" file_ref="0"/>
                <entry il_offset="0x43" start_row="6" start_column="13" end_row="6" end_column="33" file_ref="0"/>
                <entry il_offset="0x53" start_row="7" start_column="9" end_row="7" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x55" attributes="0"/>
                <local name="f" il_index="1" il_start="0x0" il_end="0x55" attributes="0"/>
                <local name="g" il_index="2" il_start="0x0" il_end="0x55" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x55">
                <importsforward declaringType="M" methodName=".cctor" parameterNames=""/>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x55" attributes="0"/>
                <local name="f" il_index="1" il_start="0x0" il_end="0x55" attributes="0"/>
                <local name="g" il_index="2" il_start="0x0" il_end="0x55" attributes="0"/>
            </scope>
        </method>
        <method containingType="M" name="_Lambda$__3" parameterNames="o">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="4" start_column="49" end_row="4" end_column="60" file_ref="0"/>
                <entry il_offset="0x1" start_row="4" start_column="61" end_row="4" end_column="62" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x7" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="M" methodName=".cctor" parameterNames=""/>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x7" attributes="0"/>
            </scope>
        </method>
        <method containingType="M" name="_Lambda$__5" parameterNames="h">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="5" start_column="84" end_row="5" end_column="95" file_ref="0"/>
                <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xe" start_row="5" start_column="96" end_row="5" end_column="112" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="VB$FRV" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="M" methodName=".cctor" parameterNames=""/>
                <local name="$VB$Closure_1" il_index="0" il_start="0x0" il_end="0x1f" attributes="0"/>
                <local name="VB$FRV" il_index="1" il_start="0x0" il_end="0x1f" attributes="0"/>
            </scope>
        </method>
        <method containingType="M+_Closure$__1" name="_Lambda$__6" parameterNames="y">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="5" start_column="96" end_row="5" end_column="107" file_ref="0"/>
                <entry il_offset="0x1" start_row="5" start_column="108" end_row="5" end_column="112" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x17" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x17">
                <importsforward declaringType="M" methodName=".cctor" parameterNames=""/>
                <local name="VB$FRV" il_index="0" il_start="0x0" il_end="0x17" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            compilation.AssertTheseDiagnostics(<expected></expected>)

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="5" start_column="5" end_row="5" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="6" start_column="9" end_row="6" end_column="25" file_ref="0"/>
                <entry il_offset="0x15" start_row="7" start_column="5" end_row="7" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x17" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x17">
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x17" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="6">
                <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="12" start_column="13" end_row="15" end_column="64" file_ref="0"/>
                <entry il_offset="0x91" start_row="17" start_column="9" end_row="17" end_column="20" file_ref="0"/>
                <entry il_offset="0x98" start_row="19" start_column="13" end_row="20" end_column="38" file_ref="0"/>
                <entry il_offset="0xe8" start_row="22" start_column="9" end_row="22" end_column="21" file_ref="0"/>
                <entry il_offset="0xef" start_row="23" start_column="5" end_row="23" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0xf0" attributes="0"/>
                <local name="qq" il_index="1" il_start="0x0" il_end="0xf0" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xf0">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xf0" attributes="0"/>
                <local name="qq" il_index="1" il_start="0x0" il_end="0xf0" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="x">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="13" start_column="26" end_row="13" end_column="27" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="x">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="60" end_row="14" end_column="67" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="x">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="14" start_column="27" end_row="14" end_column="33" file_ref="0"/>
                <entry il_offset="0x4" start_row="14" start_column="39" end_row="14" end_column="46" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xe">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="evenOdd, $VB$ItAnonymous">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="30" end_row="15" end_column="44" file_ref="0"/>
                <entry il_offset="0x27" start_row="15" start_column="50" end_row="15" end_column="64" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x53">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__8" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="34" end_row="15" end_column="43" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__10" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="54" end_row="15" end_column="63" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__13" parameterNames="x">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="19" start_column="25" end_row="19" end_column="32" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__15" parameterNames="x">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="20" start_column="26" end_row="20" end_column="27" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            compilation.AssertTheseDiagnostics(<expected></expected>)

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="14" end_column="37" file_ref="0"/>
                <entry il_offset="0x2c" start_row="15" start_column="9" end_row="15" end_column="36" file_ref="0"/>
                <entry il_offset="0x38" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            compilation.AssertTheseDiagnostics(<expected></expected>)

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="14" end_column="74" file_ref="0"/>
                <entry il_offset="0x71" start_row="15" start_column="9" end_row="15" end_column="35" file_ref="0"/>
                <entry il_offset="0x7d" start_row="16" start_column="5" end_row="16" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="28" end_row="14" end_column="35" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="68" end_row="14" end_column="74" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2b">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="57" end_row="14" end_column="64" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="42" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x38" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="29" end_row="15" end_column="42" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="85" file_ref="0"/>
                <entry il_offset="0x51" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x5d" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="37" end_row="15" end_column="50" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xb">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$It">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xc" start_row="15" start_column="64" end_row="15" end_column="85" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x20">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            compilation.AssertTheseDiagnostics(<expected></expected>)

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="45" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="9" end_row="16" end_column="36" file_ref="0"/>
                <entry il_offset="0x38" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="32" end_row="15" end_column="45" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="33" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="9" end_row="16" end_column="36" file_ref="0"/>
                <entry il_offset="0x38" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="20" end_row="15" end_column="33" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x4">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="72" file_ref="0"/>
                <entry il_offset="0x2c" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x38" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="15" start_column="32" end_row="15" end_column="45" file_ref="0"/>
                <entry il_offset="0x3" start_row="15" start_column="59" end_row="15" end_column="72" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x15">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="67" file_ref="0"/>
                <entry il_offset="0x96" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0xa2" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0xa3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xa3" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xa3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xa3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xa3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="53" end_row="14" end_column="60" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="41" end_row="15" end_column="50" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="58" end_row="15" end_column="67" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="17" end_column="106" file_ref="0"/>
                <entry il_offset="0xdb" start_row="18" start_column="9" end_row="18" end_column="35" file_ref="0"/>
                <entry il_offset="0xe7" start_row="19" start_column="5" end_row="19" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0xe8" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xe8" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xe8">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xe8" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xe8" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="63" end_row="16" end_column="72" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="46" end_row="16" end_column="55" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="rangeVar1">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="17" start_column="41" end_row="17" end_column="50" file_ref="0"/>
                <entry il_offset="0x1" start_row="17" start_column="93" end_row="17" end_column="106" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__9" parameterNames="$VB$It">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="17" start_column="58" end_row="17" end_column="67" file_ref="0"/>
                <entry il_offset="0x6" start_row="17" start_column="72" end_row="17" end_column="85" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x14">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="48" file_ref="0"/>
                <entry il_offset="0x96" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0xa2" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0xa3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xa3" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xa3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0xa3" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0xa3" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="59" end_row="14" end_column="66" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="41" end_row="15" end_column="50" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="58" end_row="15" end_column="67" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="19" end_column="57" file_ref="0"/>
                <entry il_offset="0x125" start_row="20" start_column="9" end_row="20" end_column="35" file_ref="0"/>
                <entry il_offset="0x131" start_row="21" start_column="5" end_row="21" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x132" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x132" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x132">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x132" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x132" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="59" end_row="14" end_column="66" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="63" end_row="15" end_column="70" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="69" end_row="16" end_column="78" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="52" end_row="16" end_column="61" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__9" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="17" start_column="47" end_row="17" end_column="61" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2d">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__10" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="17" start_column="51" end_row="17" end_column="60" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__13" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="18" start_column="41" end_row="18" end_column="50" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__15" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="18" start_column="58" end_row="18" end_column="67" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__17" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="19" start_column="43" end_row="19" end_column="57" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2d">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__18" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="19" start_column="47" end_row="19" end_column="56" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="79" file_ref="0"/>
                <entry il_offset="0x71" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x7d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="15" start_column="41" end_row="15" end_column="50" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="93" end_row="15" end_column="106" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar2">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="15" start_column="58" end_row="15" end_column="67" file_ref="0"/>
                <entry il_offset="0x1" start_row="15" start_column="72" end_row="15" end_column="85" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xa">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="rangeVar1, $VB$ItAnonymous">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x2" start_row="16" start_column="56" end_row="16" end_column="70" file_ref="0"/>
                <entry il_offset="0x28" start_row="16" start_column="72" end_row="16" end_column="79" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x34">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="60" end_row="16" end_column="69" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="73" file_ref="0"/>
                <entry il_offset="0x71" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x7d" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="19" end_row="15" end_column="73" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="78" file_ref="0"/>
                <entry il_offset="0x71" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x7d" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="24" end_row="15" end_column="78" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="78" file_ref="0"/>
                <entry il_offset="0x71" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x7d" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x7e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x7e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="24" end_row="15" end_column="78" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x22">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="19" file_ref="0"/>
                <entry il_offset="0xd" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x19" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1a">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="15" end_column="19" file_ref="0"/>
                <entry il_offset="0xd" start_row="16" start_column="9" end_row="16" end_column="35" file_ref="0"/>
                <entry il_offset="0x19" start_row="17" start_column="5" end_row="17" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1a">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x1a" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x1a" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="23" file_ref="0"/>
                <entry il_offset="0x91" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x9d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x9e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="22" end_row="15" end_column="31" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="32" file_ref="0"/>
                <entry il_offset="0x91" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x9d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x9e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="34" end_row="15" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="rangeVar2, $VB$ItAnonymous">
            <sequencepoints total="2">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1" start_row="16" start_column="18" end_row="16" end_column="32" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2d">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__8" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="22" end_row="16" end_column="31" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="54" file_ref="0"/>
                <entry il_offset="0x91" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x9d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x9e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x9e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x9e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="14" start_column="52" end_row="14" end_column="58" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$It">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="15" start_column="34" end_row="15" end_column="47" file_ref="0"/>
                <entry il_offset="0x8" start_row="15" start_column="61" end_row="15" end_column="74" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x1f">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="$VB$It, $VB$ItAnonymous">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xd" start_row="16" start_column="31" end_row="16" end_column="45" file_ref="0"/>
                <entry il_offset="0x33" start_row="16" start_column="47" end_row="16" end_column="54" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3f">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__8" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="35" end_row="16" end_column="44" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="36" file_ref="0"/>
                <entry il_offset="0x2c" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x38" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="13" end_row="16" end_column="36" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x56">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__2" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="33" end_row="15" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="22" end_row="16" end_column="35" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="50" file_ref="0"/>
                <entry il_offset="0x2c" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x38" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x39">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x39" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x39" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="13" end_row="16" end_column="50" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9b">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__2" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="33" end_row="15" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="65" end_row="15" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__8" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="28" end_row="16" end_column="49" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="17" end_column="36" file_ref="0"/>
                <entry il_offset="0x51" start_row="18" start_column="9" end_row="18" end_column="35" file_ref="0"/>
                <entry il_offset="0x5d" start_row="19" start_column="5" end_row="19" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="20" end_row="15" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$ItAnonymous">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="13" end_row="17" end_column="36" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x50">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="33" end_row="16" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="17" start_column="22" end_row="17" end_column="35" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="47" file_ref="0"/>
                <entry il_offset="0x51" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x5d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="13" end_row="15" end_column="42" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$It">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x6" start_row="16" start_column="24" end_row="16" end_column="38" file_ref="0"/>
                <entry il_offset="0x31" start_row="16" start_column="40" end_row="16" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x42">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="28" end_row="16" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="47" file_ref="0"/>
                <entry il_offset="0x51" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x5d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="13" end_row="15" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x76">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__2" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="33" end_row="15" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="65" end_row="15" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__9" parameterNames="$VB$It">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x6" start_row="16" start_column="24" end_row="16" end_column="38" file_ref="0"/>
                <entry il_offset="0x31" start_row="16" start_column="40" end_row="16" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x42">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__10" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="28" end_row="16" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="16" end_column="47" file_ref="0"/>
                <entry il_offset="0x51" start_row="17" start_column="9" end_row="17" end_column="35" file_ref="0"/>
                <entry il_offset="0x5d" start_row="18" start_column="5" end_row="18" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x5e">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x5e" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x5e" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="13" end_row="15" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9b">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__2" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="33" end_row="15" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="79" end_row="15" end_column="88" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="96" end_row="15" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__11" parameterNames="$VB$It">
            <sequencepoints total="3">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x6" start_row="16" start_column="24" end_row="16" end_column="38" file_ref="0"/>
                <entry il_offset="0x31" start_row="16" start_column="40" end_row="16" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x42">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__12" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="28" end_row="16" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="17" end_column="47" file_ref="0"/>
                <entry il_offset="0x76" start_row="18" start_column="9" end_row="18" end_column="35" file_ref="0"/>
                <entry il_offset="0x82" start_row="19" start_column="5" end_row="19" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x83">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="20" end_row="15" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$ItAnonymous">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="13" end_row="16" end_column="42" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__5" parameterNames="$VB$Group">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="17" start_column="24" end_row="17" end_column="38" file_ref="0"/>
                <entry il_offset="0x26" start_row="17" start_column="40" end_row="17" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x32">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="17" start_column="28" end_row="17" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="17" end_column="47" file_ref="0"/>
                <entry il_offset="0x76" start_row="18" start_column="9" end_row="18" end_column="35" file_ref="0"/>
                <entry il_offset="0x82" start_row="19" start_column="5" end_row="19" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x83">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="20" end_row="15" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$ItAnonymous">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="13" end_row="16" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x70">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="33" end_row="16" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="65" end_row="16" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x6">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__11" parameterNames="$VB$Group">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="17" start_column="24" end_row="17" end_column="38" file_ref="0"/>
                <entry il_offset="0x26" start_row="17" start_column="40" end_row="17" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x32">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__12" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="17" start_column="28" end_row="17" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="14" start_column="9" end_row="17" end_column="47" file_ref="0"/>
                <entry il_offset="0x76" start_row="18" start_column="9" end_row="18" end_column="35" file_ref="0"/>
                <entry il_offset="0x82" start_row="19" start_column="5" end_row="19" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x83">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="q" il_index="0" il_start="0x0" il_end="0x83" attributes="0"/>
                <local name="x" il_index="1" il_start="0x0" il_end="0x83" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="15" start_column="20" end_row="15" end_column="21" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="$VB$ItAnonymous">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="13" end_row="16" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x95">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__4" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="33" end_row="16" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__6" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="79" end_row="16" end_column="88" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__8" parameterNames="rangeVar3">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="16" start_column="96" end_row="16" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__13" parameterNames="$VB$Group">
            <sequencepoints total="2">
                <entry il_offset="0x0" start_row="17" start_column="24" end_row="17" end_column="38" file_ref="0"/>
                <entry il_offset="0x26" start_row="17" start_column="40" end_row="17" end_column="47" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x32">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__14" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="17" start_column="28" end_row="17" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="12" start_column="9" end_row="13" end_column="36" file_ref="0"/>
                <entry il_offset="0x56" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x57">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x57" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="12" start_column="33" end_row="12" end_column="40" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__3" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="13" start_column="22" end_row="13" end_column="35" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0xd">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            AssertXmlEqual(
<symbols>
    <methods>
        <method containingType="Module1" name="Nums" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="47" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="19" file_ref="0"/>
                <entry il_offset="0xe" start_row="8" start_column="5" end_row="8" end_column="17" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <namespace name="System.Collections" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <namespace name="System.Linq" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="Nums" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="Main" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="15" file_ref="0"/>
                <entry il_offset="0x1" start_row="12" start_column="9" end_row="13" end_column="47" file_ref="0"/>
                <entry il_offset="0x7e" start_row="14" start_column="5" end_row="14" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="x" il_index="0" il_start="0x0" il_end="0x7f" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x7f">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
                <local name="x" il_index="0" il_start="0x0" il_end="0x7f" attributes="0"/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__1" parameterNames="rangeVar1">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="12" start_column="65" end_row="12" end_column="71" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x2b">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__2" parameterNames="rangeVar2">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="12" start_column="54" end_row="12" end_column="61" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x3">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
        <method containingType="Module1" name="_Lambda$__7" parameterNames="$VB$It">
            <sequencepoints total="1">
                <entry il_offset="0x0" start_row="13" start_column="28" end_row="13" end_column="37" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward declaringType="Module1" methodName="Nums" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>,
GetPdbXml(compilation))
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

            Dim actual = GetPdbXml(compilation)

            Dim expected =
<symbols>
    <methods>
        <method containingType="IntervalUpdate" name="Update" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="2" start_column="5" end_row="2" end_column="31" file_ref="0"/>
                <entry il_offset="0x1" start_row="3" start_column="9" end_row="3" end_column="38" file_ref="0"/>
                <entry il_offset="0x15" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x16">
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="IntervalUpdate" name="Main" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="6" start_column="5" end_row="6" end_column="22" file_ref="0"/>
                <entry il_offset="0x1" start_row="7" start_column="9" end_row="7" end_column="17" file_ref="0"/>
                <entry il_offset="0x7" start_row="8" start_column="5" end_row="8" end_column="12" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x8">
                <importsforward declaringType="IntervalUpdate" methodName="Update" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyComputer" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="107" start_column="9" end_row="107" end_column="25" file_ref="0"/>
                <entry il_offset="0x1" start_row="108" start_column="13" end_row="108" end_column="25" file_ref="0"/>
                <entry il_offset="0x8" start_row="109" start_column="9" end_row="109" end_column="16" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name="My"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Computer" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="121" start_column="13" end_row="121" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="122" start_column="17" end_row="122" end_column="62" file_ref="0"/>
                <entry il_offset="0xe" start_row="123" start_column="13" end_row="123" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Computer" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_Application" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="133" start_column="13" end_row="133" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="134" start_column="17" end_row="134" end_column="57" file_ref="0"/>
                <entry il_offset="0xe" start_row="135" start_column="13" end_row="135" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Application" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_User" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="144" start_column="13" end_row="144" end_column="16" file_ref="0"/>
                <entry il_offset="0x1" start_row="145" start_column="17" end_row="145" end_column="58" file_ref="0"/>
                <entry il_offset="0xe" start_row="146" start_column="13" end_row="146" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="User" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name="get_WebServices" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="237" start_column="14" end_row="237" end_column="17" file_ref="0"/>
                <entry il_offset="0x1" start_row="238" start_column="17" end_row="238" end_column="67" file_ref="0"/>
                <entry il_offset="0xe" start_row="239" start_column="13" end_row="239" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="WebServices" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject" name=".cctor" parameterNames="">
            <sequencepoints total="4">
                <entry il_offset="0x0" start_row="126" start_column="26" end_row="126" end_column="97" file_ref="0"/>
                <entry il_offset="0xa" start_row="137" start_column="26" end_row="137" end_column="95" file_ref="0"/>
                <entry il_offset="0x14" start_row="148" start_column="26" end_row="148" end_column="136" file_ref="0"/>
                <entry il_offset="0x1e" start_row="284" start_column="26" end_row="284" end_column="105" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x29">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Equals" parameterNames="o">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="247" start_column="13" end_row="247" end_column="75" file_ref="0"/>
                <entry il_offset="0x1" start_row="248" start_column="17" end_row="248" end_column="40" file_ref="0"/>
                <entry il_offset="0x10" start_row="249" start_column="13" end_row="249" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x12">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Equals" il_index="0" il_start="0x0" il_end="0x12" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetHashCode" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="251" start_column="13" end_row="251" end_column="63" file_ref="0"/>
                <entry il_offset="0x1" start_row="252" start_column="17" end_row="252" end_column="42" file_ref="0"/>
                <entry il_offset="0xa" start_row="253" start_column="13" end_row="253" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetHashCode" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="GetType" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="255" start_column="13" end_row="255" end_column="72" file_ref="0"/>
                <entry il_offset="0x1" start_row="256" start_column="17" end_row="256" end_column="46" file_ref="0"/>
                <entry il_offset="0xe" start_row="257" start_column="13" end_row="257" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetType" il_index="0" il_start="0x0" il_end="0x10" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="ToString" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="259" start_column="13" end_row="259" end_column="59" file_ref="0"/>
                <entry il_offset="0x1" start_row="260" start_column="17" end_row="260" end_column="39" file_ref="0"/>
                <entry il_offset="0xa" start_row="261" start_column="13" end_row="261" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xc">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="ToString" il_index="0" il_start="0x0" il_end="0xc" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Create__Instance__" parameterNames="instance">
            <sequencepoints total="6">
                <entry il_offset="0x0" start_row="264" start_column="12" end_row="264" end_column="95" file_ref="0"/>
                <entry il_offset="0x1" start_row="265" start_column="17" end_row="265" end_column="44" file_ref="0"/>
                <entry il_offset="0xe" start_row="266" start_column="21" end_row="266" end_column="35" file_ref="0"/>
                <entry il_offset="0x16" start_row="267" start_column="17" end_row="267" end_column="21" file_ref="0"/>
                <entry il_offset="0x17" start_row="268" start_column="21" end_row="268" end_column="36" file_ref="0"/>
                <entry il_offset="0x1b" start_row="270" start_column="13" end_row="270" end_column="25" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x1d">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="Create__Instance__" il_index="0" il_start="0x0" il_end="0x1d" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name="Dispose__Instance__" parameterNames="instance">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="273" start_column="13" end_row="273" end_column="71" file_ref="0"/>
                <entry il_offset="0x1" start_row="274" start_column="17" end_row="274" end_column="35" file_ref="0"/>
                <entry il_offset="0x8" start_row="275" start_column="13" end_row="275" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+MyWebServices" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="279" start_column="13" end_row="279" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="280" start_column="16" end_row="280" end_column="28" file_ref="0"/>
                <entry il_offset="0x8" start_row="281" start_column="13" end_row="281" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name="get_GetInstance" parameterNames="">
            <sequencepoints total="5">
                <entry il_offset="0x0" start_row="341" start_column="17" end_row="341" end_column="20" file_ref="0"/>
                <entry il_offset="0x1" start_row="342" start_column="21" end_row="342" end_column="59" file_ref="0"/>
                <entry il_offset="0x12" start_row="342" start_column="60" end_row="342" end_column="87" file_ref="0"/>
                <entry il_offset="0x1c" start_row="343" start_column="21" end_row="343" end_column="47" file_ref="0"/>
                <entry il_offset="0x24" start_row="344" start_column="17" end_row="344" end_column="24" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x26">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
                <local name="GetInstance" il_index="0" il_start="0x0" il_end="0x26" attributes="0"/>
            </scope>
        </method>
        <method containingType="My.MyProject+ThreadSafeObjectProvider`1" name=".ctor" parameterNames="">
            <sequencepoints total="3">
                <entry il_offset="0x0" start_row="350" start_column="13" end_row="350" end_column="29" file_ref="0"/>
                <entry il_offset="0x1" start_row="351" start_column="17" end_row="351" end_column="29" file_ref="0"/>
                <entry il_offset="0x8" start_row="352" start_column="13" end_row="352" end_column="20" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x9">
                <importsforward declaringType="My.MyComputer" methodName=".ctor" parameterNames=""/>
            </scope>
        </method>
    </methods>
</symbols>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalCapturedAndHoisted()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_Lambda_Hoisted() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_0_Async_Lambda_Hoisted.MoveNext")

            ' Goal: We're looking for the double-mangled name "$VB$ResumableLocal_$VB$Closure_2$1".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_0_Async_Lambda_Hoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="15">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x12" start_row="5" start_column="5" end_row="5" end_column="58" file_ref="0"/>
                <entry il_offset="0x13" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x1e" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x2a" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x36" start_row="9" start_column="13" end_row="9" end_column="53" file_ref="0"/>
                <entry il_offset="0x48" start_row="11" start_column="9" end_row="11" end_column="55" file_ref="0"/>
                <entry il_offset="0xda" start_row="12" start_column="9" end_row="12" end_column="21" file_ref="0"/>
                <entry il_offset="0xeb" start_row="13" start_column="9" end_row="13" end_column="21" file_ref="0"/>
                <entry il_offset="0xfc" start_row="14" start_column="5" end_row="14" end_column="17" file_ref="0"/>
                <entry il_offset="0xfe" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x106" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x123" start_row="14" start_column="5" end_row="14" end_column="17" file_ref="0"/>
                <entry il_offset="0x12d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x13a" attributes="0"/>
                <local name="a" il_index="1" il_start="0x12" il_end="0xfd" attributes="0"/>
                <local name="$VB$ResumableLocal_$VB$Closure_2$1" il_index="0" il_start="0x12" il_end="0xfd" attributes="0" reusingslot="True"/>
                <local name="$ex" il_index="6" il_start="0xfe" il_end="0x122" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0x13a">
                <importsforward declaringType="C+_Closure$__1" methodName="_Lambda$__2" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0x13a" attributes="0"/>
                <scope startOffset="0x12" endOffset="0xfd">
                    <local name="a" il_index="1" il_start="0x12" il_end="0xfd" attributes="0"/>
                    <scope startOffset="0x12" endOffset="0xfd">
                        <local name="$VB$ResumableLocal_$VB$Closure_2$1" il_index="0" il_start="0x12" il_end="0xfd" attributes="0"/>
                    </scope>
                </scope>
                <scope startOffset="0xfe" endOffset="0x122">
                    <local name="$ex" il_index="6" il_start="0xfe" il_end="0x122" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="C" methodName="Async_Lambda_Hoisted" parameterNames=""/>
                <await yield="0x8e" resume="0xad" declaringType="C+VB$StateMachine_0_Async_Lambda_Hoisted" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalCapturedAndNotHoisted()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_Lambda_NotHoisted() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Dim a As Func(Of Integer) = Function() x + y

        Await Console.Out.WriteAsync((x + y).ToString)
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_0_Async_Lambda_NotHoisted.MoveNext")

            ' Goal: We're looking for the single-mangled name "$VB$Closure_1".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_0_Async_Lambda_NotHoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="13">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xf" start_row="5" start_column="5" end_row="5" end_column="61" file_ref="0"/>
                <entry il_offset="0x10" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x16" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x1d" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x24" start_row="9" start_column="13" end_row="9" end_column="53" file_ref="0"/>
                <entry il_offset="0x31" start_row="11" start_column="9" end_row="11" end_column="55" file_ref="0"/>
                <entry il_offset="0xb7" start_row="12" start_column="5" end_row="12" end_column="17" file_ref="0"/>
                <entry il_offset="0xb9" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xc1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xde" start_row="12" start_column="5" end_row="12" end_column="17" file_ref="0"/>
                <entry il_offset="0xe8" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xf5" attributes="0"/>
                <local name="$VB$Closure_1" il_index="1" il_start="0xf" il_end="0xb8" attributes="0"/>
                <local name="a" il_index="2" il_start="0xf" il_end="0xb8" attributes="0"/>
                <local name="$ex" il_index="7" il_start="0xb9" il_end="0xdd" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xf5">
                <importsforward declaringType="C+_Closure$__1" methodName="_Lambda$__2" parameterNames=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xf5" attributes="0"/>
                <scope startOffset="0xf" endOffset="0xb8">
                    <local name="$VB$Closure_1" il_index="1" il_start="0xf" il_end="0xb8" attributes="0"/>
                    <local name="a" il_index="2" il_start="0xf" il_end="0xb8" attributes="0"/>
                </scope>
                <scope startOffset="0xb9" endOffset="0xdd">
                    <local name="$ex" il_index="7" il_start="0xb9" il_end="0xdd" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="C" methodName="Async_Lambda_NotHoisted" parameterNames=""/>
                <await yield="0x6e" resume="0x8a" declaringType="C+VB$StateMachine_0_Async_Lambda_NotHoisted" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalHoistedAndNotCapture()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_NoLambda_Hoisted() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Await Console.Out.WriteAsync((x + y).ToString)
        x.ToString()
        y.ToString()
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_0_Async_NoLambda_Hoisted.MoveNext")

            ' Goal: We're looking for the single-mangled names "$VB$ResumableLocal_x$1" and "$VB$ResumableLocal_y$2".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_0_Async_NoLambda_Hoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="13">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xf" start_row="5" start_column="5" end_row="5" end_column="60" file_ref="0"/>
                <entry il_offset="0x10" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x17" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x1e" start_row="9" start_column="9" end_row="9" end_column="55" file_ref="0"/>
                <entry il_offset="0xa4" start_row="10" start_column="9" end_row="10" end_column="21" file_ref="0"/>
                <entry il_offset="0xb0" start_row="11" start_column="9" end_row="11" end_column="21" file_ref="0"/>
                <entry il_offset="0xbc" start_row="12" start_column="5" end_row="12" end_column="17" file_ref="0"/>
                <entry il_offset="0xbe" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xc6" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xe3" start_row="12" start_column="5" end_row="12" end_column="17" file_ref="0"/>
                <entry il_offset="0xed" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xfa" attributes="0"/>
                <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0xf" il_end="0xbd" attributes="0" reusingslot="True"/>
                <local name="$VB$ResumableLocal_y$2" il_index="0" il_start="0xf" il_end="0xbd" attributes="0" reusingslot="True"/>
                <local name="$ex" il_index="5" il_start="0xbe" il_end="0xe2" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xfa">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xfa" attributes="0"/>
                <scope startOffset="0xf" endOffset="0xbd">
                    <local name="$VB$ResumableLocal_x$1" il_index="0" il_start="0xf" il_end="0xbd" attributes="0"/>
                    <local name="$VB$ResumableLocal_y$2" il_index="0" il_start="0xf" il_end="0xbd" attributes="0"/>
                </scope>
                <scope startOffset="0xbe" endOffset="0xe2">
                    <local name="$ex" il_index="5" il_start="0xbe" il_end="0xe2" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="C" methodName="Async_NoLambda_Hoisted" parameterNames=""/>
                <await yield="0x58" resume="0x77" declaringType="C+VB$StateMachine_0_Async_NoLambda_Hoisted" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact(), WorkItem(827337, "DevDiv"), WorkItem(836491, "DevDiv")>
        Public Sub LocalNotHoistedAndNotCaptured()
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Threading.Tasks

Public Class C
    Private Async Function Async_NoLambda_NotHoisted() As Task
        Dim x As Integer = 1
        Dim y As Integer = 2

        Await Console.Out.WriteAsync((x + y).ToString)
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
                    source,
                    {MscorlibRef_v4_0_30316_17626, MsvbRef},
                    TestOptions.DebugDll)

            Dim actual = PDBTests.GetPdbXml(compilation, "C+VB$StateMachine_0_Async_NoLambda_NotHoisted.MoveNext")

            ' Goal: We're looking for the unmangled names "x" and "y".
            Dim expected =
<symbols>
    <methods>
        <method containingType="C+VB$StateMachine_0_Async_NoLambda_NotHoisted" name="MoveNext" parameterNames="">
            <sequencepoints total="11">
                <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xf" start_row="5" start_column="5" end_row="5" end_column="63" file_ref="0"/>
                <entry il_offset="0x10" start_row="6" start_column="13" end_row="6" end_column="29" file_ref="0"/>
                <entry il_offset="0x12" start_row="7" start_column="13" end_row="7" end_column="29" file_ref="0"/>
                <entry il_offset="0x14" start_row="9" start_column="9" end_row="9" end_column="55" file_ref="0"/>
                <entry il_offset="0x90" start_row="10" start_column="5" end_row="10" end_column="17" file_ref="0"/>
                <entry il_offset="0x92" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0x9a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                <entry il_offset="0xb7" start_row="10" start_column="5" end_row="10" end_column="17" file_ref="0"/>
                <entry il_offset="0xc1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
            </sequencepoints>
            <locals>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xce" attributes="0"/>
                <local name="x" il_index="1" il_start="0xf" il_end="0x91" attributes="0"/>
                <local name="y" il_index="2" il_start="0xf" il_end="0x91" attributes="0"/>
                <local name="$ex" il_index="7" il_start="0x92" il_end="0xb6" attributes="0"/>
            </locals>
            <scope startOffset="0x0" endOffset="0xce">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Threading.Tasks" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="VB$cachedState" il_index="0" il_start="0x0" il_end="0xce" attributes="0"/>
                <scope startOffset="0xf" endOffset="0x91">
                    <local name="x" il_index="1" il_start="0xf" il_end="0x91" attributes="0"/>
                    <local name="y" il_index="2" il_start="0xf" il_end="0x91" attributes="0"/>
                </scope>
                <scope startOffset="0x92" endOffset="0xb6">
                    <local name="$ex" il_index="7" il_start="0x92" il_end="0xb6" attributes="0"/>
                </scope>
            </scope>
            <async-info>
                <kickoff-method declaringType="C" methodName="Async_NoLambda_NotHoisted" parameterNames=""/>
                <await yield="0x47" resume="0x63" declaringType="C+VB$StateMachine_0_Async_NoLambda_NotHoisted" methodName="MoveNext" parameterNames=""/>
            </async-info>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
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
            AssertNoErrors(comp)

            Dim actual = PDBTests.GetPdbXml(comp, "My.MyApplication.Main")

            ' Just care that there's at least one non-hidden sequence point.
            Dim expected =
<symbols>
    <entryPoint declaringType="My.MyApplication" methodName="Main" parameterNames="Args"/>
    <methods>
        <method containingType="My.MyApplication" name="Main" parameterNames="Args">
            <sequencepoints total="7">
                <entry il_offset="0x0" start_row="76" start_column="9" end_row="76" end_column="55" file_ref="0"/>
                <entry il_offset="0x1" start_row="77" start_column="13" end_row="77" end_column="16" file_ref="0"/>
                <entry il_offset="0x2" start_row="78" start_column="16" end_row="78" end_column="133" file_ref="0"/>
                <entry il_offset="0xf" start_row="79" start_column="13" end_row="79" end_column="20" file_ref="0"/>
                <entry il_offset="0x11" start_row="80" start_column="13" end_row="80" end_column="20" file_ref="0"/>
                <entry il_offset="0x12" start_row="81" start_column="13" end_row="81" end_column="37" file_ref="0"/>
                <entry il_offset="0x1e" start_row="82" start_column="9" end_row="82" end_column="16" file_ref="0"/>
            </sequencepoints>
            <locals/>
            <scope startOffset="0x0" endOffset="0x1f">
                <currentnamespace name="My"/>
            </scope>
        </method>
    </methods>
</symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

    End Class

End Namespace
