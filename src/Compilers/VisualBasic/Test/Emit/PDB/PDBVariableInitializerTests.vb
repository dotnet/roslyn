' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBVariableInitializerTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.DebugExe)
            compilation.VerifyPdb("C1..ctor",
<symbols>
    <files>
        <file id="1" name="a.vb" language="VB" checksumAlgorithm="SHA1" checksum="01-41-D1-CA-DD-B0-0B-39-BE-3C-3D-69-AA-18-B3-7A-F5-65-C5-DD"/>
        <file id="2" name="b.vb" language="VB" checksumAlgorithm="SHA1" checksum="FE-FF-3A-FC-5E-54-7C-6D-96-86-05-B8-B6-FD-FC-5F-81-51-AE-FA"/>
    </files>
    <entryPoint declaringType="C1" methodName="Main" parameterNames="args"/>
    <methods>
        <method containingType="C1" name=".ctor">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x7" startLine="6" startColumn="12" endLine="6" endColumn="30" document="1"/>
                <entry offset="0xf" startLine="7" startColumn="12" endLine="7" endColumn="26" document="1"/>
                <entry offset="0x1a" startLine="8" startColumn="12" endLine="8" endColumn="14" document="1"/>
                <entry offset="0x25" startLine="8" startColumn="16" endLine="8" endColumn="18" document="1"/>
                <entry offset="0x30" startLine="11" startColumn="36" endLine="11" endColumn="54" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x39">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P As Integer = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="8" startColumn=<%= expectedStart1 %> endLine="8" endColumn=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="8" startColumn=<%= expectedStart1 %> endLine="8" endColumn=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Property ".Length + 1
            Dim expectedEnd1 = "    Property P As New Integer".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="8" startColumn=<%= expectedStart1 %> endLine="8" endColumn=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub ArrayInitializedField()
            Dim source =
<compilation>
    <file>
Class C
    Dim F(1), G(2) As Integer
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F(1)".Length + 1

            Dim expectedStart2 = "    Dim F(1), ".Length + 1
            Dim expectedEnd2 = "    Dim F(1), G(2)".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="2" startColumn=<%= expectedStart1 %> endLine="2" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="2" startColumn=<%= expectedStart2 %> endLine="2" endColumn=<%= expectedEnd2 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F(1)".Length + 1

            Dim expectedStart2 = "        Dim F(1), ".Length + 1
            Dim expectedEnd2 = "        Dim F(1), G(2)".Length + 1

            Dim expected =
<sequencePoints>
    <entry startLine="2" startColumn="5" endLine="2" endColumn="10"/>
    <entry startLine="3" startColumn=<%= expectedStart1 %> endLine="3" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="3" startColumn=<%= expectedStart2 %> endLine="3" endColumn=<%= expectedEnd2 %>/>
    <entry startLine="4" startColumn="5" endLine="4" endColumn="12"/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub FieldAsNewMultiInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F, G As New C()
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F".Length + 1

            Dim expectedStart2 = "    Dim F, ".Length + 1
            Dim expectedEnd2 = "    Dim F, G".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="2" startColumn=<%= expectedStart1 %> endLine="2" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="2" startColumn=<%= expectedStart2 %> endLine="2" endColumn=<%= expectedEnd2 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "         Dim ".Length + 1
            Dim expectedEnd1 = "         Dim F".Length + 1

            Dim expectedStart2 = "         Dim F, ".Length + 1
            Dim expectedEnd2 = "         Dim F, G".Length + 1

            Dim expected =
<sequencePoints>
    <entry startLine="2" startColumn="5" endLine="2" endColumn="10"/>
    <entry startLine="3" startColumn=<%= expectedStart1 %> endLine="3" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="3" startColumn=<%= expectedStart2 %> endLine="3" endColumn=<%= expectedEnd2 %>/>
    <entry startLine="4" startColumn="5" endLine="4" endColumn="12"/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub FieldAsNewSingleInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F As New C()
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F As New C()".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="2" startColumn=<%= expectedStart1 %> endLine="2" endColumn=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F As New C()".Length + 1

            Dim expected =
<sequencePoints>
    <entry startLine="2" startColumn="5" endLine="2" endColumn="10"/>
    <entry startLine="3" startColumn=<%= expectedStart1 %> endLine="3" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="4" startColumn="5" endLine="4" endColumn="12"/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub FieldInitializer()
            Dim source =
<compilation>
    <file>
Class C
    Dim F = 1
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C..ctor"))

            Dim expectedStart1 = "    Dim ".Length + 1
            Dim expectedEnd1 = "    Dim F = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry/>
    <entry startLine="2" startColumn=<%= expectedStart1 %> endLine="2" endColumn=<%= expectedEnd1 %>/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.DebugDll)
            compilation.VerifyDiagnostics()

            Dim actual = GetSequencePoints(GetPdbXml(compilation, "C.M"))

            Dim expectedStart1 = "        Dim ".Length + 1
            Dim expectedEnd1 = "        Dim F = 1".Length + 1

            Dim expected =
<sequencePoints>
    <entry startLine="2" startColumn="5" endLine="2" endColumn="10"/>
    <entry startLine="3" startColumn=<%= expectedStart1 %> endLine="3" endColumn=<%= expectedEnd1 %>/>
    <entry startLine="4" startColumn="5" endLine="4" endColumn="12"/>
</sequencePoints>

            AssertXml.Equal(expected, actual)
        End Sub
    End Class
End Namespace
