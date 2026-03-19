' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols

    Friend Module InstantiatingGenericsExtensions

        ' Check generic instantiation invariants.
        <Extension()>
        Public Sub VerifyGenericInstantiationInvariants(instantiation As Symbol)
            If instantiation.IsDefinition Then
                Return
            End If

            Dim originalDefinition As Symbol = instantiation.OriginalDefinition
            Dim constructedFrom As Symbol
            Dim constructedFromConstructedFrom As Symbol
            Dim typeParameters As ImmutableArray(Of TypeParameterSymbol)
            Dim typeArguments As ImmutableArray(Of TypeSymbol)
            Dim constructedFromTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Dim constructedFromTypeArguments As ImmutableArray(Of TypeSymbol)
            Dim originalDefinitionTypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Dim type = TryCast(instantiation, NamedTypeSymbol)
            Dim method As MethodSymbol = Nothing

            If type IsNot Nothing Then
                typeParameters = type.TypeParameters
                typeArguments = type.TypeArguments
                constructedFrom = type.ConstructedFrom
                constructedFromTypeParameters = type.ConstructedFrom.TypeParameters
                constructedFromTypeArguments = type.ConstructedFrom.TypeArguments
                originalDefinitionTypeParameters = type.OriginalDefinition.TypeParameters
                constructedFromConstructedFrom = type.ConstructedFrom.ConstructedFrom
            Else
                method = DirectCast(instantiation, MethodSymbol)
                typeParameters = method.TypeParameters
                typeArguments = method.TypeArguments
                constructedFrom = method.ConstructedFrom
                constructedFromTypeParameters = method.ConstructedFrom.TypeParameters
                constructedFromTypeArguments = method.ConstructedFrom.TypeArguments
                originalDefinitionTypeParameters = method.OriginalDefinition.TypeParameters
                constructedFromConstructedFrom = method.ConstructedFrom.ConstructedFrom
            End If

            Assert.Equal(instantiation.DeclaringCompilation, originalDefinition.DeclaringCompilation)
            Assert.True(originalDefinition.IsDefinition)

            ' Check ConstructedFrom invariants.
            Assert.Same(originalDefinition, constructedFrom.OriginalDefinition)
            Assert.Same(constructedFrom, constructedFromConstructedFrom)
            Assert.Same(instantiation.ContainingSymbol, constructedFrom.ContainingSymbol)
            Assert.True(constructedFromTypeArguments.SequenceEqual(constructedFromTypeParameters, ReferenceEqualityComparer.Instance))
            Assert.Equal(constructedFrom.Name, originalDefinition.Name)

            Assert.Equal(constructedFrom.Kind, originalDefinition.Kind)
            Assert.Equal(constructedFrom.DeclaredAccessibility, originalDefinition.DeclaredAccessibility)
            Assert.Equal(constructedFrom.IsShared, originalDefinition.IsShared)

            For Each typeParam In constructedFromTypeParameters
                Assert.Same(constructedFrom, typeParam.ContainingSymbol)
            Next

            Dim constructedFromIsDefinition As Boolean = constructedFrom.IsDefinition

            For Each typeParam In constructedFromTypeParameters
                Assert.Equal(constructedFromIsDefinition, typeParam.IsDefinition)
                Assert.Same(originalDefinitionTypeParameters(typeParam.Ordinal), typeParam.OriginalDefinition)
            Next

            ' Check instantiation invariants.
            Assert.True(typeParameters.SequenceEqual(constructedFromTypeParameters, ReferenceEqualityComparer.Instance))
            Assert.True(instantiation Is constructedFrom OrElse Not typeArguments.SequenceEqual(typeParameters), String.Format("Constructed symbol {0} uses its own type parameters as type arguments", instantiation.ToTestDisplayString()))
            Assert.Equal(instantiation Is constructedFrom, typeArguments.SequenceEqual(typeParameters, ReferenceEqualityComparer.Instance))
            Assert.Equal(instantiation.Name, constructedFrom.Name)

            Assert.Equal(instantiation.Kind, originalDefinition.Kind)
            Assert.Equal(instantiation.DeclaredAccessibility, originalDefinition.DeclaredAccessibility)
            Assert.Equal(instantiation.IsShared, originalDefinition.IsShared)

            ' TODO: Check constraints and other TypeParameter's properties.

            If type IsNot Nothing Then
                Assert.Equal(type.ConstructedFrom.Arity, type.OriginalDefinition.Arity)
                Assert.Equal(type.Arity, type.ConstructedFrom.Arity)
                Assert.False(type.OriginalDefinition.IsUnboundGenericType)
                Assert.True(type.Arity > 0 OrElse type.ConstructedFrom Is type, String.Format("Condition [{0} > 0 OrElse {1} Is {2}] failed.", type.Arity, type.ConstructedFrom.ToTestDisplayString(), type.ToTestDisplayString()))

                Assert.True(type Is constructedFrom OrElse Not type.CanConstruct, String.Format("Condition [{0} Is constructedFrom OrElse Not {1}] failed.", type.ToTestDisplayString(), type.CanConstruct))
                Assert.True(type.Arity > 0 OrElse Not type.CanConstruct, String.Format("Condition [{0} > 0 OrElse Not {1}] failed.", type.Arity, type.CanConstruct))

                Assert.Equal(type.OriginalDefinition.IsAnonymousType, type.ConstructedFrom.IsAnonymousType)
                Assert.Equal(type.ConstructedFrom.IsAnonymousType, type.IsAnonymousType)

                Assert.Same(type.OriginalDefinition.EnumUnderlyingType, type.ConstructedFrom.EnumUnderlyingType)
                Assert.Same(type.ConstructedFrom.EnumUnderlyingType, type.EnumUnderlyingType)

                Assert.Equal(type.OriginalDefinition.TypeKind, type.ConstructedFrom.TypeKind)
                Assert.Equal(type.ConstructedFrom.TypeKind, type.TypeKind)

                Assert.Equal(type.OriginalDefinition.IsMustInherit, type.ConstructedFrom.IsMustInherit)
                Assert.Equal(type.ConstructedFrom.IsMustInherit, type.IsMustInherit)

                Assert.Equal(type.OriginalDefinition.IsNotInheritable, type.ConstructedFrom.IsNotInheritable)
                Assert.Equal(type.ConstructedFrom.IsNotInheritable, type.IsNotInheritable)

                Assert.False(type.OriginalDefinition.MightContainExtensionMethods)
                Assert.False(type.ConstructedFrom.MightContainExtensionMethods)
                Assert.False(type.MightContainExtensionMethods)

                ' Check UnboundGenericType invariants.
                Dim containingType As NamedTypeSymbol = type.ContainingType

                If containingType IsNot Nothing Then
                    containingType.VerifyGenericInstantiationInvariants()

                    If Not type.IsUnboundGenericType AndAlso containingType.IsUnboundGenericType Then
                        Assert.False(type.CanConstruct)
                        Assert.Null(type.BaseType)
                        Assert.Equal(0, type.Interfaces.Length)
                    End If
                End If

                If type.IsUnboundGenericType OrElse (containingType IsNot Nothing AndAlso containingType.IsUnboundGenericType) Then
                    Assert.Null(type.DefaultPropertyName)
                    Assert.Null(type.ConstructedFrom.DefaultPropertyName)
                Else
                    Assert.Equal(type.OriginalDefinition.DefaultPropertyName, type.ConstructedFrom.DefaultPropertyName)
                    Assert.Equal(type.ConstructedFrom.DefaultPropertyName, type.DefaultPropertyName)
                End If

                If type.IsUnboundGenericType Then
                    Assert.False(type.CanConstruct)
                    Assert.Null(type.BaseType)
                    Assert.Equal(0, type.Interfaces.Length)

                    If containingType IsNot Nothing Then
                        Assert.Equal(containingType.IsGenericType, containingType.IsUnboundGenericType)
                    End If

                    For Each typeArgument In typeArguments
                        Assert.Same(UnboundGenericType.UnboundTypeArgument, typeArgument)
                    Next

                ElseIf containingType IsNot Nothing AndAlso Not containingType.IsUnboundGenericType Then
                    containingType = containingType.ContainingType

                    While containingType IsNot Nothing
                        Assert.False(containingType.IsUnboundGenericType)
                        containingType = containingType.ContainingType
                    End While
                End If

                Dim testArgs() As TypeSymbol = GetTestArgs(type.Arity)

                If type.CanConstruct Then
                    Dim constructed = type.Construct(testArgs)
                    Assert.NotSame(type, constructed)
                    Assert.Same(type, constructedFrom)
                    constructed.VerifyGenericInstantiationInvariants()

                    Assert.Same(type, type.Construct(type.TypeParameters.As(Of TypeSymbol)()))
                Else
                    Assert.Throws(Of InvalidOperationException)(Sub() type.Construct(testArgs))
                    Assert.Throws(Of InvalidOperationException)(Sub() type.Construct(testArgs.AsImmutableOrNull()))
                End If
            Else
                Assert.True(method Is constructedFrom OrElse Not method.CanConstruct, String.Format("Condition [{0} Is constructedFrom OrElse Not {1}] failed.", method.ToTestDisplayString(), method.CanConstruct))
                Assert.True(method.Arity > 0 OrElse Not method.CanConstruct, String.Format("Condition [{0} > 0 OrElse Not {1}] failed.", method.Arity, method.CanConstruct))
                Assert.Equal(method.ConstructedFrom.Arity, method.OriginalDefinition.Arity)
                Assert.Equal(method.Arity, method.ConstructedFrom.Arity)
                Assert.Equal(method.Arity = 0, method.ConstructedFrom Is method)

                Assert.Same(method.OriginalDefinition.IsExtensionMethod, method.ConstructedFrom.IsExtensionMethod)
                Assert.Same(method.ConstructedFrom.IsExtensionMethod, method.IsExtensionMethod)

                Dim testArgs() As TypeSymbol = GetTestArgs(type.Arity)

                If method.CanConstruct Then
                    Dim constructed = method.Construct(testArgs)
                    Assert.NotSame(method, constructed)
                    Assert.Same(method, constructedFrom)
                    constructed.VerifyGenericInstantiationInvariants()

                    Assert.Throws(Of InvalidOperationException)(Sub() method.Construct(method.TypeParameters.As(Of TypeSymbol)()))
                Else
                    Assert.Throws(Of InvalidOperationException)(Sub() method.Construct(testArgs))
                    Assert.Throws(Of InvalidOperationException)(Sub() method.Construct(testArgs.AsImmutableOrNull()))
                End If
            End If
        End Sub

        Private Function GetTestArgs(arity As Integer) As TypeSymbol()
            Dim a(arity - 1) As TypeSymbol

            For i = 0 To a.Length - 1
                a(i) = ErrorTypeSymbol.UnknownResultType
            Next

            Return a
        End Function

    End Module

    Public Class InstantiatingGenerics
        Inherits BasicTestBase

        <Fact, WorkItem(910574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910574")>
        Public Sub Test1()

            Dim assembly = MetadataTestHelpers.LoadFromBytes(TestResources.General.MDTestLib1)
            Dim module0 = assembly.Modules(0)

            Dim C1 = module0.GlobalNamespace.GetTypeMembers("C1").Single()
            Dim C1_T = C1.TypeParameters(0)

            Assert.Equal("C1(Of C1_T)", C1.ToTestDisplayString())

            Dim C2 = C1.GetTypeMembers("C2").Single()
            Dim C2_T = C2.TypeParameters(0)

            Assert.Equal("C1(Of C1_T).C2(Of C2_T)", C2.ToTestDisplayString())

            Dim C3 = C1.GetTypeMembers("C3").Single()

            Assert.Equal("C1(Of C1_T).C3", C3.ToTestDisplayString())

            Dim C4 = C3.GetTypeMembers("C4").Single()
            Dim C4_T = C4.TypeParameters(0)

            Assert.Equal("C1(Of C1_T).C3.C4(Of C4_T)", C4.ToTestDisplayString())

            Dim TC2 = module0.GlobalNamespace.GetTypeMembers("TC2").Single()
            Dim TC2_T1 = TC2.TypeParameters(0)
            Dim TC2_T2 = TC2.TypeParameters(1)

            Assert.Equal("TC2(Of TC2_T1, TC2_T2)", TC2.ToTestDisplayString())

            Dim C107 = module0.GlobalNamespace.GetTypeMembers("C107").Single()
            Dim C108 = C107.GetTypeMembers("C108").Single()
            Dim C108_T = C108.TypeParameters(0)

            Assert.Equal("C107.C108(Of C108_T)", C108.ToTestDisplayString())

            Dim g1 = C1.Construct({TC2_T1})
            Assert.Equal("C1(Of TC2_T1)", g1.ToTestDisplayString())
            Assert.Equal(C1, g1.ConstructedFrom)

            Dim g1_C2 = g1.GetTypeMembers("C2").Single()
            Assert.Equal("C1(Of TC2_T1).C2(Of C2_T)", g1_C2.ToTestDisplayString())
            Assert.Equal(g1_C2, g1_C2.ConstructedFrom)
            Assert.NotEqual(C2.TypeParameters(0), g1_C2.TypeParameters(0))
            Assert.Same(C2.TypeParameters(0), g1_C2.TypeParameters(0).OriginalDefinition)
            Assert.Same(g1_C2.TypeParameters(0), g1_C2.TypeArguments(0))

            Dim g2 = g1_C2.Construct({TC2_T2})
            Assert.Equal("C1(Of TC2_T1).C2(Of TC2_T2)", g2.ToTestDisplayString())
            Assert.Equal(g1_C2, g2.ConstructedFrom)

            Dim g1_C3 = g1.GetTypeMembers("C3").Single()
            Assert.Equal("C1(Of TC2_T1).C3", g1_C3.ToTestDisplayString())
            Assert.Equal(g1_C3, g1_C3.ConstructedFrom)

            Dim g1_C3_C4 = g1_C3.GetTypeMembers("C4").Single()
            Assert.Equal("C1(Of TC2_T1).C3.C4(Of C4_T)", g1_C3_C4.ToTestDisplayString())
            Assert.Equal(g1_C3_C4, g1_C3_C4.ConstructedFrom)

            Dim g4 = g1_C3_C4.Construct({TC2_T2})
            Assert.Equal("C1(Of TC2_T1).C3.C4(Of TC2_T2)", g4.ToTestDisplayString())
            Assert.Equal(g1_C3_C4, g4.ConstructedFrom)

            Dim g108 = C108.Construct({TC2_T1})
            Assert.Equal("C107.C108(Of TC2_T1)", g108.ToTestDisplayString())
            Assert.Equal(C108, g108.ConstructedFrom)

            Dim g_TC2 = TC2.Construct({C107, C108})
            Assert.Equal("TC2(Of C107, C107.C108(Of C108_T))", g_TC2.ToTestDisplayString())
            Assert.Equal(TC2, g_TC2.ConstructedFrom)

            Assert.Equal(TC2, TC2.Construct({TC2_T1, TC2_T2}))

            Assert.Null(TypeSubstitution.Create(TC2, {TC2_T1, TC2_T2}, {TC2_T1, TC2_T2}))

            Dim s1 = TypeSubstitution.Create(C1, {C1_T}, {TC2_T1})
            Dim g1_1 = DirectCast(C1.Construct(s1), NamedTypeSymbol)
            Assert.Equal("C1(Of TC2_T1)", g1_1.ToTestDisplayString())
            Assert.Equal(C1, g1_1.ConstructedFrom)
            Assert.Equal(g1, g1_1)

            Dim s2 = TypeSubstitution.Create(C2, {C1_T, C2_T}, {TC2_T1, TC2_T2})
            Dim g2_1 = DirectCast(C2.Construct(s2), NamedTypeSymbol)
            Assert.Equal("C1(Of TC2_T1).C2(Of TC2_T2)", g2_1.ToTestDisplayString())
            Assert.Equal(g1_C2, g2_1.ConstructedFrom)
            Assert.Equal(g2, g2_1)

            Dim s2_1 = TypeSubstitution.Create(C2, {C2_T}, {TC2_T2})

            Dim s3 = TypeSubstitution.Concat(s2_1.TargetGenericDefinition, s1, s2_1)
            Dim g2_2 = DirectCast(C2.Construct(s3), NamedTypeSymbol)
            Assert.Equal("C1(Of TC2_T1).C2(Of TC2_T2)", g2_2.ToTestDisplayString())
            Assert.Equal(g1_C2, g2_2.ConstructedFrom)
            Assert.Equal(g2, g2_2)

            Dim g2_3 = DirectCast(C2.Construct(s2_1), NamedTypeSymbol)
            Assert.Equal("C1(Of C1_T).C2(Of TC2_T2)", g2_3.ToTestDisplayString())
            Assert.Equal(C2, g2_3.ConstructedFrom)

            Dim s4 = TypeSubstitution.Create(C4, {C1_T, C4_T}, {TC2_T1, TC2_T2})

            Dim g4_1 = DirectCast(C4.Construct(s4), NamedTypeSymbol)
            Assert.Equal("C1(Of TC2_T1).C3.C4(Of TC2_T2)", g4_1.ToTestDisplayString())
            Assert.Equal(g1_C3_C4, g4_1.ConstructedFrom)
            Assert.Equal(g4, g4_1)

            Dim s108 = TypeSubstitution.Create(C108, {C108_T}, {TC2_T1})
            Dim g108_1 = DirectCast(C108.Construct(s108), NamedTypeSymbol)
            Assert.Equal("C107.C108(Of TC2_T1)", g108_1.ToTestDisplayString())
            Assert.Equal(C108, g108_1.ConstructedFrom)
            Assert.Equal(g108, g108_1)

            Dim sTC2 = TypeSubstitution.Create(TC2, {TC2_T1, TC2_T2}, {C107, C108})
            Dim g_TC2_1 = DirectCast(TC2.Construct(sTC2), NamedTypeSymbol)
            Assert.Equal("TC2(Of C107, C107.C108(Of C108_T))", g_TC2_1.ToTestDisplayString())
            Assert.Equal(TC2, g_TC2_1.ConstructedFrom)
            Assert.Equal(g_TC2, g_TC2_1)

            g1.VerifyGenericInstantiationInvariants()
            g2.VerifyGenericInstantiationInvariants()
            g4.VerifyGenericInstantiationInvariants()
            g108.VerifyGenericInstantiationInvariants()
            g_TC2.VerifyGenericInstantiationInvariants()
            g1_1.VerifyGenericInstantiationInvariants()
            g2_2.VerifyGenericInstantiationInvariants()
            g_TC2_1.VerifyGenericInstantiationInvariants()
        End Sub

        <Fact>
        Public Sub AlphaRename()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file name="a.vb">
        
Module Module1
    Sub Main()
        Dim x1 As New C1(Of Byte, Byte)()
        Dim x2 As New C1(Of Byte, Byte).C2(Of Byte, Byte)()
        Dim x3 As New C1(Of Byte, Byte).C2(Of Byte, Byte).C3(Of Byte, Byte)()
        Dim x4 As New C1(Of Byte, Byte).C2(Of Byte, Byte).C3(Of Byte, Byte).C4(Of Byte)()
        Dim x5 As New C1(Of Byte, Byte).C5()
    End Sub
End Module

Class C1(Of C1T1, C1T2)

    Class C2(Of C2T1, C2T2)

        Class C3(Of C3T1, C3T2 As C1T1)

            Function F1() As C1T1
                Return Nothing
            End Function

            Function F2() As C2T1
                Return Nothing
            End Function

            Function F3() As C3T1
                Return Nothing
            End Function

            Function F4() As C1T2
                Return Nothing
            End Function

            Function F5() As C2T2
                Return Nothing
            End Function

            Function F6() As C3T2
                Return Nothing
            End Function

            ' error BC32044: Type argument 'C3T2' does not inherit from or implement the constraint type 'Integer'.
            Dim x As C1(Of Integer, Integer).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2)

            Class C4(Of C4T1)
            End Class

        End Class

        Public V1 As C1(Of Integer, C2T2).C5
        Public V2 As C1(Of C2T1, C2T2).C5
        Public V3 As C1(Of Integer, Integer).C5

        Public V4 As C2(Of Byte, Byte)

        Public V5 As C1(Of C1T2, C1T1).C2(Of C2T1, C2T2)
        Public V6 As C1(Of C1T2, C1T1).C2(Of C2T2, C2T1)
        Public V7 As C1(Of C1T2, C1T1).C2(Of Byte, Integer)
        Public V8 As C2(Of C2T1, C2T2)
        Public V9 As C2(Of Byte, C2T2)

        Sub Test12(x As C2(Of Integer, Integer))
            Dim y As C1(Of C1T1, C1T2).C2(Of Byte, Integer) = x.V9
        End Sub

        Sub Test11(x As C1(Of Integer, Integer).C2(Of Byte, Byte))
            Dim y As C1(Of Integer, Integer).C2(Of Byte, Byte) = x.V8
        End Sub

        Sub Test6(x As C1(Of C1T2, C1T1).C2(Of C2T1, C2T2))
            Dim y As C1(Of C1T1, C1T2).C2(Of C2T1, C2T2) = x.V5
        End Sub

        Sub Test7(x As C1(Of C1T2, C1T1).C2(Of C2T2, C2T1))
            Dim y As C1(Of C1T1, C1T2).C2(Of C2T1, C2T2) = x.V6
        End Sub

        Sub Test8(x As C1(Of C1T2, C1T1).C2(Of C2T2, C2T1))
            Dim y As C1(Of C1T1, C1T2).C2(Of Byte, Integer) = x.V7
        End Sub

        Sub Test9(x As C1(Of Integer, Byte).C2(Of C2T2, C2T1))
            Dim y As C1(Of Byte, Integer).C2(Of Byte, Integer) = x.V7
        End Sub

        Sub Test10(x As C1(Of C1T1, C1T2).C2(Of C2T2, C2T1))
            Dim y As C1(Of C1T2, C1T1).C2(Of Byte, Integer) = x.V7
        End Sub

    End Class

    Class C5
    End Class

    Sub Test1(x As C2(Of C1T1, Integer))
        Dim y As C1(Of Integer, Integer).C5 = x.V1
    End Sub

    Sub Test2(x As C2(Of C1T1, C1T2))
        Dim y As C5 = x.V2
    End Sub

    Sub Test3(x As C2(Of C1T2, C1T1))
        Dim y As C1(Of Integer, Integer).C5 = x.V3
    End Sub

    Sub Test4(x As C1(Of Integer, Integer).C2(Of C1T1, C1T2))
        Dim y As C1(Of Integer, Integer).C2(Of Byte, Byte) = x.V4
    End Sub

