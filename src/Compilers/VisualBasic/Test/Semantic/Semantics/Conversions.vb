' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics.ConversionsTests.Parameters
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ConversionsTests
        Inherits BasicTestBase

        Private Const s_noConversion As ConversionKind = Nothing

        <Fact()>
        Public Sub TryCastDirectCastConversions()

            Dim dummyCode =
<file>
Class C1
    Shared Sub MethodDecl()
    End Sub
End Class
</file>
            Dim dummyTree = VisualBasicSyntaxTree.ParseText(dummyCode.Value)

            ' Tests are based on the source code used to compile VBConversions.dll, VBConversions.vb is
            ' checked in next to the DLL.

            Dim vbConversionsRef = TestReferences.SymbolsTests.VBConversions
            Dim modifiersRef = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll

            Dim c1 = VisualBasicCompilation.Create("Test", syntaxTrees:={dummyTree}, references:={TestReferences.NetFx.v4_0_21006.mscorlib, vbConversionsRef, modifiersRef})

            Dim sourceModule = DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol)
            Dim methodDeclSymbol = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("MethodDecl").Single(), SourceMethodSymbol)
            Dim methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, dummyTree, methodDeclSymbol)

            Dim asmVBConversions = c1.GetReferencedAssemblySymbol(vbConversionsRef)
            Dim asmModifiers = c1.GetReferencedAssemblySymbol(modifiersRef)

            Dim test = asmVBConversions.Modules(0).GlobalNamespace.GetTypeMembers("Test").Single()

            Dim m13 = DirectCast(test.GetMembers("M13").Single(), MethodSymbol)
            Dim m13p = m13.Parameters.Select(Function(p) p.Type).ToArray()


            Assert.Equal(ConversionKind.WideningReference, ClassifyDirectCastAssignment(m13p(a), m13p(b), methodBodyBinder)) ' Object)
            Assert.Equal(ConversionKind.WideningValue, ClassifyDirectCastAssignment(m13p(a), m13p(c), methodBodyBinder)) ' Object)
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyDirectCastAssignment(m13p(b), m13p(a), methodBodyBinder)) ' ValueType)
            Assert.Equal(ConversionKind.WideningValue, ClassifyDirectCastAssignment(m13p(b), m13p(c), methodBodyBinder)) ' ValueType)
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyDirectCastAssignment(m13p(c), m13p(a), methodBodyBinder)) ' Integer)
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyDirectCastAssignment(m13p(c), m13p(b), methodBodyBinder)) ' Integer)
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(c), m13p(c), methodBodyBinder))) ' Integer)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(c), m13p(d), methodBodyBinder)) ' Integer) 'error BC30311: Value of type 'Long' cannot be converted to 'Integer'.
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyDirectCastAssignment(m13p(c), m13p(e), methodBodyBinder)) ' Integer)
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(d), m13p(d), methodBodyBinder))) ' Long)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(d), m13p(c), methodBodyBinder)) ' Long) 'error BC30311: Value of type 'Integer' cannot be converted to 'Long'.
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(e), m13p(e), methodBodyBinder))) ' Enum1)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(e), m13p(f), methodBodyBinder)) ' Enum1) 'error BC30311: Value of type 'Enum2' cannot be converted to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyDirectCastAssignment(m13p(e), m13p(g), methodBodyBinder)) ' Enum1)
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(f), m13p(f), methodBodyBinder))) ' Enum2)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(f), m13p(g), methodBodyBinder)) ' Enum2) ' error BC30311: Value of type 'Enum4' cannot be converted to 'Enum2'.
            Assert.Equal(ConversionKind.WideningArray, ClassifyDirectCastAssignment(m13p(h), m13p(i), methodBodyBinder)) ' Class8())
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyDirectCastAssignment(m13p(i), m13p(h), methodBodyBinder)) ' Class9())
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(i), m13p(j), methodBodyBinder)) ' Class9()) ' error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(k), m13p(k), methodBodyBinder))) ' MT1)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(k), m13p(l), methodBodyBinder)) ' MT1) ' error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyDirectCastAssignment(m13p(k), m13p(m), methodBodyBinder)) ' MT1)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(k), m13p(q), methodBodyBinder)) ' MT1) ' error BC30311: Value of type 'MT4' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(l), m13p(k), methodBodyBinder)) ' MT2) ' Value of type 'MT1' cannot be converted to 'MT2'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyDirectCastAssignment(m13p(m), m13p(k), methodBodyBinder)) ' MT3)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(n), m13p(o), methodBodyBinder)) ' MT1()) ' Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(n), m13p(p), methodBodyBinder)) ' MT1()) ' error BC30332: Value of type '2-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(n), m13p(u), methodBodyBinder)) ' MT1()) ' error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT1' because 'Integer' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(q), m13p(k), methodBodyBinder)) ' MT4) ' error BC30311: Value of type 'MT1' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(q), m13p(b), methodBodyBinder)) ' MT4) ' error BC30311: Value of type 'System.ValueType' cannot be converted
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(q), m13p(c), methodBodyBinder)) ' MT4) ' error BC30311: Value of type 'Integer' cannot be converted to 'MT4'
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(r), m13p(s), methodBodyBinder)) ' MT5) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(r), m13p(t), methodBodyBinder)) ' MT5) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(r), m13p(w), methodBodyBinder)) ' MT5) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(s), m13p(r), methodBodyBinder)) ' MT6) ' error BC30311: Value of type 'MT5' cannot be converted to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(s), m13p(t), methodBodyBinder)) ' MT6) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyDirectCastAssignment(m13p(s), m13p(w), methodBodyBinder)) ' MT6)
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(t), m13p(r), methodBodyBinder)) ' MT7) ' error BC30311: Value of type 'MT5' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(t), m13p(s), methodBodyBinder)) ' MT7) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(t), m13p(w), methodBodyBinder)) ' MT7) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(u), m13p(n), methodBodyBinder)) ' Integer()) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of Integer' because 'MT1' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(u), m13p(v), methodBodyBinder)) ' Integer()) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of Integer' because 'MT4' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(v), m13p(u), methodBodyBinder)) ' MT4())     'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT4' because 'Integer' is not derived from 'MT4'.

            Dim [nothing] = New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), ConstantValue.Nothing, Nothing)
            Dim intZero = New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), ConstantValue.Create(0I), m13p(c))
            Dim longZero = New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), ConstantValue.Create(0L), m13p(d))

            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(a), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(b), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(c), [nothing], methodBodyBinder))
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(c), intZero, methodBodyBinder)))
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(c), longZero, methodBodyBinder)) 'error BC30311: Value of type 'Long' cannot be converted to 'Integer'.
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(d), intZero, methodBodyBinder)) ' error BC30311: Value of type 'Integer' cannot be converted to 'Long'.
            Assert.True(Conversions.IsIdentityConversion(ClassifyDirectCastAssignment(m13p(d), longZero, methodBodyBinder)))
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyDirectCastAssignment(m13p(e), intZero, methodBodyBinder))
            Assert.Equal(s_noConversion, ClassifyDirectCastAssignment(m13p(e), longZero, methodBodyBinder)) ' error BC30311: Value of type 'Long' cannot be converted to 'Enum1'.
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(e), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(k), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyDirectCastAssignment(m13p(q), [nothing], methodBodyBinder))

            Assert.Equal(ConversionKind.WideningReference, ClassifyTryCastAssignment(m13p(a), m13p(b), methodBodyBinder)) ' Object)
            Assert.Equal(ConversionKind.WideningValue, ClassifyTryCastAssignment(m13p(a), m13p(c), methodBodyBinder)) ' Object)
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyTryCastAssignment(m13p(b), m13p(a), methodBodyBinder)) ' ValueType)
            Assert.Equal(ConversionKind.WideningValue, ClassifyTryCastAssignment(m13p(b), m13p(c), methodBodyBinder)) ' ValueType)
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyTryCastAssignment(m13p(c), m13p(a), methodBodyBinder)) ' Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyTryCastAssignment(m13p(c), m13p(b), methodBodyBinder)) ' Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(c), m13p(c), methodBodyBinder)) ' Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(c), m13p(d), methodBodyBinder)) ' Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyTryCastAssignment(m13p(c), m13p(e), methodBodyBinder)) ' Integer) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(d), m13p(d), methodBodyBinder)) ' Long)    ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(d), m13p(c), methodBodyBinder)) ' Long)    ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(e), m13p(e), methodBodyBinder)) ' Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(e), m13p(f), methodBodyBinder)) ' Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyTryCastAssignment(m13p(e), m13p(g), methodBodyBinder)) ' Enum1)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(f), m13p(f), methodBodyBinder)) ' Enum2)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum2' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(f), m13p(g), methodBodyBinder)) ' Enum2)   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum2' is a value type.
            Assert.Equal(ConversionKind.WideningArray, ClassifyTryCastAssignment(m13p(h), m13p(i), methodBodyBinder)) ' Class8())
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyTryCastAssignment(m13p(i), m13p(h), methodBodyBinder)) ' Class9())
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(i), m13p(j), methodBodyBinder)) ' Class9()) ' error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(k), m13p(k), methodBodyBinder)) ' MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(k), m13p(l), methodBodyBinder)) ' MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyTryCastAssignment(m13p(k), m13p(m), methodBodyBinder)) ' MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(k), m13p(q), methodBodyBinder)) ' MT1)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(l), m13p(k), methodBodyBinder)) ' MT2)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT2' has no class constraint.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyTryCastAssignment(m13p(m), m13p(k), methodBodyBinder)) ' MT3)      ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT3' has no class constraint.
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(n), m13p(o), methodBodyBinder)) ' MT1())
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(n), m13p(p), methodBodyBinder)) ' MT1())
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(n), m13p(u), methodBodyBinder)) ' MT1())
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(q), m13p(k), methodBodyBinder)) ' MT4)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(q), m13p(b), methodBodyBinder)) ' MT4)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(q), m13p(c), methodBodyBinder)) ' MT4)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(r), m13p(s), methodBodyBinder)) ' MT5)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(r), m13p(t), methodBodyBinder)) ' MT5)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(r), m13p(w), methodBodyBinder)) ' MT5)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(s), m13p(r), methodBodyBinder)) ' MT6)
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(s), m13p(t), methodBodyBinder)) ' MT6) ' error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyTryCastAssignment(m13p(s), m13p(w), methodBodyBinder)) ' MT6)
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(t), m13p(r), methodBodyBinder)) ' MT7)
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(t), m13p(s), methodBodyBinder)) ' MT7) ' error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(t), m13p(w), methodBodyBinder)) ' MT7) ' error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
            Assert.Equal(ConversionKind.Narrowing, ClassifyTryCastAssignment(m13p(u), m13p(n), methodBodyBinder)) ' Integer())
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(u), m13p(v), methodBodyBinder)) ' Integer()) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of Integer' because 'MT4' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(v), m13p(u), methodBodyBinder)) ' MT4())     'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of MT4' because 'Integer' is not derived from 'MT4'.

            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(a), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningValue, ClassifyTryCastAssignment(m13p(a), intZero, methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(b), [nothing], methodBodyBinder))
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(c), [nothing], methodBodyBinder)) ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type. 
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(c), intZero, methodBodyBinder))       ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(c), longZero, methodBodyBinder))      ' error BC30792: 'TryCast' operand must be reference type, but 'Integer' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(d), intZero, methodBodyBinder))         ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
            Assert.Equal(ConversionKind.Identity, ClassifyTryCastAssignment(m13p(d), longZero, methodBodyBinder))         ' error BC30792: 'TryCast' operand must be reference type, but 'Long' is a value type.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyTryCastAssignment(m13p(e), intZero, methodBodyBinder))         ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(s_noConversion, ClassifyTryCastAssignment(m13p(e), longZero, methodBodyBinder))        ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(e), [nothing], methodBodyBinder))   ' error BC30792: 'TryCast' operand must be reference type, but 'Enum1' is a value type.
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(k), [nothing], methodBodyBinder))   ' error BC30793: 'TryCast' operands must be class-constrained type parameter, but 'MT1' has no class constraint.
            Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyTryCastAssignment(m13p(q), [nothing], methodBodyBinder))

        End Sub

        Private Shared Function ClassifyDirectCastAssignment([to] As TypeSymbol, [from] As TypeSymbol, binder As Binder) As ConversionKind
            Dim result As ConversionKind = Conversions.ClassifyDirectCastConversion([from], [to], Nothing) And Not ConversionKind.MightSucceedAtRuntime
            Return result
        End Function

        Private Shared Function ClassifyDirectCastAssignment([to] As TypeSymbol, [from] As BoundLiteral, binder As Binder) As ConversionKind
            Dim result As ConversionKind = Conversions.ClassifyDirectCastConversion([from], [to], binder, Nothing)
            Return result
        End Function

        Private Shared Function ClassifyTryCastAssignment([to] As TypeSymbol, [from] As TypeSymbol, binder As Binder) As ConversionKind
            Dim result As ConversionKind = Conversions.ClassifyTryCastConversion([from], [to], Nothing)
            Return result
        End Function

        Private Shared Function ClassifyTryCastAssignment([to] As TypeSymbol, [from] As BoundLiteral, binder As Binder) As ConversionKind
            Dim result As ConversionKind = Conversions.ClassifyTryCastConversion([from], [to], binder, Nothing)
            Return result
        End Function


        Private Shared Function ClassifyConversion(source As TypeSymbol, destination As TypeSymbol) As ConversionKind
            Dim result As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(source, destination, Nothing)
            Assert.Null(result.Value)
            Return result.Key
        End Function

        Private Shared Function ClassifyConversion(source As BoundExpression, destination As TypeSymbol, binder As Binder) As ConversionKind
            Dim result As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(source, destination, binder, Nothing)
            Assert.Null(result.Value)
            Return result.Key
        End Function


        <Fact()>
        Public Sub ConstantExpressionConversions()

            Dim dummyCode =
<file>
Class C1
    Shared Sub MethodDecl()
    End Sub
