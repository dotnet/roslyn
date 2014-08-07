' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBUsingTests
        Inherits BasicTestBase

        <Fact>
        Public Sub UsingNested()
            Dim source =
<compilation>
    <file>
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("Inside Dispose.")
    End Sub
End Class

Class C1
    Public Shared Sub Main()

        Using foo1 As New MyDisposable(), foo2 As New MyDisposable(), foo3 As MyDisposable = Nothing
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                    source,
                    TestOptions.DebugExe)

            Dim actual = PDBTests.GetPdbXml(compilation, "C1.Main")

            Dim expected = <symbols>
                               <entryPoint declaringType="C1" methodName="Main" parameterNames=""/>
                               <methods>
                                   <method containingType="C1" name="Main" parameterNames="">
                                       <sequencepoints total="13">
                                           <entry il_offset="0x0" start_row="16" start_column="5" end_row="16" end_column="29" file_ref="0"/>
                                           <entry il_offset="0x1" start_row="18" start_column="9" end_row="18" end_column="101" file_ref="0"/>
                                           <entry il_offset="0x2" start_row="18" start_column="15" end_row="18" end_column="41" file_ref="0"/>
                                           <entry il_offset="0x8" start_row="18" start_column="43" end_row="18" end_column="69" file_ref="0"/>
                                           <entry il_offset="0xe" start_row="18" start_column="71" end_row="18" end_column="101" file_ref="0"/>
                                           <entry il_offset="0x10" start_row="19" start_column="13" end_row="19" end_column="47" file_ref="0"/>
                                           <entry il_offset="0x1b" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x1d" start_row="20" start_column="9" end_row="20" end_column="18" file_ref="0"/>
                                           <entry il_offset="0x2e" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x30" start_row="20" start_column="9" end_row="20" end_column="18" file_ref="0"/>
                                           <entry il_offset="0x41" hidden="true" start_row="16707566" start_column="0" end_row="16707566" end_column="0" file_ref="0"/>
                                           <entry il_offset="0x43" start_row="20" start_column="9" end_row="20" end_column="18" file_ref="0"/>
                                           <entry il_offset="0x54" start_row="21" start_column="5" end_row="21" end_column="12" file_ref="0"/>
                                       </sequencepoints>
                                       <locals>
                                           <local name="foo1" il_index="0" il_start="0x2" il_end="0x53" attributes="0"/>
                                           <local name="foo2" il_index="1" il_start="0x2" il_end="0x53" attributes="0"/>
                                           <local name="foo3" il_index="2" il_start="0x2" il_end="0x53" attributes="0"/>
                                       </locals>
                                       <scope startOffset="0x0" endOffset="0x55">
                                           <importsforward declaringType="MyDisposable" methodName="Dispose" parameterNames=""/>
                                           <scope startOffset="0x2" endOffset="0x53">
                                               <local name="foo1" il_index="0" il_start="0x2" il_end="0x53" attributes="0"/>
                                               <local name="foo2" il_index="1" il_start="0x2" il_end="0x53" attributes="0"/>
                                               <local name="foo3" il_index="2" il_start="0x2" il_end="0x53" attributes="0"/>
                                           </scope>
                                       </scope>
                                   </method>
                               </methods>
                           </symbols>

            PDBTests.AssertXmlEqual(expected, actual)
        End Sub

        <Fact>
        Public Sub UsingExpression()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using (New DisposableObject())
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry start_row="4" start_column="5" end_row="4" end_column="15"/>
                               <entry start_row="5" start_column="9" end_row="5" end_column="39"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="7" start_column="5" end_row="7" end_column="12"/>
                           </sequencePoints>

            AssertXmlEqual(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <Fact>
        Public Sub UsingVariableDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry start_row="4" start_column="5" end_row="4" end_column="15"/>
                               <entry start_row="5" start_column="9" end_row="5" end_column="42"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="7" start_column="5" end_row="7" end_column="12"/>
                           </sequencePoints>

            AssertXmlEqual(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <Fact>
        Public Sub UsingMultipleVariableDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1 As New DisposableObject(), v2 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry start_row="4" start_column="5" end_row="4" end_column="15"/>
                               <entry start_row="5" start_column="9" end_row="5" end_column="73"/>
                               <entry start_row="5" start_column="15" end_row="5" end_column="43"/>
                               <entry start_row="5" start_column="45" end_row="5" end_column="73"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="7" start_column="5" end_row="7" end_column="12"/>
                           </sequencePoints>

            AssertXmlEqual(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <Fact>
        Public Sub UsingVariableAsNewDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1, v2 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry start_row="4" start_column="5" end_row="4" end_column="15"/>
                               <entry start_row="5" start_column="9" end_row="5" end_column="47"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="7" start_column="5" end_row="7" end_column="12"/>
                           </sequencePoints>

            AssertXmlEqual(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub

        <Fact>
        Public Sub UsingMultipleVariableAsNewDeclaration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class C1
    Sub Main()
        Using v1, v2 As New DisposableObject(), v3, v4 As New DisposableObject()
        End Using
    End Sub
End Class

Class DisposableObject
    Implements IDisposable
    
    Sub New()
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class 
    </file>
</compilation>

            Dim expected = <sequencePoints>
                               <entry start_row="4" start_column="5" end_row="4" end_column="15"/>
                               <entry start_row="5" start_column="9" end_row="5" end_column="81"/>
                               <entry start_row="5" start_column="15" end_row="5" end_column="47"/>
                               <entry start_row="5" start_column="15" end_row="5" end_column="47"/>
                               <entry start_row="5" start_column="49" end_row="5" end_column="81"/>
                               <entry start_row="5" start_column="49" end_row="5" end_column="81"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="16707566" start_column="0" end_row="16707566" end_column="0"/>
                               <entry start_row="6" start_column="9" end_row="6" end_column="18"/>
                               <entry start_row="7" start_column="5" end_row="7" end_column="12"/>
                           </sequencePoints>

            AssertXmlEqual(expected, GetSequencePoints(GetPdbXml(source, TestOptions.DebugDll, "C1.Main")))
        End Sub
    End Class
End Namespace
