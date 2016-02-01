' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBExternalSourceDirectiveTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TwoMethodsOnlyOneWithMapping()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()
        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
        Console.WriteLine("Hello World")
#End ExternalSource

        Console.WriteLine("Hello World")
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="70, 82, DD, 9A, 57, B3, BE, 57, 7E, E8, B4, AE, B8, 1E, 1B, 75, 38, 9D, 13, C9, "/>
        <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0x18" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xc" startLine="23" startColumn="9" endLine="23" endColumn="41" document="2"/>
                <entry offset="0x17" hidden="true" document="2"/>
                <entry offset="0x22" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x23">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub TwoMethodsOnlyOneWithMultipleMappingsAndRewriting()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()
        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
        Console.WriteLine("Hello World")
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def2.vb", 42)
        ' check that there are normal and hidden sequence points with mapping present
        ' because a for each will be rewritten
        for each i in new Integer() {1, 2, 3}
            Console.WriteLine(i)
        next i
#End ExternalSource

        Console.WriteLine("Hello World")

        ' hidden sequence points of rewritten statements will survive *iiks*.
        for each i in new Integer() {1, 2, 3}
            Console.WriteLine(i)
        next i
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="DB, A9, 94, EF, BC, DF, 10, C9, 60,  F, C0, C4, 9F, E4, 77, F9, 37, CF, E1, CE, "/>
        <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
        <file id="3" name="C:\abc\def2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0x18" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="372"/>
                    <slot kind="6" offset="363"/>
                    <slot kind="8" offset="363"/>
                    <slot kind="1" offset="363"/>
                    <slot kind="6" offset="606"/>
                    <slot kind="8" offset="606"/>
                    <slot kind="1" offset="606"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xc" startLine="23" startColumn="9" endLine="23" endColumn="41" document="2"/>
                <entry offset="0x17" startLine="24" startColumn="9" endLine="24" endColumn="41" document="2"/>
                <entry offset="0x22" startLine="44" startColumn="9" endLine="44" endColumn="46" document="3"/>
                <entry offset="0x36" hidden="true" document="3"/>
                <entry offset="0x41" startLine="45" startColumn="13" endLine="45" endColumn="33" document="3"/>
                <entry offset="0x4d" startLine="46" startColumn="9" endLine="46" endColumn="15" document="3"/>
                <entry offset="0x4e" hidden="true" document="3"/>
                <entry offset="0x52" hidden="true" document="3"/>
                <entry offset="0x59" hidden="true" document="3"/>
                <entry offset="0x5c" hidden="true" document="3"/>
                <entry offset="0x67" hidden="true" document="3"/>
                <entry offset="0x7d" hidden="true" document="3"/>
                <entry offset="0x8a" hidden="true" document="3"/>
                <entry offset="0x96" hidden="true" document="3"/>
                <entry offset="0x97" hidden="true" document="3"/>
                <entry offset="0x9d" hidden="true" document="3"/>
                <entry offset="0xa7" hidden="true" document="3"/>
                <entry offset="0xab" hidden="true" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xac">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
                <local name="i" il_index="0" il_start="0x0" il_end="0xac" attributes="0"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub EmptyExternalSourceWillBeIgnored()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()
        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
#End ExternalSource

        Console.WriteLine("Hello World")
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="EE, 47, B3, F6, 59, FA,  D, E8, DF, B2, 26, 6A, 7D, 82, D3, 52, 3E,  C, 36, E1, "/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="37" document="1"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="37" document="1"/>
                <entry offset="0x7" startLine="11" startColumn="9" endLine="11" endColumn="42" document="1"/>
                <entry offset="0x18" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="15" startColumn="9" endLine="15" endColumn="41" document="1"/>
                <entry offset="0xc" startLine="20" startColumn="9" endLine="20" endColumn="41" document="1"/>
                <entry offset="0x17" startLine="21" startColumn="5" endLine="21" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWillBeIgnored()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()
#ExternalSource("C:\abc\def.vb", 21)
#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 22)
#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
#End ExternalSource
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="B9, 85, 76, 74, 1E, E7, 27, 25, F7, 8A, CB, A2, B1, 9C, A4, CD, FD, 49, 8C, B7, "/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="9" startColumn="5" endLine="9" endColumn="37" document="1"/>
                <entry offset="0x1" startLine="10" startColumn="13" endLine="10" endColumn="37" document="1"/>
                <entry offset="0x7" startLine="11" startColumn="9" endLine="11" endColumn="42" document="1"/>
                <entry offset="0x18" startLine="12" startColumn="5" endLine="12" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="14" startColumn="5" endLine="14" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="18" startColumn="9" endLine="18" endColumn="41" document="1"/>
                <entry offset="0xc" startLine="23" startColumn="9" endLine="23" endColumn="41" document="1"/>
                <entry offset="0x17" startLine="27" startColumn="5" endLine="27" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWithNonEmptyExternalSource()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()
