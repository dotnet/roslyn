' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBVariableInitializerTests
        Inherits BasicTestBase

        <Fact>
        Public Sub PartialClass()
            Dim source =
    <compilation>
        <file name="a.vb">
Option strict on
imports system

partial Class C1

    public f1 as integer = 23
    public f3 As New C1()
    public f4, f5 As New C1()

    Public sub DumpFields()
        Console.WriteLine(f1)
        Console.WriteLine(f2)
    End Sub

    Public shared Sub Main(args() as string)
        Dim c as new C1
        c.DumpFields()
    End sub
End Class
    </file>

        <file name="b.vb">
Option strict on
imports system

partial Class C1


    ' more lines to see a different span in the sequence points ...



                            public f2 as integer = 42

End Class
    </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.DebugExe)

            Dim actual = GetPdbXml(compilation, "C1..ctor")

            Dim expected =
    <symbols>
        <files>
            <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum=" 1, 41, D1, CA, DD, B0,  B, 39, BE, 3C, 3D, 69, AA, 18, B3, 7A, F5, 65, C5, DD, "/>
            <file id="2" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="FE, FF, 3A, FC, 5E, 54, 7C, 6D, 96, 86,  5, B8, B6, FD, FC, 5F, 81, 51, AE, FA, "/>
        </files>
        <entryPoint declaringType="C1" methodName="Main" parameterNames="args"/>
        <methods>
            <method containingType="C1" name=".ctor" parameterNames="">
                <sequencepoints total="6">
                    <entry il_offset="0x0" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="1"/>
                    <entry il_offset="0x6" start_row="6" start_column="12" end_row="6" end_column="30" file_ref="1"/>
                    <entry il_offset="0xe" start_row="7" start_column="12" end_row="7" end_column="26" file_ref="1"/>
                    <entry il_offset="0x19" start_row="8" start_column="12" end_row="8" end_column="14" file_ref="1"/>
                    <entry il_offset="0x24" start_row="8" start_column="16" end_row="8" end_column="18" file_ref="1"/>
                    <entry il_offset="0x2f" start_row="11" start_column="36" end_row="11" end_column="54" file_ref="2"/>
                </sequencepoints>
                <locals/>
                <scope startOffset="0x0" endOffset="0x38">
                    <namespace name="System" importlevel="file"/>
                    <currentnamespace name=""/>
                </scope>
            </method>
        </methods>
    </symbols>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub AutoProperty1()
            Dim source =
<compilation>
    <file>
Interface I
    Property P As Integer
End Interface

Class C
    Implements I

    Property P As Integer = 1 Implements I.P
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P As Integer = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="8" start_column=<%= expectedStart1 %> end_row="8" end_column=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub AutoProperty2()
            Dim source =
<compilation>
    <file>
Interface I
    Property P As Object
End Interface

Class C
    Implements I

    Property P = 1 Implements I.P
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="8" start_column=<%= expectedStart1 %> end_row="8" end_column=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub AutoPropertyAsNew()
            Dim source =
<compilation>
    <file>
Interface I
    Property P As Integer
End Interface

Class C
    Implements I

    Property P As New Integer Implements I.P
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P As New Integer".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="8" start_column=<%= expectedStart1 %> end_row="8" end_column=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub ArrayInitializedField()
            Dim source =
<compilation>
    <file>
Class C
    Dim F(1), G(2) As Integer
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F(1)".Length + 1

            Dim expectedStart2 = "    Dim F(1), ".Length + 1
            Dim expectedEnd2 = "    Dim F(1), G(2)".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="2" start_column=<%= expectedStart1 %> end_row="2" end_column=<%= expectedEnd1 %>/>
    <entry start_row="2" start_column=<%= expectedStart2 %> end_row="2" end_column=<%= expectedEnd2 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub ArrayInitializedLocal()
            Dim source =
<compilation>
    <file>
Class C
    Sub M
        Dim F(1), G(2) As Integer
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F(1)".Length + 1

            Dim expectedStart2 = "        Dim F(1), ".Length + 1
            Dim expectedEnd2 = "        Dim F(1), G(2)".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="2" start_column="5" end_row="2" end_column="10"/>
    <entry start_row="3" start_column=<%= expectedStart1 %> end_row="3" end_column=<%= expectedEnd1 %>/>
    <entry start_row="3" start_column=<%= expectedStart2 %> end_row="3" end_column=<%= expectedEnd2 %>/>
    <entry start_row="4" start_column="5" end_row="4" end_column="12"/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub FieldAsNewMultiInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F, G As New C()
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F".Length + 1

            Dim expectedStart2 = "    Dim F, ".Length + 1
            Dim expectedEnd2 = "    Dim F, G".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="2" start_column=<%= expectedStart1 %> end_row="2" end_column=<%= expectedEnd1 %>/>
    <entry start_row="2" start_column=<%= expectedStart2 %> end_row="2" end_column=<%= expectedEnd2 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub LocalAsNewMultiInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Sub M
         Dim F, G As New C()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "         Dim ".Length + 1
            Dim expectedEnd1 = "         Dim F".Length + 1

            Dim expectedStart2 = "         Dim F, ".Length + 1
            Dim expectedEnd2 = "         Dim F, G".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="2" start_column="5" end_row="2" end_column="10"/>
    <entry start_row="3" start_column=<%= expectedStart1 %> end_row="3" end_column=<%= expectedEnd1 %>/>
    <entry start_row="3" start_column=<%= expectedStart2 %> end_row="3" end_column=<%= expectedEnd2 %>/>
    <entry start_row="4" start_column="5" end_row="4" end_column="12"/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub FieldAsNewSingleInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F As New C()
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F As New C()".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="2" start_column=<%= expectedStart1 %> end_row="2" end_column=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub LocalAsNewSingleInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Sub M
        Dim F As New C()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F As New C()".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="2" start_column="5" end_row="2" end_column="10"/>
    <entry start_row="3" start_column=<%= expectedStart1 %> end_row="3" end_column=<%= expectedEnd1 %>/>
    <entry start_row="4" start_column="5" end_row="4" end_column="12"/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub FieldInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F = 1
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
    <entry start_row="2" start_column=<%= expectedStart1 %> end_row="2" end_column=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub LocalInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Sub M
        Dim F = 1
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry start_row="2" start_column="5" end_row="2" end_column="10"/>
    <entry start_row="3" start_column=<%= expectedStart1 %> end_row="3" end_column=<%= expectedEnd1 %>/>
    <entry start_row="4" start_column="5" end_row="4" end_column="12"/>
</sequencePoints>

            AssertXmlEqual(expected, actual)
        End Sub
    End Class
End Namespace