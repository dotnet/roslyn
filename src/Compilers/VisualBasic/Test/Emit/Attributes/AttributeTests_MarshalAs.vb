' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class AttributeTests_MarshalAs
        Inherits BasicTestBase

#Region "Helpers"
        Private Sub VerifyFieldMetadataDecoding(verifier As CompilationVerifier, blobs As Dictionary(Of String, Byte()))
            Dim count = 0
            Using assembly = AssemblyMetadata.CreateFromImage(verifier.EmittedAssemblyData)
                Dim c = VisualBasicCompilation.Create("c", syntaxTrees:=New VisualBasicSyntaxTree() {}, references:={assembly.GetReference()})
                For Each typeSym As NamedTypeSymbol In c.GlobalNamespace.GetMembers().Where(Function(s) s.Kind = SymbolKind.NamedType)
                    Dim fields = typeSym.GetMembers().Where(Function(s) s.Kind = SymbolKind.Field)
                    For Each field As FieldSymbol In fields
                        Assert.Null(field.MarshallingInformation)
                        Dim blob = blobs(field.Name)
                        If blob IsNot Nothing AndAlso blob(0) <= &H50 Then
                            Assert.Equal(CType(blob(0), UnmanagedType), field.MarshallingType)
                        Else
                            Assert.Equal(CType(0, UnmanagedType), field.MarshallingType)
                        End If
                        count = count + 1
                    Next
                Next
            End Using

            Assert.True(count > 0, "Expected at least one field")
        End Sub

        Private Sub VerifyParameterMetadataDecoding(verifier As CompilationVerifier, blobs As Dictionary(Of String, Byte()))
            Dim count = 0
            Using assembly = AssemblyMetadata.CreateFromImage(verifier.EmittedAssemblyData)
                Dim c = VisualBasicCompilation.Create("c", syntaxTrees:=New VisualBasicSyntaxTree() {}, references:={assembly.GetReference()})
                For Each typeSym As NamedTypeSymbol In c.GlobalNamespace.GetMembers().Where(Function(s) s.Kind = SymbolKind.NamedType)
                    Dim methods = typeSym.GetMembers().Where(Function(s) s.Kind = SymbolKind.Method)
                    For Each method As MethodSymbol In methods
                        For Each parameter In method.Parameters
                            Assert.Null(parameter.MarshallingInformation)
                            Dim blob = blobs(method.Name & ":" & parameter.Name)
                            If blob IsNot Nothing AndAlso blob(0) <= &H50 Then
                                Assert.Equal(CType(blob(0), UnmanagedType), parameter.MarshallingType)
                            Else
                                Assert.Equal(CType(0, UnmanagedType), parameter.MarshallingType)
                            End If
                            count = count + 1
                        Next
                    Next
                Next
            End Using

            Assert.True(count > 0, "Expected at least one parameter")
        End Sub

