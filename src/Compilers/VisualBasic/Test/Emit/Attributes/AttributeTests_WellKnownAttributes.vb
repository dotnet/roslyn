' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AttributeTests_WellKnownAttributes
        Inherits BasicTestBase

#Region "InteropAttributes Miscellaneous Tests"
        <Fact>
        Public Sub TestInteropAttributes01()
            Dim source =
            <compilation>
                <file name="attr.vb"><![CDATA[
                Imports System
                Imports System.Runtime.InteropServices

                <Assembly: ComCompatibleVersion(1, 2, 3, 4)> 

                <ComImport(), Guid("ABCDEF5D-2448-447A-B786-64682CBEF123")>
                <InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
                <TypeLibImportClass(GetType(Object)), TypeLibType(TypeLibTypeFlags.FAggregatable)>
                <BestFitMapping(False, ThrowOnUnmappableChar:=True)>
                Public Interface IGoo

                    <AllowReversePInvokeCalls()>
                    Sub DoSomething()
                    <ComRegisterFunction()>
                    Sub Register(o As Object)

                    <ComUnregisterFunction()>
                    Sub UnRegister()

                    <TypeLibFunc(TypeLibFuncFlags.FDefaultBind)>
                    Sub LibFunc()
                End Interface
            ]]>
                </file>
            </compilation>

            Dim attributeValidator =
                Function(isFromSource As Boolean) _
                    Sub(m As ModuleSymbol)
                        Dim assembly = m.ContainingAssembly
                        Dim compilation = m.DeclaringCompilation
                        Dim globalNS = If(compilation Is Nothing, assembly.CorLibrary.GlobalNamespace, compilation.GlobalNamespace)

                        Dim sysNS = globalNS.GetMember(Of NamespaceSymbol)("System")
                        Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
                        Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)
                        Dim comCompatibleSym As NamedTypeSymbol = interopNS.GetTypeMembers("ComCompatibleVersionAttribute").First()

                        ' Assembly
                        Dim attrs = assembly.GetAttributes(comCompatibleSym)
                        Assert.Equal(1, attrs.Count)
                        Dim attrSym = attrs.First()
                        Assert.Equal("ComCompatibleVersionAttribute", attrSym.AttributeClass.Name)
                        Assert.Equal(4, attrSym.CommonConstructorArguments.Length)
                        Assert.Equal(0, attrSym.CommonNamedArguments.Length)
                        Assert.Equal(3, attrSym.CommonConstructorArguments(2).Value)

                        ' get expected attr symbol
                        Dim guidSym = DirectCast(interopNS.GetTypeMember("GuidAttribute"), NamedTypeSymbol)
                        Dim ciSym = DirectCast(interopNS.GetTypeMember("ComImportAttribute"), NamedTypeSymbol)
                        Dim iTypeSym = DirectCast(interopNS.GetTypeMember("InterfaceTypeAttribute"), NamedTypeSymbol)
                        Dim itCtor = DirectCast(iTypeSym.Constructors.First(), MethodSymbol)
                        Dim tLibSym = DirectCast(interopNS.GetTypeMember("TypeLibImportClassAttribute"), NamedTypeSymbol)
                        Dim tLTypeSym = DirectCast(interopNS.GetTypeMember("TypeLibTypeAttribute"), NamedTypeSymbol)
                        Dim bfmSym = DirectCast(interopNS.GetTypeMember("BestFitMappingAttribute"), NamedTypeSymbol)

                        ' IGoo
                        Dim igoo = DirectCast(m.GlobalNamespace.GetTypeMember("IGoo"), NamedTypeSymbol)

                        Assert.True(igoo.IsComImport)
                        ' ComImportAttribute is a pseudo-custom attribute, which is not emitted.
                        If Not isFromSource Then
                            Assert.Equal(5, igoo.GetAttributes().Length)
                        Else
                            Assert.Equal(6, igoo.GetAttributes().Length)

                            ' get attr by NamedTypeSymbol
                            attrSym = igoo.GetAttribute(ciSym)
                            Assert.Equal("ComImportAttribute", attrSym.AttributeClass.Name)
                            Assert.Equal(0, attrSym.CommonConstructorArguments.Length)
                            Assert.Equal(0, attrSym.CommonNamedArguments.Length)
                        End If

                        attrSym = igoo.GetAttribute(guidSym)
                        Assert.Equal("String", attrSym.CommonConstructorArguments(0).Type.ToDisplayString)
                        Assert.Equal("ABCDEF5D-2448-447A-B786-64682CBEF123", attrSym.CommonConstructorArguments(0).Value)

                        ' get attr by ctor
                        attrSym = igoo.GetAttribute(itCtor)
                        Assert.Equal("System.Runtime.InteropServices.ComInterfaceType", attrSym.CommonConstructorArguments(0).Type.ToDisplayString())
                        Assert.Equal(ComInterfaceType.InterfaceIsIUnknown, CType(attrSym.CommonConstructorArguments(0).Value, ComInterfaceType))

                        attrSym = igoo.GetAttribute(tLibSym)
                        Assert.Equal("Object", CType(attrSym.CommonConstructorArguments(0).Value, Symbol).ToDisplayString())

                        attrSym = igoo.GetAttribute(tLTypeSym)
                        Assert.Equal(TypeLibTypeFlags.FAggregatable, CType(attrSym.CommonConstructorArguments(0).Value, TypeLibTypeFlags))

                        attrSym = igoo.GetAttribute(bfmSym)
                        Assert.Equal(False, attrSym.CommonConstructorArguments(0).Value)
                        Assert.Equal(1, attrSym.CommonNamedArguments.Length)
                        Assert.Equal("Boolean", attrSym.CommonNamedArguments(0).Value.Type.ToDisplayString)
                        Assert.Equal("ThrowOnUnmappableChar", attrSym.CommonNamedArguments(0).Key)
                        Assert.Equal(True, attrSym.CommonNamedArguments(0).Value.Value)

                        ' =============================
                        Dim mem = DirectCast(igoo.GetMembers("DoSomething").First(), MethodSymbol)
                        Assert.Equal(1, mem.GetAttributes().Length)
                        attrSym = mem.GetAttributes().First()
                        Assert.Equal("AllowReversePInvokeCallsAttribute", attrSym.AttributeClass.Name)
                        Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                        mem = DirectCast(igoo.GetMembers("Register").First(), MethodSymbol)
                        attrSym = mem.GetAttributes().First()
                        Assert.Equal("ComRegisterFunctionAttribute", attrSym.AttributeClass.Name)
                        Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                        mem = DirectCast(igoo.GetMembers("UnRegister").First(), MethodSymbol)
                        Assert.Equal(1, mem.GetAttributes().Length)

                        mem = DirectCast(igoo.GetMembers("LibFunc").First(), MethodSymbol)
                        attrSym = mem.GetAttributes().First()
                        Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
                        Assert.Equal(TypeLibFuncFlags.FDefaultBind, CType(attrSym.CommonConstructorArguments(0).Value, TypeLibFuncFlags)) ' 32
                    End Sub


            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator(True), symbolValidator:=attributeValidator(False))
        End Sub

        <Fact>
        Public Sub TestInteropAttributes02()
            Dim source =
            <compilation>
                <file name="attr.vb"><![CDATA[
                Imports System
                Imports System.Runtime.InteropServices

                <Assembly: PrimaryInteropAssembly(1, 2)> 
                <Assembly: Guid("1234C65D-1234-447A-B786-64682CBEF136")> 

                <ComVisibleAttribute(False)>
                <UnmanagedFunctionPointerAttribute(CallingConvention.StdCall, BestFitMapping:=True, CharSet:=CharSet.Ansi, SetLastError:=True, ThrowOnUnmappableChar:=True)>
                Public Delegate Sub DGoo(p1 As Char, p2 As SByte)

                <ComDefaultInterface(GetType(Object)), ProgId("ProgId")>
                Public Class CGoo

                   <DispIdAttribute(123)> <LCIDConversion(1), ComConversionLoss()>
                    Sub Method(p1 As SByte, p2 As String)
                    End Sub
                End Class

                <ComVisible(true), TypeIdentifier("1234C65D-1234-447A-B786-64682CBEF136", "EGoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")>
                Public Enum EGoo
                    One
                    <TypeLibVar(TypeLibVarFlags.FDisplayBind)>
                    Two
                    <Obsolete("message", false)>
                    Three
                End Enum
            ]]>
                </file>
            </compilation>

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim assembly = m.ContainingAssembly
                                         Assert.Equal(ImmutableArray.Create(Of SyntaxReference)(), m.DeclaringSyntaxReferences)

                                         Dim compilation = m.DeclaringCompilation
                                         Dim globalNS = If(compilation Is Nothing, assembly.CorLibrary.GlobalNamespace, compilation.GlobalNamespace)
                                         Dim sysNS = globalNS.GetMember(Of NamespaceSymbol)("System")

                                         ' get expected attr symbol
                                         Dim runtimeNS = DirectCast(sysNS.GetMember("Runtime"), NamespaceSymbol)
                                         Dim interopNS = DirectCast(runtimeNS.GetMember("InteropServices"), NamespaceSymbol)

                                         Dim comvSym = DirectCast(interopNS.GetTypeMember("ComVisibleAttribute"), NamedTypeSymbol)
                                         Dim ufPtrSym = DirectCast(interopNS.GetTypeMember("UnmanagedFunctionPointerAttribute"), NamedTypeSymbol)
                                         Dim comdSym = DirectCast(interopNS.GetTypeMember("ComDefaultInterfaceAttribute"), NamedTypeSymbol)
                                         Dim pgidSym = DirectCast(interopNS.GetTypeMember("ProgIdAttribute"), NamedTypeSymbol)
                                         Dim tidSym = DirectCast(interopNS.GetTypeMember("TypeIdentifierAttribute"), NamedTypeSymbol)
                                         Dim dispSym = DirectCast(interopNS.GetTypeMember("DispIdAttribute"), NamedTypeSymbol)
                                         Dim lcidSym = DirectCast(interopNS.GetTypeMember("LCIDConversionAttribute"), NamedTypeSymbol)
                                         Dim comcSym = DirectCast(interopNS.GetTypeMember("ComConversionLossAttribute"), NamedTypeSymbol)

                                         Dim moduleGlobalNS = m.GlobalNamespace

                                         ' delegate DGoo
                                         Dim type1 = DirectCast(moduleGlobalNS.GetTypeMember("DGoo"), NamedTypeSymbol)
                                         Assert.Equal(2, type1.GetAttributes().Length)

                                         Dim attrSym = type1.GetAttribute(comvSym)
                                         Assert.Equal(False, attrSym.CommonConstructorArguments(0).Value)

                                         attrSym = type1.GetAttribute(ufPtrSym)
                                         Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
                                         Assert.Equal(CallingConvention.StdCall, CType(attrSym.CommonConstructorArguments(0).Value, CallingConvention)) ' 3

                                         Assert.Equal(4, attrSym.CommonNamedArguments.Length)
                                         Assert.Equal("BestFitMapping", attrSym.CommonNamedArguments(0).Key)
                                         Assert.Equal(True, attrSym.CommonNamedArguments(0).Value.Value)
                                         Assert.Equal("CharSet", attrSym.CommonNamedArguments(1).Key)
                                         Assert.Equal(CharSet.Ansi, CType(attrSym.CommonNamedArguments(1).Value.Value, CharSet))
                                         Assert.Equal("SetLastError", attrSym.CommonNamedArguments(2).Key)
                                         Assert.Equal(True, attrSym.CommonNamedArguments(2).Value.Value)
                                         Assert.Equal("ThrowOnUnmappableChar", attrSym.CommonNamedArguments(3).Key)
                                         Assert.Equal(True, attrSym.CommonNamedArguments(3).Value.Value)

                                         ' class CGoo
                                         Dim type2 = DirectCast(moduleGlobalNS.GetTypeMember("CGoo"), NamedTypeSymbol)
                                         Assert.Equal(2, type2.GetAttributes().Length)

                                         attrSym = type2.GetAttribute(comdSym)
                                         Assert.Equal("Object", CType(attrSym.CommonConstructorArguments(0).Value, Symbol).ToDisplayString())

                                         attrSym = type2.GetAttribute(pgidSym)
                                         Assert.Equal("String", attrSym.CommonConstructorArguments(0).Type.ToDisplayString)
                                         Assert.Equal("ProgId", attrSym.CommonConstructorArguments(0).Value)

                                         Dim method = DirectCast(type2.GetMembers("Method").First(), MethodSymbol)
                                         attrSym = method.GetAttribute(dispSym)
                                         Assert.Equal(123, attrSym.CommonConstructorArguments(0).Value)
                                         attrSym = method.GetAttribute(lcidSym)
                                         Assert.Equal(1, attrSym.CommonConstructorArguments(0).Value)
                                         attrSym = method.GetAttribute(comcSym)
                                         Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                                         '' enum EGoo
                                         If compilation IsNot Nothing Then
                                             ' Because this is a nopia local type it is only visible from the source assembly.
                                             Dim type3 = DirectCast(globalNS.GetTypeMember("EGoo"), NamedTypeSymbol)
                                             Assert.Equal(2, type3.GetAttributes().Length)

                                             attrSym = type3.GetAttribute(comvSym)
                                             Assert.Equal(True, attrSym.CommonConstructorArguments(0).Value)

                                             attrSym = type3.GetAttribute(tidSym)
                                             Assert.Equal(2, attrSym.CommonConstructorArguments.Length)
                                             Assert.Equal("EGoo, InteropAttribute, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", attrSym.CommonConstructorArguments(1).Value)

                                             Dim field = DirectCast(type3.GetMembers("one").First(), FieldSymbol)
                                             Assert.Equal(0, field.GetAttributes().Length)

                                             field = DirectCast(type3.GetMembers("two").First(), FieldSymbol)
                                             Assert.Equal(1, field.GetAttributes().Length)
                                             attrSym = field.GetAttributes.First
                                             Assert.Equal("TypeLibVarAttribute", attrSym.AttributeClass.Name)
                                             Assert.Equal(TypeLibVarFlags.FDisplayBind, CType(attrSym.CommonConstructorArguments(0).Value, TypeLibVarFlags))

                                             field = DirectCast(type3.GetMembers("three").First(), FieldSymbol)
                                             attrSym = field.GetAttributes().First()
                                             Assert.Equal("ObsoleteAttribute", attrSym.AttributeClass.Name)
                                             Assert.Equal(2, attrSym.CommonConstructorArguments.Length)
                                             Assert.Equal("message", attrSym.CommonConstructorArguments(0).Value)
                                             Assert.Equal(False, attrSym.CommonConstructorArguments(1).Value)
                                         End If

                                     End Sub


            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <WorkItem(540573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540573")>
        <Fact()>
        Public Sub TestPseudoAttributes01()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<ComImport()>
Public Interface IBar
    Function Method1(<OptionalAttribute(), DefaultParameterValue(99UL)> ByRef v As ULong) As ULong
    Function Method2(<InAttribute(), Out(), DefaultParameterValue("Ref")> ByRef v As String) As String

    Function Method3(<InAttribute(), OptionalAttribute(), DefaultParameterValue(" "c)> v1 As Char,
                    <Out()> <OptionalAttribute()> <DefaultParameterValue(0.0F)> v2 As Single,
                    <InAttribute()> <OptionalAttribute()> <DefaultParameterValue(Nothing)> v3 As String)

    <PreserveSig()>
    Sub Method4(
        <DateTimeConstant(123456)> p1 As DateTime,
        <DecimalConstant(0, 0, 100, 100, 100)> p2 As Decimal,
        <OptionalAttribute(), IDispatchConstant()> ByRef p3 As Object)
End Interface

<Serializable(), StructLayout(LayoutKind.Explicit, Size:=16, Pack:=8, CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
Public Class CBar
    <NonSerialized(), MarshalAs(UnmanagedType.I8), FieldOffset(0)>
    Public field As Long
End Class
]]>
    </file>
</compilation>

            Dim attributeValidator = Sub(m As ModuleSymbol)
                                         Dim assembly = m.ContainingSymbol

                                         Dim sourceAssembly = TryCast(assembly, SourceAssemblySymbol)
                                         Dim sysNS As NamespaceSymbol = Nothing
                                         If sourceAssembly IsNot Nothing Then
                                             sysNS = DirectCast(sourceAssembly.DeclaringCompilation.GlobalNamespace.GetMember("System"), NamespaceSymbol)
                                         Else
                                             Dim peAssembly = DirectCast(assembly, PEAssemblySymbol)
                                             sysNS = DirectCast(peAssembly.CorLibrary.GlobalNamespace.GetMember("System"), NamespaceSymbol)
                                         End If

                                         ' get expected attr symbol
                                         Dim runtimeNS = sysNS.GetNamespace("Runtime")
                                         Dim interopNS = runtimeNS.GetNamespace("InteropServices")
                                         Dim compsrvNS = runtimeNS.GetNamespace("CompilerServices")

                                         Dim serSym = sysNS.GetTypeMember("SerializableAttribute")
                                         Dim nosSym = sysNS.GetTypeMember("NonSerializedAttribute")

                                         Dim ciptSym = interopNS.GetTypeMember("ComImportAttribute")
                                         Dim laySym = interopNS.GetTypeMember("StructLayoutAttribute")
                                         Dim sigSym = interopNS.GetTypeMember("PreserveSigAttribute")
                                         Dim offSym = interopNS.GetTypeMember("FieldOffsetAttribute")
                                         Dim mshSym = interopNS.GetTypeMember("MarshalAsAttribute")


                                         Dim optSym = interopNS.GetTypeMember("OptionalAttribute")
                                         Dim inSym = interopNS.GetTypeMember("InAttribute")
                                         Dim outSym = interopNS.GetTypeMember("OutAttribute")
                                         ' non pseudo
                                         Dim dtcSym = compsrvNS.GetTypeMember("DateTimeConstantAttribute")
                                         Dim dmcSym = compsrvNS.GetTypeMember("DecimalConstantAttribute")
                                         Dim iscSym = compsrvNS.GetTypeMember("IDispatchConstantAttribute")

                                         Dim globalNS = m.GlobalNamespace
                                         ' Interface IBar
                                         Dim type1 = globalNS.GetTypeMember("IBar")
                                         Dim attrSym = type1.GetAttribute(ciptSym)
                                         Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                                         Dim method As MethodSymbol
                                         Dim parm As ParameterSymbol
                                         If sourceAssembly IsNot Nothing Then
                                             ' Default attribute is in system.dll not mscorlib. Only do this check for source attributes.
                                             Dim defvSym = interopNS.GetTypeMember("DefaultParameterValueAttribute")
                                             method = type1.GetMember(Of MethodSymbol)("Method1")
                                             parm = method.Parameters(0)
                                             attrSym = parm.GetAttribute(defvSym)
                                             attrSym.VerifyValue(0, TypedConstantKind.Primitive, 99UL)
                                             attrSym = parm.GetAttribute(optSym)
                                             Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                                             method = type1.GetMember(Of MethodSymbol)("Method2")
                                             parm = method.Parameters(0)
                                             Assert.Equal(3, parm.GetAttributes().Length)
                                             attrSym = parm.GetAttribute(defvSym)
                                             attrSym.VerifyValue(0, TypedConstantKind.Primitive, "Ref")
                                             attrSym = parm.GetAttribute(inSym)
                                             Assert.Equal(0, attrSym.CommonConstructorArguments.Length)
                                             attrSym = parm.GetAttribute(outSym)
                                             Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                                             method = type1.GetMember(Of MethodSymbol)("Method3")
                                             parm = method.Parameters(1) ' v2
                                             Assert.Equal(3, parm.GetAttributes().Length)
                                             attrSym = parm.GetAttribute(defvSym)
                                             attrSym.VerifyValue(0, TypedConstantKind.Primitive, 0.0F)
                                             attrSym = parm.GetAttribute(optSym)
                                             Assert.Equal(0, attrSym.CommonConstructorArguments.Length)
                                             attrSym = parm.GetAttribute(outSym)
                                             Assert.Equal(0, attrSym.CommonConstructorArguments.Length)
                                         End If

                                         method = type1.GetMember(Of MethodSymbol)("Method4")
                                         attrSym = method.GetAttributes().First()
                                         Assert.Equal("PreserveSigAttribute", attrSym.AttributeClass.Name)

                                         parm = method.Parameters(0)
                                         attrSym = parm.GetAttributes().First()
                                         Assert.Equal("DateTimeConstantAttribute", attrSym.AttributeClass.Name)
                                         attrSym.VerifyValue(0, TypedConstantKind.Primitive, 123456)

                                         parm = method.Parameters(1)
                                         attrSym = parm.GetAttributes().First()
                                         Assert.Equal("DecimalConstantAttribute", attrSym.AttributeClass.Name)
                                         Assert.Equal(5, attrSym.CommonConstructorArguments.Length)
                                         attrSym.VerifyValue(2, TypedConstantKind.Primitive, 100)

                                         parm = method.Parameters(2)
                                         attrSym = parm.GetAttribute(iscSym)
                                         Assert.Equal(0, attrSym.CommonConstructorArguments.Length)

                                         ' class CBar
                                         Dim type2 = DirectCast(globalNS.GetTypeMember("CBar"), NamedTypeSymbol)
                                         Assert.Equal(2, type2.GetAttributes().Length)

                                         attrSym = type2.GetAttribute(serSym)
                                         Assert.Equal("SerializableAttribute", attrSym.AttributeClass.Name)

                                         attrSym = type2.GetAttribute(laySym)
                                         attrSym.VerifyValue(0, TypedConstantKind.Enum, CInt(LayoutKind.Explicit))
                                         Assert.Equal(3, attrSym.CommonNamedArguments.Length)
                                         attrSym.VerifyValue(0, "Size", TypedConstantKind.Primitive, 16)
                                         attrSym.VerifyValue(1, "Pack", TypedConstantKind.Primitive, 8)
                                         attrSym.VerifyValue(2, "CharSet", TypedConstantKind.Enum, CInt(CharSet.Unicode))

                                         Dim field = DirectCast(type2.GetMembers("field").First(), FieldSymbol)
                                         Assert.Equal(3, field.GetAttributes().Length)
                                         attrSym = field.GetAttribute(nosSym)
                                         Assert.Equal(0, attrSym.CommonConstructorArguments.Length)
                                         attrSym = field.GetAttribute(mshSym)
                                         attrSym.VerifyValue(0, TypedConstantKind.Enum, CInt(UnmanagedType.I8))
                                         attrSym = field.GetAttribute(offSym)
                                         attrSym.VerifyValue(0, TypedConstantKind.Primitive, 0)
                                     End Sub

            ' Verify attributes from source .
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        <Fact>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Sub SerializableFromPE()
            Dim lib_vb =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