End Class
</file>
            Dim dummyTree = VisualBasicSyntaxTree.ParseText(dummyCode.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", syntaxTrees:={dummyTree}, references:={TestReferences.NetFx.v4_0_21006.mscorlib})

            Dim sourceModule = DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol)
            Dim methodDeclSymbol = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("MethodDecl").Single(), SourceMethodSymbol)
            Dim methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, dummyTree, methodDeclSymbol)

            Assert.True(c1.Options.CheckOverflow)

            Dim objectType = c1.GetSpecialType(System_Object)
            Dim booleanType = c1.GetSpecialType(System_Boolean)
            Dim byteType = c1.GetSpecialType(System_Byte)
            Dim sbyteType = c1.GetSpecialType(System_SByte)
            Dim int16Type = c1.GetSpecialType(System_Int16)
            Dim uint16Type = c1.GetSpecialType(System_UInt16)
            Dim int32Type = c1.GetSpecialType(System_Int32)
            Dim uint32Type = c1.GetSpecialType(System_UInt32)
            Dim int64Type = c1.GetSpecialType(System_Int64)
            Dim uint64Type = c1.GetSpecialType(System_UInt64)
            Dim doubleType = c1.GetSpecialType(System_Double)
            Dim singleType = c1.GetSpecialType(System_Single)
            Dim decimalType = c1.GetSpecialType(System_Decimal)
            Dim dateType = c1.GetSpecialType(System_DateTime)
            Dim stringType = c1.GetSpecialType(System_String)
            Dim charType = c1.GetSpecialType(System_Char)
            Dim intPtrType = c1.GetSpecialType(System_IntPtr)
            Dim typeCodeType = c1.GlobalNamespace.GetMembers("System").OfType(Of NamespaceSymbol)().Single().GetTypeMembers("TypeCode").Single()

            Dim allTestTypes = New TypeSymbol() {
                    objectType, booleanType, byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, dateType,
                    stringType, charType, intPtrType, typeCodeType}

            Dim convertibleTypes = New HashSet(Of TypeSymbol)({
                    booleanType, byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, dateType,
                    stringType, charType, typeCodeType})

            Dim integralTypes = New HashSet(Of TypeSymbol)({
                    byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, typeCodeType})

            Dim unsignedTypes = New HashSet(Of TypeSymbol)({
                    byteType, uint16Type, uint32Type, uint64Type})

            Dim numericTypes = New HashSet(Of TypeSymbol)({
                    byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, typeCodeType})

            Dim floatingTypes = New HashSet(Of TypeSymbol)({doubleType, singleType})

            ' -------------- NOTHING literal conversions

            Dim _nothing = New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), ConstantValue.Nothing, Nothing)

            Dim resultValue As ConstantValue
            Dim integerOverflow As Boolean
            Dim literal As BoundExpression
            Dim constant As BoundConversion

            For Each testType In allTestTypes
                Assert.Equal(ConversionKind.WideningNothingLiteral, ClassifyConversion(_nothing, testType, methodBodyBinder))

                resultValue = Conversions.TryFoldConstantConversion(_nothing, testType, integerOverflow)

                If convertibleTypes.Contains(testType) Then
                    Assert.NotNull(resultValue)
                    Assert.Equal(If(testType.IsStringType(), ConstantValueTypeDiscriminator.Nothing, testType.GetConstantValueTypeDiscriminator()), resultValue.Discriminator)

                    If testType IsNot dateType Then
                        Assert.Equal(0, Convert.ToInt64(resultValue.Value))

                        If testType Is stringType Then
                            Assert.Null(resultValue.StringValue)
                        End If
                    Else
                        Assert.Equal(New DateTime(), resultValue.DateTimeValue)
                    End If
                Else
                    Assert.Null(resultValue)
                End If

                Assert.False(integerOverflow)
                If resultValue IsNot Nothing Then
                    Assert.False(resultValue.IsBad)
                End If
            Next

            ' -------------- integer literal zero to enum conversions

            For Each integralType In integralTypes

                Dim zero = ConstantValue.Default(integralType.GetConstantValueTypeDiscriminator())
                literal = New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), zero, integralType)
                constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), literal, ConversionKind.Widening, True, True, zero, integralType, Nothing)

                Assert.Equal(If(integralType Is int32Type, ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, If(integralType Is typeCodeType, ConversionKind.Identity, ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions)),
                             ClassifyConversion(literal, typeCodeType, methodBodyBinder))
                resultValue = Conversions.TryFoldConstantConversion(literal, typeCodeType, integerOverflow)

                Assert.NotNull(resultValue)
                Assert.False(integerOverflow)
                Assert.Equal(ConstantValueTypeDiscriminator.Int32, resultValue.Discriminator)
                Assert.Equal(0, resultValue.Int32Value)

                Assert.Equal(If(integralType Is typeCodeType, ConversionKind.Identity, ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions), ClassifyConversion(constant, typeCodeType, methodBodyBinder))
                resultValue = Conversions.TryFoldConstantConversion(constant, typeCodeType, integerOverflow)

                Assert.NotNull(resultValue)
                Assert.False(integerOverflow)
                Assert.Equal(ConstantValueTypeDiscriminator.Int32, resultValue.Discriminator)
                Assert.Equal(0, resultValue.Int32Value)
            Next

            For Each convertibleType In convertibleTypes
                If Not integralTypes.Contains(convertibleType) Then

                    Dim zero = ConstantValue.Default(If(convertibleType.IsStringType(), ConstantValueTypeDiscriminator.Nothing, convertibleType.GetConstantValueTypeDiscriminator()))

                    If convertibleType.IsStringType() Then
                        literal = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(DirectCast(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), VisualBasicSyntaxNode), ConstantValue.Null, Nothing), ConversionKind.WideningNothingLiteral, False, True, zero, convertibleType, Nothing)
                    Else
                        literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), zero, convertibleType)
                    End If

                    Assert.Equal(ClassifyConversion(convertibleType, typeCodeType), ClassifyConversion(literal, typeCodeType, methodBodyBinder))

                    resultValue = Conversions.TryFoldConstantConversion(literal, typeCodeType, integerOverflow)

                    If Not numericTypes.Contains(convertibleType) AndAlso convertibleType IsNot booleanType Then
                        Assert.Null(resultValue)
                        Assert.False(integerOverflow)
                    Else
                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.Equal(ConstantValueTypeDiscriminator.Int32, resultValue.Discriminator)
                        Assert.Equal(0, resultValue.Int32Value)
                    End If
                End If
            Next

            ' -------------- Numeric conversions

            Dim nullableType = c1.GetSpecialType(System_Nullable_T)

            ' Zero
            For Each type1 In convertibleTypes

                Dim zero = ConstantValue.Default(If(type1.IsStringType(), ConstantValueTypeDiscriminator.Nothing, type1.GetConstantValueTypeDiscriminator()))

                If type1.IsStringType() Then
                    literal = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), ConstantValue.Null, Nothing), ConversionKind.WideningNothingLiteral, False, True, zero, type1, Nothing)
                Else
                    literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), zero, type1)
                End If

                constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing),
                                               New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Default(ConstantValueTypeDiscriminator.Int32), int32Type),
                                               ConversionKind.Widening, True, True, zero, type1, Nothing)

                For Each type2 In allTestTypes

                    Dim expectedConv As ConversionKind

                    If numericTypes.Contains(type1) AndAlso numericTypes.Contains(type2) Then

                        If type1 Is type2 Then
                            expectedConv = ConversionKind.Identity

                        ElseIf type1.IsEnumType() Then

                            expectedConv = ClassifyConversion(type1.GetEnumUnderlyingTypeOrSelf(), type2)

                            If Conversions.IsWideningConversion(expectedConv) Then
                                expectedConv = expectedConv Or ConversionKind.InvolvesEnumTypeConversions
                                If (expectedConv And ConversionKind.Identity) <> 0 Then
                                    expectedConv = (expectedConv And Not ConversionKind.Identity) Or ConversionKind.Widening Or ConversionKind.Numeric
                                End If
                            ElseIf Conversions.IsNarrowingConversion(expectedConv) Then
                                expectedConv = expectedConv Or ConversionKind.InvolvesEnumTypeConversions
                            End If

                        ElseIf type2.IsEnumType() Then

                            expectedConv = ClassifyConversion(type1, type2.GetEnumUnderlyingTypeOrSelf())

                            If Not Conversions.NoConversion(expectedConv) Then
                                expectedConv = (expectedConv And Not ConversionKind.Widening) Or ConversionKind.Narrowing Or ConversionKind.InvolvesEnumTypeConversions
                                If (expectedConv And ConversionKind.Identity) <> 0 Then
                                    expectedConv = (expectedConv And Not ConversionKind.Identity) Or ConversionKind.Numeric
                                End If
                            End If

                        ElseIf integralTypes.Contains(type2) Then

                            If integralTypes.Contains(type1) Then
                                If Conversions.IsNarrowingConversion(ClassifyConversion(type1, type2)) Then
                                    expectedConv = ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant
                                Else
                                    expectedConv = ConversionKind.WideningNumeric
                                End If
                            Else
                                expectedConv = ConversionKind.NarrowingNumeric
                            End If
                        ElseIf floatingTypes.Contains(type2) Then

                            If Conversions.IsNarrowingConversion(ClassifyConversion(type1, type2)) Then
                                expectedConv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant
                            Else
                                expectedConv = ConversionKind.WideningNumeric
                            End If
                        Else
                            Assert.Same(decimalType, type2)

                            expectedConv = ClassifyConversion(type1, type2)
                        End If

                        Assert.Equal(If(type2.IsEnumType() AndAlso type1 Is int32Type, ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, expectedConv), ClassifyConversion(literal, type2, methodBodyBinder))
                        Assert.Equal(expectedConv, ClassifyConversion(constant, type2, methodBodyBinder))

                        resultValue = Conversions.TryFoldConstantConversion(literal, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(type2.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        Assert.Equal(0, Convert.ToDouble(resultValue.Value))

                        resultValue = Conversions.TryFoldConstantConversion(constant, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(type2.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        Assert.Equal(0, Convert.ToDouble(resultValue.Value))

                        Dim nullableType2 = nullableType.Construct(type2)

                        expectedConv = ClassifyConversion(type1, nullableType2) Or (expectedConv And ConversionKind.InvolvesNarrowingFromNumericConstant)

                        If type2.IsEnumType() AndAlso type1.SpecialType = SpecialType.System_Int32 Then
                            Assert.Equal(expectedConv Or ConversionKind.InvolvesNarrowingFromNumericConstant, ClassifyConversion(literal, nullableType2, methodBodyBinder))
                        Else
                            Assert.Equal(expectedConv, ClassifyConversion(literal, nullableType2, methodBodyBinder))
                        End If

                        Assert.Equal(expectedConv, ClassifyConversion(constant, nullableType2, methodBodyBinder))

                        resultValue = Conversions.TryFoldConstantConversion(literal, nullableType2, integerOverflow)
                        Assert.Null(resultValue)
                        Assert.False(integerOverflow)

                        resultValue = Conversions.TryFoldConstantConversion(constant, nullableType2, integerOverflow)
                        Assert.Null(resultValue)
                        Assert.False(integerOverflow)

                    ElseIf type1 Is booleanType AndAlso numericTypes.Contains(type2) Then
                        ' Will test separately   
                        Continue For
                    ElseIf type2 Is booleanType AndAlso numericTypes.Contains(type1) Then
                        Assert.Equal(If(type1 Is typeCodeType, ConversionKind.NarrowingBoolean Or ConversionKind.InvolvesEnumTypeConversions, ConversionKind.NarrowingBoolean), ClassifyConversion(literal, type2, methodBodyBinder))
                        Assert.Equal(If(type1 Is typeCodeType, ConversionKind.NarrowingBoolean Or ConversionKind.InvolvesEnumTypeConversions, ConversionKind.NarrowingBoolean), ClassifyConversion(constant, type2, methodBodyBinder))

                        resultValue = Conversions.TryFoldConstantConversion(literal, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(type2.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        Assert.False(DirectCast(resultValue.Value, Boolean))

                        resultValue = Conversions.TryFoldConstantConversion(constant, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(type2.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        Assert.False(DirectCast(resultValue.Value, Boolean))
                    ElseIf type1 Is stringType AndAlso type2 Is charType Then
                        ' Will test separately   
                        Continue For
                    ElseIf type1 Is charType AndAlso type2 Is stringType Then
                        ' Will test separately   
                        Continue For
                    ElseIf type2 Is typeCodeType AndAlso integralTypes.Contains(type1) Then
                        ' Already tested
                        Continue For
                    ElseIf (type1 Is dateType AndAlso type2 Is dateType) OrElse
                        (type1 Is booleanType AndAlso type2 Is booleanType) OrElse
                        (type1 Is stringType AndAlso type2 Is stringType) OrElse
                        (type1 Is charType AndAlso type2 Is charType) Then
                        Assert.True(Conversions.IsIdentityConversion(ClassifyConversion(literal, type2, methodBodyBinder)))
                        Assert.True(Conversions.IsIdentityConversion(ClassifyConversion(constant, type2, methodBodyBinder)))

                        resultValue = Conversions.TryFoldConstantConversion(literal, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.True(type2.IsValidForConstantValue(resultValue))
                        Assert.Equal(literal.ConstantValueOpt, resultValue)

                        resultValue = Conversions.TryFoldConstantConversion(constant, type2, integerOverflow)

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.True(type2.IsValidForConstantValue(resultValue))
                        Assert.Equal(constant.ConstantValueOpt, resultValue)
                    Else
                        Dim expectedConv1 As KeyValuePair(Of ConversionKind, MethodSymbol) = Conversions.ClassifyConversion(type1, type2, Nothing)

                        Assert.Equal(expectedConv1, Conversions.ClassifyConversion(literal, type2, methodBodyBinder, Nothing))
                        Assert.Equal(expectedConv1, Conversions.ClassifyConversion(constant, type2, methodBodyBinder, Nothing))

                        resultValue = Conversions.TryFoldConstantConversion(literal, type2, integerOverflow)
                        Assert.Null(resultValue)
                        Assert.False(integerOverflow)

                        resultValue = Conversions.TryFoldConstantConversion(constant, type2, integerOverflow)
                        Assert.Null(resultValue)
                        Assert.False(integerOverflow)

                        If type2.IsValueType Then
                            Dim nullableType2 = nullableType.Construct(type2)

                            expectedConv1 = Conversions.ClassifyConversion(type1, nullableType2, Nothing)

                            Assert.Equal(expectedConv1, Conversions.ClassifyConversion(literal, nullableType2, methodBodyBinder, Nothing))
                            Assert.Equal(expectedConv1, Conversions.ClassifyConversion(constant, nullableType2, methodBodyBinder, Nothing))

                            resultValue = Conversions.TryFoldConstantConversion(literal, nullableType2, integerOverflow)
                            Assert.Null(resultValue)
                            Assert.False(integerOverflow)

                            resultValue = Conversions.TryFoldConstantConversion(constant, nullableType2, integerOverflow)
                            Assert.Null(resultValue)
                            Assert.False(integerOverflow)
                        End If
                    End If

                Next
            Next


            ' -------- Numeric non-zero values
            Dim nonZeroValues = New TypeAndValue() {
                    New TypeAndValue(sbyteType, SByte.MinValue),
                    New TypeAndValue(int16Type, Int16.MinValue),
                    New TypeAndValue(int32Type, Int32.MinValue),
                    New TypeAndValue(int64Type, Int64.MinValue),
                    New TypeAndValue(doubleType, Double.MinValue),
                    New TypeAndValue(singleType, Single.MinValue),
                    New TypeAndValue(decimalType, Decimal.MinValue),
                    New TypeAndValue(sbyteType, SByte.MaxValue),
                    New TypeAndValue(int16Type, Int16.MaxValue),
                    New TypeAndValue(int32Type, Int32.MaxValue),
                    New TypeAndValue(int64Type, Int64.MaxValue),
                    New TypeAndValue(byteType, Byte.MaxValue),
                    New TypeAndValue(uint16Type, UInt16.MaxValue),
                    New TypeAndValue(uint32Type, UInt32.MaxValue),
                    New TypeAndValue(uint64Type, UInt64.MaxValue),
                    New TypeAndValue(doubleType, Double.MaxValue),
                    New TypeAndValue(singleType, Single.MaxValue),
                    New TypeAndValue(decimalType, Decimal.MaxValue),
                    New TypeAndValue(sbyteType, CSByte(-1)),
                    New TypeAndValue(int16Type, CShort(-2)),
                    New TypeAndValue(int32Type, CInt(-3)),
                    New TypeAndValue(int64Type, CLng(-4)),
                    New TypeAndValue(sbyteType, CSByte(5)),
                    New TypeAndValue(int16Type, CShort(6)),
                    New TypeAndValue(int32Type, CInt(7)),
                    New TypeAndValue(int64Type, CLng(8)),
                    New TypeAndValue(doubleType, CDbl(-9)),
                    New TypeAndValue(singleType, CSng(-10)),
                    New TypeAndValue(decimalType, CDec(-11)),
                    New TypeAndValue(doubleType, CDbl(12)),
                    New TypeAndValue(singleType, CSng(13)),
                    New TypeAndValue(decimalType, CDec(14)),
                    New TypeAndValue(byteType, CByte(15)),
                    New TypeAndValue(uint16Type, CUShort(16)),
                    New TypeAndValue(uint32Type, CUInt(17)),
                    New TypeAndValue(uint64Type, CULng(18)),
                    New TypeAndValue(decimalType, CDec(-11.3)),
                    New TypeAndValue(doubleType, CDbl(&HF000000000000000UL)),
                    New TypeAndValue(doubleType, CDbl(&H70000000000000F0L)),
                    New TypeAndValue(typeCodeType, Int32.MinValue),
                    New TypeAndValue(typeCodeType, Int32.MaxValue),
                    New TypeAndValue(typeCodeType, CInt(-3)),
                    New TypeAndValue(typeCodeType, CInt(7))
                    }


            Dim resultValue2 As ConstantValue
            Dim integerOverflow2 As Boolean

            For Each mv In nonZeroValues

                Dim v = ConstantValue.Create(mv.Value, mv.Type.GetConstantValueTypeDiscriminator())

                Assert.Equal(v.Discriminator, mv.Type.GetConstantValueTypeDiscriminator())

                literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), v, mv.Type)
                constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing),
                                               New BoundLiteral(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), ConstantValue.Null, Nothing),
                                               ConversionKind.Widening, True, True, v, mv.Type, Nothing)

                For Each numericType In numericTypes

                    Dim typeConv = ClassifyConversion(mv.Type, numericType)

                    Dim conv = ClassifyConversion(literal, numericType, methodBodyBinder)
                    Dim conv2 = ClassifyConversion(constant, numericType, methodBodyBinder)

                    Assert.Equal(conv, conv2)

                    resultValue = Conversions.TryFoldConstantConversion(literal, numericType, integerOverflow)
                    resultValue2 = Conversions.TryFoldConstantConversion(constant, numericType, integerOverflow2)

                    Assert.Equal(resultValue Is Nothing, resultValue2 Is Nothing)
                    Assert.Equal(integerOverflow, integerOverflow2)
                    Assert.Equal(resultValue IsNot Nothing AndAlso resultValue.IsBad, resultValue2 IsNot Nothing AndAlso resultValue2.IsBad)

                    If resultValue IsNot Nothing Then
                        Assert.Equal(resultValue2, resultValue)

                        If Not resultValue.IsBad Then
                            Assert.Equal(numericType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        End If
                    End If

                    Dim resultValueAsObject As Object = Nothing
                    Dim overflow As Boolean = False

                    Try
                        resultValueAsObject = CheckedConvert(v.Value, numericType)
                    Catch ex As OverflowException
                        overflow = True
                    End Try

                    If Not overflow Then
                        If Conversions.IsIdentityConversion(typeConv) Then
                            Assert.True(Conversions.IsIdentityConversion(conv))
                        ElseIf Conversions.IsNarrowingConversion(typeConv) Then
                            If mv.Type Is doubleType AndAlso numericType Is singleType Then
                                Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, conv)
                            ElseIf integralTypes.Contains(mv.Type) AndAlso integralTypes.Contains(numericType) AndAlso Not mv.Type.IsEnumType() AndAlso Not numericType.IsEnumType() Then
                                Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, conv)
                            Else
                                Assert.Equal(typeConv, conv)
                            End If
                        ElseIf mv.Type.IsEnumType() Then
                            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, conv)
                        Else
                            Assert.Equal(ConversionKind.WideningNumeric, conv)
                        End If

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)
                        Assert.Equal(resultValueAsObject, resultValue.Value)

                        If mv.Type Is doubleType AndAlso numericType Is singleType Then
                            If v.DoubleValue = Double.MinValue Then
                                Dim min As Single = Double.MinValue
                                Assert.True(Single.IsNegativeInfinity(min))
                                Assert.Equal(resultValue.SingleValue, min)
                            ElseIf v.DoubleValue = Double.MaxValue Then
                                Dim max As Single = Double.MaxValue

                                Assert.Equal(Double.MaxValue, v.DoubleValue)
                                Assert.True(Single.IsPositiveInfinity(max))
                                Assert.Equal(resultValue.SingleValue, max)
                            End If
                        End If

                    ElseIf Not integralTypes.Contains(mv.Type) OrElse Not integralTypes.Contains(numericType) Then
                        'Assert.Equal(typeConv, conv)

                        If integralTypes.Contains(numericType) Then

                            Assert.NotNull(resultValue)

                            If resultValue.IsBad Then
                                Assert.False(integerOverflow)
                                Assert.Equal(ConversionKind.FailedDueToNumericOverflow, conv)

                            Else
                                Assert.True(integerOverflow)
                                Assert.Equal(ConversionKind.FailedDueToIntegerOverflow, conv)

                                Dim intermediate As Object

                                If unsignedTypes.Contains(numericType) Then
                                    intermediate = Convert.ToUInt64(mv.Value)
                                Else
                                    intermediate = Convert.ToInt64(mv.Value)
                                End If

                                Dim gotException As Boolean
                                Try
                                    gotException = False
                                    CheckedConvert(intermediate, numericType) ' Should get an overflow
                                Catch x As Exception
                                    gotException = True
                                End Try

                                Assert.True(gotException)

                                Assert.Equal(UncheckedConvert(intermediate, numericType), resultValue.Value)
                            End If
                        Else
                            Assert.NotNull(resultValue)
                            Assert.False(integerOverflow)
                            Assert.True(resultValue.IsBad)
                            Assert.Equal(ConversionKind.FailedDueToNumericOverflow, conv)
                        End If
                    Else
                        ' An integer overflow case
                        Assert.Equal(ConversionKind.FailedDueToIntegerOverflow, conv)
                        Assert.NotNull(resultValue)
                        Assert.True(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(UncheckedConvert(v.Value, numericType), resultValue.Value)
                    End If

                    Dim nullableType2 = nullableType.Construct(numericType)
                    Dim zero = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Default(mv.Type.GetConstantValueTypeDiscriminator()), mv.Type, Nothing)

                    conv = ClassifyConversion(literal, numericType, methodBodyBinder)

                    If (conv And ConversionKind.FailedDueToNumericOverflowMask) = 0 Then
                        conv = ClassifyConversion(mv.Type, nullableType2) Or
                            (ClassifyConversion(zero, nullableType2, methodBodyBinder) And ConversionKind.InvolvesNarrowingFromNumericConstant)
                    End If

                    Assert.Equal(conv, ClassifyConversion(literal, nullableType2, methodBodyBinder))
                    Assert.Equal(conv, ClassifyConversion(constant, nullableType2, methodBodyBinder))

                    resultValue = Conversions.TryFoldConstantConversion(literal, nullableType2, integerOverflow)
                    Assert.Null(resultValue)
                    Assert.False(integerOverflow)

                    resultValue = Conversions.TryFoldConstantConversion(constant, nullableType2, integerOverflow)
                    Assert.Null(resultValue)
                    Assert.False(integerOverflow)
                Next

            Next


            Dim dbl As Double = -1.5
            Dim doubleValue = ConstantValue.Create(dbl)

            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, doubleValue, doubleType, Nothing)
            resultValue = Conversions.TryFoldConstantConversion(constant, int32Type, integerOverflow)

            Assert.NotNull(resultValue)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)

            Assert.Equal(-2, CInt(dbl))
            Assert.Equal(-2, DirectCast(resultValue.Value, Int32))

            dbl = -2.5
            doubleValue = ConstantValue.Create(dbl)

            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, doubleValue, doubleType, Nothing)
            resultValue = Conversions.TryFoldConstantConversion(constant, int32Type, integerOverflow)

            Assert.NotNull(resultValue)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)

            Assert.Equal(-2, CInt(dbl))
            Assert.Equal(-2, DirectCast(resultValue.Value, Int32))

            dbl = 1.5
            doubleValue = ConstantValue.Create(dbl)

            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, doubleValue, doubleType, Nothing)
            resultValue = Conversions.TryFoldConstantConversion(constant, uint32Type, integerOverflow)

            Assert.NotNull(resultValue)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)

            Assert.Equal(2UI, CUInt(dbl))
            Assert.Equal(2UI, DirectCast(resultValue.Value, UInt32))

            dbl = 2.5
            doubleValue = ConstantValue.Create(dbl)

            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, doubleValue, doubleType, Nothing)
            resultValue = Conversions.TryFoldConstantConversion(constant, uint32Type, integerOverflow)

            Assert.NotNull(resultValue)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)

            Assert.Equal(2UI, CUInt(dbl))
            Assert.Equal(2UI, DirectCast(resultValue.Value, UInt32))

            dbl = 2147483648.0 * 4294967296.0 + 10
            doubleValue = ConstantValue.Create(dbl)

            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, doubleValue, doubleType, Nothing)
            resultValue = Conversions.TryFoldConstantConversion(constant, uint64Type, integerOverflow)

            Assert.NotNull(resultValue)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)

            Assert.Equal(Convert.ToUInt64(dbl), DirectCast(resultValue.Value, UInt64))


            ' -------  Boolean
            Dim falseValue = ConstantValue.Create(False)

            Assert.Equal(falseValue.Discriminator, booleanType.GetConstantValueTypeDiscriminator())

            literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), falseValue, booleanType)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, falseValue, booleanType, Nothing)

            For Each numericType In numericTypes
                Dim typeConv = ClassifyConversion(booleanType, numericType)

                Dim conv = ClassifyConversion(literal, numericType, methodBodyBinder)
                Dim conv2 = ClassifyConversion(constant, numericType, methodBodyBinder)

                Assert.Equal(conv, conv2)
                Assert.Equal(typeConv, conv)

                resultValue = Conversions.TryFoldConstantConversion(literal, numericType, integerOverflow)
                resultValue2 = Conversions.TryFoldConstantConversion(constant, numericType, integerOverflow2)

                Assert.NotNull(resultValue)
                Assert.NotNull(resultValue2)
                Assert.False(integerOverflow)
                Assert.False(resultValue.IsBad)
                Assert.Equal(integerOverflow, integerOverflow2)
                Assert.Equal(resultValue.IsBad, resultValue2.IsBad)

                Assert.Equal(resultValue2, resultValue)
                Assert.Equal(numericType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

                Assert.Equal(0, Convert.ToInt64(resultValue.Value))
            Next

            Dim trueValue = ConstantValue.Create(True)

            Assert.Equal(falseValue.Discriminator, booleanType.GetConstantValueTypeDiscriminator())

            literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), trueValue, booleanType)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, trueValue, booleanType, Nothing)

            For Each numericType In numericTypes
                Dim typeConv = ClassifyConversion(booleanType, numericType)

                Dim conv = ClassifyConversion(literal, numericType, methodBodyBinder)
                Dim conv2 = ClassifyConversion(constant, numericType, methodBodyBinder)

                Assert.Equal(conv, conv2)
                Assert.Equal(typeConv, conv)

                resultValue = Conversions.TryFoldConstantConversion(literal, numericType, integerOverflow)
                resultValue2 = Conversions.TryFoldConstantConversion(constant, numericType, integerOverflow2)

                Assert.NotNull(resultValue)
                Assert.NotNull(resultValue2)
                Assert.False(integerOverflow)
                Assert.False(resultValue.IsBad)
                Assert.Equal(integerOverflow, integerOverflow2)
                Assert.Equal(resultValue.IsBad, resultValue2.IsBad)

                Assert.Equal(resultValue2, resultValue)
                Assert.Equal(numericType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

                'The literal True converts to the literal 255 for Byte, 65535 for UShort, 4294967295 for UInteger, 18446744073709551615 for ULong, 
                'and to the expression -1 for SByte, Short, Integer, Long, Decimal, Single, and Double

                If numericType Is byteType Then
                    Assert.Equal(255, DirectCast(resultValue.Value, Byte))
                ElseIf numericType Is uint16Type Then
                    Assert.Equal(65535, DirectCast(resultValue.Value, UInt16))
                ElseIf numericType Is uint32Type Then
                    Assert.Equal(4294967295, DirectCast(resultValue.Value, UInt32))
                ElseIf numericType Is uint64Type Then
                    Assert.Equal(18446744073709551615UL, DirectCast(resultValue.Value, UInt64))
                Else
                    Assert.Equal(-1, Convert.ToInt64(resultValue.Value))
                End If

            Next


            resultValue = Conversions.TryFoldConstantConversion(literal, booleanType, integerOverflow)
            resultValue2 = Conversions.TryFoldConstantConversion(constant, booleanType, integerOverflow2)

            Assert.NotNull(resultValue)
            Assert.NotNull(resultValue2)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)
            Assert.Equal(integerOverflow, integerOverflow2)
            Assert.Equal(resultValue.IsBad, resultValue2.IsBad)

            Assert.Equal(resultValue2, resultValue)
            Assert.Equal(booleanType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

            Assert.True(DirectCast(resultValue.Value, Boolean))

            For Each mv In nonZeroValues

                Dim v = ConstantValue.Create(mv.Value, mv.Type.GetConstantValueTypeDiscriminator())

                Assert.Equal(v.Discriminator, mv.Type.GetConstantValueTypeDiscriminator())

                literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), v, mv.Type)
                constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, v, mv.Type, Nothing)

                Dim typeConv = ClassifyConversion(mv.Type, booleanType)

                Dim conv = ClassifyConversion(literal, booleanType, methodBodyBinder)
                Dim conv2 = ClassifyConversion(constant, booleanType, methodBodyBinder)

                Assert.Equal(conv, conv2)
                Assert.Equal(typeConv, conv)

                resultValue = Conversions.TryFoldConstantConversion(literal, booleanType, integerOverflow)
                resultValue2 = Conversions.TryFoldConstantConversion(constant, booleanType, integerOverflow2)

                Assert.NotNull(resultValue)
                Assert.NotNull(resultValue2)
                Assert.False(integerOverflow)
                Assert.False(resultValue.IsBad)
                Assert.Equal(integerOverflow, integerOverflow2)

                Assert.Equal(resultValue2, resultValue)
                Assert.Equal(booleanType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

                Assert.True(DirectCast(resultValue.Value, Boolean))

            Next


            ' -------  String <-> Char

            Dim stringValue = ConstantValue.Nothing

            Assert.Null(stringValue.StringValue)

            literal = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), stringValue, Nothing), ConversionKind.WideningNothingLiteral, False, True, stringValue, stringType, Nothing)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, stringValue, stringType, Nothing)

            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(literal, charType, methodBodyBinder))
            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(constant, charType, methodBodyBinder))

            resultValue = Conversions.TryFoldConstantConversion(literal, charType, integerOverflow)
            resultValue2 = Conversions.TryFoldConstantConversion(constant, charType, integerOverflow2)

            Assert.NotNull(resultValue)
            Assert.NotNull(resultValue2)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)
            Assert.Equal(integerOverflow, integerOverflow2)

            Assert.Equal(resultValue2, resultValue)
            Assert.Equal(charType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

            Assert.Equal(ChrW(0), CChar(stringValue.StringValue))
            Assert.Equal(ChrW(0), DirectCast(resultValue.Value, Char))


            stringValue = ConstantValue.Create("")

            Assert.Equal(0, stringValue.StringValue.Length)

            literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), stringValue, stringType)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, stringValue, stringType, Nothing)

            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(literal, charType, methodBodyBinder))
            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(constant, charType, methodBodyBinder))

            resultValue = Conversions.TryFoldConstantConversion(literal, charType, integerOverflow)
            resultValue2 = Conversions.TryFoldConstantConversion(constant, charType, integerOverflow2)

            Assert.NotNull(resultValue)
            Assert.NotNull(resultValue2)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)
            Assert.Equal(integerOverflow, integerOverflow2)

            Assert.Equal(resultValue2, resultValue)
            Assert.Equal(charType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

            Assert.Equal(ChrW(0), CChar(""))
            Assert.Equal(ChrW(0), DirectCast(resultValue.Value, Char))


            stringValue = ConstantValue.Create("abc")

            literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), stringValue, stringType)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, stringValue, stringType, Nothing)

            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(literal, charType, methodBodyBinder))
            Assert.Equal(ConversionKind.NarrowingString, ClassifyConversion(constant, charType, methodBodyBinder))

            resultValue = Conversions.TryFoldConstantConversion(literal, charType, integerOverflow)
            resultValue2 = Conversions.TryFoldConstantConversion(constant, charType, integerOverflow2)

            Assert.NotNull(resultValue)
            Assert.NotNull(resultValue2)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)
            Assert.Equal(integerOverflow, integerOverflow2)

            Assert.Equal(resultValue2, resultValue)
            Assert.Equal(charType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

            Assert.Equal("a"c, DirectCast(resultValue.Value, Char))


            Dim charValue = ConstantValue.Create("b"c)

            literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), charValue, charType)
            constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing), New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, charValue, charType, Nothing)

            Assert.Equal(ConversionKind.WideningString, ClassifyConversion(literal, stringType, methodBodyBinder))
            Assert.Equal(ConversionKind.WideningString, ClassifyConversion(constant, stringType, methodBodyBinder))

            resultValue = Conversions.TryFoldConstantConversion(literal, stringType, integerOverflow)
            resultValue2 = Conversions.TryFoldConstantConversion(constant, stringType, integerOverflow2)

            Assert.NotNull(resultValue)
            Assert.NotNull(resultValue2)
            Assert.False(integerOverflow)
            Assert.False(resultValue.IsBad)
            Assert.Equal(integerOverflow, integerOverflow2)

            Assert.Equal(resultValue2, resultValue)
            Assert.Equal(stringType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)

            Assert.Equal("b", DirectCast(resultValue.Value, String))

        End Sub

        <Fact()>
        Public Sub ConstantExpressionConversions2()
            Dim dummyCode =