End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim int = compilation.GetSpecialType(SpecialType.System_Int32)

            Assert.Throws(Of InvalidOperationException)(Function() int.Construct())

            Dim c1 = compilation.GetTypeByMetadataName("C1`2")
            Dim c2 = c1.GetTypeMembers("C2").Single()
            Dim c3 = c2.GetTypeMembers("C3").Single()
            Dim c4 = c3.GetTypeMembers("C4").Single()
            Dim c5 = c1.GetTypeMembers("C5").Single()

            Dim c3OfIntInt = c3.Construct(int, int)
            Dim c2_c3OfIntInt = c3OfIntInt.ContainingType
            Dim c1_c2_c3OfIntInt = c2_c3OfIntInt.ContainingType

            Assert.Equal("C1(Of C1T1, C1T2).C2(Of C2T1, C2T2).C3(Of System.Int32, System.Int32)", c3OfIntInt.ToTestDisplayString())
            Assert.Same(c1, c1_c2_c3OfIntInt)
            Assert.Same(c2, c2_c3OfIntInt)
            Assert.Same(c3.TypeParameters(0), c3OfIntInt.TypeParameters(0))
            Assert.Same(c3, c3OfIntInt.ConstructedFrom)

            Dim substitution As TypeSubstitution

            substitution = TypeSubstitution.Create(c1, {c1.TypeParameters(0), c1.TypeParameters(1)}, {int, int})
            Dim c1OfIntInt_c2_c3 = DirectCast(c3.Construct(substitution), NamedTypeSymbol)
            Dim c1OfIntInt_c2 = c1OfIntInt_c2_c3.ContainingType
            Dim c1OfIntInt = c1OfIntInt_c2.ContainingType

            Assert.Equal("C1(Of System.Int32, System.Int32).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2)", c1OfIntInt_c2_c3.ToTestDisplayString())
            Assert.Equal("C1(Of System.Int32, System.Int32).C2(Of C2T1, C2T2)", c1OfIntInt_c2.ToTestDisplayString())
            Assert.Equal("C1(Of System.Int32, System.Int32)", c1OfIntInt.ToTestDisplayString())

            Assert.Same(c1.TypeParameters(0), c1OfIntInt.TypeParameters(0))
            Assert.Same(int, c1OfIntInt.TypeArguments(0))
            Assert.NotSame(c2.TypeParameters(0), c1OfIntInt_c2.TypeParameters(0))
            Assert.Same(c1OfIntInt_c2.TypeParameters(0), c1OfIntInt_c2.TypeArguments(0))
            Assert.Same(c1OfIntInt_c2, c1OfIntInt_c2.TypeParameters(0).ContainingSymbol)
            Assert.NotSame(c3.TypeParameters(0), c1OfIntInt_c2_c3.TypeParameters(0))
            Assert.Same(c1OfIntInt_c2_c3.TypeParameters(0), c1OfIntInt_c2_c3.TypeArguments(0))
            Assert.Same(c1OfIntInt_c2_c3, c1OfIntInt_c2_c3.TypeParameters(0).ContainingSymbol)

            Dim c1OfIntInt_c2_c3_F1 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F1").Single(), MethodSymbol)
            Dim c1OfIntInt_c2_c3_F2 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F2").Single(), MethodSymbol)
            Dim c1OfIntInt_c2_c3_F3 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F3").Single(), MethodSymbol)
            Dim c1OfIntInt_c2_c3_F4 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F4").Single(), MethodSymbol)
            Dim c1OfIntInt_c2_c3_F5 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F5").Single(), MethodSymbol)
            Dim c1OfIntInt_c2_c3_F6 = DirectCast(c1OfIntInt_c2_c3.GetMembers("F6").Single(), MethodSymbol)

            Assert.Same(c1OfIntInt.TypeArguments(0), c1OfIntInt_c2_c3_F1.ReturnType)
            Assert.Same(c1OfIntInt_c2.TypeArguments(0), c1OfIntInt_c2_c3_F2.ReturnType)
            Assert.Same(c1OfIntInt_c2_c3.TypeArguments(0), c1OfIntInt_c2_c3_F3.ReturnType)
            Assert.Same(c1OfIntInt.TypeArguments(1), c1OfIntInt_c2_c3_F4.ReturnType)
            Assert.Same(c1OfIntInt_c2.TypeArguments(1), c1OfIntInt_c2_c3_F5.ReturnType)
            Assert.Same(c1OfIntInt_c2_c3.TypeArguments(1), c1OfIntInt_c2_c3_F6.ReturnType)

            substitution = TypeSubstitution.Create(c3, {c1.TypeParameters(0), c1.TypeParameters(1)}, {int, int})
            Dim c1OfIntInt_c2Of_c3Of = DirectCast(c3.Construct(substitution), NamedTypeSymbol)

            ' We need to distinguish these two things in order to be able to detect constraint violation.
            ' error BC32044: Type argument 'C3T2' does not inherit from or implement the constraint type 'Integer'.
            Assert.NotEqual(c1OfIntInt_c2Of_c3Of.ConstructedFrom, c1OfIntInt_c2Of_c3Of)

            Dim c1OfIntInt_c2Of = c1OfIntInt_c2Of_c3Of.ContainingType

            Assert.Equal(c1OfIntInt, c1OfIntInt_c2Of.ContainingType)
            c1OfIntInt = c1OfIntInt_c2Of.ContainingType

            Assert.Equal("C1(Of System.Int32, System.Int32).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2)", c1OfIntInt_c2Of_c3Of.ToTestDisplayString())
            Assert.Equal("C1(Of System.Int32, System.Int32).C2(Of C2T1, C2T2)", c1OfIntInt_c2Of.ToTestDisplayString())
            Assert.Equal("C1(Of System.Int32, System.Int32)", c1OfIntInt.ToTestDisplayString())

            Assert.Same(c1.TypeParameters(0), c1OfIntInt.TypeParameters(0))
            Assert.Same(int, c1OfIntInt.TypeArguments(0))
            Assert.NotSame(c2.TypeParameters(0), c1OfIntInt_c2Of.TypeParameters(0))
            Assert.NotSame(c1OfIntInt_c2Of.TypeParameters(0), c1OfIntInt_c2Of.TypeArguments(0))
            Assert.Same(c1OfIntInt_c2Of.TypeParameters(0).OriginalDefinition, c1OfIntInt_c2Of.TypeArguments(0))
            Assert.NotSame(c1OfIntInt_c2Of, c1OfIntInt_c2Of.TypeParameters(0).ContainingSymbol)
            Assert.Same(c1OfIntInt_c2Of.ConstructedFrom, c1OfIntInt_c2Of.TypeParameters(0).ContainingSymbol)
            Assert.NotSame(c3.TypeParameters(0), c1OfIntInt_c2Of_c3Of.TypeParameters(0))
            Assert.NotSame(c1OfIntInt_c2Of_c3Of.TypeParameters(0), c1OfIntInt_c2Of_c3Of.TypeArguments(0))
            Assert.Same(c1OfIntInt_c2Of_c3Of.TypeParameters(0).OriginalDefinition, c1OfIntInt_c2Of_c3Of.TypeArguments(0))
            Assert.NotSame(c1OfIntInt_c2Of_c3Of, c1OfIntInt_c2Of_c3Of.TypeParameters(0).ContainingSymbol)
            Assert.Same(c1OfIntInt_c2Of_c3Of.ConstructedFrom, c1OfIntInt_c2Of_c3Of.TypeParameters(0).ContainingSymbol)

            Dim c1OfIntInt_c2Of_c3Of_F1 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F1").Single(), MethodSymbol)
            Dim c1OfIntInt_c2Of_c3Of_F2 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F2").Single(), MethodSymbol)
            Dim c1OfIntInt_c2Of_c3Of_F3 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F3").Single(), MethodSymbol)
            Dim c1OfIntInt_c2Of_c3Of_F4 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F4").Single(), MethodSymbol)
            Dim c1OfIntInt_c2Of_c3Of_F5 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F5").Single(), MethodSymbol)
            Dim c1OfIntInt_c2Of_c3Of_F6 = DirectCast(c1OfIntInt_c2Of_c3Of.GetMembers("F6").Single(), MethodSymbol)

            Assert.Same(c1OfIntInt.TypeArguments(0), c1OfIntInt_c2Of_c3Of_F1.ReturnType)
            Assert.Same(c1OfIntInt_c2Of.TypeArguments(0), c1OfIntInt_c2Of_c3Of_F2.ReturnType)
            Assert.Same(c1OfIntInt_c2Of_c3Of.TypeArguments(0), c1OfIntInt_c2Of_c3Of_F3.ReturnType)
            Assert.Same(c1OfIntInt.TypeArguments(1), c1OfIntInt_c2Of_c3Of_F4.ReturnType)
            Assert.Same(c1OfIntInt_c2Of.TypeArguments(1), c1OfIntInt_c2Of_c3Of_F5.ReturnType)
            Assert.Same(c1OfIntInt_c2Of_c3Of.TypeArguments(1), c1OfIntInt_c2Of_c3Of_F6.ReturnType)

            substitution = TypeSubstitution.Create(c2,
                                                   {c1.TypeParameters(0), c1.TypeParameters(1), c2.TypeParameters(0), c2.TypeParameters(1)},
                                                   {int, int, c2.TypeParameters(0), c2.TypeParameters(1)})
            Dim c1OfIntInt_c2Of_c3 = c3.Construct(substitution)

            Assert.NotEqual(c1OfIntInt_c2_c3, c1OfIntInt_c2Of_c3)

            Dim c1OfIntInt_c2Of_c3OfInt = c1OfIntInt_c2Of_c3.Construct(int, c3.TypeParameters(1))

            Assert.Equal("C1(Of System.Int32, System.Int32).C2(Of C2T1, C2T2).C3(Of System.Int32, C3T2)", c1OfIntInt_c2Of_c3OfInt.ToTestDisplayString())
            Assert.True(c1OfIntInt_c2Of_c3OfInt.TypeArguments(1).IsDefinition)
            Assert.False(c1OfIntInt_c2Of_c3OfInt.TypeParameters(1).IsDefinition)
            Assert.NotEqual(c1OfIntInt_c2_c3, c1OfIntInt_c2Of_c3OfInt.ConstructedFrom)
            Assert.Same(c1OfIntInt_c2Of_c3, c1OfIntInt_c2Of_c3OfInt.ConstructedFrom)
            Assert.NotEqual(c1OfIntInt_c2Of_c3.TypeParameters(1), c1OfIntInt_c2Of_c3OfInt.TypeArguments(1))
            Assert.Same(c1OfIntInt_c2Of_c3.TypeParameters(1).OriginalDefinition, c1OfIntInt_c2Of_c3OfInt.TypeArguments(1))

            Assert.Same(c3.TypeParameters(1), c1OfIntInt_c2Of_c3.TypeParameters(1).OriginalDefinition)
            Assert.Same(c3.TypeParameters(1).Name, c1OfIntInt_c2Of_c3.TypeParameters(1).Name)
            Assert.Equal(c3.TypeParameters(1).HasConstructorConstraint, c1OfIntInt_c2Of_c3.TypeParameters(1).HasConstructorConstraint)
            Assert.Equal(c3.TypeParameters(1).HasReferenceTypeConstraint, c1OfIntInt_c2Of_c3.TypeParameters(1).HasReferenceTypeConstraint)
            Assert.Equal(c3.TypeParameters(1).HasValueTypeConstraint, c1OfIntInt_c2Of_c3.TypeParameters(1).HasValueTypeConstraint)
            Assert.Equal(c3.TypeParameters(1).Ordinal, c1OfIntInt_c2Of_c3.TypeParameters(1).Ordinal)

            Assert.Throws(Of InvalidOperationException)(Sub() c1OfIntInt_c2_c3.Construct(c3.TypeParameters(0), c3.TypeParameters(1)))

            Dim c1OfIntInt_c2Of_c3Constructed = c1OfIntInt_c2Of_c3.Construct(c3.TypeParameters(0), c3.TypeParameters(1))

            Assert.Same(c1OfIntInt_c2Of_c3, c1OfIntInt_c2Of_c3Constructed.ConstructedFrom)
            Assert.False(c1OfIntInt_c2Of_c3Constructed.CanConstruct)
            Assert.False(c1OfIntInt_c2Of_c3Constructed.ContainingType.CanConstruct)
            Assert.NotEqual(c1OfIntInt_c2Of_c3Constructed.ContainingType, c1OfIntInt_c2Of_c3Constructed.ContainingType.ConstructedFrom)

            ' We need to distinguish these two things in order to be able to detect constraint violation.
            ' error BC32044: Type argument 'C3T2' does not inherit from or implement the constraint type 'Integer'.
            Assert.NotEqual(c1OfIntInt_c2Of_c3, c1OfIntInt_c2Of_c3Constructed)

            Assert.Same(c3, c3.Construct(c3.TypeParameters(0), c3.TypeParameters(1)))

            substitution = TypeSubstitution.Create(c1, {c1.TypeParameters(0), c1.TypeParameters(1)}, {int, int})
            Dim c1OfIntInt_C5_1 = c5.Construct(substitution)

            substitution = TypeSubstitution.Create(c5, {c1.TypeParameters(0), c1.TypeParameters(1)}, {int, int})
            Dim c1OfIntInt_C5_2 = c5.Construct(substitution)

            Assert.Equal(c1OfIntInt_C5_1, c1OfIntInt_C5_2)
            Assert.Equal(0, c1OfIntInt_C5_1.TypeParameters.Length)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC32044: Type argument 'C3T2' does not inherit from or implement the constraint type 'Integer'.
            Dim x As C1(Of Integer, Integer).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2)
                ~
