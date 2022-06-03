' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_StructLayout
        Inherits BasicTestBase

        <Fact>
        Public Sub Pack()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=0)>
Class Pack0
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=1)>
Class Pack1
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=2)>
Class Pack2
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=4)>
Class Pack4
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=8)>
Class Pack8
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=16)>
Class Pack16
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=32)>
Class Pack32
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=64)>
Class Pack64
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=128)>
Class Pack128
End Class
]]>
    </file>
</compilation>

            Const typeDefMask As TypeAttributes = TypeAttributes.StringFormatMask Or TypeAttributes.LayoutMask

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(9, reader.GetTableRowCount(TableIndex.ClassLayout))

                    For Each typeHandle In reader.TypeDefinitions
                        Dim type = reader.GetTypeDefinition(typeHandle)

                        If type.GetLayout().IsDefault Then
                            Continue For
                        End If

                        Assert.Equal(TypeAttributes.SequentialLayout, type.Attributes And typeDefMask)
                        Dim typeName As String = reader.GetString(type.Name)

                        Dim expectedAlignment As Integer = Integer.Parse(typeName.Substring("Pack".Length))
                        Assert.Equal(expectedAlignment, type.GetLayout().PackingSize)
                        Assert.Equal(1, type.GetLayout().Size)
                    Next
                End Sub)
        End Sub

        <Fact>
        Public Sub SizeAndPack()
            Dim verifiable =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class Classes
    <StructLayout(LayoutKind.Explicit)>
    Class E
    End Class

    <StructLayout(LayoutKind.Explicit, Size:=0)>
    Class E_S0
    End Class

    <StructLayout(LayoutKind.Explicit, Size:=1)>
    Class E_S1
    End Class

    <StructLayout(LayoutKind.Explicit, Pack:=0)>
    Class E_P0
    End Class

    <StructLayout(LayoutKind.Explicit, Pack:=1)>
    Class E_P1
    End Class

    <StructLayout(LayoutKind.Explicit, Pack:=0, Size:=0)>
    Class E_P0_S0
    End Class

    <StructLayout(LayoutKind.Explicit, Pack:=1, Size:=10)>
    Class E_P1_S10
    End Class

    <StructLayout(LayoutKind.Sequential)>
    Class Q
    End Class

    <StructLayout(LayoutKind.Sequential, Size:=0)>
    Class Q_S0
    End Class

    <StructLayout(LayoutKind.Sequential, Size:=1)>
    Class Q_S1
    End Class

    <StructLayout(LayoutKind.Sequential, Pack:=0)>
    Class Q_P0
    End Class

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Class Q_P1
    End Class

    <StructLayout(LayoutKind.Sequential, Pack:=0, Size:=0)>
    Class Q_P0_S0
    End Class

    <StructLayout(LayoutKind.Sequential, Pack:=1, Size:=10)>
    Class Q_P1_S10
    End Class

    <StructLayout(LayoutKind.Auto)>
    Class A
    End Class
End Class

Class Structs
    <StructLayout(LayoutKind.Explicit)>
    Structure E
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Size:=0)>
    Structure E_S0
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Size:=1)>
    Structure E_S1
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Pack:=0)>
    Structure E_P0
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Pack:=1)>
    Structure E_P1
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Pack:=0, Size:=0)>
    Structure E_P0_S0
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Explicit, Pack:=1, Size:=10)>
    Structure E_P1_S10
        <FieldOffset(0)>
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Structure Q
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Size:=0)>
    Structure Q_S0
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Size:=1)>
    Structure Q_S1
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=0)>
    Structure Q_P0
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Structure Q_P1
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=0, Size:=0)>
    Structure Q_P0_S0
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1, Size:=10)>
    Structure Q_P1_S10
        Dim a As Integer
    End Structure

    <StructLayout(LayoutKind.Auto)>
    Structure A
        Dim a As Integer
    End Structure
End Class
]]>
    </file>
</compilation>

            ' peverify reports errors, but the types can be loaded and used:
            Dim unverifiable =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class Classes
    <StructLayout(LayoutKind.Auto, Size:=0)>
    Class A_S0
    End Class

    <StructLayout(LayoutKind.Auto, Size:=1)>
    Class A_S1
    End Class

    <StructLayout(LayoutKind.Auto, Pack:=0)>
    Class A_P0
    End Class

    <StructLayout(LayoutKind.Auto, Pack:=1)>
    Class A_P1
    End Class

    <StructLayout(LayoutKind.Auto, Pack:=0, Size:=0)>
    Class A_P0_S0
    End Class

    <StructLayout(LayoutKind.Auto, Pack:=1, Size:=10)>
    Class A_P1_S10
    End Class