<file>
Class C1
    Shared Sub MethodDecl()
    End Sub
End Class
</file>
            Dim dummyTree = VisualBasicSyntaxTree.ParseText(dummyCode.Value)

            Dim c1 = VisualBasicCompilation.Create("Test", syntaxTrees:={dummyTree}, references:={TestReferences.NetFx.v4_0_21006.mscorlib},
                                        options:=TestOptions.ReleaseExe.WithOverflowChecks(False))

            Dim sourceModule = DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol)
            Dim methodDeclSymbol = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("C1").Single().GetMembers("MethodDecl").Single(), SourceMethodSymbol)
            Dim methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, dummyTree, methodDeclSymbol)

            Assert.False(c1.Options.CheckOverflow)

            Dim objectType = c1.GetSpecialType(System_Object)
            Dim booleanType = c1.GetSpecialType(System_Boolean)
            Dim byteType = c1.GetSpecialType(System_Byte)
            Dim sbyteType = c1.GetSpecialType(System_SByte)
            Dim int16Type = c1.GetSpecialType(System_Int16)
            Dim uint16Type = c1.GetSpecialType(System_UInt16)
            Dim int32Type = c1.GetSpecialType(System_Int32)
            Dim uint32Type = c1.GetSpecialType(System_UInt32)
            Dim int64Type = c1.GetSpecialType(System_Int64)
            Dim uint64Type = c1.GetSpecialType(System_UInt64)
            Dim doubleType = c1.GetSpecialType(System_Double)
            Dim singleType = c1.GetSpecialType(System_Single)
            Dim decimalType = c1.GetSpecialType(System_Decimal)
            Dim dateType = c1.GetSpecialType(System_DateTime)
            Dim stringType = c1.GetSpecialType(System_String)
            Dim charType = c1.GetSpecialType(System_Char)
            Dim intPtrType = c1.GetSpecialType(System_IntPtr)
            Dim typeCodeType = c1.GlobalNamespace.GetMembers("System").OfType(Of NamespaceSymbol)().Single().GetTypeMembers("TypeCode").Single()

            Dim allTestTypes = New TypeSymbol() {
                    objectType, booleanType, byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, dateType,
                    stringType, charType, intPtrType, typeCodeType}

            Dim convertibleTypes = New HashSet(Of TypeSymbol)({
                    booleanType, byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, dateType,
                    stringType, charType, typeCodeType})

            Dim integralTypes = New HashSet(Of TypeSymbol)({
                    byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, typeCodeType})

            Dim unsignedTypes = New HashSet(Of TypeSymbol)({
                    byteType, uint16Type, uint32Type, uint64Type})

            Dim numericTypes = New HashSet(Of TypeSymbol)({
                    byteType, sbyteType, int16Type, uint16Type, int32Type, uint32Type, int64Type, uint64Type, doubleType, singleType, decimalType, typeCodeType})

            Dim floatingTypes = New HashSet(Of TypeSymbol)({doubleType, singleType})

            Dim _nothing = New BoundLiteral(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), ConstantValue.Nothing, Nothing)

            Dim resultValue As ConstantValue
            Dim integerOverflow As Boolean
            Dim literal As BoundLiteral
            Dim constant As BoundConversion
            Dim nullableType = c1.GetSpecialType(System_Nullable_T)

            ' -------- Numeric non-zero values
            Dim nonZeroValues = New TypeAndValue() {
                    New TypeAndValue(sbyteType, SByte.MinValue),
                    New TypeAndValue(int16Type, Int16.MinValue),
                    New TypeAndValue(int32Type, Int32.MinValue),
                    New TypeAndValue(int64Type, Int64.MinValue),
                    New TypeAndValue(doubleType, Double.MinValue),
                    New TypeAndValue(singleType, Single.MinValue),
                    New TypeAndValue(decimalType, Decimal.MinValue),
                    New TypeAndValue(sbyteType, SByte.MaxValue),
                    New TypeAndValue(int16Type, Int16.MaxValue),
                    New TypeAndValue(int32Type, Int32.MaxValue),
                    New TypeAndValue(int64Type, Int64.MaxValue),
                    New TypeAndValue(byteType, Byte.MaxValue),
                    New TypeAndValue(uint16Type, UInt16.MaxValue),
                    New TypeAndValue(uint32Type, UInt32.MaxValue),
                    New TypeAndValue(uint64Type, UInt64.MaxValue),
                    New TypeAndValue(doubleType, Double.MaxValue),
                    New TypeAndValue(singleType, Single.MaxValue),
                    New TypeAndValue(decimalType, Decimal.MaxValue),
                    New TypeAndValue(sbyteType, CSByte(-1)),
                    New TypeAndValue(int16Type, CShort(-2)),
                    New TypeAndValue(int32Type, CInt(-3)),
                    New TypeAndValue(int64Type, CLng(-4)),
                    New TypeAndValue(sbyteType, CSByte(5)),
                    New TypeAndValue(int16Type, CShort(6)),
                    New TypeAndValue(int32Type, CInt(7)),
                    New TypeAndValue(int64Type, CLng(8)),
                    New TypeAndValue(doubleType, CDbl(-9)),
                    New TypeAndValue(singleType, CSng(-10)),
                    New TypeAndValue(decimalType, CDec(-11)),
                    New TypeAndValue(doubleType, CDbl(12)),
                    New TypeAndValue(singleType, CSng(13)),
                    New TypeAndValue(decimalType, CDec(14)),
                    New TypeAndValue(byteType, CByte(15)),
                    New TypeAndValue(uint16Type, CUShort(16)),
                    New TypeAndValue(uint32Type, CUInt(17)),
                    New TypeAndValue(uint64Type, CULng(18)),
                    New TypeAndValue(decimalType, CDec(-11.3)),
                    New TypeAndValue(doubleType, CDbl(&HF000000000000000UL)),
                    New TypeAndValue(doubleType, CDbl(&H70000000000000F0L)),
                    New TypeAndValue(typeCodeType, Int32.MinValue),
                    New TypeAndValue(typeCodeType, Int32.MaxValue),
                    New TypeAndValue(typeCodeType, CInt(-3)),
                    New TypeAndValue(typeCodeType, CInt(7))
                    }


            Dim resultValue2 As ConstantValue
            Dim integerOverflow2 As Boolean

            For Each mv In nonZeroValues

                Dim v = ConstantValue.Create(mv.Value, mv.Type.GetConstantValueTypeDiscriminator())

                Assert.Equal(v.Discriminator, mv.Type.GetConstantValueTypeDiscriminator())

                literal = New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), v, mv.Type)
                constant = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing),
                                               New BoundLiteral(DirectCast(dummyTree.GetRoot(Nothing), VisualBasicSyntaxNode), ConstantValue.Null, Nothing),
                                               ConversionKind.Widening, True, True, v, mv.Type, Nothing)

                For Each numericType In numericTypes

                    Dim typeConv = ClassifyConversion(mv.Type, numericType)

                    Dim conv = ClassifyConversion(literal, numericType, methodBodyBinder)
                    Dim conv2 = ClassifyConversion(constant, numericType, methodBodyBinder)

                    Assert.Equal(conv, conv2)

                    resultValue = Conversions.TryFoldConstantConversion(literal, numericType, integerOverflow)
                    resultValue2 = Conversions.TryFoldConstantConversion(constant, numericType, integerOverflow2)

                    Assert.Equal(resultValue Is Nothing, resultValue2 Is Nothing)
                    Assert.Equal(integerOverflow, integerOverflow2)

                    If resultValue IsNot Nothing Then
                        Assert.Equal(resultValue2, resultValue)

                        If Not resultValue.IsBad Then
                            Assert.Equal(numericType.GetConstantValueTypeDiscriminator(), resultValue.Discriminator)
                        End If
                    End If

                    Dim resultValueAsObject As Object = Nothing
                    Dim overflow As Boolean = False

                    Try
                        resultValueAsObject = CheckedConvert(v.Value, numericType)
                    Catch ex As OverflowException
                        overflow = True
                    End Try

                    If Not overflow Then
                        If Conversions.IsIdentityConversion(typeConv) Then
                            Assert.True(Conversions.IsIdentityConversion(conv))
                        ElseIf Conversions.IsNarrowingConversion(typeConv) Then
                            If mv.Type Is doubleType AndAlso numericType Is singleType Then
                                Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, conv)
                            ElseIf integralTypes.Contains(mv.Type) AndAlso numericType.IsEnumType() Then
                                Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, conv)
                            ElseIf integralTypes.Contains(mv.Type) AndAlso integralTypes.Contains(numericType) AndAlso Not mv.Type.IsEnumType() AndAlso Not numericType.IsEnumType() Then
                                Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, conv)
                            Else
                                Assert.Equal(typeConv, conv)
                            End If
                        ElseIf mv.Type.IsEnumType() Then
                            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, conv)
                        Else
                            Assert.Equal(ConversionKind.WideningNumeric, conv)
                        End If

                        Assert.NotNull(resultValue)
                        Assert.False(integerOverflow)
                        Assert.False(resultValue.IsBad)
                        Assert.Equal(resultValueAsObject, resultValue.Value)

                        If mv.Type Is doubleType AndAlso numericType Is singleType Then
                            If v.DoubleValue = Double.MinValue Then
                                Dim min As Single = Double.MinValue
                                Assert.True(Single.IsNegativeInfinity(min))
                                Assert.Equal(resultValue.SingleValue, min)
                            ElseIf v.DoubleValue = Double.MaxValue Then
                                Dim max As Single = Double.MaxValue

                                Assert.Equal(Double.MaxValue, v.DoubleValue)
                                Assert.True(Single.IsPositiveInfinity(max))
                                Assert.Equal(resultValue.SingleValue, max)
                            End If
                        End If

                    ElseIf Not integralTypes.Contains(mv.Type) OrElse Not integralTypes.Contains(numericType) Then
                        'Assert.Equal(typeConv, conv)

                        If integralTypes.Contains(numericType) Then

                            Assert.NotNull(resultValue)

                            If resultValue.IsBad Then
                                Assert.False(integerOverflow)
                                Assert.Equal(ConversionKind.FailedDueToNumericOverflow, conv)
                            Else
                                Assert.True(integerOverflow)
                                Assert.Equal(typeConv, conv)

                                Dim intermediate As Object

                                If unsignedTypes.Contains(numericType) Then
                                    intermediate = Convert.ToUInt64(mv.Value)
                                Else
                                    intermediate = Convert.ToInt64(mv.Value)
                                End If

                                Dim gotException As Boolean
                                Try
                                    gotException = False
                                    CheckedConvert(intermediate, numericType) ' Should get an overflow
                                Catch x As Exception
                                    gotException = True
                                End Try

                                Assert.True(gotException)

                                Assert.Equal(UncheckedConvert(intermediate, numericType), resultValue.Value)
                            End If
                        Else
                            Assert.NotNull(resultValue)
                            Assert.False(integerOverflow)
                            Assert.True(resultValue.IsBad)
                            Assert.Equal(ConversionKind.FailedDueToNumericOverflow, conv)
                        End If
                    Else
                        ' An integer overflow case
                        If numericType.IsEnumType() OrElse mv.Type.IsEnumType() Then
                            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, conv)
                        Else
                            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesNarrowingFromNumericConstant, conv)
                        End If

                        Assert.NotNull(resultValue)
                        Assert.True(integerOverflow)
                        Assert.False(resultValue.IsBad)

                        Assert.Equal(UncheckedConvert(v.Value, numericType), resultValue.Value)
                    End If

                    Dim nullableType2 = nullableType.Construct(numericType)
                    Dim zero = New BoundConversion(dummyTree.GetVisualBasicRoot(Nothing),
                                                   New BoundLiteral(dummyTree.GetVisualBasicRoot(Nothing), ConstantValue.Default(ConstantValueTypeDiscriminator.Int32), int32Type),
                                                   ConversionKind.Widening, True, True, ConstantValue.Default(mv.Type.GetConstantValueTypeDiscriminator()), mv.Type, Nothing)

                    conv = ClassifyConversion(literal, numericType, methodBodyBinder)

                    If (conv And ConversionKind.FailedDueToNumericOverflowMask) = 0 Then
                        conv = ClassifyConversion(mv.Type, nullableType2) Or
                            (ClassifyConversion(zero, nullableType2, methodBodyBinder) And ConversionKind.InvolvesNarrowingFromNumericConstant)
                    End If

                    Assert.Equal(conv, ClassifyConversion(literal, nullableType2, methodBodyBinder))
                    Assert.Equal(conv, ClassifyConversion(constant, nullableType2, methodBodyBinder))

                    resultValue = Conversions.TryFoldConstantConversion(literal, nullableType2, integerOverflow)
                    Assert.Null(resultValue)
                    Assert.False(integerOverflow)

                    resultValue = Conversions.TryFoldConstantConversion(constant, nullableType2, integerOverflow)
                    Assert.Null(resultValue)
                    Assert.False(integerOverflow)
                Next

            Next


        End Sub

        Private Function CheckedConvert(value As Object, type As TypeSymbol) As Object
            type = type.GetEnumUnderlyingTypeOrSelf()

            Dim c = CType(value, IConvertible)
            Select Case type.SpecialType
                Case System_Byte : Return c.ToByte(Nothing)
                Case System_SByte : Return c.ToSByte(Nothing)
                Case System_Int16 : Return c.ToInt16(Nothing)
                Case System_UInt16 : Return c.ToUInt16(Nothing)
                Case System_Int32 : Return c.ToInt32(Nothing)
                Case System_UInt32 : Return c.ToUInt32(Nothing)
                Case System_Int64 : Return c.ToInt64(Nothing)
                Case System_UInt64 : Return c.ToUInt64(Nothing)
                Case System_Single : Return c.ToSingle(Nothing)
                Case System_Double : Return c.ToDouble(Nothing)
                Case System_Decimal : Return c.ToDecimal(Nothing)
                Case Else
                    Throw New NotSupportedException()
            End Select
        End Function

        Private Function UncheckedConvert(value As Object, type As TypeSymbol) As Object

            type = type.GetEnumUnderlyingTypeOrSelf()

            Select Case System.Type.GetTypeCode(value.GetType())
                Case TypeCode.Byte, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64
                    Dim val As UInt64 = Convert.ToUInt64(value)

                    Select Case type.SpecialType
                        Case System_Byte : Return UncheckedCByte(UncheckedCLng(val))
                        Case System_SByte : Return UncheckedCSByte(UncheckedCLng(val))
                        Case System_Int16 : Return UncheckedCShort(val)
                        Case System_UInt16 : Return UncheckedCUShort(UncheckedCLng(val))
                        Case System_Int32 : Return UncheckedCInt(val)
                        Case System_UInt32 : Return UncheckedCUInt(val)
                        Case System_Int64 : Return UncheckedCLng(val)
                        Case System_UInt64 : Return UncheckedCULng(val)
                        Case Else
                            Throw New NotSupportedException()
                    End Select

                Case TypeCode.SByte, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64
                    Dim val As Int64 = Convert.ToInt64(value)

                    Select Case type.SpecialType
                        Case System_Byte : Return UncheckedCByte(val)
                        Case System_SByte : Return UncheckedCSByte(val)
                        Case System_Int16 : Return UncheckedCShort(val)
                        Case System_UInt16 : Return UncheckedCUShort(val)
                        Case System_Int32 : Return UncheckedCInt(val)
                        Case System_UInt32 : Return UncheckedCUInt(val)
                        Case System_Int64 : Return UncheckedCLng(val)
                        Case System_UInt64 : Return UncheckedCULng(val)
                        Case Else
                            Throw New NotSupportedException()
                    End Select

                Case Else
                    Throw New NotSupportedException()
            End Select

            Select Case type.SpecialType
                Case System_Byte : Return CByte(value)
                Case System_SByte : Return CSByte(value)
                Case System_Int16 : Return CShort(value)
                Case System_UInt16 : Return CUShort(value)
                Case System_Int32 : Return CInt(value)
                Case System_UInt32 : Return CUInt(value)
                Case System_Int64 : Return CLng(value)
                Case System_UInt64 : Return CULng(value)
                Case System_Single : Return CSng(value)
                Case System_Double : Return CDbl(value)
                Case System_Decimal : Return CDec(value)
                Case Else
                    Throw New NotSupportedException()
            End Select
        End Function

        Friend Structure TypeAndValue
            Public ReadOnly Type As TypeSymbol
            Public ReadOnly Value As Object

            Public Sub New(type As TypeSymbol, value As Object)
                Me.Type = type
                Me.Value = value
            End Sub
        End Structure

        <Fact()>
        Public Sub PredefinedNotBuiltIn()

            ' Tests are based on the source code used to compile VBConversions.dll, VBConversions.vb is
            ' checked in next to the DLL.

            Dim vbConversionsRef = TestReferences.SymbolsTests.VBConversions
            Dim modifiersRef = TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll

            Dim c1 = VisualBasicCompilation.Create("Test", references:={TestReferences.NetFx.v4_0_21006.mscorlib, vbConversionsRef, modifiersRef})

            Dim asmVBConversions = c1.GetReferencedAssemblySymbol(vbConversionsRef)
            Dim asmModifiers = c1.GetReferencedAssemblySymbol(modifiersRef)

            Dim test = asmVBConversions.Modules(0).GlobalNamespace.GetTypeMembers("Test").Single()

            '--------------- Identity
            Dim m1 = DirectCast(test.GetMembers("M1").Single(), MethodSymbol)
            Dim m1p = m1.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(a), m1p(b))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(a), m1p(c))) 'error BC30311: Value of type 'Class2' cannot be converted to 'Class1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(a), m1p(d))) 'error BC30311: Value of type '1-dimensional array of Class1' cannot be converted to 'Class1'. 
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(d), m1p(e))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(d), m1p(f))) 'error BC30332: Value of type '1-dimensional array of Class2' cannot be converted to '1-dimensional array of Class1' because 'Class2' is not derived from 'Class1'. 
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(g), m1p(h))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(g), m1p(i))) 'error BC30311: Value of type 'Class2.Class3(Of Byte)' cannot be converted to 'Class2.Class3(Of Integer)'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(g), m1p(j))) 'error BC30311: Value of type 'Class4(Of Integer)' cannot be converted to 'Class2.Class3(Of Integer)'.
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(j), m1p(k))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(j), m1p(l))) 'error BC30311: Value of type 'Class4(Of Byte)' cannot be converted to 'Class4(Of Integer)'. 
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(m), m1p(n))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(m), m1p(o))) 'error BC30311: Value of type 'Class4(Of Byte).Class5(Of Integer)' cannot be converted to 'Class4(Of Integer).Class5(Of Integer)'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(m), m1p(p))) 'error BC30311: Value of type 'Class4(Of Integer).Class5(Of Byte)' cannot be converted to 'Class4(Of Integer).Class5(Of Integer)'. 
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(q), m1p(r))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(q), m1p(s))) 'error BC30311: Value of type 'Class4(Of Byte).Class6' cannot be converted to 'Class4(Of Integer).Class6'.
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m1p(t), m1p(u))))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(t), m1p(v))) 'error BC30311: Value of type 'Class4(Of Byte).Class6.Class7(Of Integer)' cannot be converted to 'Class4(Of Integer).Class6.Class7(Of Integer)'. 
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m1p(t), m1p(w))) 'error BC30311: Value of type 'Class4(Of Integer).Class6.Class7(Of Byte)' cannot be converted to 'Class4(Of Integer).Class6.Class7(Of Integer)'. 

            Dim modifiers = asmModifiers.Modules(0).GlobalNamespace.GetTypeMembers("Modifiers").Single()
            Dim modifiedArrayInt32 = modifiers.GetMembers("F5").OfType(Of MethodSymbol)().Single().Parameters(0).Type
            Dim arrayInt32 = c1.CreateArrayTypeSymbol(c1.GetSpecialType(System_Int32))

            Assert.NotEqual(modifiedArrayInt32, arrayInt32)
            Assert.NotEqual(arrayInt32, modifiedArrayInt32)
            Assert.True(arrayInt32.IsSameTypeIgnoringAll(modifiedArrayInt32))
            Assert.True(modifiedArrayInt32.IsSameTypeIgnoringAll(arrayInt32))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(arrayInt32, modifiedArrayInt32)))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(modifiedArrayInt32, arrayInt32)))

            Dim enumerable = c1.GetSpecialType(System_Collections_Generic_IEnumerable_T)
            Dim enumerableOfModifiedArrayInt32 = enumerable.Construct(modifiedArrayInt32)
            Dim enumerableOfArrayInt32 = enumerable.Construct(arrayInt32)

            Assert.NotEqual(enumerableOfModifiedArrayInt32, enumerableOfArrayInt32)
            Assert.NotEqual(enumerableOfArrayInt32, enumerableOfModifiedArrayInt32)
            Assert.True(enumerableOfArrayInt32.IsSameTypeIgnoringAll(enumerableOfModifiedArrayInt32))
            Assert.True(enumerableOfModifiedArrayInt32.IsSameTypeIgnoringAll(enumerableOfArrayInt32))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(enumerableOfArrayInt32, enumerableOfModifiedArrayInt32)))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(enumerableOfModifiedArrayInt32, enumerableOfArrayInt32)))


            '--------------- Numeric
            Dim m2 = DirectCast(test.GetMembers("M2").Single(), MethodSymbol)
            Dim m2p = m2.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m2p(a), m2p(b))))
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(d))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum3' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(f))) 'error BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(g))) 'error BC30512: Option Strict On disallows implicit conversions from 'Short' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(a), m2p(h))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum4' to 'Enum1'.
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(e), m2p(a)))
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(e), m2p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Integer'.
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(e), m2p(d)))
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(f), m2p(a)))
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(f), m2p(c)))
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(f), m2p(d)))
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(g), m2p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'Short'.
            Assert.Equal(ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(g), m2p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum2' to 'Short'.
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m2p(g), m2p(d)))

            '--------------- Reference
            Dim m3 = DirectCast(test.GetMembers("M3").Single(), MethodSymbol)
            Dim m3p = m3.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m3p(a), m3p(a))))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(a), m3p(d)))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m3p(b), m3p(b))))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(b), m3p(c)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(b), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(c), m3p(d)))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(d), m3p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Class10'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(c), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Class9'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(d), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Class10'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(d), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Class10'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(c), m3p(e))) 'error BC30311: Value of type 'Class11' cannot be converted to 'Class9'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(e), m3p(c))) 'error BC30311: Value of type 'Class9' cannot be converted to 'Class11'.
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(a), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(f), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(a), m3p(h)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(f), m3p(h)))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to '1-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(f))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Array' to '1-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(h), m3p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to '2-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(h), m3p(f))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Array' to '2-dimensional array of Integer'.
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(i), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(j), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(k), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(l), m3p(c)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(l), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(m), m3p(b)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(m), m3p(c)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(m), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(n), m3p(d)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(p), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(p), m3p(h)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(q), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(r), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(s), m3p(g)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(v), m3p(u)))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m3p(i), m3p(i))))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(i), m3p(j)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(i), m3p(k)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(i), m3p(o)))
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(n), m3p(o)))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(i), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(i), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(i), m3p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class11' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(j), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface2'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(j), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface2'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(k), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface3'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(k), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface3'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(l), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface4'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(n), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface6'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(n), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface6'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(o), m3p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(o), m3p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(o), m3p(d))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class10' to 'Interface7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(q), m3p(h))) 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.IList(Of Integer)'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(r), m3p(h))) 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.ICollection(Of Integer)'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(s), m3p(h))) 'error BC30311: Value of type '2-dimensional array of Integer' cannot be converted to 'System.Collections.Generic.IEnumerable(Of Integer)'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(t), m3p(g))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to 'System.Collections.Generic.IList(Of Long)'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(w), m3p(u))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'System.Collections.Generic.IList(Of Class11)'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(i), m3p(l))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface4' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingReference Or ConversionKind.DelegateRelaxationLevelNarrowing, ClassifyPredefinedAssignment(m3p(o), m3p(x))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Action' to 'Interface7'.
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment(m3p(a), m3p(o)))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(x), m3p(o))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'System.Action'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(e), m3p(o))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'Class11'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(g), m3p(o))) 'error BC30311: Value of type 'Interface7' cannot be converted to '1-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(h), m3p(o))) 'error BC30311: Value of type 'Interface7' cannot be converted to '2-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(u), m3p(o))) 'error BC30311: Value of type 'Interface7' cannot be converted to '1-dimensional array of Class9'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to '1-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(h), m3p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to '2-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(q))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Integer)' to '1-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(h), m3p(q))) 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Integer)' cannot be converted to '2-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(t))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Long)' to '1-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(h), m3p(t))) 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Long)' cannot be converted to '2-dimensional array of Integer'.
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(g), m3p(w))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class11)' to '1-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m3p(h), m3p(w))) 'error BC30311: Value of type 'System.Collections.Generic.IList(Of Class11)' cannot be converted to '2-dimensional array of Integer'.

            Dim [object] = c1.GetSpecialType(System_Object)
            Dim module2 = asmVBConversions.Modules(0).GlobalNamespace.GetTypeMembers("Module2").Single()
            Assert.Equal(ConversionKind.WideningReference, ClassifyPredefinedAssignment([object], module2))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(module2, [object]))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(m3p(i), module2))
            Assert.Equal(ConversionKind.NarrowingReference, ClassifyPredefinedAssignment(module2, m3p(i)))



            ' ------------- Type Parameter
            Dim m6 = DirectCast(test.GetMembers("M6").Single(), MethodSymbol)
            Dim m6p = m6.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m6p(b), m6p(b))))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(a), m6p(b)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(a), m6p(c)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(a), m6p(d)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(b), m6p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(c), m6p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT2'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(d), m6p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT3'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(e), m6p(f)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(e), m6p(h)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(f), m6p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(h), m6p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(f), m6p(g))) 'error BC30311: Value of type 'MT5' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(g), m6p(f))) 'error BC30311: Value of type 'MT4' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(h), m6p(i))) 'error BC30311: Value of type 'MT7' cannot be converted to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(i), m6p(h))) 'error BC30311: Value of type 'MT6' cannot be converted to 'MT7'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(e), m6p(k)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(j), m6p(k)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(j), m6p(f)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(k), m6p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'MT8'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(k), m6p(j))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT8'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(f), m6p(j))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT4'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(l), m6p(k)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(m), m6p(k)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(p), m6p(c)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(k), m6p(l))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class10' to 'MT8'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(k), m6p(m))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'MT8'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(c), m6p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(n), m6p(k))) 'error BC30311: Value of type 'MT8' cannot be converted to 'Class12'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(k), m6p(n))) 'error BC30311: Value of type 'Class12' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(k), m6p(o))) 'error BC30311: Value of type 'MT9' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(o), m6p(k))) 'error BC30311: Value of type 'MT8' cannot be converted to 'MT9'.
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(b), m6p(q)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(b), m6p(r)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(t), m6p(s)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m6p(m), m6p(s)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(q), m6p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT10'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(r), m6p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT11'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(s), m6p(t))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class9' to 'MT13'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(s), m6p(m))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class8' to 'MT13'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(l), m6p(s))) 'error BC30311: Value of type 'MT13' cannot be converted to 'Class10'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(s), m6p(l))) 'error BC30311: Value of type 'Class10' cannot be converted to 'MT13'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(u), m6p(k))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT8' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(k), m6p(u))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT8'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(u), m6p(f))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT4' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(f), m6p(u))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(u), m6p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(b), m6p(u))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(u), m6p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'Interface7'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m6p(c), m6p(u))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface7' to 'MT2'.

            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(v), m6p(q))) 'error BC30311: Value of type 'MT10' cannot be converted to 'MT14'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m6p(q), m6p(v))) 'error BC30311: Value of type 'MT14' cannot be converted to 'MT10'.

            Dim m7 = DirectCast(test.GetMembers("M7").Single(), MethodSymbol)
            Dim m7p = m7.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(p), m7p(a)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(p), m7p(b)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(q), m7p(c)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(q), m7p(d)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(r), m7p(d)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(t), m7p(g)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(v), m7p(j)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(v), m7p(k)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(w), m7p(n)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(x), m7p(i)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(y), m7p(j)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m7p(y), m7p(k)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(a), m7p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(b), m7p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'MT2'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(c), m7p(q))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'MT3'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(d), m7p(q))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(d), m7p(r))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(g), m7p(t))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to 'MT7'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(j), m7p(v))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'MT10'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(k), m7p(v))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class9' to 'MT11'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(n), m7p(w))) 'error BC30512: Option Strict On disallows implicit conversions from 'Structure1' to 'MT14'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(i), m7p(x))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.IEnumerable' to 'MT9'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(y), m7p(i))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT9' to 'System.Collections.Generic.IList(Of Class9)'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(i), m7p(y))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT9'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(j), m7p(y))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT10'
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(k), m7p(y))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT11'
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(y), m7p(z))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT15' to 'System.Collections.Generic.IList(Of Class9)'
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(z), m7p(y))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Collections.Generic.IList(Of Class9)' to 'MT15'
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(n), m7p(o))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT14'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(o), m7p(n))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT14' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(m), m7p(o))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT13'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(o), m7p(m))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT13' to 'Interface1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(d), m7p(o))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m7p(o), m7p(d))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT4' to 'Interface1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(r), m7p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'Enum1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(e), m7p(r))) 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(s), m7p(d))) 'error BC30311: Value of type 'MT4' cannot be converted to 'Enum2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(d), m7p(s))) 'error BC30311: Value of type 'Enum2' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(r), m7p(f))) 'error BC30311: Value of type 'MT6' cannot be converted to 'Enum1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(f), m7p(r))) 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(t), m7p(h))) 'error BC30311: Value of type 'MT8' cannot be converted to '1-dimensional array of Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(h), m7p(t))) 'error BC30311: Value of type '1-dimensional array of Integer' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(v), m7p(i))) 'error BC30311: Value of type 'MT9' cannot be converted to '1-dimensional array of Class9'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(i), m7p(v))) 'error BC30311: Value of type '1-dimensional array of Class9' cannot be converted to 'MT9'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(a), m7p(b))) 'error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(b), m7p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(g), m7p(h))) 'error BC30311: Value of type 'MT8' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(h), m7p(g))) 'error BC30311: Value of type 'MT7' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(g), m7p(l))) 'error BC30311: Value of type 'MT12' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(l), m7p(g))) 'error BC30311: Value of type 'MT7' cannot be converted to 'MT12'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(c), m7p(d))) 'error BC30311: Value of type 'MT4' cannot be converted to 'MT3'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(d), m7p(c))) 'error BC30311: Value of type 'MT3' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(i), m7p(j))) 'error BC30311: Value of type 'MT10' cannot be converted to 'MT9'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(j), m7p(i))) 'error BC30311: Value of type 'MT9' cannot be converted to 'MT10'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(a), m7p(n))) 'error BC30311: Value of type 'MT14' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(n), m7p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT14'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(d), m7p(f))) 'error BC30311: Value of type 'MT6' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m7p(f), m7p(d))) 'error BC30311: Value of type 'MT4' cannot be converted to 'MT6'.

            Dim m8 = DirectCast(test.GetMembers("M8").Single(), MethodSymbol)
            Dim m8p = m8.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m8p(a), m8p(a))))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m8p(a), m8p(d)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m8p(b), m8p(f)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m8p(a), m8p(c)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m8p(b), m8p(e)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m8p(g), m8p(h)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m8p(c), m8p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT3'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m8p(d), m8p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT4'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m8p(e), m8p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'MT5'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m8p(f), m8p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT2' to 'MT6'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m8p(h), m8p(g))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT7' to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(a), m8p(b))) 'error BC30311: Value of type 'MT2' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(b), m8p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(b), m8p(d))) 'error BC30311: Value of type 'MT4' cannot be converted to 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(d), m8p(b))) 'error BC30311: Value of type 'MT2' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(a), m8p(g))) 'error BC30311: Value of type 'MT7' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m8p(g), m8p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT7'.


            Dim m9 = DirectCast(test.GetMembers("M9").Single(), MethodSymbol)
            Dim m9p = m9.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(a), m9p(b)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(j), m9p(a)))
            Assert.Equal(ConversionKind.WideningTypeParameter Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m9p(j), m9p(e)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(l), m9p(e)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(m), m9p(n)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(p), m9p(q)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(s), m9p(u)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(t), m9p(u)))
            Assert.Equal(ConversionKind.WideningTypeParameter, ClassifyPredefinedAssignment(m9p(s), m9p(v)))
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(b), m9p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT1' to 'MT2'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(a), m9p(j))) 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'MT1'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m9p(e), m9p(j))) 'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'MT5'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(e), m9p(l))) 'error BC30512: Option Strict On disallows implicit conversions from 'Enum1' to 'MT5'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(n), m9p(m))) 'error BC30512: Option Strict On disallows implicit conversions from 'Structure1' to 'MT10'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(q), m9p(p))) 'error BC30512: Option Strict On disallows implicit conversions from 'Class1' to 'MT12'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(u), m9p(s))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT15'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(u), m9p(t))) 'error BC30512: Option Strict On disallows implicit conversions from 'MT14' to 'MT15'.
            Assert.Equal(ConversionKind.NarrowingTypeParameter, ClassifyPredefinedAssignment(m9p(v), m9p(s))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'MT16'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(e), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(e), m9p(g))) 'error BC30311: Value of type 'MT7' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(g), m9p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'MT7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(l))) 'error BC30311: Value of type 'Enum1' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(l), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'Enum1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(i))) 'error BC30311: Value of type 'MT9' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(c))) 'error BC30311: Value of type 'MT3' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(c), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT3'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(d))) 'error BC30311: Value of type 'MT4' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(d), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(f))) 'error BC30311: Value of type 'MT6' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(f), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(h))) 'error BC30311: Value of type 'MT8' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(h), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(e), m9p(f))) 'error BC30311: Value of type 'MT6' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(f), m9p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(e), m9p(h))) 'error BC30311: Value of type 'MT8' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(h), m9p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(a), m9p(k))) 'error BC30311: Value of type 'UInteger' cannot be converted to 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(k), m9p(a))) 'error BC30311: Value of type 'MT1' cannot be converted to 'UInteger'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(e), m9p(k))) 'error BC30311: Value of type 'UInteger' cannot be converted to 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(k), m9p(e))) 'error BC30311: Value of type 'MT5' cannot be converted to 'UInteger'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(n), m9p(o))) 'error BC30311: Value of type 'MT11' cannot be converted to 'MT10'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(o), m9p(n))) 'error BC30311: Value of type 'MT10' cannot be converted to 'MT11'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(q), m9p(r))) 'error BC30311: Value of type 'MT13' cannot be converted to 'MT12'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(r), m9p(q))) 'error BC30311: Value of type 'MT12' cannot be converted to 'MT13'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(v), m9p(w))) 'error BC30311: Value of type 'MT17' cannot be converted to 'MT16'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m9p(w), m9p(v))) 'error BC30311: Value of type 'MT16' cannot be converted to 'MT17'.

            ' ------------- Array conversions
            Dim m4 = DirectCast(test.GetMembers("M4").Single(), MethodSymbol)
            Dim m4p = m4.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m4p(a), m4p(a))))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m4p(l), m4p(l))))
            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m4p(n), m4p(n))))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(a), m4p(d)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(b), m4p(f)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(i), m4p(j)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(i), m4p(k)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(l), m4p(m)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(n), m4p(o)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(p), m4p(i)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(x), m4p(i)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m4p(x), m4p(w)))
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(a), m4p(c))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT3' to '1-dimensional array of MT1'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(c), m4p(a))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT3'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(d), m4p(a))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT4'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(b), m4p(e))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT2'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(e), m4p(b))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT2' to '1-dimensional array of MT5'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(f), m4p(b))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT2' to '1-dimensional array of MT6'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(g), m4p(h))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT8' to '1-dimensional array of MT7'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(h), m4p(g))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT7' to '1-dimensional array of MT8'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(j), m4p(i))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class8' to '1-dimensional array of Class9'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(k), m4p(i))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class8' to '1-dimensional array of Class11'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(m), m4p(l))) 'error BC30512: Option Strict On disallows implicit conversions from '2-dimensional array of Class8' to '2-dimensional array of Class9'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(o), m4p(n))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of 1-dimensional array of Class8' to '1-dimensional array of 1-dimensional array of Class9'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(i), m4p(p))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Interface5' to '1-dimensional array of Class8'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(i), m4p(x))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Object' to '1-dimensional array of Class8'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m4p(w), m4p(x))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Object' to '1-dimensional array of System.ValueType'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(a), m4p(b))) 'error BC30332: Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT1' because 'MT2' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(b), m4p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT2' because 'MT1' is not derived from 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(b), m4p(d))) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of MT2' because 'MT4' is not derived from 'MT2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(d), m4p(b))) 'error BC30332: Value of type '1-dimensional array of MT2' cannot be converted to '1-dimensional array of MT4' because 'MT2' is not derived from 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(a), m4p(g))) 'error BC30332: Value of type '1-dimensional array of MT7' cannot be converted to '1-dimensional array of MT1' because 'MT7' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(g), m4p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT7' because 'MT1' is not derived from 'MT7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(j), m4p(k))) 'error BC30332: Value of type '1-dimensional array of Class11' cannot be converted to '1-dimensional array of Class9' because 'Class11' is not derived from 'Class9'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(i), m4p(l))) 'error BC30414: Value of type '2-dimensional array of Class8' cannot be converted to '1-dimensional array of Class8' because the array types have different numbers of dimensions.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(l), m4p(i))) 'error BC30414: Value of type '1-dimensional array of Class8' cannot be converted to '2-dimensional array of Class8' because the array types have different numbers of dimensions.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(l), m4p(n))) 'error BC30332: Value of type '1-dimensional array of 1-dimensional array of Class8' cannot be converted to '2-dimensional array of Class8' because '1-dimensional array of Class8' is not derived from 'Class8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(n), m4p(l))) 'error BC30332: Value of type '2-dimensional array of Class8' cannot be converted to '1-dimensional array of 1-dimensional array of Class8' because 'Class8' is not derived from '1-dimensional array of Class8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(p), m4p(q))) 'error BC30332: Value of type '1-dimensional array of Structure1' cannot be converted to '1-dimensional array of Interface5' because 'Structure1' is not derived from 'Interface5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(q), m4p(p))) 'error BC30332: Value of type '1-dimensional array of Interface5' cannot be converted to '1-dimensional array of Structure1' because 'Interface5' is not derived from 'Structure1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(q), m4p(w))) 'error BC30332: Value of type '1-dimensional array of System.ValueType' cannot be converted to '1-dimensional array of Structure1' because 'System.ValueType' is not derived from 'Structure1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(w), m4p(q))) 'error BC30333: Value of type '1-dimensional array of Structure1' cannot be converted to '1-dimensional array of System.ValueType' because 'Structure1' is not a reference type.
            Assert.Equal(ConversionKind.WideningArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(r), m4p(t)))
            Assert.Equal(ConversionKind.WideningArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(s), m4p(u)))
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(t), m4p(r))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of Enum1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(u), m4p(s))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Long' to '1-dimensional array of Enum2'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(t), m4p(v))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum4' to '1-dimensional array of Enum1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m4p(v), m4p(t))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of Enum4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(r), m4p(s))) 'error BC30332: Value of type '1-dimensional array of Long' cannot be converted to '1-dimensional array of Integer' because 'Long' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(s), m4p(r))) 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of Long' because 'Integer' is not derived from 'Long'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(r), m4p(u))) 'error BC30332: Value of type '1-dimensional array of Enum2' cannot be converted to '1-dimensional array of Integer' because 'Enum2' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(u), m4p(r))) 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of Enum2' because 'Integer' is not derived from 'Enum2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(t), m4p(u))) 'error BC30332: Value of type '1-dimensional array of Enum2' cannot be converted to '1-dimensional array of Enum1' because 'Enum2' is not derived from 'Enum1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m4p(u), m4p(t))) 'error BC30332: Value of type '1-dimensional array of Enum1' cannot be converted to '1-dimensional array of Enum2' because 'Enum1' is not derived from 'Enum2'.


            Dim m5 = DirectCast(test.GetMembers("M5").Single(), MethodSymbol)
            Dim m5p = m5.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m5p(a), m5p(b)))
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(b), m5p(a))) ' error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT2'.

            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m5p(j), m5p(a)))
            Assert.Equal(ConversionKind.WideningArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(j), m5p(e)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m5p(l), m5p(e)))
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(a), m5p(e))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(e), m5p(a))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of MT5'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(e), m5p(g))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT7' to '1-dimensional array of MT5'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(g), m5p(e))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT5' to '1-dimensional array of MT7'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(a), m5p(j))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of MT1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(a), m5p(l))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of MT1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(l), m5p(a))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT1' to '1-dimensional array of Enum1'.
            Assert.Equal(ConversionKind.NarrowingArray Or ConversionKind.InvolvesEnumTypeConversions, ClassifyPredefinedAssignment(m5p(e), m5p(j))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Integer' to '1-dimensional array of MT5'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(e), m5p(l))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Enum1' to '1-dimensional array of MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(i))) 'error BC30332: Value of type '1-dimensional array of MT9' cannot be converted to '1-dimensional array of MT1' because 'MT9' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(c))) 'error BC30332: Value of type '1-dimensional array of MT3' cannot be converted to '1-dimensional array of MT1' because 'MT3' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(c), m5p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT3' because 'MT1' is not derived from 'MT3'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(d))) 'error BC30332: Value of type '1-dimensional array of MT4' cannot be converted to '1-dimensional array of MT1' because 'MT4' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(d), m5p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT4' because 'MT1' is not derived from 'MT4'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(f))) 'error BC30332: Value of type '1-dimensional array of MT6' cannot be converted to '1-dimensional array of MT1' because 'MT6' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(f), m5p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT6' because 'MT1' is not derived from 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(h))) 'error BC30332: Value of type '1-dimensional array of MT8' cannot be converted to '1-dimensional array of MT1' because 'MT8' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(h), m5p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of MT8' because 'MT1' is not derived from 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(e), m5p(f))) 'error BC30332: Value of type '1-dimensional array of MT6' cannot be converted to '1-dimensional array of MT5' because 'MT6' is not derived from 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(f), m5p(e))) 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of MT6' because 'MT5' is not derived from 'MT6'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(e), m5p(h))) 'error BC30332: Value of type '1-dimensional array of MT8' cannot be converted to '1-dimensional array of MT5' because 'MT8' is not derived from 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(h), m5p(e))) 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of MT8' because 'MT5' is not derived from 'MT8'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(a), m5p(k))) 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of MT1' because 'UInteger' is not derived from 'MT1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(k), m5p(a))) 'error BC30332: Value of type '1-dimensional array of MT1' cannot be converted to '1-dimensional array of UInteger' because 'MT1' is not derived from 'UInteger'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(e), m5p(k))) 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of MT5' because 'UInteger' is not derived from 'MT5'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(k), m5p(e))) 'error BC30332: Value of type '1-dimensional array of MT5' cannot be converted to '1-dimensional array of UInteger' because 'MT5' is not derived from 'UInteger'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(j), m5p(k))) 'error BC30332: Value of type '1-dimensional array of UInteger' cannot be converted to '1-dimensional array of Integer' because 'UInteger' is not derived from 'Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(k), m5p(j))) 'error BC30332: Value of type '1-dimensional array of Integer' cannot be converted to '1-dimensional array of UInteger' because 'Integer' is not derived from 'UInteger'.

            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m5p(m), m5p(n)))
            Assert.Equal(ConversionKind.WideningArray, ClassifyPredefinedAssignment(m5p(p), m5p(q)))
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(n), m5p(m))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Structure1' to '1-dimensional array of MT10'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(q), m5p(p))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of Class1' to '1-dimensional array of MT12'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(s), m5p(u))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT15' to '1-dimensional array of System.ValueType'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(u), m5p(s))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of System.ValueType' to '1-dimensional array of MT15'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(t), m5p(u))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT15' to '1-dimensional array of MT14'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(u), m5p(t))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT14' to '1-dimensional array of MT15'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(s), m5p(v))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of MT16' to '1-dimensional array of System.ValueType'.
            Assert.Equal(ConversionKind.NarrowingArray, ClassifyPredefinedAssignment(m5p(v), m5p(s))) 'error BC30512: Option Strict On disallows implicit conversions from '1-dimensional array of System.ValueType' to '1-dimensional array of MT16'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(n), m5p(o))) 'error BC30332: Value of type '1-dimensional array of MT11' cannot be converted to '1-dimensional array of MT10' because 'MT11' is not derived from 'MT10'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(o), m5p(n))) 'error BC30332: Value of type '1-dimensional array of MT10' cannot be converted to '1-dimensional array of MT11' because 'MT10' is not derived from 'MT11'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(q), m5p(r))) 'error BC30332: Value of type '1-dimensional array of MT13' cannot be converted to '1-dimensional array of MT12' because 'MT13' is not derived from 'MT12'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(r), m5p(q))) 'error BC30332: Value of type '1-dimensional array of MT12' cannot be converted to '1-dimensional array of MT13' because 'MT12' is not derived from 'MT13'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(v), m5p(w))) 'error BC30332: Value of type '1-dimensional array of MT17' cannot be converted to '1-dimensional array of MT16' because 'MT17' is not derived from 'MT16'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m5p(w), m5p(v))) 'error BC30332: Value of type '1-dimensional array of MT16' cannot be converted to '1-dimensional array of MT17' because 'MT16' is not derived from 'MT17'.


            ' ------------- Value Type 
            Dim void = c1.GetSpecialType(System_Void)
            Dim valueType = c1.GetSpecialType(System_ValueType)

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(void, void)))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment([object], void))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(void, [object]))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(valueType, void))
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(void, valueType))

            Dim m10 = DirectCast(test.GetMembers("M10").Single(), MethodSymbol)
            Dim m10p = m10.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m10p(f), m10p(f))))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(a), m10p(f)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(b), m10p(f)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(a), m10p(h)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(b), m10p(h)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(c), m10p(h)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(d), m10p(f)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m10p(i), m10p(f)))
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(f), m10p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Structure2'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(f), m10p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Structure2'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(h), m10p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(h), m10p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(h), m10p(c))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.Enum' to 'Enum1'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(f), m10p(d))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'Structure2'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m10p(f), m10p(i))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(c), m10p(f))) 'error BC30311: Value of type 'Structure2' cannot be converted to 'System.Enum'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(f), m10p(c))) 'error BC30311: Value of type 'System.Enum' cannot be converted to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(d), m10p(h))) 'error BC30311: Value of type 'Enum1' cannot be converted to 'Interface1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(h), m10p(d))) 'error BC30311: Value of type 'Interface1' cannot be converted to 'Enum1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(e), m10p(f))) 'error BC30311: Value of type 'Structure2' cannot be converted to 'Interface7'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(f), m10p(e))) 'error BC30311: Value of type 'Interface7' cannot be converted to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(f), m10p(g))) 'error BC30311: Value of type 'Structure1' cannot be converted to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(g), m10p(f))) 'error BC30311: Value of type 'Structure2' cannot be converted to 'Structure1'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(f), m10p(h))) 'error BC30311: Value of type 'Enum1' cannot be converted to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m10p(h), m10p(f))) 'error BC30311: Value of type 'Structure2' cannot be converted to 'Enum1'.

            ' ------------ Nullable
            Dim m11 = DirectCast(test.GetMembers("M11").Single(), MethodSymbol)
            Dim m11p = m11.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.True(Conversions.IsIdentityConversion(ClassifyPredefinedAssignment(m11p(d), m11p(d))))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m11p(a), m11p(d)))
            Assert.Equal(ConversionKind.WideningValue, ClassifyPredefinedAssignment(m11p(b), m11p(d)))
            Assert.Equal(ConversionKind.WideningNullable, ClassifyPredefinedAssignment(m11p(d), m11p(c)))
            Assert.Equal(ConversionKind.WideningNullable, ClassifyPredefinedAssignment(m11p(e), m11p(d)))
            Assert.Equal(ConversionKind.WideningNullable, ClassifyPredefinedAssignment(m11p(f), m11p(d)))
            Assert.Equal(ConversionKind.WideningNullable, ClassifyPredefinedAssignment(m11p(i), m11p(h)))
            Assert.Equal(ConversionKind.WideningNullable, ClassifyPredefinedAssignment(m11p(k), m11p(i)))
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m11p(d), m11p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Structure2?'.
            Assert.Equal(ConversionKind.NarrowingValue, ClassifyPredefinedAssignment(m11p(d), m11p(b))) 'error BC30512: Option Strict On disallows implicit conversions from 'System.ValueType' to 'Structure2?'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(c), m11p(d))) 'error BC30512: Option Strict On disallows implicit conversions from 'Structure2?' to 'Structure2'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(d), m11p(e))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface1' to 'Structure2?'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(d), m11p(f))) 'error BC30512: Option Strict On disallows implicit conversions from 'Interface3' to 'Structure2?'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(h), m11p(i))) 'error BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Integer'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(i), m11p(k))) 'error BC30512: Option Strict On disallows implicit conversions from 'Long?' to 'Integer?'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(i), m11p(j))) 'error BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer?'.
            Assert.Equal(ConversionKind.NarrowingNullable, ClassifyPredefinedAssignment(m11p(j), m11p(i))) 'error BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Long'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(c), m11p(i))) 'error BC30311: Value of type 'Integer?' cannot be converted to 'Structure2'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(i), m11p(c))) 'error BC30311: Value of type 'Structure2' cannot be converted to 'Integer?'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(d), m11p(h))) 'error BC30311: Value of type 'Integer' cannot be converted to 'Structure2?'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(h), m11p(d))) 'error BC30311: Value of type 'Structure2?' cannot be converted to 'Integer'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(d), m11p(i))) 'error BC30311: Value of type 'Integer?' cannot be converted to 'Structure2?'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m11p(i), m11p(d))) 'error BC30311: Value of type 'Structure2?' cannot be converted to 'Integer?'.


            ' ------------ String
            Dim m12 = DirectCast(test.GetMembers("M12").Single(), MethodSymbol)
            Dim m12p = m12.Parameters.Select(Function(p) p.Type).ToArray()

            Assert.Equal(ConversionKind.WideningString, ClassifyPredefinedAssignment(m12p(a), m12p(b)))
            Assert.Equal(ConversionKind.WideningString, ClassifyPredefinedAssignment(m12p(a), m12p(c)))
            Assert.Equal(ConversionKind.NarrowingString, ClassifyPredefinedAssignment(m12p(b), m12p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
            Assert.Equal(ConversionKind.NarrowingString, ClassifyPredefinedAssignment(m12p(c), m12p(a))) 'error BC30512: Option Strict On disallows implicit conversions from 'String' to '1-dimensional array of Char'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m12p(b), m12p(c))) 'error BC30311: Value of type '1-dimensional array of Char' cannot be converted to 'Char'.
            Assert.Equal(s_noConversion, ClassifyPredefinedAssignment(m12p(c), m12p(b))) 'error BC30311: Value of type 'Char' cannot be converted to '1-dimensional array of Char'.

        End Sub

        Private Shared Function ClassifyPredefinedAssignment([to] As TypeSymbol, [from] As TypeSymbol) As ConversionKind
            Dim result As ConversionKind = Conversions.ClassifyPredefinedConversion([from], [to], Nothing) And Not ConversionKind.MightSucceedAtRuntime
            Assert.Equal(result, ClassifyConversion([from], [to]))
            Return result
        End Function

        Public Enum Parameters
            a
            b
            c
            d
            e
            f
            g
            h
            i
            j
            k
            l
            m
            n
            o
            p
            q
            r
            s
            t
            u
            v
            w
            x
            y
            z
        End Enum

        <Fact()>
        Public Sub BuiltIn()

            Dim c1 = VisualBasicCompilation.Create("Test", references:={TestReferences.NetFx.v4_0_21006.mscorlib})

            Dim nullable = c1.GetSpecialType(System_Nullable_T)

            Dim types As NamedTypeSymbol() = {
                c1.GetSpecialType(System_Byte),
                c1.GetSpecialType(System_SByte),
                c1.GetSpecialType(System_UInt16),
                c1.GetSpecialType(System_Int16),
                c1.GetSpecialType(System_UInt32),
                c1.GetSpecialType(System_Int32),
                c1.GetSpecialType(System_UInt64),
                c1.GetSpecialType(System_Int64),
                c1.GetSpecialType(System_Decimal),
                c1.GetSpecialType(System_Single),
                c1.GetSpecialType(System_Double),
                c1.GetSpecialType(System_String),
                c1.GetSpecialType(System_Char),
                c1.GetSpecialType(System_Boolean),
                c1.GetSpecialType(System_DateTime),
                c1.GetSpecialType(System_Object),
                nullable.Construct(c1.GetSpecialType(System_Byte)),
                nullable.Construct(c1.GetSpecialType(System_SByte)),
                nullable.Construct(c1.GetSpecialType(System_UInt16)),
                nullable.Construct(c1.GetSpecialType(System_Int16)),
                nullable.Construct(c1.GetSpecialType(System_UInt32)),
                nullable.Construct(c1.GetSpecialType(System_Int32)),
                nullable.Construct(c1.GetSpecialType(System_UInt64)),
                nullable.Construct(c1.GetSpecialType(System_Int64)),
                nullable.Construct(c1.GetSpecialType(System_Decimal)),
                nullable.Construct(c1.GetSpecialType(System_Single)),
                nullable.Construct(c1.GetSpecialType(System_Double)),
                nullable.Construct(c1.GetSpecialType(System_Char)),
                nullable.Construct(c1.GetSpecialType(System_Boolean)),
                nullable.Construct(c1.GetSpecialType(System_DateTime))
                }


            For i As Integer = 0 To types.Length - 1 Step 1
                For j As Integer = 0 To types.Length - 1 Step 1

                    Dim convClass = Conversions.ClassifyPredefinedConversion(types(i), types(j), Nothing)

                    Assert.Equal(convClass, Conversions.ConversionEasyOut.ClassifyPredefinedConversion(types(i), types(j)))
                    Assert.Equal(convClass, ClassifyConversion(types(i), types(j)))

                    If (i = j) Then
                        Assert.True(Conversions.IsIdentityConversion(convClass))
                    Else
                        Dim baseline = HasBuiltInWideningConversions(types(i), types(j))

                        If baseline = s_noConversion Then
                            baseline = HasBuiltInNarrowingConversions(types(i), types(j))
                        End If

                        Assert.Equal(baseline, convClass)
                    End If
                Next
            Next
        End Sub

        Private Function HasBuiltInWideningConversions(from As TypeSymbol, [to] As TypeSymbol) As ConversionKind
            Dim result = HasBuiltInWideningConversions(from.SpecialType, [to].SpecialType)

            If result = s_noConversion Then
                Dim fromIsNullable = from.IsNullableType()
                Dim fromElement = If(fromIsNullable, from.GetNullableUnderlyingType(), Nothing)

                If fromIsNullable AndAlso [to].SpecialType = System_Object Then
                    Return ConversionKind.WideningValue
                End If

                Dim toIsNullable = [to].IsNullableType()
                Dim toElement = If(toIsNullable, [to].GetNullableUnderlyingType(), Nothing)

                'Nullable Value Type conversions
                '•	From a type T? to a type S?, where there is a widening conversion from the type T to the type S.
                If (fromIsNullable AndAlso toIsNullable) Then
                    If (HasBuiltInWideningConversions(fromElement, toElement) And ConversionKind.Widening) <> 0 Then
                        Return ConversionKind.WideningNullable
                    End If
                End If

                If (Not fromIsNullable AndAlso toIsNullable) Then
                    '•	From a type T to the type T?.
                    If from.Equals(toElement) Then
                        Return ConversionKind.WideningNullable
                    End If

                    '•	From a type T to a type S?, where there is a widening conversion from the type T to the type S.
                    If (HasBuiltInWideningConversions(from, toElement) And ConversionKind.Widening) <> 0 Then
                        Return ConversionKind.WideningNullable
                    End If
                End If
            End If

            Return result
        End Function

        Private Function HasBuiltInNarrowingConversions(from As TypeSymbol, [to] As TypeSymbol) As ConversionKind
            Dim result = HasBuiltInNarrowingConversions(from.SpecialType, [to].SpecialType)

            If result = s_noConversion Then
                Dim toIsNullable = [to].IsNullableType()
                Dim toElement = If(toIsNullable, [to].GetNullableUnderlyingType(), Nothing)

                If from.SpecialType = System_Object AndAlso toIsNullable Then
                    Return ConversionKind.NarrowingValue
                End If

                Dim fromIsNullable = from.IsNullableType()
                Dim fromElement = If(fromIsNullable, from.GetNullableUnderlyingType(), Nothing)

                'Nullable Value Type conversions
                If (fromIsNullable AndAlso Not toIsNullable) Then
                    '•	From a type T? to a type T.
                    If fromElement.Equals([to]) Then
                        Return ConversionKind.NarrowingNullable
                    End If

                    '•	From a type S? to a type T, where there is a conversion from the type S to the type T.
                    If HasBuiltInWideningConversions(fromElement, [to]) <> s_noConversion OrElse
                        HasBuiltInNarrowingConversions(fromElement, [to]) <> s_noConversion Then
                        Return ConversionKind.NarrowingNullable
                    End If
                End If

                '•	From a type T? to a type S?, where there is a narrowing conversion from the type T to the type S.
                If (fromIsNullable AndAlso toIsNullable) Then
                    If (HasBuiltInNarrowingConversions(fromElement, toElement) And ConversionKind.Narrowing) <> 0 Then
                        Return ConversionKind.NarrowingNullable
                    End If
                End If

                '•	From a type T to a type S?, where there is a narrowing conversion from the type T to the type S.
                If (Not fromIsNullable AndAlso toIsNullable) Then
                    If (HasBuiltInNarrowingConversions(from, toElement) And ConversionKind.Narrowing) <> 0 Then
                        Return ConversionKind.NarrowingNullable
                    End If
                End If

            End If

            Return result
        End Function

        Private Const s_byte = System_Byte
        Private Const s_SByte = System_SByte
        Private Const s_UShort = System_UInt16
        Private Const s_short = System_Int16
        Private Const s_UInteger = System_UInt32
        Private Const s_integer = System_Int32
        Private Const s_ULong = System_UInt64
        Private Const s_long = System_Int64
        Private Const s_decimal = System_Decimal
        Private Const s_single = System_Single
        Private Const s_double = System_Double
        Private Const s_string = System_String
        Private Const s_char = System_Char
        Private Const s_boolean = System_Boolean
        Private Const s_date = System_DateTime
        Private Const s_object = System_Object

        Private Function HasBuiltInWideningConversions(from As SpecialType, [to] As SpecialType) As ConversionKind
            Select Case CInt(from)
                'Numeric conversions
                '•	From Byte to UShort, Short, UInteger, Integer, ULong, Long, Decimal, Single, or Double.
                Case s_byte
                    Select Case CInt([to])

                        Case s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From SByte to Short, Integer, Long, Decimal, Single, or Double.
                Case s_SByte
                    Select Case CInt([to])

                        Case s_short, s_integer, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From UShort to UInteger, Integer, ULong, Long, Decimal, Single, or Double.
                Case s_UShort
                    Select Case CInt([to])

                        Case s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From Short to Integer, Long, Decimal, Single or Double.
                Case s_short
                    Select Case CInt([to])

                        Case s_integer, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From UInteger to ULong, Long, Decimal, Single, or Double.
                Case s_UInteger
                    Select Case CInt([to])

                        Case s_ULong, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From Integer to Long, Decimal, Single or Double.
                Case s_integer
                    Select Case CInt([to])

                        Case s_long, s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From ULong to Decimal, Single, or Double.
                Case s_ULong
                    Select Case CInt([to])

                        Case s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From Long to Decimal, Single or Double.
                Case s_long
                    Select Case CInt([to])

                        Case s_decimal, s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From Decimal to Single or Double.
                Case s_decimal
                    Select Case CInt([to])

                        Case s_single, s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                '•	From Single to Double.
                Case s_single
                    Select Case CInt([to])

                        Case s_double
                            Return ConversionKind.WideningNumeric
                    End Select

                'Reference conversions
                '•	From a reference type to a base type.
                Case s_string
                    Select Case CInt([to])

                        Case s_object
                            Return ConversionKind.WideningReference
                    End Select

                'String conversions
                '•	From Char to String.
                Case s_char
                    Select Case CInt([to])

                        Case s_string
                            Return ConversionKind.WideningString
                    End Select

            End Select

            Select Case CInt([to])
                'Value Type conversions
                '•	From a value type to a base type.
                Case s_object
                    Select Case CInt([from])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double,
                                s_char, s_boolean, s_date
                            Return ConversionKind.WideningValue
                    End Select

            End Select

            Return s_noConversion
        End Function

        Private Function HasBuiltInNarrowingConversions(from As SpecialType, [to] As SpecialType) As ConversionKind
            Select Case CInt(from)
                'Boolean conversions
                '•	From Boolean to Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Decimal, Single, or Double.
                Case s_boolean
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.NarrowingBoolean
                    End Select

                'Numeric conversions
                '•	From Byte to SByte.
                Case s_byte
                    Select Case CInt([to])

                        Case s_SByte
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From SByte to Byte, UShort, UInteger, or ULong.
                Case s_SByte
                    Select Case CInt([to])

                        Case s_byte, s_UShort, s_UInteger, s_ULong
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From UShort to Byte, SByte, or Short.
                Case s_UShort
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_short
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Short to Byte, SByte, UShort, UInteger, or ULong.
                Case s_short
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_UInteger, s_ULong
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From UInteger to Byte, SByte, UShort, Short, or Integer.
                Case s_UInteger
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_integer
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Integer to Byte, SByte, UShort, Short, UInteger, or ULong.
                Case s_integer
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_ULong
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From ULong to Byte, SByte, UShort, Short, UInteger, Integer, or Long.
                Case s_ULong
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_long
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Long to Byte, SByte, UShort, Short, UInteger, Integer, or ULong.
                Case s_long
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Decimal to Byte, SByte, UShort, Short, UInteger, Integer, ULong, or Long.
                Case s_decimal
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Single to Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, or Decimal.
                Case s_single
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal
                            Return ConversionKind.NarrowingNumeric
                    End Select

                '•	From Double to Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Decimal, or Single.
                Case s_double
                    Select Case CInt([to])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single
                            Return ConversionKind.NarrowingNumeric
                    End Select

                'String conversions
                '•	From String to Char.
                '•	From String to Boolean and from Boolean to String.
                '•	Conversions between String and Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Decimal, Single, or Double.
                '•	From String to Date and from Date to String.
                Case s_string
                    Select Case CInt([to])

                        Case s_char,
                             s_boolean,
                             s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double,
                             s_date
                            Return ConversionKind.NarrowingString
                    End Select

                'VB Runtime Conversions
                Case s_object
                    Select Case CInt([to])
                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double,
                                s_char, s_boolean, s_date
                            Return ConversionKind.NarrowingValue
                        Case s_string
                            Return ConversionKind.NarrowingReference
                    End Select
            End Select

            Select Case CInt([to])
                'Boolean conversions
                '•	From            Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Decimal, Single, or Double to Boolean.
                Case s_boolean
                    Select Case CInt([from])

                        Case s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double
                            Return ConversionKind.NarrowingBoolean
                    End Select

                'String conversions
                '•	From String to Boolean and from Boolean to String.
                '•	Conversions between String and Byte, SByte, UShort, Short, UInteger, Integer, ULong, Long, Decimal, Single, or Double.
                '•	From String to Date and from Date to String.
                Case s_string
                    Select Case CInt([from])

                        Case s_boolean,
                             s_byte, s_SByte, s_UShort, s_short, s_UInteger, s_integer, s_ULong, s_long, s_decimal, s_single, s_double,
                             s_date
                            Return ConversionKind.NarrowingString
                    End Select
            End Select

            Return s_noConversion
        End Function

        <Fact()>
        Public Sub EnumConversions()
            CompileAndVerify(
<compilation name="VBEnumConversions">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Globalization
Imports System.Collections.Generic

Module Module1


    Sub Main()
        Dim BoFalse As Boolean
        Dim BoTrue As Boolean
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim St As String
        Dim Ob As Object
        Dim Tc As TypeCode
        Dim TcAsVT As ValueType

        BoFalse = False
        BoTrue = True
        SB = 1
        By = 2
        Sh = 3
        US = 4
        [In] = 5
        UI = 6
        Lo = 7
        UL = 8
        Si = 10
        [Do] = 11
        De = 9D
        St = "12"
        Ob = 13

        Tc = TypeCode.Decimal
        TcAsVT = Tc

        System.Console.WriteLine("Conversions to enum:")

        PrintResultTc(BoFalse)
        PrintResultTc(BoTrue)
        PrintResultTc(SB)
        PrintResultTc(By)
        PrintResultTc(Sh)
        PrintResultTc(US)
        PrintResultTc([In])
        PrintResultTc(UI)
        PrintResultTc(Lo)
        PrintResultTc(UL)
        PrintResultTc(Si)
        PrintResultTc([Do])
        PrintResultTc(De)
        PrintResultTc(Ob)
        PrintResultTc(St)
        PrintResultTc(TcAsVT)

        System.Console.WriteLine()
        System.Console.WriteLine("Conversions from enum:")
        PrintResultBo(Tc)
        PrintResultSB(Tc)
        PrintResultBy(Tc)
        PrintResultSh(Tc)
        PrintResultUs(Tc)
        PrintResultIn(Tc)
        PrintResultUI(Tc)
        PrintResultLo(Tc)
        PrintResultUL(Tc)
        PrintResultSi(Tc)
        PrintResultDo(Tc)
        PrintResultDe(Tc)
        PrintResultOb(Tc)
        PrintResultSt(Tc)
        PrintResultValueType(Tc)

    End Sub



    Sub PrintResultTc(val As TypeCode)
        System.Console.WriteLine("TypeCode: {0}", val)
    End Sub
    Sub PrintResultBo(val As Boolean)
        System.Console.WriteLine("Boolean: {0}", val)
    End Sub
    Sub PrintResultSB(val As SByte)
        System.Console.WriteLine("SByte: {0}", val)
    End Sub
    Sub PrintResultBy(val As Byte)
        System.Console.WriteLine("Byte: {0}", val)
    End Sub
    Sub PrintResultSh(val As Short)
        System.Console.WriteLine("Short: {0}", val)
    End Sub
    Sub PrintResultUs(val As UShort)
        System.Console.WriteLine("UShort: {0}", val)
    End Sub
    Sub PrintResultIn(val As Integer)
        System.Console.WriteLine("Integer: {0}", val)
    End Sub
    Sub PrintResultUI(val As UInteger)
        System.Console.WriteLine("UInteger: {0}", val)
    End Sub
    Sub PrintResultLo(val As Long)
        System.Console.WriteLine("Long: {0}", val)
    End Sub
    Sub PrintResultUL(val As ULong)
        System.Console.WriteLine("ULong: {0}", val)
    End Sub
    Sub PrintResultDe(val As Decimal)
        System.Console.WriteLine("Decimal: {0}", val)
    End Sub
    Sub PrintResultSi(val As Single)
        System.Console.WriteLine("Single: {0}", val)
    End Sub
    Sub PrintResultDo(val As Double)
        System.Console.WriteLine("Double: {0}", val)
    End Sub
    Sub PrintResultSt(val As String)
        System.Console.WriteLine("String: {0}", val)
    End Sub
    Sub PrintResultOb(val As Object)
        System.Console.WriteLine("Object: {0}", val)
    End Sub
    Sub PrintResultValueType(val As ValueType)
        System.Console.WriteLine("ValueType: {0}", val)
    End Sub
End Module
    </file>
</compilation>,
            expectedOutput:=<![CDATA[
Conversions to enum:
TypeCode: Empty
TypeCode: -1
TypeCode: Object
TypeCode: DBNull
TypeCode: Boolean
TypeCode: Char
TypeCode: SByte
TypeCode: Byte
TypeCode: Int16
TypeCode: UInt16
TypeCode: UInt32
TypeCode: Int64
TypeCode: Int32
TypeCode: Single
TypeCode: UInt64
TypeCode: Decimal

Conversions from enum:
Boolean: True
SByte: 15
Byte: 15
Short: 15
UShort: 15
Integer: 15
UInteger: 15
Long: 15
ULong: 15
Single: 15
Double: 15
Decimal: 15
Object: Decimal
String: 15
ValueType: Decimal    
]]>)
        End Sub

        <Fact()>
        Public Sub ConversionDiagnostic1()

            Dim compilationDef =
<compilation name="VBConversionsDiagnostic1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()

        Dim [In] As Integer

        [In] = Console.WriteLine()
        [In] = CType(Console.WriteLine(), Integer)
        [In] = CType(1, UnknownType)
        [In] = CType(unknownValue, Integer)
        [In] = CType(unknownValue, UnknownType)

        Dim tr As System.TypedReference = Nothing
        Dim ai As System.ArgIterator = Nothing
        Dim ra As System.RuntimeArgumentHandle = Nothing

        Dim Ob As Object

        Ob = tr
        Ob = ai
        Ob = ra
        Ob = CType(tr, Object)
        Ob = CType(ai, Object)
        Ob = CType(ra, Object)

        Dim vt As ValueType

        vt = tr
        vt = ai
        vt = ra
        vt = CType(tr, ValueType)
        vt = CType(ai, ValueType)
        vt = CType(ra, ValueType)

        Dim collection As Microsoft.VisualBasic.Collection = Nothing
        Dim _collection As _Collection = Nothing

        collection = _collection
        _collection = collection
        collection = CType(_collection, Microsoft.VisualBasic.Collection)
        _collection = CType(collection, _Collection)

        Dim Si As Single
        Dim De As Decimal

        [In] = Int64.MaxValue
        [In] = CInt(Int64.MaxValue)
        Si = System.Double.MaxValue
        Si = CSng(System.Double.MaxValue)
        De = System.Double.MaxValue
        De = CDec(System.Double.MaxValue)
        De = 10.0F
        De = CDec(10.0F)

        Dim Da As DateTime = Nothing
        [In] = Da
        [In] = CInt(Da)
        Da = [In]
        Da = CDate([In])

        Dim [Do] As Double = Nothing
        Dim Ch As Char = Nothing

        [Do] = Da
        [Do] = CDbl(Da)
        Da = [Do]
        Da = CDate([Do])

        [In] = Ch
        [In] = CInt(Ch)
        Ch = [In]
        Ch = CChar([In])

        Dim InArray As Integer() = Nothing
        Dim ObArray As Object() = Nothing
        Dim VtArray As ValueType() = Nothing

        ObArray = InArray
        ObArray = CType(InArray, Object())
        VtArray = InArray
        VtArray = CType(InArray, ValueType())

        Dim TC1Array As TestClass1() = Nothing
        Dim TC2Array As TestClass2() = Nothing

        TC1Array = TC2Array
        TC2Array = CType(TC1Array, TestClass2())

        Dim InArray2 As Integer(,) = Nothing

        InArray = InArray2
        InArray2 = CType(InArray, Integer(,))

        Dim TI1Array As TestInterface1() = Nothing

        InArray = TI1Array
        TI1Array = CType(InArray, TestInterface1())
    End Sub

End Module

Interface TestInterface1
End Interface

Interface _Collection
End Interface

Class TestClass1
End Class

Class TestClass2
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.ERR_NarrowingConversionCollection2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_NarrowingConversionCollection2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "10.0F").WithArguments("Single", "Decimal"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC2Array").WithArguments("TestClass2()", "TestClass1()", "TestClass2", "TestClass1"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray2").WithArguments("Integer(*,*)", "Integer()"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TI1Array").WithArguments("TestInterface1()", "Integer()", "TestInterface1", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"))

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC2Array").WithArguments("TestClass2()", "TestClass1()", "TestClass2", "TestClass1"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray2").WithArguments("Integer(*,*)", "Integer()"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TI1Array").WithArguments("TestInterface1()", "Integer()", "TestInterface1", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"))

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_ImplicitConversionSubst1, "_collection").WithArguments("Implicit conversion from '_Collection' to 'Collection'."),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.WRN_ImplicitConversionSubst1, "collection").WithArguments("Implicit conversion from 'Collection' to '_Collection'."),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Int64.MaxValue").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "System.Double.MaxValue").WithArguments("Decimal"),
                Diagnostic(ERRID.WRN_ImplicitConversionSubst1, "10.0F").WithArguments("Implicit conversion from 'Single' to 'Decimal'."),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC2Array").WithArguments("TestClass2()", "TestClass1()", "TestClass2", "TestClass1"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray2").WithArguments("Integer(*,*)", "Integer()"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TI1Array").WithArguments("TestInterface1()", "Integer()", "TestInterface1", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"))
        End Sub

        <Fact()>
        Public Sub DirectCastDiagnostic1()
            Dim compilationDef =
