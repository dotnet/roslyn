' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBExternalSourceDirectiveTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TwoMethodsOnlyOneWithMapping()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="DF, AE, 30, 1B, 67, A9, A3, 3D,  6, 9F, C1, B3, 2A, A0, 80, E3,  4, 7F, 4B, 91, "/>
                                   <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x18" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="5">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0xc" start_row="23" start_column="9" end_row="23" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x17" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="2"/>
                                           <entry il_offset="0x22" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="2"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x23">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>


            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub TwoMethodsOnlyOneWithMultipleMappingsAndRewriting()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="65, 76,  F, 5A, AF, 25, A8, AC, 3D, A0, 45, 59, 8A, 29, 21, 21, 87, 2C, 94, 5D, "/>
                                   <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
                                   <file id="3" name="C:\abc\def2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x18" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="18">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0xc" start_row="23" start_column="9" end_row="23" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x17" start_row="24" start_column="9" end_row="24" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x22" start_row="44" start_column="9" end_row="44" end_column="46" file_ref="3"/>
                                           <entry il_offset="0x36" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x41" start_row="45" start_column="13" end_row="45" end_column="33" file_ref="3"/>
                                           <entry il_offset="0x4d" start_row="46" start_column="9" end_row="46" end_column="15" file_ref="3"/>
                                           <entry il_offset="0x4e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x52" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x5c" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x67" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x7d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x8a" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x96" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x97" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0x9d" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                           <entry il_offset="0xa9" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="3"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="i" il_index="0" il_start="0x0" il_end="0xaa" attributes="0"/>
                                           <local name="VB$ForEachArray" il_index="1" il_start="0x22" il_end="0x5b" attributes="1"/>
                                           <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x22" il_end="0x5b" attributes="1"/>
                                           <local name="VB$ForEachArray" il_index="4" il_start="0x67" il_end="0xa8" attributes="1"/>
                                           <local name="VB$ForEachArrayIndex" il_index="5" il_start="0x67" il_end="0xa8" attributes="1"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0xaa">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                           <local name="i" il_index="0" il_start="0x0" il_end="0xaa" attributes="0"/>
                                           <scope startOffset="0x22" endOffset="0x5b">
                                               <local name="VB$ForEachArray" il_index="1" il_start="0x22" il_end="0x5b" attributes="1"/>
                                               <local name="VB$ForEachArrayIndex" il_index="2" il_start="0x22" il_end="0x5b" attributes="1"/>
                                           </scope>
                                           <scope startOffset="0x67" endOffset="0xa8">
                                               <local name="VB$ForEachArray" il_index="4" il_start="0x67" il_end="0xa8" attributes="1"/>
                                               <local name="VB$ForEachArrayIndex" il_index="5" il_start="0x67" il_end="0xa8" attributes="1"/>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub EmptyExternalSourceWillBeIgnored()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="1B, 27, 83, 49, 73,  B, CD,  B,  9, B1, DC, C7, FB,  F, 3C,  7, 43, 55, D3, C4, "/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="37" file_ref="1"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="37" file_ref="1"/>
                                           <entry il_offset="0x7" start_row="11" start_column="9" end_row="11" end_column="42" file_ref="1"/>
                                           <entry il_offset="0x18" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="14" start_column="5" end_row="14" end_column="29" file_ref="1"/>
                                           <entry il_offset="0x1" start_row="15" start_column="9" end_row="15" end_column="41" file_ref="1"/>
                                           <entry il_offset="0xc" start_row="20" start_column="9" end_row="20" end_column="41" file_ref="1"/>
                                           <entry il_offset="0x17" start_row="21" start_column="5" end_row="21" end_column="12" file_ref="1"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x18">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWillBeIgnored()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="39, CD,  4, 94, 17, A3, 40,  D, F3, DB, 40, FF, FA, DE, 1E, 1A, 36, 20, 83, 3B, "/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="9" start_column="5" end_row="9" end_column="37" file_ref="1"/>
                                           <entry il_offset="0x1" start_row="10" start_column="13" end_row="10" end_column="37" file_ref="1"/>
                                           <entry il_offset="0x7" start_row="11" start_column="9" end_row="11" end_column="42" file_ref="1"/>
                                           <entry il_offset="0x18" start_row="12" start_column="5" end_row="12" end_column="12" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="14" start_column="5" end_row="14" end_column="29" file_ref="1"/>
                                           <entry il_offset="0x1" start_row="18" start_column="9" end_row="18" end_column="41" file_ref="1"/>
                                           <entry il_offset="0xc" start_row="23" start_column="9" end_row="23" end_column="41" file_ref="1"/>
                                           <entry il_offset="0x17" start_row="27" start_column="5" end_row="27" end_column="12" file_ref="1"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x18">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWithNonEmptyExternalSource()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="C5, D5, A4, 4F, E0, 43, 7E, F3, 89, 89, ED, 89, AF, A7, 11, EA, 74, 89, 10, 53, "/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x18" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0xc" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x17" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x18">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub MultipleEmptyExternalSourceWithNonEmptyExternalSourceFollowedByEmptyExternalSource()
            Dim source =