</errors>)

            Assert.Throws(Of InvalidOperationException)(Sub() c5.Construct(c1))

            c3OfIntInt.VerifyGenericInstantiationInvariants()
            c1OfIntInt_c2_c3.VerifyGenericInstantiationInvariants()
            c1OfIntInt_c2Of_c3Of.VerifyGenericInstantiationInvariants()
            c1OfIntInt_c2Of_c3.VerifyGenericInstantiationInvariants()
            c1OfIntInt_c2Of_c3OfInt.VerifyGenericInstantiationInvariants()
            c1OfIntInt_c2Of_c3Constructed.VerifyGenericInstantiationInvariants()
            c1OfIntInt_C5_1.VerifyGenericInstantiationInvariants()
            c1OfIntInt_C5_2.VerifyGenericInstantiationInvariants()

            c1OfIntInt_c2.VerifyGenericInstantiationInvariants()

            Dim c1OfIntInt_c2_1 = c1OfIntInt.GetTypeMembers("c2").Single()

            Assert.Equal(c1OfIntInt_c2, c1OfIntInt_c2_1)
            Assert.NotSame(c1OfIntInt_c2, c1OfIntInt_c2_1) ' Checks below need equal, but not identical symbols to test target scenarios!

            Assert.Same(c1OfIntInt_c2, c1OfIntInt_c2.Construct(New List(Of TypeSymbol) From {c1OfIntInt_c2.TypeParameters(0), c1OfIntInt_c2.TypeParameters(1)}))

            Assert.Same(c1OfIntInt_c2, c1OfIntInt_c2.Construct(c1OfIntInt_c2_1.TypeParameters(0), c1OfIntInt_c2_1.TypeParameters(1)))
            Dim alphaConstructedC2 = c1OfIntInt_c2.Construct(c1OfIntInt_c2_1.TypeParameters(1), c1OfIntInt_c2_1.TypeParameters(0))

            Assert.Same(c1OfIntInt_c2, alphaConstructedC2.ConstructedFrom)
            Assert.Same(alphaConstructedC2.TypeArguments(0), c1OfIntInt_c2.TypeParameters(1))
            Assert.NotSame(alphaConstructedC2.TypeArguments(0), c1OfIntInt_c2_1.TypeParameters(1))
            Assert.Same(alphaConstructedC2.TypeArguments(1), c1OfIntInt_c2.TypeParameters(0))
            Assert.NotSame(alphaConstructedC2.TypeArguments(1), c1OfIntInt_c2_1.TypeParameters(0))

            alphaConstructedC2 = c1OfIntInt_c2.Construct(c1OfIntInt_c2_1.TypeParameters(0), c1OfIntInt)

            Assert.Same(c1OfIntInt_c2, alphaConstructedC2.ConstructedFrom)
            Assert.Same(alphaConstructedC2.TypeArguments(0), c1OfIntInt_c2.TypeParameters(0))
            Assert.NotSame(alphaConstructedC2.TypeArguments(0), c1OfIntInt_c2_1.TypeParameters(0))
            Assert.Same(alphaConstructedC2.TypeArguments(1), c1OfIntInt)

            alphaConstructedC2 = c1OfIntInt_c2.Construct(c1OfIntInt, c1OfIntInt_c2_1.TypeParameters(1))

            Assert.Same(c1OfIntInt_c2, alphaConstructedC2.ConstructedFrom)
            Assert.Same(alphaConstructedC2.TypeArguments(0), c1OfIntInt)
            Assert.Same(alphaConstructedC2.TypeArguments(1), c1OfIntInt_c2.TypeParameters(1))
            Assert.NotSame(alphaConstructedC2.TypeArguments(1), c1OfIntInt_c2_1.TypeParameters(1))
        End Sub

        <Fact>
        Public Sub TypeSubstitutionTypeTest()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Class C1(Of C1T1, C1T2)

    Class C2(Of C2T1, C2T2)

        Class C3(Of C3T1, C3T2 As C1T1)

            Class C4(Of C4T1)
            End Class

        End Class

    End Class

    Class C5
    End Class