<compilation name="DirectCastDiagnostic1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()

        Dim [In] As Integer

        [In] = DirectCast(Console.WriteLine(), Integer)
        [In] = DirectCast(1, UnknownType)
        [In] = DirectCast(unknownValue, Integer)
        [In] = DirectCast(unknownValue, UnknownType)

        Dim tr As System.TypedReference = Nothing
        Dim ai As System.ArgIterator = Nothing
        Dim ra As System.RuntimeArgumentHandle = Nothing

        Dim Ob As Object

        Ob = DirectCast(tr, Object)
        Ob = DirectCast(ai, Object)
        Ob = DirectCast(ra, Object)

        Dim vt As ValueType

        vt = DirectCast(tr, ValueType)
        vt = DirectCast(ai, ValueType)
        vt = DirectCast(ra, ValueType)

        Dim collection As Microsoft.VisualBasic.Collection = Nothing
        Dim _collection As _Collection = Nothing

        collection = DirectCast(_collection, Microsoft.VisualBasic.Collection)
        _collection = DirectCast(collection, _Collection)

        Dim Si As Single
        Dim De As Decimal

        [In] = DirectCast(Int64.MaxValue, Int32)
        De = DirectCast(System.Double.MaxValue, System.Decimal)

        Dim Da As DateTime = Nothing
        [In] = DirectCast(Da, Int32)
        Da = DirectCast([In], DateTime)

        Dim [Do] As Double = Nothing
        Dim Ch As Char = Nothing

        [Do] = DirectCast(Da, System.Double)
        Da = DirectCast([Do], DateTime)

        [In] = DirectCast(Ch, Int32)
        Ch = DirectCast([In], System.Char)

        Dim InArray As Integer() = Nothing
        Dim ObArray As Object() = Nothing
        Dim VtArray As ValueType() = Nothing

        ObArray = DirectCast(InArray, Object())
        VtArray = DirectCast(InArray, ValueType())

        Dim TC1Array As TestClass1() = Nothing
        Dim TC2Array As TestClass2() = Nothing

        TC2Array = DirectCast(TC1Array, TestClass2())

        Dim InArray2 As Integer(,) = Nothing

        InArray2 = DirectCast(InArray, Integer(,))

        Dim TI1Array As TestInterface1() = Nothing

        TI1Array = DirectCast(InArray, TestInterface1())

        Dim St As String = Nothing
        Dim ChArray As Char() = Nothing

        Ch = DirectCast(St, System.Char)
        St = DirectCast(Ch, System.String)

        St = DirectCast(ChArray, System.String)
        ChArray = DirectCast(St, System.Char())

        [In] = DirectCast([In], System.Int32)
        Si = DirectCast(Si, System.Single)
        [Do] = DirectCast([Do], System.Double)
        [Do] = DirectCast(Si, System.Double)

    End Sub