<compilation>
    <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum=" 4, E4, 78, D6, 3B, E1, 9E, 8B, 4C, A7, 1B, 19, F5, 4A, 58, D2, 88, 6C, 1B, 2B, "/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="FooInvisible" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x7" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x18" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x19">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                           <local name="str" il_index="0" il_start="0x0" il_end="0x19" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="5">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0xc" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x17" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x22" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x23">
                                           <importsforward declaringType="C1" methodName="FooInvisible" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact()>
        Public Sub TestPartialClassFieldInitializersWithExternalSource()
            Dim source =
<compilation>
    <file name="C:\Abc\ACTUAL.vb" checksum="default">
        
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                                source,
                                OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="C:\Abc\ACTUAL.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="CA, ED, BA, 1B, AA, CF, 42, AE, 41, 3E, A1, 8E, A7, 99, 4C, 68, CF, D7, 60,  A, "/>
                                   <file id="2" name="C:\abc\def1.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
                                   <file id="3" name="C:\abc\def2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a799" checkSum="12, 34, "/>
                               </files>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames="args"/>
                               <methods>
                                   <method containingType="C1" name=".ctor" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x6" start_row="46" start_column="12" end_row="46" end_column="30" file_ref="2"/>
                                           <entry il_offset="0xe" start_row="27" start_column="36" end_row="27" end_column="54" file_ref="3"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x17">
                                           <namespace name="System" importlevel="file"/>
                                           <currentnamespace name=""/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="DumpFields" parameterNames="">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="10" start_column="5" end_row="10" end_column="28" file_ref="2"/>
                                           <entry il_offset="0x1" start_row="11" start_column="9" end_row="11" end_column="30" file_ref="2"/>
                                           <entry il_offset="0xd" start_row="12" start_column="9" end_row="12" end_column="30" file_ref="2"/>
                                           <entry il_offset="0x19" start_row="13" start_column="5" end_row="13" end_column="12" file_ref="2"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x1a">
                                           <importsforward declaringType="C1" methodName=".ctor" parameterNames=""/>
                                       </scope>
                                   </method>
                                   <method containingType="C1" name="Main" parameterNames="args">
                                       <sequencepoints total="4">
                                           <entry il_offset="0x0" start_row="1" start_column="5" end_row="1" end_column="45" file_ref="2"/>
                                           <entry il_offset="0x1" start_row="2" start_column="13" end_row="2" end_column="24" file_ref="2"/>
                                           <entry il_offset="0x7" start_row="3" start_column="9" end_row="3" end_column="23" file_ref="2"/>
                                           <entry il_offset="0xe" start_row="4" start_column="5" end_row="4" end_column="12" file_ref="2"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="c" il_index="0" il_start="0x0" il_end="0xf" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0xf">
                                           <importsforward declaringType="C1" methodName=".ctor" parameterNames=""/>
                                           <local name="c" il_index="0" il_start="0x0" il_end="0xf" attributes="0"/>
                                       </scope>
                                   </method>
                                   <method containingType="InActual" name=".ctor" parameterNames="">
                                       <sequencepoints total="2">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x6" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0xf">
                                           <importsforward declaringType="C1" methodName=".ctor" parameterNames=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
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
                                OptionsExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_NestedExternalSource, "#ExternalSource(""bar1.vb"", 41)"),
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
                                OptionsExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"),
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
                                OptionsExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_EndExternalSource, "#End ExternalSource"),
                                                              Diagnostic(ERRID.ERR_ExpectedEndExternalSource, "#ExternalSource(""bar1.vb"", 23)"),
                                                              Diagnostic(ERRID.ERR_ExpectedDeclaration, "boo"))
        End Sub

        <WorkItem(545302, "DevDiv")>
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
                                OptionsExe).VerifyDiagnostics(Diagnostic(ERRID.ERR_NestedExternalSource, "#ExternalSource (""bar1.vb"", 23)"))
        End Sub

        <WorkItem(545307, "DevDiv")>
        <Fact>
        Public Sub OverflowLineNumbers()
            Dim source =
    <compilation>
        <file name="a.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsExe.WithOptimizations(False))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="69, 72,  D, 99, 17, F1, 6D, 62, 6F, 8B, A1, 6B, CF,  D, 1D, 18, 36, 5A, D9, 8E, "/>
                                   <file id="2" name="C:\abc\def.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
                               </files>
                               <entryPoint declaringType="Program" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="Program" name="Main" parameterNames="">
                                       <sequencepoints total="10">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0xc" start_row="0" start_column="9" end_row="0" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x17" start_row="1" start_column="9" end_row="1" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x22" start_row="16777215" start_column="9" end_row="16777215" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x2d" start_row="16777215" start_column="9" end_row="16777215" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x38" start_row="16777215" start_column="9" end_row="16777215" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x43" start_row="16707565" start_column="9" end_row="16707565" end_column="41" file_ref="2"/>
                                           <entry il_offset="0x4e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="2"/>
                                           <entry il_offset="0x59" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="2"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x5a">
                                           <namespace name="System" importlevel="file"/>
                                           <namespace name="System.Collections.Generic" importlevel="file"/>
                                           <currentnamespace name=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact, WorkItem(846584, "DevDiv")>
        Public Sub RelativePathForExternalSource()
            Dim source =