End Class

Class Structs
    <StructLayout(LayoutKind.Auto, Size:=0)>
    Structure A_S0
    End Structure

    <StructLayout(LayoutKind.Auto, Size:=1)>
    Structure A_S1
    End Structure

    <StructLayout(LayoutKind.Auto, Pack:=0)>
    Structure A_P0_S1  ' this is different from C#, which emits size = 0
    End Structure

    <StructLayout(LayoutKind.Auto, Pack:=1)>
    Structure A_P1_S1  ' this is different from C#, which emits size = 0
    End Structure

    <StructLayout(LayoutKind.Auto, Pack:=0, Size:=0)>
    Structure A_P0_S0
    End Structure

    <StructLayout(LayoutKind.Auto, Pack:=1, Size:=10)>
    Structure A_P1_S10
    End Structure
End Class
]]>
    </file>
</compilation>

            ' types can't be loaded as they are too big:
            Dim unloadable =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class Classes
    <StructLayout(LayoutKind.Auto, Pack:=1, Size:=Int32.MaxValue)>
    Class A_P1_S2147483647
    End Class

    <StructLayout(LayoutKind.Sequential, Pack:=1, Size:=Int32.MaxValue)>
    Class Q_P1_S2147483647
    End Class

    <StructLayout(LayoutKind.Explicit, Pack:=1, Size:=Int32.MaxValue)>
    Class E_P1_S2147483647
    End Class
End Class

Class Structs
    <StructLayout(LayoutKind.Auto, Pack:=1, Size:=Int32.MaxValue)>
    Structure A_P1_S2147483647
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1, Size:=Int32.MaxValue)>
    Structure Q_P1_S2147483647
    End Structure

    <StructLayout(LayoutKind.Explicit, Pack:=1, Size:=Int32.MaxValue)>
    Structure E_P1_S2147483647
    End Structure
End Class
]]>
    </file>
</compilation>

            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    For Each typeHandle In reader.TypeDefinitions
                        Dim type = reader.GetTypeDefinition(typeHandle)

                        Dim layout = type.GetLayout()
                        If layout.IsDefault Then
                            Continue For
                        End If

                        Dim isValueType As Boolean = (type.Attributes And TypeAttributes.Sealed) <> 0
                        Dim typeName As String = reader.GetString(type.Name)

                        Dim expectedSize As UInteger = 0
                        Dim expectedPack As UShort = 0
                        Dim expectedKind As TypeAttributes = TypeAttributes.AutoLayout

                        If typeName <> "Structs" AndAlso typeName <> "Classes" Then
                            For Each part In typeName.Split("_"c)
                                Select Case part(0)
                                    Case "A"c
                                        expectedKind = TypeAttributes.AutoLayout

                                    Case "E"c
                                        expectedKind = TypeAttributes.ExplicitLayout

                                    Case "Q"c
                                        expectedKind = TypeAttributes.SequentialLayout

                                    Case "P"c
                                        expectedPack = UShort.Parse(part.Substring(1))

                                    Case "S"c
                                        expectedSize = UInteger.Parse(part.Substring(1))
                                End Select
                            Next
                        End If

                        Assert.False(expectedPack = 0 AndAlso expectedSize = 0, "Either expectedPack or expectedSize should be non-zero")
                        Assert.Equal(CInt(expectedPack), layout.PackingSize)
                        Assert.Equal(CInt(expectedSize), layout.Size)
                        Assert.Equal(expectedKind, type.Attributes And TypeAttributes.LayoutMask)
                    Next
                End Sub

            CompileAndVerify(verifiable, validator:=validator)
            CompileAndVerify(unverifiable, validator:=validator, verify:=Verification.FailsPEVerify)
            CompileAndVerify(unloadable, validator:=validator, verify:=Verification.FailsPEVerify)
        End Sub

        <Fact>
        Public Sub Pack_Errors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=-1)>
Class PM1
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=3)>
Class P3
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=5)>
Class P5
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=6)>
Class P6
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=256)>
Class P256
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=512)>
Class P512
End Class