<Serializable, Bob>
Public Class C
End Class

<AttributeUsage(AttributeTargets.Class)>
Public Class BobAttribute
    Inherits Attribute
End Class
    ]]></file>
</compilation>
            Dim lib_comp = CreateCompilationWithMscorlib40AndVBRuntime(lib_vb)

            Dim typeC As INamedTypeSymbol = lib_comp.GetTypeByMetadataName("C")
            AssertEx.SetEqual({"System.SerializableAttribute", "BobAttribute"}, typeC.GetAttributes().Select(Function(a) a.ToString()))
            Assert.True(typeC.IsSerializable)

            Dim typeBobAttribute As INamedTypeSymbol = lib_comp.GetTypeByMetadataName("BobAttribute")
            Assert.False(typeBobAttribute.IsSerializable)

            Dim empty_vb =
<compilation>
    <file name="attr.vb"></file>
</compilation>

            Dim client1 = CreateCompilationWithMscorlib40AndVBRuntime(empty_vb, additionalRefs:={lib_comp.ToMetadataReference()})

            Dim typeC1 As INamedTypeSymbol = client1.GetTypeByMetadataName("C")
            AssertEx.SetEqual({"System.SerializableAttribute", "BobAttribute"}, typeC1.GetAttributes().Select(Function(a) a.ToString()))
            Assert.True(typeC1.IsSerializable)

            Dim typeBobAttribute1 As INamedTypeSymbol = client1.GetTypeByMetadataName("BobAttribute")
            Assert.False(typeBobAttribute1.IsSerializable)

            Dim client2 = CreateCompilationWithMscorlib40AndVBRuntime(empty_vb, additionalRefs:={lib_comp.EmitToImageReference()})

            Dim typeC2 As INamedTypeSymbol = client2.GetTypeByMetadataName("C")
            AssertEx.SetEqual({"BobAttribute"}, typeC2.GetAttributes().Select(Function(a) a.ToString()))
            Assert.True(typeC2.IsSerializable)

            Dim typeBobAttribute2 As INamedTypeSymbol = client2.GetTypeByMetadataName("BobAttribute")
            Assert.False(typeBobAttribute2.IsSerializable)
        End Sub

        <Fact>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Sub SerializableSubstitutedType()
            Dim lib_vb =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System

<Serializable>
Public Class C(Of T)
End Class

' Not serializable
Public Class D(Of T)
End Class

Public Class C2
    Inherits C(Of Integer)
End Class
Public Class D2
    Inherits D(Of Integer)