<compilation>
    <file name="C:\Folder1\Folder2\Test1.vb" checksum="default">
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

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    OptionsDll.WithOptimizations(False).WithSourceReferenceResolver(SourceFileResolver.Default))

            Dim actual = PDBTests.GetPdbXml(compilation)

            Dim expected = <symbols>
                               <files>
                                   <file id="1" name="C:\Folder1\Folder2\Test1.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="D6, 39, 82, D1, FD, 90,  9, A2, F7, 70, AA, 5F, 46, 2C, B1, 7C, 59, 55, AC, 80, "/>
                                   <file id="2" name="C:\Folder1\Test2.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a799" checkSum="DB, 78, 88, 82, 72, 1B, 2B, 27, C9,  5, 79, D5, FE, 2A,  4, 18, "/>
                               </files>
                               <methods>
                                   <method containingType="Test1" name="Main" parameterNames="">
                                       <sequencepoints total="3">
                                           <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                                           <entry il_offset="0x1" start_row="4" start_column="2" end_row="4" end_column="8" file_ref="2"/>
                                           <entry il_offset="0x8" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="2"/>
                                       </sequencepoints>
                                       <locals/>
                                       <scope startOffset="0x0" endOffset="0x9">
                                           <currentnamespace name=""/>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

    End Class

End Namespace