End Module

Interface TestInterface1
End Interface

Interface _Collection
End Interface

Class TestClass1
End Class

Class TestClass2
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Int64.MaxValue").WithArguments("Long", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "System.Double.MaxValue").WithArguments("Double", "Decimal"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Ch").WithArguments("Char", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "ChArray").WithArguments("Char()", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char()"),
                Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "[In]"),
                Diagnostic(ERRID.ERR_IdentityDirectCastForFloat, "Si"),
                Diagnostic(ERRID.ERR_IdentityDirectCastForFloat, "[Do]"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Si").WithArguments("Single", "Double"))

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Int64.MaxValue").WithArguments("Long", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "System.Double.MaxValue").WithArguments("Double", "Decimal"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Da").WithArguments("Date", "Integer"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "[In]").WithArguments("Integer", "Date"),
                Diagnostic(ERRID.ERR_DateToDoubleConversion, "Da"),
                Diagnostic(ERRID.ERR_DoubleToDateConversion, "[Do]"),
                Diagnostic(ERRID.ERR_CharToIntegralTypeMismatch1, "Ch").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_IntegralToCharTypeMismatch1, "[In]").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Ch").WithArguments("Char", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "ChArray").WithArguments("Char()", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char()"),
                Diagnostic(ERRID.WRN_ObsoleteIdentityDirectCastForValueType, "[In]"),
                Diagnostic(ERRID.ERR_IdentityDirectCastForFloat, "Si"),
                Diagnostic(ERRID.ERR_IdentityDirectCastForFloat, "[Do]"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Si").WithArguments("Single", "Double"))
        End Sub

        <Fact()>
        Public Sub TryCastDiagnostic1()
            Dim compilationDef =
<compilation name="TryCastDiagnostic1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()

        Dim [In] As Integer

        [In] = TryCast(Console.WriteLine(), Integer)
        [In] = TryCast(1, UnknownType)
        [In] = TryCast(unknownValue, Integer)
        [In] = TryCast(unknownValue, UnknownType)

        Dim tr As System.TypedReference = Nothing
        Dim ai As System.ArgIterator = Nothing
        Dim ra As System.RuntimeArgumentHandle = Nothing

        Dim Ob As Object

        Ob = TryCast(tr, Object)
        Ob = TryCast(ai, Object)
        Ob = TryCast(ra, Object)

        Dim vt As ValueType

        vt = TryCast(tr, ValueType)
        vt = TryCast(ai, ValueType)
        vt = TryCast(ra, ValueType)

        Dim collection As Microsoft.VisualBasic.Collection = Nothing
        Dim _collection As _Collection = Nothing

        collection = TryCast(_collection, Microsoft.VisualBasic.Collection)
        _collection = TryCast(collection, _Collection)

        Dim De As Decimal

        [In] = TryCast(Int64.MaxValue, Int32)
        De = TryCast(System.Double.MaxValue, System.Decimal)

        Dim Ch As Char = Nothing

        Dim InArray As Integer() = Nothing
        Dim ObArray As Object() = Nothing
        Dim VtArray As ValueType() = Nothing

        ObArray = TryCast(InArray, Object())
        VtArray = TryCast(InArray, ValueType())

        Dim TC1Array As TestClass1() = Nothing
        Dim TC2Array As TestClass2() = Nothing

        TC2Array = TryCast(TC1Array, TestClass2())

        Dim InArray2 As Integer(,) = Nothing

        InArray2 = TryCast(InArray, Integer(,))

        Dim TI1Array As TestInterface1() = Nothing

        TI1Array = TryCast(InArray, TestInterface1())

        Dim St As String = Nothing
        Dim ChArray As Char() = Nothing

        Ch = TryCast(St, System.Char)
        St = TryCast(Ch, System.String)

        St = TryCast(ChArray, System.String)
        ChArray = TryCast(St, System.Char())


    End Sub

End Module

Interface TestInterface1
End Interface

Interface _Collection
End Interface

Class TestClass1
End Class

Class TestClass2
End Class

Class TestClass3(Of T)

    Sub Test(val As Object)
        Dim x As T = TryCast(val, T)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "Int32").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "System.Decimal").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "System.Char").WithArguments("Char"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Ch").WithArguments("Char", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "ChArray").WithArguments("Char()", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char()"),
                Diagnostic(ERRID.ERR_TryCastOfUnconstrainedTypeParam1, "T").WithArguments("T"))

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_VoidValue, "Console.WriteLine()"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "unknownValue").WithArguments("unknownValue"),
                Diagnostic(ERRID.ERR_UndefinedType1, "UnknownType").WithArguments("UnknownType"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "tr").WithArguments("System.TypedReference"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ai").WithArguments("System.ArgIterator"),
                Diagnostic(ERRID.ERR_RestrictedConversion1, "ra").WithArguments("System.RuntimeArgumentHandle"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "_collection").WithArguments("_Collection", "Microsoft.VisualBasic.Collection"),
                Diagnostic(ERRID.WRN_InterfaceConversion2, "collection").WithArguments("Microsoft.VisualBasic.Collection", "_Collection"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "Int32").WithArguments("Integer"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "System.Decimal").WithArguments("Decimal"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "Object()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertObjectArrayMismatch3, "InArray").WithArguments("Integer()", "System.ValueType()", "Integer"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "TC1Array").WithArguments("TestClass1()", "TestClass2()", "TestClass1", "TestClass2"),
                Diagnostic(ERRID.ERR_ConvertArrayRankMismatch2, "InArray").WithArguments("Integer()", "Integer(*,*)"),
                Diagnostic(ERRID.ERR_ConvertArrayMismatch4, "InArray").WithArguments("Integer()", "TestInterface1()", "Integer", "TestInterface1"),
                Diagnostic(ERRID.ERR_TryCastOfValueType1, "System.Char").WithArguments("Char"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "Ch").WithArguments("Char", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "ChArray").WithArguments("Char()", "String"),
                Diagnostic(ERRID.ERR_TypeMismatch2, "St").WithArguments("String", "Char()"),
                Diagnostic(ERRID.ERR_TryCastOfUnconstrainedTypeParam1, "T").WithArguments("T"))
        End Sub

        <Fact()>
        Public Sub ExplicitConversions1()
            ' the argument past to CDate("") is following system setting, 
            ' so "1/2/2012" could be Jan 2nd OR Feb 1st
            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US")
            Try

                Dim compilationDef =
    <compilation name="VBExplicitConversions1">
        <file name="lib.vb">
            <%= My.Resources.Resource.PrintResultTestSource %>
        </file>
        <file name="a.vb">
Option Strict Off

Imports System
Imports System.Collections.Generic

Module Module1

    Sub Main()
        PrintResult(CObj(Nothing))
        PrintResult(CBool(Nothing))
        PrintResult(CByte(Nothing))
        'PrintResult(CChar(Nothing))
        PrintResult(CDate(Nothing))
        PrintResult(CDec(Nothing))
        PrintResult(CDbl(Nothing))
        PrintResult(CInt(Nothing))
        PrintResult(CLng(Nothing))
        PrintResult(CSByte(Nothing))
        PrintResult(CShort(Nothing))
        PrintResult(CSng(Nothing))
        PrintResult(CStr(Nothing))
        PrintResult(CUInt(Nothing))
        PrintResult(CULng(Nothing))
        PrintResult(CUShort(Nothing))
        PrintResult(CType(Nothing, System.Object))
        PrintResult(CType(Nothing, System.Boolean))
        PrintResult(CType(Nothing, System.Byte))
        'PrintResult(CType(Nothing, System.Char))
        PrintResult(CType(Nothing, System.DateTime))
        PrintResult(CType(Nothing, System.Decimal))
        PrintResult(CType(Nothing, System.Double))
        PrintResult(CType(Nothing, System.Int32))
        PrintResult(CType(Nothing, System.Int64))
        PrintResult(CType(Nothing, System.SByte))
        PrintResult(CType(Nothing, System.Int16))
        PrintResult(CType(Nothing, System.Single))
        PrintResult(CType(Nothing, System.String))
        PrintResult(CType(Nothing, System.UInt32))
        PrintResult(CType(Nothing, System.UInt64))
        PrintResult(CType(Nothing, System.UInt16))
        PrintResult(CType(Nothing, System.Guid))
        PrintResult(CType(Nothing, System.ValueType))

        PrintResult(CByte(300))

        PrintResult(CObj("String"))
        PrintResult(CBool("False"))
        PrintResult(CByte("1"))
        PrintResult(CChar("a"))
        PrintResult(CDate("11/12/2001 12:00:00 AM"))
        PrintResult(CDec("-2"))
        PrintResult(CDbl("3"))
        PrintResult(CInt("-4"))
        PrintResult(CLng("5"))
        PrintResult(CSByte("-6"))
        PrintResult(CShort("7"))
        PrintResult(CSng("-8"))
        PrintResult(CStr(9))
        PrintResult(CUInt("10"))
        PrintResult(CULng("11"))
        PrintResult(CUShort("12"))
    End Sub

    Function Int32ToInt32(val As System.Int32) As Int32
        Return CInt(val)
    End Function

    Function SingleToSingle(val As System.Single) As Single
        Return CSng(val)
    End Function

    Function DoubleToDouble(val As System.Double) As Double
        Return CDbl(val)
    End Function
End Module
    </file>
    </compilation>
                CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithOverflowChecks(False),
                             expectedOutput:=<![CDATA[
Object: []
Boolean: False
Byte: 0
Date: 1/1/0001 12:00:00 AM
Decimal: 0
Double: 0
Integer: 0
Long: 0
SByte: 0
Short: 0
Single: 0
String: []
UInteger: 0
ULong: 0
UShort: 0
Object: []
Boolean: False
Byte: 0
Date: 1/1/0001 12:00:00 AM
Decimal: 0
Double: 0
Integer: 0
Long: 0
SByte: 0
Short: 0
Single: 0
String: []
UInteger: 0
ULong: 0
UShort: 0
Guid: 00000000-0000-0000-0000-000000000000
ValueType: []
Byte: 44
Object: [String]
Boolean: False
Byte: 1
Char: [a]
Date: 11/12/2001 12:00:00 AM
Decimal: -2
Double: 3
Integer: -4
Long: 5
SByte: -6
Short: 7
Single: -8
String: [9]
UInteger: 10
ULong: 11
UShort: 12
]]>).
            VerifyIL("Module1.Int32ToInt32",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>).
            VerifyIL("Module1.SingleToSingle",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>).
            VerifyIL("Module1.DoubleToDouble",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>)

            Catch ex As Exception

            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try
        End Sub

        <Fact()>
        Public Sub DirectCast1()
            Dim compilationDef =
