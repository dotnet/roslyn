' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Public Class EditAndContinuePdbTests
        Inherits EditAndContinueTestBase

        <ConditionalTheory(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        <MemberData(NameOf(ExternalPdbFormats))>
        Public Sub MethodExtents(format As DebugInformationFormat)
            Dim source0 = MarkedSource("
Imports System

Public Class C
    Function A() As Integer 
        Return 1
    End Function
                                   
    Sub F()              
#ExternalSource(""C:\F\A.vb"", 10)  
        Console.WriteLine()
#End ExternalSource
#ExternalSource(""C:\F\B.vb"", 20)  
        Console.WriteLine()
#End ExternalSource
    End Sub

    Function E() As Integer 
        Return 1
    End Function

#ExternalSource(""C:\Enc1.vb"", 20) 
    Sub G()
        Dim H1 = <N:0>Function() 1</N:0>

        Dim H2 = <N:1>Sub()
            Dim H3 = <N:2>Function() 3</N:2>
        End Sub</N:1>
    End Sub
#End ExternalSource 
End Class                           
", fileName:="C:\Enc1.vb")

            Dim source1 = MarkedSource("
Imports System

Public Class C
    Function A() As Integer 
        Return 1
    End Function
                                   
    Sub F()              
#ExternalSource(""C:\F\A.vb"", 10)  
        Console.WriteLine()
#End ExternalSource
#ExternalSource(""C:\F\C.vb"", 10)  
        Console.WriteLine()
#End ExternalSource
    End Sub

#ExternalSource(""C:\Enc1.vb"", 20) 
    Sub G()
        Dim H1 = <N:0>Function() 1</N:0>

        Dim H2 = <N:1>Sub()
            Dim H3 = <N:2>Function() 3</N:2>
            Dim H4 = <N:3>Function() 4</N:3>
        End Sub</N:1>
    End Sub
#End ExternalSource 

    Function E() As Integer 
        Return 1
    End Function
End Class", fileName:="C:\Enc1.vb")

            Dim source2 = MarkedSource("
Imports System

Public Class C
    Function A() As Integer 
        Return 3
    End Function
                                   
    Sub F()              
#ExternalSource(""C:\F\A.vb"", 10)  
        Console.WriteLine()
#End ExternalSource
#ExternalSource(""C:\F\E.vb"", 10)  
        Console.WriteLine()
#End ExternalSource
    End Sub

#ExternalSource(""C:\Enc1.vb"", 20) 
    Sub G()


        Dim H2 = <N:1>Sub()
            
            Dim H4 = <N:3>Function() 4</N:3>
        End Sub</N:1>
    End Sub
#End ExternalSource

    Function E() As Integer 
        Return 1
    End Function

    Function B() As Integer 
        Return 4
    End Function
End Class", fileName:="C:\Enc1.vb")

            Dim compilation0 = CreateCompilationWithMscorlib40({source0.Tree}, options:=ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName:="EncMethodExtents")
            Dim compilation1 = compilation0.WithSource(source1.Tree)
            Dim compilation2 = compilation1.WithSource(source2.Tree)

            compilation0.VerifyDiagnostics()
            compilation1.VerifyDiagnostics()
            compilation2.VerifyDiagnostics()

            Dim v0 = CompileAndVerify(compilation0, emitOptions:=EmitOptions.Default.WithDebugInformationFormat(format))
            Dim md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData)

            Dim f0 = compilation0.GetMember(Of MethodSymbol)("C.F")
            Dim f1 = compilation1.GetMember(Of MethodSymbol)("C.F")
            Dim f2 = compilation2.GetMember(Of MethodSymbol)("C.F")

            Dim g0 = compilation0.GetMember(Of MethodSymbol)("C.G")
            Dim g1 = compilation1.GetMember(Of MethodSymbol)("C.G")
            Dim g2 = compilation2.GetMember(Of MethodSymbol)("C.G")

            Dim a1 = compilation1.GetMember(Of MethodSymbol)("C.A")
            Dim a2 = compilation2.GetMember(Of MethodSymbol)("C.A")

            Dim b2 = compilation2.GetMember(Of MethodSymbol)("C.B")

            Dim generation0 = EmitBaseline.CreateInitialBaseline(md0, AddressOf v0.CreateSymReader().GetEncMethodDebugInfo)

            Dim syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1)

            Dim diff1 = compilation1.EmitDifference(generation0,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f0, f1, syntaxMap1, preserveLocalVariables:=True),
                                      New SemanticEdit(SemanticEditKind.Update, g0, g1, syntaxMap1, preserveLocalVariables:=True)))
            diff1.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I4-0, $I4-2, $I4-3#1, $I4-1, _Lambda$__4-0, _Lambda$__4-1, _Lambda$__4-2, _Lambda$__4-3#1}")

            Dim reader1 = diff1.GetMetadata().Reader

            CheckEncLogDefinitions(reader1,
               Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
               Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
               Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
               Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddField),
               Row(5, TableIndex.Field, EditAndContinueOperation.Default),
               Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
               Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
               Row(16, TableIndex.MethodDef, EditAndContinueOperation.Default),
               Row(17, TableIndex.MethodDef, EditAndContinueOperation.Default),
               Row(18, TableIndex.MethodDef, EditAndContinueOperation.Default),
               Row(5, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
               Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default))

            If format = DebugInformationFormat.PortablePdb Then
                Using pdbProvider = MetadataReaderProvider.FromPortablePdbImage(diff1.PdbDelta)
                    CheckEncMap(pdbProvider.GetMetadataReader(),
                        Handle(11, TableIndex.MethodDebugInformation),
                        Handle(13, TableIndex.MethodDebugInformation),
                        Handle(16, TableIndex.MethodDebugInformation),
                        Handle(17, TableIndex.MethodDebugInformation),
                        Handle(18, TableIndex.MethodDebugInformation),
                        Handle(19, TableIndex.MethodDebugInformation))
                End Using
            End If

            diff1.VerifyPdb(Enumerable.Range(&H6000001, 20),
<symbols>
    <files>
        <file id="1" name="C:\F\A.vb" language="VB"/>
        <file id="2" name="C:\F\C.vb" language="VB"/>
        <file id="3" name="C:\Enc1.vb" language="VB" checksumAlgorithm="SHA1" checksum="E2-3A-75-D7-B2-2D-78-1C-0E-F7-75-E2-8C-09-4B-4E-E1-68-2E-9D"/>
    </files>
    <methods>
        <method token="0x600000b">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="10" startColumn="9" endLine="10" endColumn="28" document="1"/>
                <entry offset="0x7" startLine="10" startColumn="9" endLine="10" endColumn="28" document="2"/>
                <entry offset="0xd" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method token="0x600000d">
            <sequencePoints>
                <entry offset="0x0" startLine="20" startColumn="5" endLine="20" endColumn="12" document="3"/>
                <entry offset="0x1" startLine="21" startColumn="13" endLine="21" endColumn="35" document="3"/>
                <entry offset="0x26" startLine="23" startColumn="13" endLine="26" endColumn="16" document="3"/>
                <entry offset="0x4b" startLine="27" startColumn="5" endLine="27" endColumn="12" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4c">
                <importsforward token="0x600000b"/>
                <local name="H1" il_index="2" il_start="0x0" il_end="0x4c" attributes="0"/>
                <local name="H2" il_index="3" il_start="0x0" il_end="0x4c" attributes="0"/>
            </scope>
        </method>
        <method token="0x6000010">
            <sequencePoints>
                <entry offset="0x0" startLine="21" startColumn="23" endLine="21" endColumn="33" document="3"/>
                <entry offset="0x1" startLine="21" startColumn="34" endLine="21" endColumn="35" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward token="0x600000b"/>
            </scope>
        </method>
        <method token="0x6000011">
            <sequencePoints>
                <entry offset="0x0" startLine="23" startColumn="23" endLine="23" endColumn="28" document="3"/>
                <entry offset="0x1" startLine="24" startColumn="17" endLine="24" endColumn="39" document="3"/>
                <entry offset="0x26" startLine="25" startColumn="17" endLine="25" endColumn="39" document="3"/>
                <entry offset="0x4b" startLine="26" startColumn="9" endLine="26" endColumn="16" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x4c">
                <importsforward token="0x600000b"/>
                <local name="H3" il_index="0" il_start="0x0" il_end="0x4c" attributes="0"/>
                <local name="H4" il_index="1" il_start="0x0" il_end="0x4c" attributes="0"/>
            </scope>
        </method>
        <method token="0x6000012">
            <sequencePoints>
                <entry offset="0x0" startLine="24" startColumn="27" endLine="24" endColumn="37" document="3"/>
                <entry offset="0x1" startLine="24" startColumn="38" endLine="24" endColumn="39" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward token="0x600000b"/>
            </scope>
        </method>
        <method token="0x6000013">
            <sequencePoints>
                <entry offset="0x0" startLine="25" startColumn="27" endLine="25" endColumn="37" document="3"/>
                <entry offset="0x1" startLine="25" startColumn="38" endLine="25" endColumn="39" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward token="0x600000b"/>
            </scope>
        </method>
    </methods>
</symbols>)

            Dim syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2)
            Dim diff2 = compilation2.EmitDifference(diff1.NextGeneration,
                ImmutableArray.Create(New SemanticEdit(SemanticEditKind.Update, f1, f2, syntaxMap2, preserveLocalVariables:=True),
                                      New SemanticEdit(SemanticEditKind.Update, g1, g2, syntaxMap2, preserveLocalVariables:=True),
                                      New SemanticEdit(SemanticEditKind.Update, a1, a2, syntaxMap2, preserveLocalVariables:=True),
                                      New SemanticEdit(SemanticEditKind.Insert, Nothing, b2)))

            diff2.VerifySynthesizedMembers(
                "C: {_Closure$__}",
                "C._Closure$__: {$I4-3#1, $I4-1, _Lambda$__4-1, _Lambda$__4-3#1, $I4-0, $I4-2, _Lambda$__4-0, _Lambda$__4-2}")

            Dim reader2 = diff2.GetMetadata().Reader
            CheckEncLogDefinitions(reader2,
                Row(7, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(8, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(9, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(10, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(13, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(17, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(19, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(20, TableIndex.MethodDef, EditAndContinueOperation.Default))

            If format = DebugInformationFormat.PortablePdb Then
                Using pdbProvider = MetadataReaderProvider.FromPortablePdbImage(diff2.PdbDelta)
                    CheckEncMap(pdbProvider.GetMetadataReader(),
                        Handle(10, TableIndex.MethodDebugInformation),
                        Handle(11, TableIndex.MethodDebugInformation),
                        Handle(13, TableIndex.MethodDebugInformation),
                        Handle(17, TableIndex.MethodDebugInformation),
                        Handle(19, TableIndex.MethodDebugInformation),
                        Handle(20, TableIndex.MethodDebugInformation))
                End Using
            End If

            diff2.VerifyPdb(Enumerable.Range(&H6000001, 20),
<symbols>
    <files>
        <file id="1" name="C:\F\A.vb" language="VB"/>
        <file id="2" name="C:\F\E.vb" language="VB"/>
        <file id="3" name="C:\Enc1.vb" language="VB" checksumAlgorithm="SHA1" checksum="DB-81-EA-11-DD-DE-3B-51-F3-07-C3-A7-7E-0B-41-D3-D4-12-86-93"/>
    </files>
    <methods>
        <method token="0x600000b">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" startLine="10" startColumn="9" endLine="10" endColumn="28" document="1"/>
                <entry offset="0x7" startLine="10" startColumn="9" endLine="10" endColumn="28" document="2"/>
                <entry offset="0xd" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0xe">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
        <method token="0x600000d">
            <sequencePoints>
                <entry offset="0x0" startLine="20" startColumn="5" endLine="20" endColumn="12" document="3"/>
                <entry offset="0x1" startLine="23" startColumn="13" endLine="26" endColumn="16" document="3"/>
                <entry offset="0x27" startLine="27" startColumn="5" endLine="27" endColumn="12" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x28">
                <importsforward token="0x600000b"/>
                <local name="H2" il_index="4" il_start="0x0" il_end="0x28" attributes="0"/>
            </scope>
        </method>
        <method token="0x6000011">
            <sequencePoints>
                <entry offset="0x0" startLine="23" startColumn="23" endLine="23" endColumn="28" document="3"/>
                <entry offset="0x1" startLine="25" startColumn="17" endLine="25" endColumn="39" document="3"/>
                <entry offset="0x26" startLine="26" startColumn="9" endLine="26" endColumn="16" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x27">
                <importsforward token="0x600000b"/>
                <local name="H4" il_index="0" il_start="0x0" il_end="0x27" attributes="0"/>
            </scope>
        </method>
        <method token="0x6000013">
            <sequencePoints>
                <entry offset="0x0" startLine="25" startColumn="27" endLine="25" endColumn="37" document="3"/>
                <entry offset="0x1" startLine="25" startColumn="38" endLine="25" endColumn="39" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x7">
                <importsforward token="0x600000b"/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub
    End Class
End Namespace