#End Region

        ''' <summary> 
        ''' type only, others ignored, field type ignored 
        ''' </summary>       
        <Fact>
        Public Sub SimpleTypes()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X
    <MarshalAs(CShort(0))>
    Public ZeroShort As X

    <MarshalAs(DirectCast(0, UnmanagedType))>
    Public Zero As X

    <MarshalAs(DirectCast(&H1FFFFFFF, UnmanagedType))>
    Public MaxValue As X

    <MarshalAs(DirectCast((&H123456), UnmanagedType))>
    Public _0x123456 As X

    <MarshalAs(DirectCast((&H1000), UnmanagedType))>
    Public _0x1000 As X

    <MarshalAs(UnmanagedType.AnsiBStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public AnsiBStr As X

    <MarshalAs(UnmanagedType.AsAny, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public AsAny As Double

    <MarshalAs(UnmanagedType.Bool, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public Bool As X

    <MarshalAs(UnmanagedType.BStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public BStr As X

    <MarshalAs(UnmanagedType.Currency, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public Currency As Integer

    <MarshalAs(UnmanagedType.[Error], ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public [Error] As Integer

    <MarshalAs(UnmanagedType.FunctionPtr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public FunctionPtr As Integer

    <MarshalAs(UnmanagedType.I1, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public I1 As Integer

    <MarshalAs(UnmanagedType.I2, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public I2 As Integer

    <MarshalAs(UnmanagedType.I4, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public I4 As Integer

    <MarshalAs(UnmanagedType.I8, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public I8 As Integer

    <MarshalAs(UnmanagedType.LPStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public LPStr As Integer

    <MarshalAs(UnmanagedType.LPStruct, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public LPStruct As Integer

    <MarshalAs(UnmanagedType.LPTStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public LPTStr As Integer

    <MarshalAs(UnmanagedType.LPWStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public LPWStr As Integer

    <MarshalAs(UnmanagedType.R4, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public R4 As Integer

    <MarshalAs(UnmanagedType.R8, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public R8 As Integer

    <MarshalAs(UnmanagedType.Struct, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public Struct As Integer

    <MarshalAs(UnmanagedType.SysInt, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public SysInt As Decimal

    <MarshalAs(UnmanagedType.SysUInt, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public SysUInt As Integer()

    <MarshalAs(UnmanagedType.TBStr, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public TBStr As Object()

    <MarshalAs(UnmanagedType.U1, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public U1 As Integer

    <MarshalAs(UnmanagedType.U2, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public U2 As Double

    <MarshalAs(UnmanagedType.U4, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public U4 As Boolean

    <MarshalAs(UnmanagedType.U8, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public U8 As String

    <MarshalAs(UnmanagedType.VariantBool, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public VariantBool As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"ZeroShort", New Byte() {&H0}},
                {"Zero", New Byte() {&H0}},
                {"MaxValue", New Byte() {&HDF, &HFF, &HFF, &HFF}},
                {"_0x1000", New Byte() {&H90, &H0}},
                {"_0x123456", New Byte() {&HC0, &H12, &H34, &H56}},
                {"AnsiBStr", New Byte() {&H23}},
                {"AsAny", New Byte() {&H28}},
                {"Bool", New Byte() {&H2}},
                {"BStr", New Byte() {&H13}},
                {"Currency", New Byte() {&HF}},
                {"Error", New Byte() {&H2D}},
                {"FunctionPtr", New Byte() {&H26}},
                {"I1", New Byte() {&H3}},
                {"I2", New Byte() {&H5}},
                {"I4", New Byte() {&H7}},
                {"I8", New Byte() {&H9}},
                {"LPStr", New Byte() {&H14}},
                {"LPStruct", New Byte() {&H2B}},
                {"LPTStr", New Byte() {&H16}},
                {"LPWStr", New Byte() {&H15}},
                {"R4", New Byte() {&HB}},
                {"R8", New Byte() {&HC}},
                {"Struct", New Byte() {&H1B}},
                {"SysInt", New Byte() {&H1F}},
                {"SysUInt", New Byte() {&H20}},
                {"TBStr", New Byte() {&H24}},
                {"U1", New Byte() {&H4}},
                {"U2", New Byte() {&H6}},
                {"U4", New Byte() {&H8}},
                {"U8", New Byte() {&HA}},
                {"VariantBool", New Byte() {&H25}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub SimpleTypes_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

Class X
    <MarshalAs(CType(-1, UnmanagedType))>
    Dim MinValue_1 As X

    <MarshalAs(CType(&H20000000, UnmanagedType))>
    Dim MaxValue_1 As X
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadAttribute1, "CType(-1, UnmanagedType)").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "CType(&H20000000, UnmanagedType)").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"))
        End Sub

        ''' <summary>
        '''  (type, IidParamIndex), others ignored, field type ignored
        ''' </summary>
        <Fact>
        Public Sub ComInterfaces()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.IDispatch, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=0, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public IDispatch As Byte

    <MarshalAs(UnmanagedType.[Interface], ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public [Interface] As X

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=2, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public IUnknown As X()

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=&H1FFFFFFF, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public MaxValue As Integer

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=&H123456, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public m_123456 As Integer

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=&H1000, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public m_0x1000 As X

    <MarshalAs(UnmanagedType.IDispatch)>
    Public [Default] As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"IDispatch", New Byte() {&H1A, &H0}},
                {"Interface", New Byte() {&H1C, &H1}},
                {"IUnknown", New Byte() {&H19, &H2}},
                {"MaxValue", New Byte() {&H19, &HDF, &HFF, &HFF, &HFF}},
                {"m_123456", New Byte() {&H19, &HC0, &H12, &H34, &H56}},
                {"m_0x1000", New Byte() {&H19, &H90, &H0}},
                {"Default", New Byte() {&H1A}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub ComInterfaces_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

Class X
    <MarshalAs(UnmanagedType.IDispatch, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim IDispatch_MinValue_1 As Integer

    <MarshalAs(UnmanagedType.[Interface], ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim Interface_MinValue_1 As Integer

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim IUnknown_MinValue_1 As Integer

    <MarshalAs(UnmanagedType.IUnknown, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=&H20000000, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim IUnknown_MaxValue_1 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadAttribute1, "IidParameterIndex:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "IidParameterIndex:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "IidParameterIndex:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "IidParameterIndex:=&H20000000").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"))
        End Sub

        ''' <summary>
        '''  (ArraySubType, SizeConst, SizeParamIndex), SafeArraySubType not allowed, others ignored
        '''  </summary>       
        <Fact>
        Public Sub NativeTypeArray()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.LPArray)>
    Public LPArray0 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public LPArray1 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=0, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public LPArray2 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=&H1FFFFFFF, SizeParamIndex:=Short.MaxValue, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public LPArray3 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=DirectCast(&H50, UnmanagedType))>
    Public LPArray4 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=DirectCast(&H1FFFFFFF, UnmanagedType))>
    Public LPArray5 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=DirectCast(0, UnmanagedType))>
    Public LPArray6 As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"LPArray0", New Byte() {&H2A, &H50}},
                {"LPArray1", New Byte() {&H2A, &H17}},
                {"LPArray2", New Byte() {&H2A, &H17, &H0, &H0, &H0}},
                {"LPArray3", New Byte() {&H2A, &H17, &HC0, &H0, &H7F, &HFF, &HDF, &HFF, &HFF, &HFF, &H1}},
                {"LPArray4", New Byte() {&H2A, &H50}},
                {"LPArray5", New Byte() {&H2A, &HDF, &HFF, &HFF, &HFF}},
                {"LPArray6", New Byte() {&H2A, &H0}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeArray_ElementTypes()
            Dim source As StringBuilder = New StringBuilder(<text>
Imports System
Imports System.Runtime.InteropServices

Class X
</text>.Value)

            Dim expectedBlobs = New Dictionary(Of String, Byte())()
            For i = 0 To SByte.MaxValue
                If i <> DirectCast(UnmanagedType.CustomMarshaler, Integer) Then
                    Dim fldName As String = String.Format("m_{0:X}", i)
                    source.AppendLine(String.Format("<MarshalAs(UnmanagedType.LPArray, ArraySubType := CType(&H{0:X}, UnmanagedType))>Dim {1} As Integer", i, fldName))
                    expectedBlobs.Add(fldName, New Byte() {&H2A, CByte(i)})
                End If
            Next
            source.AppendLine("End Class")
            CompileAndVerifyFieldMarshal(source.ToString(), expectedBlobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeArray_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class X

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim LPArray_e0 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=-1)>
    Dim LPArray_e1 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=0, SizeParamIndex:=-1)>
    Dim LPArray_e2 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=Int32.MaxValue, SizeParamIndex:=Int16.MaxValue)>
    Dim LPArray_e3 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.U8, SizeConst:=Int32.MaxValue / 4 + 1, SizeParamIndex:=Int16.MaxValue)>
    Dim LPArray_e4 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=UnmanagedType.CustomMarshaler)>
    Dim LPArray_e5 As Integer

    <MarshalAs(UnmanagedType.LPArray, SafeArraySubType:=VarEnum.VT_I1)>
    Dim LPArray_e6 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=DirectCast(&H20000000, UnmanagedType))>
    Dim LPArray_e7 As Integer

    <MarshalAs(UnmanagedType.LPArray, ArraySubType:=DirectCast((-1), UnmanagedType))>
    Dim LPArray_e8 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArraySubType:=VarEnum.VT_BSTR"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeParamIndex:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeParamIndex:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=Int32.MaxValue").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=Int32.MaxValue / 4 + 1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ArraySubType:=UnmanagedType.CustomMarshaler").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArraySubType:=VarEnum.VT_I1"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ArraySubType:=DirectCast(&H20000000, UnmanagedType)").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ArraySubType:=DirectCast((-1), UnmanagedType)").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"))
        End Sub

        ''' <summary> 
        ''' (ArraySubType, SizeConst), (SizeParamIndex, SafeArraySubType) not allowed, others ignored 
        ''' </summary>       
        <Fact>
        Public Sub NativeTypeFixedArray()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.ByValArray)>
    Public ByValArray0 As Integer
    
    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public ByValArray1 As Integer
    
    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=0, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public ByValArray2 As Integer
    
    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=(Int32.MaxValue - 3) / 4, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=Nothing)>
    Public ByValArray3 As Integer
    
    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.AsAny)>
    Public ByValArray4 As Integer
    
    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.CustomMarshaler)>
    Public ByValArray5 As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"ByValArray0", New Byte() {&H1E, &H1}},
                {"ByValArray1", New Byte() {&H1E, &H1, &H17}},
                {"ByValArray2", New Byte() {&H1E, &H0, &H17}},
                {"ByValArray3", New Byte() {&H1E, &HDF, &HFF, &HFF, &HFF, &H17}},
                {"ByValArray4", New Byte() {&H1E, &H1, &H28}},
                {"ByValArray5", New Byte() {&H1E, &H1, &H2C}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeFixedArray_ElementTypes()
            Dim source As StringBuilder = New StringBuilder(<text>
Imports System
Imports System.Runtime.InteropServices

Class X
</text>.Value)

            Dim expectedBlobs = New Dictionary(Of String, Byte())()
            Dim i As Integer = 0

            While i < SByte.MaxValue
                Dim fldName As String = String.Format("m_{0:X}", i)
                source.AppendLine(String.Format("<MarshalAs(UnmanagedType.ByValArray, ArraySubType := CType(&H{0:X}, UnmanagedType))>Dim {1} As Integer", i, fldName))
                expectedBlobs.Add(fldName, New Byte() {&H1E, &H1, CByte(i)})
                i = i + 1
            End While

            source.AppendLine("End Class")
            CompileAndVerifyFieldMarshal(source.ToString(), expectedBlobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeFixedArray_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.ByValTStr, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim ByValArray_e1 As Integer

    <MarshalAs(UnmanagedType.ByValArray, SizeParamIndex:=Int16.MaxValue)>
    Dim ByValArray_e2 As Integer

    <MarshalAs(UnmanagedType.ByValArray, SafeArraySubType:=VarEnum.VT_I2)>
    Dim ByValArray_e3 As Integer

    <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.ByValTStr, SizeConst:=&H20000000)>
    Dim ByValArray_e4 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArraySubType:=VarEnum.VT_BSTR"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=-1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=Int16.MaxValue"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArraySubType:=VarEnum.VT_I2"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=&H20000000").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"))
        End Sub

        ''' <summary> 
        ''' (SafeArraySubType, SafeArrayUserDefinedSubType), (ArraySubType, SizeConst, SizeParamIndex) not allowed,
        ''' (SafeArraySubType, SafeArrayUserDefinedSubType) not allowed together unless VT_DISPATCH, VT_UNKNOWN, VT_RECORD; others ignored. 
        ''' </summary>       
        <Fact>
        Public Sub NativeTypeSafeArray()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.SafeArray)>
    Public SafeArray0 As Integer

    <MarshalAs(UnmanagedType.SafeArray, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR)>
    Public SafeArray1 As Integer

    <MarshalAs(UnmanagedType.SafeArray, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArrayUserDefinedSubType:=GetType(X))>
    Public SafeArray2 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType:=Nothing)>
    Public SafeArray3 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType:=GetType(Void))>
    Public SafeArray4 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_EMPTY)>
    Public SafeArray8 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_RECORD, SafeArrayUserDefinedSubType:=GetType(Integer()()))>
    Public SafeArray9 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_RECORD, SafeArrayUserDefinedSubType:=GetType(Nullable(Of)))>
    Public SafeArray10 As Integer