End Class
            ]]></file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(lib_vb)

            Dim cOfInt = comp.GetTypeByMetadataName("C2").BaseType()
            Assert.IsType(GetType(SubstitutedNamedType.ConstructedInstanceType), cOfInt)
            Assert.True(DirectCast(cOfInt, INamedTypeSymbol).IsSerializable)
            Assert.True(DirectCast(cOfInt.ConstructedFrom, INamedTypeSymbol).IsSerializable)

            Dim dOfInt = comp.GetTypeByMetadataName("D2").BaseType()
            Assert.IsType(GetType(SubstitutedNamedType.ConstructedInstanceType), dOfInt)
            Assert.False(DirectCast(dOfInt, INamedTypeSymbol).IsSerializable)
            Assert.False(DirectCast(dOfInt.ConstructedFrom, INamedTypeSymbol).IsSerializable)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttribute()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    Sub Method(<DateTimeConstant(-1)> p1 As DateTime)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim symValidator As Action(Of ModuleSymbol) =
                Sub(peModule)

                    Dim bar = peModule.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Bar")
                    Dim method = bar.GetMember(Of MethodSymbol)("Method")
                    Dim parameters = method.Parameters
                    Dim theParameter = DirectCast(parameters(0), PEParameterSymbol)
                    Dim peModuleSymbol = DirectCast(peModule, PEModuleSymbol)

                    Assert.Equal(ParameterAttributes.None, theParameter.ParamFlags)

                    ' let's find the attribute in the PE metadata
                    Dim attributeInfo = CodeAnalysis.PEModule.FindTargetAttribute(peModuleSymbol.Module.MetadataReader, theParameter.Handle, AttributeDescription.DateTimeConstantAttribute)
                    Assert.True(attributeInfo.HasValue)

                    Dim attributeValue As Long
                    Assert.True(peModuleSymbol.Module.TryExtractLongValueFromAttribute(attributeInfo.Handle, attributeValue))
                    Assert.Equal(-1L, attributeValue)

                    ' check .param has no value
                    Dim constantHandle = peModuleSymbol.Module.MetadataReader.GetParameter(theParameter.Handle).GetDefaultValue()
                    Assert.True(constantHandle.IsNil)
                End Sub

            CompileAndVerify(source, symbolValidator:=symValidator)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttributeWithBadDefaultValue()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    Public Function Method(<DateTimeConstant(-1)> Optional p1 As DateTime = # 8/23/1970 3:45:39AM #) As DateTime
        Return p1
    End Function
    Public Shared Sub Main()
        Console.WriteLine(New Bar().Method().Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            ' The native VB compiler emits this:
            ' .method public instance void  Method([opt] valuetype [mscorlib]System.DateTime p1) cil managed
            ' {
            ' .param [1]
            ' .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute:: .ctor(Int64) = (1 00 80 73 3E 42 F6 37 A0 08 00 00 )
            ' .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute:: .ctor(Int64) = (1 00 FF FF FF FF FF FF FF FF 00 00 )

            ' Using the native compiler, the code would output 621558279390000000

            Dim comp = CreateCompilationWithMscorlib40(source)
            AssertTheseDiagnostics(comp,
                                   <expected><![CDATA[
BC37226: The parameter has multiple distinct default values.
    Public Function Method(<DateTimeConstant(-1)> Optional p1 As DateTime = # 8/23/1970 3:45:39AM #) As DateTime
                                                                            ~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttributeWithValidDefaultValue()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    Public Function Method(<DateTimeConstant(42)> Optional p1 As DateTime = # 8/23/1970 3:45:39AM #) As DateTime
        Return p1
    End Function
    Public Shared Sub Main()
        Console.WriteLine(New Bar().Method().Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            ' The native VB compiler emits this:
            ' .method public instance valuetype [mscorlib]System.DateTime
            ' Method([opt] valuetype [mscorlib]System.DateTime p1) cil managed
            ' {
            ' .param [1]
            ' .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute:: .ctor(Int64) = (1 00 2A 00 00 00 00 00 00 00 00 00 )
            ' .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute:: .ctor(Int64) = (1 00 80 73 3E 42 F6 37 A0 08 00 00 )

            ' Using the native compiler, the code would output 621558279390000000

            Dim comp = CreateCompilationWithMscorlib40(source)
            AssertTheseDiagnostics(comp,
                                   <expected><![CDATA[
BC37226: The parameter has multiple distinct default values.
    Public Function Method(<DateTimeConstant(42)> Optional p1 As DateTime = # 8/23/1970 3:45:39AM #) As DateTime
                                                                            ~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttributeWithBadDefaultValueOnField()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    <DateTimeConstant(-1)>
    Public Const F As DateTime = # 8/23/1970 3:45:39AM #

    Public Shared Sub Main()
        Console.WriteLine(Bar.F.Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            ' The native compiler would output 621558279390000000
            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<expected><![CDATA[
BC37228: The field has multiple distinct constant values.
    <DateTimeConstant(-1)>
     ~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttributeWithValidDefaultValueOnField()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    <DateTimeConstant(42)>
    Public Const F As DateTime = # 8/23/1970 3:45:39AM #

    Public Shared Sub Main()
        Console.WriteLine(Bar.F.Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            ' With the native VB compiler, this code outputs 621558279390000000
            Dim comp = CreateCompilationWithMscorlib40(source)
            comp.AssertTheseDiagnostics(<expected><![CDATA[
BC37228: The field has multiple distinct constant values.
    <DateTimeConstant(42)>
     ~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub DateTimeConstantAttributeReferencedViaRef()
            Dim source1 =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Public Class Bar
    Public Sub Method(<DateTimeConstant(-1)> p1 As DateTime)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim source2 =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class Consumer
    Public Shared Sub Main()
        Dim test = New Bar()
        test.Method()
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim libComp = CreateCompilationWithMscorlib40(source1)
            Dim libCompRef = New VisualBasicCompilationReference(libComp)

            Dim comp2 = CreateCompilationWithMscorlib40(source2, references:={libCompRef})
            AssertTheseDiagnostics(comp2,
                                   <expected><![CDATA[
BC30455: Argument not specified for parameter 'p1' of 'Public Sub Method(p1 As Date)'.
        test.Method()
             ~~~~~~
]]></expected>)

            Dim libAssemblyRef = libComp.EmitToImageReference()
            Dim comp3 = CreateCompilationWithMscorlib40(source2, references:={libAssemblyRef})
            AssertTheseDiagnostics(comp3,
                <expected><![CDATA[
BC30455: Argument not specified for parameter 'p1' of 'Public Sub Method(p1 As Date)'.
        test.Method()
             ~~~~~~
]]></expected>)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub LoadingDateTimeConstantWithBadValueOnField()
            Dim ilSource = <![CDATA[
.class public auto ansi C
       extends [mscorlib]System.Object
{
  .field public static initonly valuetype [mscorlib]System.DateTime F
  .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 ff ff ff ff ff ff ff ff 00 00 )
  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor
} // end of class C
                ]]>

            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class D
    Shared Sub Main()
        System.Console.WriteLine(C.F.Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            ' Using the native compiler, this code crashed
            Dim ilReference = CompileIL(ilSource.Value)
            Dim comp = CreateCompilationWithMscorlib40(source, references:={ilReference})
            AssertTheseDiagnostics(comp,
                <expected><![CDATA[
BC30799: Field 'C.F' has an invalid constant value.
        System.Console.WriteLine(C.F.Ticks)
                                 ~~~
]]></expected>)
        End Sub

        <WorkItem(217740, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=217740")>
        <Fact()>
        Public Sub LoadingDateTimeConstantWithBadValue()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method public hidebysig instance valuetype [mscorlib]System.DateTime
          Method([opt] valuetype [mscorlib]System.DateTime p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.CompilerServices.DateTimeConstantAttribute::.ctor(int64) = ( 01 00 FF FF FF FF FF FF FF FF 00 00 )
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (valuetype [mscorlib]System.DateTime V_0)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C::Method

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C
                ]]>

            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class D

    Shared Sub Main()
        System.Console.WriteLine(New C().Method().Ticks)
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim ilReference = CompileIL(ilSource.Value)
            CompileAndVerify(source, expectedOutput:="0", references:={ilReference})
            ' The native compiler would produce a working exe, but that exe would fail at runtime
        End Sub

        <WorkItem(531121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531121")>
        <Fact()>
        Public Sub TestDecimalConstantAttribute()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices
Imports System.Reflection

Module Form1
    Public Sub Main()
        For Each field In GetType(C).GetFields()
            PrintAttribute(field)
        Next
    End Sub

    Private Sub PrintAttribute(field As FieldInfo)
        Dim attr = field.GetCustomAttributesData()(0)
        Console.WriteLine("{0}, {1}, {2}, {3}, {4}",
                          attr.ConstructorArguments(0),
                          attr.ConstructorArguments(1),
                          attr.ConstructorArguments(2),
                          attr.ConstructorArguments(3),
                          attr.ConstructorArguments(4))
    End Sub
End Module

Public Class C
    Public Const _Min As Decimal = Decimal.MinValue
    Public Const _Max As Decimal = Decimal.MaxValue
    Public Const _One As Decimal = Decimal.One
    Public Const _MinusOne As Decimal = Decimal.MinusOne
    Public Const _Zero As Decimal = Decimal.Zero
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
(Byte)0, (Byte)128, (UInt32)4294967295, (UInt32)4294967295, (UInt32)4294967295
(Byte)0, (Byte)0, (UInt32)4294967295, (UInt32)4294967295, (UInt32)4294967295
(Byte)0, (Byte)0, (UInt32)0, (UInt32)0, (UInt32)1
(Byte)0, (Byte)128, (UInt32)0, (UInt32)0, (UInt32)1
(Byte)0, (Byte)0, (UInt32)0, (UInt32)0, (UInt32)0
]]>)
        End Sub

#End Region

#Region "DllImportAttribute, MethodImplAttribute, PreserveSigAttribute"
        ''' 6879: Pseudo DllImport looks very different in metadata: pinvokeimpl(...) +
        ''' PreserveSig
        <WorkItem(540573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540573")>
        <Fact>
        Public Sub TestPseudoDllImport()
            Dim source =
                <compilation>
                    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

''' PreserveSigAttribute: automatically insert by compiler
Public Class DllImportTest
    'Metadata - .method public static pinvokeimpl("unmanaged.dll" lasterr fastcall) 
    '            void  DllImportSub() cil managed preservesig
    <DllImport("unmanaged.dll", CallingConvention:=CallingConvention.FastCall, SetLastError:=True)>
    Public Shared Sub DllImportSub()
    End Sub

    ' Metadata  .method public static pinvokeimpl("user32.dll" unicode winapi) 
    '              int32  MessageBox(native int hwnd,  string t,  string caption, uint32 t2) cil managed preservesig
    '
    ' MSDN has table for 'default' ExactSpelling value
    '   C#|C++: always 'false'
    '   VB: true if CharSet is ANSI|UniCode; otherwise false
    <DllImport("user32.dll", CharSet:=CharSet.Unicode, ExactSpelling:=False, EntryPoint:="MessageBox")> _
    Shared Function MessageBox(ByVal hwnd As IntPtr, ByVal t As String, ByVal caption As String, ByVal t2 As UInt32) As Integer
    End Function
End Class
                ]]>
                    </file>
                </compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim assembly = m.ContainingAssembly
                    Dim compilation = m.DeclaringCompilation
                    Dim globalNS = If(compilation Is Nothing, assembly.CorLibrary.GlobalNamespace, compilation.GlobalNamespace)
                    Dim sysNS = globalNS.GetMember(Of NamespaceSymbol)("System")

                    ' get expected attr symbol
                    Dim runtimeNS = sysNS.GetNamespace("Runtime")
                    Dim interopNS = runtimeNS.GetNamespace("InteropServices")
                    Dim compsrvNS = runtimeNS.GetNamespace("CompilerServices")

                    Dim type1 = m.GlobalNamespace.GetTypeMember("DllImportTest")

                    Dim method As MethodSymbol
                    method = type1.GetMember(Of MethodSymbol)("DllImportSub")
                    Dim attrSym = method.GetAttributes().First()
                    Assert.Equal("DllImportAttribute", attrSym.AttributeClass.Name)
                    Assert.Equal("unmanaged.dll", attrSym.CommonConstructorArguments(0).Value)

                    Assert.Equal("CallingConvention", attrSym.CommonNamedArguments(0).Key)
                    Assert.Equal(TypedConstantKind.Enum, attrSym.CommonNamedArguments(0).Value.Kind)
                    Assert.Equal(CallingConvention.FastCall, CType(attrSym.CommonNamedArguments(0).Value.Value, CallingConvention))
                    Assert.Equal("SetLastError", attrSym.CommonNamedArguments(1).Key)
                    Assert.Equal(True, attrSym.CommonNamedArguments(1).Value.Value)

                    method = DirectCast(type1.GetMembers("MessageBox").First(), MethodSymbol)
                    attrSym = method.GetAttributes().First()
                    Assert.Equal("DllImportAttribute", attrSym.AttributeClass.Name)
                    Assert.Equal("user32.dll", attrSym.CommonConstructorArguments(0).Value)

                    Assert.Equal("CharSet", attrSym.CommonNamedArguments(0).Key)
                    Assert.Equal(TypedConstantKind.Enum, attrSym.CommonNamedArguments(0).Value.Kind)
                    Assert.Equal(CharSet.Unicode, CType(attrSym.CommonNamedArguments(0).Value.Value, CharSet))
                    Assert.Equal("ExactSpelling", attrSym.CommonNamedArguments(1).Key)
                    Assert.Equal(TypedConstantKind.Primitive, attrSym.CommonNamedArguments(1).Value.Kind)
                    Assert.Equal(False, attrSym.CommonNamedArguments(1).Value.Value)

                End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        <Fact>
        Public Sub DllImport_AttributeRedefinition()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Namespace System.Runtime.InteropServices

    <DllImport>
    Public Class DllImportAttribute
    End Class

End Namespace
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_AttributeMustInheritSysAttr, "DllImport").WithArguments("System.Runtime.InteropServices.DllImportAttribute"))
        End Sub

        <Fact>
        Public Sub DllImport_InvalidArgs1()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Strings

Class C
    <DllImport(Nothing)>
    Public Shared Sub F1()
    End Sub

    <DllImport("")>
    Public Shared Sub F2()
    End Sub

    <DllImport("goo", EntryPoint:=Nothing)>
    Public Shared Sub F3()
    End Sub

    <DllImport("goo", EntryPoint:="")>
    Public Shared Sub F4()
    End Sub

    <DllImport(Nothing, EntryPoint:=Nothing)>
    Public Shared Sub F5()
    End Sub

    <DllImport(ChrW(0))>
    Public Shared Sub Empty1()
    End Sub

    <DllImport(ChrW(0) & "b")>
    Public Shared Sub Empty2()
    End Sub

    <DllImport("b" & ChrW(0))>
    Public Shared Sub Empty3()
    End Sub

    <DllImport("x" & ChrW(0) & "y")>
    Public Shared Sub Empty4()
    End Sub

    <DllImport("x", EntryPoint:="x" & ChrW(0) & "y")>
    Public Shared Sub Empty5()
    End Sub

    <DllImport(ChrW(&H800))>
    Public Shared Sub LeadingSurrogate()
    End Sub

    <DllImport(ChrW(&HDC00))>
    Public Shared Sub TrailingSurrogate()
    End Sub

    <DllImport(ChrW(&HDC00) & ChrW(&HD800))>
    Public Shared Sub ReversedSurrogates1()
    End Sub

    <DllImport("x", EntryPoint:=ChrW(&HDC00) & ChrW(&HD800))>
    Public Shared Sub ReversedSurrogates2()
    End Sub
End Class
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadAttribute1, "Nothing").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, """""").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "EntryPoint:=Nothing").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "EntryPoint:=""""").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "Nothing").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "EntryPoint:=Nothing").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ChrW(0)").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ChrW(0) & ""b""").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, """b"" & ChrW(0)").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, """x"" & ChrW(0) & ""y""").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "EntryPoint:=""x"" & ChrW(0) & ""y""").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ChrW(&HDC00)").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "ChrW(&HDC00) & ChrW(&HD800)").WithArguments("System.Runtime.InteropServices.DllImportAttribute"),
                Diagnostic(ERRID.ERR_BadAttribute1, "EntryPoint:=ChrW(&HDC00) & ChrW(&HD800)").WithArguments("System.Runtime.InteropServices.DllImportAttribute"))
        End Sub

        <Fact>
        Public Sub DllImport_SpecialCharactersInName()
            Dim source =
    <compilation>
        <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Strings

Class Program
    <DllImport(ChrW(&HFFFF))>
    Shared Sub InvalidCharacter()
    End Sub

    <DllImport(ChrW(&HD800) & ChrW(&HDC00))>
    Shared Sub SurrogatePairMin()
    End Sub

    <DllImport(ChrW(&HDBFF) & ChrW(&HDFFF))>
    Shared Sub SurrogatePairMax()
    End Sub
End Class
]]>
        </file>
    </compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(3, reader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(3, reader.GetTableRowCount(TableIndex.ImplMap))

                    For Each method In reader.GetImportedMethods()
                        Dim import = method.GetImport()
                        Dim moduleName As String = reader.GetString(reader.GetModuleReference(import.Module).Name)
                        Dim methodName As String = reader.GetString(method.Name)
                        Select Case methodName
                            Case "InvalidCharacter"
                                Assert.Equal(ChrW(&HFFFF), moduleName)

                            Case "SurrogatePairMin"
                                Assert.Equal(ChrW(&HD800) & ChrW(&HDC00), moduleName)

                            Case "SurrogatePairMax"
                                Assert.Equal(ChrW(&HDBFF) & ChrW(&HDFFF), moduleName)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(methodName)
                        End Select
                    Next
                End Sub)
        End Sub

        <Fact>
        Public Sub DllImport_TypeCharacterInName()
            Dim source =
 <compilation>
     <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Module Module1
    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Function MessageBox%(hwnd As IntPtr, t As String, caption As String, t2 As UInt32)
    End Function
End Module
 ]]>
     </file>
 </compilation>

            Dim attributeValidator =
                Sub(m As ModuleSymbol)
                    Dim type1 = m.GlobalNamespace.GetTypeMember("Module1")

                    Dim method = DirectCast(type1.GetMembers("MessageBox").First(), MethodSymbol)
                    Dim attrSym = method.GetAttributes().First()
                    Assert.Equal("DllImportAttribute", attrSym.AttributeClass.Name)
                    Assert.Equal("user32.dll", attrSym.CommonConstructorArguments(0).Value)

                    Assert.Equal("CharSet", attrSym.CommonNamedArguments(0).Key)
                    Assert.Equal(TypedConstantKind.Enum, attrSym.CommonNamedArguments(0).Value.Kind)
                    Assert.Equal(CharSet.Unicode, CType(attrSym.CommonNamedArguments(0).Value.Value, CharSet))
                End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        <Fact()>
        <WorkItem(544176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544176")>
        Public Sub TestPseudoAttributes_DllImport_AllTrue()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("mscorlib", EntryPoint:="bar", CallingConvention:=CallingConvention.Cdecl, CharSet:=CharSet.Unicode, ExactSpelling:=True, PreserveSig:=True, SetLastError:=True, BestFitMapping:=True, ThrowOnUnmappableChar:=True)>
    Public Shared Sub M()
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()

                    ' ModuleRef:
                    Dim moduleRefName = reader.GetModuleReference(reader.GetModuleReferences().Single()).Name
                    Assert.Equal("mscorlib", reader.GetString(moduleRefName))

                    ' FileRef:
                    ' Although the Metadata spec says there should be a File entry for each ModuleRef entry 
                    ' Dev10 compiler doesn't add it and peverify doesn't complain.
                    Assert.Equal(0, reader.GetTableRowCount(TableIndex.File))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ImplMap))

                    ' ImplMap:
                    Dim import = reader.GetImportedMethods().Single().GetImport()
                    Assert.Equal("bar", reader.GetString(import.Name))
                    Assert.Equal(1, reader.GetRowNumber(import.Module))
                    Assert.Equal(MethodImportAttributes.ExactSpelling Or
                                 MethodImportAttributes.CharSetUnicode Or
                                 MethodImportAttributes.SetLastError Or
                                 MethodImportAttributes.CallingConventionCDecl Or
                                 MethodImportAttributes.BestFitMappingEnable Or
                                 MethodImportAttributes.ThrowOnUnmappableCharEnable, import.Attributes)

                    ' MethodDef:
                    Dim methodDefs As MethodDefinitionHandle() = reader.MethodDefinitions.AsEnumerable().ToArray()
                    Assert.Equal(2, methodDefs.Length) ' ctor, M
                    Assert.Equal(MethodImplAttributes.PreserveSig, reader.GetMethodDefinition(methodDefs(1)).ImplAttributes)
                End Sub

            Dim symValidator As Action(Of ModuleSymbol) =
                Sub(peModule)

                    Dim c = peModule.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                    Dim m = c.GetMember(Of MethodSymbol)("M")
                    Dim info = m.GetDllImportData()
                    Assert.Equal("mscorlib", info.ModuleName)
                    Assert.Equal("bar", info.EntryPointName)
                    Assert.Equal(CharSet.Unicode, info.CharacterSet)
                    Assert.True(info.ExactSpelling)
                    Assert.True(info.SetLastError)
                    Assert.Equal(True, info.BestFitMapping)
                    Assert.Equal(True, info.ThrowOnUnmappableCharacter)

                    Assert.Equal(
                        MethodImportAttributes.ExactSpelling Or
                        MethodImportAttributes.CharSetUnicode Or
                        MethodImportAttributes.SetLastError Or
                        MethodImportAttributes.CallingConventionCDecl Or
                        MethodImportAttributes.BestFitMappingEnable Or
                        MethodImportAttributes.ThrowOnUnmappableCharEnable, DirectCast(info, Cci.IPlatformInvokeInformation).Flags)
                End Sub

            CompileAndVerify(source, validator:=validator, symbolValidator:=symValidator)
        End Sub

        <Fact>
        <WorkItem(544601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544601")>
        Public Sub GetDllImportData_UnspecifiedProperties()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C
    <DllImport("mscorlib")>
    Shared Sub M()
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim validator As Func(Of Boolean, Action(Of ModuleSymbol)) =
                Function(isFromSource As Boolean) _
                    Sub([module] As ModuleSymbol)
                        Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                        Dim m = c.GetMember(Of MethodSymbol)("M")
                        Dim info = m.GetDllImportData()
                        Assert.Equal("mscorlib", info.ModuleName)
                        Assert.Equal(If(isFromSource, Nothing, "M"), info.EntryPointName)
                        Assert.Equal(CharSet.None, info.CharacterSet)
                        Assert.Equal(CallingConvention.Winapi, info.CallingConvention)
                        Assert.False(info.ExactSpelling)
                        Assert.False(info.SetLastError)
                        Assert.Equal(Nothing, info.BestFitMapping)
                        Assert.Equal(Nothing, info.ThrowOnUnmappableCharacter)
                    End Sub

            CompileAndVerify(source, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        <WorkItem(544601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544601")>
        Public Sub GetDllImportData_Declare()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C
    Declare Unicode Sub M1 Lib "goo"()
    Declare Unicode Sub M2 Lib "goo" Alias "bar"()
End Class
]]>
    </file>
</compilation>

            Dim validator =
                Function(isFromSource As Boolean) _
                    Sub([module] As ModuleSymbol)
                        Dim c = [module].GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

                        Dim info = c.GetMember(Of MethodSymbol)("M1").GetDllImportData()
                        Assert.Equal("goo", info.ModuleName)
                        Assert.Equal(If(isFromSource, Nothing, "M1"), info.EntryPointName)
                        Assert.Equal(CharSet.Unicode, info.CharacterSet)
                        Assert.Equal(CallingConvention.Winapi, info.CallingConvention)
                        Assert.True(info.ExactSpelling)
                        Assert.True(info.SetLastError)
                        Assert.Equal(Nothing, info.BestFitMapping)
                        Assert.Equal(Nothing, info.ThrowOnUnmappableCharacter)

                        info = c.GetMember(Of MethodSymbol)("M2").GetDllImportData()
                        Assert.Equal("goo", info.ModuleName)
                        Assert.Equal("bar", info.EntryPointName)
                        Assert.Equal(CharSet.Unicode, info.CharacterSet)
                        Assert.Equal(CallingConvention.Winapi, info.CallingConvention)
                        Assert.True(info.ExactSpelling)
                        Assert.True(info.SetLastError)
                        Assert.Equal(Nothing, info.BestFitMapping)
                        Assert.Equal(Nothing, info.ThrowOnUnmappableCharacter)
                    End Sub

            CompileAndVerify(source, sourceSymbolValidator:=validator(True), symbolValidator:=validator(False))
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_DllImport_Operators()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("goo")>
    Public Shared Operator +(a As C, b As C) As Integer
    End Operator
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ImplMap))

                    Dim method = reader.GetImportedMethods().Single()
                    Dim import = method.GetImport()
                    Dim moduleName As String = reader.GetString(reader.GetModuleReference(import.Module).Name)
                    Dim entryPointName As String = reader.GetString(method.Name)

                    Assert.Equal("op_Addition", entryPointName)
                    Assert.Equal("goo", moduleName)
                End Sub)
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_DllImport_Conversions()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("goo")>
    Public Shared Narrowing Operator CType(a As C) As Integer
    End Operator
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()
                    Assert.Equal(1, peFileReader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(1, peFileReader.GetTableRowCount(TableIndex.ImplMap))

                    Dim method = peFileReader.GetImportedMethods().Single()
                    Dim moduleName As String = peFileReader.GetString(peFileReader.GetModuleReference(method.GetImport().Module).Name)
                    Dim entryPointName As String = peFileReader.GetString(method.Name)

                    Assert.Equal("op_Explicit", entryPointName)
                    Assert.Equal("goo", moduleName)
                End Sub)
        End Sub

        <Fact>
        Public Sub DllImport_Partials()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("module name")>
    Shared Partial Private Sub goo()
    End Sub
 
    Shared Private Sub goo()
    End Sub
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ImplMap))

                    Dim method = reader.GetImportedMethods().Single()
                    Dim moduleName As String = reader.GetString(reader.GetModuleReference(method.GetImport().Module).Name)
                    Dim entryPointName As String = reader.GetString(method.Name)

                    Assert.Equal("module name", moduleName)
                    Assert.Equal("goo", entryPointName)
                End Sub)
        End Sub

        <Fact>
        Public Sub DllImport_Partials_Errors()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("module name")>
    Partial Private Sub goo()
    End Sub
 
    Private Sub goo()
    End Sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_DllImportOnInstanceMethod, "DllImport"))
        End Sub

        <Fact>
        Public Sub DllImport_Partials_NonEmptyBody()
            Dim source =
<compilation>
    <file><![CDATA[
Module Module1
    <System.Runtime.InteropServices.DllImport("a")>
    Private Sub f1()
    End Sub

    Partial Private Sub f1()
    End Sub

    <System.Runtime.InteropServices.DllImport("a")>
    Partial Private Sub f2()
    End Sub

    Private Sub f2()
        System.Console.WriteLine()
    End Sub
End Module
]]>
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_DllImportOnNonEmptySubOrFunction, "System.Runtime.InteropServices.DllImport"))

        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_DllImport_NotAllowed()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

Public Class C
    Public Shared Property F As Integer
        <DllImport("a")>
        Get
            Return 1
        End Get

        <DllImport(Nothing)>
        Set(value As Integer)

        End Set
    End Property

    Custom Event x As Action(Of Integer)
        <DllImport("goo")>
        AddHandler(value As Action(Of Integer))
        End AddHandler

        <DllImport("goo")>
        RemoveHandler(value As Action(Of Integer))
        End RemoveHandler

        <DllImport("goo")>
        RaiseEvent(obj As Integer)
        End RaiseEvent
    End Event

    <DllImport("goo")>
    Sub InstanceMethod
    End Sub

    <DllImport("goo")>
    Shared Sub NonEmptyBody
       System.Console.WriteLine() 
    End Sub

    <DllImport("goo")>
    Shared Sub GenericMethod(Of T)()
    End Sub
End Class

Interface I
    <DllImport("goo")>
    Sub InterfaceMethod()
End Interface

Interface I(Of T)
    <DllImport("goo")>
    Sub InterfaceMethod()
End Interface

Class C(Of T)
    Interface Goo
        Class D
            <DllImport("goo")>
            Shared Sub MethodOnGenericType()
            End Sub
        End Class
    End Interface
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_DllImportNotLegalOnGetOrSet, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportNotLegalOnGetOrSet, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportNotLegalOnEventMethod, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportNotLegalOnEventMethod, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportNotLegalOnEventMethod, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnInstanceMethod, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnNonEmptySubOrFunction, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnGenericSubOrFunction, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnGenericSubOrFunction, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnInterfaceMethod, "DllImport"),
                Diagnostic(ERRID.ERR_DllImportOnInterfaceMethod, "DllImport"))
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_DllImport_Flags()
            Dim cases =
            {
                New With {.n = 0, .attr = MakeDllImport(), .expected = MethodImportAttributes.CallingConventionWinApi},
                New With {.n = 1, .attr = MakeDllImport(cc:=CallingConvention.Cdecl), .expected = MethodImportAttributes.CallingConventionCDecl},
                New With {.n = 2, .attr = MakeDllImport(cc:=CallingConvention.FastCall), .expected = MethodImportAttributes.CallingConventionFastCall},
                New With {.n = 3, .attr = MakeDllImport(cc:=CallingConvention.StdCall), .expected = MethodImportAttributes.CallingConventionStdCall},
                New With {.n = 4, .attr = MakeDllImport(cc:=CallingConvention.ThisCall), .expected = MethodImportAttributes.CallingConventionThisCall},
                New With {.n = 5, .attr = MakeDllImport(cc:=CallingConvention.Winapi), .expected = MethodImportAttributes.CallingConventionWinApi},
                                                                                                                                                   _
                New With {.n = 6, .attr = MakeDllImport(), .expected = MethodImportAttributes.CallingConventionWinApi},
                New With {.n = 7, .attr = MakeDllImport(charSet:=CharSet.None), .expected = MethodImportAttributes.CallingConventionWinApi},
                New With {.n = 8, .attr = MakeDllImport(charSet:=CharSet.Ansi), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetAnsi},
                New With {.n = 9, .attr = MakeDllImport(charSet:=CharSet.Unicode), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetUnicode},
                New With {.n = 10, .attr = MakeDllImport(charSet:=CharSet.Auto), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetAuto},
                                                                                                                                                                                   _
                New With {.n = 11, .attr = MakeDllImport(exactSpelling:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ExactSpelling},
                New With {.n = 12, .attr = MakeDllImport(exactSpelling:=False), .expected = MethodImportAttributes.CallingConventionWinApi},
                                                                                                                                            _
                New With {.n = 13, .attr = MakeDllImport(charSet:=CharSet.Ansi, exactSpelling:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetAnsi},
                New With {.n = 14, .attr = MakeDllImport(charSet:=CharSet.Ansi, exactSpelling:=False), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetAnsi},
                New With {.n = 15, .attr = MakeDllImport(charSet:=CharSet.Unicode, exactSpelling:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetUnicode},
                New With {.n = 16, .attr = MakeDllImport(charSet:=CharSet.Unicode, exactSpelling:=False), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetUnicode},
                New With {.n = 17, .attr = MakeDllImport(charSet:=CharSet.Auto, exactSpelling:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetAuto},
                New With {.n = 18, .attr = MakeDllImport(charSet:=CharSet.Auto, exactSpelling:=False), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.CharSetAuto},
                                                                                                                                                                                                         _
                New With {.n = 19, .attr = MakeDllImport(preserveSig:=True), .expected = MethodImportAttributes.CallingConventionWinApi},
                New With {.n = 20, .attr = MakeDllImport(preserveSig:=False), .expected = MethodImportAttributes.CallingConventionWinApi},
                                                                                                                                          _
                New With {.n = 21, .attr = MakeDllImport(setLastError:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.SetLastError},
                New With {.n = 22, .attr = MakeDllImport(setLastError:=False), .expected = MethodImportAttributes.CallingConventionWinApi},
                                                                                                                                           _
                New With {.n = 23, .attr = MakeDllImport(bestFitMapping:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.BestFitMappingEnable},
                New With {.n = 24, .attr = MakeDllImport(bestFitMapping:=False), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.BestFitMappingDisable},
                                                                                                                                                                                             _
                New With {.n = 25, .attr = MakeDllImport(throwOnUnmappableChar:=True), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ThrowOnUnmappableCharEnable},
                New With {.n = 26, .attr = MakeDllImport(throwOnUnmappableChar:=False), .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.ThrowOnUnmappableCharDisable},
                                                                                                                                                                                                           _
                New With {.n = 27, .attr = "<DllImport(""bar"", CharSet:=CType(15, CharSet), SetLastError:=True)>", .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.SetLastError},
                New With {.n = 28, .attr = "<DllImport(""bar"", CallingConvention:=CType(15, CallingConvention), SetLastError:=True)>", .expected = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.SetLastError}
            }

            ' NOTE: case #28 - when an invalid calling convention is specified Dev10 compiler emits invalid metadata (calling convention 0). 
            ' We emit calling convention WinAPI.

            Dim sb As StringBuilder = New StringBuilder(
<text>
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
</text>.Value)

            For Each testCase In cases
                sb.Append(testCase.attr)
                sb.AppendLine()
                sb.AppendLine("Shared Sub M" & testCase.n & "()")
                sb.AppendLine("End Sub")
            Next

            sb.AppendLine("End Class")
            Dim code = <compilation><file name="attr.vb"><%= sb.ToString() %></file></compilation>

            CompileAndVerify(code, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(cases.Length, reader.GetTableRowCount(TableIndex.ImplMap))
                    Dim j = 0
                    For Each method In reader.GetImportedMethods()
                        Assert.Equal(cases(j).expected, method.GetImport().Attributes)
                        j = j + 1
                    Next
                End Sub)
        End Sub

        Private Function MakeDllImport(Optional cc As CallingConvention? = Nothing, Optional charSet As CharSet? = Nothing, Optional exactSpelling As Boolean? = Nothing, Optional preserveSig As Boolean? = Nothing, Optional setLastError As Boolean? = Nothing, Optional bestFitMapping As Boolean? = Nothing, Optional throwOnUnmappableChar As Boolean? = Nothing) As String
            Dim sb As StringBuilder = New StringBuilder("<DllImport(""bar""")
            If cc IsNot Nothing Then
                sb.Append(", CallingConvention := CallingConvention.")
                sb.Append(cc.Value.ToString())
            End If

            If charSet IsNot Nothing Then
                sb.Append(", CharSet := CharSet.")
                sb.Append(charSet.Value.ToString())
            End If

            If exactSpelling IsNot Nothing Then
                sb.Append(", ExactSpelling := ")
                sb.Append(If(exactSpelling.Value, "True", "False"))
            End If

            If preserveSig IsNot Nothing Then
                sb.Append(", PreserveSig := ")
                sb.Append(If(preserveSig.Value, "True", "False"))
            End If

            If setLastError IsNot Nothing Then
                sb.Append(", SetLastError := ")
                sb.Append(If(setLastError.Value, "True", "False"))
            End If

            If bestFitMapping IsNot Nothing Then
                sb.Append(", BestFitMapping := ")
                sb.Append(If(bestFitMapping.Value, "True", "False"))
            End If

            If throwOnUnmappableChar IsNot Nothing Then
                sb.Append(", ThrowOnUnmappableChar := ")
                sb.Append(If(throwOnUnmappableChar.Value, "True", "False"))
            End If

            sb.Append(")>")
            Return sb.ToString()
        End Function

        <Fact>
        Public Sub TestMethodImplAttribute_VerifiableMD()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

MustInherit Class C

    <MethodImpl(MethodImplOptions.ForwardRef)>
    Public Shared Sub ForwardRef()
        System.Console.WriteLine(0)
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub NoInlining()
        System.Console.WriteLine(1)
    End Sub

    <MethodImpl(MethodImplOptions.NoOptimization)>
    Public Shared Sub NoOptimization()
        System.Console.WriteLine(2)
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized)>
    Public Shared Sub Synchronized()
        System.Console.WriteLine(3)
    End Sub

    <MethodImpl(MethodImplOptions.InternalCall)>                    ' ok, body ignored
    Public Shared Sub InternalCallStatic()                      
        System.Console.WriteLine(3)
    End Sub

    <MethodImpl(MethodImplOptions.InternalCall)>                    ' ok, body ignored
    Public Sub InternalCallInstance()
        System.Console.WriteLine(3)
    End Sub

    <MethodImpl(MethodImplOptions.InternalCall)>
    Public MustOverride Sub InternalCallAbstract()
End Class
]]>
    </file>
</compilation>

            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim peReader = assembly.GetMetadataReader()

                    For Each methodDef In peReader.MethodDefinitions
                        Dim row = peReader.GetMethodDefinition(methodDef)
                        Dim actualFlags = row.ImplAttributes
                        Dim expectedFlags As MethodImplAttributes

                        Select Case peReader.GetString(row.Name)
                            Case "NoInlining"
                                expectedFlags = MethodImplAttributes.NoInlining

                            Case "NoOptimization"
                                expectedFlags = MethodImplAttributes.NoOptimization

                            Case "Synchronized"
                                expectedFlags = MethodImplAttributes.Synchronized

                            Case "InternalCallStatic", "InternalCallInstance", "InternalCallAbstract"
                                expectedFlags = MethodImplAttributes.InternalCall

                            Case "ForwardRef"
                                expectedFlags = MethodImplAttributes.ForwardRef

                            Case ".ctor"
                                expectedFlags = MethodImplAttributes.IL

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(peReader.GetString(row.Name))
                        End Select

                        Assert.Equal(expectedFlags, actualFlags)
                    Next
                End Sub

            CompileAndVerify(source, validator:=validator)
        End Sub

        <Fact>
        Public Sub TestMethodImplAttribute_UnverifiableMD()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class C

    <MethodImpl(MethodImplOptions.Unmanaged)>                       ' peverify: type load failed
    Public Shared Sub Unmanaged()
        System.Console.WriteLine(1)
    End Sub                                                         

    <MethodImpl(MethodCodeType:=MethodCodeType.Native)>             ' peverify: type load failed
    Public Shared Sub Native()                                      
        System.Console.WriteLine(2)
    End Sub

    <MethodImpl(MethodCodeType:=MethodCodeType.OPTIL)>              ' peverify: type load failed
    Public Shared Sub OPTIL()
        System.Console.WriteLine(3)                                 
    End Sub

    <MethodImpl(MethodCodeType:=MethodCodeType.Runtime)>            ' peverify: type load failed
    Public Shared Sub Runtime()
        System.Console.WriteLine(4)
    End Sub

    <MethodImpl(MethodImplOptions.InternalCall)>
    Public Shared Sub InternalCallGeneric1(Of T)()                  ' peverify: type load failed (InternalCall method can't be generic)
    End Sub
End Class

Class C(Of T)

    <MethodImpl(MethodImplOptions.InternalCall)>
    Public Shared Sub InternalCallGeneric2()                        ' peverify: type load failed (InternalCall method can't be in a generic type)
    End Sub
End Class
]]>
    </file>
</compilation>)

            Dim image = compilation.EmitToArray()
            Dim peReader = ModuleMetadata.CreateFromImage(image).Module.GetMetadataReader()

            For Each methodDef In peReader.MethodDefinitions
                Dim row = peReader.GetMethodDefinition(methodDef)
                Dim actualFlags = row.ImplAttributes
                Dim actualHasBody = row.RelativeVirtualAddress <> 0

                Dim expectedFlags As MethodImplAttributes
                Dim expectedHasBody As Boolean
                Select Case peReader.GetString(row.Name)
                    Case "ForwardRef"
                        expectedFlags = MethodImplAttributes.ForwardRef
                        expectedHasBody = True

                    Case "Unmanaged"
                        expectedFlags = MethodImplAttributes.Unmanaged
                        expectedHasBody = True

                    Case "Native"
                        expectedFlags = MethodImplAttributes.Native
                        expectedHasBody = True

                    Case "Runtime"
                        expectedFlags = MethodImplAttributes.Runtime
                        expectedHasBody = False

                    Case "OPTIL"
                        expectedFlags = MethodImplAttributes.OPTIL
                        expectedHasBody = True

                    Case "InternalCallStatic", "InternalCallGeneric1", "InternalCallGeneric2"
                        expectedFlags = MethodImplAttributes.InternalCall
                        expectedHasBody = False

                    Case ".ctor"
                        expectedFlags = MethodImplAttributes.IL
                        expectedHasBody = True

                    Case Else
                        Throw TestExceptionUtilities.UnexpectedValue(peReader.GetString(row.Name))
                End Select

                Assert.Equal(expectedFlags, actualFlags)
                Assert.Equal(expectedHasBody, actualHasBody)
            Next
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_DllImport_Declare()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.InteropServices

Public Class C
    <DllImport("Baz")>
    Declare Ansi Sub Goo Lib "Goo" Alias "bar" ()
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_DllImportNotLegalOnDeclare, "DllImport"))
        End Sub

        <Fact>
        Public Sub ExternalExtensionMethods()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Module M
    <Extension()>
    <MethodImpl(MethodImplOptions.InternalCall)>
    Sub InternalCall(a As Integer)
    End Sub

    <Extension()>
    <DllImport("goo")>
    Sub DllImp(a As Integer)
    End Sub

    <Extension()>
    Declare Sub DeclareSub Lib "bar" (a As Integer)
End Module

Class C
    Shared Sub Main()
        Dim x = 1
        x.DeclareSub()
        x.DllImp()
        x.InternalCall()
    End Sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {TestMetadata.Net40.SystemCore}).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestMethodImplAttribute_PreserveSig()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

MustInherit Class C
    Sub New
    End Sub

    <PreserveSig>
    MustOverride Public Sub f0()

    <MethodImpl(MethodImplOptions.PreserveSig)>
    MustOverride Public Sub f1()

    <DllImport("goo")>
    Public Shared Sub f2()
    End Sub

    <DllImport("goo", PreserveSig:=True)>
    Public Shared Sub f3()
    End Sub

    <DllImport("goo", PreserveSig:=False)>
    Public Shared Sub f4()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), DllImport("goo", PreserveSig:=True)>
    Public Shared Sub f5()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), DllImport("goo", PreserveSig:=False)>
    Public Shared Sub f6()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), PreserveSig>
    MustOverride Public Sub f7()

    <DllImport("goo"), PreserveSig>
    Public Shared Sub f8()
    End Sub

    <PreserveSig, DllImport("goo", PreserveSig:=True)>
    Public Shared Sub f9()
    End Sub

    ' false
    <DllImport("goo", PreserveSig:=False), PreserveSig>
    Public Shared Sub f10()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), DllImport("goo", PreserveSig:=True), PreserveSig>
    Public Shared Sub f11()
    End Sub

    ' false
    <DllImport("goo", PreserveSig:=False), PreserveSig, MethodImpl(MethodImplOptions.PreserveSig)>
    Public Shared Sub f12()
    End Sub

    ' false
    <DllImport("goo", PreserveSig:=False), MethodImpl(MethodImplOptions.PreserveSig), PreserveSig>
    Public Shared Sub f13()
    End Sub

    <PreserveSig, DllImport("goo", PreserveSig:=False), MethodImpl(MethodImplOptions.PreserveSig)>
    Public Shared Sub f14()
    End Sub

    <PreserveSig, MethodImpl(MethodImplOptions.PreserveSig), DllImport("goo", PreserveSig:=False)>
    Public Shared Sub f15()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), PreserveSig, DllImport("goo", PreserveSig:=False)>
    Public Shared Sub f16()
    End Sub

    <MethodImpl(MethodImplOptions.PreserveSig), DllImport("goo", PreserveSig:=False), PreserveSig>
    Public Shared Sub f17()
    End Sub

    ' false
    Public Shared Sub f18()
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized), DllImport("goo", PreserveSig:=False), PreserveSig>
    Public Shared Sub f19()
    End Sub

    <MethodImpl(MethodImplOptions.Synchronized), PreserveSig>
    Public Shared Sub f20()
    End Sub
End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim peReader = assembly.GetMetadataReader()
                    For Each methodDef In peReader.MethodDefinitions
                        Dim row = peReader.GetMethodDefinition(methodDef)
                        Dim actualFlags = row.ImplAttributes
                        Dim expectedFlags As MethodImplAttributes
                        Dim name = peReader.GetString(row.Name)

                        Select Case name
                            Case "f0", "f1", "f2", "f3", "f5", "f6", "f7", "f8", "f9", "f11", "f14", "f15", "f16", "f17"
                                expectedFlags = MethodImplAttributes.PreserveSig

                            Case "f4", "f10", "f12", "f13", "f18", ".ctor"
                                expectedFlags = 0

                            Case "f19"
                                expectedFlags = MethodImplAttributes.Synchronized

                            Case "f20"
                                expectedFlags = MethodImplAttributes.PreserveSig Or MethodImplAttributes.Synchronized

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select

                        Assert.Equal(expectedFlags, actualFlags)
                    Next

                    ' no custom attributes applied on methods:
                    For Each ca In peReader.CustomAttributes
                        Dim parent = peReader.GetCustomAttribute(ca).Parent
                        Assert.NotEqual(parent.Kind, HandleKind.MethodDefinition)
                    Next
                End Sub)
        End Sub

        <Fact>
        Public Sub MethodImplAttribute_Errors()
            Dim source =
<compilation>
    <file><![CDATA[                
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Class Program1

    <MethodImpl(CShort(0))>
    Sub f0()
    End Sub

    <MethodImpl(CShort(1))>
    Sub f1()
    End Sub

    <MethodImpl(CShort(2))>
    Sub f2()
    End Sub

    <MethodImpl(CShort(3))>
    Sub f3()
    End Sub

    <MethodImpl(CShort(4))>
    Sub f4()
    End Sub

    <MethodImpl(CShort(5))>
    Sub f5()
    End Sub

    <MethodImpl(CType(2, MethodImplOptions))>
    Sub f6()
    End Sub

    <MethodImpl(CShort(4), MethodCodeType:=CType(8, MethodCodeType), MethodCodeType:=CType(9, MethodCodeType))>
    Sub f7()
    End Sub
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(1))>
                ~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(2))>
                ~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(3))>
                ~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(5))>
                ~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CType(2, MethodImplOptions))>
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(4), MethodCodeType:=CType(8, MethodCodeType), MethodCodeType:=CType(9, MethodCodeType))>
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <MethodImpl(CShort(4), MethodCodeType:=CType(8, MethodCodeType), MethodCodeType:=CType(9, MethodCodeType))>
                                                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        Private Sub DisableJITOptimizationTestHelper(
            assembly As PEAssembly,
            methods As String(),
            implFlags As MethodImplAttributes()
        )
            Dim m = assembly.Modules(0)
            Dim disableOptDef As TypeDefinitionHandle = Nothing
            Dim name As String = Nothing

            For Each typeDef In m.GetMetadataReader().TypeDefinitions
                name = m.GetTypeDefNameOrThrow(typeDef)

                If name.Equals("DisableJITOptimization") Then
                    disableOptDef = typeDef
                    Exit For
                End If
            Next

            Assert.NotEqual(Nothing, disableOptDef)

            Dim map As New Dictionary(Of String, MethodDefinitionHandle)()

            For Each methodDef In m.GetMethodsOfTypeOrThrow(disableOptDef)
                map.Add(m.GetMethodDefNameOrThrow(methodDef), methodDef)
            Next

            For i As Integer = 0 To methods.Length - 1
                Dim actualFlags As MethodImplAttributes
                m.GetMethodDefPropsOrThrow(map(methods(i)), name, actualFlags, Nothing, Nothing)
                Assert.Equal(implFlags(i), actualFlags)
            Next
        End Sub

        <Fact>
        Public Sub DisableJITOptimization_01()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports Microsoft.VisualBasic
Public Module DisableJITOptimization
    Sub Main()
        Err.Raise(0)
    End Sub

    Sub Main2()
 	    Dim x = Sub() Err.Raise(0)
    End Sub
End Module
]]>
    </file>
</compilation>
            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Const implFlags As MethodImplAttributes = MethodImplAttributes.IL Or MethodImplAttributes.Managed Or MethodImplAttributes.NoInlining Or MethodImplAttributes.NoOptimization
                    DisableJITOptimizationTestHelper(assembly, {"Main", "Main2"}, {implFlags, 0})
                End Sub

            CompileAndVerify(source, validator:=validator)
        End Sub

#End Region

#Region "DefaultCharSetAttribute"
        <Fact, WorkItem(544518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544518")>
        Public Sub DllImport_DefaultCharSet1()
            Dim source =
<compilation>
    <file><![CDATA[                
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Module:DefaultCharSet(CharSet.Ansi)>
MustInherit Class C

    <DllImport("goo")>
    Shared Sub f1()
    End Sub
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ImplMap))
                    Assert.False(MetadataValidation.FindCustomAttribute(reader, "DefaultCharSetAttribute").IsNil)
                    Dim import = reader.GetImportedMethods().Single().GetImport()
                    Assert.Equal(MethodImportAttributes.CharSetAnsi, import.Attributes And MethodImportAttributes.CharSetMask)
                End Sub)
        End Sub

        <Fact>
        Public Sub DllImport_DefaultCharSet2()
            Dim source =
<compilation>
    <file><![CDATA[                
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Module:DefaultCharSet(CharSet.None)>

<StructLayout(LayoutKind.Explicit)>
MustInherit Class C
    <DllImport("goo")>
    Shared Sub f1()
    End Sub
End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()

                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ImplMap))

                    Assert.False(MetadataValidation.FindCustomAttribute(reader, "DefaultCharSetAttribute").IsNil)

                    Dim import = reader.GetImportedMethods().Single().GetImport()
                    Assert.Equal(MethodImportAttributes.None, import.Attributes And MethodImportAttributes.CharSetMask)

                    For Each typeDef In reader.TypeDefinitions
                        Dim def = reader.GetTypeDefinition(typeDef)
                        Dim name = reader.GetString(def.Name)
                        Select Case name
                            Case "C"
                                Assert.Equal(TypeAttributes.ExplicitLayout Or TypeAttributes.Abstract, def.Attributes)

                            Case "<Module>"

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select
                    Next
                End Sub)
        End Sub


        <Fact>
        Public Sub DllImport_DefaultCharSet_Errors()
            Dim source =
<compilation>
    <file><![CDATA[   
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

<Module:DefaultCharSet(DirectCast(Integer.MaxValue, CharSet))>
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC30127: Attribute 'DefaultCharSetAttribute' is not valid: Incorrect argument value.
<Module:DefaultCharSet(DirectCast(Integer.MaxValue, CharSet))>
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>)
        End Sub

        <Fact>
        Public Sub DefaultCharSet_Types()
            Dim source =
<compilation>
    <file><![CDATA[   
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

<Module:DefaultCharSet(CharSet.Unicode)>
Class C
    Class D
        Dim arr As Integer() = {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0}

        Sub goo()
            Dim a As Integer = 1
            Dim b As Integer = 2
            Dim q = New With {.f = 1, .g = 2}
            Dim z = New Action(Sub() Console.WriteLine(a + arr(b)))
        End Sub
    End Class

    Event Goo(a As Integer, b As String)
End Class

<SpecialName>
Public Class Special
End Class

<StructLayout(LayoutKind.Sequential, Pack:=4, Size:=10)>
Public Structure SeqLayout
End Structure

Structure S
End Structure

Enum E
   A
End Enum

Interface I
End Interface

Delegate Sub D()

<Microsoft.VisualBasic.ComClass("", "", "")>
Public Class CC
    Public Sub F()
    End Sub
End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()
                    For Each typeDef In peFileReader.TypeDefinitions
                        Dim row = peFileReader.GetTypeDefinition(typeDef)
                        Dim name = peFileReader.GetString(row.Name)
                        Dim actual = row.Attributes And TypeAttributes.StringFormatMask

                        If name = "<Module>" OrElse
                           name.StartsWith("__StaticArrayInitTypeSize=", StringComparison.Ordinal) OrElse
                           name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal) Then
                            Assert.Equal(TypeAttributes.AnsiClass, actual)
                        Else
                            Assert.Equal(TypeAttributes.UnicodeClass, actual)
                        End If
                    Next
                End Sub)
        End Sub

        ''' <summary>
        ''' DefaultCharSet is not applied on embedded types.
        ''' </summary>
        <WorkItem(546644, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546644")>
        <Fact>
        Public Sub DefaultCharSet_EmbeddedTypes()
            Dim source =
<compilation>
    <file><![CDATA[   
Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic

<Module:DefaultCharSet(CharSet.Unicode)>

Friend Class C
    Public Sub Goo(x As Integer)    
      Console.WriteLine(ChrW(x))
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim c = CompilationUtils.CreateEmptyCompilationWithReferences(source,
                                                                     references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                     options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
            CompileAndVerify(c, validator:=
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()
                    For Each typeDef In peFileReader.TypeDefinitions
                        Dim row = peFileReader.GetTypeDefinition(typeDef)
                        Dim name = peFileReader.GetString(row.Name)
                        Dim actual = row.Attributes And TypeAttributes.StringFormatMask

                        If name = "C" Then
                            Assert.Equal(TypeAttributes.UnicodeClass, actual)
                        Else
                            ' embedded types should not be affected
                            Assert.Equal(TypeAttributes.AnsiClass, actual)
                        End If
                    Next
                End Sub)
        End Sub

#End Region

#Region "Declare Method PInvoke Flags"
        <Fact>
        Public Sub TestPseudoAttributes_Declare_DefaultFlags()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    Declare Sub Bar Lib "Goo" ()
End Class
]]>
    </file>
</compilation>

            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()

                    ' ModuleRef:
                    Dim moduleRefName = reader.GetModuleReference(reader.GetModuleReferences().Single()).Name
                    Assert.Equal("Goo", reader.GetString(moduleRefName))

                    ' FileRef:
                    ' Although the Metadata spec says there should be a File entry for each ModuleRef entry 
                    ' Dev10 compiler doesn't add it and peverify doesn't complain.
                    Assert.Equal(0, reader.GetTableRowCount(TableIndex.File))
                    Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef))

                    ' ImplMap:
                    Dim import = reader.GetImportedMethods().Single().GetImport()
                    Assert.Equal("Bar", reader.GetString(import.Name))
                    Assert.Equal(1, reader.GetRowNumber(import.Module))
                    Assert.Equal(MethodImportAttributes.ExactSpelling Or
                                 MethodImportAttributes.CharSetAnsi Or
                                 MethodImportAttributes.CallingConventionWinApi Or
                                 MethodImportAttributes.SetLastError, import.Attributes)

                    ' MethodDef:
                    Dim methodDefs As MethodDefinitionHandle() = reader.MethodDefinitions.AsEnumerable().ToArray()
                    Assert.Equal(2, methodDefs.Length) ' ctor, M
                    Assert.Equal(MethodImplAttributes.PreserveSig, reader.GetMethodDefinition(methodDefs(1)).ImplAttributes)
                End Sub

            CompileAndVerify(source, validator:=validator)
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_Declare_Flags()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Public Class C
    Declare Ansi Sub _Ansi Lib "a" ()
    Declare Unicode Sub _Unicode Lib "b" ()
    Declare Auto Sub _Auto Lib "c" ()

    Declare Function _Alias Lib "d" Alias "Baz" () As Integer
End Class
]]>
    </file>
</compilation>
            Const declareFlags = MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.SetLastError

            Dim validator As Action(Of PEAssembly) =
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()
                    Assert.Equal(4, peFileReader.GetTableRowCount(TableIndex.ModuleRef))
                    Assert.Equal(4, peFileReader.GetTableRowCount(TableIndex.ImplMap))

                    For Each method In peFileReader.GetImportedMethods()
                        Dim import = method.GetImport()
                        Dim moduleName As String = peFileReader.GetString(peFileReader.GetModuleReference(import.Module).Name
                                                                        )
                        Dim entryPointName As String = peFileReader.GetString(method.Name)
                        Dim importname As String = peFileReader.GetString(import.Name)

                        Select Case entryPointName
                            Case "_Ansi"
                                Assert.Equal("a", moduleName)
                                Assert.Equal("_Ansi", importname)
                                Assert.Equal(declareFlags Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetAnsi, import.Attributes)

                            Case "_Unicode"
                                Assert.Equal("b", moduleName)
                                Assert.Equal("_Unicode", importname)
                                Assert.Equal(declareFlags Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetUnicode, import.Attributes)

                            Case "_Auto"
                                Assert.Equal("c", moduleName)
                                Assert.Equal("_Auto", importname)
                                Assert.Equal(declareFlags Or MethodImportAttributes.CharSetAuto, import.Attributes)

                            Case "_Alias"
                                Assert.Equal("d", moduleName)
                                Assert.Equal("Baz", importname)
                                Assert.Equal(declareFlags Or MethodImportAttributes.ExactSpelling Or MethodImportAttributes.CharSetAnsi, import.Attributes)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(entryPointName)
                        End Select
                    Next
                End Sub

            CompileAndVerify(source, validator:=validator)
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_Declare_Modifiers()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class D
    Shared Declare Sub F1 Lib "d" ()
    Static Declare Sub F2 Lib "d" ()
    ReadOnly Declare Sub F3 Lib "d" ()
    WriteOnly Declare Sub F4 Lib "d" ()
    Overrides Declare Sub F5 Lib "d" ()
    Overridable Declare Sub F6 Lib "d" ()
    MustOverride Declare Sub F7 Lib "d" ()
    NotOverridable Declare Sub F8 Lib "d" ()
    Overloads Declare Sub F9 Lib "d" ()
    Shadows Declare Sub F10 Lib "d" ()
    Dim Declare Sub F11 Lib "d" ()
    Const Declare Sub F12 Lib "d" ()
    Static Declare Sub F13 Lib "d" ()
    Default Declare Sub F14 Lib "d" ()
    WithEvents Declare Sub F17 Lib "d" ()
    Widening Declare Sub F18 Lib "d" ()
    Narrowing Declare Sub F19 Lib "d" ()
    Partial Declare Sub F20 Lib "d" ()
    MustInherit Declare Sub F21 Lib "d" ()
    NotInheritable Declare Sub F22 Lib "d" ()
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Shared").WithArguments("Shared"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Static").WithArguments("Static"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "ReadOnly").WithArguments("ReadOnly"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "WriteOnly").WithArguments("WriteOnly"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Overrides").WithArguments("Overrides"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Overridable").WithArguments("Overridable"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "MustOverride").WithArguments("MustOverride"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "NotOverridable").WithArguments("NotOverridable"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Dim").WithArguments("Dim"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Const").WithArguments("Const"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Static").WithArguments("Static"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Default").WithArguments("Default"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "WithEvents").WithArguments("WithEvents"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Widening").WithArguments("Widening"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Narrowing").WithArguments("Narrowing"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "Partial").WithArguments("Partial"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "MustInherit").WithArguments("MustInherit"),
                Diagnostic(ERRID.ERR_BadDeclareFlags1, "NotInheritable").WithArguments("NotInheritable")
            )
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_Declare_Missing1()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class D
    Declare Sub Lib "d" ()
End Class
]]>
    </file>
</compilation>

            ' TODO (tomat): Dev10 only reports ERR_InvalidUseOfKeyword 
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidUseOfKeyword, "Lib"),
                Diagnostic(ERRID.ERR_MissingLibInDeclare, "")
            )
        End Sub

        <Fact>
        Public Sub TestPseudoAttributes_Declare_Missing2()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Public Class D
    Declare Sub $42 Lib "d" ()
End Class
]]>
    </file>
</compilation>

            ' TODO (tomat): Dev10 only reports ERR_IllegalChar 
            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpectedIdentifier, ""),
                Diagnostic(ERRID.ERR_IllegalChar, "$")
            )
        End Sub
#End Region

#Region "InAttribute, OutAttribute"
        <Fact()>
        Public Sub InOutAttributes()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.InteropServices

Class C
    Public Shared Sub M1(<[In]> ByRef a As Integer, <[In]> b As Integer, <[In]> ParamArray c As Object())
    End Sub

    Public Shared Sub M2(<Out> ByRef d As Integer, <Out> e As Integer, <Out> ParamArray f As Object())
    End Sub

    Public Shared Sub M3(<[In], Out> ByRef g As Integer, <[In], Out> h As Integer, <[In], [Out]> ParamArray i As Object())
    End Sub

    Public Shared Sub M4(<[In]>Optional j As Integer = 1, <[Out]>Optional k As Integer = 2, <[In], [Out]>Optional l As Integer = 3)
    End Sub
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()
                    Assert.Equal(12, reader.GetTableRowCount(TableIndex.Param))

                    For Each paramDef In reader.GetParameters()
                        Dim row = reader.GetParameter(paramDef)
                        Dim name As String = reader.GetString(row.Name)
                        Dim expectedFlags As ParameterAttributes
                        Select Case name
                            Case "a", "b", "c"
                                expectedFlags = ParameterAttributes.In

                            Case "d", "e", "f"
                                expectedFlags = ParameterAttributes.Out

                            Case "g", "h", "i"
                                expectedFlags = ParameterAttributes.In Or ParameterAttributes.Out

                            Case "j"
                                expectedFlags = ParameterAttributes.In Or ParameterAttributes.HasDefault Or ParameterAttributes.Optional

                            Case "k"
                                expectedFlags = ParameterAttributes.Out Or ParameterAttributes.HasDefault Or ParameterAttributes.Optional

                            Case "l"
                                expectedFlags = ParameterAttributes.In Or ParameterAttributes.Out Or ParameterAttributes.HasDefault Or ParameterAttributes.Optional

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select

                        Assert.Equal(expectedFlags, row.Attributes)
                    Next
                End Sub)
        End Sub

        <Fact()>
        Public Sub InOutAttributes_Properties()
            Dim source =
<compilation>
    <file name="attr.vb"><![CDATA[
Imports System.Runtime.InteropServices

Class C
    Public Property P1(<[In], Out>a As String, <[In]>b As String, <Out>c As String) As String
        Get
            Return Nothing
        End Get
        Set(<[In], Out>value As String)
        End Set
    End Property
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim reader = assembly.GetMetadataReader()

                    ' property parameters are copied for both getter and setter
                    Assert.Equal(3 + 3 + 1, reader.GetTableRowCount(TableIndex.Param))

                    For Each paramDef In reader.GetParameters()
                        Dim row = reader.GetParameter(paramDef)
                        Dim name As String = reader.GetString(row.Name)
                        Dim expectedFlags As ParameterAttributes
                        Select Case name
                            Case "a", "value"
                                expectedFlags = ParameterAttributes.In Or ParameterAttributes.Out

                            Case "b"
                                expectedFlags = ParameterAttributes.In

                            Case "c"
                                expectedFlags = ParameterAttributes.Out

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select

                        Assert.Equal(expectedFlags, row.Attributes)
                    Next
                End Sub)
        End Sub

#End Region

#Region "ParamArrayAttribute"
        <WorkItem(529684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529684")>
        <Fact>
        Public Sub TestParamArrayAttributeForParams2()
            Dim source =
<compilation>
    <file name="TestParamArrayAttributeForParams"><![CDATA[
imports System
Module M1
    Public Sub Lang(ParamArray list As Integer())
    End Sub

    Public Sub Both(<[ParamArray]>ParamArray list As Integer())
    End Sub

    Public Sub Custom(<[ParamArray]>list As Integer())
    End Sub

    Public Sub None(list As Integer())
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source)
            Dim attributeValidator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    Dim type = DirectCast(m.GlobalNamespace.GetMember("M1"), NamedTypeSymbol)

                    Dim lang = DirectCast(type.GetMember("Lang"), MethodSymbol)
                    Dim both = DirectCast(type.GetMember("Both"), MethodSymbol)
                    Dim custom = DirectCast(type.GetMember("Custom"), MethodSymbol)
                    Dim none = DirectCast(type.GetMember("None"), MethodSymbol)

                    Dim attrsLang = lang.Parameters(0).GetAttributes("System", "ParamArrayAttribute")
                    Dim attrsBoth = both.Parameters(0).GetAttributes("System", "ParamArrayAttribute")
                    Dim attrsCustom = custom.Parameters(0).GetAttributes("System", "ParamArrayAttribute")
                    Dim attrsNone = none.Parameters(0).GetAttributes("System", "ParamArrayAttribute")

                    If TypeOf type Is PENamedTypeSymbol Then
                        ' An attribute is created when loading from metadata
                        Assert.Equal(0, attrsLang.Count)
                        Assert.Equal(0, attrsBoth.Count)
                        Assert.Equal(0, attrsCustom.Count)
                        Assert.Equal(0, attrsNone.Count)

                        Assert.Equal(True, lang.Parameters(0).IsParamArray)
                        Assert.Equal(True, both.Parameters(0).IsParamArray)
                        Assert.Equal(True, custom.Parameters(0).IsParamArray)
                        Assert.Equal(False, none.Parameters(0).IsParamArray)
                    Else
                        ' No attribute because paramarray is a language construct not a custom attribute
                        Assert.Equal(0, attrsLang.Count)
                        Assert.Equal(1, attrsBoth.Count)
                        Assert.Equal(1, attrsCustom.Count)
                        Assert.Equal(0, attrsNone.Count)

                        Assert.Equal(True, lang.Parameters(0).IsParamArray)
                        Assert.Equal(True, both.Parameters(0).IsParamArray)
                        Assert.Equal(True, custom.Parameters(0).IsParamArray)
                        Assert.Equal(False, none.Parameters(0).IsParamArray)
                    End If
                End Sub

            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

#End Region

#Region "SpecialNameAttribute"
        <Fact>
        Public Sub SpecialName_AllTargets()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<SpecialName>
Class Z
    <SpecialName>
    Sub m()
    End Sub

    <SpecialName>
    Dim f As Integer

    <SpecialName>
    Property p1 As Integer

    <SpecialName>
    ReadOnly Property p2 As Integer
        Get
            Return 1
        End Get
    End Property

    <SpecialName>
    Property p3 As Integer
        <SpecialName()>
        Get
            Return 1
        End Get

        <SpecialName>
        Set(value As Integer)
        End Set
    End Property

    <SpecialName>
    Event e As Action
End Class

<SpecialName>
Module M
    <SpecialName>
    Public WithEvents we As New Z

    <SpecialName>
    Sub WEHandler() Handles we.e
    End Sub
End Module

Enum En
    <SpecialName>
    A = 1
    <SpecialName>
    B
End Enum

<SpecialName>
Structure S
End Structure
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()

                    For Each ca In peFileReader.CustomAttributes
                        Dim name = MetadataValidation.GetAttributeName(peFileReader, ca)
                        Assert.NotEqual("SpecialNameAttribute", name)
                    Next

                    For Each typeDef In peFileReader.TypeDefinitions
                        Dim row = peFileReader.GetTypeDefinition(typeDef)
                        Dim name = peFileReader.GetString(row.Name)
                        Select Case name
                            Case "S", "Z", "M"
                                Assert.Equal(TypeAttributes.SpecialName, row.Attributes And TypeAttributes.SpecialName)

                            Case "<Module>", "En"
                                Assert.Equal(0, row.Attributes And TypeAttributes.SpecialName)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select
                    Next

                    For Each methodDef In peFileReader.MethodDefinitions
                        Dim flags = peFileReader.GetMethodDefinition(methodDef).Attributes
                        Assert.Equal(MethodAttributes.SpecialName, flags And MethodAttributes.SpecialName)
                    Next

                    For Each fieldDef In peFileReader.FieldDefinitions
                        Dim field = peFileReader.GetFieldDefinition(fieldDef)
                        Dim name = peFileReader.GetString(field.Name)
                        Dim flags = field.Attributes
                        Select Case name
                            Case "En", "value__", "_we", "f", "A", "B"
                                Assert.Equal(FieldAttributes.SpecialName, flags And FieldAttributes.SpecialName)

                            Case "_p1", "eEvent"
                                Assert.Equal(0, flags And FieldAttributes.SpecialName)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select
                    Next

                    For Each propertyDef In peFileReader.PropertyDefinitions
                        Dim prop = peFileReader.GetPropertyDefinition(propertyDef)
                        Dim name = peFileReader.GetString(prop.Name)
                        Dim flags = prop.Attributes
                        Select Case name
                            Case "p1", "p2", "p3"
                                Assert.Equal(PropertyAttributes.SpecialName, flags And PropertyAttributes.SpecialName)

                            Case "we"
                                Assert.Equal(0, flags And PropertyAttributes.SpecialName)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select
                    Next

                    For Each eventDef In peFileReader.EventDefinitions
                        Dim flags = peFileReader.GetEventDefinition(eventDef).Attributes
                        Assert.Equal(EventAttributes.SpecialName, flags And EventAttributes.SpecialName)
                    Next
                End Sub)
        End Sub

#End Region

#Region "SerializableAttribute, NonSerializedAttribute"
        <Fact>
        Public Sub Serializable_NonSerialized_AllTargets()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<Serializable>
Class A
    <NonSerialized>
    Event e1 As Action

    <NonSerialized>
    Event e3(a As Integer)
End Class

<Serializable>
Module M
    <NonSerialized>
    Public WithEvents we As New EventClass

    Sub WEHandler() Handles we.e2
    End Sub
End Module

<Serializable>
Structure B
    <NonSerialized>
    Dim x As Integer
End Structure

<Serializable>
Enum E
    <NonSerialized>
    A = 1
End Enum

<Serializable>
Delegate Sub D()

<Serializable>
Class EventClass
    <NonSerialized>
    Public Event e2()

    Sub RaiseEvents()
        RaiseEvent e2()
    End Sub
End Class
]]>
    </file>
</compilation>

            CompileAndVerify(source, validator:=
                Sub(assembly)
                    Dim peFileReader = assembly.GetMetadataReader()

                    For Each ca In peFileReader.CustomAttributes
                        Dim name = MetadataValidation.GetAttributeName(peFileReader, ca)
                        Assert.NotEqual("SerializableAttribute", name)
                        Assert.NotEqual("NonSerializedAttribute", name)
                    Next

                    For Each typeDef In peFileReader.TypeDefinitions
                        Dim row = peFileReader.GetTypeDefinition(typeDef)
                        Dim name = peFileReader.GetString(row.Name)
                        Select Case name
                            Case "A", "B", "D", "E", "EventClass", "M"
                                Assert.Equal(TypeAttributes.Serializable, row.Attributes And TypeAttributes.Serializable)

                            Case "<Module>", "StandardModuleAttribute", "e2EventHandler", "e3EventHandler"
                                Assert.Equal(0, row.Attributes And TypeAttributes.Serializable)

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(name)
                        End Select
                    Next

                    For Each fieldDef In peFileReader.FieldDefinitions
                        Dim field = peFileReader.GetFieldDefinition(fieldDef)
                        Dim name = peFileReader.GetString(field.Name)
                        Dim flags = field.Attributes
                        Select Case name
                            Case "e1Event", "x", "A", "e2Event", "_we", "e3Event"
                                Assert.Equal(FieldAttributes.NotSerialized, flags And FieldAttributes.NotSerialized)

                            Case "value__"
                                Assert.Equal(0, flags And FieldAttributes.NotSerialized)

                        End Select
                    Next
                End Sub)
        End Sub

        <WorkItem(545199, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545199")>
        <Fact>
        Public Sub Serializable_NonSerialized_CustomEvents()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Interface I
    <NonSerialized>
    Event e1 As Action
End Interface

MustInherit Class C
    <NonSerialized>
    Custom Event e2 As EventHandler 
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
]]>
    </file>
</compilation>

            CompilationUtils.AssertTheseDiagnostics(CreateCompilationWithMscorlib40(source),
<expected><![CDATA[
BC30662: Attribute 'NonSerializedAttribute' cannot be applied to 'e1' because the attribute is not valid on this declaration type.
    <NonSerialized>
     ~~~~~~~~~~~~~
BC30662: Attribute 'NonSerializedAttribute' cannot be applied to 'e2' because the attribute is not valid on this declaration type.
    <NonSerialized>
     ~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40682: The test hook is blocked by this issue.
        <WorkItem(40682, "https://github.com/dotnet/roslyn/issues/40682")>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Public Sub TestIsSerializableProperty()
            Dim missing =
<compilation>
    <file name="missing.vb"><![CDATA[
Public Class TopLevel
    Public Class Nested
    End Class
End Class
Public Class TopLevel(Of T)
    Public Class Nested(Of U)
    End Class
End Class
Public Class Constructed(Of T)
End Class
]]></file>
</compilation>

            Dim source =
<compilation>
    <file name="source.vb"><![CDATA[
Public Class C(Of T)
    Public Class Nested
    End Class
End Class

<System.Serializable>
Public Class CS(Of T)
    <System.Serializable>
    Public Class NestedS
    End Class

    Public Class Nested(Of U)
    End Class

    <System.Serializable>
    Public Class NestedS(Of U)
    End Class
End Class

Public Class SubstitutedNested
    Inherits C(Of Integer).Nested
End Class
Public Class SubstitutedNestedS
    Inherits CS(Of Integer).NestedS
End Class

Public Class Constructed
    Inherits C(Of Integer)
End Class
Public Class ConstructedS
    Inherits CS(Of Integer)
End Class
Public Class MissingTopLevel
    Inherits TopLevel
End Class
Public Class MissingNested
    Inherits TopLevel.Nested
End Class
Public Class MissingConstructed
    Inherits Constructed(Of Integer)
End Class

Public Class MissingSubstitutedNested(Of T, U)
    Inherits TopLevel(Of T).Nested(Of U)
End Class

Public Class SpecializedGenericType
    Inherits CS(Of Integer).Nested(Of Integer)
End Class
Public Class SpecializedGenericTypeS
    Inherits CS(Of Integer).NestedS(Of Integer)
End Class

Namespace System
    <System.Serializable>
    Public Structure ValueTuple(Of T1, T2)
    End Structure

    Public Class InNamespace
    End Class

    <System.Serializable>
    Public Class InNamespaceS
    End Class
End Namespace

Public Class ValueTupleS
    Function M() As (Integer, Integer)
        Throw New System.Exception()
    End Function
End Class
]]></file>
</compilation>

            Dim errors =
<compilation>
    <file name="errors.vb"><![CDATA[
Public Class ExtendedError
    Inherits ExtendedErrorBase
End Class
]]></file>
</compilation>
            Dim lib1 = CreateCompilationWithMscorlib45AndVBRuntime(missing)
            lib1.VerifyDiagnostics()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source, references:={lib1.EmitToImageReference()})
            comp.VerifyDiagnostics()
            Dim comp2 = CreateCompilationWithMscorlib45AndVBRuntime(errors, references:={comp.EmitToImageReference()})

            Dim substitutedNested = comp.GetTypeByMetadataName("SubstitutedNested").BaseType()
            Assert.IsType(Of SubstitutedNamedType.SpecializedNonGenericType)(substitutedNested)
            Assert.False(DirectCast(substitutedNested, INamedTypeSymbol).IsSerializable)

            Dim substitutedNestedS = comp.GetTypeByMetadataName("SubstitutedNestedS").BaseType()
            Assert.IsType(Of SubstitutedNamedType.SpecializedNonGenericType)(substitutedNestedS)
            Assert.True(DirectCast(substitutedNestedS, INamedTypeSymbol).IsSerializable)

            Dim specialized = comp.GetTypeByMetadataName("SpecializedGenericType").BaseType()
            Assert.IsType(Of SubstitutedNamedType.ConstructedSpecializedGenericType)(specialized)
            Assert.False(DirectCast(specialized, INamedTypeSymbol).IsSerializable)

            Dim specializedS = comp.GetTypeByMetadataName("SpecializedGenericTypeS").BaseType()
            Assert.IsType(Of SubstitutedNamedType.ConstructedSpecializedGenericType)(specializedS)
            Assert.True(DirectCast(specializedS, INamedTypeSymbol).IsSerializable)

            Dim valueTupleS = DirectCast(comp.GetTypeByMetadataName("ValueTupleS").GetMember("M"), SourceMemberMethodSymbol).ReturnType
            Assert.IsType(Of TupleTypeSymbol)(valueTupleS)
            Assert.True(DirectCast(valueTupleS, INamedTypeSymbol).IsSerializable)

            Dim constructed = comp.GetTypeByMetadataName("Constructed").BaseType()
            Assert.IsType(Of SubstitutedNamedType.ConstructedInstanceType)(constructed)
            Assert.False(DirectCast(constructed, INamedTypeSymbol).IsSerializable)

            Dim constructedPE = comp2.GetTypeByMetadataName("Constructed").BaseType().ConstructedFrom
            Assert.IsType(Of PENamedTypeSymbol)(constructedPE)
            Assert.False(DirectCast(constructedPE, INamedTypeSymbol).IsSerializable)

            Dim constructedFrom = constructed.ConstructedFrom
            Assert.IsType(Of SourceNamedTypeSymbol)(constructedFrom)
            Assert.False(DirectCast(constructedFrom, INamedTypeSymbol).IsSerializable)

            Dim constructedS = comp.GetTypeByMetadataName("ConstructedS").BaseType()
            Assert.IsType(Of SubstitutedNamedType.ConstructedInstanceType)(constructedS)
            Assert.True(DirectCast(constructedS, INamedTypeSymbol).IsSerializable)

            Dim constructedSPE = comp2.GetTypeByMetadataName("ConstructedS").BaseType().ConstructedFrom
            Assert.IsType(Of PENamedTypeSymbol)(constructedSPE)
            Assert.True(DirectCast(constructedSPE, INamedTypeSymbol).IsSerializable)

            Dim constructedFromS = constructedS.ConstructedFrom
            Assert.IsType(Of SourceNamedTypeSymbol)(constructedFromS)
            Assert.True(DirectCast(constructedFromS, INamedTypeSymbol).IsSerializable)

            Dim extendedError = comp2.GetTypeByMetadataName("ExtendedError").BaseType()
            Assert.IsType(Of ExtendedErrorTypeSymbol)(extendedError)
            Assert.False(DirectCast(extendedError, INamedTypeSymbol).IsSerializable)

            Dim topLevel = comp2.GetTypeByMetadataName("MissingTopLevel").BaseType()
            Assert.IsType(Of MissingMetadataTypeSymbol.TopLevel)(topLevel)
            Assert.False(DirectCast(topLevel, INamedTypeSymbol).IsSerializable)

            Dim nested = comp2.GetTypeByMetadataName("MissingNested").BaseType()
            Assert.IsType(Of MissingMetadataTypeSymbol.Nested)(nested)
            Assert.False(DirectCast(nested, INamedTypeSymbol).IsSerializable)

            Dim constructedError = comp2.GetTypeByMetadataName("MissingConstructed").BaseType()
            Assert.IsType(Of SubstitutedErrorType)(constructedError)
            Assert.False(DirectCast(constructedError, INamedTypeSymbol).IsSerializable)

            Dim nestedSubstitutedError = comp2.GetTypeByMetadataName("MissingSubstitutedNested`2").BaseType().ConstructedFrom
            Assert.IsType(Of SubstitutedErrorType)(nestedSubstitutedError)
            Assert.False(DirectCast(nestedSubstitutedError, INamedTypeSymbol).IsSerializable)

            Dim script = CreateCompilationWithMscorlib40("", parseOptions:=TestOptions.Script)
            Dim scriptClass = script.GetTypeByMetadataName("Script")
            Assert.IsType(Of ImplicitNamedTypeSymbol)(scriptClass)
            Assert.False(DirectCast(scriptClass, INamedTypeSymbol).IsSerializable)

            Dim inNamespace = comp2.GetTypeByMetadataName("System.InNamespace")
            Assert.IsType(Of PENamedTypeSymbolWithEmittedNamespaceName)(inNamespace)
            Assert.False(DirectCast(inNamespace, INamedTypeSymbol).IsSerializable)

            Dim inNamespaceS = comp2.GetTypeByMetadataName("System.InNamespaceS")
            Assert.IsType(Of PENamedTypeSymbolWithEmittedNamespaceName)(inNamespaceS)
            Assert.True(DirectCast(inNamespaceS, INamedTypeSymbol).IsSerializable)
        End Sub

        <Fact>
        <WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")>
        Public Sub TestAttributeWithNestedUnboundGeneric()
            Dim library =
    <file name="Library.vb"><![CDATA[
Namespace ClassLibrary1
    <System.Serializable>
    Public Class C1(Of T1)
    End Class
End Namespace
]]>
    </file>

            Dim compilation1 = VisualBasicCompilation.Create("library.dll",
                                                             {VisualBasicSyntaxTree.ParseText(library.Value)},
                                                             {MscorlibRef},
                                                             TestOptions.ReleaseDll)

            Dim classLibrary = MetadataReference.CreateFromImage(compilation1.EmitToArray())

            Dim source =
        <compilation>
            <file name="TestAttributeWithNestedUnboundGeneric.vb"><![CDATA[
Imports System

Class A
    Inherits Attribute

    Public Sub New(o As Object)
    End Sub
End Class

<A(GetType(ClassLibrary1.C1(Of )))>
Module Module1
    Sub Main()
    End Sub
End Module
]]>
            </file>
        </compilation>
            Dim compilation2 = CreateCompilationWithMscorlib40AndReferences(source, {SystemRef, MsvbRef, classLibrary})
            compilation2.VerifyDiagnostics()

            Dim gt = compilation2.GetTypeByMetadataName("Module1").GetAttributes().First().CommonConstructorArguments.First()
            Dim arg = DirectCast(gt.Value, UnboundGenericType)
            Assert.True(DirectCast(arg, INamedTypeSymbol).IsSerializable)
        End Sub

#End Region

#Region "AttributeUsageAttribute"

        <WorkItem(541733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541733")>
        <Fact()>
        Public Sub TestSourceOverrideWellKnownAttribute_01()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(AttributeTargets.Class)>
    <AttributeUsage(AttributeTargets.Class)>
    Class AttributeUsageAttribute
        Inherits Attribute
        Public Sub New(x As AttributeTargets)
        End Sub
    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source)

            ' BC30663: Attribute 'AttributeUsageAttribute' cannot be applied multiple times.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "AttributeUsage(AttributeTargets.Class)").WithArguments("AttributeUsageAttribute"))
        End Sub

        <WorkItem(541733, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541733")>
        <Fact()>
        Public Sub TestSourceOverrideWellKnownAttribute_02()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= True)>
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= True)>
    Class AttributeUsageAttribute
        Inherits Attribute
        Public Sub New(x As AttributeTargets)
        End Sub

        Public Property AllowMultiple As Boolean
            Get
                Return False
            End Get
            Set
            End Set
        End Property

    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)

            Dim attributeValidator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                                    Dim ns = DirectCast(m.GlobalNamespace.GetMember("System"), NamespaceSymbol)
                                                                    Dim attrType = ns.GetTypeMember("AttributeUsageAttribute")

                                                                    Dim attrs = attrType.GetAttributes(attrType)
                                                                    Assert.Equal(2, attrs.Count)

                                                                    ' Verify attributes
                                                                    Dim attrSym = attrs(0)
                                                                    Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
                                                                    Assert.Equal(TypedConstantKind.Enum, attrSym.CommonConstructorArguments(0).Kind)
                                                                    Assert.Equal(AttributeTargets.Class, DirectCast(attrSym.CommonConstructorArguments(0).Value, AttributeTargets))
                                                                    Assert.Equal(1, attrSym.CommonNamedArguments.Length)
                                                                    Assert.Equal("Boolean", attrSym.CommonNamedArguments(0).Value.Type.ToDisplayString)
                                                                    Assert.Equal("AllowMultiple", attrSym.CommonNamedArguments(0).Key)
                                                                    Assert.Equal(True, attrSym.CommonNamedArguments(0).Value.Value)

                                                                    attrSym = attrs(1)
                                                                    Assert.Equal(1, attrSym.CommonConstructorArguments.Length)
                                                                    Assert.Equal(TypedConstantKind.Enum, attrSym.CommonConstructorArguments(0).Kind)
                                                                    Assert.Equal(AttributeTargets.Class, DirectCast(attrSym.CommonConstructorArguments(0).Value, AttributeTargets))
                                                                    Assert.Equal(1, attrSym.CommonNamedArguments.Length)
                                                                    Assert.Equal("Boolean", attrSym.CommonNamedArguments(0).Value.Type.ToDisplayString)
                                                                    Assert.Equal("AllowMultiple", attrSym.CommonNamedArguments(0).Key)
                                                                    Assert.Equal(True, attrSym.CommonNamedArguments(0).Value.Value)

                                                                    ' Verify AttributeUsage
                                                                    Dim attributeUage = attrType.GetAttributeUsageInfo()
                                                                    Assert.Equal(AttributeTargets.Class, attributeUage.ValidTargets)
                                                                    Assert.Equal(True, attributeUage.AllowMultiple)
                                                                    Assert.Equal(True, attributeUage.Inherited)

                                                                End Sub


            ' Verify attributes from source and then load metadata to see attributes are written correctly.
            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator, symbolValidator:=attributeValidator)
        End Sub

        <Fact()>
        Public Sub TestSourceOverrideWellKnownAttribute_03()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= True)>      ' First AttributeUsageAttribute is used for determining AttributeUsage.
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= False)>
    Class AttributeUsageAttribute
        Inherits Attribute
        Public Sub New(x As AttributeTargets)
        End Sub

        Public Property AllowMultiple As Boolean
            Get
                Return False
            End Get
            Set
            End Set
        End Property

    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub TestSourceOverrideWellKnownAttribute_03_DifferentOrder()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= False)>     ' First AttributeUsageAttribute is used for determining AttributeUsage.
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:= True)>
    Class AttributeUsageAttribute
        Inherits Attribute
        Public Sub New(x As AttributeTargets)
        End Sub

        Public Property AllowMultiple As Boolean
            Get
                Return False
            End Get
            Set
            End Set
        End Property

    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "AttributeUsage(AttributeTargets.Class, AllowMultiple:= True)").WithArguments("AttributeUsageAttribute"))
        End Sub

        <Fact()>
        Public Sub TestAttributeUsageInvalidTargets_01()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(0)> ' No error here
    Class X
        Inherits Attribute
    End Class

    <AttributeUsage(-1)> ' No error here
    Class Y
        Inherits Attribute
    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub TestAttributeUsageInvalidTargets_02()
            Dim source = <compilation>
                             <file name="attr.vb"><![CDATA[
Namespace System
    <AttributeUsage(0)> ' No error here
    Class X
        Inherits Attribute
    End Class

    <AttributeUsage(-1)> ' No error here
    Class Y
        Inherits Attribute
    End Class

    <X> ' Error here
    <Y> ' No Error here
    Class Z
    End Class
End Namespace
                ]]>
                             </file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30662: Attribute 'X' cannot be applied to 'Z' because the attribute is not valid on this declaration type.
    <X> ' Error here
     ~
]]></expected>)

        End Sub