<compilation name="DirectCast1">
    <file name="helper.vb"><%= My.Resources.Resource.PrintResultTestSource %></file>
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Globalization
Imports System.Collections.Generic

Module Module1

    Sub Main()
        PrintResult(DirectCast(Nothing, System.Object))
        PrintResult(DirectCast(Nothing, System.Boolean))
        PrintResult(DirectCast(Nothing, System.Byte))
        PrintResult(DirectCast(Nothing, System.DateTime))
        PrintResult(DirectCast(Nothing, System.Decimal))
        PrintResult(DirectCast(Nothing, System.Double))
        PrintResult(DirectCast(Nothing, System.Int32))
        PrintResult(DirectCast(Nothing, System.Int64))
        PrintResult(DirectCast(Nothing, System.SByte))
        PrintResult(DirectCast(Nothing, System.Int16))
        PrintResult(DirectCast(Nothing, System.Single))
        PrintResult(DirectCast(Nothing, System.String))
        PrintResult(DirectCast(Nothing, System.UInt32))
        PrintResult(DirectCast(Nothing, System.UInt64))
        PrintResult(DirectCast(Nothing, System.UInt16))
        PrintResult(DirectCast(Nothing, System.ValueType))

        PrintResult(DirectCast(1, System.Object))
        PrintResult(DirectCast(3.5R, System.ValueType))

        Dim guid As Guid = New Guid("8c5dffd5-1778-4dd3-a9f5-6a9708146a7c")
        Dim guidObject As Object = guid

        PrintResult(DirectCast(guid, System.Object))
        PrintResult(DirectCast(guidObject, System.Guid))

        PrintResult(DirectCast("abc", System.IComparable))

        PrintResult(GenericParamTestHelperOfString.NothingToT())
        'PrintResult(DirectCast(Nothing, System.Guid))
        PrintResult(GenericParamTestHelperOfGuid.NothingToT())

        PrintResult(GenericParamTestHelperOfString.TToObject("abcd"))
        PrintResult(GenericParamTestHelperOfString.TToObject(Nothing))
        PrintResult(GenericParamTestHelperOfGuid.TToObject(guid))

        PrintResult(GenericParamTestHelperOfString.TToIComparable("abcde"))
        PrintResult(GenericParamTestHelperOfString.TToIComparable(Nothing))
        PrintResult(GenericParamTestHelperOfGuid.TToIComparable(guid))

        PrintResult(GenericParamTestHelperOfGuid.ObjectToT(guidObject))
        'PrintResult(GenericParamTestHelperOfGuid.ObjectToT(Nothing))
        PrintResult(GenericParamTestHelperOfString.ObjectToT("ObString"))
        PrintResult(GenericParamTestHelperOfString.ObjectToT(Nothing))
        'PrintResult(GenericParamTestHelper(Of System.Int32).ObjectToT(Nothing))

        PrintResult(GenericParamTestHelperOfString.IComparableToT("abcde"))
        PrintResult(GenericParamTestHelperOfString.IComparableToT(Nothing))
        PrintResult(GenericParamTestHelperOfGuid.IComparableToT(guid))
        'PrintResult(GenericParamTestHelperOfGuid.IComparableToT(Nothing))
        'PrintResult(GenericParamTestHelper(Of System.Double).IComparableToT(Nothing))

        Dim [In] As Integer = 23
        Dim De As Decimal = 24
        PrintResult(DirectCast([In], System.Int32))
        PrintResult(DirectCast(De, System.Decimal))
    End Sub

    Function NothingToGuid() As Guid
        Return DirectCast(Nothing, System.Guid)
    End Function

    Function NothingToInt32() As Int32
        Return DirectCast(Nothing, System.Int32)
    End Function

    Function Int32ToInt32(val As System.Int32) As Int32
        Return DirectCast(val, System.Int32)
    End Function

    Class GenericParamTestHelper(Of T)
        Public Shared Function ObjectToT(val As Object) As T
            Return DirectCast(val, T)
        End Function
        Public Shared Function TToObject(val As T) As Object
            Return DirectCast(val, Object)
        End Function
        Public Shared Function TToIComparable(val As T) As IComparable
            Return DirectCast(val, IComparable)
        End Function
        Public Shared Function IComparableToT(val As IComparable) As T
            Return DirectCast(val, T)
        End Function

        Public Shared Function NothingToT() As T
            Return DirectCast(Nothing, T)
        End Function
    End Class

    Class GenericParamTestHelperOfString
        Inherits GenericParamTestHelper(Of String)
    End Class

    Class GenericParamTestHelperOfGuid
        Inherits GenericParamTestHelper(Of Guid)
    End Class