End Class
]]>
    </file>
</compilation>
            Dim aqn = Encoding.ASCII.GetBytes("System.Int32[][], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
            Dim openGenericAqn = Encoding.ASCII.GetBytes("System.Nullable`1, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"SafeArray0", New Byte() {&H1D}},
                {"SafeArray1", New Byte() {&H1D, &H8}},
                {"SafeArray2", New Byte() {&H1D}},
                {"SafeArray3", New Byte() {&H1D}},
                {"SafeArray4", New Byte() {&H1D}},
                {"SafeArray8", New Byte() {&H1D, &H0}},
                {"SafeArray9", New Byte() {&H1D, &H24, CByte(aqn.Length)}.Append(aqn)},
                {"SafeArray10", New Byte() {&H1D, &H24, CByte(openGenericAqn.Length)}.Append(openGenericAqn)}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact>
        Public Sub NativeTypeSafeArray_CCIOnly()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class C(Of T)

    Public Class D(Of S)

        Public Class E

        End Class
    End Class
End Class

Public Class X

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_RECORD, SafeArrayUserDefinedSubType:=GetType(C(Of Integer).D(Of Boolean).E))>
    Public SafeArray11 As Integer
End Class
]]>
    </file>
</compilation>

            Dim nestedAqn = Encoding.ASCII.GetBytes("C`1+D`1+E[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"SafeArray11", New Byte() {&H1D, &H24, &H80, &HC4}.Append(nestedAqn)}
            }

            ' RefEmit has slightly different encoding of the type name
            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeSafeArray_RefEmitDiffers()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType:=GetType(List(Of X)()()))>
    Dim SafeArray5 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_UNKNOWN, SafeArrayUserDefinedSubType:=GetType(X))>
    Dim SafeArray6 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_RECORD, SafeArrayUserDefinedSubType:=GetType(X))>
    Dim SafeArray7 As Integer