#End Region

#Region "Security Attributes"
        <Fact>
        Public Sub TestHostProtectionAttribute()

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<System.Security.Permissions.HostProtection(MayLeakOnAbort := true)>
public structure EventDescriptor
end structure
]]>
                             </file>
                         </compilation>

            Dim attributeValidator As Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim assembly = [module].ContainingAssembly
                    Dim sourceAssembly = DirectCast(assembly, SourceAssemblySymbol)
                    Dim compilation = sourceAssembly.DeclaringCompilation

                    ' Get System.Security.Permissions.HostProtection
                    Dim emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "HostProtectionAttribute")
                    Dim hostProtectionAttr As NamedTypeSymbol = sourceAssembly.CorLibrary.LookupTopLevelMetadataType(emittedName, True)
                    Assert.NotNull(hostProtectionAttr)

                    ' Verify type security attributes
                    Dim type = DirectCast([module].GlobalNamespace.GetMember("EventDescriptor").GetCciAdapter(), Microsoft.Cci.ITypeDefinition)
                    Debug.Assert(type.HasDeclarativeSecurity)
                    Dim typeSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = type.SecurityAttributes

                    Assert.Equal(1, typeSecurityAttributes.Count())

                    ' Verify <System.Security.Permissions.HostProtection(MayLeakOnAbort := true)>
                    Dim securityAttribute = typeSecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.LinkDemand, securityAttribute.Action)
                    Dim typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(hostProtectionAttr, typeAttribute.AttributeClass)
                    Assert.Equal(0, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "MayLeakOnAbort", TypedConstantKind.Primitive, True)
                End Sub

            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        <Fact()>
        Public Sub TestValidSecurityAction()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Imports System
    Imports System.Security.Permissions
    Imports System.Security.Principal

    <PrincipalPermission(DirectCast(1, SecurityAction))>
    <PrincipalPermission(SecurityAction.Assert)>
    <PrincipalPermission(SecurityAction.Demand)>
    <PrincipalPermission(SecurityAction.Deny)>
    <PrincipalPermission(SecurityAction.PermitOnly)>
    Class A
    End Class

    Module Module1
            Sub Main()
            End Sub
    End Module]]>
                             </file>
                         </compilation>

            CompileAndVerify(source)
        End Sub

        <Fact()>
        Public Sub TestValidSecurityActionForTypeOrMethod()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Imports System
    Imports System.Security.Permissions
    Imports System.Security

    <MySecurityAttribute(Directcast(1,SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
    <MySecurityAttribute(SecurityAction.Assert)>
    <MySecurityAttribute(SecurityAction.Demand)>
    <MySecurityAttribute(SecurityAction.Deny)>
    <MySecurityAttribute(SecurityAction.InheritanceDemand)>
    <MySecurityAttribute(SecurityAction.LinkDemand)>
    <MySecurityAttribute(SecurityAction.PermitOnly)>
    <MyCodeAccessSecurityAttribute(Directcast(1,SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
    <MyCodeAccessSecurityAttribute(SecurityAction.Assert)>
    <MyCodeAccessSecurityAttribute(SecurityAction.Demand)>
    <MyCodeAccessSecurityAttribute(SecurityAction.Deny)>
    <MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)>
    <MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)>
    <MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)>
    class Test 
        <MySecurityAttribute(directcast(1, SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
        <MySecurityAttribute(SecurityAction.Assert)>
        <MySecurityAttribute(SecurityAction.Demand)>
        <MySecurityAttribute(SecurityAction.Deny)>
        <MySecurityAttribute(SecurityAction.InheritanceDemand)>
        <MySecurityAttribute(SecurityAction.LinkDemand)>
        <MySecurityAttribute(SecurityAction.PermitOnly)>
        <MyCodeAccessSecurityAttribute(Directcast(1,SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
        <MyCodeAccessSecurityAttribute(SecurityAction.Assert)>
        <MyCodeAccessSecurityAttribute(SecurityAction.Demand)>
        <MyCodeAccessSecurityAttribute(SecurityAction.Deny)>
        <MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)>
        <MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)>
        <MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)>
        public shared sub Main()
        End Sub
    end class

    class MySecurityAttribute
        inherits SecurityAttribute

        public sub new (a as SecurityAction)
            mybase.new(a)
        end sub

        public overrides function CreatePermission() as IPermission 
            return nothing
        end function
    end class

    class MyCodeAccessSecurityAttribute 
        inherits CodeAccessSecurityAttribute

        public sub new (a as SecurityAction)
            mybase.new(a)
        end sub

        public overrides function CreatePermission() as IPermission 
            return nothing
        end function

        public shared sub Main()
        end sub
    end class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(source)
        End Sub

        <Fact()>
        Public Sub TestValidSecurityActionsForAssembly()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
imports System
imports System.Security
imports System.Security.Permissions

<assembly: MySecurityAttribute(SecurityAction.RequestMinimum)>
<assembly: MySecurityAttribute(SecurityAction.RequestOptional)>
<assembly: MySecurityAttribute(SecurityAction.RequestRefuse)>

<assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)>

class MySecurityAttribute 
    inherits SecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function
end class

class MyCodeAccessSecurityAttribute 
    inherits CodeAccessSecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function

    public shared sub Main()
    end sub
end class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(source)
        End Sub

        <Fact()>
        Public Sub TestInvalidSecurityActionErrors()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Imports System.Security.Permissions

    Public Class MySecurityAttribute
	    Inherits SecurityAttribute
	    Public Sub New(action As SecurityAction)
		    MyBase.New(action)
	    End Sub
	    Public Overrides Function CreatePermission() As System.Security.IPermission
		    Return Nothing
	    End Function
    End Class

    <MySecurityAttribute(DirectCast(0, SecurityAction))>
    <MySecurityAttribute(DirectCast(11, SecurityAction))>
    <MySecurityAttribute(DirectCast(-1, SecurityAction))>
    <FileIOPermission(DirectCast(0, SecurityAction))>
    <FileIOPermission(DirectCast(11, SecurityAction))>
    <FileIOPermission(DirectCast(-1, SecurityAction))>
    <FileIOPermission()>
    Class A
        <FileIOPermission(SecurityAction.Demand)>
        Public Field as Integer
    End Class

    Module Module1
            Sub Main()
            End Sub
    End Module]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_OmittedArgument2, "FileIOPermission").WithArguments("action", "Public Overloads Sub New(action As System.Security.Permissions.SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(0, SecurityAction)").WithArguments("MySecurityAttribute", "DirectCast(0, SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(11, SecurityAction)").WithArguments("MySecurityAttribute", "DirectCast(11, SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(-1, SecurityAction)").WithArguments("MySecurityAttribute", "DirectCast(-1, SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(0, SecurityAction)").WithArguments("FileIOPermission", "DirectCast(0, SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(11, SecurityAction)").WithArguments("FileIOPermission", "DirectCast(11, SecurityAction)"),
                                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "DirectCast(-1, SecurityAction)").WithArguments("FileIOPermission", "DirectCast(-1, SecurityAction)"),
                                Diagnostic(ERRID.ERR_InvalidAttributeUsage2, "FileIOPermission").WithArguments("FileIOPermissionAttribute", "Field"))
        End Sub

        <Fact()>
        Public Sub TestMissingSecurityActionErrors()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