End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                expectedOutput:=<![CDATA[
Object: []
Boolean: False
Byte: 0
Date: 1/1/0001 12:00:00 AM
Decimal: 0
Double: 0
Integer: 0
Long: 0
SByte: 0
Short: 0
Single: 0
String: []
UInteger: 0
ULong: 0
UShort: 0
ValueType: []
Object: [1]
ValueType: [3.5]
Object: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
Guid: 8c5dffd5-1778-4dd3-a9f5-6a9708146a7c
IComparable: [abc]
String: []
Guid: 00000000-0000-0000-0000-000000000000
Object: [abcd]
Object: []
Object: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
IComparable: [abcde]
IComparable: []
IComparable: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
Guid: 8c5dffd5-1778-4dd3-a9f5-6a9708146a7c
String: [ObString]
String: []
String: [abcde]
String: []
Guid: 8c5dffd5-1778-4dd3-a9f5-6a9708146a7c
Integer: 23
Decimal: 24
]]>).
            VerifyIL("Module1.NothingToGuid",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  unbox.any  "System.Guid"
  IL_0006:  ret
}
]]>).
            VerifyIL("Module1.NothingToInt32",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
]]>).
            VerifyIL("Module1.Int32ToInt32",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper(Of T).ObjectToT",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  "T"
  IL_0006:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper(Of T).TToObject",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper(Of T).TToIComparable",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  castclass  "System.IComparable"
  IL_000b:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper(Of T).IComparableToT",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  "T"
  IL_0006:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper(Of T).NothingToT",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.0
  IL_0009:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TryCast1()
            Dim compilationDef =
<compilation name="TryCast1">
    <file name="helper.vb"><%= My.Resources.Resource.PrintResultTestSource %></file>
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Globalization
Imports System.Collections.Generic

Module Module1

    Sub Main()
        PrintResult(TryCast(Nothing, System.Object))
        PrintResult(TryCast(Nothing, System.String))
        PrintResult(TryCast(Nothing, System.ValueType))

        PrintResult(TryCast(1, System.Object))
        PrintResult(TryCast(3.5R, System.ValueType))
        PrintResult(TryCast(CObj("sdf"), System.String))
        PrintResult(TryCast(New Object(), System.String))

        Dim guid As Guid = New Guid("8c5dffd5-1778-4dd3-a9f5-6a9708146a7c")
        Dim guidObject As Object = guid

        PrintResult(TryCast(guid, System.Object))

        PrintResult(TryCast("abc", System.IComparable))
        PrintResult(TryCast(guid, System.IComparable))

        PrintResult(GenericParamTestHelperOfString2.NothingToT())

        PrintResult(GenericParamTestHelperOfString1.TToObject("abcd"))
        PrintResult(GenericParamTestHelperOfString1.TToObject(Nothing))
        PrintResult(GenericParamTestHelperOfGuid1.TToObject(guid))

        PrintResult(GenericParamTestHelperOfString1.TToIComparable("abcde"))
        PrintResult(GenericParamTestHelperOfString1.TToIComparable(Nothing))
        PrintResult(GenericParamTestHelperOfGuid1.TToIComparable(guid))

        PrintResult(GenericParamTestHelperOfString2.ObjectToT("ObString"))
        PrintResult(GenericParamTestHelperOfString2.ObjectToT(Nothing))

        PrintResult(GenericParamTestHelperOfString2.IComparableToT("abcde"))
        PrintResult(GenericParamTestHelperOfString2.IComparableToT(Nothing))

    End Sub

    Function NothingToString() As String
        Return DirectCast(Nothing, System.String)
    End Function

    Function StringToString(val As String) As String
        Return DirectCast(val, System.String)
    End Function

    Class GenericParamTestHelper1(Of T)
        Public Shared Function TToObject(val As T) As Object
            Return TryCast(val, Object)
        End Function
        Public Shared Function TToIComparable(val As T) As IComparable
            Return TryCast(val, IComparable)
        End Function
    End Class

    Class GenericParamTestHelper2(Of T As Class)
        Public Shared Function ObjectToT(val As Object) As T
            Return TryCast(val, T)
        End Function
        Public Shared Function IComparableToT(val As IComparable) As T
            Return TryCast(val, T)
        End Function
        Public Shared Function NothingToT() As T
            Return TryCast(Nothing, T)
        End Function
    End Class

    Class GenericParamTestHelperOfString1
        Inherits GenericParamTestHelper1(Of String)
    End Class

    Class GenericParamTestHelperOfString2
        Inherits GenericParamTestHelper2(Of String)
    End Class

    Class GenericParamTestHelperOfGuid1
        Inherits GenericParamTestHelper1(Of Guid)
    End Class

    End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=<![CDATA[
Object: []
String: []
ValueType: []
Object: [1]
ValueType: [3.5]
String: [sdf]
String: []
Object: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
IComparable: [abc]
IComparable: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
String: []
Object: [abcd]
Object: []
Object: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
IComparable: [abcde]
IComparable: []
IComparable: [8c5dffd5-1778-4dd3-a9f5-6a9708146a7c]
String: [ObString]
String: []
String: [abcde]
String: []
]]>).
                VerifyIL("Module1.NothingToString",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
]]>).
                VerifyIL("Module1.StringToString",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>).
                VerifyIL("Module1.GenericParamTestHelper2(Of T).ObjectToT",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     "T"
  IL_0006:  unbox.any  "T"
  IL_000b:  ret
}
]]>).
                VerifyIL("Module1.GenericParamTestHelper1(Of T).TToObject",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  isinst     "Object"
  IL_000b:  ret
}
]]>).
                VerifyIL("Module1.GenericParamTestHelper1(Of T).TToIComparable",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  isinst     "System.IComparable"
  IL_000b:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper2(Of T).IComparableToT",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     "T"
  IL_0006:  unbox.any  "T"
  IL_000b:  ret
}
]]>).
            VerifyIL("Module1.GenericParamTestHelper2(Of T).NothingToT",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.0
  IL_0009:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Bug4281_1()
            Dim compilationDef =
<compilation name="Bug4281_1">
    <file name="a.vb">
Imports System
Module M
  Sub Main()
    Dim x As Object= DirectCast(1, DayOfWeek)
    System.Console.WriteLine(x.GetType())
    System.Console.WriteLine(x)

    Dim y As Integer = 2
    x = DirectCast(y, DayOfWeek)
    System.Console.WriteLine(x.GetType())
    System.Console.WriteLine(x)
  End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                expectedOutput:=<![CDATA[
System.DayOfWeek
Monday
System.DayOfWeek
Tuesday
]]>).
                VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  box        "System.DayOfWeek"
  IL_0006:  dup
  IL_0007:  callvirt   "Function Object.GetType() As System.Type"
  IL_000c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0011:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0016:  call       "Sub System.Console.WriteLine(Object)"
  IL_001b:  ldc.i4.2
  IL_001c:  box        "System.DayOfWeek"
  IL_0021:  dup
  IL_0022:  callvirt   "Function Object.GetType() As System.Type"
  IL_0027:  call       "Sub System.Console.WriteLine(Object)"
  IL_002c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0031:  call       "Sub System.Console.WriteLine(Object)"
  IL_0036:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Bug4281_2()
            Dim compilationDef =
<compilation name="Bug4281_2">
    <file name="a.vb">
Imports System
Module M
  Sub Main()
    Dim x As DayOfWeek= DirectCast(1, DayOfWeek)
    System.Console.WriteLine(x.GetType())
    System.Console.WriteLine(x)

    Dim y As Integer = 2
    x = DirectCast(y, DayOfWeek)
    System.Console.WriteLine(x.GetType())
    System.Console.WriteLine(x)
  End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
              expectedOutput:=<![CDATA[
System.DayOfWeek
1
System.DayOfWeek
2
]]>)
        End Sub

        <Fact()>
        Public Sub Bug4256()
            Dim compilationDef =
<compilation name="Bug4256">
    <file name="a.vb">