End Class
]]>.Value

            Dim e = Encoding.ASCII
            Dim cciBlobs = New Dictionary(Of String, Byte()) From
            {
                {"SafeArray5", New Byte() {&H1D, &H9, &H75}.Append(e.GetBytes("System.Collections.Generic.List`1[X][][], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))},
                {"SafeArray6", New Byte() {&H1D, &HD, &H1, &H58}},
                {"SafeArray7", New Byte() {&H1D, &H24, &H1, &H58}}
            }

            CompileAndVerifyFieldMarshal(source, cciBlobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeSafeArray_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.SafeArray, ArraySubType:=UnmanagedType.ByValTStr, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim SafeArray_e1 As Integer

    <MarshalAs(UnmanagedType.SafeArray, ArraySubType:=UnmanagedType.ByValTStr)>
    Dim SafeArray_e2 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SizeConst:=1)>
    Dim SafeArray_e3 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SizeParamIndex:=1)>
    Dim SafeArray_e4 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing)>
    Dim SafeArray_e5 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType:=Nothing, SafeArraySubType:=VarEnum.VT_BLOB)>
    Dim SafeArray_e6 As Integer

    <MarshalAs(UnmanagedType.SafeArray, SafeArrayUserDefinedSubType:=GetType(Integer), SafeArraySubType:=0)>
    Dim SafeArray_e7 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "ArraySubType:=UnmanagedType.ByValTStr"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeConst:=-1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=-1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType:=Nothing"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "ArraySubType:=UnmanagedType.ByValTStr"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeConst:=1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType:=Nothing"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType:=Nothing"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SafeArrayUserDefinedSubType:=GetType(Integer)"))
        End Sub

        ''' <summary> 
        ''' (SizeConst - required), (SizeParamIndex, ArraySubType) not allowed 
        ''' </summary>
        <Fact>
        Public Sub NativeTypeFixedSysString()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1)>
    Public ByValTStr1 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=&H1FFFFFFF, SafeArrayUserDefinedSubType:=GetType(Integer), IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing)>
    Public ByValTStr2 As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"ByValTStr1", New Byte() {&H17, &H1}},
                {"ByValTStr2", New Byte() {&H17, &HDF, &HFF, &HFF, &HFF}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub NativeTypeFixedSysString_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class X

    <MarshalAs(UnmanagedType.ByValTStr, ArraySubType:=UnmanagedType.ByValTStr, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Dim ByValTStr_e1 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=-1)>
    Dim ByValTStr_e2 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=(Int32.MaxValue - 3) / 4 + 1)>
    Dim ByValTStr_e3 As Integer

    <MarshalAs(UnmanagedType.ByValTStr)>
    Dim ByValTStr_e4 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1, SizeParamIndex:=1)>
    Dim ByValTStr_e5 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1, ArraySubType:=UnmanagedType.ByValTStr)>
    Dim ByValTStr_e6 As Integer

    <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1, SafeArraySubType:=VarEnum.VT_BSTR)>
    Dim ByValTStr_e7 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "ArraySubType:=UnmanagedType.ByValTStr"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=-1"),
                Diagnostic(ERRID.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=-1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                Diagnostic(ERRID.ERR_BadAttribute1, "SizeConst:=(Int32.MaxValue - 3) / 4 + 1").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_AttributeParameterRequired1, "MarshalAs").WithArguments("SizeConst"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "SizeParamIndex:=1"),
                Diagnostic(ERRID.ERR_ParameterNotValidForType, "ArraySubType:=UnmanagedType.ByValTStr"))
        End Sub

        ''' <summary> 
        ''' Custom (MarshalType, MarshalTypeRef, MarshalCookie) one of {MarshalType, MarshalTypeRef} required, others ignored 
        ''' </summary>        
        <Fact>
        Public Sub CustomMarshal()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Strings