imports System.Security
imports System.Security.Permissions

Public Class MySecurityAttribute
    Inherits CodeAccessSecurityAttribute

    Public Field As Boolean
    Public Property Prop As Boolean
    Public Overrides Function CreatePermission() As IPermission
        Return Nothing
    End Function

    Public Sub New()
        MyBase.New(SecurityAction.Assert)
    End Sub

    Public Sub New(x As Integer, a1 As SecurityAction)
        MyBase.New(a1)
    End Sub
End Class

<MySecurityAttribute()>
<MySecurityAttribute(Field := true)>
<MySecurityAttribute(Field := true, Prop := true)>
<MySecurityAttribute(Prop := true)>
<MySecurityAttribute(Prop := true, Field := true)>
<MySecurityAttribute(0, SecurityAction.Assert)>
public class C
end class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute"),
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute"),
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute"),
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute"),
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute"),
                Diagnostic(ERRID.ERR_SecurityAttributeMissingAction, "MySecurityAttribute").WithArguments("MySecurityAttribute")
                )
        End Sub

        <Fact()>
        Public Sub TestInvalidSecurityActionsForAssemblyErrors()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
imports System.Security
imports System.Security.Permissions

<assembly: MySecurityAttribute(DirectCast(1, SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
<assembly: MySecurityAttribute(SecurityAction.Assert)>
<assembly: MySecurityAttribute(SecurityAction.Demand)>
<assembly: MySecurityAttribute(SecurityAction.Deny)>
<assembly: MySecurityAttribute(SecurityAction.InheritanceDemand)>
<assembly: MySecurityAttribute(SecurityAction.LinkDemand)>
<assembly: MySecurityAttribute(SecurityAction.PermitOnly)>

<assembly: MyCodeAccessSecurityAttribute(DirectCast(1, SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.Assert)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.Demand)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.Deny)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.InheritanceDemand)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.LinkDemand)>
<assembly: MyCodeAccessSecurityAttribute(SecurityAction.PermitOnly)>