Option Strict On
Module M
  Sub Main()
    Dim x As Object = 1
    Dim y As Long = CLng(x)
    System.Console.WriteLine(y.GetType())
    System.Console.WriteLine(y)
  End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                expectedOutput:=<![CDATA[
System.Int64
1
]]>)
        End Sub

        <WorkItem(11515, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub TestIdentityConversionForGenericNested()
            Dim vbCompilation = CreateVisualBasicCompilation("TestIdentityConversionForGenericNested",
            <![CDATA[Option Strict On
Imports System

Public Module Program
    Class C1(Of T)
        Public EnumField As E1
        Structure S1
            Public EnumField As E1
        End Structure
        Enum E1
            A
        End Enum
    End Class
    Sub Main()
        Dim outer As New C1(Of Integer)
        Dim inner As New C1(Of Integer).S1
        outer.EnumField = C1(Of Integer).E1.A
        inner.EnumField = C1(Of Integer).E1.A     
        Goo(inner.EnumField)
    End Sub
    Sub Goo(x As Object)
        Console.WriteLine(x.ToString)
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))
            CompileAndVerify(vbCompilation, expectedOutput:="A").VerifyDiagnostics()
        End Sub

        <WorkItem(544919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544919")>
        <Fact>
        Public Sub TestClassifyConversion()
            Dim source =
<text>
Imports System

Module Program

    Sub M()
    End Sub

    Sub M(l As Long)
    End Sub

    Sub M(s As Short)
    End Sub

    Sub M(i As Integer)
    End Sub

    Sub Main()
        Dim ii As Integer = 0
        Console.WriteLine(ii)
        Dim jj As Short = 1
        Console.WriteLine(jj)
        Dim ss As String = String.Empty
        Console.WriteLine(ss)

        ' Perform conversion classification here.
    End Sub
End Module
</text>.Value

            Dim tree = Parse(source)
            Dim c As VisualBasicCompilation = VisualBasicCompilation.Create("MyCompilation").AddReferences(MscorlibRef).AddSyntaxTrees(tree)

            Dim model = c.GetSemanticModel(tree)

            ' Get VariableDeclaratorSyntax corresponding to variable 'ii' above.
            Dim variableDeclarator = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("ii", StringComparison.Ordinal)).Parent.Parent, VariableDeclaratorSyntax)

            ' Get TypeSymbol corresponding to above VariableDeclaratorSyntax.
            Dim targetType As TypeSymbol = CType(model.GetDeclaredSymbol(variableDeclarator.Names.Single), LocalSymbol).Type

            Dim local As LocalSymbol = CType(model.GetDeclaredSymbol(variableDeclarator.Names.Single), LocalSymbol)
            Assert.Equal(1, local.Locations.Length)

            ' Perform ClassifyConversion for expressions from within the above SyntaxTree.
            Dim sourceExpression1 = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("jj)", StringComparison.Ordinal)).Parent, ExpressionSyntax)
            Dim conversion As Conversion = model.ClassifyConversion(sourceExpression1, targetType)

            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)

            Dim sourceExpression2 = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("ss)", StringComparison.Ordinal)).Parent, ExpressionSyntax)
            conversion = model.ClassifyConversion(sourceExpression2, targetType)

            Assert.True(conversion.IsNarrowing)
            Assert.True(conversion.IsString)

            ' Perform ClassifyConversion for constructed expressions
            ' at the position identified by the comment "' Perform ..." above.
            Dim sourceExpression3 As ExpressionSyntax = SyntaxFactory.IdentifierName("jj")
            Dim position = source.IndexOf("' ", StringComparison.Ordinal)
            conversion = model.ClassifyConversion(position, sourceExpression3, targetType)

            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)

            Dim sourceExpression4 As ExpressionSyntax = SyntaxFactory.IdentifierName("ss")
            conversion = model.ClassifyConversion(position, sourceExpression4, targetType)

            Assert.True(conversion.IsNarrowing)
            Assert.True(conversion.IsString)

            Dim sourceExpression5 As ExpressionSyntax = SyntaxFactory.ParseExpression("100L")
            conversion = model.ClassifyConversion(position, sourceExpression5, targetType)

            ' This is Widening because the numeric literal constant 100L can be converted to Integer
            ' without any data loss. Note: This is special for literal constants.
            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(544919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544919")>
        <Fact>
        Public Sub TestClassifyConversionStaticLocal()
            Dim source =
<text>
Imports System

Module Program

    Sub M()
    End Sub

    Sub M(l As Long)
    End Sub

    Sub M(s As Short)
    End Sub

    Sub M(i As Integer)
    End Sub

    Sub Main()
        Static ii As Integer = 0
        Console.WriteLine(ii)
        Static jj As Short = 1
        Console.WriteLine(jj)
        Static ss As String = String.Empty
        Console.WriteLine(ss)

        ' Perform conversion classification here.
    End Sub
End Module
</text>.Value

            Dim tree = Parse(source)
            Dim c As VisualBasicCompilation = VisualBasicCompilation.Create("MyCompilation").AddReferences(MscorlibRef).AddSyntaxTrees(tree)

            Dim model = c.GetSemanticModel(tree)

            ' Get VariableDeclaratorSyntax corresponding to variable 'ii' above.
            Dim variableDeclarator = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("ii", StringComparison.Ordinal)).Parent.Parent, VariableDeclaratorSyntax)

            ' Get TypeSymbol corresponding to above VariableDeclaratorSyntax.
            Dim targetType As TypeSymbol = CType(model.GetDeclaredSymbol(variableDeclarator.Names.Single), LocalSymbol).Type

            Dim local As LocalSymbol = CType(model.GetDeclaredSymbol(variableDeclarator.Names.Single), LocalSymbol)
            Assert.Equal(1, local.Locations.Length)

            ' Perform ClassifyConversion for expressions from within the above SyntaxTree.
            Dim sourceExpression1 = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("jj)", StringComparison.Ordinal)).Parent, ExpressionSyntax)
            Dim conversion As Conversion = model.ClassifyConversion(sourceExpression1, targetType)

            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)

            Dim sourceExpression2 = CType(tree.GetCompilationUnitRoot().FindToken(source.IndexOf("ss)", StringComparison.Ordinal)).Parent, ExpressionSyntax)
            conversion = model.ClassifyConversion(sourceExpression2, targetType)

            Assert.True(conversion.IsNarrowing)
            Assert.True(conversion.IsString)

            ' Perform ClassifyConversion for constructed expressions
            ' at the position identified by the comment "' Perform ..." above.
            Dim sourceExpression3 As ExpressionSyntax = SyntaxFactory.IdentifierName("jj")
            Dim position = source.IndexOf("' ", StringComparison.Ordinal)
            conversion = model.ClassifyConversion(position, sourceExpression3, targetType)

            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)

            Dim sourceExpression4 As ExpressionSyntax = SyntaxFactory.IdentifierName("ss")
            conversion = model.ClassifyConversion(position, sourceExpression4, targetType)

            Assert.True(conversion.IsNarrowing)
            Assert.True(conversion.IsString)

            Dim sourceExpression5 As ExpressionSyntax = SyntaxFactory.ParseExpression("100L")
            conversion = model.ClassifyConversion(position, sourceExpression5, targetType)

            ' This is Widening because the numeric literal constant 100L can be converted to Integer
            ' without any data loss. Note: This is special for literal constants.
            Assert.True(conversion.IsWidening)
            Assert.True(conversion.IsNumeric)
        End Sub

        <WorkItem(544620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")>
        <Fact()>
        Public Sub Bug13088()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program

    Public Const Z1 As Integer = 300
    Public Const Z2 As Byte = Z1
    Public Const Z3 As Byte = CByte(300)
    Public Const Z4 As Byte = DirectCast(300, Byte)

    Sub Main()
    End Sub
End Module
    ]]></file>
    </compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExpressionOverflow1, "Z1").WithArguments("Byte"),
                                           Diagnostic(ERRID.ERR_ExpressionOverflow1, "300").WithArguments("Byte"),
                                           Diagnostic(ERRID.ERR_TypeMismatch2, "300").WithArguments("Integer", "Byte"))

            Dim symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z2").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)

            symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z3").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)

            symbol = compilation.GlobalNamespace.GetTypeMembers("Program").Single.GetMembers("Z4").Single
            Assert.False(DirectCast(symbol, FieldSymbol).HasConstantValue)
        End Sub

        <WorkItem(545760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545760")>
        <Fact()>
        Public Sub Bug14409()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Strict On

Module M1
    Sub Main()
        Dim x As System.DayOfWeek? = 0
        Test(0)
    End Sub

    Sub Test(x As System.DayOfWeek?)
    End Sub
End Module
    ]]></file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation)
        End Sub

        <WorkItem(545760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545760")>
        <Fact()>
        Public Sub Bug14409_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Strict On

Module M1
    Sub Main()
        Test(0)
    End Sub

    Sub Test(x As System.DayOfWeek?)
    End Sub

    Sub Test(x As System.TypeCode?)
    End Sub
End Module
    ]]></file>
    </compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'Test' can be called without a narrowing conversion:
    'Public Sub Test(x As DayOfWeek?)': Argument matching parameter 'x' narrows from 'Integer' to 'DayOfWeek?'.
    'Public Sub Test(x As TypeCode?)': Argument matching parameter 'x' narrows from 'Integer' to 'TypeCode?'.
        Test(0)
        ~~~~
</expected>)

        End Sub

        <WorkItem(571095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571095")>
        <Fact()>
        Public Sub Bug571095_01()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
imports System

Module Module1
    Sub Main()
        Dim Y(10, 10) As Integer
        'COMPILEERROR: BC30311, "Y"
        For Each x As string() In Y
            Console.WriteLine(x)
        Next x
        'COMPILEERROR: BC30311, "Y"
        For Each x As Integer(,) In Y
            Console.WriteLine(x)
        Next x
    End Sub
End Module

    ]]></file>
    </compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'String()'.
        For Each x As string() In Y
                                  ~
BC30311: Value of type 'Integer' cannot be converted to 'Integer(*,*)'.
        For Each x As Integer(,) In Y
                                    ~
</expected>)

        End Sub

        <WorkItem(571095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571095")>
        <Fact()>
        Public Sub Bug571095_02()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
imports System

Module Module1
    Sub Main()
        Dim Y(10) As Integer
        'COMPILEERROR: BC30311, "Y"
        For Each x As string() In Y
            Console.WriteLine(x)
        Next x
        'COMPILEERROR: BC30311, "Y"
        For Each x As Integer() In Y
            Console.WriteLine(x)
        Next x
    End Sub
End Module

    ]]></file>
    </compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'String()'.
        For Each x As string() In Y
                                  ~
BC30311: Value of type 'Integer' cannot be converted to 'Integer()'.
        For Each x As Integer() In Y
                                   ~
</expected>)

        End Sub

        <WorkItem(31, "https://roslyn.codeplex.com/workitem/31")>
        <Fact()>
        Public Sub BugCodePlex_31()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Strict On

Module Module1
    Property Value As BooleanEx?
    Sub Main()
        If Value Then
        End If
        System.Console.WriteLine("---")
        Value = true
        System.Console.WriteLine("---")
        Dim x as Boolean? = Value
        System.Console.WriteLine("---")
        If Value Then
        End If
    End Sub
End Module

Structure BooleanEx
    Private b As Boolean

    Public Sub New(value As Boolean)
        b = value
    End Sub

    Public Shared Widening Operator CType(value As Boolean) As BooleanEx
        System.Console.WriteLine("CType(value As Boolean) As BooleanEx")
        Return New BooleanEx(value)
    End Operator

    Public Shared Widening Operator CType(value As BooleanEx) As Boolean
        System.Console.WriteLine("CType(value As BooleanEx) As Boolean")
        Return value.b
    End Operator

    Public Shared Widening Operator CType(value As Integer) As BooleanEx
        System.Console.WriteLine("CType(value As Integer) As BooleanEx")
        Return New BooleanEx(CBool(value))
    End Operator

    Public Shared Widening Operator CType(value As BooleanEx) As Integer
        System.Console.WriteLine("CType(value As BooleanEx) As Integer")
        Return CInt(value.b)
    End Operator

    Public Shared Widening Operator CType(value As String) As BooleanEx
        System.Console.WriteLine("CType(value As String) As BooleanEx")
        Return New BooleanEx(CBool(value))
    End Operator

    Public Shared Widening Operator CType(value As BooleanEx) As String
        System.Console.WriteLine("CType(value As BooleanEx) As String")
        Return CStr(value.b)
    End Operator

    Public Shared Operator =(value1 As BooleanEx, value2 As Boolean) As Boolean
        System.Console.WriteLine("=(value1 As BooleanEx, value2 As Boolean) As Boolean")
        Return False
    End Operator

    Public Shared Operator <>(value1 As BooleanEx, value2 As Boolean) As Boolean
        System.Console.WriteLine("<>(value1 As BooleanEx, value2 As Boolean) As Boolean")
        Return False
    End Operator
End Structure
    ]]></file>
    </compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
"---
CType(value As Boolean) As BooleanEx
---
CType(value As BooleanEx) As Boolean
---
CType(value As BooleanEx) As Boolean")
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_01()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Integer = Double.MaxValue
    End Sub
End Module
    ]]></file>
    </compilation>)

            Dim expectedErr = <expected>
BC30439: Constant expression not representable in type 'Integer'.
        Dim x As Integer = Double.MaxValue
                           ~~~~~~~~~~~~~~~
                              </expected>

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedErr)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedErr)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedErr)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedErr)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedErr)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedErr)
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_02()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Integer? = Double.MaxValue
    End Sub
End Module
    ]]></file>
    </compilation>)

            Dim expectedError = <expected>
BC30439: Constant expression not representable in type 'Integer?'.
        Dim x As Integer? = Double.MaxValue
                            ~~~~~~~~~~~~~~~
                                </expected>

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_03()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Short = Integer.MaxValue
        System.Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
    </compilation>)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()


            Dim expectedError = <expected>
BC30439: Constant expression not representable in type 'Short'.
        Dim x As Short = Integer.MaxValue
                         ~~~~~~~~~~~~~~~~
                                </expected>

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_04()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Short? = Integer.MaxValue
        System.Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
    </compilation>)

            Dim expectedIL = <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (Short? V_0) //x
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  ldc.i4.m1
  IL_0004:  call       "Sub Short?..ctor(Short)"
  IL_0009:  ldloc.0
  IL_000a:  box        "Short?"
  IL_000f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0014:  nop
  IL_0015:  ret
}
]]>
            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()
            verifier.VerifyIL("Program.Main", expectedIL)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))

            verifier = CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()
            verifier.VerifyIL("Program.Main", expectedIL)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))

            verifier = CompileAndVerify(compilation, expectedOutput:="-1").VerifyDiagnostics()
            verifier.VerifyIL("Program.Main", expectedIL)

            Dim expectedError = <expected>
BC30439: Constant expression not representable in type 'Short?'.
        Dim x As Short? = Integer.MaxValue
                          ~~~~~~~~~~~~~~~~
                                </expected>

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            AssertTheseDiagnostics(compilation, expectedError)
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_05()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Short = CInt(Short.MaxValue)
        System.Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
    </compilation>)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_06()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As Short? = CInt(Short.MaxValue)
        System.Console.WriteLine(x)
    End Sub
End Module
    ]]></file>
    </compilation>)

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(False))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Custom).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.On).WithOverflowChecks(True))
            CompileAndVerify(compilation, expectedOutput:="32767").VerifyDiagnostics()
        End Sub

        <WorkItem(1099862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099862")>
        <Fact()>
        Public Sub Bug1099862_07()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x = CType(Double.MaxValue, System.Nullable(Of Integer))
    End Sub
End Module
    ]]></file>
    </compilation>)

            Dim expectedError = <expected>
BC30439: Constant expression not representable in type 'Integer?'.
        Dim x = CType(Double.MaxValue, System.Nullable(Of Integer))
                      ~~~~~~~~~~~~~~~
                                </expected>

            compilation = compilation.WithOptions(TestOptions.DebugExe.WithOptionStrict(OptionStrict.Off).WithOverflowChecks(False))
            AssertTheseDiagnostics(compilation, expectedError)
        End Sub

        <WorkItem(2094, "https://github.com/dotnet/roslyn/issues/2094")>
        <Fact()>
        Public Sub DirectCastNothingToAStructure()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Try
            Dim val = DirectCast(Nothing, S)
            System.Console.WriteLine("Unexpected - 1 !!!")
        Catch e as System.NullReferenceException
            System.Console.WriteLine("Expected - 1")
        End Try

        Try
            M(DirectCast(Nothing, S))
            System.Console.WriteLine("Unexpected - 2 !!!")
        Catch e as System.NullReferenceException
            System.Console.WriteLine("Expected - 2")
        End Try
    End Sub

    Shared Sub M(val as S)
    End Sub
End Class

Structure S
End Structure
    ]]></file>
    </compilation>, options:=TestOptions.ReleaseExe)

            Dim expectedOutput = <![CDATA[
Expected - 1
Expected - 2
]]>
            CompileAndVerify(compilation, expectedOutput:=expectedOutput).VerifyDiagnostics()

            compilation = compilation.WithOptions(TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput).VerifyDiagnostics()
        End Sub

        <WorkItem(8475, "https://github.com/dotnet/roslyn/issues/8475")>
        <Fact()>
        Public Sub ConvertConstantBeforeItsDeclaration()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim x as Integer = STR
        Const STR As String = ""
    End Sub
End Class
    ]]></file>
    </compilation>, options:=TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
<expected>
BC32000: Local variable 'STR' cannot be referred to before it is declared.
        Dim x as Integer = STR
                           ~~~
</expected>)
        End Sub

        <WorkItem(9887, "https://github.com/dotnet/roslyn/issues/9887")>
        <Fact()>
        Public Sub ConvertReferenceTypeToIntrinsicValueType()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim value As System.IComparable = 1.0R
        TestBoolean(value)
        TestByte(value)
        TestSByte(value)
        TestShort(value)
        TestUShort(value)
        TestInt(value)
        TestUInt(value)
        TestLng(value)
        TestULng(value)
        TestDec(value)

        Dim value2 As System.IComparable = 1
        TestSng(value2)
        TestDbl(value)

        Dim value3 As System.IComparable = "2016-5-23"
        TestDate(value3)
        TestChar(value3)
    End Sub

    Private Sub TestArray(value2 As System.Collections.IEnumerable)
        System.Console.WriteLine(CType(value2, Integer()))
    End Sub

    Private Sub TestString(value2 As IComparable)
        System.Console.WriteLine(CStr(value2))
    End Sub

    Private Sub TestChar(value3 As IComparable)
        System.Console.WriteLine(CChar(value3))
    End Sub

    Private Sub TestDate(value3 As IComparable)
        System.Console.WriteLine(CDate(value3).Day)
    End Sub

    Private Sub TestDbl(value As IComparable)
        System.Console.WriteLine(CDbl(value))
    End Sub

    Private Sub TestSng(value2 As IComparable)
        System.Console.WriteLine(CSng(value2))
    End Sub

    Private Sub TestDec(value As IComparable)
        System.Console.WriteLine(CDec(value))
    End Sub

    Private Sub TestULng(value As IComparable)
        System.Console.WriteLine(CULng(value))
    End Sub

    Private Sub TestLng(value As IComparable)
        System.Console.WriteLine(CLng(value))
    End Sub

    Private Sub TestUInt(value As IComparable)
        System.Console.WriteLine(CUInt(value))
    End Sub

    Private Sub TestInt(value As IComparable)
        System.Console.WriteLine(CInt(value))
    End Sub

    Private Sub TestUShort(value As IComparable)
        System.Console.WriteLine(CUShort(value))
    End Sub

    Private Sub TestShort(value As IComparable)
        System.Console.WriteLine(CShort(value))
    End Sub

    Private Sub TestSByte(value As IComparable)
        System.Console.WriteLine(CSByte(value))
    End Sub

    Private Sub TestByte(value As IComparable)
        System.Console.WriteLine(CByte(value))
    End Sub

    Private Sub TestBoolean(value As IComparable)
        System.Console.WriteLine(CBool(value))
    End Sub

    Sub TestTypeParameter(Of T)(value As IComparable)
        System.Console.WriteLine(CType(value, T))
    End Sub
End Module
    ]]></file>
    </compilation>, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
"True
1
1
1
1
1
1
1
1
1
1
1
23
2")

            verifier.VerifyIL("Module1.TestArray",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  castclass  "Integer()"
  IL_0006:  call       "Sub System.Console.WriteLine(Object)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestString",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  castclass  "String"
  IL_0006:  call       "Sub System.Console.WriteLine(String)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestChar",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToChar(Object) As Char"
  IL_0006:  call       "Sub System.Console.WriteLine(Char)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestDate",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Date V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDate(Object) As Date"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Function Date.get_Day() As Integer"
  IL_000e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestDbl",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDouble(Object) As Double"
  IL_0006:  call       "Sub System.Console.WriteLine(Double)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestSng",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSingle(Object) As Single"
  IL_0006:  call       "Sub System.Console.WriteLine(Single)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestDec",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDecimal(Object) As Decimal"
  IL_0006:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestULng",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToULong(Object) As ULong"
  IL_0006:  call       "Sub System.Console.WriteLine(ULong)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestLng",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToLong(Object) As Long"
  IL_0006:  call       "Sub System.Console.WriteLine(Long)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestUInt",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToUInteger(Object) As UInteger"
  IL_0006:  call       "Sub System.Console.WriteLine(UInteger)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestInt",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestUShort",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToUShort(Object) As UShort"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestShort",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToShort(Object) As Short"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestSByte",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSByte(Object) As SByte"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestByte",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToByte(Object) As Byte"
  IL_0006:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestBoolean",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0006:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.TestTypeParameter",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  "T"
  IL_0006:  box        "T"
  IL_000b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0010:  ret
}
]]>)
        End Sub

    End Class
End Namespace
