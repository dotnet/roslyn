' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class AttributeTests_Tuples
        Inherits BasicTestBase

        Private Const s_tuplesTestSource As String =
"Imports System

Public Class Base0
End Class
Public Class Base1(Of T)
End Class
Public Class Base2(Of T, U)
End Class

Public Class Outer(Of T)
    Inherits Base1(Of (key As Integer, val As Integer))
    Public Class Inner(Of U, V)
        Inherits Base2(Of (key2 As Integer, val2 As Integer), V)
        Public Class InnerInner(Of W)
            Inherits Base1(Of (key3 As Integer, val2 As Integer))
        End Class
    End Class
End Class

Public Class Derived(Of T)
    Inherits Outer(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer))).
        Inner(Of
            Outer(Of (e5 As Integer, e6 As Integer)).
                Inner(Of (e7 As Integer, e8 As Integer)(), (e9 As Integer, e10 As Integer)).
                    InnerInner(Of Integer)(),
            (e13 As (e11 As Integer, e12 As Integer), e14 As Integer)).
            InnerInner(Of (e17 As (e15 As Integer, e16 As Integer), e22 As (e18 As Integer, e21 As Base1(Of (e19 As Integer, e20 As Integer)))))

    Public Shared Field1 As (e1 As Integer, e2 As Integer)
    Public Shared Field2 As (e1 As Integer, e2 As Integer)
    Public Shared Field3 As Base1(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer)))
    Public Shared Field4 As ValueTuple(Of Base1(Of (e1 As Integer, e2 As (Integer, (Object, Object)))), Integer)
    Public Shared Field5 As Outer(Of (e1 As Object, e2 As Object)).Inner(Of (e3 As Object, e4 As Object), ValueTuple(Of Object, Object))
    ' No names
    Public Shared Field6 As Base1(Of (Integer, ValueTuple(Of Integer, ValueTuple)))
    Public Shared Field7 As ValueTuple
    ' Long tuples
    Public Shared Field8 As (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer)
    Public Shared Field9 As Base1(Of (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer))

    Public Shared Function Method1() As (e1 As Integer, e2 As Integer)
        Return Nothing
    End Function
    Public Shared Sub Method2(x As (e1 As Integer, e2 As Integer))
    End Sub
    Public Shared Function Method3(x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
        Return x
    End Function
    Public Shared Function Method4(ByRef x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
        Return x
    End Function
    Public Shared Function Method5(ByRef x As (Object, Object)) As ((Integer, (Object, (Object, Object)), Object, Integer), ValueTuple)
        Return Nothing
    End Function
    Public Shared Function Method6() As (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer)
        Return Nothing
    End Function

    Public Shared ReadOnly Property Prop1 As (e1 As Integer, e2 As Integer)
        Get
            Return Nothing
        End Get
    End Property
    Public Shared Property Prop2 As (e1 As Integer, e2 As Integer)

    Public Default Property Prop3(param As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
        Get
            Return param
        End Get
        Set
        End Set
    End Property

    Public Delegate Sub Delegate1(Of V)(sender As Object, args As ValueTuple(Of V, (e4 As (e1 As Object, e2 As Object, e3 As Object), e5 As Object), (Object, Object)))

    Public Shared Custom Event Event1 As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object))))
        AddHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
        End AddHandler
        RemoveHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class"

        Private Shared ReadOnly s_valueTupleRefs As MetadataReference() = New MetadataReference() {ValueTupleRef, SystemRuntimeFacadeRef}

        <Fact>
        Public Sub TestCompile()
            Dim comp = CreateCompilationWithMscorlib40({s_tuplesTestSource}, options:=TestOptions.ReleaseDll, references:=s_valueTupleRefs)
            CompileAndVerify(comp)
        End Sub

        <Fact>
        Public Sub TestTupleAttributes()
            Dim comp = CreateCompilationWithMscorlib40({s_tuplesTestSource}, options:=TestOptions.ReleaseDll, references:=s_valueTupleRefs)
            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol) TupleAttributeValidator.ValidateTupleAttributes(m.ContainingAssembly))
        End Sub

        <Fact>
        Public Sub TupleAttributeWithOnlyOneConstructor()
            Const attributeSource =
"Namespace System.Runtime.CompilerServices
    Public Class TupleElementNamesAttribute
        Inherits Attribute
        Public Sub New(names() As String)
        End Sub
    End Class