class MySecurityAttribute 
    inherits SecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function
end class

class MyCodeAccessSecurityAttribute 
    inherits CodeAccessSecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function

    public shared sub Main()
    end sub
end class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            VerifyDiagnostics(compilation,
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "DirectCast(1, SecurityAction)").WithArguments("DirectCast(1, SecurityAction)"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Assert").WithArguments("SecurityAction.Assert"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Demand").WithArguments("SecurityAction.Demand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Deny").WithArguments("SecurityAction.Deny"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.PermitOnly").WithArguments("SecurityAction.PermitOnly"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "DirectCast(1, SecurityAction)").WithArguments("DirectCast(1, SecurityAction)"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Assert").WithArguments("SecurityAction.Assert"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Demand").WithArguments("SecurityAction.Demand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.Deny").WithArguments("SecurityAction.Deny"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionAssembly, "SecurityAction.PermitOnly").WithArguments("SecurityAction.PermitOnly"))
        End Sub

        <Fact()>
        Public Sub TestInvalidSecurityActionForTypeOrMethod()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    Imports System.Security.Permissions
    Imports System.Security

    <MySecurityAttribute(SecurityAction.RequestMinimum)>
    <MySecurityAttribute(SecurityAction.RequestOptional)>
    <MySecurityAttribute(SecurityAction.RequestRefuse)>
    <MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)>
    <MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)>
    <MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)>
    class Test 
        <MySecurityAttribute(SecurityAction.RequestMinimum)>
        <MySecurityAttribute(SecurityAction.RequestOptional)>
        <MySecurityAttribute(SecurityAction.RequestRefuse)>
        <MyCodeAccessSecurityAttribute(SecurityAction.RequestMinimum)>
        <MyCodeAccessSecurityAttribute(SecurityAction.RequestOptional)>
        <MyCodeAccessSecurityAttribute(SecurityAction.RequestRefuse)>
        public shared sub Main()
        End Sub
    end class

    class MySecurityAttribute 
        inherits SecurityAttribute

        public sub new (a as SecurityAction)
            mybase.new(a)
        end sub

        public overrides function CreatePermission() as IPermission 
            return nothing
        end function
    end class

    class MyCodeAccessSecurityAttribute 
        inherits CodeAccessSecurityAttribute

        public sub new (a as SecurityAction)
            mybase.new(a)
        end sub

        public overrides function CreatePermission() as IPermission 
            return nothing
        end function

        public shared sub Main()
        end sub
    end class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            VerifyDiagnostics(compilation,
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestRefuse").WithArguments("RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestRefuse").WithArguments("RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestRefuse").WithArguments("RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestRefuse").WithArguments("RequestRefuse", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestMinimum").WithArguments("SecurityAction.RequestMinimum"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestOptional").WithArguments("SecurityAction.RequestOptional"),
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidActionTypeOrMethod, "SecurityAction.RequestRefuse").WithArguments("SecurityAction.RequestRefuse"))
        End Sub

        <WorkItem(546623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546623")>
        <Fact>
        Public Sub TestSecurityAttributeInvalidTarget()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System
Imports System.Security
Imports System.Security.Permissions

Class Program
	<MyPermission(SecurityAction.Demand)> _
	Private x As Integer
End Class

<AttributeUsage(AttributeTargets.All)> _
Class MyPermissionAttribute
	Inherits CodeAccessSecurityAttribute
	Public Sub New(action As SecurityAction)
		MyBase.New(action)
	End Sub

	Public Overrides Function CreatePermission() As IPermission
		Return Nothing
	End Function
End Class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            VerifyDiagnostics(compilation,
                Diagnostic(ERRID.ERR_SecurityAttributeInvalidTarget, "MyPermission").WithArguments("MyPermissionAttribute"))
        End Sub

        <WorkItem(544929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544929")>
        <Fact>
        Public Sub PrincipalPermissionAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
    imports System.Security.Permissions

    Class Program

        <PrincipalPermission(DirectCast(1,SecurityAction))>        ' Native compiler allows this security action value for type/method security attributes, but not for assembly.
        <PrincipalPermission(SecurityAction.Assert)>
        <PrincipalPermission(SecurityAction.Demand)>
        <PrincipalPermission(SecurityAction.Deny)>
        <PrincipalPermission(SecurityAction.InheritanceDemand)>     ' BC31209
        <PrincipalPermission(SecurityAction.LinkDemand)>            ' BC31209
        <PrincipalPermission(SecurityAction.PermitOnly)>
        public shared sub Main()
        End Sub
    End Class
]]>
                             </file>
                         </compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                                                                    Diagnostic(ERRID.ERR_PrincipalPermissionInvalidAction, "SecurityAction.InheritanceDemand").WithArguments("SecurityAction.InheritanceDemand"),
                                                                    Diagnostic(ERRID.ERR_PrincipalPermissionInvalidAction, "SecurityAction.LinkDemand").WithArguments("SecurityAction.LinkDemand"))
        End Sub

        <WorkItem(544956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544956")>
        <Fact>
        Public Sub SuppressUnmanagedCodeSecurityAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
imports System

<System.Security.SuppressUnmanagedCodeSecurityAttribute>
Class Program

    <System.Security.SuppressUnmanagedCodeSecurityAttribute>
    public shared sub Main()
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(source)
        End Sub

#End Region

#Region "ComImportAttribute"
        <Fact>
        Public Sub TestCompImport()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices

<ComImport()>
Public Interface I
    Property PI As Object
    Event EI As Action
    Sub MI()
End Interface

<ComImport>
Public Class C
    Dim WithEvents WEC As New EventClass
    
    Property PC As Object
    
    Property QC As Object
      Get
        Return Nothing
      End Get
      Set(value AS Object)
      End Set
    End Property

    Custom Event CEC As Action
        AddHandler(value As Action)
        End AddHandler

        RemoveHandler(value As Action)
        End RemoveHandler

        RaiseEvent()
        End RaiseEvent
    End Event
    
    Event EC As Action
    
    Sub MC
    End Sub
End Class

<ComImport>
Class EventClass
    Public Event XEvent()
End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source, validator:=
                Sub(m)
                    Dim reader = m.GetMetadataReader()
                    For Each methodDef In reader.MethodDefinitions
                        Dim row = reader.GetMethodDefinition(methodDef)
                        Dim name = reader.GetString(row.Name)
                        Dim actual = row.ImplAttributes

                        Dim expected As MethodImplAttributes
                        Select Case name
                            Case ".ctor"
                                Continue For

                            Case "get_WEC",
                                 "get_PC",
                                 "set_PC",
                                 "get_QC",
                                 "set_QC",
                                 "add_CEC",
                                 "remove_CEC",
                                 "raise_CEC",
                                 "MC"
                                ' runtime managed internalcall
                                expected = MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime

                            Case "set_WEC"
                                ' runtime managed internalcall synchronized
                                expected = MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime Or MethodImplAttributes.Synchronized

                            Case "BeginInvoke",
                                 "EndInvoke",
                                 "Invoke"
                                ' runtime managed
                                expected = MethodImplAttributes.Runtime

                            Case "get_PI",
                                 "set_PI",
                                 "add_EI",
                                 "remove_EI",
                                 "MI"
                                ' cil managed
                                expected = MethodImplAttributes.IL

                            Case "add_XEvent",
                                 "remove_XEvent",
                                 "add_EC",
                                 "remove_EC"
                                ' Dev11:  runtime managed internalcall synchronized
                                ' Roslyn: runtime managed internalcall
                                expected = MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime
                        End Select

                        Assert.Equal(expected, actual)
                    Next

                End Sub)
        End Sub
#End Region

#Region "ClassInterfaceAttribute"

        <Fact>
        Public Sub TestClassInterfaceAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

' Valid cases

<Assembly: ClassInterface(ClassInterfaceType.None)>

<ClassInterface(ClassInterfaceType.AutoDispatch)>
Public Class Class1
End Class

<ClassInterface(ClassInterfaceType.AutoDual)>
Public Class Class2
End Class

<ClassInterface(CShort(0))>
Public Class Class4
End Class

<ClassInterface(CShort(1))>
Public Class Class5
End Class

<ClassInterface(CShort(2))>
Public Class Class6
End Class


' Invalid cases

<ClassInterface(DirectCast(-1, ClassInterfaceType))>
Public Class InvalidClass1
End Class

<ClassInterface(DirectCast(3, ClassInterfaceType))>
Public Class InvalidClass2
End Class

<ClassInterface(CShort(-1))>
Public Class InvalidClass3
End Class

<ClassInterface(CShort(3))>
Public Class InvalidClass4
End Class

<ClassInterface(3)>
Public Class InvalidClass5
End Class

<ClassInterface(ClassInterfaceType.None)>
Public Interface InvalidTarget
End Interface
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30127: Attribute 'ClassInterfaceAttribute' is not valid: Incorrect argument value.
<ClassInterface(DirectCast(-1, ClassInterfaceType))>
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'ClassInterfaceAttribute' is not valid: Incorrect argument value.
<ClassInterface(DirectCast(3, ClassInterfaceType))>
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'ClassInterfaceAttribute' is not valid: Incorrect argument value.
<ClassInterface(CShort(-1))>
                ~~~~~~~~~~
BC30127: Attribute 'ClassInterfaceAttribute' is not valid: Incorrect argument value.
<ClassInterface(CShort(3))>
                ~~~~~~~~~
BC30519: Overload resolution failed because no accessible 'New' can be called without a narrowing conversion:
    'Public Overloads Sub New(classInterfaceType As ClassInterfaceType)': Argument matching parameter 'classInterfaceType' narrows from 'Integer' to 'ClassInterfaceType'.
    'Public Overloads Sub New(classInterfaceType As Short)': Argument matching parameter 'classInterfaceType' narrows from 'Integer' to 'Short'.
<ClassInterface(3)>
 ~~~~~~~~~~~~~~
BC30662: Attribute 'ClassInterfaceAttribute' cannot be applied to 'InvalidTarget' because the attribute is not valid on this declaration type.
<ClassInterface(ClassInterfaceType.None)>
 ~~~~~~~~~~~~~~
]]></expected>)
        End Sub

#End Region

#Region "InterfaceTypeAttribute, TypeLibTypeAttribute"

        <Fact>
        Public Sub TestInterfaceTypeAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

' Valid cases

<InterfaceType(ComInterfaceType.InterfaceIsDual)>
Public Interface Interface1
End Interface

<InterfaceType(ComInterfaceType.InterfaceIsIDispatch)>
Public Interface Interface2
End Interface

<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface Interface4
End Interface

' ComInterfaceType.InterfaceIsIInspectable seems to be undefined in version of mscorlib used by the test framework.
<InterfaceType(DirectCast(3, ComInterfaceType))>
Public Interface Interface3
End Interface

<InterfaceType(CShort(0))>
Public Interface Interface5
End Interface

<InterfaceType(CShort(1))>
Public Interface Interface6
End Interface

<InterfaceType(CShort(2))>
Public Interface Interface7
End Interface

<InterfaceType(CShort(3))>
Public Interface Interface8
End Interface


