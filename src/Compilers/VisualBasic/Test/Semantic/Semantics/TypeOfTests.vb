' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class TypeOfTests
        Inherits BasicTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesNoErrorsOnBoxedValueTypes_ObjectExpressionAndPrimitiveType()
            Dim source = <![CDATA[
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim o As Object = 1

        If TypeOf o Is Integer Then'BIND:"TypeOf o Is Integer"
            Console.WriteLine("Boxed as System.Object")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o Is Integer')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.Int32
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesNoErrorsOnBoxedValueTypes_ObjectExpressionAndIComparableType()
            Dim source = <![CDATA[
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim o As Object = 1

        If TypeOf o Is IComparable Then'BIND:"TypeOf o Is IComparable"
            Console.WriteLine("Boxed as System.Object to interface type")
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o Is IComparable')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.IComparable
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesNoErrorsOnBoxedValueTypes_ValueTypeExpressionAndEnumType()
            Dim source = <![CDATA[
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim v As ValueType = DayOfWeek.Monday

        If TypeOf v Is DayOfWeek Then'BIND:"TypeOf v Is DayOfWeek"
            v = 1

            If TypeOf v Is Integer Then
                Console.WriteLine("Boxed as System.ValueType")
            End If
        End If
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf v Is DayOfWeek')
  Operand: 
    ILocalReferenceOperation: v (OperationKind.LocalReference, Type: System.ValueType) (Syntax: 'v')
  IsType: System.DayOfWeek
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesNoErrorsOnBoxedValueTypes_EnumTypeExpressionAndEnumType()
            Dim source = <![CDATA[
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim e As [Enum] = DayOfWeek.Tuesday

        If TypeOf e Is DayOfWeek Then'BIND:"TypeOf e Is DayOfWeek"
            Console.WriteLine("Boxed as System.Enum")
        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf e Is DayOfWeek')
  Operand: 
    ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Enum) (Syntax: 'e')
  IsType: System.DayOfWeek
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ProducesNoErrorsWithClassTypeUnconstrainedTypeParameterTargetTypes_ClassConstraint()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim oT As T = Nothing

        If TypeOf oT Is TRef Then'BIND:"TypeOf oT Is TRef"

        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf oT Is TRef')
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'TypeOf oT Is TRef')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: oT (OperationKind.LocalReference, Type: T) (Syntax: 'oT')
  IsType: TRef
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ProducesNoErrorsWithClassTypeUnconstrainedTypeParameterTargetTypes_StructureConstraint()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim oT As T = Nothing

        If TypeOf oT Is TVal Then'BIND:"TypeOf oT Is TVal"

        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf oT Is TVal')
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'TypeOf oT Is TVal')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: oT (OperationKind.LocalReference, Type: T) (Syntax: 'oT')
  IsType: TVal
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ProducesNoErrorsWithClassTypeUnconstrainedTypeParameterTargetTypes_StructureAndClassConstraint()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim vVal As TVal = Nothing

        If TypeOf vVal Is TRef Then'BIND:"TypeOf vVal Is TRef"

        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf vVal Is TRef')
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'TypeOf vVal Is TRef')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: vVal (OperationKind.LocalReference, Type: TVal) (Syntax: 'vVal')
  IsType: TRef
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub ProducesNoErrorsWithClassTypeUnconstrainedTypeParameterTargetTypes_StringType()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim oT As T = Nothing

        If TypeOf oT Is String Then'BIND:"TypeOf oT Is String"

        End If

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf oT Is String')
  Operand: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'TypeOf oT Is String')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: oT (OperationKind.LocalReference, Type: T) (Syntax: 'oT')
  IsType: System.String
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesErrorsWhenNoReferenceConversionExists_BC30371()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Public Sub Main()

        Dim obj As Object = ""

        If TypeOf obj Is Program Then'BIND:"TypeOf obj Is Program"

        End If
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf obj Is Program')
  Operand: 
    ILocalReferenceOperation: obj (OperationKind.LocalReference, Type: System.Object) (Syntax: 'obj')
  IsType: Program
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30371: Module 'Program' cannot be used as a type.
        If TypeOf obj Is Program Then'BIND:"TypeOf obj Is Program"
                         ~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesErrorsWhenNoReferenceConversionExists_BC31430()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Public Sub Main()

        Dim s As String = Nothing

        If TypeOf s Is AppDomain Then'BIND:"TypeOf s Is AppDomain"

        ElseIf TypeOf s Is IDisposable Then

        End If
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is AppDomain')
  Operand: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 's')
  IsType: System.AppDomain
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31430: Expression of type 'String' can never be of type 'AppDomain'.
        If TypeOf s Is AppDomain Then'BIND:"TypeOf s Is AppDomain"
           ~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesErrorsWhenNoReferenceConversionExists_BC31430_02()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Public Sub Main()

        Dim s As String = Nothing

        If TypeOf s Is Integer Then'BIND:"TypeOf s Is Integer"

        End If
    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf s Is Integer')
  Operand: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String, IsInvalid) (Syntax: 's')
  IsType: System.Int32
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC31430: Expression of type 'String' can never be of type 'Integer'.
        If TypeOf s Is Integer Then'BIND:"TypeOf s Is Integer"
           ~~~~~~~~~~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub ProducesErrorsWhenNoReferenceConversionExists_BC30021()
            Dim source = <![CDATA[
Option Strict On
Imports System
Module Program
    Public Sub Main()

        Dim i As Integer = 0

        If TypeOf i Is String Then'BIND:"TypeOf i Is String"

        End If

    End Sub

End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean, IsInvalid) (Syntax: 'TypeOf i Is String')
  Operand: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
  IsType: System.String
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30021: 'TypeOf ... Is' requires its left operand to have a reference type, but this operand has the value type 'Integer'.
        If TypeOf i Is String Then'BIND:"TypeOf i Is String"
                  ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ProducesErrorsWhenNoReferenceConversionExistsBetweenConstrainedTypeParameterTypes()
            Dim source =
<compilation name="ProducesErrorsWhenNoReferenceConversionExistsBetweenClassTypeConstrainedTypeParameterTypes">
    <file name="Program.vb">
Option Strict On
Imports System
Module Program
 
    Sub Main()
    End Sub
 
    Class A : End Class
    Class B : End Class
 
    Sub M(Of T, TA As A, TB As B, TC As Structure, TD As IDisposable)(x As T, a As TA, b As TB, c As TC, d As TD, s As String)
 
        If TypeOf x Is TA OrElse TypeOf x Is TB OrElse TypeOf x Is TC OrElse TypeOf x Is TD OrElse TypeOf x Is String Then
            Console.WriteLine("Success!")
        End If
 
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf a Is T OrElse TypeOf a Is TD Then
            Console.WriteLine("Success!")
        End If
 
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf b Is T OrElse TypeOf b Is TD Then
            Console.WriteLine("Success!")
        End If
 
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf c Is T OrElse TypeOf c Is TD Then
            Console.WriteLine("Success!")
        End If
 
        If TypeOf d Is T OrElse TypeOf d Is TA OrElse TypeOf d Is TB OrElse TypeOf d Is TC OrElse TypeOf d Is String Then
            Console.WriteLine("Success!")
        End If
 
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
            Console.WriteLine("Fail!")
        ElseIf TypeOf s Is T OrElse TypeOf s Is T OrElse TypeOf s Is TD Then
            Console.WriteLine("Success!")
        End If
 
    End Sub
 
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31430: Expression of type 'TA' can never be of type 'TB'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TA' can never be of type 'TC'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TA' can never be of type 'String'.
        If TypeOf a Is TB OrElse TypeOf a Is TC OrElse TypeOf a Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'TA'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'TC'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TB' can never be of type 'String'.
        If TypeOf b Is TA OrElse TypeOf b Is TC OrElse TypeOf b Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'TA'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'TB'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'TC' can never be of type 'String'.
        If TypeOf c Is TA OrElse TypeOf c Is TB OrElse TypeOf c Is String Then
                                                       ~~~~~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TA'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
           ~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TB'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
                                 ~~~~~~~~~~~~~~
BC31430: Expression of type 'String' can never be of type 'TC'.
        If TypeOf s Is TA OrElse TypeOf s Is TB OrElse TypeOf s Is TC Then
                                                       ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases()
            Dim source =
<compilation name="SeesThroughTypeAndNamespaceAliases">
    <file name="Program.vb">
Option Strict On
 
Imports HRESULT = System.Int32
Imports CharacterSequence = System.String
 
Module Program
    Sub Main(args As String())
 
        Dim o As Object = ""
 
        Dim isString = TypeOf o Is CharacterSequence
 
        Dim isInteger = TypeOf o Is HRESULT
 
        Dim isNotString = TypeOf o IsNot CharacterSequence
 
        Dim isNotInteger = TypeOf o IsNot HRESULT 
 
        System.Console.WriteLine(isString)
        System.Console.WriteLine(isInteger)
        System.Console.WriteLine(isNotString)
        System.Console.WriteLine(isNotInteger)
 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, <![CDATA[
True
False
False
True
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases_IsStringAliasIOperationTest()
            Dim source = <![CDATA[
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is CharacterSequence'BIND:"TypeOf o Is CharacterSequence"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o Is ... terSequence')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.String
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases_IsIntegerAliasIOperationTest()
            Dim source = <![CDATA[
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isInteger = TypeOf o Is HRESULT'BIND:"TypeOf o Is HRESULT"

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o Is HRESULT')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.Int32
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases_IsNotStringAliasIOperationTest()
            Dim source = <![CDATA[
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isNotString = TypeOf o IsNot CharacterSequence'BIND:"TypeOf o IsNot CharacterSequence"

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (IsNotExpression) (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o Is ... terSequence')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.String
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub SeesThroughTypeAndNamespaceAliases_IsNotIntegerAliasIOperationTest()
            Dim source = <![CDATA[
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isNotInteger = TypeOf o IsNot HRESULT'BIND:"TypeOf o IsNot HRESULT"

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIsTypeOperation (IsNotExpression) (OperationKind.IsType, Type: System.Boolean) (Syntax: 'TypeOf o IsNot HRESULT')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  IsType: System.Int32
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of TypeOfExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub ReturnsExpectedValuesFromSemanticModelApi()

            Dim source =
<compilation name="ReturnsExpectedValuesFromSemanticModelApi">
    <file name="Program.vb">
Option Strict On

Imports HRESULT = System.Int32
Imports CharacterSequence = System.String

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is CharacterSequence '0

        Dim isInteger = TypeOf o Is HRESULT '1

        Dim isNotString = TypeOf o IsNot String '2

        Dim isNotInteger As Object = TypeOf o IsNot Integer '3

        If TypeOf CObj(isString) Is Boolean Then '4
            System.Console.WriteLine(True)
        End If

        System.Console.WriteLine(isString)
        System.Console.WriteLine(isInteger)
        System.Console.WriteLine(isNotString)
        System.Console.WriteLine(isNotInteger)

        System.Console.WriteLine(TypeOf "" Is String) '5

    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            Dim semantics = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            Dim typeOfExpressions = compilation.SyntaxTrees(0).GetCompilationUnitRoot().DescendantNodes.OfType(Of TypeOfExpressionSyntax).ToArray()

            ' Dim isString = TypeOf o Is CharacterSequence '0
            Assert.Equal("System.Boolean", semantics.GetTypeInfo(typeOfExpressions(0)).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("System.String", semantics.GetSymbolInfo(typeOfExpressions(0).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isInteger = TypeOf o Is HRESULT '1
            Dim aliasSymbol = semantics.GetAliasInfo(CType(typeOfExpressions(1).Type, IdentifierNameSyntax))
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind)
            Assert.Equal("HRESULT", aliasSymbol.Name)
            Assert.Equal("System.Int32", semantics.GetSymbolInfo(typeOfExpressions(1).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isNotString = TypeOf o IsNot String '2
            Assert.Equal("System.Boolean", semantics.GetTypeInfo(typeOfExpressions(2)).Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("System.String", semantics.GetSymbolInfo(typeOfExpressions(2).Type).Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Dim isNotInteger As Object = TypeOf o IsNot Integer '3
            Dim typeInfo = semantics.GetTypeInfo(typeOfExpressions(3))
            Dim conv = semantics.GetConversion(typeOfExpressions(3))
            Assert.Equal(ConversionKind.WideningValue, conv.Kind)
            Assert.Equal(SpecialType.System_Object, typeInfo.ConvertedType.SpecialType)

            ' If TypeOf CObj(isString) Is Boolean Then '4
            Dim symbolInfo = semantics.GetSymbolInfo(CType(typeOfExpressions(4).Expression, PredefinedCastExpressionSyntax).Expression)
            Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind)
            Assert.Equal("isString", symbolInfo.Symbol.Name)

            Dim expressionAnalysis = semantics.AnalyzeDataFlow(typeOfExpressions(4))

            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.AlwaysAssigned)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.Captured)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.DataFlowsIn)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.DataFlowsOut)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.ReadInside)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.ReadOutside)
            Assert.True(expressionAnalysis.Succeeded)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.VariablesDeclared)
            Assert.DoesNotContain(symbolInfo.Symbol, expressionAnalysis.WrittenInside)
            Assert.Contains(symbolInfo.Symbol, expressionAnalysis.WrittenOutside)

            Dim statementDataAnalysis = semantics.AnalyzeDataFlow(CType(typeOfExpressions(4).Parent.Parent, StatementSyntax))

            AssertSequenceEqual(expressionAnalysis.AlwaysAssigned, statementDataAnalysis.AlwaysAssigned)
            AssertSequenceEqual(expressionAnalysis.Captured, statementDataAnalysis.Captured)
            AssertSequenceEqual(expressionAnalysis.DataFlowsIn, statementDataAnalysis.DataFlowsIn)
            AssertSequenceEqual(expressionAnalysis.DataFlowsOut, statementDataAnalysis.DataFlowsOut)
            AssertSequenceEqual(expressionAnalysis.ReadInside, statementDataAnalysis.ReadInside)
            AssertSequenceEqual(expressionAnalysis.ReadOutside, statementDataAnalysis.ReadOutside)
            Assert.Equal(expressionAnalysis.Succeeded, statementDataAnalysis.Succeeded)
            AssertSequenceEqual(expressionAnalysis.VariablesDeclared, statementDataAnalysis.VariablesDeclared)
            AssertSequenceEqual(expressionAnalysis.WrittenInside, statementDataAnalysis.WrittenInside)
            AssertSequenceEqual(expressionAnalysis.WrittenOutside, statementDataAnalysis.WrittenOutside)

            Assert.False(semantics.GetConstantValue(typeOfExpressions(5)).HasValue)

        End Sub

        Private Shared Sub AssertSequenceEqual(Of TElement)(a1 As ImmutableArray(Of TElement), a2 As ImmutableArray(Of TElement))
            Assert.Equal(a1.Length, a2.Length)
            For i As Integer = 0 To a1.Length - 1
                Assert.Equal(a1(i), a2(i))
            Next
        End Sub

        <Fact>
        Public Sub ExecutesCorrectlyInExpression()
            Dim source =
<compilation name="ExecutesCorrectlyInExpression">
    <file name="Program.vb">
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is String

        Dim isInteger = TypeOf o Is Integer

        Dim isNotString = TypeOf o IsNot String

        Dim isNotInteger = TypeOf o IsNot Integer 

        Console.WriteLine(isString)
        Console.WriteLine(isInteger)
        Console.WriteLine(isNotString)
        Console.WriteLine(isNotInteger)

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, <![CDATA[
True
False
False
True
]]>)
        End Sub

        <Fact>
        Public Sub ExecutesCorrectlyAsIfCondition()
            Dim source =
<compilation name="ExecutedCorrectlyAsIfCondition">
    <file name="Program.vb">
Option Strict On
Imports System
Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        If TypeOf o Is String Then
            Console.WriteLine("It's a String")
        End If

        If TypeOf o Is Integer Then
            Console.WriteLine("It's an Integer")
        End If

        If TypeOf o IsNot String Then
            Console.WriteLine("It's NOT a String")
        End If

        If TypeOf o IsNot Integer Then
            Console.WriteLine("It's NOT an Integer")
        End If

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, , TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation, <![CDATA[
It's a String
It's NOT an Integer
]]>)

        End Sub

        <Fact>
        Public Sub GeneratesILCorrectlyUnderRelease()

            Dim source =
<compilation name="GeneratesILCorrectlyUnderRelease">
    <file name="Program.vb">
Option Strict On

Imports System

Module Program
    Sub Main(args As String())

        Dim o As Object = ""

        Dim isString = TypeOf o Is String
        Console.WriteLine(isString)

        If TypeOf o IsNot Integer Then
            Console.WriteLine(True)
        Else
            Console.WriteLine(False)
        End If

        Dim isInteger = TypeOf o Is Integer
        Console.WriteLine(isInteger)

        If TypeOf o IsNot String Then
            Console.WriteLine(True)
        Else
            Console.WriteLine(False)
        End If

        Console.WriteLine(If(TypeOf o Is Decimal, True, False))
        Console.WriteLine(If(TypeOf o IsNot Decimal, True, False))

    End Sub

    Sub M(Of T, TRef As Class, TVal As Structure, TBase As Class, TDerived As TBase)()

        Dim oT As T = Nothing

        If TypeOf oT Is TRef Then
            Console.WriteLine(False)
        End If

        If TypeOf oT Is TVal Then
            Console.WriteLine(False)
        End If

        Dim vVal As TVal = Nothing

        If TypeOf vVal Is TRef Then
            Console.WriteLine(False)
        End If

        If TypeOf oT Is String Then
            Console.WriteLine(False)
        End If

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(source).VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      111 (0x6f)
  .maxstack  3
  IL_0000:  ldstr      ""
  IL_0005:  dup
  IL_0006:  isinst     "String"
  IL_000b:  ldnull
  IL_000c:  cgt.un
  IL_000e:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0013:  dup
  IL_0014:  isinst     "Integer"
  IL_0019:  brtrue.s   IL_0023
  IL_001b:  ldc.i4.1
  IL_001c:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0021:  br.s       IL_0029
  IL_0023:  ldc.i4.0
  IL_0024:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0029:  dup
  IL_002a:  isinst     "Integer"
  IL_002f:  ldnull
  IL_0030:  cgt.un
  IL_0032:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0037:  dup
  IL_0038:  isinst     "String"
  IL_003d:  brtrue.s   IL_0047
  IL_003f:  ldc.i4.1
  IL_0040:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0045:  br.s       IL_004d
  IL_0047:  ldc.i4.0
  IL_0048:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_004d:  dup
  IL_004e:  isinst     "Decimal"
  IL_0053:  brtrue.s   IL_0058
  IL_0055:  ldc.i4.0
  IL_0056:  br.s       IL_0059
  IL_0058:  ldc.i4.1
  IL_0059:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_005e:  isinst     "Decimal"
  IL_0063:  brfalse.s  IL_0068
  IL_0065:  ldc.i4.0
  IL_0066:  br.s       IL_0069
  IL_0068:  ldc.i4.1
  IL_0069:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_006e:  ret
}
]]>).VerifyIL("Program.M",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (T V_0,
  TVal V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "T"
  IL_0008:  ldloc.0
  IL_0009:  dup
  IL_000a:  box        "T"
  IL_000f:  isinst     "TRef"
  IL_0014:  brfalse.s  IL_001c
  IL_0016:  ldc.i4.0
  IL_0017:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001c:  dup
  IL_001d:  box        "T"
  IL_0022:  isinst     "TVal"
  IL_0027:  brfalse.s  IL_002f
  IL_0029:  ldc.i4.0
  IL_002a:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_002f:  ldloca.s   V_1
  IL_0031:  initobj    "TVal"
  IL_0037:  ldloc.1
  IL_0038:  box        "TVal"
  IL_003d:  isinst     "TRef"
  IL_0042:  brfalse.s  IL_004a
  IL_0044:  ldc.i4.0
  IL_0045:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_004a:  box        "T"
  IL_004f:  isinst     "String"
  IL_0054:  brfalse.s  IL_005c
  IL_0056:  ldc.i4.0
  IL_0057:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_005c:  ret
}
]]>)

        End Sub

        ' For compatibility with Dev10, "TypeOf t" should be
        ' supported for type parameter with Structure constraint,
        ' and the generated IL should box the argument.
        <Fact()>
        Public Sub TypeParameterWithConstraints01()
            Dim source =
<compilation>
    <file name="Program.vb">
Imports System
Module M
    Sub M(Of T, U As Structure, V As Class)(x As T, y As U, z As V)
        If TypeOf x Is Integer Then
            Console.WriteLine("x")
        End If
        If TypeOf y Is Integer Then
            Console.WriteLine("y")
        End If
        If TypeOf z Is Integer Then
            Console.WriteLine("z")
        End If
    End Sub
    Sub Main()
        M(1, 2.0, DirectCast(3, Object))
        M(1.0, 2, DirectCast(3.0, Object))
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(source, expectedOutput:=<![CDATA[
x
z
y
]]>).VerifyIL("M.M(Of T, U, V)(T, U, V)",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  isinst     "Integer"
  IL_000b:  brfalse.s  IL_0017
  IL_000d:  ldstr      "x"
  IL_0012:  call       "Sub System.Console.WriteLine(String)"
  IL_0017:  ldarg.1
  IL_0018:  box        "U"
  IL_001d:  isinst     "Integer"
  IL_0022:  brfalse.s  IL_002e
  IL_0024:  ldstr      "y"
  IL_0029:  call       "Sub System.Console.WriteLine(String)"
  IL_002e:  ldarg.2
  IL_002f:  box        "V"
  IL_0034:  isinst     "Integer"
  IL_0039:  brfalse.s  IL_0045
  IL_003b:  ldstr      "z"
  IL_0040:  call       "Sub System.Console.WriteLine(String)"
  IL_0045:  ret
}
]]>)
        End Sub

        <WorkItem(543781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543781")>
        <Fact()>
        Public Sub TypeParameterWithConstraints02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="Program.vb">
Structure S1
End Structure
Structure S2
End Structure
MustInherit Class A0(Of T1, T2)
    Public MustOverride Sub M(Of U1 As T1, U2 As T2)(_1 As U1, _2 As U2)
End Class
Class B0
    Inherits A0(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As S1, U2 As S2)(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B0.M
        End If
        If TypeOf _1 Is U2 Then ' B0.M
        End If
    End Sub
End Class
MustInherit Class A1(Of T1, T2)
    Public MustOverride Sub M(Of U1 As {T1, Structure}, U2 As {T2})(_1 As U1, _2 As U2)
End Class
Class B1
    Inherits A1(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As {Structure, S1}, U2 As {S2})(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B1.M
        End If
        If TypeOf _1 Is U2 Then ' B1.M
        End If
        If TypeOf _2 Is S1 Then ' B1.M
        End If
        If TypeOf _2 Is U1 Then ' B1.M
        End If
    End Sub
End Class
MustInherit Class A2(Of T1, T2)
    Public MustOverride Sub M(Of U1 As {T1, Structure}, U2 As {T2, Structure})(_1 As U1, _2 As U2)
End Class
Class B2
    Inherits A2(Of S1, S2)
    Public Overloads Overrides Sub M(Of U1 As {Structure, S1}, U2 As {Structure, S2})(_1 As U1, _2 As U2)
        If TypeOf _1 Is S2 Then ' B2.M
        End If
        If TypeOf _1 Is U2 Then ' B2.M (Dev10 no error)
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B0.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'U2'.
        If TypeOf _1 Is U2 Then ' B0.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'U2'.
        If TypeOf _1 Is U2 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U2' can never be of type 'S1'.
        If TypeOf _2 Is S1 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U2' can never be of type 'U1'.
        If TypeOf _2 Is U1 Then ' B1.M
           ~~~~~~~~~~~~~~~
BC31430: Expression of type 'U1' can never be of type 'S2'.
        If TypeOf _1 Is S2 Then ' B2.M
           ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="Program.vb">
Interface I
End Interface
NotInheritable Class C
End Class
Structure S
End Structure
MustInherit Class A(Of T1, T2)
    Public MustOverride Sub M(Of U1 As T1, U2 As T2)(_1 As U1, _2 As U2)
End Class
Class B1
    Inherits A(Of C, I)
    Public Overrides Sub M(Of U1 As C, U2 As I)(_1 As U1, _2 As U2)
        If TypeOf _1 Is I Then
        End If
        If TypeOf _1 Is U2 Then
        End If
        If TypeOf _2 Is C Then
        End If
        If TypeOf _2 Is U1 Then
        End If
    End Sub
End Class
Class B2
    Inherits A(Of S, I)
    Public Overrides Sub M(Of U1 As S, U2 As I)(_1 As U1, _2 As U2)
        If TypeOf _1 Is I Then
        End If
        If TypeOf _1 Is U2 Then
        End If
        If TypeOf _2 Is S Then
        End If
        If TypeOf _2 Is U1 Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertNoErrors()
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="Program.vb">
MustInherit Class A(Of T)
    Public MustOverride Sub M(Of U As T)(_u As U)
End Class
Class B0
    Inherits A(Of System.Array)
    Public Overrides Sub M(Of U As System.Array)(_u As U)
        If TypeOf _u Is String() Then
        End If
    End Sub
End Class
Class B1
    Inherits A(Of String())
    Public Overrides Sub M(Of U As String())(_u As U)
        If TypeOf _u Is System.Array Then
        End If
        If TypeOf _u Is Integer() Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U' can never be of type 'Integer()'.
        If TypeOf _u Is Integer() Then
           ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TypeParameterWithConstraints05()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="Program.vb">
Enum E
    A
End Enum
MustInherit Class A(Of T)
    Public MustOverride Sub M(Of U As T)(_u As U)
End Class
Class B1
    Inherits A(Of Integer)
    Public Overrides Sub M(Of U As Integer)(_u As U)
        If TypeOf _u Is E Then
        End If
    End Sub
End Class
Class B2
    Inherits A(Of E)
    Public Overrides Sub M(Of U As E)(_u As U)
        If TypeOf _u Is Integer Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(<expected>
BC31430: Expression of type 'U' can never be of type 'E'.
        If TypeOf _u Is E Then
           ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30021ERR_TypeOfRequiresReferenceType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Class C
        Shared Sub M()
            Dim i2 As Integer?
            'COMPILEERROR: BC30021, "i2"
            If TypeOf i2 Is Integer Then
            End If
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30021: 'TypeOf ... Is' requires its left operand to have a reference type, but this operand has the value type 'Integer?'.
            If TypeOf i2 Is Integer Then
                      ~~
</expected>)
        End Sub

        ' For compatibility with Dev10, "TypeOf t" should be
        ' supported for type parameter T, regardless of constraints.
        <Fact()>
        Public Sub BC30021ERR_TypeOfRequiresReferenceType1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C
    Shared Sub M(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
        If TypeOf _1 Is Object Then
        End If
        If TypeOf _2 Is Object Then
        End If
        If TypeOf _3 Is Object Then
        End If
        If TypeOf _4 Is Object Then
        End If
        If TypeOf _5 Is Object Then
        End If
        If TypeOf _6 Is Object Then
        End If
        If TypeOf _7 Is Object Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC31430ERR_TypeOfExprAlwaysFalse2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOfExprAlwaysFalse2">
        <file name="a.vb">
        Module M
            Sub Goo(Of T As Structure)(ByVal x As T)
                Dim y = TypeOf x Is String
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31430: Expression of type 'T' can never be of type 'String'.
                Dim y = TypeOf x Is String
                        ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class
End Namespace