#ExternalSource("C:\abc\def.vb", 21)
#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 22)
#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
' boo!
#End ExternalSource
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="B6, 80, 9E, 65, 43, 38,  0, C1, 35, 7F, AE, D0, 60, F2, 24, 44, A8, 11, C2, 63, "/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0x18" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xc" hidden="true" document="1"/>
                <entry offset="0x17" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWithNonEmptyExternalSourceFollowedByEmptyExternalSource()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System
Imports System.Collections.Generic

Class C1
    Public Shared Sub FooInvisible()
        dim str as string = "World!"
        Console.WriteLine("Hello " &amp; str)
    End Sub

    Public Shared Sub Main()

#ExternalSource("C:\abc\def.vb", 21)
#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 22)

#End ExternalSource

        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 23)
#End ExternalSource

        Console.WriteLine("Hello World")
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="73,  5, 84, 40, AC, E0, 15, 63, CC, FE, BD, 9A, 99, 23, AA, BD, 24, 40, 24, 44, "/>
    </files>
    <entryPoint declaringType="C1" methodName="Main"/>
    <methods>
        <method containingType="C1" name="FooInvisible">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
                <entry offset="0x18" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x19">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
                <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
            </scope>
        </method>
        <method containingType="C1" name="Main">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xc" hidden="true" document="1"/>
                <entry offset="0x17" hidden="true" document="1"/>
                <entry offset="0x22" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x23">
                <importsforward declaringType="C1" methodName="FooInvisible"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub TestPartialClassFieldInitializersWithExternalSource()
            Dim source =
<compilation>
    <file name="C:\Abc\ACTUAL.vb">
        
#ExternalSource("C:\abc\def1.vb", 41)

Option strict on
imports system

partial Class C1
    public f1 as integer = 23
#End ExternalSource

#ExternalSource("C:\abc\def1.vb", 10)
    Public sub DumpFields()
        Console.WriteLine(f1)
        Console.WriteLine(f2)
    End Sub
#End ExternalSource

#ExternalSource("C:\abc\def1.vb", 1)
    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
#End ExternalSource
End Class

Class InActual
    public f1 as integer = 23
end Class

#ExternalChecksum("C:\abc\def2.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "1234")
#ExternalChecksum("BOGUS.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "1234")
#ExternalChecksum("C:\Abc\ACTUAL.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "6789")

    </file>

    <file name="b.vb">
Option strict on
imports system

partial Class C1

#ExternalSource("C:\abc\def2.vb", 23)
    ' more lines to see a different span in the sequence points ...



                            public f2 as integer = 42
#End ExternalSource

#ExternalChecksum("BOGUS.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "1234")
#ExternalChecksum("C:\Abc\ACTUAL.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "6789")


End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="C:\Abc\ACTUAL.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="27, 52, E9, 85, 5A, AC, 31,  5, A5, 6F, 70, 40, 55, 3A, 9C, 43, D2,  7,  D, 4B, "/>
        <file id="2" name="C:\abc\def1.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
        <file id="3" name="C:\abc\def2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a799" checkSum="12, 34, "/>
    </files>
    <entryPoint declaringType="C1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="46" startColumn="12" endLine="46" endColumn="30" document="2"/>
                <entry offset="0xf" startLine="27" startColumn="36" endLine="27" endColumn="54" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x18">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method containingType="C1" name="DumpFields">
            <sequencePoints>
                <entry offset="0x0" startLine="10" startColumn="5" endLine="10" endColumn="28" document="2"/>
                <entry offset="0x1" startLine="11" startColumn="9" endLine="11" endColumn="30" document="2"/>
                <entry offset="0xd" startLine="12" startColumn="9" endLine="12" endColumn="30" document="2"/>
                <entry offset="0x19" startLine="13" startColumn="5" endLine="13" endColumn="12" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1a">
                <importsforward declaringType="C1" methodName=".ctor"/>
            </scope>
        </method>
        <method containingType="C1" name="Main" parameterNames="args">
            <customDebugInfo>
                <encLocalSlotMap>
                    <slot kind="0" offset="4"/>
                </encLocalSlotMap>
            </customDebugInfo>
            <sequencePoints>
                <entry offset="0x0" startLine="1" startColumn="5" endLine="1" endColumn="45" document="2"/>
                <entry offset="0x1" startLine="2" startColumn="13" endLine="2" endColumn="24" document="2"/>
                <entry offset="0x7" startLine="3" startColumn="9" endLine="3" endColumn="23" document="2"/>
                <entry offset="0xe" startLine="4" startColumn="5" endLine="4" endColumn="12" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xf">
                <importsforward declaringType="C1" methodName=".ctor"/>
                <local name="c" il_index="0" il_start="0x0" il_end="0xf" attributes="0"/>
            </scope>
        </method>
        <method containingType="InActual" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" hidden="true" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x10">
                <importsforward declaringType="C1" methodName=".ctor"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <Fact()>
        Public Sub IllegalExternalSourceUsageShouldNotAssert_1()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on