' Invalid cases

<InterfaceType(DirectCast(-1, ComInterfaceType))>
Public Interface InvalidInterface1
End Interface

<InterfaceType(DirectCast(4, ComInterfaceType))>
Public Interface InvalidInterface2
End Interface

<InterfaceType(CShort(-1))>
Public Interface InvalidInterface3
End Interface

<InterfaceType(CShort(4))>
Public Interface InvalidInterface4
End Interface

<InterfaceType(4)>
Public Interface InvalidInterface5
End Interface

<InterfaceType(ComInterfaceType.InterfaceIsDual)>
Public Class InvalidTarget
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30127: Attribute 'InterfaceTypeAttribute' is not valid: Incorrect argument value.
<InterfaceType(DirectCast(-1, ComInterfaceType))>
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'InterfaceTypeAttribute' is not valid: Incorrect argument value.
<InterfaceType(DirectCast(4, ComInterfaceType))>
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30127: Attribute 'InterfaceTypeAttribute' is not valid: Incorrect argument value.
<InterfaceType(CShort(-1))>
               ~~~~~~~~~~
BC30127: Attribute 'InterfaceTypeAttribute' is not valid: Incorrect argument value.
<InterfaceType(CShort(4))>
               ~~~~~~~~~
BC30519: Overload resolution failed because no accessible 'New' can be called without a narrowing conversion:
    'Public Overloads Sub New(interfaceType As ComInterfaceType)': Argument matching parameter 'interfaceType' narrows from 'Integer' to 'ComInterfaceType'.
    'Public Overloads Sub New(interfaceType As Short)': Argument matching parameter 'interfaceType' narrows from 'Integer' to 'Short'.
<InterfaceType(4)>
 ~~~~~~~~~~~~~
BC30662: Attribute 'InterfaceTypeAttribute' cannot be applied to 'InvalidTarget' because the attribute is not valid on this declaration type.
<InterfaceType(ComInterfaceType.InterfaceIsDual)>
 ~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(546664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546664")>
        <Fact()>
        Public Sub TestIsExtensibleInterface()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Imports System
Imports Microsoft.VisualBasic
Imports System.Runtime.InteropServices

' InterfaceTypeAttribute

' Extensible interface
<InterfaceType(ComInterfaceType.InterfaceIsIDispatch)>
Public Interface ExtensibleInterface1
End Interface

' Not Extensible interface
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface NotExtensibleInterface1
End Interface

' Extensible interface via inheritance
Public Interface ExtensibleInterface2
    Inherits ExtensibleInterface1
End Interface

' TypeLibTypeAttribute

' Extensible interface
<TypeLibTypeAttribute(CType(TypeLibTypeFlags.FAppObject, Int16))>
Public Interface ExtensibleInterface3
End Interface

' Extensible interface
<TypeLibTypeAttribute(TypeLibTypeFlags.FAppObject)>
Public Interface ExtensibleInterface4
End Interface

' Extensible interface
<TypeLibTypeAttribute(0)>
Public Interface ExtensibleInterface5
End Interface

' Extensible interface via inheritance
Public Interface ExtensibleInterface6
    Inherits ExtensibleInterface3
End Interface

' Not Extensible interface
<TypeLibTypeAttribute(TypeLibTypeFlags.FNonExtensible)>
Public Interface NotExtensibleInterface2
End Interface

' Not Extensible interface
<TypeLibTypeAttribute(CType(TypeLibTypeFlags.FNonExtensible Or TypeLibTypeFlags.FAppObject, Int16))>
Public Interface NotExtensibleInterface3
End Interface
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll)

            Dim validator =
                Sub(m As ModuleSymbol)
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface1").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface2").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface3").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface4").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface5").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.True(m.GlobalNamespace.GetTypeMember("ExtensibleInterface6").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.False(m.GlobalNamespace.GetTypeMember("NotExtensibleInterface1").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.False(m.GlobalNamespace.GetTypeMember("NotExtensibleInterface2").IsExtensibleInterfaceNoUseSiteDiagnostics())
                    Assert.False(m.GlobalNamespace.GetTypeMember("NotExtensibleInterface3").IsExtensibleInterfaceNoUseSiteDiagnostics())
                End Sub

            CompileAndVerify(compilation, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <WorkItem(546664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546664")>
        <Fact()>
        Public Sub TestIsExtensibleInterface_LateBinding()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Imports System
Imports Microsoft.VisualBasic
Imports System.Runtime.InteropServices

' InterfaceTypeAttribute

' Extensible interface
<InterfaceType(ComInterfaceType.InterfaceIsIDispatch)>
Public Interface ExtensibleInterface1
End Interface

' Not Extensible interface
<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
Public Interface NotExtensibleInterface1
End Interface

' Extensible interface via inheritance
Public Interface ExtensibleInterface2
    Inherits ExtensibleInterface1
End Interface

' TypeLibTypeAttribute

' Extensible interface
<TypeLibTypeAttribute(CType(TypeLibTypeFlags.FAppObject, Int16))>
Public Interface ExtensibleInterface3
End Interface

' Extensible interface
<TypeLibTypeAttribute(TypeLibTypeFlags.FAppObject)>
Public Interface ExtensibleInterface4
End Interface

' Extensible interface
<TypeLibTypeAttribute(0)>
Public Interface ExtensibleInterface5
End Interface

' Extensible interface via inheritance
Public Interface ExtensibleInterface6
    Inherits ExtensibleInterface3
End Interface

' Not Extensible interface
<TypeLibTypeAttribute(TypeLibTypeFlags.FNonExtensible)>
Public Interface NotExtensibleInterface2
End Interface

' Not Extensible interface
<TypeLibTypeAttribute(CType(TypeLibTypeFlags.FNonExtensible Or TypeLibTypeFlags.FAppObject, Int16))>
Public Interface NotExtensibleInterface3
End Interface

Public Class C
    Dim fExtensible1 As ExtensibleInterface1
    Dim fExtensible2 As ExtensibleInterface2
    Dim fExtensible3 As ExtensibleInterface3
    Dim fExtensible4 As ExtensibleInterface4
    Dim fExtensible5 As ExtensibleInterface5
    Dim fExtensible6 As ExtensibleInterface6

    Dim fNotExtensible1 As NotExtensibleInterface1
    Dim fNotExtensible2 As NotExtensibleInterface2
    Dim fNotExtensible3 As NotExtensibleInterface3

    Public Sub Goo()
        fExtensible1.LateBound()
        fExtensible2.LateBound()
        fExtensible3.LateBound()
        fExtensible4.LateBound()
        fExtensible5.LateBound()
        fExtensible6.LateBound()

        fNotExtensible1.LateBound()
        fNotExtensible2.LateBound()
        fNotExtensible3.LateBound()
    End Sub
End Class
]]>
    </file>
</compilation>, options:=TestOptions.ReleaseDll)

            Dim expectedErrors =
<errors><![CDATA[
BC30456: 'LateBound' is not a member of 'NotExtensibleInterface1'.
        fNotExtensible1.LateBound()
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'LateBound' is not a member of 'NotExtensibleInterface2'.
        fNotExtensible2.LateBound()
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'LateBound' is not a member of 'NotExtensibleInterface3'.
        fNotExtensible3.LateBound()
        ~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <WorkItem(546664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546664")>
        <Fact()>
        Public Sub Bug16489_StackOverflow()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Strict Off
Imports System
Imports Microsoft.VisualBasic
Imports System.Runtime.InteropServices

Module Module1
    Class XAttribute
        Inherits Attribute
        Public Sub New(x As Object)
        End Sub
    End Class

    Sub Main()
    End Sub

    <InterfaceType(CType(3, ComInterfaceType))>
    <X(DirectCast(New Object(), II1).GoHome())>
    Interface II1
    End Interface

    Property I1 As II2

    <InterfaceType(ComInterfaceType.InterfaceIsIDispatch)>
    <X(DirectCast(New Object(), II2).GoHome())>
    Interface II2
    End Interface

    Property I2 As II2

    <TypeLibTypeAttribute(CType(TypeLibTypeFlags.FAppObject, Int16))>
    <X(DirectCast(New Object(), II3).GoHome())>
    Interface II3
    End Interface

    Property I3 As II3

    <TypeLibTypeAttribute(TypeLibTypeFlags.FAppObject)>
    <X(DirectCast(New Object(), II4).GoHome())>
    Interface II4
    End Interface

    Property I4 As II4

End Module
]]>
    </file>
</compilation>)

            Dim expectedErrors =
<errors><![CDATA[
BC30059: Constant expression is required.
    <X(DirectCast(New Object(), II1).GoHome())>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    <X(DirectCast(New Object(), II2).GoHome())>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    <X(DirectCast(New Object(), II3).GoHome())>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    <X(DirectCast(New Object(), II4).GoHome())>
       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub
#End Region

#Region "TypeLibVersionAttribute"

        <Fact>
        Public Sub TestTypeLibVersionAttribute_Valid()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: TypeLibVersionAttribute(0, Integer.MaxValue)>
Class C
End Class
]]>
                             </file>
                         </compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestTypeLibVersionAttribute_Valid2()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices
<Assembly: TypeLibVersionAttribute(2147483647, CInt(C.CS * C.CS))> 
Public Class C
    Public Const CS As Integer = Short.MaxValue
End Class
]]>
                             </file>
                         </compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestTypeLibVersionAttribute_Invalid()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: TypeLibVersionAttribute(-1, Integer.MinValue)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30127: Attribute 'TypeLibVersionAttribute' is not valid: Incorrect argument value.
<Assembly: TypeLibVersionAttribute(-1, Integer.MinValue)>
                                   ~~
BC30127: Attribute 'TypeLibVersionAttribute' is not valid: Incorrect argument value.
<Assembly: TypeLibVersionAttribute(-1, Integer.MinValue)>
                                       ~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub TestTypeLibVersionAttribute_Invalid_02()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: TypeLibVersionAttribute("str", 0)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30934: Conversion from 'String' to 'Integer' cannot occur in a constant expression used as an argument to an attribute.
<Assembly: TypeLibVersionAttribute("str", 0)>
                                   ~~~~~
]]></expected>)
        End Sub

#End Region

#Region "ComCompatibleVersionAttribute"

        <Fact>
        Public Sub TestComCompatibleVersionAttribute_Valid()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ComCompatibleVersionAttribute(0, 0, 0, 0)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertNoErrors(comp)
        End Sub

        <Fact>
        Public Sub TestComCompatibleVersionAttribute_Invalid()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30127: Attribute 'ComCompatibleVersionAttribute' is not valid: Incorrect argument value.
<Assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)>
                                         ~~
BC30127: Attribute 'ComCompatibleVersionAttribute' is not valid: Incorrect argument value.
<Assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)>
                                             ~~
BC30127: Attribute 'ComCompatibleVersionAttribute' is not valid: Incorrect argument value.
<Assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)>
                                                 ~~
BC30127: Attribute 'ComCompatibleVersionAttribute' is not valid: Incorrect argument value.
<Assembly: ComCompatibleVersionAttribute(-1, -1, -1, -1)>
                                                     ~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub TestComCompatibleVersionAttribute_Invalid_02()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<Assembly: ComCompatibleVersionAttribute("str", 0, 0, 0)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30934: Conversion from 'String' to 'Integer' cannot occur in a constant expression used as an argument to an attribute.
<Assembly: ComCompatibleVersionAttribute("str", 0, 0, 0)>
                                         ~~~~~
]]></expected>)
        End Sub
#End Region

#Region "GuidAttribute"

        <Fact>
        Public Sub TestInvalidGuidAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

<ComImport>
<Guid("69D3E2A0-BB0F-4FE3-9860-ED714C510756")> ' valid (36 chars)
Class A
End Class

<Guid("69D3E2A0-BB0F-4FE3-9860-ED714C51075")> ' incorrect length (35 chars)
Class B
End Class

<Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")> ' invalid format
Class C
End Class

<Guid("")> ' empty string
Class D
End Class

<Guid(Nothing)> ' Nothing
Class E
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '69D3E2A0-BB0F-4FE3-9860-ED714C51075' is not correct.
<Guid("69D3E2A0-BB0F-4FE3-9860-ED714C51075")> ' incorrect length (35 chars)
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '69D3E2A0BB0F--4FE3-9860-ED714C510756' is not correct.
<Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")> ' invalid format
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '' is not correct.
<Guid("")> ' empty string
      ~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID 'Nothing' is not correct.
<Guid(Nothing)> ' Nothing
      ~~~~~~~
]]></expected>)
        End Sub

        <WorkItem(545490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545490")>
        <Fact>
        Public Sub TestInvalidGuidAttribute_02()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

' Following are alternate valid Guid formats, but disallowed by the native compiler. Ensure we disallow them.

' 32 digits, no hyphens
<Guid("69D3E2A0BB0F4FE39860ED714C510756")>
Class A
End Class

' 32 digits separated by hyphens, enclosed in braces
<Guid("{69D3E2A0-BB0F-4FE3-9860-ED714C510756}")>
Class B
End Class

' 32 digits separated by hyphens, enclosed in parentheses
<Guid("(69D3E2A0-BB0F-4FE3-9860-ED714C510756)")>
Class C
End Class

' Four hexadecimal values enclosed in braces, where the fourth value is a subset of eight hexadecimal values that is also enclosed in braces
<Guid("{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}")>
Class D
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '69D3E2A0BB0F4FE39860ED714C510756' is not correct.
<Guid("69D3E2A0BB0F4FE39860ED714C510756")>
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '{69D3E2A0-BB0F-4FE3-9860-ED714C510756}' is not correct.
<Guid("{69D3E2A0-BB0F-4FE3-9860-ED714C510756}")>
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '(69D3E2A0-BB0F-4FE3-9860-ED714C510756)' is not correct.
<Guid("(69D3E2A0-BB0F-4FE3-9860-ED714C510756)")>
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}' is not correct.
<Guid("{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}")>
      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub TestInvalidGuidAttribute_Assembly()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.InteropServices

' invalid format
<Assembly: Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC32500: 'GuidAttribute' cannot be applied because the format of the GUID '69D3E2A0BB0F--4FE3-9860-ED714C510756' is not correct.
<Assembly: Guid("69D3E2A0BB0F--4FE3-9860-ED714C510756")>
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

#End Region

#Region "WindowsRuntimeImportAttribute"

        <ConditionalFact(GetType(WindowsDesktopOnly))>
        <WorkItem(6190, "https://github.com/dotnet/roslyn/issues/6190")>
        <WorkItem(531295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531295")>
        Public Sub TestWindowsRuntimeImportAttribute()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System
Imports System.Runtime.InteropServices

Namespace System.Runtime.InteropServices.WindowsRuntime
	<AttributeUsage(AttributeTargets.[Class] Or AttributeTargets.[Interface] Or AttributeTargets.[Enum] Or AttributeTargets.Struct Or AttributeTargets.[Delegate], Inherited := False)>
	Friend NotInheritable Class WindowsRuntimeImportAttribute
		Inherits Attribute
		Public Sub New()
		End Sub
	End Class
End Namespace

<System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImport>
Class A
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim sourceValidator =
                Sub(m As ModuleSymbol)
                    Dim assembly = m.ContainingSymbol
                    Dim sysNS = DirectCast(m.DeclaringCompilation.GlobalNamespace.GetMember("System"), NamespaceSymbol)
                    Dim runtimeNS = sysNS.GetNamespace("Runtime")
                    Dim interopNS = runtimeNS.GetNamespace("InteropServices")
                    Dim windowsRuntimeNS = interopNS.GetNamespace("WindowsRuntime")
                    Dim windowsRuntimeImportAttrType = windowsRuntimeNS.GetTypeMembers("WindowsRuntimeImportAttribute").First()

                    Dim typeA = m.GlobalNamespace.GetTypeMember("A")
                    Assert.Equal(1, typeA.GetAttributes(windowsRuntimeImportAttrType).Count)
                    Assert.True(typeA.IsWindowsRuntimeImport, "Metadata flag not set for IsWindowsRuntimeImport")
                End Sub

            Dim metadataValidator =
                Sub(m As ModuleSymbol)
                    Dim typeA = m.GlobalNamespace.GetTypeMember("A")
                    Assert.Equal(0, typeA.GetAttributes().Length)
                    Assert.True(typeA.IsWindowsRuntimeImport, "Metadata flag not set for IsWindowsRuntimeImport")
                End Sub

            ' Verify that PEVerify will fail despite the fact that compiler produces no errors
            ' This is consistent with Dev10 behavior
            '
            ' Dev10 PEVerify failure:
            ' [token  0x02000003] Type load failed.
            '
            ' Dev10 Runtime Exception:
            ' Unhandled Exception: System.TypeLoadException: Windows Runtime types can only be declared in Windows Runtime assemblies.

            Dim validator = CompileAndVerifyEx(source, sourceSymbolValidator:=sourceValidator, symbolValidator:=metadataValidator, verify:=Verification.Fails, targetFramework:=TargetFramework.Mscorlib45)
            validator.EmitAndVerify("Type load failed.")
        End Sub

#End Region

#Region "STAThreadAttribute, MTAThreadAttribute"
        Private Sub VerifySTAThreadAttribute(method As MethodSymbol, expected As Boolean)
            Dim attributes = method.GetAttributes().Where(Function(attribute) attribute.AttributeClass.MetadataName = "STAThreadAttribute")

            If expected Then
                Dim attribute = attributes.Single()
                Assert.Empty(attribute.ConstructorArguments)
                Assert.Empty(attribute.NamedArguments)
            Else
                Assert.Empty(attributes)
            End If
        End Sub

        <Fact>
        Public Sub TestSynthesizedSTAThread()
            Dim source =
    <compilation>
        <file name="a.vb">
            Imports System
            Module Module1
                Sub goo()
                End Sub

                Sub Main()
                End Sub
            End Module
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.AssertNoErrors()

            Dim validator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                           Dim type = m.GlobalNamespace.GetTypeMember("Module1")
                                                           Dim gooMethod = type.GetMethod("goo")
                                                           VerifySTAThreadAttribute(gooMethod, expected:=False)

                                                           Dim mainMethod = type.GetMethod("Main")
                                                           VerifySTAThreadAttribute(mainMethod, expected:=True)
                                                       End Sub

            CompileAndVerify(compilation, symbolValidator:=validator, expectedOutput:="")
        End Sub

        <Fact>
        Public Sub TestNoSynthesizedSTAThread_01()
            Dim source =
    <compilation>
        <file name="a.vb">
            Imports System
            Module Module1
                Sub goo()
                End Sub

                Sub Main()
                End Sub
            End Module
        </file>
    </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.AssertNoErrors()

            Dim validator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                           Dim type = m.GlobalNamespace.GetTypeMember("Module1")
                                                           Dim gooMethod = type.GetMethod("goo")
                                                           VerifySTAThreadAttribute(gooMethod, expected:=False)

                                                           Dim mainMethod = type.GetMethod("Main")
                                                           VerifySTAThreadAttribute(mainMethod, expected:=False)
                                                       End Sub

            CompileAndVerify(compilation, symbolValidator:=validator)
        End Sub

        <Fact>
        Public Sub TestNoSynthesizedSTAThread_02()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[ 
            Imports System
                Module Module1
            Sub goo()
            End Sub

            <STAThread()>
            Sub Main()
            End Sub
            End Module
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.AssertNoErrors()

            Dim sourceValidator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                                 Dim type = m.GlobalNamespace.GetTypeMember("Module1")
                                                                 Dim gooMethod = type.GetMethod("goo")
                                                                 VerifySTAThreadAttribute(gooMethod, expected:=False)

                                                                 Dim mainMethod = type.GetMethod("Main")
                                                                 VerifySTAThreadAttribute(mainMethod, expected:=True)
                                                             End Sub

            Dim peValidator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                             Dim type = m.GlobalNamespace.GetTypeMember("Module1")
                                                             Dim gooMethod = type.GetMethod("goo")
                                                             VerifySTAThreadAttribute(gooMethod, expected:=False)

                                                             Dim mainMethod = type.GetMethod("Main")
                                                             VerifySTAThreadAttribute(mainMethod, expected:=True)
                                                         End Sub

            CompileAndVerify(compilation, sourceSymbolValidator:=sourceValidator, symbolValidator:=peValidator)
        End Sub

        <Fact>
        Public Sub TestNoSynthesizedSTAThread_03()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[ 
            Imports System
                Module Module1
            Sub goo()
            End Sub

            <MTAThread()>
            Sub Main()
            End Sub
            End Module
        ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe)
            compilation.AssertNoErrors()

            Dim validator As Action(Of ModuleSymbol) = Sub(m As ModuleSymbol)
                                                           Dim type = m.GlobalNamespace.GetTypeMember("Module1")
                                                           Dim gooMethod = type.GetMethod("goo")
                                                           VerifySTAThreadAttribute(gooMethod, expected:=False)

                                                           Dim mainMethod = type.GetMethod("Main")
                                                           VerifySTAThreadAttribute(mainMethod, expected:=False)
                                                       End Sub

            CompileAndVerify(compilation, symbolValidator:=validator)
        End Sub
#End Region

#Region "SkipLocalsInitAttribute"

        <Fact>
        Public Sub SkipLocalsInitAttributeOnMethod()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Sub S()
    End Sub

    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Function F() As Integer
        Return 1
    End Function
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnClass()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
Class C
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnProperty()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Property P As Integer
        Get
            Return 1
        End Get

        Set
        End Set
    End Property
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnAccessors()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Property P As Integer
        <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
        Get
            Return 1
        End Get

        <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
        Set
        End Set
    End Property
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
        <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
        <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnModule()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<Module: System.Runtime.CompilerServices.SkipLocalsInitAttribute>

Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<Module: System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnAssembly()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<Assembly: System.Runtime.CompilerServices.SkipLocalsInitAttribute>

Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<Assembly: System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnEnum()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
Enum E
    Member
End Enum
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnEnumMember()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Enum E
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Member1
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Member2
End Enum
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnEvent()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Event E(ByVal i As Integer)
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnDelegate()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Delegate Sub D()
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnInterface()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
Interface I
End Interface
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnStructure()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
Structure S
End Structure
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
<System.Runtime.CompilerServices.SkipLocalsInitAttribute>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnReturnValue()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Function F() As <System.Runtime.CompilerServices.SkipLocalsInitAttribute> Integer
        Return 1
    End Function
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    Function F() As <System.Runtime.CompilerServices.SkipLocalsInitAttribute> Integer
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnParameter()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Sub M(<System.Runtime.CompilerServices.SkipLocalsInitAttribute> ByVal i As Integer)
    End Sub
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    Sub M(<System.Runtime.CompilerServices.SkipLocalsInitAttribute> ByVal i As Integer)
           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub SkipLocalsInitAttributeOnField()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class SkipLocalsInitAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
    Dim i As Integer
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.SkipLocalsInitAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.SkipLocalsInitAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

#End Region

#Region "RequiredAttributeAttribute"

        <Fact, WorkItem(81, "https://github.com/dotnet/roslyn/issues/81")>
        Public Sub DisallowRequiredAttributeInSource()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace VBClassLibrary

    <System.Runtime.CompilerServices.RequiredAttribute(GetType(RS))>
    Public Structure RS
        Public F1 As Integer
        Public Sub New(ByVal p1 As Integer)
            F1 = p1
        End Sub
    End Structure

    <System.Runtime.CompilerServices.RequiredAttribute(GetType(RI))>
    Public Interface RI
        Function F() As Integer
    End Interface

    Public Class CRI
        Implements RI
        Public Function F() As Integer Implements RI.F
            F = 0
        End Function
        Public Shared Frs As RS = New RS(0)
    End Class

End Namespace
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC37235: The RequiredAttribute attribute is not permitted on Visual Basic types.
    <System.Runtime.CompilerServices.RequiredAttribute(GetType(RS))>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37235: The RequiredAttribute attribute is not permitted on Visual Basic types.
    <System.Runtime.CompilerServices.RequiredAttribute(GetType(RI))>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(81, "https://github.com/dotnet/roslyn/issues/81")>
        Public Sub DisallowRequiredAttributeFromMetadata01()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit RequiredAttrClass
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 59 53 79 73 74 65 6D 2E 49 6E 74 33 32 2C   // ..YSystem.Int32,
                                                                                                                                     20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
                                                                                                                                     6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
                                                                                                                                     75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
                                                                                                                                     6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
                                                                                                                                     35 63 35 36 31 39 33 34 65 30 38 39 00 00 )       // 5c561934e089..
  .field public int32 intVar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RequiredAttrClass::.ctor

} // end of class RequiredAttrClass
                ]]>

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Module M
    Sub Main()
        Dim r = New RequiredAttrClass()
        System.Console.WriteLine(r)
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim ilReference = CompileIL(ilSource.Value)

            Dim comp = CreateCompilationWithMscorlib40AndReferences(source, references:={MsvbRef, ilReference})
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30649: 'RequiredAttrClass' is an unsupported type.
        Dim r = New RequiredAttrClass()
                    ~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(81, "https://github.com/dotnet/roslyn/issues/81")>
        Public Sub DisallowRequiredAttributeFromMetadata02()
            Dim ilSource = <![CDATA[
.class public auto ansi beforefieldinit RequiredAttr.Scenario1
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 59 53 79 73 74 65 6D 2E 49 6E 74 33 32 2C   // ..YSystem.Int32,
                                                                                                                                     20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
                                                                                                                                     6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
                                                                                                                                     75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
                                                                                                                                     6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
                                                                                                                                     35 63 35 36 31 39 33 34 65 30 38 39 00 00 )       // 5c561934e089..
  .field public int32 intVar
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Scenario1::.ctor

} // end of class RequiredAttr.Scenario1