<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=Int32.MaxValue)>
Class PMax
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=-1)>
                                              ~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=3)>
                                              ~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=5)>
                                              ~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=6)>
                                              ~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=256)>
                                              ~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=512)>
                                              ~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=1, Pack:=Int32.MaxValue)>
                                              ~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub Size_Errors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential, Size:=-1)>
Class S
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, Size:=-1)>
                                     ~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub LayoutAndCharSet_Errors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(DirectCast((-1), LayoutKind), CharSet:=CharSet.Ansi)>
Public Class C1
End Class

<StructLayout(DirectCast(4, LayoutKind), CharSet:=CharSet.Ansi)>
Public Class C2
End Class

<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast((-1), CharSet))>
Public Class C3
End Class

<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast(5, CharSet))>
Public Class C4
End Class

<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast(Int32.MaxValue, CharSet))>
Public Class C5
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(DirectCast((-1), LayoutKind), CharSet:=CharSet.Ansi)>
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(DirectCast(4, LayoutKind), CharSet:=CharSet.Ansi)>
              ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast((-1), CharSet))>
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast(5, CharSet))>
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, CharSet:=DirectCast(Int32.MaxValue, CharSet))>
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        ''' <summary> 
        ''' CLI spec (22.8 ClassLayout): 
        '''  "A type has layout if it is marked SequentialLayout or ExplicitLayout.   
        '''   If any type within an inheritance chain has layout, then so shall all its base classes,    
        '''   up to the one that descends immediately from System.ValueType (if it exists in the type's hierarchy);    
        '''   otherwise, from System.Object."    
        ''' 
        ''' But this rule is only enforced by the loader, not by the compiler. 
        ''' TODO: should we report an error? 
        ''' </summary>        
        <Fact>
        Public Sub Inheritance()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Sequential)>
Public Class A
    Public a As Integer, b As Integer
End Class

Public Class B
    Inherits A 
    Public c As Integer, d As Integer
End Class

<StructLayout(LayoutKind.Sequential)>
Public Class C
    Inherits B
    Public e As Integer, f As Integer
End Class
]]>
    </file>
</compilation>
            ' type C can't be loaded
            CompileAndVerify(source, verify:=Verification.FailsPEVerify)
        End Sub

        <Fact>
        Public Sub ExplicitFieldLayout()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Explicit)>