Public Class X

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:=Nothing)>
    Public CustomMarshaler1 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef:=Nothing)>
    Public CustomMarshaler2 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="foo", MarshalTypeRef:=GetType(Integer))>
    Public CustomMarshaler3 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:=ChrW(&H1234) & "f" & ChrW(0) & "oozzz")>
    Public CustomMarshaler4 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="f" & ChrW(0) & "oozzz")>
    Public CustomMarshaler5 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")>
    Public CustomMarshaler6 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, ArraySubType:=UnmanagedType.ByValTStr, IidParameterIndex:=-1, MarshalCookie:=Nothing, MarshalType:=Nothing, MarshalTypeRef:=Nothing, SafeArraySubType:=VarEnum.VT_BSTR, SafeArrayUserDefinedSubType:=Nothing, SizeConst:=-1, SizeParamIndex:=-1)>
    Public CustomMarshaler7 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef:=GetType(Integer))>
    Public CustomMarshaler8 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef:=GetType(Integer), MarshalType:="foo", MarshalCookie:="hello" & ChrW(0) & "world(" & ChrW(&H1234) & ")")>
    Public CustomMarshaler9 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:=Nothing, MarshalTypeRef:=GetType(Integer))>
    Public CustomMarshaler10 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="foo", MarshalTypeRef:=Nothing)>
    Public CustomMarshaler11 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:=Nothing, MarshalTypeRef:=Nothing)>
    Public CustomMarshaler12 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="aaa" & ChrW(0) & "bbb", MarshalCookie:="ccc" & ChrW(0) & "ddd")>
    Public CustomMarshaler13 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:=ChrW(&HD869) & ChrW(&HDED6), MarshalCookie:=ChrW(&HD869) & ChrW(&HDED6))>
    Public CustomMarshaler14 As Integer
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"CustomMarshaler1", New Byte() {&H2C, &H0, &H0, &H0, &H0}},
                {"CustomMarshaler2", New Byte() {&H2C, &H0, &H0, &H0, &H0}},
                {"CustomMarshaler3", New Byte() {&H2C, &H0, &H0, &H3, &H66, &H6F, &H6F, &H0}},
                {"CustomMarshaler4", New Byte() {&H2C, &H0, &H0, &HA, &HE1, &H88, &HB4, &H66, &H0, &H6F, &H6F, &H7A, &H7A, &H7A, &H0}},
                {"CustomMarshaler5", New Byte() {&H2C, &H0, &H0, &H7, &H66, &H0, &H6F, &H6F, &H7A, &H7A, &H7A, &H0}},
                {"CustomMarshaler6", New Byte() {&H2C, &H0, &H0, &H60}.Append(Encoding.UTF8.GetBytes("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" & ChrW(0)))},
                {"CustomMarshaler7", New Byte() {&H2C, &H0, &H0, &H0, &H0}},
                {"CustomMarshaler8", New Byte() {&H2C, &H0, &H0, &H59}.Append(Encoding.UTF8.GetBytes("System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" & ChrW(0)))},
                {"CustomMarshaler9", New Byte() {&H2C, &H0, &H0, &H3, &H66, &H6F, &H6F, &H10, &H68, &H65, &H6C, &H6C, &H6F, &H0, &H77, &H6F, &H72, &H6C, &H64, &H28, &HE1, &H88, &HB4, &H29}},
                {"CustomMarshaler10", New Byte() {&H2C, &H0, &H0, &H0, &H0}},
                {"CustomMarshaler11", New Byte() {&H2C, &H0, &H0, &H3, &H66, &H6F, &H6F, &H0}},
                {"CustomMarshaler12", New Byte() {&H2C, &H0, &H0, &H0, &H0}},
                {"CustomMarshaler13", New Byte() {&H2C, &H0, &H0, &H7, &H61, &H61, &H61, &H0, &H62, &H62, &H62, &H7, &H63, &H63, &H63, &H0, &H64, &H64, &H64}},
                {"CustomMarshaler14", New Byte() {&H2C, &H0, &H0, &H4, &HF0, &HAA, &H9B, &H96, &H4, &HF0, &HAA, &H9B, &H96}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs)
            VerifyFieldMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub CustomMarshal_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Strings