End Class
    </file>
</compilation>)

            Dim int = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim bte = compilation.GetSpecialType(SpecialType.System_Byte)
            Dim chr = compilation.GetSpecialType(SpecialType.System_Char)
            Dim c1 = compilation.GetTypeByMetadataName("C1`2")
            Dim c2 = c1.GetTypeMembers("C2").Single()
            Dim c3 = c2.GetTypeMembers("C3").Single()
            Dim c4 = c3.GetTypeMembers("C4").Single()
            Dim c5 = c1.GetTypeMembers("C5").Single()

            Dim substitution1 As TypeSubstitution
            Dim substitution2 As TypeSubstitution
            Dim substitution3 As TypeSubstitution

            substitution1 = TypeSubstitution.Create(c1, {c1.TypeParameters(0), c1.TypeParameters(1)}, {int, int})
            Assert.Equal("C1(Of C1T1, C1T2) : {C1T1->Integer, C1T2->Integer}", substitution1.ToString())

            substitution2 = TypeSubstitution.Create(c4, {c3.TypeParameters(0), c4.TypeParameters(0)}, {bte, chr})
            Assert.Equal("C1(Of C1T1, C1T2).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2).C4(Of C4T1) : {C3T1->Byte}, {C4T1->Char}", substitution2.ToString())

            Assert.Same(substitution1, TypeSubstitution.Concat(c1, Nothing, substitution1))
            Assert.Same(substitution1, TypeSubstitution.Concat(c1, substitution1, Nothing))
            Assert.Null(TypeSubstitution.Concat(c1, Nothing, Nothing))

            substitution3 = TypeSubstitution.Concat(c2, substitution1, Nothing)
            Assert.Equal("C1(Of C1T1, C1T2).C2(Of C2T1, C2T2) : {C1T1->Integer, C1T2->Integer}, {}", substitution3.ToString())

            substitution3 = TypeSubstitution.Concat(c4, substitution1, substitution2)
            Assert.Equal("C1(Of C1T1, C1T2).C2(Of C2T1, C2T2).C3(Of C3T1, C3T2).C4(Of C4T1) : {C1T1->Integer, C1T2->Integer}, {}, {C3T1->Byte}, {C4T1->Char}", substitution3.ToString())

            Assert.Null(TypeSubstitution.Create(c4, {c1.TypeParameters(0)}, {c1.TypeParameters(0)}))
        End Sub

        <Fact>
        Public Sub ConstructionWithAlphaRenaming()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="C">
    <file name="a.vb">
Module M
    Public G As C1(Of Integer)
End Module

Class C1(Of T)
    Class C2(Of U)
        Public F As U()
    End Class
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim moduleM = DirectCast(globalNS.GetMembers("M").First(), NamedTypeSymbol)
            Dim fieldG = DirectCast(moduleM.GetMembers("G").First(), FieldSymbol)
            Dim typeC1OfInteger = fieldG.Type

            Dim typeC2 = DirectCast(typeC1OfInteger.GetMembers("C2").First(), NamedTypeSymbol)
            Dim fieldF = DirectCast(typeC2.GetMembers("F").First(), FieldSymbol)
            Dim fieldFType = fieldF.Type
            Assert.Equal("U()", fieldF.Type.ToTestDisplayString())
        End Sub
    End Class

End Namespace