.class public auto ansi beforefieldinit RequiredAttr.ReqAttrUsage
       extends [mscorlib]System.Object
{
  .field public class RequiredAttr.Scenario1 sc1_field
  .method public hidebysig newslot specialname virtual 
          instance class RequiredAttr.Scenario1 
          get_sc1_prop() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals (class RequiredAttr.Scenario1 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::sc1_field
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ret
  } // end of method ReqAttrUsage::get_sc1_prop

  .method public hidebysig instance class RequiredAttr.Scenario1 
          sc1_method() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals (class RequiredAttr.Scenario1 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::sc1_field
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ret
  } // end of method ReqAttrUsage::sc1_method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ReqAttrUsage::.ctor

  .property instance class RequiredAttr.Scenario1
          sc1_prop()
  {
    .get instance class RequiredAttr.Scenario1 RequiredAttr.ReqAttrUsage::get_sc1_prop()
  } // end of property ReqAttrUsage::sc1_prop
} // end of class RequiredAttr.ReqAttrUsage
                ]]>

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports RequiredAttr

Public Class C
    Public Shared Function Main() As Integer
        Dim r = New ReqAttrUsage()
        r.sc1_field = Nothing
        Dim o As Object = r.sc1_prop
        r.sc1_method()
        Return 1
    End Function
End Class
]]>
                             </file>
                         </compilation>

            Dim ilReference = CompileIL(ilSource.Value)

            Dim comp = CreateCompilationWithMscorlib40(source, references:={ilReference})
            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC30656: Field 'sc1_field' is of an unsupported type.
        r.sc1_field = Nothing
        ~~~~~~~~~~~
BC30643: Property 'sc1_prop' is of an unsupported type.
        Dim o As Object = r.sc1_prop
                            ~~~~~~~~
BC30657: 'sc1_method' has a return type that is not supported or parameter types that are not supported.
        r.sc1_method()
          ~~~~~~~~~~
]]></expected>)
        End Sub

#End Region

#Region "ModuleInitializerAttribute"
        <Fact>
        Public Sub ModuleInitializerAttributeOnMethod()
            Dim source =
            <compilation>
                <file name="attr.vb"><![CDATA[
Namespace System.Runtime.CompilerServices
    Public Class ModuleInitializerAttribute
        Inherits Attribute
    End Class
End Namespace

Class Program
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Sub S()
    End Sub

    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Function F() As Integer
        Return 1
    End Function
End Class
]]>
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.ModuleInitializerAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42381: 'System.Runtime.CompilerServices.ModuleInitializerAttribute' is not supported in VB.
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnClass()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.ModuleInitializerAttribute>
Class C
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnProperty()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Property P As Integer
        Get
            Return 1
        End Get

        Set
        End Set
    End Property
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnAccessors()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Property P As Integer
        <System.Runtime.CompilerServices.ModuleInitializerAttribute>
        Get
            Return 1
        End Get

        <System.Runtime.CompilerServices.ModuleInitializerAttribute>
        Set
        End Set
    End Property
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected><![CDATA[
BC42381: 'System.Runtime.CompilerServices.ModuleInitializerAttribute' is not supported in VB.
        <System.Runtime.CompilerServices.ModuleInitializerAttribute>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42381: 'System.Runtime.CompilerServices.ModuleInitializerAttribute' is not supported in VB.
        <System.Runtime.CompilerServices.ModuleInitializerAttribute>
         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnModule()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<Module: System.Runtime.CompilerServices.ModuleInitializerAttribute>

Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnAssembly()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
<Assembly: System.Runtime.CompilerServices.ModuleInitializerAttribute>

Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnEnum()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.ModuleInitializerAttribute>
Enum E
    Member
End Enum
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnEnumMember()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Enum E
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Member1
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Member2
End Enum
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnEvent()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Event E(ByVal i As Integer)
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnDelegate()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Delegate Sub D()
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnInterface()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.ModuleInitializerAttribute>
Interface I
End Interface
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnStructure()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.ModuleInitializerAttribute>
Structure S
End Structure
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnReturnValue()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Function F() As <System.Runtime.CompilerServices.ModuleInitializerAttribute> Integer
        Return 1
    End Function
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnParameter()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    Sub M(<System.Runtime.CompilerServices.ModuleInitializerAttribute> ByVal i As Integer)
    End Sub
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

        <Fact>
        Public Sub ModuleInitializerAttributeOnField()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Namespace System.Runtime.CompilerServices
    Class ModuleInitializerAttribute
        Inherits System.Attribute
    End Class
End Namespace

Class C
    <System.Runtime.CompilerServices.ModuleInitializerAttribute>
    Dim i As Integer
End Class
]]>

                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            CompilationUtils.AssertNoDiagnostics(comp)
        End Sub

#End Region

        <Fact, WorkItem(807, "https://github.com/dotnet/roslyn/issues/807")>
        Public Sub TestAttributePropagationForAsyncAndIterators_01()
            Dim source =
            <compilation>
                <file name="attr.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

Class Program

    Shared Sub Main()
    End Sub

    <MyAttribute>
    <System.Diagnostics.DebuggerNonUserCodeAttribute>
    <System.Diagnostics.DebuggerHiddenAttribute>
    <System.Diagnostics.DebuggerStepperBoundaryAttribute>
    <System.Diagnostics.DebuggerStepThroughAttribute>
    Public Async Function test1() As Task(Of Integer)
        Return Await DoNothing()
    End Function

    Public Async Function test2() As Task(Of Integer)
        Return Await DoNothing()
    End Function

    Private Async Function DoNothing() As Task(Of Integer)
        Return 1
    End Function

    <MyAttribute>
    <System.Diagnostics.DebuggerNonUserCodeAttribute>
    <System.Diagnostics.DebuggerHiddenAttribute>
    <System.Diagnostics.DebuggerStepperBoundaryAttribute>
    <System.Diagnostics.DebuggerStepThroughAttribute>
    Public Iterator Function Test3() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function

    Public Iterator Function Test4() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function
End Class

Class MyAttribute
    Inherits System.Attribute
End Class
            ]]>
                </file>
            </compilation>

            Dim attributeValidator As Action(Of ModuleSymbol) =
            Sub(m As ModuleSymbol)
                Dim program = m.GlobalNamespace.GetTypeMember("Program")

                Assert.Equal("", CheckAttributePropagation(DirectCast(program.GetMember(Of MethodSymbol)("test1").
                                                           GetAttributes("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol).
                                                           GetMember(Of MethodSymbol)("MoveNext")))

                Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", DirectCast(program.GetMember(Of MethodSymbol)("test2").
                                GetAttributes("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").Single().
                                ConstructorArguments.Single().Value, NamedTypeSymbol).
                                GetMember(Of MethodSymbol)("MoveNext").GetAttributes().Single().AttributeClass.ToTestDisplayString())

                Assert.Equal("", CheckAttributePropagation(DirectCast(program.GetMember(Of MethodSymbol)("Test3").
                                                           GetAttributes("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol).
                                                           GetMember(Of MethodSymbol)("MoveNext")))

                Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", DirectCast(program.GetMember(Of MethodSymbol)("Test4").
                                GetAttributes("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").Single().
                                ConstructorArguments.Single().Value, NamedTypeSymbol).
                                GetMember(Of MethodSymbol)("MoveNext").GetAttributes().Single().AttributeClass.ToTestDisplayString())
            End Sub

            CompileAndVerify(CreateCompilationWithMscorlib45AndVBRuntime(source), symbolValidator:=attributeValidator)
        End Sub

        Private Shared Function CheckAttributePropagation(symbol As Symbol) As String
            Dim result = ""

            If symbol.GetAttributes("", "MyAttribute").Any() Then
                result += "MyAttribute is present" & vbCr
            End If

            If Not symbol.GetAttributes("System.Diagnostics", "DebuggerNonUserCodeAttribute").Any() Then
                result += "DebuggerNonUserCodeAttribute is missing" & vbCr
            End If

            If Not symbol.GetAttributes("System.Diagnostics", "DebuggerHiddenAttribute").Any() Then
                result += "DebuggerHiddenAttribute is missing" & vbCr
            End If

            If Not symbol.GetAttributes("System.Diagnostics", "DebuggerStepperBoundaryAttribute").Any() Then
                result += "DebuggerStepperBoundaryAttribute is missing" & vbCr
            End If

            If Not symbol.GetAttributes("System.Diagnostics", "DebuggerStepThroughAttribute").Any() Then
                result += "DebuggerStepThroughAttribute is missing" & vbCr
            End If

            If Not symbol.GetAttributes("System.Runtime.CompilerServices", "CompilerGeneratedAttribute").Any() Then
                result += "CompilerGeneratedAttribute is missing" & vbCr
            End If

            Return result
        End Function

        <Fact, WorkItem(4521, "https://github.com/dotnet/roslyn/issues/4521")>
        Public Sub TestAttributePropagationForAsyncAndIterators_02()
            Dim source =
            <compilation>
                <file name="attr.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks

<MyAttribute>
<System.Diagnostics.DebuggerNonUserCodeAttribute>
<System.Diagnostics.DebuggerStepThroughAttribute>
Class Program1

    Shared Sub Main()
    End Sub

    Public Async Function test1() As Task(Of Integer)
        Return Await DoNothing()
    End Function

    Private Async Function DoNothing() As Task(Of Integer)
        Return 1
    End Function

    Public Iterator Function Test3() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function
End Class

Class Program2

    Shared Sub Main()
    End Sub

    Public Async Function test2() As Task(Of Integer)
        Return Await DoNothing()
    End Function

    Private Async Function DoNothing() As Task(Of Integer)
        Return 1
    End Function

    Public Iterator Function Test4() As IEnumerable(Of Integer)
        Yield 1
        Yield 2
    End Function
End Class

Class MyAttribute
    Inherits System.Attribute
End Class
            ]]>
                </file>
            </compilation>

            Dim attributeValidator As Action(Of ModuleSymbol) =
            Sub(m As ModuleSymbol)
                Dim program1 = m.GlobalNamespace.GetTypeMember("Program1")
                Dim program2 = m.GlobalNamespace.GetTypeMember("Program2")

                Assert.Equal("DebuggerHiddenAttribute is missing" & vbCr & "DebuggerStepperBoundaryAttribute is missing" & vbCr,
                             CheckAttributePropagation(DirectCast(program1.GetMember(Of MethodSymbol)("test1").
                                                           GetAttributes("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol)))

                Assert.Equal("DebuggerNonUserCodeAttribute is missing" & vbCr & "DebuggerHiddenAttribute is missing" & vbCr & "DebuggerStepperBoundaryAttribute is missing" & vbCr & "DebuggerStepThroughAttribute is missing" & vbCr,
                             CheckAttributePropagation(DirectCast(program2.GetMember(Of MethodSymbol)("test2").
                                                           GetAttributes("System.Runtime.CompilerServices", "AsyncStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol)))

                Assert.Equal("DebuggerHiddenAttribute is missing" & vbCr & "DebuggerStepperBoundaryAttribute is missing" & vbCr,
                             CheckAttributePropagation(DirectCast(program1.GetMember(Of MethodSymbol)("Test3").
                                                           GetAttributes("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol)))

                Assert.Equal("DebuggerNonUserCodeAttribute is missing" & vbCr & "DebuggerHiddenAttribute is missing" & vbCr & "DebuggerStepperBoundaryAttribute is missing" & vbCr & "DebuggerStepThroughAttribute is missing" & vbCr,
                             CheckAttributePropagation(DirectCast(program2.GetMember(Of MethodSymbol)("Test4").
                                                           GetAttributes("System.Runtime.CompilerServices", "IteratorStateMachineAttribute").Single().
                                                           ConstructorArguments.Single().Value, NamedTypeSymbol)))
            End Sub

            CompileAndVerify(CreateCompilationWithMscorlib45AndVBRuntime(source), symbolValidator:=attributeValidator)
        End Sub

        <Fact, WorkItem(10639, "https://github.com/dotnet/roslyn/issues/10639")>
        Public Sub UsingStaticDirectiveDoesNotIgnoreObsoleteAttribute_DifferentSeverity()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System
Imports TestAssembly.TestError
Imports TestAssembly.TestWarning

<Obsolete("Broken Error Class", True)>
Public Class TestError
    Public Shared Sub TestErrorFunc()
    End Sub
End Class

<Obsolete("Broken Warning Class", False)>
Public Class TestWarning
    Public Shared Sub TestWarningFunc()
    End Sub
End Class

Public Module Test
    Public Sub Main()
        TestErrorFunc()
        TestWarningFunc()
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, rootNamespace:="TestAssembly")
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, options)

            compilation.AssertTheseDiagnostics(<expected><![CDATA[
BC30668: 'TestError' is obsolete: 'Broken Error Class'.
Imports TestAssembly.TestError
        ~~~~~~~~~~~~~~~~~~~~~~
BC40000: 'TestWarning' is obsolete: 'Broken Warning Class'.
Imports TestAssembly.TestWarning
        ~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(10639, "https://github.com/dotnet/roslyn/issues/10639")>
        Public Sub UsingStaticDirectiveDoesNotIgnoreObsoleteAttribute_NestedClasses()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System
Imports TestAssembly.ActiveParent.ObsoleteChild
Imports TestAssembly.ObsoleteParent.ActiveChild
Imports TestAssembly.BothObsoleteParent.BothObsoleteChild

Public Class ActiveParent
    <Obsolete>
    Public Class ObsoleteChild
        Public Shared Sub ObsoleteChildFunc()
        End Sub
    End Class
End Class

<Obsolete>
Public Class ObsoleteParent
    Public Class ActiveChild
        Public Shared Sub ActiveChildFunc()
        End Sub
    End Class
End Class

<Obsolete>
Public Class BothObsoleteParent
    <Obsolete>
    Public Class BothObsoleteChild
        Public Shared Sub BothObsoleteFunc()
        End Sub
    End Class
End Class

Public Module Test
    Public Sub Main()
        ObsoleteChildFunc()
        ActiveChildFunc()
        BothObsoleteFunc()
    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, rootNamespace:="TestAssembly")
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, options)

            compilation.AssertTheseDiagnostics(<expected><![CDATA[
BC40008: 'ActiveParent.ObsoleteChild' is obsolete.
Imports TestAssembly.ActiveParent.ObsoleteChild
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40008: 'ObsoleteParent' is obsolete.
Imports TestAssembly.ObsoleteParent.ActiveChild
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40008: 'BothObsoleteParent' is obsolete.
Imports TestAssembly.BothObsoleteParent.BothObsoleteChild
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC40008: 'BothObsoleteParent.BothObsoleteChild' is obsolete.
Imports TestAssembly.BothObsoleteParent.BothObsoleteChild
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        <WorkItem(19394, "https://github.com/dotnet/roslyn/issues/19394")>
        Public Sub WellKnownTypeAsStruct_DefaultConstructor_ParamArrayAttribute()
            Dim code = <compilation><file name="a.vb"><![CDATA[
Namespace System
	public Structure ParamArrayAttribute
	End Structure
End Namespace
Public Class C
    Public Sub Test(ByVal ParamArray args() As Double)
    End Sub
End Class
]]></file></compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(code).VerifyDiagnostics().AssertTheseEmitDiagnostics(<expected><![CDATA[
BC31503: 'ParamArrayAttribute' cannot be used as an attribute because it is not a class.
]]></expected>)
        End Sub

        <Fact>
        <WorkItem(19394, "https://github.com/dotnet/roslyn/issues/19394")>
        Public Sub WellKnownTypeAsStruct_NonDefaultConstructor_TupleElementNamesAttribute()
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(
<compilation>
    <file name="errors.vb"><![CDATA[
Imports System

Namespace System.Runtime.CompilerServices
    Public Structure TupleElementNamesAttribute
        Public Sub New(transformNames As String())
        End Sub
    End Structure
End Namespace

Module Program
    Public Sub Main(args As String())
        Test(("first", "second"))
    End Sub

    Public Sub Test(tuple As (a As String, b As String))
        Console.WriteLine(tuple.a)
        Console.WriteLine(tuple.b)
    End Sub
End Module
]]></file>
</compilation>,
                references:={ValueTupleRef, SystemRuntimeFacadeRef},
                options:=TestOptions.ReleaseExe)

            CompileAndVerify(
                compilation,
                expectedOutput:="
first
second",
                symbolValidator:=
                    Sub([module] As ModuleSymbol)
                        Dim attribute = [module].ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("Test").Parameters.Single().GetAttributes().Single()

                        Assert.Equal("System.Runtime.CompilerServices.TupleElementNamesAttribute", attribute.AttributeClass.ToTestDisplayString())
                        Assert.True(attribute.AttributeClass.IsStructureType())
                        Assert.Equal([module].ContainingAssembly, attribute.AttributeClass.ContainingAssembly)
                        Assert.Equal("transformNames", attribute.AttributeConstructor.Parameters.Single().Name)
                    End Sub)
        End Sub

        <Fact>
        <WorkItem(59003, "https://github.com/dotnet/roslyn/issues/59003")>
        Public Sub ErrorInPropertyValue_01()
            Dim source =
<compilation>
    <file><![CDATA[
class C
    <System.Runtime.CompilerServices.MethodImpl(MethodCodeType := System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)>
    public Function Count() as Integer
        return 0
    end function
end class
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilation(source)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <System.Runtime.CompilerServices.MethodImpl(MethodCodeType := System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)>
                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )
        End Sub

        <Fact>
        <WorkItem(59003, "https://github.com/dotnet/roslyn/issues/59003")>
        Public Sub ErrorInPropertyValue_02()
            Dim source =
<compilation>
    <file><![CDATA[
class C
    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized, MethodCodeType := System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)>
    public Function Count() as Integer
        return 0
    end function
end class
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilation(source)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30127: Attribute 'MethodImplAttribute' is not valid: Incorrect argument value.
    <System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized, MethodCodeType := System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)>
                                                                                                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>
            )
        End Sub

        <Fact()>
        Public Sub ErrorInPropertyValue_03()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System.Runtime.InteropServices

<StructLayout(CharSet:=0)>
public structure S1
end structure

<StructLayout(LayoutKind.Sequential, CharSet:=0)>
public structure S2
end structure
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilation(source)
            compilation.AssertTheseDiagnostics(
<expected><![CDATA[
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
<StructLayout(CharSet:=0)>
 ~~~~~~~~~~~~
BC30127: Attribute 'StructLayoutAttribute' is not valid: Incorrect argument value.
<StructLayout(LayoutKind.Sequential, CharSet:=0)>
                                     ~~~~~~~~~~
]]></expected>
            )
        End Sub
    End Class
End Namespace