Public Class X

    <MarshalAs(UnmanagedType.CustomMarshaler)>
    Dim CustomMarshaler_e0 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="a" & ChrW(&HDC00) & "b", MarshalCookie:="b")>
    Dim CustomMarshaler_e1 As Integer

    <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="x", MarshalCookie:="y" & ChrW(&HDC00))>
    Dim CustomMarshaler_e2 As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_AttributeParameterRequired2, "MarshalAs").WithArguments("MarshalType", "MarshalTypeRef"),
                Diagnostic(ERRID.ERR_BadAttribute1, "MarshalType:=""a"" & ChrW(&HDC00) & ""b""").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "MarshalCookie:=""y"" & ChrW(&HDC00)").WithArguments("System.Runtime.InteropServices.MarshalAsAttribute"))
        End Sub

        <Fact()>
        Public Sub Events_Error()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class C
    <MarshalAs(UnmanagedType.Bool)>
    Event e As Action
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "MarshalAs").WithArguments("MarshalAsAttribute", "e"))
        End Sub

        <Fact()>
        Public Sub MarshalAs_AllFieldTargets()
            Dim source = <compilation><file><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class Z
    <MarshalAs(UnmanagedType.Bool)>
    Dim f As Integer
End Class

Module M
    <MarshalAs(UnmanagedType.Bool)>
    Public WithEvents we As New Z
End Module

Enum En
    <MarshalAs(UnmanagedType.Bool)>
    A = 1
    <MarshalAs(UnmanagedType.Bool)>
    B
End Enum
]]></file></compilation>

            CompileAndVerifyFieldMarshal(source,
                Function(name, _omitted1)
                    Return If(name = "f" Or name = "_we" Or name = "A" Or name = "B", New Byte() {&H2}, Nothing)
                End Function)
        End Sub

        <Fact()>
        Public Sub Parameters()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Strings

Class X
    Public Shared Function foo(<MarshalAs(UnmanagedType.IDispatch)> ByRef IDispatch As Integer,
                               <MarshalAs(UnmanagedType.LPArray)> ByRef LPArray0 As Integer,
                               <MarshalAs(UnmanagedType.SafeArray, SafeArraySubType:=VarEnum.VT_EMPTY)> SafeArray8 As Integer,
                               <MarshalAs(UnmanagedType.CustomMarshaler, MarshalType:="aaa" & ChrW(0) & "bbb", MarshalCookie:="ccc" & ChrW(0) & "ddd")> CustomMarshaler13 As Integer) As <MarshalAs(UnmanagedType.LPStr)> X
        Return Nothing
    End Function
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {"foo:", New Byte() {&H14}},
                {"foo:IDispatch", New Byte() {&H1A}},
                {"foo:LPArray0", New Byte() {&H2A, &H50}},
                {"foo:SafeArray8", New Byte() {&H1D, &H0}},
                {"foo:CustomMarshaler13", New Byte() {&H2C, &H0, &H0, &H7, &H61, &H61, &H61, &H0, &H62, &H62, &H62, &H7, &H63, &H63, &H63, &H0, &H64, &H64, &H64}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False)
            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub

        <Fact>
        Public Sub MarshalAs_AllParameterTargets_Events()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module X
    Custom Event E As Action
        AddHandler(<MarshalAs(UnmanagedType.BStr)> eAdd As Action)
        End AddHandler
        RemoveHandler(<MarshalAs(UnmanagedType.BStr)> eRemove As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Module
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {"add_E:eAdd", New Byte() {&H13}},
                {"remove_E:eRemove", New Byte() {&H13}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False)
            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub

        <Fact>
        Public Sub MarshalAs_AllParameterTargets_Properties()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module C
    Property P(<MarshalAs(UnmanagedType.I2)> pIndex As Integer) As <MarshalAs(UnmanagedType.I4)> Integer
        Get
            Return 0
        End Get
        Set(<MarshalAs(UnmanagedType.I8)> pValue As Integer)
        End Set
    End Property

    Property Q As <MarshalAs(UnmanagedType.I4)> Integer
        Get
            Return 0
        End Get
        Set(qValue As Integer)
        End Set
    End Property

    Property CRW As <MarshalAs(UnmanagedType.I4)> Integer

    WriteOnly Property CW As <MarshalAs(UnmanagedType.I4)> Integer
        Set(sValue As Integer)
        End Set
    End Property

    ReadOnly Property CR As <MarshalAs(UnmanagedType.I4)> Integer
        Get
            Return 0
        End Get
    End Property
End Module

Interface I
    Property IRW As <MarshalAs(UnmanagedType.I4)> Integer
    ReadOnly Property IR As <MarshalAs(UnmanagedType.I4)> Integer
    WriteOnly Property IW As <MarshalAs(UnmanagedType.I4)> Integer

    Property IRW2(a As Integer, b As Integer) As <MarshalAs(UnmanagedType.I4)> Integer
    ReadOnly Property IR2(a As Integer, b As Integer) As <MarshalAs(UnmanagedType.I4)> Integer
    WriteOnly Property IW2(a As Integer, b As Integer) As <MarshalAs(UnmanagedType.I4)> Integer
End Interface
]]>
    </file>