End Namespace"
            Dim comp0 = CreateCSharpCompilation(TestResources.NetFX.ValueTuple.tuplelib_cs)
            comp0.VerifyDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40({s_tuplesTestSource, attributeSource}, options:=TestOptions.ReleaseDll, references:={SystemRuntimeFacadeRef, ref0})
            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol) TupleAttributeValidator.ValidateTupleAttributes(m.ContainingAssembly))
        End Sub

        <Fact>
        Public Sub TupleLambdaParametersMissingString()
            Const source0 =
"Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Class MulticastDelegate
    End Class
    Public Interface IAsyncResult
    End Interface
    Public Class AsyncCallback
    End Class
End Namespace"
            Const source1 =
"Delegate Sub D(Of T)(o As T)
Class C
    Shared Sub Main()
        Dim d As D(Of (x As Integer, y As Integer)) = Sub(o)
            End Sub
        d((0, 0))
    End Sub
End Class"
            Dim comp = CreateEmptyCompilation({source0}, assemblyName:="corelib")
            comp.AssertTheseDiagnostics()
            Dim ref0 = comp.EmitToImageReference()
            comp = CreateEmptyCompilation({source1}, references:={ValueTupleRef, SystemRuntimeFacadeRef, ref0})
            comp.AssertTheseDiagnostics(
                <expected>
BC30652: Reference required to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' containing the type 'ValueType'. Add one to your project.
        Dim d As D(Of (x As Integer, y As Integer)) = Sub(o)
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'String' from assembly or module 'corelib.dll' failed.
        Dim d As D(Of (x As Integer, y As Integer)) = Sub(o)
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' containing the type 'ValueType'. Add one to your project.
        d((0, 0))
          ~~~~~~
                </expected>)
        End Sub

        <Fact>
        Public Sub TupleInDeclarationFailureWithoutString()
            Const source0 =
"Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
End Namespace"
            Const source1 =
"Class C
    Shared Function M() As (x As Integer, y As Integer)
        Return Nothing
    End Function