Public Class A
    <FieldOffset(4)>
    Dim a As Integer

    <FieldOffset(8)>
    WithEvents b As A

    ' FieldOffset can't be applied on Property or Event backing fields
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(2, reader.GetTableRowCount(TableIndex.FieldLayout))

                    For Each fieldHandle In reader.FieldDefinitions
                        Dim field = reader.GetFieldDefinition(fieldHandle)
                        Dim name = reader.GetString(field.Name)

                        Dim expectedOffset As Integer
                        Select Case name
                            Case "a"
                                expectedOffset = 4

                            Case "_b"
                                expectedOffset = 8

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select

                        Assert.Equal(expectedOffset, field.GetOffset())
                    Next
                End Sub)
        End Sub

        ''' <summary> 
        ''' CLI spec (22.16 FieldLayout):  
        ''' - Offset shall be zero or more.  
        ''' - The Type whose Fields are described by each row of the FieldLayout table shall have Flags.ExplicitLayout.  
        ''' - Flags.Static for the row in the Field table indexed by Field shall be non-static  
        ''' - Every Field of an ExplicitLayout Type shall be given an offset; that is, it shall have a row in the FieldLayout table 
        ''' 
        ''' Dev11 VB checks only the first rule.
        ''' </summary>        
        <Fact>
        Public Sub ExplicitFieldLayout_Errors()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Runtime.InteropServices

<StructLayout(LayoutKind.Auto)>
Public Class A
    <FieldOffset(4)>
    Dim a As Integer
End Class

<StructLayout(LayoutKind.Sequential)>
Public Class S
    <FieldOffset(4)>
    Dim a As Integer

    <FieldOffset(-1)>
    Dim b As Integer
End Class

<StructLayout(LayoutKind.Explicit)>
Public Class E
    <FieldOffset(-1)>
    Dim a As Integer

    <FieldOffset(5)>
    Shared b As Integer

    Dim c1 As Integer, c2 As Integer
    Shared d As Integer
    Const e As Integer = 3

    <FieldOffset(10)>
    Dim f As Object

    <FieldOffset(-1)>
    Shared g As Integer

    <FieldOffset(5)>
    Const h As Integer = 1
End Class

Enum En
    <FieldOffset(5)>
    A = 1
End Enum
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'FieldOffsetAttribute' is not valid: Incorrect argument value.
    <FieldOffset(-1)>
                 ~~
BC30127: Attribute 'FieldOffsetAttribute' is not valid: Incorrect argument value.
    <FieldOffset(-1)>
                 ~~
BC30127: Attribute 'FieldOffsetAttribute' is not valid: Incorrect argument value.
    <FieldOffset(-1)>
                 ~~
]]>)
        End Sub

        <Fact>
        Public Sub ReadingFromMetadata()
            Using [module] = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.Invalid.ClassLayout)
                Dim reader = [module].Module.GetMetadataReader()

                For Each typeHandle In reader.TypeDefinitions
                    Dim type = reader.GetTypeDefinition(typeHandle)
                    Dim name = reader.GetString(type.Name)

                    Dim classSize As UInteger = 0, packingSize As UInteger = 0

                    Dim badLayout = False
                    Dim mdLayout As System.Reflection.Metadata.TypeLayout
                    Try
                        mdLayout = type.GetLayout()
                    Catch ex As BadImageFormatException
                        badLayout = True
                        mdLayout = Nothing
                    End Try

                    Dim hasClassLayout = Not mdLayout.IsDefault
                    Dim layout As TypeLayout = [module].Module.GetTypeLayout(typeHandle)

                    Select Case name
                        Case "<Module>"
                            Assert.False(hasClassLayout)
                            Assert.Equal(Nothing, layout)
                            Assert.False(badLayout)

                        Case "S1"
                        Case "S2"
                            ' invalid size/pack value
                            Assert.False(hasClassLayout)
                            Assert.True(badLayout)

                        Case "S3"
                            Assert.True(hasClassLayout)
                            Assert.Equal(1, mdLayout.Size)
                            Assert.Equal(2, mdLayout.PackingSize)
                            Assert.Equal(New TypeLayout(LayoutKind.Sequential, size:=1, alignment:=2), layout)
                            Assert.False(badLayout)

                        Case "S4"
                            Assert.True(hasClassLayout)
                            Assert.Equal(&H12345678, mdLayout.Size)
                            Assert.Equal(0, mdLayout.PackingSize)
                            Assert.Equal(New TypeLayout(LayoutKind.Sequential, size:=&H12345678, alignment:=0), layout)
                            Assert.False(badLayout)

                        Case "S5"
                            ' doesn't have layout
                            Assert.False(hasClassLayout)
                            Assert.Equal(New TypeLayout(LayoutKind.Sequential, size:=0, alignment:=0), layout)
                            Assert.False(badLayout)

                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(name)
                    End Select
                Next

            End Using
        End Sub

        Private Sub VerifyStructLayout(source As System.Xml.Linq.XElement, hasInstanceFields As Boolean)
            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Dim type = reader.TypeDefinitions _
                        .Select(Function(handle) reader.GetTypeDefinition(handle)) _
                        .Where(Function(typeDef) reader.GetString(typeDef.Name) = "S") _
                        .Single()

                    Dim layout = type.GetLayout()
                    If Not hasInstanceFields Then
                        Const typeDefMask As TypeAttributes = TypeAttributes.StringFormatMask Or TypeAttributes.LayoutMask

                        Assert.False(layout.IsDefault)
                        Assert.Equal(TypeAttributes.SequentialLayout, type.Attributes And typeDefMask)
                        Assert.Equal(0, layout.PackingSize)
                        Assert.Equal(1, layout.Size)
                    Else
                        Assert.True(layout.IsDefault)
                    End If
                End Sub)
        End Sub

        <Fact>
        Public Sub Bug1075326()
            ' no instance fields
            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Shared f As Integer
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Shared Property P As Integer
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    ReadOnly Property P As Integer
        Get
            Return 0
        End Get
    End Property
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Shared ReadOnly Property P As Integer
        Get
            Return 0
        End Get
    End Property
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Shared Event D()
End Structure
]]></file></compilation>, hasInstanceFields:=False)

            ' instance fields
            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Private f As Integer
End Structure
]]></file></compilation>, hasInstanceFields:=True)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Property P As Integer
End Structure
]]></file></compilation>, hasInstanceFields:=True)

            VerifyStructLayout(<compilation><file><![CDATA[
Structure S
    Event D()
End Structure
]]></file></compilation>, hasInstanceFields:=True)
        End Sub
    End Class
End Namespace