</compilation>

            Dim i2 = New Byte() {&H5}
            Dim i4 = New Byte() {&H7}
            Dim i8 = New Byte() {&H9}

            ' Dev11 incorrectly applies return-type MarshalAs on the first parameter of an interface property.

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {"get_P:", i4},
                {"get_P:pIndex", i2},
                {"set_P:pIndex", i2},
                {"set_P:pValue", i8},
                {"get_Q:", i4},
                {"set_Q:qValue", Nothing},
                {"get_CRW:", i4},
                {"set_CRW:AutoPropertyValue", Nothing},
                {"set_CW:sValue", Nothing},
                {"get_CR:", i4},
                {"get_IRW:", i4},
                {"set_IRW:Value", i4},
                {"get_IR:", i4},
                {"set_IW:Value", i4},
                {"get_IRW2:", i4},
                {"get_IRW2:a", Nothing},
                {"get_IRW2:b", Nothing},
                {"set_IRW2:a", Nothing},
                {"set_IRW2:b", Nothing},
                {"set_IRW2:Value", i4},
                {"get_IR2:", i4},
                {"get_IR2:a", Nothing},
                {"get_IR2:b", Nothing},
                {"set_IW2:a", Nothing},
                {"set_IW2:b", Nothing},
                {"set_IW2:Value", i4}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<errors><![CDATA[
BC42364: Attributes applied on a return type of a WriteOnly Property have no effect.
    WriteOnly Property CW As <MarshalAs(UnmanagedType.I4)> Integer
                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)

            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub

        <Fact>
        Public Sub MarshalAs_PropertyReturnType_MissingAccessors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module C
    Property Q As <MarshalAs(UnmanagedType.I4)> Integer
    End Property
End Module
]]>
    </file>
</compilation>

            Dim c = CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c,
<errors><![CDATA[
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
    Property Q As <MarshalAs(UnmanagedType.I4)> Integer
             ~
]]></errors>)

        End Sub

        <Fact>
        Public Sub MarshalAs_AllParameterTargets_PartialSubs()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Partial Class X
    Private Partial Sub F(<MarshalAs(UnmanagedType.BStr)> pf As Integer)
    End Sub

    Private Sub F(pf As Integer)
    End Sub

    Private Partial Sub G(pg As Integer)
    End Sub

    Private Sub G(<MarshalAs(UnmanagedType.BStr)> pg As Integer)
    End Sub

    Private Sub H(<MarshalAs(UnmanagedType.BStr)> ph As Integer)
    End Sub

    Private Partial Sub H(ph As Integer)
    End Sub

    Private Sub I(pi As Integer)
    End Sub

    Private Partial Sub I(<MarshalAs(UnmanagedType.BStr)> pi As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {"F:pf", New Byte() {&H13}},
                {"G:pg", New Byte() {&H13}},
                {"H:ph", New Byte() {&H13}},
                {"I:pi", New Byte() {&H13}}
            }

            CompileAndVerifyFieldMarshal(source, blobs, isField:=False)
        End Sub

        <Fact>
        Public Sub MarshalAs_AllParameterTargets_Delegate()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Delegate Function D(<MarshalAs(UnmanagedType.BStr)>p As Integer, <MarshalAs(UnmanagedType.BStr)>ByRef q As Integer) As <MarshalAs(UnmanagedType.BStr)> Integer
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {".ctor:TargetObject", Nothing},
                {".ctor:TargetMethod", Nothing},
                {"BeginInvoke:p", New Byte() {&H13}},
                {"BeginInvoke:q", New Byte() {&H13}},
                {"BeginInvoke:DelegateCallback", Nothing},
                {"BeginInvoke:DelegateAsyncState", Nothing},
                {"EndInvoke:p", New Byte() {&H13}},
                {"EndInvoke:q", New Byte() {&H13}},
                {"EndInvoke:DelegateAsyncResult", Nothing},
                {"Invoke:", New Byte() {&H13}},
                {"Invoke:p", New Byte() {&H13}},
                {"Invoke:q", New Byte() {&H13}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False)
            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub

        <Fact>
        Public Sub MarshalAs_AllParameterTargets_Declare()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Module M
    Declare Function Foo Lib "foo" (
        <MarshalAs(UnmanagedType.BStr)> explicitInt As Integer,
        <MarshalAs(UnmanagedType.BStr)> ByRef explicitByRefInt As Integer,
        <MarshalAs(UnmanagedType.Bool)> explicitString As String,
        <MarshalAs(UnmanagedType.Bool)> ByRef explicitByRefString As String,
        pString As String,
        ByRef pByRefString As String,
        pInt As Integer,
        ByRef pByRefInt As Integer
    ) As <MarshalAs(UnmanagedType.BStr)> Integer
End Module
]]>
    </file>