public Class C1

#ExternalSource("bar1.vb", 41)
#ExternalSource("bar1.vb", 41)
    public shared sub main()
    End sub
#End ExternalSource
#End ExternalSource

    boo
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_NestedExternalSource, "#ExternalSource(""bar1.vb"", 41)"),
                                                              Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"))
        End Sub

        <Fact()>
        Public Sub IllegalExternalSourceUsageShouldNotAssert_2()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on

public Class C1

#End ExternalSource
#End ExternalSource
    public shared sub main()
    End sub

    boo
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"),
                                                              Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"),
                                                              Diagnostic(ERRID.ERR_ExpectedDeclaration, "boo"))
        End Sub

        <Fact()>
        Public Sub IllegalExternalSourceUsageShouldNotAssert_3()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on

public Class C1


#End ExternalSource
#ExternalSource("bar1.vb", 23)

    public shared sub main()
    End sub

    boo
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"),
                                                              Diagnostic(ERRID.ERR_ExpectedEndExternalSource, "#ExternalSource(""bar1.vb"", 23)"),
                                                              Diagnostic(ERRID.ERR_ExpectedDeclaration, "boo"))
        End Sub

        <WorkItem(545302, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545302")>
        <Fact()>
        Public Sub IllegalExternalSourceUsageShouldNotAssert_4()
            Dim source =
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()
#ExternalSource ("bar1.vb", 23)
#ExternalSource ("bar1.vb", 23)
        System.Console.WriteLine("boo")
#End ExternalSource
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                TestOptions.ReleaseExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_NestedExternalSource, "#ExternalSource (""bar1.vb"", 23)"))
        End Sub

        <WorkItem(545307, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545307")>
        <Fact>
        Public Sub OverflowLineNumbers()
            Dim source =
    <compilation>
        <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Module Program
    Public Sub Main()
        Console.WriteLine("Hello World")

#ExternalSource("C:\abc\def.vb", 0)
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def.vb", 1)
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def.vb", 2147483647)
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def.vb", 2147483648)
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def.vb", 2147483649)
        Console.WriteLine("Hello World")
#End ExternalSource

#ExternalSource("C:\abc\def.vb", &amp;hfeefed)
        Console.WriteLine("Hello World")
#End ExternalSource

        Console.WriteLine("Hello World")
    End Sub
End Module
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="D2, FF,  5, F8, B7, A2, 25, B0, 96, D9, 97, 2F,  5, F8, F0, B5, 81, 8D, 98, 1D, "/>
        <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
    </files>
    <entryPoint declaringType="Program" methodName="Main"/>
    <methods>
        <method containingType="Program" name="Main">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0xc" startLine="0" startColumn="9" endLine="0" endColumn="41" document="2"/>
                <entry offset="0x17" startLine="1" startColumn="9" endLine="1" endColumn="41" document="2"/>
                <entry offset="0x22" startLine="16777215" startColumn="9" endLine="16777215" endColumn="41" document="2"/>
                <entry offset="0x2d" startLine="16777215" startColumn="9" endLine="16777215" endColumn="41" document="2"/>
                <entry offset="0x38" startLine="16777215" startColumn="9" endLine="16777215" endColumn="41" document="2"/>
                <entry offset="0x43" startLine="16707565" startColumn="9" endLine="16707565" endColumn="41" document="2"/>
                <entry offset="0x4e" hidden="true" document="2"/>
                <entry offset="0x59" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x5a">
                <namespace name="System" importlevel="file"/>
                <namespace name="System.Collections.Generic" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>, format:=DebugInformationFormat.Pdb)
        End Sub

        <Fact, WorkItem(846584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846584")>
        Public Sub RelativePathForExternalSource()
            Dim source =
<compilation>
    <file name="C:\Folder1\Folder2\Test1.vb">
#ExternalChecksum("..\Test2.vb","{406ea660-64cf-4c82-b6f0-42d48172a799}","DB788882721B2B27C90579D5FE2A0418")

Class Test1
    Sub Main()
        #ExternalSource("..\Test2.vb",4)
	Main()
        #End ExternalSource
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll.WithSourceReferenceResolver(SourceFileResolver.Default))

            compilation.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="C:\Folder1\Folder2\Test1.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="B9, 49, 3D, 62, 89, 9B, B2, 2F, B6, 72, 90, A1, 2D,  1, 11, 89, B4, C2, 83, B4, "/>
        <file id="2" name="C:\Folder1\Test2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a799" checkSum="DB, 78, 88, 82, 72, 1B, 2B, 27, C9,  5, 79, D5, FE, 2A,  4, 18, "/>
    </files>
    <methods>
        <method containingType="Test1" name="Main">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="2" endLine="4" endColumn="8" document="2"/>
                <entry offset="0x8" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x9">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class

End Namespace