End Class"
            Dim comp = CreateEmptyCompilation({source0}, assemblyName:="corelib")
            comp.AssertTheseDiagnostics()
            Dim ref0 = comp.EmitToImageReference()
            comp = CreateEmptyCompilation({source1}, references:={ValueTupleRef, SystemRuntimeFacadeRef, ref0})
            comp.AssertTheseDiagnostics(
                <expected>
BC30652: Reference required to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' containing the type 'ValueType'. Add one to your project.
    Shared Function M() As (x As Integer, y As Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'String' from assembly or module 'corelib.dll' failed.
    Shared Function M() As (x As Integer, y As Integer)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                </expected>)
        End Sub

        <Fact>
        Public Sub RoundTrip()
            Dim comp = CreateCompilationWithMscorlib40({s_tuplesTestSource}, options:=TestOptions.ReleaseDll, references:=s_valueTupleRefs)
            Dim sourceModule As ModuleSymbol = Nothing
            Dim peModule As ModuleSymbol = Nothing
            CompileAndVerify(comp,
                sourceSymbolValidator:=Sub(m) sourceModule = m,
                symbolValidator:=Sub(m) peModule = m)

            Dim srcTypes = sourceModule.GlobalNamespace.GetTypeMembers()
            Dim peTypes = peModule.GlobalNamespace.GetTypeMembers().WhereAsArray(Function(t) t.Name <> "<Module>")

            Assert.Equal(srcTypes.Length, peTypes.Count)

            For i = 0 To srcTypes.Length - 1
                Dim srcType = srcTypes(i)
                Dim peType = peTypes(i)

                Assert.Equal(ToTestString(srcType.BaseType), ToTestString(peType.BaseType))

                Dim srcMembers = srcType.GetMembers().Where(AddressOf IncludeMember).Select(AddressOf ToTestString).ToList()
                Dim peMembers = peType.GetMembers().Select(AddressOf ToTestString).ToList()

                srcMembers.Sort()
                peMembers.Sort()
                AssertEx.Equal(srcMembers, peMembers)
            Next
        End Sub

        Private Shared Function IncludeMember(s As Symbol) As Boolean
            Select Case s.Kind
                Case SymbolKind.Method
                    If DirectCast(s, MethodSymbol).MethodKind = MethodKind.EventRaise Then
                        Return False
                    End If
                Case SymbolKind.Field
                    If DirectCast(s, FieldSymbol).AssociatedSymbol IsNot Nothing Then
                        Return False
                    End If
            End Select
            Return True
        End Function

        Private Shared Function ToTestString(symbol As Symbol) As String
            Dim typeSymbols = ArrayBuilder(Of TypeSymbol).GetInstance()
            Select Case symbol.Kind
                Case SymbolKind.Method
                    Dim method = DirectCast(symbol, MethodSymbol)
                    typeSymbols.Add(method.ReturnType)
                    typeSymbols.AddRange(method.Parameters.SelectAsArray(Function(p) p.Type))
                Case SymbolKind.NamedType
                    Dim type = DirectCast(symbol, NamedTypeSymbol)
                    typeSymbols.Add(If(type.BaseType, type))
                Case SymbolKind.Field
                    typeSymbols.Add(DirectCast(symbol, FieldSymbol).Type)
                Case SymbolKind.Property
                    typeSymbols.Add(DirectCast(symbol, PropertySymbol).Type)
                Case SymbolKind.Event
                    typeSymbols.Add(DirectCast(symbol, EventSymbol).Type)
            End Select
            Dim symbolString = String.Join(" | ", typeSymbols.Select(Function(s) s.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            typeSymbols.Free()
            Return $"{symbol.Name}: {symbolString}"
        End Function

        Private Structure TupleAttributeValidator
            Private ReadOnly _base0Class As NamedTypeSymbol
            Private ReadOnly _base1Class As NamedTypeSymbol
            Private ReadOnly _base2Class As NamedTypeSymbol
            Private ReadOnly _outerClass As NamedTypeSymbol
            Private ReadOnly _derivedClass As NamedTypeSymbol

            Private Sub New(assembly As AssemblySymbol)
                Dim globalNs = assembly.GlobalNamespace
                _base0Class = globalNs.GetTypeMember("Base0")
                _base1Class = globalNs.GetTypeMember("Base1")
                _base2Class = globalNs.GetTypeMember("Base2")
                _outerClass = globalNs.GetTypeMember("Outer")
                _derivedClass = globalNs.GetTypeMember("Derived")
            End Sub

            Shared Sub ValidateTupleAttributes(assembly As AssemblySymbol)
                Dim validator = New TupleAttributeValidator(assembly)
                validator.ValidateAttributesOnNamedTypes()
                validator.ValidateAttributesOnFields()
                validator.ValidateAttributesOnMethods()
                validator.ValidateAttributesOnProperties()
                validator.ValidateAttributesOnEvents()
                validator.ValidateAttributesOnDelegates()
            End Sub

            Private Sub ValidateAttributesOnDelegates()
                Dim delegate1 = _derivedClass.GetMember(Of NamedTypeSymbol)("Delegate1")
                Assert.True(delegate1.IsDelegateType())

                Dim invokeMethod = delegate1.DelegateInvokeMethod
                ValidateTupleNameAttribute(invokeMethod, expectedTupleNamesAttribute:=False)

                Assert.Equal(2, invokeMethod.ParameterCount)
                Dim sender = invokeMethod.Parameters(0)
                Assert.Equal("sender", sender.Name)
                Assert.Equal(SpecialType.System_Object, sender.Type.SpecialType)
                ValidateTupleNameAttribute(sender, expectedTupleNamesAttribute:=False)

                Dim args = invokeMethod.Parameters(1)
                Assert.Equal("args", args.Name)
                ValidateTupleNameAttribute(args, expectedTupleNamesAttribute:=True, expectedElementNames:={Nothing, Nothing, Nothing, "e4", "e5", "e1", "e2", "e3", Nothing, Nothing})
            End Sub

            Private Sub ValidateAttributesOnEvents()
                Dim event1 = _derivedClass.GetMember(Of EventSymbol)("Event1")
                ValidateTupleNameAttribute(event1, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e4", Nothing, "e2", "e3"})
            End Sub

            Private Sub ValidateAttributesOnNamedTypes()
                ValidateTupleNameAttribute(_base0Class, expectedTupleNamesAttribute:=False)
                ValidateTupleNameAttribute(_base1Class, expectedTupleNamesAttribute:=False)
                ValidateTupleNameAttribute(_base2Class, expectedTupleNamesAttribute:=False)
                Assert.True(_outerClass.BaseType.ContainsTuple())
                ValidateTupleNameAttribute(_outerClass, expectedTupleNamesAttribute:=True, expectedElementNames:={"key", "val"})
                ValidateTupleNameAttribute(
                    _derivedClass,
                    expectedTupleNamesAttribute:=True,
                    expectedElementNames:={"e1", "e4", "e2", "e3", "e5", "e6", "e7", "e8", "e9", "e10", "e13", "e14", "e11", "e12", "e17", "e22", "e15", "e16", "e18", "e21", "e19", "e20"})
            End Sub

            Private Sub ValidateAttributesOnFields()
                Dim field1 = _derivedClass.GetMember(Of FieldSymbol)("Field1")
                ValidateTupleNameAttribute(field1, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                Dim field2 = _derivedClass.GetMember(Of FieldSymbol)("Field2")
                ValidateTupleNameAttribute(field2, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                Dim field3 = _derivedClass.GetMember(Of FieldSymbol)("Field3")
                ValidateTupleNameAttribute(field3, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e4", "e2", "e3"})
                Dim field4 = _derivedClass.GetMember(Of FieldSymbol)("Field4")
                ValidateTupleNameAttribute(field4, expectedTupleNamesAttribute:=True, expectedElementNames:={Nothing, Nothing, "e1", "e2", Nothing, Nothing, Nothing, Nothing})
                Dim field5 = _derivedClass.GetMember(Of FieldSymbol)("Field5")
                ValidateTupleNameAttribute(field5, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2", "e3", "e4", Nothing, Nothing})

                Dim field6 = _derivedClass.GetMember(Of FieldSymbol)("Field6")
                ValidateTupleNameAttribute(field6, expectedTupleNamesAttribute:=False)
                Dim field6Type = DirectCast(field6.Type, NamedTypeSymbol)
                Assert.Equal("Base1", field6Type.Name)
                Assert.Equal(1, field6Type.TypeParameters.Length)
                Dim firstTuple = field6Type.TypeArguments.Single()
                Assert.True(firstTuple.IsTupleType)
                Assert.True(firstTuple.TupleElementNames.IsDefault)
                Assert.Equal(2, firstTuple.TupleElementTypes.Length)
                Dim secondTuple = firstTuple.TupleElementTypes(1)
                Assert.True(secondTuple.IsTupleType)
                Assert.True(secondTuple.TupleElementNames.IsDefault)
                Assert.Equal(2, secondTuple.TupleElementTypes.Length)

                Dim field7 = _derivedClass.GetMember(Of FieldSymbol)("Field7")
                ValidateTupleNameAttribute(field7, expectedTupleNamesAttribute:=False)
                Assert.False(field7.Type.IsTupleType)

                Dim field8 = _derivedClass.GetMember(Of FieldSymbol)("Field8")
                ValidateTupleNameAttribute(field8, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", Nothing, Nothing})
                Dim field9 = _derivedClass.GetMember(Of FieldSymbol)("Field9")
                ValidateTupleNameAttribute(field9, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", Nothing, Nothing})
            End Sub

            Private Sub ValidateAttributesOnMethods()
                Dim method1 = _derivedClass.GetMember(Of MethodSymbol)("Method1")
                ValidateTupleNameAttribute(method1, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"}, forReturnType:=True)
                Dim method2 = _derivedClass.GetMember(Of MethodSymbol)("Method2")
                ValidateTupleNameAttribute(method2.Parameters.Single(), expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                Dim method3 = _derivedClass.GetMember(Of MethodSymbol)("Method3")
                ValidateTupleNameAttribute(method3, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"}, forReturnType:=True)
                ValidateTupleNameAttribute(method3.Parameters.Single(), expectedTupleNamesAttribute:=True, expectedElementNames:={"e3", "e4"})
                Dim method4 = _derivedClass.GetMember(Of MethodSymbol)("Method4")
                ValidateTupleNameAttribute(method4, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"}, forReturnType:=True)
                ValidateTupleNameAttribute(method4.Parameters.Single(), expectedTupleNamesAttribute:=True, expectedElementNames:={"e3", "e4"})
                Dim method5 = _derivedClass.GetMember(Of MethodSymbol)("Method5")
                ValidateTupleNameAttribute(method5, expectedTupleNamesAttribute:=False, forReturnType:=True)
                ValidateTupleNameAttribute(method5.Parameters.Single(), expectedTupleNamesAttribute:=False)
                Dim method6 = _derivedClass.GetMember(Of MethodSymbol)("Method6")
                ValidateTupleNameAttribute(method6, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", Nothing, Nothing}, forReturnType:=True)
            End Sub

            Private Sub ValidateAttributesOnProperties()
                Dim prop1 = _derivedClass.GetMember(Of PropertySymbol)("Prop1")
                ValidateTupleNameAttribute(prop1, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                Dim prop2 = _derivedClass.GetMember(Of PropertySymbol)("Prop2")
                ValidateTupleNameAttribute(prop2, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                Dim prop3 = _derivedClass.GetMember(Of PropertySymbol)("Prop3")
                ValidateTupleNameAttribute(prop3, expectedTupleNamesAttribute:=True, expectedElementNames:={"e1", "e2"})
                ValidateTupleNameAttribute(prop3.Parameters.Single(), expectedTupleNamesAttribute:=True, expectedElementNames:={"e3", "e4"})
            End Sub

            Private Sub ValidateTupleNameAttribute(
                symbol As Symbol,
                expectedTupleNamesAttribute As Boolean,
                Optional expectedElementNames As String() = Nothing,
                Optional forReturnType As Boolean = False)

                Dim tupleElementNamesAttr =
                    If(forReturnType, DirectCast(symbol, MethodSymbol).GetReturnTypeAttributes(), symbol.GetAttributes()).
                    Where(Function(attr) String.Equals(attr.AttributeClass.Name, "TupleElementNamesAttribute", StringComparison.Ordinal)).
                    AsImmutable()

                If Not expectedTupleNamesAttribute Then
                    Assert.Empty(tupleElementNamesAttr)
                    Assert.Null(expectedElementNames)
                Else
                    Dim tupleAttr = tupleElementNamesAttr.Single()
                    Assert.Equal("System.Runtime.CompilerServices.TupleElementNamesAttribute", tupleAttr.AttributeClass.ToTestDisplayString())
                    Assert.Equal("System.String()", tupleAttr.AttributeConstructor.Parameters.Single().Type.ToTestDisplayString())

                    If expectedElementNames Is Nothing Then
                        Assert.True(tupleAttr.CommonConstructorArguments.IsEmpty)
                    Else
                        Dim arg = tupleAttr.CommonConstructorArguments.Single()
                        Assert.Equal(TypedConstantKind.Array, arg.Kind)
                        Dim actualElementNames = arg.Values.SelectAsArray(AddressOf TypedConstantString)
                        AssertEx.Equal(expectedElementNames, actualElementNames)
                    End If
                End If
            End Sub

            Private Shared Function TypedConstantString(constant As TypedConstant) As String
                Assert.True(constant.Type.SpecialType = SpecialType.System_String)
                Return DirectCast(constant.Value, String)
            End Function
        End Structure

        <Fact>
        Public Sub TupleAttributeMissing()
            Dim comp0 = CreateCSharpCompilation(TestResources.NetFX.ValueTuple.tuplelib_cs)
            comp0.VerifyDiagnostics()
            Dim ref0 = comp0.EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40({s_tuplesTestSource}, options:=TestOptions.ReleaseDll, references:={SystemRuntimeFacadeRef, ref0})
            comp.AssertTheseDiagnostics(
                <expected>
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Inherits Base1(Of (key As Integer, val As Integer))
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        Inherits Base2(Of (key2 As Integer, val2 As Integer), V)
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            Inherits Base1(Of (key3 As Integer, val2 As Integer))
                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Inherits Outer(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer))).
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Inherits Outer(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer))).
                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            Outer(Of (e5 As Integer, e6 As Integer)).
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                Inner(Of (e7 As Integer, e8 As Integer)(), (e9 As Integer, e10 As Integer)).
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
                Inner(Of (e7 As Integer, e8 As Integer)(), (e9 As Integer, e10 As Integer)).
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            (e13 As (e11 As Integer, e12 As Integer), e14 As Integer)).
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            (e13 As (e11 As Integer, e12 As Integer), e14 As Integer)).
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            InnerInner(Of (e17 As (e15 As Integer, e16 As Integer), e22 As (e18 As Integer, e21 As Base1(Of (e19 As Integer, e20 As Integer)))))
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            InnerInner(Of (e17 As (e15 As Integer, e16 As Integer), e22 As (e18 As Integer, e21 As Base1(Of (e19 As Integer, e20 As Integer)))))
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            InnerInner(Of (e17 As (e15 As Integer, e16 As Integer), e22 As (e18 As Integer, e21 As Base1(Of (e19 As Integer, e20 As Integer)))))
                                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
            InnerInner(Of (e17 As (e15 As Integer, e16 As Integer), e22 As (e18 As Integer, e21 As Base1(Of (e19 As Integer, e20 As Integer)))))
                                                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field1 As (e1 As Integer, e2 As Integer)
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field2 As (e1 As Integer, e2 As Integer)
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field3 As Base1(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer)))
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field3 As Base1(Of (e1 As Integer, e4 As (e2 As Integer, e3 As Integer)))
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field4 As ValueTuple(Of Base1(Of (e1 As Integer, e2 As (Integer, (Object, Object)))), Integer)
                                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field5 As Outer(Of (e1 As Object, e2 As Object)).Inner(Of (e3 As Object, e4 As Object), ValueTuple(Of Object, Object))
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field5 As Outer(Of (e1 As Object, e2 As Object)).Inner(Of (e3 As Object, e4 As Object), ValueTuple(Of Object, Object))
                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field8 As (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer)
                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Field9 As Base1(Of (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer))
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method1() As (e1 As Integer, e2 As Integer)
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Sub Method2(x As (e1 As Integer, e2 As Integer))
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method3(x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method3(x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method4(ByRef x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method4(ByRef x As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Function Method6() As (e1 As Integer, e2 As Integer, e3 As Integer, e4 As Integer, e5 As Integer, e6 As Integer, e7 As Integer, e8 As Integer, e9 As Integer)
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared ReadOnly Property Prop1 As (e1 As Integer, e2 As Integer)
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Property Prop2 As (e1 As Integer, e2 As Integer)
                                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Default Property Prop3(param As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Default Property Prop3(param As (e3 As Integer, e4 As Integer)) As (e1 As Integer, e2 As Integer)
                                                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Delegate Sub Delegate1(Of V)(sender As Object, args As ValueTuple(Of V, (e4 As (e1 As Object, e2 As Object, e3 As Object), e5 As Object), (Object, Object)))
                                                                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Delegate Sub Delegate1(Of V)(sender As Object, args As ValueTuple(Of V, (e4 As (e1 As Object, e2 As Object, e3 As Object), e5 As Object), (Object, Object)))
                                                                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Custom Event Event1 As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object))))
                                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
    Public Shared Custom Event Event1 As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object))))
                                                                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        AddHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        AddHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
                                                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        RemoveHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
                                            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37268: Cannot define a class or member that utilizes tuples because the compiler required type 'System.Runtime.CompilerServices.TupleElementNamesAttribute' cannot be found. Are you missing a reference?
        RemoveHandler(value As Delegate1(Of (e1 As Object, e4 As ValueTuple(Of (e2 As Object, e3 As Object)))))
                                                                               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                </expected>)
        End Sub

        <Fact>
        Public Sub ExplicitTupleNamesAttribute()
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoTuples">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

<TupleElementNames({"a", "b"})>
Public Class C
    <TupleElementNames({Nothing, Nothing})>
    Public Field1 As ValueTuple(Of Integer, Integer)

    <TupleElementNames({"x", "y"})>
    Public ReadOnly Prop1 As ValueTuple(Of Integer, Integer)

    Public ReadOnly Property Prop2 As Integer
        <TupleElementNames({"x", "y"})>
        Get
            Return Nothing
        End Get
    End Property


    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
        Return (0, 0)
    End Function

    Public Delegate Sub Delegate1(Of T)(sender As Object, <TupleElementNames({"x"})> args As ValueTuple(Of T))

    <TupleElementNames({"y"})>
    Public Custom Event Event1 As Delegate1(Of ValueTuple(Of Integer))
        AddHandler(value As Delegate1(Of ValueTuple(Of Integer)))

        End AddHandler
        RemoveHandler(value As Delegate1(Of ValueTuple(Of Integer)))

        End RemoveHandler
        RaiseEvent(sender As Object, args As ValueTuple(Of ValueTuple(Of Integer)))

        End RaiseEvent
    End Event

    <TupleElementNames({"a", "b"})>
    Default Public ReadOnly Property Item1(<TupleElementNames> t As (a As Integer, b As Integer)) As (a As Integer, b As Integer)
        Get
            Return t
        End Get
    End Property
End Class

<TupleElementNames({"a", "b"})>
Public Structure S
End Structure

]]></file>
</compilation>, additionalRefs:=s_valueTupleRefs)

            comp.AssertTheseDiagnostics(
<errors>
    <![CDATA[
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
<TupleElementNames({"a", "b"})>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({Nothing, Nothing})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({"x", "y"})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31445: Attribute 'TupleElementNamesAttribute' cannot be applied to 'Get' of 'Prop2' because the attribute is not valid on this declaration type.
        <TupleElementNames({"x", "y"})>
         ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Function M(<TupleElementNames({Nothing})> x As ValueTuple) As <TupleElementNames({Nothing, Nothing})> ValueTuple(Of Integer, Integer)
                                                                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Public Delegate Sub Delegate1(Of T)(sender As Object, <TupleElementNames({"x"})> args As ValueTuple(Of T))
                                                           ~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'TupleElementNamesAttribute' cannot be applied to 'Event1' because the attribute is not valid on this declaration type.
    <TupleElementNames({"y"})>
     ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    <TupleElementNames({"a", "b"})>
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
    Default Public ReadOnly Property Item1(<TupleElementNames> t As (a As Integer, b As Integer)) As (a As Integer, b As Integer)
                                            ~~~~~~~~~~~~~~~~~
BC37269: Cannot reference 'System.Runtime.CompilerServices.TupleElementNamesAttribute' explicitly. Use the tuple syntax to define tuple names.
<TupleElementNames({"a", "b"})>
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    ]]>
</errors>)

        End Sub

        <Fact>
        <WorkItem(14844, "https://github.com/dotnet/roslyn/issues/14844")>
        Public Sub AttributesOnTypeConstraints()
            Dim src = <compilation>
                          <file src="a.vb">
                              <![CDATA[
Public Interface I1(Of T)
End Interface

Public Interface I2(Of T As I1(Of (a as Integer, b as Integer)))
End Interface
Public Interface I3(Of T As I1(Of (c as Integer, d as Integer)))
End Interface
]]>
                          </file>
                      </compilation>

            Dim validator =
            Sub(assembly As PEAssembly)
                Dim reader = assembly.ManifestModule.MetadataReader

                Dim verifyTupleConstraint =
                Sub(def As TypeDefinition, tupleNames As String())
                    Dim typeParams = def.GetGenericParameters()
                    Assert.Equal(1, typeParams.Count)
                    Dim typeParam = reader.GetGenericParameter(typeParams(0))
                    Dim constraintHandles = typeParam.GetConstraints()
                    Assert.Equal(1, constraintHandles.Count)
                    Dim constraint = reader.GetGenericParameterConstraint(constraintHandles(0))

                    Dim Attributes = constraint.GetCustomAttributes()
                    Assert.Equal(1, Attributes.Count)
                    Dim attr = reader.GetCustomAttribute(Attributes.Single())

                    ' Verify that the attribute contains an array of matching tuple names
                    Dim argsReader = reader.GetBlobReader(attr.Value)
                    ' Prolog
                    Assert.Equal(1, argsReader.ReadUInt16())
                    ' Array size
                    Assert.Equal(tupleNames.Length, argsReader.ReadInt32())

                    For Each name In tupleNames
                        Assert.Equal(name, argsReader.ReadSerializedString())
                    Next
                End Sub

                For Each typeHandle In reader.TypeDefinitions
                    Dim def = reader.GetTypeDefinition(typeHandle)
                    Dim name = reader.GetString(def.Name)
                    Select Case name
                        Case "I1`1"
                        Case "<Module>"
                            Continue For

                        Case "I2`1"
                            verifyTupleConstraint(def, {"a", "b"})
                            Exit For

                        Case "I3`1"
                            verifyTupleConstraint(def, {"c", "d"})
                            Exit For

                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(name)

                    End Select

                Next
            End Sub

            Dim symbolValidator =
                Sub(m As ModuleSymbol)
                    Dim verifyTupleImpls =
                    Sub(t As NamedTypeSymbol, tupleNames As String())
                        Dim typeParam = t.TypeParameters.Single()
                        Dim constraint = DirectCast(typeParam.ConstraintTypes.Single(), NamedTypeSymbol)
                        Dim typeArg = constraint.TypeArguments.Single()
                        Assert.True(typeArg.IsTupleType)
                        Assert.Equal(tupleNames, typeArg.TupleElementNames)
                    End Sub

                    For Each t In m.GlobalNamespace.GetTypeMembers()
                        Select Case t.Name
                            Case "I1"
                            Case "<Module>"
                                Continue For

                            Case "I2"
                                verifyTupleImpls(t, {"a", "b"})
                                Exit For

                            Case "I3"
                                verifyTupleImpls(t, {"c", "d"})
                                Exit For

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(t.Name)
                        End Select
                    Next
                End Sub

            CompileAndVerify(src,
                             references:={ValueTupleRef, SystemRuntimeFacadeRef},
                             validator:=validator,
                             symbolValidator:=symbolValidator)
        End Sub

        <Fact>
        <WorkItem(14844, "https://github.com/dotnet/roslyn/issues/14844")>
        Public Sub AttributesOnInterfaceImplementations()
            Dim src = <compilation>
                          <file name="a.vb">
                              <![CDATA[
Public Interface I1(Of T)
End Interface

Public Interface I2
    Inherits I1(Of (a as Integer, b as Integer))
End Interface
Public Interface I3
    Inherits I1(Of (c as Integer, d as Integer))
End Interface
]]>
                          </file>
                      </compilation>

            Dim validator =
            Sub(assembly As PEAssembly)
                Dim reader = assembly.ManifestModule.MetadataReader

                Dim verifyTupleImpls =
                Sub(def As TypeDefinition, tupleNames As String())
                    Dim interfaceImpls = def.GetInterfaceImplementations()
                    Assert.Equal(1, interfaceImpls.Count)
                    Dim interfaceImpl = reader.GetInterfaceImplementation(interfaceImpls.Single())

                    Dim attributes = interfaceImpl.GetCustomAttributes()
                    Assert.Equal(1, attributes.Count)
                    Dim attr = reader.GetCustomAttribute(attributes.Single())

                    ' Verify that the attribute contains an array of matching tuple names
                    Dim argsReader = reader.GetBlobReader(attr.Value)
                    ' Prolog
                    Assert.Equal(1, argsReader.ReadUInt16())
                    ' Array size
                    Assert.Equal(tupleNames.Length, argsReader.ReadInt32())

                    For Each name In tupleNames
                        Assert.Equal(name, argsReader.ReadSerializedString())
                    Next
                End Sub

                For Each typeHandle In reader.TypeDefinitions
                    Dim def = reader.GetTypeDefinition(typeHandle)
                    Dim name = reader.GetString(def.Name)

                    Select Case name
                        Case "I1`1"
                        Case "<Module>"
                            Continue For

                        Case "I2"
                            verifyTupleImpls(def, {"a", "b"})
                            Exit Select

                        Case "I3"
                            verifyTupleImpls(def, {"c", "d"})
                            Exit Select

                        Case Else
                            Throw TestExceptionUtilities.UnexpectedValue(name)

                    End Select
                Next
            End Sub

            Dim symbolValidator =
                Sub(m As ModuleSymbol)
                    Dim VerifyTupleImpls =
                    Sub(t As NamedTypeSymbol, tupleNames As String())
                        Dim interfaceImpl = t.Interfaces.Single()
                        Dim typeArg = interfaceImpl.TypeArguments.Single()
                        Assert.True(typeArg.IsTupleType)
                        Assert.Equal(tupleNames, typeArg.TupleElementNames)
                    End Sub

                    For Each t In m.GlobalNamespace.GetTypeMembers()
                        Select Case t.Name
                            Case "I1"
                            Case "<Module>"
                                Continue For

                            Case "I2"
                                VerifyTupleImpls(t, {"a", "b"})
                                Exit Select

                            Case "I3"
                                VerifyTupleImpls(t, {"c", "d"})
                                Exit Select

                            Case Else
                                Throw TestExceptionUtilities.UnexpectedValue(t.Name)
                        End Select
                    Next
                End Sub

            CompileAndVerify(src,
                references:={ValueTupleRef, SystemRuntimeFacadeRef},
                validator:=validator,
                symbolValidator:=symbolValidator)
        End Sub
    End Class
End Namespace