</compilation>

            Const bstr = &H13
            Const bool = &H2
            Const byvalstr = &H22
            Const ansi_bstr = &H23

            Dim blobs = New Dictionary(Of String, Byte())() From
            {
                {"Foo:", New Byte() {bstr}},
                {"Foo:explicitInt", New Byte() {bstr}},
                {"Foo:explicitByRefInt", New Byte() {bstr}},
                {"Foo:explicitString", New Byte() {bool}},
                {"Foo:explicitByRefString", New Byte() {bool}},
                {"Foo:pString", New Byte() {byvalstr}},
                {"Foo:pByRefString", New Byte() {ansi_bstr}},
                {"Foo:pInt", Nothing},
                {"Foo:pByRefInt", Nothing}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False, expectedSignatures:=
            {
                Signature("M", "Foo",
                      ".method [System.Runtime.InteropServices.DllImportAttribute(""foo"", EntryPoint = ""Foo"", CharSet = 2, ExactSpelling = True, SetLastError = True, PreserveSig = True, CallingConvention = 1, BestFitMapping = False, ThrowOnUnmappableChar = False)] " &
                      "[System.Runtime.InteropServices.PreserveSigAttribute()] " &
                      "public static pinvokeimpl System.Int32 Foo(" &
                            "System.Int32 explicitInt, " &
                            "System.Int32& explicitByRefInt, " &
                            "System.String explicitString, " &
                            "System.String& explicitByRefString, " &
                            "System.String& pString, " &
                            "System.String& pByRefString, " &
                            "System.Int32 pInt, " &
                            "System.Int32& pByRefInt" &
                      ") cil managed preservesig")
            })

            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub

        <Fact()>
        Public Sub Parameters_Errors()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.InteropServices

Class X

    Public Shared Sub f1(<MarshalAs(UnmanagedType.ByValArray)> ByValArray As Integer, <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1)> ByValTStr As Integer)
    End Sub

    Public Shared Function f2() As <MarshalAs(UnmanagedType.ByValArray)> Integer
        Return 0
    End Function

    Public Shared Function f3() As <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=1)> Integer
        Return 0
    End Function

    <MarshalAs(UnmanagedType.VBByRefStr)>
    Public field As Integer
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray"),
                Diagnostic(ERRID.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr"),
                Diagnostic(ERRID.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValArray").WithArguments("ByValArray"),
                Diagnostic(ERRID.ERR_MarshalUnmanagedTypeOnlyValidForFields, "UnmanagedType.ByValTStr").WithArguments("ByValTStr"),
                Diagnostic(ERRID.ERR_MarshalUnmanagedTypeNotValidForFields, "UnmanagedType.VBByRefStr").WithArguments("VBByRefStr"))
        End Sub

        ''' <summary>  
        ''' type only, only on parameters 
        ''' </summary>   
        <Fact>
        Public Sub NativeTypeByValStr()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Class X
    Shared Function f(
        <MarshalAs(UnmanagedType.VBByRefStr,
                   ArraySubType:=UnmanagedType.ByValTStr,
                   IidParameterIndex:=-1,
                   MarshalCookie:=Nothing,
                   MarshalType:=Nothing,
                   MarshalTypeRef:=Nothing,
                   SafeArraySubType:=VarEnum.VT_BSTR,
                   SafeArrayUserDefinedSubType:=Nothing,
                   SizeConst:=-1,
                   SizeParamIndex:=-1)> ByRef VBByRefStr_e1 As Integer,
 _
        <MarshalAs(UnmanagedType.VBByRefStr,
                   ArraySubType:=UnmanagedType.ByValTStr,
                   IidParameterIndex:=-1,
                   MarshalCookie:=Nothing,
                   MarshalType:=Nothing,
                   MarshalTypeRef:=Nothing,
                   SafeArraySubType:=VarEnum.VT_BSTR,
                   SafeArrayUserDefinedSubType:=Nothing,
                   SizeConst:=-1,
                   SizeParamIndex:=-1)> VBByRefStr_e2 As Char(),
 _
        <MarshalAs(UnmanagedType.VBByRefStr,
                   ArraySubType:=UnmanagedType.ByValTStr,
                   IidParameterIndex:=-1,
                   MarshalCookie:=Nothing,
                   MarshalType:=Nothing,
                   MarshalTypeRef:=Nothing,
                   SafeArraySubType:=VarEnum.VT_BSTR,
                   SafeArrayUserDefinedSubType:=Nothing,
                   SizeConst:=-1,
                   SizeParamIndex:=-1)> VBByRefStr_e3 As Integer) _
        As <MarshalAs(UnmanagedType.VBByRefStr,
               ArraySubType:=UnmanagedType.ByValTStr,
               IidParameterIndex:=-1,
               MarshalCookie:=Nothing,
               MarshalType:=Nothing,
               MarshalTypeRef:=Nothing,
               SafeArraySubType:=VarEnum.VT_BSTR,
               SafeArrayUserDefinedSubType:=Nothing,
               SizeConst:=-1,
               SizeParamIndex:=-1)> Integer
        Return 0
    End Function
End Class
]]>
    </file>
</compilation>

            Dim blobs = New Dictionary(Of String, Byte()) From
            {
                {"f:", New Byte() {&H22}},
                {"f:VBByRefStr_e1", New Byte() {&H22}},
                {"f:VBByRefStr_e2", New Byte() {&H22}},
                {"f:VBByRefStr_e3", New Byte() {&H22}}
            }

            Dim verifier = CompileAndVerifyFieldMarshal(source, blobs, isField:=False)
            VerifyParameterMetadataDecoding(verifier, blobs)
        End Sub
    End Class
End Namespace
