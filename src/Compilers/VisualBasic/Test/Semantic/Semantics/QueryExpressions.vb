' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class QueryExpressions
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q 
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Select
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub ImplicitSelectClause_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q'BIND:"From s In q"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q')
  Expression: 
    IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'From s In q')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'From s In q')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'From s In q')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'From s In q')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'From s In q')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'From s In q')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Test2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0 
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Where s > 0  Where 10 > s
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where
-----
Where
Where
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub WhereClause_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Where s > 0 Where 10 > s'BIND:"From s In q Where s > 0 Where 10 > s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... here 10 > s')
  Expression: 
    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where 10 > s')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where s > 0')
          Instance Receiver: 
            ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                              Left: 
                                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '10 > s')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: '10 > s')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '10 > s')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '10 > s')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '10 > s')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: '10 > s')
                          Left: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                          Right: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Test3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(Of T, U)(x As Func(Of T, U)) As QueryAble
        System.Console.WriteLine("Where {0}", x.GetType())
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0 
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where System.Func`2[System.Int32,System.Boolean]
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Test4()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(Of T)(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where {0}", x.GetType())
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble, IsInvalid) (Syntax: 'From s In q Where s > 0')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: QueryAble, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
      Children(2):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
            Children(1):
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                    Left: 
                      IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32050: Type parameter 'T' for 'Public Function Where(Of T)(x As Func(Of Integer, Boolean)) As QueryAble' cannot be inferred.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Test5()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(Of T, U)(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where {0}", x.GetType())
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble, IsInvalid) (Syntax: 'From s In q Where s > 0')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: QueryAble, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
      Children(2):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
            Children(1):
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                    Left: 
                      IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32050: Type parameter 'T' for 'Public Function Where(Of T, U)(x As Func(Of Integer, Boolean)) As QueryAble' cannot be inferred.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
BC32050: Type parameter 'U' for 'Public Function Where(Of T, U)(x As Func(Of Integer, Boolean)) As QueryAble' cannot be inferred.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Test6()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(Of T, U)(x As Func(Of T, Action(Of U))) As QueryAble
        System.Console.WriteLine("Where {0}", x.GetType())
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble, IsInvalid) (Syntax: 'From s In q Where s > 0')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: QueryAble, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
      Children(2):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Where s > 0')
            Children(1):
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                    Left: 
                      IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
BC36648: Data type(s) of the type parameter(s) in method 'Public Function Where(Of T, U)(x As Func(Of T, Action(Of U))) As QueryAble' cannot be inferred from these arguments.
        Dim q1 As Object = From s In q Where s > 0'BIND:"From s In q Where s > 0"
                                       ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Test7()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble1
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Class QueryAble2

    Function AsQueryable() As QueryAble1
        System.Console.WriteLine("AsQueryable")
        Return New QueryAble1()
    End Function

    Function AsEnumerable() As QueryAble1
        System.Console.WriteLine("AsEnumerable")
        Return New QueryAble1()
    End Function

    Function Cast(Of T)() As QueryAble2
        System.Console.WriteLine("Cast")
        Return Me
    End Function
End Class

Class C
    Function [Select](ByRef f As Func(Of String, String)) As C
        System.Console.WriteLine("[Select](ByRef f As Func(Of String, String))")
        Return Me
    End Function

    Function [Select](ByRef f As Func(Of Integer, String)) As C
        System.Console.WriteLine("[Select](ByRef f As Func(Of Integer, String))")
        Return Me
    End Function

    Function [Select](ByVal f As Func(Of Integer, Integer)) As C
        System.Console.WriteLine("[Select](ByVal f As Func(Of Integer, Integer))")
        Return Me
    End Function
End Class
 
Module Module1
    Sub Main()
        Dim q As New QueryAble2()
        Dim q1 As Object = From s In q
        Dim y = From z In New C Select z Select z = z.ToString() Select z.ToUpper()
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
AsQueryable
[Select](ByVal f As Func(Of Integer, Integer))
[Select](ByRef f As Func(Of Integer, String))
[Select](ByRef f As Func(Of String, String))
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub MultipleSelectClauses_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble1
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Class QueryAble2

    Function AsQueryable() As QueryAble1
        System.Console.WriteLine("AsQueryable")
        Return New QueryAble1()
    End Function

    Function AsEnumerable() As QueryAble1
        System.Console.WriteLine("AsEnumerable")
        Return New QueryAble1()
    End Function

    Function Cast(Of T)() As QueryAble2
        System.Console.WriteLine("Cast")
        Return Me
    End Function
End Class

Class C
    Function [Select](ByRef f As Func(Of String, String)) As C
        System.Console.WriteLine("[Select](ByRef f As Func(Of String, String))")
        Return Me
    End Function

    Function [Select](ByRef f As Func(Of Integer, String)) As C
        System.Console.WriteLine("[Select](ByRef f As Func(Of Integer, String))")
        Return Me
    End Function

    Function [Select](ByVal f As Func(Of Integer, Integer)) As C
        System.Console.WriteLine("[Select](ByVal f As Func(Of Integer, Integer))")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim y = From z In New C Select z Select z = z.ToString() Select z.ToUpper()'BIND:"From z In New C Select z Select z = z.ToString() Select z.ToUpper()"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: C) (Syntax: 'From z In N ... z.ToUpper()')
  Expression: 
    IInvocationOperation ( Function C.Select(ByRef f As System.Func(Of System.String, System.String)) As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Select z.ToUpper()')
      Instance Receiver: 
        IInvocationOperation ( Function C.Select(ByRef f As System.Func(Of System.Int32, System.String)) As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Select z = z.ToString()')
          Instance Receiver: 
            IInvocationOperation ( Function C.Select(f As System.Func(Of System.Int32, System.Int32)) As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Select z')
              Instance Receiver: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C')
                  Arguments(0)
                  Initializer: 
                    null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: f) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'z')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'z')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (z As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'z')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'z')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'z')
                              ReturnedValue: 
                                IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: f) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'z.ToString()')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.String), IsImplicit) (Syntax: 'z.ToString()')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (z As System.Int32) As System.String) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'z.ToString()')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'z.ToString()')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'z.ToString()')
                          ReturnedValue: 
                            IInvocationOperation (virtual Function System.Int32.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'z.ToString()')
                              Instance Receiver: 
                                IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'z')
                              Arguments(0)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: f) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'z.ToUpper()')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.String, System.String), IsImplicit) (Syntax: 'z.ToUpper()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (z As System.String) As System.String) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'z.ToUpper()')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'z.ToUpper()')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'z.ToUpper()')
                      ReturnedValue: 
                        IInvocationOperation ( Function System.String.ToUpper() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: 'z.ToUpper()')
                          Instance Receiver: 
                            IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.String) (Syntax: 'z')
                          Arguments(0)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Test8()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble1
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Class QueryAble2

    Function AsEnumerable() As QueryAble1
        System.Console.WriteLine("AsEnumerable")
        Return New QueryAble1()
    End Function

    Function Cast(Of T)() As QueryAble2
        System.Console.WriteLine("Cast")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble2()
        Dim q1 As Object = From s In q
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
AsEnumerable
]]>)

        End Sub

        <Fact>
        Public Sub Test9()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble1
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Class QueryAble2
    Function Cast(Of T)() As QueryAble2
        System.Console.WriteLine("Cast")
        Return Me
    End Function

    Public Function Where(Of T)(x As Func(Of T, Boolean)) As QueryAble2
        System.Console.WriteLine("Where {0}", x.GetType())
        x.Invoke(CType(CObj(1), T))
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble2()
        Dim q1 As Object = From s In q
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Where s > 0
        System.Console.WriteLine("-----")
        Dim x as Object = new Object()
        Dim q3 As Object = From s In q Where DirectCast(Function() s > 0 AndAlso x IsNot Nothing, Func(Of Boolean)).Invoke()
        System.Console.WriteLine("-----")
        Dim q4 As Object = From s In q Where (From s1 In q Where s > s1 ) IsNot Nothing
        System.Console.WriteLine("-----")
        Dim q5 As Object = From s In q Where DirectCast(Function() 
                                                            System.Console.WriteLine(s)
                                                            System.Console.WriteLine(PassByRef1(s))
                                                            System.Console.WriteLine(s)
                                                            System.Console.WriteLine(PassByRef2(s))
                                                            System.Console.WriteLine(s)
                                                            return True
                                                        End Function, Func(Of Boolean)).Invoke()
    End Sub

    Function PassByRef1(ByRef x as Object) As Integer
        x=x+1
        Return x
    End Function 

    Function PassByRef2(ByRef x as Short) As Integer
        x=x+1
        Return x
    End Function 
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Cast
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
1
2
1
2
1
]]>)

        End Sub

        <Fact>
        Public Sub Test10()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble2
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble2()
        Dim q1 As Object = From s In q
        Dim q2 As Object = From s In q Where s.Goo()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble2' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q1 As Object = From s In q
                                     ~
BC36593: Expression of type 'QueryAble2' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q2 As Object = From s In q Where s.Goo()
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub Test11()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Integer) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Integer)) As QueryAble
        Return Nothing
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where
]]>)

        End Sub

        <Fact>
        Public Sub Test12()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](Of T)(x As Func(Of T, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function
End Class

Class Test1(Of T)
    Class Test2

    End Class
End Class

Class QueryAble2
    Public Function [Select](Of T)(x As Func(Of Test1(Of T).Test2, Integer)) As QueryAble2
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Integer)) As QueryAble
        Return Nothing
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
        Dim q2 As Object = From s In New QueryAble2()
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q1 As Object = From s In q Where s > 0
                                     ~
BC36593: Expression of type 'QueryAble2' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q2 As Object = From s In New QueryAble2()
                                     ~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test13()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select]() As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Integer)) As QueryAble
        Return Nothing
    End Function

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](this As QueryAble, x As Func(Of Long, Integer)) As QueryAble
        Return Nothing
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q1 As Object = From s In q Where s > 0
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub Test14()

            Dim customIL = <![CDATA[
.class public auto ansi sealed WithOpt
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method WithOpt::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(int32 x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method WithOpt::BeginInvoke

  .method public newslot strict virtual instance int32 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method WithOpt::EndInvoke

  .method public newslot strict virtual instance int32 
          Invoke([opt] int32 x) runtime managed
  {
  } // end of method WithOpt::Invoke

} // end of class WithOpt


.class public auto ansi sealed WithParamArray
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method WithParamArray::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(int32 x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method WithParamArray::BeginInvoke

  .method public newslot strict virtual instance int32 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method WithParamArray::EndInvoke

  .method public newslot strict virtual instance int32 
          Invoke(int32 x) runtime managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
  } // end of method WithParamArray::Invoke

} // end of class WithParamArray
]]>

            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Delegate Function WithByRef(ByRef x As Integer) As Integer

Class QueryAble
    Public Sub [Select](x As Func(Of Integer, Integer))
        System.Console.WriteLine("Select")
    End Sub

    Public Function [Select](x As Action(Of Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function [Select](x As Func(Of Integer, Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function [Select](x As WithByRef) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function [Select](x As WithOpt) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function [Select](x As WithParamArray) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, customIL.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q1 As Object = From s In q Where s > 0
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub Test15()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0 
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test16()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q5 As Object = From s In q Where DirectCast(Function() 
                                                            s = 1
                                                            return True
                                                        End Function, Func(Of Boolean)).Invoke()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                                                            s = 1
                                                            ~
</expected>)

        End Sub

        <Fact>
        Public Sub Test17()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](Of T)(x As Func(Of Func(Of T()), Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q1 As Object = From s In q Where s > 0
                                     ~
</expected>)

        End Sub

        <Fact>
        Public Sub Test18()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As System.Delegate) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where(x As System.MulticastDelegate) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where(x As Object) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where(x As Action(Of Integer)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Delegate Function WithByRef(ByRef x As Integer) As Boolean

    Public Function Where(x As WithByRef) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where(x As Func(Of Byte, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Where' can be called with these arguments:
    'Public Function Where(x As [Delegate]) As QueryAble': Lambda expression cannot be converted to '[Delegate]' because type '[Delegate]' is declared 'MustInherit' and cannot be created.
    'Public Function Where(x As MulticastDelegate) As QueryAble': Lambda expression cannot be converted to 'MulticastDelegate' because type 'MulticastDelegate' is declared 'MustInherit' and cannot be created.
    'Public Function Where(x As Object) As QueryAble': Lambda expression cannot be converted to 'Object' because 'Object' is not a delegate type.
    'Public Function Where(x As Action(Of Integer)) As QueryAble': Nested function does not have the same signature as delegate 'Action(Of Integer)'.
    'Public Function Where(x As Func(Of Integer, Integer, Boolean)) As QueryAble': Nested function does not have the same signature as delegate 'Func(Of Integer, Integer, Boolean)'.
    'Public Function Where(x As QueryAble.WithByRef) As QueryAble': Nested function does not have the same signature as delegate 'QueryAble.WithByRef'.
    'Public Function Where(x As Func(Of Byte, Boolean)) As QueryAble': Nested function does not have the same signature as delegate 'Func(Of Byte, Boolean)'.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test19()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Sub Where(x As Func(Of Integer, Boolean))
    End Sub

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test20()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where() As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC30057: Too many arguments to 'Public Function Where() As QueryAble'.
        Dim q1 As Object = From s In q Where s > 0
                                             ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test21()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean), Optional y As Integer = 0) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'Where' accepts this number of arguments.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test22()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean), Optional y As Integer = 0) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function Where() As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'Where' accepts this number of arguments.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test23()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean), ParamArray y As Integer()) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'Where' accepts this number of arguments.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test24()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(ParamArray x As Func(Of Integer, Boolean)()) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
BC30589: Argument cannot match a ParamArray parameter.
        Dim q1 As Object = From s In q Where s > 0
                                             ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test25()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public ReadOnly Property Where As QueryAble
        Get
            Return Nothing
        End Get
    End Property
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Where(this As QueryAble, x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return this
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where
]]>)

        End Sub
        <Fact>
        Public Sub Test26()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Where As QueryAble
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Where(this As QueryAble, x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return this
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where
]]>)

        End Sub

        <Fact>
        Public Sub Test27()

            Dim customIL = <![CDATA[
.class interface public abstract auto ansi IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Where(class [mscorlib]System.Func`2<int32,bool> x) cil managed
  {
  } // end of method IBase::Where1

} // end of class IBase

.class interface public abstract auto ansi IQueryAble1
       implements IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Select(class [mscorlib]System.Func`2<int32,int32> x) cil managed
  {
  } // end of method IQueryAble1::Select

  .method public newslot specialname abstract strict virtual 
          instance int32  get_Where() cil managed
  {
  } // end of method IQueryAble1::get_Where

  .property instance int32 Where()
  {
    .get instance int32 IQueryAble1::get_Where()
  } // end of property IQueryAble1::Where
} // end of class IQueryAble1
]]>

            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim q As IQueryAble1 = Nothing
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, customIL.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test28()

            Dim customIL = <![CDATA[
.class interface public abstract auto ansi IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Where(class [mscorlib]System.Func`2<int32,bool> x) cil managed
  {
  } // end of method IBase::Where1

} // end of class IBase

.class interface public abstract auto ansi IQueryAble1
       implements IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Select(class [mscorlib]System.Func`2<int32,int32> x) cil managed
  {
  } // end of method IQueryAble1::Select

  .method public newslot specialname abstract strict virtual 
          instance int32  get_Where() cil managed
  {
  } // end of method IQueryAble1::get_Where

  .property instance int32 Where()
  {
    .get instance int32 IQueryAble1::get_Where()
  } // end of property IQueryAble1::Where
} // end of class IQueryAble1
]]>

            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Where(this As IQueryAble1, x As Func(Of Integer, Boolean)) As IQueryAble1
        System.Console.WriteLine("Where")
        Return this
    End Function

    Sub Main()
        Dim q As IQueryAble1 = Nothing
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, customIL.Value, options:=TestOptions.ReleaseExe, includeVbRuntime:=True)

            CompileAndVerify(compilation, expectedOutput:="Where")

        End Sub

        <Fact>
        Public Sub Test29()

            Dim customIL = <![CDATA[
.class interface public abstract auto ansi IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Where(class [mscorlib]System.Func`2<int32,bool> x) cil managed
  {
  } // end of method IBase::Where1

} // end of class IBase

.class interface public abstract auto ansi IQueryAble1
       implements IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Select(class [mscorlib]System.Func`2<int32,int32> x) cil managed
  {
  } // end of method IQueryAble1::Select

  .method public hidebysig newslot specialname abstract strict virtual 
          instance int32  get_Where() cil managed
  {
  } // end of method IQueryAble1::get_Where

  .property instance int32 Where()
  {
    .get instance int32 IQueryAble1::get_Where()
  } // end of property IQueryAble1::Where
} // end of class IQueryAble1
]]>

            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Sub Main()
        Dim q As IQueryAble1 = Nothing
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, customIL.Value, includeVbRuntime:=True)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test30()

            Dim customIL = <![CDATA[
.class interface public abstract auto ansi IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Where(class [mscorlib]System.Func`2<int32,bool> x) cil managed
  {
  } // end of method IBase::Where1

} // end of class IBase

.class interface public abstract auto ansi IQueryAble1
       implements IBase
{
  .method public newslot abstract strict virtual 
          instance class IQueryAble1  Select(class [mscorlib]System.Func`2<int32,int32> x) cil managed
  {
  } // end of method IQueryAble1::Select

  .method public hidebysig newslot specialname abstract strict virtual 
          instance int32  get_Where() cil managed
  {
  } // end of method IQueryAble1::get_Where

  .property instance int32 Where()
  {
    .get instance int32 IQueryAble1::get_Where()
  } // end of property IQueryAble1::Where
} // end of class IQueryAble1
]]>

            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Where(this As IQueryAble1, x As Func(Of Integer, Boolean)) As IQueryAble1
        System.Console.WriteLine("Where")
        Return this
    End Function

    Sub Main()
        Dim q As IQueryAble1 = Nothing
        Dim q1 As Object = From s In q Where s > 0
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(compilationDef, customIL.Value, TestOptions.ReleaseExe, includeVbRuntime:=True)

            CompileAndVerify(compilation, expectedOutput:="Where")

        End Sub

        <Fact>
        Public Sub Select1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine()
        System.Console.WriteLine("Select")
        System.Console.Write(x(1))
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine()
        System.Console.WriteLine("Where")
        System.Console.Write(x(2))
        Return Me
    End Function
End Class

Module Module1

    Function Num1() As Integer
        System.Console.WriteLine("Num1")
        Return -10
    End Function

    Function Num2() As Integer
        System.Console.WriteLine("Num2")
        Return -20
    End Function

    Class Index
        Default Property Item(x As String) As Integer
            Get
                System.Console.WriteLine("Item {0}", x)
                Return 100
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Sub Main()
        Dim q As New QueryAble()
        System.Console.WriteLine("-----")
        Dim q1 As Object = From s In q Select t = s * 2 Select t
        System.Console.WriteLine()
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Select s * 3 Where 100 Select -1
        System.Console.WriteLine()
        System.Console.WriteLine("-----")
        Dim ind As New Index()

        Dim q3 As Object = From s In q
                           Select s
                           Where s > 0
                           Select Num1
                           Where Num1 = -10
                           Select Module1.Num2()
                           Where Num2 = -10 + Num1()
                           Select ind!Two
                           Where Two > 0

        System.Console.WriteLine()
        System.Console.WriteLine("-----")
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
-----

Select
2
Select
1
-----

Select
3
Where
True
Select
-1
-----

Select
1
Where
True
Select
Num1
-10
Where
False
Select
Num2
-20
Where
Num1
False
Select
Item Two
100
Where
True
-----
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Select1_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine()
        System.Console.WriteLine("Select")
        System.Console.Write(x(1))
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine()
        System.Console.WriteLine("Where")
        System.Console.Write(x(2))
        Return Me
    End Function
End Class

Module Module1

    Function Num1() As Integer
        System.Console.WriteLine("Num1")
        Return -10
    End Function

    Function Num2() As Integer
        System.Console.WriteLine("Num2")
        Return -20
    End Function

    Class Index
        Default Property Item(x As String) As Integer
            Get
                System.Console.WriteLine("Item {0}", x)
                Return 100
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class

    Sub Main()
        Dim q As New QueryAble()
        Dim ind As New Index()

        Dim q3 As Object = From s In q'BIND:"From s In q"
                           Select s
                           Where s > 0
                           Select Num1()
                           Where Num1 = -10
                           Select Module1.Num2()
                           Where Num2 = -10 + Num1()
                           Select ind!Two
                           Where Two > 0
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... ere Two > 0')
  Expression: 
    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where Two > 0')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select ind!Two')
          Instance Receiver: 
            IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where Num2  ... 10 + Num1()')
              Instance Receiver: 
                IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select Module1.Num2()')
                  Instance Receiver: 
                    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where Num1 = -10')
                      Instance Receiver: 
                        IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select Num1()')
                          Instance Receiver: 
                            IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where s > 0')
                              Instance Receiver: 
                                IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select s')
                                  Instance Receiver: 
                                    ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
                                          Target: 
                                            IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                                  ReturnedValue: 
                                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
                                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
                                      Target: 
                                        IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                                              ReturnedValue: 
                                                IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                                                  Left: 
                                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                                  Right: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Num1()')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'Num1()')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Num1()')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Num1()')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Num1()')
                                          ReturnedValue: 
                                            IInvocationOperation (Function Module1.Num1() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Num1()')
                                              Instance Receiver: 
                                                null
                                              Arguments(0)
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Num1 = -10')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 'Num1 = -10')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (Num1 As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Num1 = -10')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Num1 = -10')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Num1 = -10')
                                      ReturnedValue: 
                                        IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'Num1 = -10')
                                          Left: 
                                            IParameterReferenceOperation: Num1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Num1')
                                          Right: 
                                            IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Int32, Constant: -10) (Syntax: '-10')
                                              Operand: 
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Module1.Num2()')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'Module1.Num2()')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function (Num1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Module1.Num2()')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Module1.Num2()')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Module1.Num2()')
                                  ReturnedValue: 
                                    IInvocationOperation (Function Module1.Num2() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Module1.Num2()')
                                      Instance Receiver: 
                                        null
                                      Arguments(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Num2 = -10 + Num1()')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 'Num2 = -10 + Num1()')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (Num2 As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Num2 = -10 + Num1()')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Num2 = -10 + Num1()')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Num2 = -10 + Num1()')
                              ReturnedValue: 
                                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'Num2 = -10 + Num1()')
                                  Left: 
                                    IParameterReferenceOperation: Num2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Num2')
                                  Right: 
                                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '-10 + Num1()')
                                      Left: 
                                        IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Int32, Constant: -10) (Syntax: '-10')
                                          Operand: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                                      Right: 
                                        IInvocationOperation (Function Module1.Num1() As System.Int32) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Num1()')
                                          Instance Receiver: 
                                            null
                                          Arguments(0)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'ind!Two')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'ind!Two')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (Num2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'ind!Two')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'ind!Two')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'ind!Two')
                          ReturnedValue: 
                            IPropertyReferenceOperation: Property Module1.Index.Item(x As System.String) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'ind!Two')
                              Instance Receiver: 
                                ILocalReferenceOperation: ind (OperationKind.LocalReference, Type: Module1.Index) (Syntax: 'ind')
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Two')
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Two") (Syntax: 'Two')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Two > 0')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 'Two > 0')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (Two As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Two > 0')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Two > 0')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Two > 0')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'Two > 0')
                          Left: 
                            IParameterReferenceOperation: Two (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Two')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Select2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(ParamArray x As Func(Of Integer, Boolean)()) As QueryAble
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
    End Sub
End Module

Class TestClass(Of Name1)

    Sub Name5()
    End Sub

    Sub Test(Of Name2)(name3 As Object)
        Dim q As New QueryAble()
        Dim name4 As Integer = 0

        name7% = 1

        Dim q1 As Object = From x In q Select name1 = x
        Dim q2 As Object = From x In q Select name2 = x
        Dim q3 As Object = From x In q Select name3 = x
        Dim q4 As Object = From x In q Select name4 = x
        Dim q5 As Object = From x In q Select name5 = x
        Dim q6 As Object = From x In q Select getHashcode = x
        Dim q7 As Object = From x In q Select x.GetHashCode()
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
        Dim q9 As Object = From x In q Select name7 = x
        Dim q10 As Object = From x In q Select name8 = x + name8%
        Dim q11 As Object = From x In q Select name9 = x
        Dim q12 As Object = From x In q Select x1 As Integer = x

        name9% = 1
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            Assert.True(compilation.Options.OptionExplicit)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'name7' is not declared. It may be inaccessible due to its protection level.
        name7% = 1
        ~~~~~~
BC32089: 'name2' is already declared as a type parameter of this method.
        Dim q2 As Object = From x In q Select name2 = x
                                              ~~~~~
BC30978: Range variable 'name3' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q3 As Object = From x In q Select name3 = x
                                              ~~~~~
BC30978: Range variable 'name4' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q4 As Object = From x In q Select name4 = x
                                              ~~~~~
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q6 As Object = From x In q Select getHashcode = x
                                              ~~~~~~~~~~~
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q7 As Object = From x In q Select x.GetHashCode()
                                                ~~~~~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                           ~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30978: Range variable 'name6' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                                                      ~~~~~
BC36610: Name 'name8' is either not declared or not in the current scope.
        Dim q10 As Object = From x In q Select name8 = x + name8%
                                                           ~~~~~~
BC36610: Name 'x1' is either not declared or not in the current scope.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                               ~~
BC30205: End of statement expected.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                                  ~~
BC30451: 'name9' is not declared. It may be inaccessible due to its protection level.
        name9% = 1
        ~~~~~~
</expected>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionExplicit(False))

            Assert.False(compilation.Options.OptionExplicit)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32089: 'name2' is already declared as a type parameter of this method.
        Dim q2 As Object = From x In q Select name2 = x
                                              ~~~~~
BC36633: Range variable 'name3' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q3 As Object = From x In q Select name3 = x
                                              ~~~~~
BC36633: Range variable 'name4' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q4 As Object = From x In q Select name4 = x
                                              ~~~~~
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q6 As Object = From x In q Select getHashcode = x
                                              ~~~~~~~~~~~
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q7 As Object = From x In q Select x.GetHashCode()
                                                ~~~~~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                           ~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36633: Range variable 'name6' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q8 As Object = From name6 In q Select (From x In q Select name6 = x)
                                                                      ~~~~~
BC36633: Range variable 'name7' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q9 As Object = From x In q Select name7 = x
                                              ~~~~~
BC36633: Range variable 'name8' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q10 As Object = From x In q Select name8 = x + name8%
                                               ~~~~~
BC36633: Range variable 'name9' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q11 As Object = From x In q Select name9 = x
                                               ~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                        ~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                               ~~
BC36633: Range variable 'x1' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                               ~~
BC42104: Variable 'x1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                               ~~
BC30205: End of statement expected.
        Dim q12 As Object = From x In q Select x1 As Integer = x
                                                  ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Select3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
    End Sub
End Module

Structure TestStruct

    Dim q As QueryAble
    Dim y As Integer

    Delegate Function DD(ByRef z As Integer) As Boolean

    Sub New(ByRef x As Integer)
        Dim q01 As Object = From i In q Select i + x
        Dim q02 As Object = From i In q Select i + y
        Dim q1 As New QueryAble()
        Dim q03 As Object = From s In q
                            Where (DirectCast(Function(ByRef z As Integer)
                                                  System.Console.WriteLine(z)
                                                  System.Console.WriteLine(x)
                                                  System.Console.WriteLine(y)
                                                  Dim ff As Func(Of Integer) = Function() 
                                                                                   Dim q04 As Object = From i In q1 Select i + z
                                                                                   Return x + y + z
                                                                               End Function
                                                  Dim q05 As Object = From i In q1 Select i + z
                                                  Return True
                                              End Function, DD).Invoke(1))

        Dim gg As DD = Function(ByRef z As Integer)
                           System.Console.WriteLine(y)
                           Dim q06 As Object = From i In q1 Select i + y
                           System.Console.WriteLine(x)
                           Dim q07 As Object = From i In q1 Select i + x
                           Return True
                       End Function

        For ii As Integer = 1 To 1
            Dim q101 As Object = From i In q Select ii + i
        Next
    End Sub

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36533: 'ByRef' parameter 'x' cannot be used in a query expression.
        Dim q01 As Object = From i In q Select i + x
                                                   ~
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
        Dim q02 As Object = From i In q Select i + y
                                                   ~
BC36533: 'ByRef' parameter 'x' cannot be used in a query expression.
                                                  System.Console.WriteLine(x)
                                                                           ~
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
                                                  System.Console.WriteLine(y)
                                                                           ~
BC36639: 'ByRef' parameter 'z' cannot be used in a lambda expression.
                                                                                   Dim q04 As Object = From i In q1 Select i + z
                                                                                                                               ~
BC36533: 'ByRef' parameter 'x' cannot be used in a query expression.
                                                                                   Return x + y + z
                                                                                          ~
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
                                                                                   Return x + y + z
                                                                                              ~
BC36639: 'ByRef' parameter 'z' cannot be used in a lambda expression.
                                                                                   Return x + y + z
                                                                                                  ~
BC36533: 'ByRef' parameter 'z' cannot be used in a query expression.
                                                  Dim q05 As Object = From i In q1 Select i + z
                                                                                              ~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                           System.Console.WriteLine(y)
                                                    ~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                           Dim q06 As Object = From i In q1 Select i + y
                                                                       ~
BC36639: 'ByRef' parameter 'x' cannot be used in a lambda expression.
                           System.Console.WriteLine(x)
                                                    ~
BC36639: 'ByRef' parameter 'x' cannot be used in a lambda expression.
                           Dim q07 As Object = From i In q1 Select i + x
                                                                       ~
BC42327: Using the iteration variable in a query expression may have unexpected results.  Instead, create a local variable within the loop and assign it the value of the iteration variable.
            Dim q101 As Object = From i In q Select ii + i
                                                    ~~
</expected>)

        End Sub

        <Fact>
        <CompilerTrait(CompilerFeature.IOperation)>
        Public Sub Select4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function

End Class

Module Module1

    Function Goo(x As Integer) As Integer
        Return x
    End Function

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Select x% = s
        Dim q2 As Object = From s In q Select Goo(s)
        Dim q3 As Object = From s In q Select  = s
        Dim q4 As Object = From s In q Where Date.Now() Select s
        Dim q5 As Object = From s In q Select s.Equals(0)
        Dim q6 As Object = From s In q Select DoesntExist
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36601: Type characters cannot be used in range variable declarations.
        Dim q1 As Object = From s In q Select x% = s
                                              ~~
BC30201: Expression expected.
        Dim q3 As Object = From s In q Select  = s
                                               ~
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
        Dim q4 As Object = From s In q Where Date.Now() Select s
                                             ~~~~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q5 As Object = From s In q Select s.Equals(0)
                                       ~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q5 As Object = From s In q Select s.Equals(0)
                                              ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        Dim q6 As Object = From s In q Select DoesntExist
                                              ~~~~~~~~~~~
</expected>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of WhereClauseSyntax)().Single()

            Assert.Equal("Date.Now()", node.Condition.ToString())

            compilation.VerifyOperationTree(node.Condition, expectedOperationTree:=
            <![CDATA[
IPropertyReferenceOperation: ReadOnly Property System.DateTime.Now As System.DateTime (Static) (OperationKind.PropertyReference, Type: System.DateTime, IsInvalid) (Syntax: 'Date.Now()')
  Instance Receiver: 
    null
]]>.Value)
        End Sub

        <Fact>
        Public Sub Select5()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s > 0 Select s
        Dim q2 As Object = From s In q Select 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s > 0 Select s
                                       ~~~~~
BC30201: Expression expected.
        Dim q2 As Object = From s In q Select 
                                              ~
</expected>)

        End Sub

        <Fact>
        Public Sub From1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s? In q Select s
        Dim q2 As Object = From s() In q Select s
        Dim q3 As Object = From s? As Integer In q Select s
        Dim q4 As Object = From s% In q Select s
        Dim q5 As Object = From s% As Integer In q Select s
        Dim q6 As Object = From s As DoesntExist In q Select s
        Dim q7 As Object = From s As DoesntExist In q Where s > 0
        Dim q8 As Object = From s As DoesntExist In q Select s Where s > 0
        Dim q9 As Object = From s As DoesntExist In q Where s > 0 Select s 
        Dim q10 As Object = From s As DoesntExist In q
        Dim q11 As Object = From s In Nothing
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36629: Nullable type inference is not supported in this context.
        Dim q1 As Object = From s? In q Select s
                                 ~
BC36607: 'In' expected.
        Dim q2 As Object = From s() In q Select s
                                 ~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q3 As Object = From s? As Integer In q Select s
                                   ~~~~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q3 As Object = From s? As Integer In q Select s
                                   ~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q3 As Object = From s? As Integer In q Select s
                                                          ~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q4 As Object = From s% In q Select s
                                ~~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q5 As Object = From s% As Integer In q Select s
                                ~~
BC30002: Type 'DoesntExist' is not defined.
        Dim q6 As Object = From s As DoesntExist In q Select s
                                     ~~~~~~~~~~~
BC30002: Type 'DoesntExist' is not defined.
        Dim q7 As Object = From s As DoesntExist In q Where s > 0
                                     ~~~~~~~~~~~
BC30002: Type 'DoesntExist' is not defined.
        Dim q8 As Object = From s As DoesntExist In q Select s Where s > 0
                                     ~~~~~~~~~~~
BC30002: Type 'DoesntExist' is not defined.
        Dim q9 As Object = From s As DoesntExist In q Where s > 0 Select s 
                                     ~~~~~~~~~~~
BC30002: Type 'DoesntExist' is not defined.
        Dim q10 As Object = From s As DoesntExist In q
                                      ~~~~~~~~~~~
BC36593: Expression of type 'Object' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q11 As Object = From s In Nothing
                                      ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub From2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As DoesntExist
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q10 As Object = From s In q
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'DoesntExist' is not defined.
    Public Function [Select](x As Func(Of Integer, Integer)) As DoesntExist
                                                                ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplicitSelect1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Byte In q
        Dim q2 As Object = From s As Byte In q Select s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q1 As Object = From s As Byte In q
                                  ~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q1 As Object = From s As Byte In q
                                  ~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q2 As Object = From s As Byte In q Select s
                                  ~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q2 As Object = From s As Byte In q Select s
                                  ~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q2 As Object = From s As Byte In q Select s
                                                      ~
</expected>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
        Dim q1 As Object = From s As Byte In q
                                  ~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q1 As Object = From s As Byte In q
                                  ~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q1 As Object = From s As Byte In q
                                  ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
        Dim q2 As Object = From s As Byte In q Select s
                                  ~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q2 As Object = From s As Byte In q Select s
                                  ~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q2 As Object = From s As Byte In q Select s
                                  ~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q2 As Object = From s As Byte In q Select s
                                                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplicitSelect2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble
    Public Function Cast(Of T)() As DoesntExist
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q10 As Object = From s As Guid In q
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30002: Type 'DoesntExist' is not defined.
    Public Function Cast(Of T)() As DoesntExist
                                    ~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Guid'.
        Dim q10 As Object = From s As Guid In q
                                   ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplicitSelect3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q10 As Object = From s As Integer In q Select CType(s, Guid)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36593: Expression of type 'QueryAble' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
        Dim q10 As Object = From s As Integer In q Select CType(s, Guid)
                                                 ~
BC30311: Value of type 'Integer' cannot be converted to 'Guid'.
        Dim q10 As Object = From s As Integer In q Select CType(s, Guid)
                                                                ~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplicitSelect4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("[Select]")
        Return this
    End Function

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
        System.Console.WriteLine("[Where]")
        Return this
    End Function


    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s As Integer In q Where s > 1
        System.Console.WriteLine("------")
        Dim q2 As Object = From s As Long In q Where s > 1
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where
------
[Select]
[Where]
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Where1()
            Dim source = <![CDATA[
Option Strict On

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer?, Integer?)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer?, Boolean?)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s'BIND:"From s In q Where s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble, IsInvalid) (Syntax: 'From s In q Where s')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: QueryAble, IsInvalid, IsImplicit) (Syntax: 'Where s')
      Children(2):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Where s')
            Children(1):
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Nullable(Of System.Int32)) As ?) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 's')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 's')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 's')
                ReturnedValue: 
                  IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsInvalid) (Syntax: 's')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where s'BIND:"From s In q Where s"
                                       ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Boolean?'.
        Dim q1 As Object = From s In q Where s'BIND:"From s In q Where s"
                                             ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Where2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
        Dim q1 As Object = From s In q Where s
                                             ~
</expected>)

        End Sub

        <Fact>
        Public Sub Where3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Date, Boolean?)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Date' cannot be converted to 'Boolean'.
        Dim q1 As Object = From s In q Where s
                                             ~
</expected>)

        End Sub

        <Fact>
        Public Sub Where4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where CObj(s)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where CObj(s)
                                       ~~~~~
BC30311: Value of type 'Boolean' cannot be converted to 'Date'.
        Dim q1 As Object = From s In q Where CObj(s)
                                             ~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Where5()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Date, Date)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Date, Object)) As QueryAble
        System.Console.WriteLine("Where Object")
        Return Me
    End Function

    Public Function Where(x As Func(Of Date, Boolean)) As QueryAble
        System.Console.WriteLine("Where Boolean")
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where CObj(s)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where Boolean
]]>)

        End Sub

        <Fact>
        Public Sub Where6()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("Where Byte")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, SByte)) As QueryAble
        System.Console.WriteLine("Where SByte")
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where 0
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where Byte
]]>)

        End Sub

        <Fact>
        Public Sub Where7()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Long)) As QueryAble
        System.Console.WriteLine("Where Long")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, System.DateTimeKind)) As QueryAble
        System.Console.WriteLine("Where System.DateTimeKind")
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where 0
        q.Where(Function(s) 0)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where Long
Where Long
]]>)

        End Sub

        <Fact>
        Public Sub Where8()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("Where Byte")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, System.DateTimeKind)) As QueryAble
        System.Console.WriteLine("Where System.DateTimeKind")
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where 0
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where System.DateTimeKind
]]>)

        End Sub

        <Fact>
        Public Sub Where9()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, System.DateTimeKind)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where 0
        q.Where(Function(s) 0)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Where' is most specific for these arguments:
    'Public Function Where(x As Func(Of Integer, Byte)) As QueryAble': Not most specific.
    'Public Function Where(x As Func(Of Integer, DateTimeKind)) As QueryAble': Not most specific.
        q.Where(Function(s) 0)
          ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Where10()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, SByte)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where CObj(s)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'Where' can be called without a narrowing conversion:
    'Public Function Where(x As Func(Of Integer, Byte)) As QueryAble': Return type of nested function matching parameter 'x' narrows from 'Boolean' to 'Byte'.
    'Public Function Where(x As Func(Of Integer, SByte)) As QueryAble': Return type of nested function matching parameter 'x' narrows from 'Boolean' to 'SByte'.
        Dim q1 As Object = From s In q Where CObj(s)
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where CObj(s)
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Where10b()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Linq.Expressions

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Expression(Of Func(Of Integer, Byte))) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Expression(Of Func(Of Integer, SByte))) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Where CObj(s)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'Where' can be called without a narrowing conversion:
    'Public Function Where(x As Expression(Of Func(Of Integer, Byte))) As QueryAble': Return type of nested function matching parameter 'x' narrows from 'Boolean' to 'Byte'.
    'Public Function Where(x As Expression(Of Func(Of Integer, SByte))) As QueryAble': Return type of nested function matching parameter 'x' narrows from 'Boolean' to 'SByte'.
        Dim q1 As Object = From s In q Where CObj(s)
                                       ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        Dim q1 As Object = From s In q Where CObj(s)
                                       ~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Where11()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Where Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where System.Func`2[System.Int32,System.Boolean]
]]>)
        End Sub

        <Fact>
        Public Sub Where12()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)()
    End Function

    Public Function Where(x As Func(Of T, Object)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)()
    End Function

    Public Function Where(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Where Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Where System.Func`2[System.Int32,System.Boolean]
]]>)
        End Sub

        <Fact>
        Public Sub Where13()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Where(x As Func(Of T, String)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Where Nothing
    End Sub
End Module
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom),
                                expectedOutput:=
            <![CDATA[
Where System.Func`2[System.Int32,System.String]
]]>)

            CompilationUtils.AssertTheseDiagnostics(verifier.Compilation,
<expected>
BC42016: Implicit conversion from 'Boolean' to 'String'.
        q = From s In q1 Where Nothing
                               ~~~~~~~
</expected>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub While1_TakeWhile()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Take While s > 1'BIND:"From s In q Take While s > 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s In q ... While s > 1')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Take While s > 1')
      Children(2):
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 1')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 1')
                    Left: 
                      IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'TakeWhile' is not accessible in this context.
        Dim q1 As Object = From s In q Take While s > 1'BIND:"From s In q Take While s > 1"
                                       ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub While1_SkipWhile()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Skip While s > 1'BIND:"From s In q Skip While s > 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s In q ... While s > 1')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Skip While s > 1')
      Children(2):
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 1')
                ReturnedValue: 
                  IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 1')
                    Left: 
                      IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q2 As Object = From s In q Skip While s > 1'BIND:"From s In q Skip While s > 1"
                                       ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub While2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("TakeWhile")
        Return Me
    End Function

    Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("SkipWhile")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Skip While s > 0 
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Take While s > 0 
        System.Console.WriteLine("-----")
        Dim q3 As Object = From s In q Skip While s > 0 Take While 10 > s Skip While s > 0 Select s
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
SkipWhile
-----
TakeWhile
-----
SkipWhile
TakeWhile
SkipWhile
Select
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub SkipWhile_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("TakeWhile")
        Return Me
    End Function

    Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("SkipWhile")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Skip While s > 0'BIND:"From s In q Skip While s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... While s > 0')
  Expression: 
    IInvocationOperation ( Function QueryAble.SkipWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Skip While s > 0')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                          Left: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub TakeWhile_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("TakeWhile")
        Return Me
    End Function

    Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("SkipWhile")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Take While s > 0'BIND:"From s In q Take While s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... While s > 0')
  Expression: 
    IInvocationOperation ( Function QueryAble.TakeWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Take While s > 0')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                          Left: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub TakeWhileAndSkipWhile_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

    Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("TakeWhile")
        Return Me
    End Function

    Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("SkipWhile")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q3 As Object = From s In q Skip While s > 0 Take While 10 > s Skip While s > 0 Select s'BIND:"From s In q Skip While s > 0 Take While 10 > s Skip While s > 0 Select s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ...  0 Select s')
  Expression: 
    IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select s')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.SkipWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Skip While s > 0')
          Instance Receiver: 
            IInvocationOperation ( Function QueryAble.TakeWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Take While 10 > s')
              Instance Receiver: 
                IInvocationOperation ( Function QueryAble.SkipWhile(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Skip While s > 0')
                  Instance Receiver: 
                    ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                                  ReturnedValue: 
                                    IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                                      Left: 
                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                      Right: 
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '10 > s')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: '10 > s')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '10 > s')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '10 > s')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '10 > s')
                              ReturnedValue: 
                                IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: '10 > s')
                                  Left: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                                  Right: 
                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                              Left: 
                                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Distinct1()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Distinct'BIND:"From s In q Distinct"
        Dim q2 As Object = From s In q Skip While s > 1 Distinct
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s In q Distinct')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Distinct')
      Children(1):
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'Distinct' is not accessible in this context.
        Dim q1 As Object = From s In q Distinct'BIND:"From s In q Distinct"
                                       ~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q2 As Object = From s In q Skip While s > 1 Distinct
                                       ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Distinct2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Distinct() As QueryAble
        System.Console.WriteLine("Distinct")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Distinct
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Select s + 1 Distinct Distinct
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Distinct
-----
Select
Distinct
Distinct
]]>)

        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Distinct2_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Distinct() As QueryAble
        System.Console.WriteLine("Distinct")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Distinct'BIND:"From s In q Distinct"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q Distinct')
  Expression: 
    IInvocationOperation ( Function QueryAble.Distinct() As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Distinct')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub MultipleDistinct_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Distinct() As QueryAble
        System.Console.WriteLine("Distinct")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Select s + 1 Distinct Distinct'BIND:"From s In q Select s + 1 Distinct Distinct"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... ct Distinct')
  Expression: 
    IInvocationOperation ( Function QueryAble.Distinct() As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Distinct')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.Distinct() As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Distinct')
          Instance Receiver: 
            IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select s + 1')
              Instance Receiver: 
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's + 1')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's + 1')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's + 1')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's + 1')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's + 1')
                              ReturnedValue: 
                                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's + 1')
                                  Left: 
                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(0)
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub SkipTake1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Skip 1
        Dim q2 As Object = From s In q Skip While s > 1 Take 2
        Dim q3 As Object = From s In q Skip While s > 1 Take DoesntExist
        Dim q4 As Object = From s In q Take s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Skip' is not accessible in this context.
        Dim q1 As Object = From s In q Skip 1
                                       ~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q2 As Object = From s In q Skip While s > 1 Take 2
                                       ~~~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q3 As Object = From s In q Skip While s > 1 Take DoesntExist
                                       ~~~~~~~~~~
BC30451: 'DoesntExist' is not declared. It may be inaccessible due to its protection level.
        Dim q3 As Object = From s In q Skip While s > 1 Take DoesntExist
                                                             ~~~~~~~~~~~
BC30451: 's' is not declared. It may be inaccessible due to its protection level.
        Dim q4 As Object = From s In q Take s
                                            ~
</expected>)

        End Sub

        <Fact>
        Public Sub SkipTake2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Skip(count As Date) As QueryAble
        System.Console.WriteLine("Skip {0}", count.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        Return Me
    End Function

    Public Function Take(count As Integer) As QueryAble
        System.Console.WriteLine("Skip {0}", count)
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q Skip #12:00:00 AM#
        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Take 1 Select s
        System.Console.WriteLine("-----")
        Dim q3 As Object = From s In q Select s + 1 Take 2
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Skip 1/1/0001 12:00:00 AM
-----
Skip 1
Select
-----
Select
Skip 2
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Skip_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Skip(count As Date) As QueryAble
        System.Console.WriteLine("Skip {0}", count.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        Return Me
    End Function

    Public Function Take(count As Integer) As QueryAble
        System.Console.WriteLine("Skip {0}", count)
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q1 As Object = From s In q Skip #12:00:00 AM#'BIND:"From s In q Skip #12:00:00 AM#"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... 2:00:00 AM#')
  Expression: 
    IInvocationOperation ( Function QueryAble.Skip(count As System.DateTime) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Skip #12:00:00 AM#')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '#12:00:00 AM#')
            ILiteralOperation (OperationKind.Literal, Type: System.DateTime, Constant: 01/01/0001 00:00:00) (Syntax: '#12:00:00 AM#')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Take_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Skip(count As Date) As QueryAble
        System.Console.WriteLine("Skip {0}", count.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
        Return Me
    End Function

    Public Function Take(count As Integer) As QueryAble
        System.Console.WriteLine("Skip {0}", count)
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Take 1 Select s'BIND:"From s In q Take 1 Select s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ...  1 Select s')
  Expression: 
    IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select s')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.Take(count As System.Int32) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Take 1')
          Instance Receiver: 
            ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub SkipTake3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Skip(x As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Skip Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Skip 0
]]>)
        End Sub

        <Fact>
        Public Sub OrderBy1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Order s
        Dim q2 As Object = From s In q Order By s Descending
        Dim q3 As Object = From s In q Order By s Ascending
        Dim q4 As Object = From s In q Skip While s > 1 Order By s
        Dim q5 As Object = From s In q Skip While s > 1 Order By s Descending
        Dim q6 As Object = From s In q Skip While s > 1 Order By s Ascending
        Dim q7 As Object = From s In q Skip While s > 1 Order By DoesntExist
        Dim q8 As Object = From s In q Skip While s > 1 Order By DoesntExist1, DoesntExist2
        Dim q9 As Object = From s In q Order By s, s
        Dim q10 As Object = From s In q Order By s, DoesntExist
        Dim q11 As Object = From s In q Order By :
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'OrderBy' is not accessible in this context.
        Dim q1 As Object = From s In q Order s
                                       ~~~~~
BC36605: 'By' expected.
        Dim q1 As Object = From s In q Order s
                                             ~
BC36594: Definition of method 'OrderByDescending' is not accessible in this context.
        Dim q2 As Object = From s In q Order By s Descending
                                       ~~~~~~~~
BC36594: Definition of method 'OrderBy' is not accessible in this context.
        Dim q3 As Object = From s In q Order By s Ascending
                                       ~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q4 As Object = From s In q Skip While s > 1 Order By s
                                       ~~~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q5 As Object = From s In q Skip While s > 1 Order By s Descending
                                       ~~~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q6 As Object = From s In q Skip While s > 1 Order By s Ascending
                                       ~~~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q7 As Object = From s In q Skip While s > 1 Order By DoesntExist
                                       ~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        Dim q7 As Object = From s In q Skip While s > 1 Order By DoesntExist
                                                                 ~~~~~~~~~~~
BC36594: Definition of method 'SkipWhile' is not accessible in this context.
        Dim q8 As Object = From s In q Skip While s > 1 Order By DoesntExist1, DoesntExist2
                                       ~~~~~~~~~~
BC36610: Name 'DoesntExist1' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Skip While s > 1 Order By DoesntExist1, DoesntExist2
                                                                 ~~~~~~~~~~~~
BC36610: Name 'DoesntExist2' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Skip While s > 1 Order By DoesntExist1, DoesntExist2
                                                                               ~~~~~~~~~~~~
BC36594: Definition of method 'OrderBy' is not accessible in this context.
        Dim q9 As Object = From s In q Order By s, s
                                       ~~~~~~~~
BC36594: Definition of method 'OrderBy' is not accessible in this context.
        Dim q10 As Object = From s In q Order By s, DoesntExist
                                        ~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        Dim q10 As Object = From s In q Order By s, DoesntExist
                                                    ~~~~~~~~~~~
BC30201: Expression expected.
        Dim q11 As Object = From s In q Order By :
                                                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub OrderBy2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Order By s, s
        Dim q2 As Object = From s In q Order By s, s Descending
        Dim q3 As Object = From s In q Order By s, s Ascending
        Dim q4 As Object = From s In q Order By s, s, s
        Dim q5 As Object = From s In q Order By s, s Ascending, s Descending
        Dim q6 As Object = From s In q Order By s, s Descending, s Ascending
        Dim q7 As Object = From s In q Order By s, s, DoesntExist
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
        Dim q9 As Object = From s In q Select DoesntExist1 Order By DoesntExist2, DoesntExist3
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q1 As Object = From s In q Order By s, s
                                                 ~
BC36594: Definition of method 'ThenByDescending' is not accessible in this context.
        Dim q2 As Object = From s In q Order By s, s Descending
                                                 ~
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q3 As Object = From s In q Order By s, s Ascending
                                                 ~
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q4 As Object = From s In q Order By s, s, s
                                                 ~
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q5 As Object = From s In q Order By s, s Ascending, s Descending
                                                 ~
BC36594: Definition of method 'ThenByDescending' is not accessible in this context.
        Dim q6 As Object = From s In q Order By s, s Descending, s Ascending
                                                 ~
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q7 As Object = From s In q Order By s, s, DoesntExist
                                                 ~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        Dim q7 As Object = From s In q Order By s, s, DoesntExist
                                                      ~~~~~~~~~~~
BC36594: Definition of method 'ThenBy' is not accessible in this context.
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
                                                 ~
BC36610: Name 'DoesntExist1' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
                                                      ~~~~~~~~~~~~
BC36610: Name 'DoesntExist2' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
                                                                    ~~~~~~~~~~~~
BC36610: Name 'DoesntExist1' is either not declared or not in the current scope.
        Dim q9 As Object = From s In q Select DoesntExist1 Order By DoesntExist2, DoesntExist3
                                              ~~~~~~~~~~~~
BC36610: Name 'DoesntExist2' is either not declared or not in the current scope.
        Dim q9 As Object = From s In q Select DoesntExist1 Order By DoesntExist2, DoesntExist3
                                                                    ~~~~~~~~~~~~
BC36610: Name 'DoesntExist3' is either not declared or not in the current scope.
        Dim q9 As Object = From s In q Select DoesntExist1 Order By DoesntExist2, DoesntExist3
                                                                                  ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub OrderBy3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function ThenBy(x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = From s In q Order By s, s
        Dim q2 As Object = From s In q Order By s, s Descending
        Dim q3 As Object = From s In q Order By s, s Ascending
        Dim q4 As Object = From s In q Order By s, s, s
        Dim q5 As Object = From s In q Order By s, s Ascending, s Descending
        Dim q6 As Object = From s In q Order By s, s Descending, s Ascending
        Dim q7 As Object = From s In q Order By s, s, DoesntExist
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
        Dim q9 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2 Descending
        Dim q10 As Object = From s In q Select s + 1 Order By s
        Dim q11 As Object = From s In q Order By :
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'ThenByDescending' is not accessible in this context.
        Dim q2 As Object = From s In q Order By s, s Descending
                                                 ~
BC36594: Definition of method 'ThenByDescending' is not accessible in this context.
        Dim q5 As Object = From s In q Order By s, s Ascending, s Descending
                                                              ~
BC36594: Definition of method 'ThenByDescending' is not accessible in this context.
        Dim q6 As Object = From s In q Order By s, s Descending, s Ascending
                                                 ~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        Dim q7 As Object = From s In q Order By s, s, DoesntExist
                                                      ~~~~~~~~~~~
BC36610: Name 'DoesntExist1' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
                                                      ~~~~~~~~~~~~
BC36610: Name 'DoesntExist2' is either not declared or not in the current scope.
        Dim q8 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2
                                                                    ~~~~~~~~~~~~
BC36610: Name 'DoesntExist1' is either not declared or not in the current scope.
        Dim q9 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2 Descending
                                                      ~~~~~~~~~~~~
BC36610: Name 'DoesntExist2' is either not declared or not in the current scope.
        Dim q9 As Object = From s In q Order By s, s, DoesntExist1, DoesntExist2 Descending
                                                                    ~~~~~~~~~~~~
BC36610: Name 's' is either not declared or not in the current scope.
        Dim q10 As Object = From s In q Select s + 1 Order By s
                                                              ~
BC30201: Expression expected.
        Dim q11 As Object = From s In q Order By :
                                                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub OrderBy4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(0)

        Dim q1 As Object = From s In q
                           Order By s, s, s Descending, s Ascending
                           Order By s Descending, s Descending, s
                           Order By s Ascending
                           Select s

        System.Console.WriteLine("-----")
        Dim q2 As Object = From s In q Select s + 1 Order By 0
        System.Console.WriteLine("-----")
        Dim q3 As Object = From s In q Order By s
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
OrderBy 0
ThenBy 1
ThenByDescending 2
ThenBy 3
OrderByDescending 4
ThenByDescending 5
ThenBy 6
OrderBy 7
Select 8
-----
Select 0
OrderBy 1
-----
OrderBy 0
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub OrderByAscending_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(0)
        Dim q3 As Object = From s In q Order By s'BIND:"From s In q Order By s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q Order By s')
  Expression: 
    IInvocationOperation ( Function QueryAble.OrderBy(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub OrderByDescending_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(0)
        Dim q3 As Object = From s In q Order By s Descending'BIND:"From s In q Order By s Descending"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ...  Descending')
  Expression: 
    IInvocationOperation ( Function QueryAble.OrderByDescending(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Descending')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub OrderByAscendingDescending_IOperation()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenBy {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("OrderByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function

    Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
        System.Console.WriteLine("ThenByDescending {0}", v)
        Return New QueryAble(v + 1)
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(0)

        Dim q1 As Object = From s In q'BIND:"From s In q"
                           Order By s, s, s Descending, s Ascending
                           Order By s Descending, s Descending, s
                           Order By s Ascending
                           Select s
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... Select s')
  Expression: 
    IInvocationOperation ( Function QueryAble.Select(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Select s')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.OrderBy(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Ascending')
          Instance Receiver: 
            IInvocationOperation ( Function QueryAble.ThenBy(x As System.Func(Of System.Int32, System.Byte)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's')
              Instance Receiver: 
                IInvocationOperation ( Function QueryAble.ThenByDescending(x As System.Func(Of System.Int32, System.Byte)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Descending')
                  Instance Receiver: 
                    IInvocationOperation ( Function QueryAble.OrderByDescending(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Descending')
                      Instance Receiver: 
                        IInvocationOperation ( Function QueryAble.ThenBy(x As System.Func(Of System.Int32, System.Byte)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Ascending')
                          Instance Receiver: 
                            IInvocationOperation ( Function QueryAble.ThenByDescending(x As System.Func(Of System.Int32, System.Byte)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's Descending')
                              Instance Receiver: 
                                IInvocationOperation ( Function QueryAble.ThenBy(x As System.Func(Of System.Int32, System.Byte)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's')
                                  Instance Receiver: 
                                    IInvocationOperation ( Function QueryAble.OrderBy(x As System.Func(Of System.Int32, System.Int32)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 's')
                                      Instance Receiver: 
                                        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
                                              Target: 
                                                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                                      ReturnedValue: 
                                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Byte), IsImplicit) (Syntax: 's')
                                          Target: 
                                            IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Byte) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                                  ReturnedValue: 
                                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 's')
                                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                      Operand: 
                                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Byte), IsImplicit) (Syntax: 's')
                                      Target: 
                                        IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Byte) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                              ReturnedValue: 
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 's')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                  Operand: 
                                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Byte), IsImplicit) (Syntax: 's')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Byte) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                          ReturnedValue: 
                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 's')
                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Operand: 
                                                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                      ReturnedValue: 
                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Byte), IsImplicit) (Syntax: 's')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Byte) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                                  ReturnedValue: 
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 's')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Operand: 
                                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Byte), IsImplicit) (Syntax: 's')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Byte) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                              ReturnedValue: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Byte, IsImplicit) (Syntax: 's')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                          ReturnedValue: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's')
                      ReturnedValue: 
                        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub OrderBy5()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function OrderBy(Of S)(x As Func(Of T, S)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Order By Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
OrderBy System.Func`2[System.Int32,System.Object]
]]>)
        End Sub

        <Fact>
        Public Sub OrderBy6()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Order By Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
OrderBy System.Func`2[System.Int32,System.Int32]
]]>)
        End Sub

        <Fact>
        Public Sub Select6()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable = From s In New Integer() {1,2} Select s, t=s+1

        For Each v In q0
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q1 As IEnumerable = From s In New Integer() {1,-1} Select s, t=s*2 Where s > t

        For Each v In q1
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q2 As IEnumerable = From s In New Integer() {1,-1} Select s, t=s*2 Select t, s

        For Each v In q2
           System.Console.WriteLine(v)
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s = 1, t = 2 }
{ s = 2, t = 3 }
------
{ s = -1, t = -2 }
------
{ t = 2, s = 1 }
{ t = -2, s = -1 }
]]>)
        End Sub

        <Fact>
        Public Sub Select7()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim s As Integer = 0
        Dim t As Integer = 0
        Dim q0 As IEnumerable = From s In New Integer() {1,2} Select s, t=s+1
        Dim q1 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s1, s1=s1+1
        Dim q2 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s1+1
        Dim q3 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s2%=s1+1
        Dim q4 As IEnumerable = From s1 In New Integer() {1,2} Select s1, GetHashCode=s1+1
        Dim q5 As Object = From s1 In New Integer() {1,2} Select x1 = s1, x2 = x1
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30978: Range variable 's' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q0 As IEnumerable = From s In New Integer() {1,2} Select s, t=s+1
                                     ~
BC30978: Range variable 's' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q0 As IEnumerable = From s In New Integer() {1,2} Select s, t=s+1
                                                                     ~
BC30978: Range variable 't' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q0 As IEnumerable = From s In New Integer() {1,2} Select s, t=s+1
                                                                        ~
BC36600: Range variable 's1' is already declared.
        Dim q1 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s1, s1=s1+1
                                                                          ~~
BC36600: Range variable 's1' is already declared.
        Dim q1 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s1, s1=s1+1
                                                                              ~~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        Dim q2 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s1+1
                                                                          ~~~~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q3 As IEnumerable = From s1 In New Integer() {1,2} Select s1, s2%=s1+1
                                                                          ~~~
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q4 As IEnumerable = From s1 In New Integer() {1,2} Select s1, GetHashCode=s1+1
                                                                          ~~~~~~~~~~~
BC36610: Name 'x1' is either not declared or not in the current scope.
        Dim q5 As Object = From s1 In New Integer() {1,2} Select x1 = s1, x2 = x1
                                                                               ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Select8()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q As New QueryAble()
        Dim q0 As Object = From s In q Select x1 = s, x2 = s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q0 As Object = From s In q Select x1 = s, x2 = s
                                       ~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q0 As Object = From s In q Select x1 = s, x2 = s
                                                   ~
</expected>)
        End Sub

        <Fact>
        Public Sub Select9()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object
        q0 = From s In New Integer() {1, 2} Select s + 1

        q0 = From s In New Integer() {1, 2} Select s + 1 Join s1 In New Integer() {1, 2} On s Equals s1

        q0 = From s In New Integer() {1, 2} Select s + 1 Where True Order By 1 Distinct Take While True Skip While False Skip 0 Take 0 

        q0 = From s In New Integer() {1, 2} Select s + 1 Where True Order By 1 Distinct Take While True Skip While False Skip 0 Take 0 Join s1 In New Integer() {1, 2} On s Equals s1
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        q0 = From s In New Integer() {1, 2} Select s + 1 Join s1 In New Integer() {1, 2} On s Equals s1
                                                   ~~~~~
BC36610: Name 's' is either not declared or not in the current scope.
        q0 = From s In New Integer() {1, 2} Select s + 1 Join s1 In New Integer() {1, 2} On s Equals s1
                                                                                            ~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        q0 = From s In New Integer() {1, 2} Select s + 1 Where True Order By 1 Distinct Take While True Skip While False Skip 0 Take 0 Join s1 In New Integer() {1, 2} On s Equals s1
                                                   ~~~~~
BC36610: Name 's' is either not declared or not in the current scope.
        q0 = From s In New Integer() {1, 2} Select s + 1 Where True Order By 1 Distinct Take While True Skip While False Skip 0 Take 0 Join s1 In New Integer() {1, 2} On s Equals s1
                                                                                                                                                                          ~
</expected>)
        End Sub

        <Fact>
        Public Sub Select10()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Select Nothing
        q = From s In q1 Select x=Nothing, y=Nothing
        q = From s In q1 Let x = Nothing
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Select System.Func`2[System.Int32,System.Object]
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Object,System.Object]]
Select System.Func`2[System.Int32,VB$AnonymousType_1`2[System.Int32,System.Object]]
]]>)
        End Sub

        <Fact>
        Public Sub Let1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1+1

        For Each v In q0
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q1 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1

        For Each v In q1
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q2 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3

        For Each v In q2
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q3 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3, s5 = s1+s2+s3+s4

        For Each v In q3
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q4 As IEnumerable = From s1 In New Integer() {2} Let s2 = s1+1 Let s3 = s2+s1, s4 = s1+s2+s3, s5 = s1+s2+s3+s4

        For Each v In q4
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q5 As IEnumerable = From s1 In New Integer() {3} Let s2 = s1+1, s3 = s2+s1 Let s4 = s1+s2+s3, s5 = s1+s2+s3+s4

        For Each v In q5
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q6 As IEnumerable = From s1 In New Integer() {4} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3 Let s5 = s1+s2+s3+s4

        For Each v In q6
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q7 As IEnumerable = From s1 In New Integer() {5} Select s1+1 Let s2 = 7

        For Each v In q7
           System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        Dim q8 As IEnumerable = From s1 In New Integer() {5} Select s1+1 Let s2 = 7, s3 = 8

        For Each v In q8
           System.Console.WriteLine(v)
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s1 = 1, s2 = 2 }
------
{ s1 = 1, s2 = 2, s3 = 3 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 6 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 6, s5 = 12 }
------
{ s1 = 2, s2 = 3, s3 = 5, s4 = 10, s5 = 20 }
------
{ s1 = 3, s2 = 4, s3 = 7, s4 = 14, s5 = 28 }
------
{ s1 = 4, s2 = 5, s3 = 9, s4 = 18, s5 = 36 }
------
7
------
{ s2 = 7, s3 = 8 }
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Let_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1 + 1'BIND:"From s1 In New Integer() {1} Let s2 = s1 + 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (Syntax: 'From s1 In  ... s2 = s1 + 1')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)(selector As System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's2 = s1 + 1')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {1}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
              Initializer: 
                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                  Element Values(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's1 + 1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 = s1 + 1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 + 1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                Right: 
                                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                                    Left: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub LetMultipleVariables_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q1 As IEnumerable = From s1 In New Integer() {1} Let s2 = s1 + 1, s3 = s2 + s1'BIND:"From s1 In New Integer() {1} Let s2 = s1 + 1, s3 = s2 + s1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) (Syntax: 'From s1 In  ... 3 = s2 + s1')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>).Select(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)(selector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 's3 = s2 + s1')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)(selector As System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's2 = s1 + 1')
          Instance Receiver: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {1}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                  Initializer: 
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                      Element Values(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's1 + 1')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                              Initializers(2):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                    Right: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 = s1 + 1')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 + 1')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                    Right: 
                                      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                                        Left: 
                                          IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                        Right: 
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 + s1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 's2 + s1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2 + s1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2 + s1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2 + s1')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 = s2 + s1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2 + s1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                Right: 
                                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's2 + s1')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
                                    Right: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub LetMultipleClauses_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q5 As IEnumerable = From s1 In New Integer() {3} Let s2 = s1 + 1, s3 = s2 + s1 Let s4 = s1 + s2 + s3, s5 = s1 + s2 + s3 + s4'BIND:"From s1 In New Integer() {3} Let s2 = s1 + 1, s3 = s2 + s1 Let s4 = s1 + s2 + s3, s5 = s1 + s2 + s3 + s4"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>)) (Syntax: 'From s1 In  ... 2 + s3 + s4')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>).Select(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>)(selector As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>), IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>).Select(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>)(selector As System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>), IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
          Instance Receiver: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>).Select(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>)(selector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>), IsImplicit) (Syntax: 's3 = s2 + s1')
              Instance Receiver: 
                IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)(selector As System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's2 = s1 + 1')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {3}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3}')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {3}')
                          Initializer: 
                            IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3}')
                              Element Values(1):
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's1 + 1')
                          Target: 
                            IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                                  ReturnedValue: 
                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                      Initializers(2):
                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {3}')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                                Instance Receiver: 
                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                            Right: 
                                              IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 = s1 + 1')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 + 1')
                                                Instance Receiver: 
                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 = s1 + 1')
                                            Right: 
                                              IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                                                Left: 
                                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                                Right: 
                                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 + s1')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>), IsImplicit) (Syntax: 's2 + s1')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2 + s1')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2 + s1')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2 + s1')
                              ReturnedValue: 
                                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                  Initializers(2):
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Let s2 = s1 ... 3 = s2 + s1')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                        Right: 
                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 = s2 + s1')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2 + s1')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's3 = s2 + s1')
                                        Right: 
                                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's2 + s1')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                                Instance Receiver: 
                                                  IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
                                            Right: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                                Instance Receiver: 
                                                  IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's2 + s1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>), IsImplicit) (Syntax: 's1 + s2 + s3')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>) As <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                              Initializers(2):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Let s4 = s1 ... 2 + s3 + s4')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                                    Right: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.s4 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 + s2 + s3')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's4 = s1 + s2 + s3')
                                    Right: 
                                      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + s2 + s3')
                                        Left: 
                                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + s2')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                                Instance Receiver: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3')
                                                    Instance Receiver: 
                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3')
                                            Right: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                                Instance Receiver: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3')
                                                    Instance Receiver: 
                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3')
                                        Right: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's3')
                                            Instance Receiver: 
                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>), IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                          Initializers(5):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {3}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                        Instance Receiver: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                            Instance Receiver: 
                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                        Instance Receiver: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                            Instance Receiver: 
                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                    Instance Receiver: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's4 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>.s4 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's4')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.s4 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's4')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>.s5 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32, Key s5 As System.Int32>, IsImplicit) (Syntax: 's5 = s1 + s2 + s3 + s4')
                                Right: 
                                  IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + s2 + s3 + s4')
                                    Left: 
                                      IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + s2 + s3')
                                        Left: 
                                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + s2')
                                            Left: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                                Instance Receiver: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                                    Instance Receiver: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                                        Instance Receiver: 
                                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                            Right: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                                Instance Receiver: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.$VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                                    Instance Receiver: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                                        Instance Receiver: 
                                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                        Right: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's3')
                                            Instance Receiver: 
                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.$VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                                Instance Receiver: 
                                                  IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
                                    Right: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>.s4 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's4')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It As <anonymous type: Key $VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, Key s3 As System.Int32>, Key s4 As System.Int32>, IsImplicit) (Syntax: 's1 + s2 + s3 + s4')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Let2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object = From s In New Integer() {1, 2} Let s.GetHashCode
        Dim q1 As Object = From s In New Integer() {1, 2} Let =s.GetHashCode
        Dim q2 As Object = From s In New Integer() {1, 2} Let _=s.GetHashCode
        Dim q3 As Object = From s In New Integer() {1, 2} Let 

        Dim q4 As Object = From s In New Integer() {1, 2} Let t

        Dim q5 As Object = From s In New Integer() {1, 2} Let t=

        Dim q6 As Object = From s In New Integer() {1, 2} Let t1=, t2=s

        Dim q7 As Object = From s In New Integer() {1, 2} Let t% = s

        Dim q8 As Object = From s In New Integer() {1, 2} Let t% As Integer = s
        Dim q9 As Object = From s In New Integer() {1, 2} Let t? As Integer = s
        Dim q10 As Object = From s In New Integer() {1, 2} Let t? = s
        Dim q11 As Object = From s In New Integer() {1, 2} Select t? As Integer = s
        Dim q12 As Object = From s In New Integer() {1, 2} Select t As Integer = s

        Dim q13 As Object = From s In New Integer() {1, 2} Let q0 = s + 1
        Dim q14 As Object = From s In New Integer() {1, 2} Let s1 = s + 1, s1 = s + 2
        Dim q15 As Object = From s In New Integer() {1, 2} Let s = s + 1

        Dim q16 As Object = From s In New Integer() {1, 2} Let s1 As Date = s
        Dim q17 As Object = From s In New Integer() {1, 2} Let s1? As Byte = s
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30978: Range variable 's' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q0 As Object = From s In New Integer() {1, 2} Let s.GetHashCode
                                                              ~
BC32020: '=' expected.
        Dim q0 As Object = From s In New Integer() {1, 2} Let s.GetHashCode
                                                               ~
BC30203: Identifier expected.
        Dim q1 As Object = From s In New Integer() {1, 2} Let =s.GetHashCode
                                                              ~
BC30203: Identifier expected.
        Dim q2 As Object = From s In New Integer() {1, 2} Let _=s.GetHashCode
                                                              ~
BC30203: Identifier expected.
        Dim q3 As Object = From s In New Integer() {1, 2} Let 
                                                              ~
BC32020: '=' expected.
        Dim q3 As Object = From s In New Integer() {1, 2} Let 
                                                              ~
BC32020: '=' expected.
        Dim q4 As Object = From s In New Integer() {1, 2} Let t
                                                               ~
BC30201: Expression expected.
        Dim q5 As Object = From s In New Integer() {1, 2} Let t=
                                                                ~
BC30201: Expression expected.
        Dim q6 As Object = From s In New Integer() {1, 2} Let t1=, t2=s
                                                                 ~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q7 As Object = From s In New Integer() {1, 2} Let t% = s
                                                              ~~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q8 As Object = From s In New Integer() {1, 2} Let t% As Integer = s
                                                              ~~
BC36629: Nullable type inference is not supported in this context.
        Dim q10 As Object = From s In New Integer() {1, 2} Let t? = s
                                                                ~
BC36610: Name 't' is either not declared or not in the current scope.
        Dim q11 As Object = From s In New Integer() {1, 2} Select t? As Integer = s
                                                                  ~
BC36637: The '?' character cannot be used here.
        Dim q11 As Object = From s In New Integer() {1, 2} Select t? As Integer = s
                                                                   ~
BC36610: Name 't' is either not declared or not in the current scope.
        Dim q12 As Object = From s In New Integer() {1, 2} Select t As Integer = s
                                                                  ~
BC30205: End of statement expected.
        Dim q12 As Object = From s In New Integer() {1, 2} Select t As Integer = s
                                                                    ~~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q13 As Object = From s In New Integer() {1, 2} Let q0 = s + 1
                                                               ~~
BC30978: Range variable 's1' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q14 As Object = From s In New Integer() {1, 2} Let s1 = s + 1, s1 = s + 2
                                                                           ~~
BC30978: Range variable 's' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q15 As Object = From s In New Integer() {1, 2} Let s = s + 1
                                                               ~
BC30311: Value of type 'Integer' cannot be converted to 'Date'.
        Dim q16 As Object = From s In New Integer() {1, 2} Let s1 As Date = s
                                                                            ~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        Dim q17 As Object = From s In New Integer() {1, 2} Let s1? As Byte = s
                                                                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub Let3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    'Inherits Base

    'Public Shadows [Select] As Byte
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(0)

        Dim q0 As Object = From s In q Let t1 = s + 1
        System.Console.WriteLine("------")
        Dim q1 As Object = From s In q Let t1 = s + 1, t2 = t1
        System.Console.WriteLine("------")
        Dim q2 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2
        System.Console.WriteLine("------")
        Dim q3 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2, t4 = t3
        System.Console.WriteLine("------")
        Dim q4 As Object = From s In q Let t1 = s + 1 Let t2 = t1, t3 = t2, t4 = t3
        System.Console.WriteLine("------")
        Dim q5 As Object = From s In q Let t1 = s + 1, t2 = t1 Let t3 = t2, t4 = t3
        System.Console.WriteLine("------")
        Dim q6 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Let t4 = t3
        System.Console.WriteLine("------")
        Dim q7 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Select s, t1, t2, t3, t4 = t3

        System.Console.WriteLine("------")
        Dim q8 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
        System.Console.WriteLine("------")
        Dim q9 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 Select s, t1, t2, t3, t4 = t3
        System.Console.WriteLine("------")
        Dim q10 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 Let t4 = 1
        System.Console.WriteLine("------")
        Dim q11 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 From t4 In q
        System.Console.WriteLine("------")
        Dim q12 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Join t4 in q On t3 Equals t4
        System.Console.WriteLine("------")
        Dim q13 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Group s By t3 Into Group
        System.Console.WriteLine("------")
        Dim q14 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Group By t3 Into Group
        System.Console.WriteLine("------")
        Dim q15 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Group Join t4 in q On t3 Equals t4 Into Group
        System.Console.WriteLine("------")
        Dim q16 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Aggregate t4 in q Into Where(True)
        System.Console.WriteLine("------")
        Dim q17 As Object = From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                            Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                            Aggregate t4 in q Into Where(True), Distinct
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
Skip 0
Take 0
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32,VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32,VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
Skip 0
Take 0
GroupBy 
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32],VB$AnonymousType_7`5[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_8`5[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_9`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32]]]
Select System.Func`2[VB$AnonymousType_9`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32]],VB$AnonymousType_10`6[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32],QueryAble`1[System.Int32]]]
]]>)
        End Sub

        <Fact>
        Public Sub Let4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble1
        Return nothing
    End Function
End Class

Class QueryAble1
    Public Function [Select](Of T, S)(x As Func(Of T, S)) As QueryAble2
        Return Nothing
    End Function
End Class

Class QueryAble2
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q0 As Object = From s In q Let t1 = s + 1

        Dim q1 As Object = From s In q Where s>0 Let t1 = s + 1, t2 = t1

        Dim q2 As Object = From s In q Select s1 Let t1 = s1 + 1

        Dim q3 As Object = From s In q Where s>0 Let s1 + 1, t2 = t12
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q0 As Object = From s In q Let t1 = s + 1
                                       ~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim q0 As Object = From s In q Let t1 = s + 1
                                                ~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        Dim q1 As Object = From s In q Where s>0 Let t1 = s + 1, t2 = t1
                                                               ~
BC36610: Name 's1' is either not declared or not in the current scope.
        Dim q2 As Object = From s In q Select s1 Let t1 = s1 + 1
                                              ~~
BC32020: '=' expected.
        Dim q3 As Object = From s In q Where s>0 Let s1 + 1, t2 = t12
                                                        ~
BC36610: Name 't12' is either not declared or not in the current scope.
        Dim q3 As Object = From s In q Where s>0 Let s1 + 1, t2 = t12
                                                                  ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub From3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable
        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3}, s3 In New Integer() {4, 5}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} From s2 In New Integer() {2, 3}, s3 In New Integer() {6, 7}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3} From s3 In New Integer() {8, 9}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, -1} Select s1 + 1 From s2 In New Integer() {2, 3}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, -1} Select s1 + 1 From s2 In New Integer() {2, 3}, s3 In New Integer() {4, 5}

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Select s2, s1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Let s3 = s1 + s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Let s3 = s1 + s2, s4 = s3 + 1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, 2} Select s1 + 1 From s2 In New Integer() {2, 3} Select s3 = 4, s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, 2} Select s1 + 1 From s2 In New Integer() {2, 3} Let s3 = 5

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer()() {New Integer() {1, 2}, New Integer() {2, 3}}, s2 In s1 Select s3 = s1(0), s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s1 = 1, s2 = 2 }
{ s1 = 1, s2 = 3 }
------
{ s1 = 1, s2 = 2, s3 = 4 }
{ s1 = 1, s2 = 2, s3 = 5 }
{ s1 = 1, s2 = 3, s3 = 4 }
{ s1 = 1, s2 = 3, s3 = 5 }
------
{ s1 = 1, s2 = 2, s3 = 6 }
{ s1 = 1, s2 = 2, s3 = 7 }
{ s1 = 1, s2 = 3, s3 = 6 }
{ s1 = 1, s2 = 3, s3 = 7 }
------
{ s1 = 1, s2 = 2, s3 = 8 }
{ s1 = 1, s2 = 2, s3 = 9 }
{ s1 = 1, s2 = 3, s3 = 8 }
{ s1 = 1, s2 = 3, s3 = 9 }
------
2
3
2
3
------
{ s2 = 2, s3 = 4 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 4 }
{ s2 = 3, s3 = 5 }
{ s2 = 2, s3 = 4 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 4 }
{ s2 = 3, s3 = 5 }
------
{ s2 = 2, s1 = 1 }
{ s2 = 3, s1 = 1 }
------
{ s1 = 1, s2 = 2, s3 = 3 }
{ s1 = 1, s2 = 3, s3 = 4 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 4 }
{ s1 = 1, s2 = 3, s3 = 4, s4 = 5 }
------
{ s3 = 4, s2 = 2 }
{ s3 = 4, s2 = 3 }
{ s3 = 4, s2 = 2 }
{ s3 = 4, s2 = 3 }
------
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 5 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 5 }
------
{ s3 = 1, s2 = 1 }
{ s3 = 1, s2 = 2 }
{ s3 = 2, s2 = 2 }
{ s3 = 2, s2 = 3 }
]]>)
        End Sub

        <Fact>
        Public Sub From4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object = From s In New Integer() {1, 2} From GetHashCode 
        Dim q1 As Object = From s In New Integer() {1, 2} From  In New Integer() {1, 2}
        Dim q2 As Object = From s In New Integer() {1, 2} From _ In New Integer() {1, 2}
        Dim q3 As Object = From s In New Integer() {1, 2} From 

        Dim q4 As Object = From s In New Integer() {1, 2} From t

        Dim q5 As Object = From s In New Integer() {1, 2} From t In

        Dim q6 As Object = From s In New Integer() {1, 2} From t1 In , t2 In New Integer() {1, 2}

        Dim q7 As Object = From s In New Integer() {1, 2} From t% In New Integer() {1, 2}

        Dim q8 As Object = From s In New Integer() {1, 2} From t% As Integer In New Integer() {1, 2}
        Dim q9 As Object = From s In New Integer() {1, 2} From t? As Integer In New Integer() {1, 2}
        Dim q10 As Object = From s In New Integer() {1, 2} From t? In New Integer() {1, 2}

        Dim q11 As Object = From s In New Integer() {1, 2} From t In New Integer() {1, 2}, 

        Dim q12 As Object = From s In New Integer() {1, 2} From t1 In New Integer() {1, 2}, t2 In 

        Dim q13 As Object = From s In New Integer() {1, 2} From q0 In New Integer() {1, 2}
        Dim q14 As Object = From s In New Integer() {1, 2} From s1 In New Integer() {1, 2}, s1 In New Integer() {1, 2}
        Dim q15 As Object = From s In New Integer() {1, 2} From s In New Integer() {1, 2}

        Dim q16 As Object = From s In New Integer() {1, 2} From s1 As Date In New Integer() {1, 2}
        Dim q17 As Object = From s In New Integer() {1, 2} From s1? As Byte In New Integer() {1, 2}
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        Dim q0 As Object = From s In New Integer() {1, 2} From GetHashCode 
                                                               ~~~~~~~~~~~
BC36607: 'In' expected.
        Dim q0 As Object = From s In New Integer() {1, 2} From GetHashCode 
                                                                           ~
BC30183: Keyword is not valid as an identifier.
        Dim q1 As Object = From s In New Integer() {1, 2} From  In New Integer() {1, 2}
                                                                ~~
BC36607: 'In' expected.
        Dim q1 As Object = From s In New Integer() {1, 2} From  In New Integer() {1, 2}
                                                                                   ~
BC30203: Identifier expected.
        Dim q1 As Object = From s In New Integer() {1, 2} From  In New Integer() {1, 2}
                                                                                     ~
BC36607: 'In' expected.
        Dim q1 As Object = From s In New Integer() {1, 2} From  In New Integer() {1, 2}
                                                                                     ~
BC30203: Identifier expected.
        Dim q2 As Object = From s In New Integer() {1, 2} From _ In New Integer() {1, 2}
                                                               ~
BC30203: Identifier expected.
        Dim q3 As Object = From s In New Integer() {1, 2} From 
                                                               ~
BC36607: 'In' expected.
        Dim q3 As Object = From s In New Integer() {1, 2} From 
                                                               ~
BC36607: 'In' expected.
        Dim q4 As Object = From s In New Integer() {1, 2} From t
                                                                ~
BC30201: Expression expected.
        Dim q5 As Object = From s In New Integer() {1, 2} From t In
                                                                   ~
BC30201: Expression expected.
        Dim q6 As Object = From s In New Integer() {1, 2} From t1 In , t2 In New Integer() {1, 2}
                                                                     ~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q7 As Object = From s In New Integer() {1, 2} From t% In New Integer() {1, 2}
                                                               ~~
BC36601: Type characters cannot be used in range variable declarations.
        Dim q8 As Object = From s In New Integer() {1, 2} From t% As Integer In New Integer() {1, 2}
                                                               ~~
BC36629: Nullable type inference is not supported in this context.
        Dim q10 As Object = From s In New Integer() {1, 2} From t? In New Integer() {1, 2}
                                                                 ~
BC30203: Identifier expected.
        Dim q11 As Object = From s In New Integer() {1, 2} From t In New Integer() {1, 2}, 
                                                                                           ~
BC36607: 'In' expected.
        Dim q11 As Object = From s In New Integer() {1, 2} From t In New Integer() {1, 2}, 
                                                                                           ~
BC30201: Expression expected.
        Dim q12 As Object = From s In New Integer() {1, 2} From t1 In New Integer() {1, 2}, t2 In 
                                                                                                  ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q13 As Object = From s In New Integer() {1, 2} From q0 In New Integer() {1, 2}
                                                                ~~
BC30978: Range variable 's1' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q14 As Object = From s In New Integer() {1, 2} From s1 In New Integer() {1, 2}, s1 In New Integer() {1, 2}
                                                                                            ~~
BC30978: Range variable 's' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q15 As Object = From s In New Integer() {1, 2} From s In New Integer() {1, 2}
                                                                ~
BC30311: Value of type 'Integer' cannot be converted to 'Date'.
        Dim q16 As Object = From s In New Integer() {1, 2} From s1 As Date In New Integer() {1, 2}
                                                                   ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        Dim q17 As Object = From s In New Integer() {1, 2} From s1? As Byte In New Integer() {1, 2}
                                                                    ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub From5()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)

        Dim q0 As Object
        q0 = From s1 In qi From s2 In qb
        System.Console.WriteLine("------")
        q0 = From s1 In qi, s2 In qb
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu, s5 In ql
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb From s3 In qs, s4 In qu, s5 In ql
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs From s4 In qu, s5 In ql
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu From s5 In ql
        System.Console.WriteLine("------")

        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 From s5 In ql
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 Let s5 = 1L
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 Select s4, s3, s2, s1
        System.Console.WriteLine("------")

        q0 = From s1 In qi, s2 In qb Select s2, s1
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Select s4, s3, s2, s1
        System.Console.WriteLine("------")

        q0 = From s1 In qi, s2 In qb Let s3 = 1L
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Let s5 = 1L
        System.Console.WriteLine("------")
        q0 = From s1 In qi, s2 In qb Let s3 = 1S, s4 = 1UI
        System.Console.WriteLine("------")
        q0 = From s1 In qi, s2 In qb Let s3 = 1S Let s4 = 1UI
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Let s5 = 1L Select s5, s4, s3, s2, s1
        System.Console.WriteLine("------")

        q0 = From s1 In qi Select s1 + 1 From s2 In qb
        System.Console.WriteLine("------")
        q0 = From s1 In qi Select s1 + 1 From s2 In qb Select s2, s3 = 1S
        System.Console.WriteLine("------")
        q0 = From s1 In qi Select s1 + 1 From s2 In qb Let s3 = 1S
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Join s5 In ql On s1 Equals s5
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Group s1 By s2 Into Group
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Group By s2 Into Group
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Group Join s5 In ql On s1 Equals s5 Into Group
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Aggregate s5 In ql Into Where(True)
        System.Console.WriteLine("------")
        q0 = From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
             Aggregate s5 In ql Into Where(True), Distinct
        System.Console.WriteLine("------")
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
Skip 0
Take 0
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_6`4[System.UInt32,System.Int16,System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_7`2[System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_6`4[System.UInt32,System.Int16,System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
Select System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16],VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
Select System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16],VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_8`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,System.Int64]]
Select System.Func`2[VB$AnonymousType_8`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,System.Int64],VB$AnonymousType_9`5[System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,System.Byte]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_10`2[System.Byte,System.Int16]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_10`2[System.Byte,System.Int16]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
Skip 0
Take 0
GroupBy 
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64],VB$AnonymousType_12`5[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_13`5[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_14`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64]]]
Select System.Func`2[VB$AnonymousType_14`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64]],VB$AnonymousType_15`6[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Int64]]]
------
]]>)
        End Sub

        <Fact>
        Public Sub From6()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble1
        Return nothing
    End Function
End Class

Class QueryAble1
    Public Function SelectMany(Of S)(x As Func(Of Integer, QueryAble), y As Func(Of Integer, Integer, S)) As QueryAble2
        Return Nothing
    End Function
End Class

Class QueryAble2
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q0 As Object = From s In q From t1 In q

        Dim q1 As Object = From s In q Where s>0 From t1 In q, t2 In q

        Dim q2 As Object = From s In q Select s1 From t1 In q

        Dim q3 As Object = From s In q Where s>0 From _ In q, t2 In q
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'SelectMany' is not accessible in this context.
        Dim q0 As Object = From s In q From t1 In q
                                       ~~~~
BC36594: Definition of method 'SelectMany' is not accessible in this context.
        Dim q1 As Object = From s In q Where s>0 From t1 In q, t2 In q
                                                             ~
BC36610: Name 's1' is either not declared or not in the current scope.
        Dim q2 As Object = From s In q Select s1 From t1 In q
                                              ~~
BC30203: Identifier expected.
        Dim q3 As Object = From s In q Where s>0 From _ In q, t2 In q
                                                      ~
BC36594: Definition of method 'SelectMany' is not accessible in this context.
        Dim q3 As Object = From s In q Where s>0 From _ In q, t2 In q
                                                            ~
</expected>)
        End Sub

        <Fact>
        Public Sub Join1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable

        q0 = From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s1 Equals s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s2 + 1 Equals s1 + 2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Select s2, s1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Select s3, s2, s1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2 Select s3, s2, s1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Let s3 = s1 + s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Let s4 = s1 + s2 + s3

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2 Let s4 = s1 + s2 + s3

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Let s3 = s1 + s2, s4 = s3 + 1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}
             Join s2 In New Integer() {2, 3}
             On s1 + 1 Equals s2
             Join s3 In New Integer() {3, 4}
             On s2 + 1 Equals s3
             Join s4 In New Integer() {4, 5}
                 Join s5 In New Integer() {5, 6}
                 On s4 + 1 Equals s5
                 Join s6 In New Integer() {6, 7}
                 On s5 + 1 Equals s6
             On s3 + 1 Equals s4

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}
             Join s2 In New Integer() {2, 3}
             On s1 + 1 Equals s2
             Join s3 In New Integer() {3, 4}
             On s2 + 1 Equals s3
             Join s4 In New Integer() {4, 5}
                 Join s5 In New Integer() {5, 6}
                 On s4 + 1 Equals s5
                 Join s6 In New Integer() {6, 7}
                 On s5 + 1 Equals s6
             On s3 + 1 Equals s4
             Select s1 + s2 + s3 + s4 + s5 + s6

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1}
             Join s2 In New Integer() {2, 3}
             On s1 + 1 Equals s2
             Join s3 In New Integer() {3, 4}
             On s2 + 1 Equals s3
             Join s4 In New Integer() {4, 5}
                 Join s5 In New Integer() {5, 6}
                 On s4 + 1 Equals s5
                 Join s6 In New Integer() {6, 7}
                 On s5 + 1 Equals s6
             On s3 + 1 Equals s4
             Let s7 = s1 + s2 + s3 + s4 + s5 + s6

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New IComparable() {New Guid("F31A2538-E129-437E-AD69-B484F979246E")}
             Join s2 In New Guid() {New Guid("F31A2538-E129-437E-AD69-B484F979246E")} On s1 Equals s2

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New String() {"1", "2"}
             Join s2 In New Integer() {2, 3} On s1 Equals s2 - 1

        For Each v In q0
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        q0 = From s1 In New Integer() {1, 2, 3, 4, 5}
             Join s2 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 * 2 Equals s2
             Join s3 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 + 1 Equals s3 And s2 - 1 Equals s3 + 1

        For Each v In q0
            System.Console.WriteLine(v)
        Next
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s1 = 3, s2 = 3 }
------
{ s1 = 1, s2 = 2 }
------
{ s1 = 1, s2 = 2, s3 = 4 }
------
{ s1 = 1, s2 = 2, s3 = 4 }
------
{ s2 = 2, s1 = 1 }
------
{ s3 = 4, s2 = 2, s1 = 1 }
------
{ s3 = 4, s2 = 2, s1 = 1 }
------
{ s1 = 1, s2 = 2, s3 = 3 }
------
{ s1 = 1, s2 = 2, s3 = 4, s4 = 7 }
------
{ s1 = 1, s2 = 2, s3 = 4, s4 = 7 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 4 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 4, s5 = 5, s6 = 6 }
------
21
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 4, s5 = 5, s6 = 6, s7 = 21 }
------
{ s1 = f31a2538-e129-437e-ad69-b484f979246e, s2 = f31a2538-e129-437e-ad69-b484f979246e }
------
{ s1 = 1, s2 = 2 }
{ s1 = 2, s2 = 3 }
------
{ s1 = 3, s2 = 6, s3 = 4 }
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub Join_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable = From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s1 Equals s2'BIND:"From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s1 Equals s2"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (Syntax: 'From s1 In  ... 1 Equals s2')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Join(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... er() {1, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {1, 3}')
              Initializer: 
                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 3}')
                  Element Values(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(4):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2, 3}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {2, 3}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2, 3}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                      ReturnedValue: 
                        IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, s2 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New I ... er() {1, 3}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                                Right: 
                                  IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub JoinMultiple_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable
        q0 = From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2'BIND:"From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) (Syntax: 'From s1 In  ... uals s2 * 2')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>).Join(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Join(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
          Instance Receiver: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {1}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                  Initializer: 
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                      Element Values(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Arguments(4):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2, 3}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {2, 3}')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2, 3}')
                          Element Values(2):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1 + 1')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                              Left: 
                                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                          ReturnedValue: 
                            IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's2')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, s2 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                              Initializers(2):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                                    Right: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s2 In  ... 1 Equals s2')
                                    Right: 
                                      IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(4):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3 In New I ... er() {4, 5}')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's3 In New I ... er() {4, 5}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {4, 5}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {4, 5}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{4, 5}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Int32), IsImplicit) (Syntax: 's3')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's3')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's3')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's3')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's2 * 2')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                              Instance Receiver: 
                                IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 's3')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 * 2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2 * 2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s3 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2 * 2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2 * 2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2 * 2')
                      ReturnedValue: 
                        IParameterReferenceOperation: s3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It1 As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, s3 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 In New I ... er() {4, 5}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join s3 In  ... uals s2 * 2')
                                Right: 
                                  IParameterReferenceOperation: s3 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Join2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object
        q0 = From s In New Integer() {1, 2} Join GetHashCode 

        q0 = From s In New Integer() {1, 2} Join s1 In

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On s

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On s Equals 

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On _ Equals _

        q0 = From s In New Integer() {1, 2} Join  In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join _ In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join 

        q0 = From s In New Integer() {1, 2} Join t

        q0 = From s In New Integer() {1, 2} Join t1 In Join t2 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join t% In New Integer() {1, 2} On s Equals t

        q0 = From s In New Integer() {1, 2} Join t% As Integer In New Integer() {1, 2} On s Equals t

        q0 = From s In New Integer() {1, 2} Join t? As Integer In New Integer() {1, 2} On s Equals t

        q0 = From s In New Integer() {1, 2} Join t? In New Integer() {1, 2} On s Equals t

        q0 = From s In New Integer() {1, 2} Join t In New Integer() {1, 2} Join

        q0 = From s In New Integer() {1, 2} Join t1 In New Integer() {1, 2} Join t2 In 

        q0 = From s In New Integer() {1, 2} Join q0 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s1 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 On s Equals s1

        q0 = From s In New Integer() {1, 2} Join s1 As Date In New Integer() {1, 2} On s Equals s1

        q0 = From s In New Integer() {1, 2} Join s1? As Byte In New Integer() {1, 2} On s Equals s1

        q0 = From s In New Integer() {1, 2} Join s1 In New String() {"1"} On s Equals s1

        q0 = From s In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 Join s1 In New Integer() {1, 2} On s Equals s1

        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 Equals s2 + s1
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 Equals s2
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 Equals s1
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 Equals 0 + 1 + 2
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 Equals 0
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 Equals 0

        q0 = From s1 In New Integer() {1}
             Join s2 In New Integer() {2, 3}
             On s1 + 1 Equals s2
             Join s3 In New Integer() {3, 4}
             On s2 + 1 Equals s3
             Join s4 In New Integer() {4, 5}
                 Join s5 In New Integer() {5, 6}
                 On s4 + 1 Equals s5
                 Join s6 In New Integer() {6, 7}
                 On s5 + 1 Equals s6
             On s1 + s4 + s6 Equals s2 + s5 + s3

        q0 = From s1 In New Integer() {1}
             Join s2 In New Integer() {2, 3}
             On s1 + 1 Equals s2
             Join s3 In New Integer() {3, 4}
             On s2 + 1 Equals s3
             Join s4 In New Integer() {4, 5}
                 Join s5 In New Integer() {5, 6}
                 On s4 + 1 Equals s5
                 Join s6 In New Integer() {6, 7}
                 On s5 + 1 Equals s6
             On s1 + s4 + s3 Equals s2 + s5 + s6

        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals 0 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 + DoesntExist Equals 0 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals 0 + 1 + 2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + s2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals 0 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s2 + DoesntExist
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + s2 + DoesntExist

        q0 = From s1 In New Date() {} Join s2 In New Guid() {} On s1 Equals s2

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2}, s2 In New Integer() {1, 2}

        q0 = From s1 In New Integer() {1, 2, 3, 4, 5}
             Join s2 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 * 2 Equals s2
             Join s3 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 + 1 Equals s3 And s2 - 1 Equals s3 + s1

        q0 = From s1 In New Integer() {1}
             Join s1 In New Integer() {10} On s1 * 2 Equals 0
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        q0 = From s In New Integer() {1, 2} Join GetHashCode 
                                                 ~~~~~~~~~~~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Join GetHashCode 
                                                             ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join GetHashCode 
                                                             ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Join s1 In
                                                      ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In
                                                      ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2}
                                                                           ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On
                                                                              ~
BC36619: 'Equals' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On s
                                                                                ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On s Equals 
                                                                                        ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On _ Equals _
                                                                               ~
BC36619: 'Equals' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On _ Equals _
                                                                               ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} On _ Equals _
                                                                               ~
BC30183: Keyword is not valid as an identifier.
        q0 = From s In New Integer() {1, 2} Join  In New Integer() {1, 2}
                                                  ~~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Join  In New Integer() {1, 2}
                                                     ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join  In New Integer() {1, 2}
                                                                     ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Join _ In New Integer() {1, 2}
                                                 ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join _ In New Integer() {1, 2}
                                                                          ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Join 
                                                 ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Join 
                                                 ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join 
                                                 ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Join t
                                                  ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t
                                                  ~
BC30451: 'Join' is not declared. It may be inaccessible due to its protection level.
        q0 = From s In New Integer() {1, 2} Join t1 In Join t2 In New Integer() {1, 2}
                                                       ~~~~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t1 In Join t2 In New Integer() {1, 2}
                                                            ~
BC36601: Type characters cannot be used in range variable declarations.
        q0 = From s In New Integer() {1, 2} Join t% In New Integer() {1, 2} On s Equals t
                                                 ~~
BC36601: Type characters cannot be used in range variable declarations.
        q0 = From s In New Integer() {1, 2} Join t% As Integer In New Integer() {1, 2} On s Equals t
                                                 ~~
BC36629: Nullable type inference is not supported in this context.
        q0 = From s In New Integer() {1, 2} Join t? In New Integer() {1, 2} On s Equals t
                                                  ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Join t In New Integer() {1, 2} Join
                                                                               ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Join t In New Integer() {1, 2} Join
                                                                               ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t In New Integer() {1, 2} Join
                                                                               ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t In New Integer() {1, 2} Join
                                                                               ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Join t1 In New Integer() {1, 2} Join t2 In 
                                                                                       ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t1 In New Integer() {1, 2} Join t2 In 
                                                                                       ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join t1 In New Integer() {1, 2} Join t2 In 
                                                                                       ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        q0 = From s In New Integer() {1, 2} Join q0 In New Integer() {1, 2}
                                                 ~~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join q0 In New Integer() {1, 2}
                                                                           ~
BC36600: Range variable 's1' is already declared.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s1 In New Integer() {1, 2}
                                                                                 ~~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s1 In New Integer() {1, 2}
                                                                                                           ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s1 In New Integer() {1, 2}
                                                                                                           ~
BC36600: Range variable 's' is already declared.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 On s Equals s1
                                                                                 ~
BC36610: Name 's' is either not declared or not in the current scope.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 On s Equals s1
                                                                                                              ~
BC30311: Value of type 'Integer' cannot be converted to 'Date'.
        q0 = From s In New Integer() {1, 2} Join s1 As Date In New Integer() {1, 2} On s Equals s1
                                                    ~~~~~~~
BC36621: 'Equals' cannot compare a value of type 'Integer' with a value of type 'Date'.
        q0 = From s In New Integer() {1, 2} Join s1 As Date In New Integer() {1, 2} On s Equals s1
                                                                                       ~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte?'.
        q0 = From s In New Integer() {1, 2} Join s1? As Byte In New Integer() {1, 2} On s Equals s1
                                                     ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Double'.
        q0 = From s In New Integer() {1, 2} Join s1 In New String() {"1"} On s Equals s1
                                                                                      ~~
BC36600: Range variable 's' is already declared.
        q0 = From s In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 Join s1 In New Integer() {1, 2} On s Equals s1
                                                 ~
BC36610: Name 's1' is either not declared or not in the current scope.
        q0 = From s In New Integer() {1, 2} Join s In New Integer() {1, 2} On s Equals s1 Join s1 In New Integer() {1, 2} On s Equals s1
                                                                                       ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 Equals s2 + s1
                                                                                     ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 Equals s2 + s1
                                                                                                    ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 Equals s2
                                                                                ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 Equals s1
                                                                                ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 Equals 0 + 1 + 2
                                                                                          ~~~~~~~~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 Equals 0
                                                                                          ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 Equals 0
                                                                                ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1' must appear on one side of the 'Equals' operator, and range variable(s) 's2' must appear on the other.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 Equals 0
                                                                                         ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s6 Equals s2 + s5 + s3
                     ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s6 Equals s2 + s5 + s3
                          ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s6 Equals s2 + s5 + s3
                                    ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s6 Equals s2 + s5 + s3
                                              ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s3 Equals s2 + s5 + s6
                     ~~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2', 's3' must appear on one side of the 'Equals' operator, and range variable(s) 's4', 's5', 's6' must appear on the other.
             On s1 + s4 + s3 Equals s2 + s5 + s6
                                    ~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + s1 + DoesntExist
                                                                                          ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + s1 + DoesntExist
                                                                                                                       ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals 0 + DoesntExist
                                                                                          ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals 0 + DoesntExist
                                                                                                                 ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s1 + DoesntExist
                                                                                          ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s1 + DoesntExist
                                                                                                                  ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + DoesntExist
                                                                                          ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + s2 + DoesntExist Equals s2 + DoesntExist
                                                                                                                  ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + DoesntExist
                                                                                    ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + DoesntExist
                                                                                                            ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s1 + DoesntExist
                                                                                    ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s1 + DoesntExist
                                                                                                            ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 + DoesntExist Equals 0 + DoesntExist
                                                                                    ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 1 + DoesntExist Equals 0 + DoesntExist
                                                                                                           ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + s1 + DoesntExist
                                                                                    ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On 0 + DoesntExist Equals s2 + s1 + DoesntExist
                                                                                                                 ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals 0 + 1 + 2 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals 0 + 1 + 2 + DoesntExist
                                                                                                                    ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s2 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s2 + DoesntExist
                                                                                                             ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + DoesntExist
                                                                                                             ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + s2 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s1 + DoesntExist Equals s1 + s2 + DoesntExist
                                                                                                                  ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals 0 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals 0 + DoesntExist
                                                                                                            ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + DoesntExist
                                                                                                             ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s2 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s2 + DoesntExist
                                                                                                             ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + s2 + DoesntExist
                                                                                     ~~~~~~~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = From s1 In New Integer() {1, 2} Join s2 In New Integer() {1, 2} On s2 + DoesntExist Equals s1 + s2 + DoesntExist
                                                                                                                  ~~~~~~~~~~~
BC36621: 'Equals' cannot compare a value of type 'Date' with a value of type 'Guid'.
        q0 = From s1 In New Date() {} Join s2 In New Guid() {} On s1 Equals s2
                                                                  ~~~~~~~~~~~~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2}, s2 In New Integer() {1, 2}
                                                                           ~
BC36622: You must reference at least one range variable on both sides of the 'Equals' operator. Range variable(s) 's1', 's2' must appear on one side of the 'Equals' operator, and range variable(s) 's3' must appear on the other.
             Join s3 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 + 1 Equals s3 And s2 - 1 Equals s3 + s1
                                                                                                                 ~~
BC36600: Range variable 's1' is already declared.
             Join s1 In New Integer() {10} On s1 * 2 Equals 0
                  ~~
    </expected>)
        End Sub

        <Fact>
        Public Sub Join3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)
        Dim qd As New QueryAble(Of Double)(0)

        Dim q0 As Object
        q0 = From s1 In qi Join s2 In qb On s1 Equals s2
        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Select s6, s5, s4, s3, s2, s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Let s7 = s6 + s5 + s4 + s3 + s2 + s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Select s6, s5, s4, s3, s2, s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Let s7 = s6 + s5 + s4 + s3 + s2 + s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Join s7 In qd On s1 Equals s7

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             From s7 In qd 

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group s1 By s2 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group By s2 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group Join s7 In qd On s1 Equals s7 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate s7 In qd Into Where(True)

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Join s2 In qb
             On s1 + 1 Equals s2
             Join s3 In qs
             On s2 + 1 Equals s3
             Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s5 + 1 Equals s6
             On s1 + s2 + s3 Equals s4 + s5 + s6
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate s7 In qd Into Where(True), Distinct
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_5`6[System.Double,System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
Where System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
Skip 0
Take 0
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_5`6[System.Double,System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Double,VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Double,VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
Where System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
Skip 0
Take 0
GroupBy 
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double],VB$AnonymousType_9`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double]]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_10`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double]]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_11`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double]]]
Select System.Func`2[VB$AnonymousType_11`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double]],VB$AnonymousType_12`8[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double],QueryAble`1[System.Double]]]
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Join4()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q0 As Object = From s1 In q Join t1 In q On s1 Equals t1'BIND:"From s1 In q Join t1 In q On s1 Equals t1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s1 In  ... 1 Equals t1')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
      Children(5):
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                ReturnedValue: 
                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
          IAnonymousFunctionOperation (Symbol: Function (t1 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 't1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 't1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 't1')
                ReturnedValue: 
                  IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 't1')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, t1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
                    Initializers(2):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In q')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
                          Right: 
                            IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 't1 In q')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>.t1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 't1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key t1 As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'Join t1 In  ... 1 Equals t1')
                          Right: 
                            IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 't1')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'Join' is not accessible in this context.
        Dim q0 As Object = From s1 In q Join t1 In q On s1 Equals t1'BIND:"From s1 In q Join t1 In q On s1 Equals t1"
                                        ~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub GroupBy1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As IEnumerable

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Count()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Count()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1, s1str = CStr(s1) By s1 = s1 Mod 2, s2 = s1 Mod 3 Into gr = Group, c = Count(), Max(s1)
            System.Console.WriteLine(v)
            For Each gv In v.gr
                System.Console.WriteLine(gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group Join s1 In New Integer() {1, 2} On key Equals s1
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Count(s1 - 2)
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s1 = 1, Group = System.Int32[] }
1
{ s1 = 2, Group = System.Int32[] }
2
2
{ s1 = 3, Group = System.Int32[] }
3
3
{ s1 = 4, Group = System.Int32[] }
4
------
{ s1 = 1, Count = 1 }
{ s1 = 2, Count = 2 }
{ s1 = 3, Count = 2 }
{ s1 = 4, Count = 1 }
------
{ s1 = 1, Group = System.Int32[] }
1
{ s1 = 2, Group = System.Int32[] }
2
2
{ s1 = 3, Group = System.Int32[] }
3
3
{ s1 = 4, Group = System.Int32[] }
4
------
{ s1 = 1, Count = 1 }
{ s1 = 2, Count = 2 }
{ s1 = 3, Count = 2 }
{ s1 = 4, Count = 1 }
------
{ s1 = 1, s2 = 1, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 1, Max = 1 }
{ s1 = 1, s1str = 1 }
{ s1 = 0, s2 = 2, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 2, Max = 2 }
{ s1 = 2, s1str = 2 }
{ s1 = 2, s1str = 2 }
{ s1 = 1, s2 = 0, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 2, Max = 3 }
{ s1 = 3, s1str = 3 }
{ s1 = 3, s1str = 3 }
{ s1 = 0, s2 = 1, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 1, Max = 4 }
{ s1 = 4, s1str = 4 }
------
{ key = 1, Group = System.Int32[] }
2
3
------
{ key = 1, Group = System.Int32[], s1 = 1 }
2
3
------
{ s1 = 1, Count = 1 }
{ s1 = 2, Count = 0 }
{ s1 = 3, Count = 2 }
{ s1 = 4, Count = 1 }
------
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy_GroupAggregation_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Module Module1
    Sub Main()
        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Group'BIND:"From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Group"
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (Syntax: 'From s1 In  ...  Into Group')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupBy(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)(keySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group By s1 Into Group')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... 3, 4, 2, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Initializer: 
                IArrayInitializerOperation (6 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3, 4, 2, 3}')
                  Element Values(6):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Group')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group By s1 Into Group')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Group')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Group')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Group')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By s1 Into Group')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By s1 Into Group')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ...  Into Group')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group By s1 Into Group')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By s1 Into Group')
                                Right: 
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group By s1 Into Group')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy_FunctionAggregation_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Count()'BIND:"From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Count()"
            System.Console.WriteLine(v)
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>)) (Syntax: 'From s1 In  ... nto Count()')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupBy(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>)(keySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By s1 Into Count()')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... 3, 4, 2, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Initializer: 
                IArrayInitializerOperation (6 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3, 4, 2, 3}')
                  Element Values(6):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Count()')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>), IsImplicit) (Syntax: 'Group By s1 Into Count()')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>.Count As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Count As System.Int32>, IsImplicit) (Syntax: 'Group By s1 Into Count()')
                                Right: 
                                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group By s1 Into Count()')
                                    Arguments(0)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy_WithOptionalGroupClause_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Group'BIND:"From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Group"
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (Syntax: 'From s1 In  ...  Into Group')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupBy(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)(keySelector As System.Func(Of System.Int32, System.Int32), elementSelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... 3, 4, 2, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Initializer: 
                IArrayInitializerOperation (6 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3, 4, 2, 3}')
                  Element Values(6):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(3):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: elementSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ...  Into Group')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
                                Right: 
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group s1 By ...  Into Group')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy_MultipleAggregations_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1, s1str = CStr(s1) By s1 = s1 Mod 2, s2 = s1 Mod 3 Into gr = Group, c = Count(), Max(s1)'BIND:"From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1, s1str = CStr(s1) By s1 = s1 Mod 2, s2 = s1 Mod 3 Into gr = Group, c = Count(), Max(s1)"
            System.Console.WriteLine(v)
            For Each gv In v.gr
                System.Console.WriteLine(gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>)) (Syntax: 'From s1 In  ... (), Max(s1)')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupBy(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>)(keySelector As System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), elementSelector As System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), resultSelector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... 3, 4, 2, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'New Integer ... 3, 4, 2, 3}')
              Initializer: 
                IArrayInitializerOperation (6 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3, 4, 2, 3}')
                  Element Values(6):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(3):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 Mod 2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>), IsImplicit) (Syntax: 's1 Mod 2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 Mod 2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 Mod 2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 Mod 2')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 = s1 Mod 2')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 Mod 2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 Mod 2')
                                    Left: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 = s1 Mod 3')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1 Mod 3')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 Mod 3')
                                    Left: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                    Right: 
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: elementSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As <anonymous type: Key s1 As System.Int32, Key s1str As System.String>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s1str As System.String>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 's1str = CStr(s1)')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s1str As System.String>.s1str As System.String (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'CStr(s1)')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String) (Syntax: 'CStr(s1)')
                                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    Operand: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>)) As <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                          Initializers(5):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 'From s1 In  ... (), Max(s1)')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>.gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'c = Count()')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>.c As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                    Arguments(0)
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Max(s1)')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>.Max As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Max(s1)')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s2 As System.Int32, Key gr As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), Key c As System.Int32, Key Max As System.Int32>, IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                Right: 
                                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>).Max(selector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Max(s1)')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>), IsImplicit) (Syntax: 'Group s1, s ... (), Max(s1)')
                                    Arguments(1):
                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
                                          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, System.Int32), IsImplicit) (Syntax: 's1')
                                            Target: 
                                              IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key s1str As System.String>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                                                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                                                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                                                    ReturnedValue: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s1str As System.String>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                                        Instance Receiver: 
                                                          IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s1str As System.String>, IsImplicit) (Syntax: 's1')
                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy_WithJoin_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group Join s1 In New Integer() {1, 2} On key Equals s1'BIND:"From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group Join s1 In New Integer() {1, 2} On key Equals s1"
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine(gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>)) (Syntax: 'From s1 In  ... y Equals s1')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>).Join(Of System.Int32, System.Int32, <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32, <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>), IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupBy(Of System.Int32, <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)(keySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group By ke ...  Into Group')
          Instance Receiver: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of System.Int32)(selector As System.Func(Of System.Int32, System.Int32)) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Select s1 + 1')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... er() {1, 2}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 2}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {1, 2}')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
                          Element Values(2):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1 + 1')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                              ReturnedValue: 
                                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                                  Left: 
                                    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: '1')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function ($VB$ItAnonymous As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '1')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '1')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
                          ReturnedValue: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (key As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                              Initializers(2):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'key =')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.key As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'key')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                                    Right: 
                                      IParameterReferenceOperation: key (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'key')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ... y Equals s1')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                                    Right: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group By ke ...  Into Group')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(4):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 In New I ... er() {1, 2}')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... er() {1, 2}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 2}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {1, 2}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'key')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32), IsImplicit) (Syntax: 'key')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'key')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'key')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'key')
                      ReturnedValue: 
                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.key As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'key')
                          Instance Receiver: 
                            IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'key')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32, <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>), IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It1 As <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, s1 As System.Int32) As <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'key =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>.key As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'key')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.key As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'key')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New I ... er() {1, 2}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key key As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32), Key s1 As System.Int32>, IsImplicit) (Syntax: 'Join s1 In  ... y Equals s1')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub GroupBy2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object

1:        q0 = From s1 In New Integer() {1} Group By Into Group

2:        q0 = From s1 In New Integer() {1} Group s2 As Integer = s1 By s1 Into Group

3:        q0 = From s1 In New Integer() {1} Group s1 By s2 As Integer =s1 Into Group

4:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into s2 As Integer =Group

5:        q0 = From s1 In New Integer() {1} Group By s1 Into

6:        q0 = From s1 In New Integer() {1} Group q0 = s1 By s1 Into Group

7:        q0 = From s1 In New Integer() {1} Group s1 By q0 = s1 Into Group

8:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into q0 = Group

9:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into s1 = Group

10:        Dim count As Integer = 0

11:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into Count()

12:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into Count

        If count > 0 Then
            Dim group As String = ""
13:            q0 = From s1 In New Integer() {1} Group s1 By s1 Into Group
        End If

14:        q0 = From s1 In New Integer() {1} Group 

15:        q0 = From s1 In New Integer() {1} Group By 

16:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into 

17:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into s2 = 

18:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into Max(s1 + s2)

19:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into s3 = Max(), s3 = Min()

20:        q0 = From s1 In New Integer() {1} Group s2 = s1, s2 = s1 By s1, s1 Into s3 = Max()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36610: Name 'Into' is either not declared or not in the current scope.
1:        q0 = From s1 In New Integer() {1} Group By Into Group
                                                     ~~~~
BC36615: 'Into' expected.
1:        q0 = From s1 In New Integer() {1} Group By Into Group
                                                          ~
BC30201: Expression expected.
1:        q0 = From s1 In New Integer() {1} Group By Into Group
                                                               ~
BC36605: 'By' expected.
1:        q0 = From s1 In New Integer() {1} Group By Into Group
                                                               ~
BC36615: 'Into' expected.
1:        q0 = From s1 In New Integer() {1} Group By Into Group
                                                               ~
BC36610: Name 's2' is either not declared or not in the current scope.
2:        q0 = From s1 In New Integer() {1} Group s2 As Integer = s1 By s1 Into Group
                                                  ~~
BC36605: 'By' expected.
2:        q0 = From s1 In New Integer() {1} Group s2 As Integer = s1 By s1 Into Group
                                                     ~
BC36615: 'Into' expected.
2:        q0 = From s1 In New Integer() {1} Group s2 As Integer = s1 By s1 Into Group
                                                     ~
BC36610: Name 's2' is either not declared or not in the current scope.
3:        q0 = From s1 In New Integer() {1} Group s1 By s2 As Integer =s1 Into Group
                                                        ~~
BC36615: 'Into' expected.
3:        q0 = From s1 In New Integer() {1} Group s1 By s2 As Integer =s1 Into Group
                                                           ~
BC36594: Definition of method 's2' is not accessible in this context.
4:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into s2 As Integer =Group
                                                                ~~
BC30205: End of statement expected.
4:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into s2 As Integer =Group
                                                                   ~~
BC36707: 'Group' or an identifier expected.
5:        q0 = From s1 In New Integer() {1} Group By s1 Into
                                                            ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
6:        q0 = From s1 In New Integer() {1} Group q0 = s1 By s1 Into Group
                                                  ~~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
7:        q0 = From s1 In New Integer() {1} Group s1 By q0 = s1 Into Group
                                                        ~~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
8:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into q0 = Group
                                                                ~~
BC36600: Range variable 's1' is already declared.
9:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into s1 = Group
                                                                     ~~
BC30978: Range variable 'Count' hides a variable in an enclosing block or a range variable previously defined in the query expression.
11:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into Count()
                                                                 ~~~~~
BC30978: Range variable 'Count' hides a variable in an enclosing block or a range variable previously defined in the query expression.
12:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into Count
                                                                 ~~~~~
BC30978: Range variable 'Group' hides a variable in an enclosing block or a range variable previously defined in the query expression.
13:            q0 = From s1 In New Integer() {1} Group s1 By s1 Into Group
                                                                     ~~~~~
BC30201: Expression expected.
14:        q0 = From s1 In New Integer() {1} Group 
                                                   ~
BC36605: 'By' expected.
14:        q0 = From s1 In New Integer() {1} Group 
                                                   ~
BC36615: 'Into' expected.
14:        q0 = From s1 In New Integer() {1} Group 
                                                   ~
BC30201: Expression expected.
15:        q0 = From s1 In New Integer() {1} Group By 
                                                      ~
BC36615: 'Into' expected.
15:        q0 = From s1 In New Integer() {1} Group By 
                                                      ~
BC36707: 'Group' or an identifier expected.
16:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into 
                                                                 ~
BC36707: 'Group' or an identifier expected.
17:        q0 = From s1 In New Integer() {1} Group s1 By s1 Into s2 = 
                                                                      ~
BC36610: Name 's1' is either not declared or not in the current scope.
18:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into Max(s1 + s2)
                                                                          ~~
BC36600: Range variable 's3' is already declared.
19:        q0 = From s1 In New Integer() {1} Group s2 = s1 By s1 Into s3 = Max(), s3 = Min()
                                                                                  ~~
BC36600: Range variable 's2' is already declared.
20:        q0 = From s1 In New Integer() {1} Group s2 = s1, s2 = s1 By s1, s1 Into s3 = Max()
                                                            ~~
BC36600: Range variable 's1' is already declared.
20:        q0 = From s1 In New Integer() {1} Group s2 = s1, s2 = s1 By s1, s1 Into s3 = Max()
                                                                           ~~
</expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupBy3()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q0 As Object = From s1 In q Group By s1 Into Group'BIND:"From s1 In q Group By s1 Into Group"

        Dim q1 As Object = From s1 In q Group s2 = s1 By s1 Into Group
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s1 In  ...  Into Group')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
      Children(3):
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                ReturnedValue: 
                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As ?) As <anonymous type: Key s1 As System.Int32, Key Group As ?>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
                    Initializers(2):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As ?>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
                          Right: 
                            IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'From s1 In  ...  Into Group')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As ?>.Group As ? (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
                          Right: 
                            IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group By s1 Into Group')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'GroupBy' is not accessible in this context.
        Dim q0 As Object = From s1 In q Group By s1 Into Group'BIND:"From s1 In q Group By s1 Into Group"
                                        ~~~~~~~~
BC36594: Definition of method 'GroupBy' is not accessible in this context.
        Dim q1 As Object = From s1 In q Group s2 = s1 By s1 Into Group
                                        ~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub GroupBy4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function GroupBy(key As Func(Of Integer, Integer), into As Action(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function GroupBy(key As Func(Of Integer, Integer), into As Func(Of Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function GroupBy(key As Func(Of Integer, Integer), into As Func(Of Byte, Integer, Integer)) As QueryAble
        Return Me
    End Function

    Public Function GroupBy(key As Func(Of Integer, Integer), into As Func(Of Integer, Integer, Integer)) As QueryAble
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()

        Dim q0 As Object = From s1 In q Group By s1 Into Group
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'GroupBy' is not accessible in this context.
        Dim q0 As Object = From s1 In q Group By s1 Into Group
                                        ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub GroupBy5()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On

Imports System

Class QueryAble(Of T)
    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        System.Console.WriteLine("        {0}", key)
        System.Console.WriteLine("        {0}", into)
        Return New QueryAble(Of R)()
    End Function
End Class

Module Module1
    Sub Main()
        Dim q1 As New QueryAble(Of Integer)()
        Dim q As Object

        q = From s In q1 Group Nothing By key = Nothing Into Group
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
GroupBy System.Func`2[System.Int32,System.Object]
        System.Func`2[System.Int32,System.Object]
        System.Func`3[System.Object,QueryAble`1[System.Object],VB$AnonymousType_0`2[System.Object,QueryAble`1[System.Object]]]
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsDesktopTypes)>
        Public Sub GroupJoin1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()

        For Each v In From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s1 Equals s2 Into Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine("    {0}", gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s2 + 1 Equals s1 + 2 Into Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine("    {0}", gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Into gr1 = Group Group Join s3 In New Integer() {4, 5} On s3 Equals (s1 + 1) * 2 Into gr2 = Group
            System.Console.WriteLine(v)
            For Each gv In v.gr1
                System.Console.WriteLine("    {0}", gv)
            Next
            For Each gv In v.gr2
                System.Console.WriteLine("        {0}", gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} Group Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Into gr1 = Group On s1 + 1 Equals s2 Into gr2 = Group
            System.Console.WriteLine(v)
            For Each gr2 In v.gr2
                System.Console.WriteLine("        {0}", gr2)
                For Each gr1 In gr2.gr1
                    System.Console.WriteLine("    {0}", gr1)
                Next
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In New Integer() {1}
                         Group Join s2 In New Integer() {2, 3}
                         On s1 + 1 Equals s2 Into g1 = Group
                         Group Join s3 In New Integer() {3, 4}
                         On s1 + 2 Equals s3 Into g2 = Group
                         Group Join s4 In New Integer() {4, 5}
                             Group Join s5 In New Integer() {5, 6}
                             On s4 + 1 Equals s5 Into g3 = Group
                             Group Join s6 In New Integer() {6, 7}
                             On s4 + 2 Equals s6 Into g4 = Group
                         On s1 + 3 Equals s4 Into g5 = Group

            System.Console.WriteLine(v)
            For Each gr1 In v.g1
                System.Console.WriteLine("    {0}", gr1)
            Next
            For Each gr2 In v.g2
                System.Console.WriteLine("        {0}", gr2)
            Next
            For Each gr5 In v.g5
                System.Console.WriteLine("                        {0}", gr5)
                For Each gr3 In gr5.g3
                    System.Console.WriteLine("            {0}", gr3)
                Next
                For Each gr4 In gr5.g4
                    System.Console.WriteLine("                {0}", gr4)
                Next
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s1 In From s1 In New Integer() {1}
                                 Group Join
                                     s2 In New Integer() {1}
                                         Join
                                             s3 In New Integer() {1}
                                         On s2 Equals s3
                                         Join
                                             s4 In New Integer() {1}
                                         On s2 Equals s4
                                 On s1 Equals s2 Into s3 = Group

            System.Console.WriteLine(v)
            For Each gv In v.s3
                System.Console.WriteLine("    {0}", gv)
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s In New Integer() {1, 2}
                      Group Join
                          s1 In New Integer() {1, 2}
                          Group Join
                              s In New Integer() {1, 2}
                              Group Join
                                  s1 In New Integer() {1, 2}
                                  Group Join
                                      s In New Integer() {1, 2}
                                  On s Equals s1 Into Group
                              On s Equals s1 Into Group
                          On s Equals s1 Into Group
                      On s Equals s1 Into Group

            System.Console.WriteLine(v)
            For Each g1 In v.Group
                System.Console.WriteLine("    {0}", g1)
                For Each g2 In g1.Group
                    System.Console.WriteLine("        {0}", g2)
                    For Each g3 In g2.Group
                        System.Console.WriteLine("            {0}", g3)
                        For Each g4 In g3.Group
                            System.Console.WriteLine("                {0}", g4)
                        Next
                    Next
                Next
            Next
        Next

        System.Console.WriteLine("------")

        For Each v In From s In New Integer() {1, 2}
                      Join
                          s1 In New Integer() {1, 2}
                          Join
                              s2 In New Integer() {1, 2}
                              Group Join
                                  s1 In New Integer() {1, 2}
                                  Group Join
                                      s In New Integer() {1, 2}
                                  On s Equals s1 Into Group
                              On s2 Equals s1 Into Group
                          On s2 Equals s1
                      On s Equals s1

            System.Console.WriteLine(v)
            For Each g1 In v.Group
                System.Console.WriteLine("    {0}", g1)
                For Each g2 In g1.Group
                    System.Console.WriteLine("        {0}", g2)
                Next
            Next
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer() {1, 2} Group Join y In New Integer() {0, 3, 4} On x + 1 Equals y Into Count(y + x), Group
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine("    {0}", gv)
            Next
        Next

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
{ s1 = 1, Group = System.Int32[] }
{ s1 = 3, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
    3
------
{ s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
    2
{ s1 = 3, Group = System.Int32[] }
------
{ s1 = 1, gr1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], gr2 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
    2
        4
------
{ s1 = 1, gr2 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_4`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
        { s2 = 2, gr1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
    4
------
{ s1 = 1, g1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g2 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g5 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_9`3[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32],System.Collections.Generic.IEnumerable`1[System.Int32]]] }
    2
        3
                        { s4 = 4, g3 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g4 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            5
                6
------
{ s1 = 1, s3 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_12`3[System.Int32,System.Int32,System.Int32]] }
    { s2 = 1, s3 = 1, s4 = 1 }
------
{ s = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]]]] }
    { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]] }
        { s = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
            { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                1
{ s = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]]]] }
    { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]] }
        { s = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
            { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                2
------
{ s = 1, s1 = 1, s2 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
    { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
        1
{ s = 2, s1 = 2, s2 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
    { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
        2
------
{ x = 1, Count = 0, Group = System.Int32[] }
{ x = 2, Count = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
    3
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupJoin_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()

        For Each v In From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s1 Equals s2 Into Group'BIND:"From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s1 Equals s2 Into Group"
            System.Console.WriteLine(v)
            For Each gv In v.Group
                System.Console.WriteLine("    {0}", gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (Syntax: 'From s1 In  ...  Into Group')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupJoin(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ...  Into Group')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New I ... er() {1, 3}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 3}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {1, 3}')
              Initializer: 
                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 3}')
                  Element Values(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      Arguments(4):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2, 3}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {2, 3}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2, 3}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                      ReturnedValue: 
                        IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ...  Into Group')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New I ... er() {1, 3}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                                Right: 
                                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ...  Into Group')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>.Group As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                                Right: 
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ...  Into Group')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupJoin_Nested_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()

        For Each v In From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Into gr1 = Group Group Join s3 In New Integer() {4, 5} On s3 Equals (s1 + 1) * 2 Into gr2 = Group'BIND:"From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Into gr1 = Group Group Join s3 In New Integer() {4, 5} On s3 Equals (s1 + 1) * 2 Into gr2 = Group"
            System.Console.WriteLine(v)
            For Each gv In v.gr1
                System.Console.WriteLine("    {0}", gv)
            Next
            For Each gv In v.gr2
                System.Console.WriteLine("        {0}", gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (Syntax: 'From s1 In  ... gr2 = Group')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>).GroupJoin(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupJoin(Of System.Int32, System.Int32, <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
          Instance Receiver: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {1}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                  Initializer: 
                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                      Element Values(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Arguments(4):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's2 In New I ... er() {2, 3}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2, 3}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {2, 3}')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2, 3}')
                          Element Values(2):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1 + 1')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1 + 1')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1 + 1')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1 + 1')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1 + 1')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                              Left: 
                                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                          ReturnedValue: 
                            IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's2')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                              Initializers(2):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                                    Right: 
                                      IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ... gr2 = Group')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>.gr1 As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                                    Right: 
                                      IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ... gr1 = Group')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(4):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3 In New I ... er() {4, 5}')
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's3 In New I ... er() {4, 5}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {4, 5}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {4, 5}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{4, 5}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Int32), IsImplicit) (Syntax: 's3')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's3')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's3')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's3')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.Multiply, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '(s1 + 1) * 2')
                          Left: 
                            IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32) (Syntax: '(s1 + 1)')
                              Operand: 
                                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 's1 + 1')
                                  Left: 
                                    IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's1')
                                      Instance Receiver: 
                                        IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 's3')
                                  Right: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(s1 + 1) * 2')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: '(s1 + 1) * 2')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s3 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '(s1 + 1) * 2')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '(s1 + 1) * 2')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '(s1 + 1) * 2')
                      ReturnedValue: 
                        IParameterReferenceOperation: s3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's3')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, System.Collections.Generic.IEnumerable(Of System.Int32), <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>), IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of System.Int32)) As <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                      ReturnedValue: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'gr1 =')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>.gr1 As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'gr1')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                                Right: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>.gr1 As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'gr1')
                                    Instance Receiver: 
                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'From s1 In  ... gr2 = Group')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>.gr2 As System.Collections.Generic.IEnumerable(Of System.Int32) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key gr1 As System.Collections.Generic.IEnumerable(Of System.Int32), Key gr2 As System.Collections.Generic.IEnumerable(Of System.Int32)>, IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
                                Right: 
                                  IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Group Join  ... gr2 = Group')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupJoin_NestedJoin_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From s1 In From s1 In New Integer() {1}'BIND:"From s1 In From s1 In New Integer() {1}"
                                 Group Join
                                     s2 In New Integer() {1}
                                         Join
                                             s3 In New Integer() {1}
                                         On s2 Equals s3
                                         Join
                                             s4 In New Integer() {1}
                                         On s2 Equals s4
                                 On s1 Equals s2 Into s3 = Group

            System.Console.WriteLine(v)
            For Each gv In v.s3
                System.Console.WriteLine("    {0}", gv)
            Next
        Next
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) (Syntax: 'From s1 In  ...  s3 = Group')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>).Select(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)(selector As System.Func(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>), IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
      Instance Receiver: 
        ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) (Syntax: 'From s1 In  ...  s3 = Group')
          Expression: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).GroupJoin(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, System.Int32, <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)(inner As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, System.Int32), resultSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>), IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's1 In New Integer() {1}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                          Element Values(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Arguments(4):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>).Join(Of System.Int32, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                      Instance Receiver: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Join(Of System.Int32, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>)(inner As System.Collections.Generic.IEnumerable(Of System.Int32), outerKeySelector As System.Func(Of System.Int32, System.Int32), innerKeySelector As System.Func(Of System.Int32, System.Int32), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's2 In New Integer() {1}')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                                  Dimension Sizes(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                                  Initializer: 
                                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                                      Element Values(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                          Arguments(4):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3 In New Integer() {1}')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's3 In New Integer() {1}')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                                      Dimension Sizes(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                                      Initializer: 
                                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                                          Element Values(1):
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's2')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                                          ReturnedValue: 
                                            IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's2')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's3')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's3')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (s3 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's3')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's3')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's3')
                                          ReturnedValue: 
                                            IParameterReferenceOperation: s3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>), IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function (s2 As System.Int32, s3 As System.Int32) As <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                          ReturnedValue: 
                                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                              Initializers(2):
                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 In New Integer() {1}')
                                                    Left: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                                        Instance Receiver: 
                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                                    Right: 
                                                      IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 In New Integer() {1}')
                                                    Left: 
                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                                        Instance Receiver: 
                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s3')
                                                    Right: 
                                                      IParameterReferenceOperation: s3 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Arguments(4):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: inner) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's4 In New Integer() {1}')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 's4 In New Integer() {1}')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                                  Dimension Sizes(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                                  Initializer: 
                                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                                      Element Values(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, System.Int32), IsImplicit) (Syntax: 's2')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                                      ReturnedValue: 
                                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                          Instance Receiver: 
                                            IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 's2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's4')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's4')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function (s4 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's4')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's4')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's4')
                                      ReturnedValue: 
                                        IParameterReferenceOperation: s4 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, System.Int32, <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                              Target: 
                                IAnonymousFunctionOperation (Symbol: Function ($VB$It1 As <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, s4 As System.Int32) As <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                      ReturnedValue: 
                                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                          Initializers(3):
                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's2 In New Integer() {1}')
                                                Left: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                                    Instance Receiver: 
                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                                Right: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's2')
                                                    Instance Receiver: 
                                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's3 In New Integer() {1}')
                                                Left: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                                    Instance Receiver: 
                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                                Right: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>.s3 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's3')
                                                    Instance Receiver: 
                                                      IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's4 In New Integer() {1}')
                                                Left: 
                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>.s4 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's4')
                                                    Instance Receiver: 
                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, IsImplicit) (Syntax: 'Join ... 2 Equals s4')
                                                Right: 
                                                  IParameterReferenceOperation: s4 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's4')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: outerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's1')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 's1')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                              ReturnedValue: 
                                IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: innerKeySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's2')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, System.Int32), IsImplicit) (Syntax: 's2')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's2')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's2')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's2')
                              ReturnedValue: 
                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>.s2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 's2')
                                  Instance Receiver: 
                                    IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>, IsImplicit) (Syntax: 's2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>), IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)) As <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                              ReturnedValue: 
                                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                                  Initializers(2):
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In New Integer() {1}')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                                        Right: 
                                          IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>.s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                                        Right: 
                                          IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>), IsImplicit) (Syntax: 'Group Join ...  s3 = Group')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>), IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s1 As <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>) As <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'From s1 In  ...  s3 = Group')
                      ReturnedValue: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: <anonymous type: Key s1 As System.Int32, Key s3 As System.Collections.Generic.IEnumerable(Of <anonymous type: Key s2 As System.Int32, Key s3 As System.Int32, Key s4 As System.Int32>)>, IsImplicit) (Syntax: 's1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub GroupJoin2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object

        q0 = From s In New Integer() {1, 2} Group Join GetHashCode 

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :

        q0 = From s In New Integer() {1, 2} Group Join  In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join _ In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join 

        q0 = From s In New Integer() {1, 2} Group Join t

        q0 = From s In New Integer() {1, 2} Group Join s1 In

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals 

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into Group, 

        q0 = From s In New Integer() {1, 2} Group Join t1 In Group Join t2 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join

        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 

        q0 = From s In New Integer() {1, 2} Group Join q0 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}

        q0 = From s In New Integer() {1, 2} Group Join s In New Integer() {1, 2} On s Equals s1 Into Group

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into s = Group

        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}, s2 In New Integer() {1, 2}

        q0 = From s1 In New Integer() {1}
                    Group Join
                        s2 In New Integer() {1}
                    On s1 Equals s2 Into s1 = Group

        q0 = From s1 In New Integer() {1}
                 Join s2 In New Integer() {1}
                        Group Join
                            s1 In New Integer() {1}
                        On s1 Equals s2 Into s1 = Group
                 On s1 Equals s2

        q0 = From s1 In New Integer() {1}
                 Join s2 In New Integer() {1}
                        Group Join
                            s1 In New Integer() {1}
                        On s1 Equals s2 Into s2 = Group
                 On s1 Equals s2


        q0 = From s In New Integer() {1, 2}
                 Group Join
                     s1 In New Integer() {1, 2}
                     Group Join
                         s2 In New Integer() {1, 2}
                         Group Join
                             s1 In New Integer() {1, 2}
                             Group Join
                                 s In New Integer() {1, 2}
                             On s Equals s1 Into Group
                         On s Equals s1 Into Group
                     On s Equals s1 Into Group
                 On s Equals s1 Into Group

        q0 = From s In New Integer() {1, 2}
                 Join
                     s1 In New Integer() {1, 2}
                     Group Join
                         s2 In New Integer() {1, 2}
                         Group Join
                             s1 In New Integer() {1, 2}
                             Group Join
                                 s In New Integer() {1, 2}
                             On s Equals s1 Into Group
                         On s Equals s1 Into Group
                     On s Equals s1 Into Group
                 On s Equals s1

        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Group s2 In New Integer() {1, 2} On s1 Equals s2 Into group On s Equals s1 

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
        q0 = From s In New Integer() {1, 2} Group Join GetHashCode 
                                                       ~~~~~~~~~~~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Group Join GetHashCode 
                                                                   ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join GetHashCode 
                                                                   ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join GetHashCode 
                                                                   ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :
                                                                                     ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :
                                                                                     ~
BC36619: 'Equals' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :
                                                                                     ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :
                                                                                     ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On _ Equals _ :
                                                                                              ~
BC30183: Keyword is not valid as an identifier.
        q0 = From s In New Integer() {1, 2} Group Join  In New Integer() {1, 2}
                                                        ~~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Group Join  In New Integer() {1, 2}
                                                           ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join  In New Integer() {1, 2}
                                                                           ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join  In New Integer() {1, 2}
                                                                           ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join _ In New Integer() {1, 2}
                                                       ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join _ In New Integer() {1, 2}
                                                                                ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join _ In New Integer() {1, 2}
                                                                                ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join 
                                                       ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Group Join 
                                                       ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join 
                                                       ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join 
                                                       ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Group Join t
                                                        ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t
                                                        ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t
                                                        ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In
                                                            ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In
                                                            ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In
                                                            ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                 ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                 ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On
                                                                                    ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On
                                                                                    ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s
                                                                                      ~
BC36619: 'Equals' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s
                                                                                      ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals 
                                                                                              ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals 
                                                                                              ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1
                                                                                                ~
BC36707: 'Group' or an identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into
                                                                                                     ~
BC36707: 'Group' or an identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into Group, 
                                                                                                             ~
BC30451: 'Group' is not declared. It may be inaccessible due to its protection level.
        q0 = From s In New Integer() {1, 2} Group Join t1 In Group Join t2 In New Integer() {1, 2}
                                                             ~~~~~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In Group Join t2 In New Integer() {1, 2}
                                                                                                  ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In Group Join t2 In New Integer() {1, 2}
                                                                                                  ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In Group Join t2 In New Integer() {1, 2}
                                                                                                  ~
BC30203: Identifier expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC36607: 'In' expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t In New Integer() {1, 2} Group Join
                                                                                           ~
BC30201: Expression expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 
                                                                                                   ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 
                                                                                                   ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 
                                                                                                   ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 
                                                                                                   ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join t1 In New Integer() {1, 2} Group Join t2 In 
                                                                                                   ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        q0 = From s In New Integer() {1, 2} Group Join q0 In New Integer() {1, 2}
                                                       ~~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join q0 In New Integer() {1, 2}
                                                                                 ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join q0 In New Integer() {1, 2}
                                                                                 ~
BC36600: Range variable 's1' is already declared.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                             ~~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                                                       ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                                                       ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                                                       ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}
                                                                                                                       ~
BC36600: Range variable 's' is already declared.
        q0 = From s In New Integer() {1, 2} Group Join s In New Integer() {1, 2} On s Equals s1 Into Group
                                                       ~
BC36610: Name 's1' is either not declared or not in the current scope.
        q0 = From s In New Integer() {1, 2} Group Join s In New Integer() {1, 2} On s Equals s1 Into Group
                                                                                             ~~
BC36600: Range variable 's' is already declared.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2} On s Equals s1 Into s = Group
                                                                                                      ~
BC36615: 'Into' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}, s2 In New Integer() {1, 2}
                                                                                 ~
BC36618: 'On' expected.
        q0 = From s In New Integer() {1, 2} Group Join s1 In New Integer() {1, 2}, s2 In New Integer() {1, 2}
                                                                                 ~
BC36600: Range variable 's1' is already declared.
                    On s1 Equals s2 Into s1 = Group
                                         ~~
BC36600: Range variable 's1' is already declared.
                        On s1 Equals s2 Into s1 = Group
                                             ~~
BC36600: Range variable 's2' is already declared.
                        On s1 Equals s2 Into s2 = Group
                                             ~~
BC36610: Name 's' is either not declared or not in the current scope.
                         On s Equals s1 Into Group
                            ~
BC36610: Name 's' is either not declared or not in the current scope.
                     On s Equals s1 Into Group
                        ~
BC36610: Name 's' is either not declared or not in the current scope.
                         On s Equals s1 Into Group
                            ~
BC36610: Name 's' is either not declared or not in the current scope.
                     On s Equals s1 Into Group
                        ~
BC36631: 'Join' expected.
        q0 = From s In New Integer() {1, 2} Join s1 In New Integer() {1, 2} Group s2 In New Integer() {1, 2} On s1 Equals s2 Into group On s Equals s1 
                                                                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub GroupJoin3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)
        Dim qu As New QueryAble(Of UInteger)(0)
        Dim ql As New QueryAble(Of Long)(0)
        Dim qd As New QueryAble(Of Double)(0)

        Dim q0 As Object
        q0 = From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Group
        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Select g1, g2, g5, s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Let s7 = s1

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Join s7 In qd On s1 Equals s7

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             From s7 In qd

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group s1 By s2 = s1 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group By s2 = s1 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Group Join s7 In qd On s1 Equals s7 Into Group

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
                 Group Join s3 In qd
                 On s4 Equals s3 Into g2 = Group
             On s1 Equals s4 Into g5 = Where(True)

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s4 In qu
                 Join s5 In ql
                 On s4 + 1 Equals s5
                 Join s6 In qd
                 On s4 + 2 Equals s6
                 Join s3 In qd
                 On s4 Equals s3
             On s1 Equals s4 Into g5 = Where(True)

        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate s7 In qd Into Where(True)


        System.Console.WriteLine("------")
        q0 = From s1 In qi
             Group Join s2 In qb
             On s1 + 1 Equals s2 Into g1 = Group
             Group Join s3 In qs
             On s1 + 2 Equals s3 Into g2 = Group
             Group Join s4 In qu
                 Group Join s5 In ql
                 On s4 + 1 Equals s5 Into g3 = Group
                 Group Join s6 In qd
                 On s4 + 2 Equals s6 Into g4 = Group
             On s1 Equals s4 Into g5 = Group
             Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate s7 In qd Into Where(True), Distinct
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_0`2[System.Int32,QueryAble`1[System.Byte]]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_7`4[QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Int32]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Int32]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Double,VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Double]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Double,VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Double]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
GroupBy 
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double],VB$AnonymousType_10`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double]]]
------
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_12`2[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_12`2[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double]],QueryAble`1[System.Double],VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[System.Int32,QueryAble`1[VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]],VB$AnonymousType_11`2[System.Int32,QueryAble`1[VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]]]]
------
Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_14`2[System.UInt32,System.Int64]]
Join System.Func`3[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_15`2[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double]]
Join System.Func`3[VB$AnonymousType_15`2[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double],System.Double,VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]]
GroupJoin System.Func`3[System.Int32,QueryAble`1[VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]],VB$AnonymousType_11`2[System.Int32,QueryAble`1[VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]]]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_17`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double]]]
------
GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_18`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double]]]
Select System.Func`2[VB$AnonymousType_18`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double]],VB$AnonymousType_19`6[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double],QueryAble`1[System.Double]]]
]]>)
        End Sub

        <Fact>
        Public Sub GroupJoin4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(0)

        Dim q0 As Object = From s1 In q Group Join t1 In q On s1 Equals t1 Into Group

        Dim q1 As Object = From s1 In q Join t1 In q Group Join t2 In q On t1 Equals t2 Into Group On s1 Equals t1

        Dim q2 As Object = From s1 In q Join t1 In q Group t2 In q On t1 Equals t2 Into Group On s1 Equals t1
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'GroupJoin' is not accessible in this context.
        Dim q0 As Object = From s1 In q Group Join t1 In q On s1 Equals t1 Into Group
                                        ~~~~~~~~~~
BC36594: Definition of method 'GroupJoin' is not accessible in this context.
        Dim q1 As Object = From s1 In q Join t1 In q Group Join t2 In q On t1 Equals t2 Into Group On s1 Equals t1
                                                     ~~~~~~~~~~
BC36594: Definition of method 'GroupJoin' is not accessible in this context.
        Dim q2 As Object = From s1 In q Join t1 In q Group t2 In q On t1 Equals t2 Into Group On s1 Equals t1
                                                     ~~~~~
BC36631: 'Join' expected.
        Dim q2 As Object = From s1 In q Join t1 In q Group t2 In q On t1 Equals t2 Into Group On s1 Equals t1
                                                           ~
</expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub GroupJoin5()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of I, R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(0)

        Dim q0 As Object = From s1 In q Group Join t1 In q On s1 Equals t1 Into Group'BIND:"From s1 In q Group Join t1 In q On s1 Equals t1 Into Group"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From s1 In  ...  Into Group')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
      Children(5):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
            Children(1):
                ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble(Of System.Int32)) (Syntax: 'q')
          ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble(Of System.Int32)) (Syntax: 'q')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's1')
                ReturnedValue: 
                  IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's1')
          IAnonymousFunctionOperation (Symbol: Function (t1 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 't1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 't1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 't1')
                ReturnedValue: 
                  IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 't1')
          IAnonymousFunctionOperation (Symbol: Function (s1 As System.Int32, $VB$ItAnonymous As ?) As <anonymous type: Key s1 As System.Int32, Key Group As ?>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                    Initializers(2):
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 's1 In q')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As ?>.s1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                          Right: 
                            IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 's1')
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'From s1 In  ...  Into Group')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key s1 As System.Int32, Key Group As ?>.Group As ? (OperationKind.PropertyReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                              Instance Receiver: 
                                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key s1 As System.Int32, Key Group As ?>, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
                          Right: 
                            IParameterReferenceOperation: $VB$ItAnonymous (OperationKind.ParameterReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Group Join  ...  Into Group')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'GroupJoin' is not accessible in this context.
        Dim q0 As Object = From s1 In q Group Join t1 In q On s1 Equals t1 Into Group'BIND:"From s1 In q Group Join t1 In q On s1 Equals t1 Into Group"
                                        ~~~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Aggregate1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count())
        System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count(), Sum(y \ 2))
        System.Console.WriteLine(Aggregate x In New Integer() {3, 4}, y In New Integer() {1, 3} Where x > y Into Sum(x + y))

        System.Console.WriteLine("------")
        For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate y In New Integer() {3, 4} Into Count()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate y In New Integer() {3, 4} Into Count(), Sum()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer()() {New Integer() {3, 4}} Aggregate y In x Into Sum()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer()() {New Integer() {3, 4}} Aggregate y In x Into Sum(), Count()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer()() {New Integer() {3, 4}} From z In x Aggregate y In x Into Sum(z + y)
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer()() {New Integer() {3, 4}} From z In x Aggregate y In x Into Sum(z + y), Count()
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)
            For Each vv In v
                System.Console.WriteLine(vv)
            Next
        Next

        System.Console.WriteLine("------")
        For Each v In Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3}
                          Where True Order By x Distinct Take While True Skip While False Skip 0 Take 100
                          Select x, y, z Let w = x + y + z
                      Into Where(True)
            System.Console.WriteLine(v)
        Next

        System.Console.WriteLine("------")
        For Each v In From x In New Integer() {3, 4} Select x + 1
                      Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3}
                          Where True Order By x Distinct Take While True Skip While False Skip 0 Take 100
                          Select x, y, z Let w = x + y + z
                      Into Where(True)
            For Each vv In v
                System.Console.WriteLine(vv)
            Next
        Next

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, references:={LinqAssemblyRef},
                                expectedOutput:=
            <![CDATA[
2
{ Count = 2, Sum = 3 }
16
------
2
2
------
{ Count = 2, Sum = 7 }
{ Count = 2, Sum = 7 }
------
{ x = System.Int32[], Sum = 7 }
------
{ x = System.Int32[], Sum = 7, Count = 2 }
------
{ x = System.Int32[], z = 3, Sum = 13 }
{ x = System.Int32[], z = 4, Sum = 15 }
------
{ x = System.Int32[], z = 3, Sum = 13, Count = 2 }
{ x = System.Int32[], z = 4, Sum = 15, Count = 2 }
------
{ x = 1, y = 2, z = 3 }
------
{ x = 1, y = 2, z = 3 }
{ x = 1, y = 2, z = 3 }
------
{ x = 1, y = 2, z = 3, w = 6 }
------
{ x = 1, y = 2, z = 3, w = 6 }
{ x = 1, y = 2, z = 3, w = 6 }
]]>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AggregateClause_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count())'BIND:"Aggregate y In New Integer() {3, 4} Into Count()"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Int32) (Syntax: 'Aggregate y ... nto Count()')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
      Instance Receiver: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'y In New In ... er() {3, 4}')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3, 4}')
              Dimension Sizes(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {3, 4}')
              Initializer: 
                IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3, 4}')
                  Element Values(2):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AggregateClause_MultipleAggregations_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count(), Sum(y \ 2)) 'BIND:"Aggregate y In New Integer() {3, 4} Into Count(), Sum(y \ 2)"'BIND:"Aggregate y In New Integer() {3, 4} Into Count(), Sum(y \ 2)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
  Expression: 
    IAggregateQueryOperation (OperationKind.None, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
      Group: 
        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3, 4}')
          Dimension Sizes(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {3, 4}')
          Initializer: 
            IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3, 4}')
              Element Values(2):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
      Aggregation: 
        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
          Initializers(2):
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                Left: 
                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>.Count As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                Right: 
                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                    Instance Receiver: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IPlaceholderOperation (OperationKind.None, Type: System.Int32(), IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                    Arguments(0)
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y \ 2)')
                Left: 
                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>.Sum As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y \ 2)')
                    Instance Receiver: 
                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                Right: 
                  IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Sum(selector As System.Func(Of System.Int32, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y \ 2)')
                    Instance Receiver: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IPlaceholderOperation (OperationKind.None, Type: System.Int32(), IsImplicit) (Syntax: 'Aggregate y ...  Sum(y \ 2)')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y \ 2')
                          IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'y \ 2')
                            Target: 
                              IAnonymousFunctionOperation (Symbol: Function (y As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'y \ 2')
                                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'y \ 2')
                                  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'y \ 2')
                                    ReturnedValue: 
                                      IBinaryOperation (BinaryOperatorKind.IntegerDivide, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'y \ 2')
                                        Left: 
                                          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                                        Right: 
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AggregateClause_WithWhereFilter_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()        
        System.Console.WriteLine(Aggregate x In New Integer() {3, 4}, y In New Integer() {1, 3} Where x > y Into Sum(x + y))'BIND:"Aggregate x In New Integer() {3, 4}, y In New Integer() {1, 3} Where x > y Into Sum(x + y)"
      End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Int32) (Syntax: 'Aggregate x ...  Sum(x + y)')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>).Sum(selector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Sum(x + y)')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>).Where(predicate As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'Where x > y')
          Instance Receiver: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).SelectMany(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)(collectionSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In New In ... er() {3, 4}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3, 4}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {3, 4}')
                      Initializer: 
                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3, 4}')
                          Element Values(2):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {1, 3}')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), IsImplicit) (Syntax: 'New Integer() {1, 3}')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {1, 3}')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {1, 3}')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {1, 3}')
                              ReturnedValue: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 3}')
                                      Dimension Sizes(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {1, 3}')
                                      Initializer: 
                                        IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 3}')
                                          Element Values(2):
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Aggregate x ...  Sum(x + y)')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'Aggregate x ...  Sum(x + y)')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (x As System.Int32, y As System.Int32) As <anonymous type: Key x As System.Int32, Key y As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Aggregate x ...  Sum(x + y)')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Aggregate x ...  Sum(x + y)')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Aggregate x ...  Sum(x + y)')
                              ReturnedValue: 
                                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
                                  Initializers(2):
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In New In ... er() {3, 4}')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
                                        Right: 
                                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New In ... er() {1, 3}')
                                        Right: 
                                          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x > y')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'x > y')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x > y')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x > y')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x > y')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'x > y')
                              Left: 
                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                  Instance Receiver: 
                                    IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x > y')
                              Right: 
                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                                  Instance Receiver: 
                                    IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x > y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x + y')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32), IsImplicit) (Syntax: 'x + y')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x + y')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x + y')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + y')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
                          Left: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                              Instance Receiver: 
                                IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x + y')
                          Right: 
                            IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                              Instance Receiver: 
                                IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x + y')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AggregateClause_MultipleRangeVariableDeclarations_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)'BIND:"Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)"
            System.Console.WriteLine(v)
        Next
      End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) (Syntax: 'Aggregate x ... Where(True)')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>).Where(predicate As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>), IsImplicit) (Syntax: 'Where(True)')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>).SelectMany(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)(collectionSelector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Collections.Generic.IEnumerable(Of System.Int32)), resultSelector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>), IsImplicit) (Syntax: 'z In New Integer() {3}')
          Instance Receiver: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).SelectMany(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)(collectionSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'y In New Integer() {2}')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In New Integer() {1}')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                      Dimension Sizes(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                      Initializer: 
                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                          Element Values(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), IsImplicit) (Syntax: 'New Integer() {2}')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                              ReturnedValue: 
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'y In New Integer() {2}')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: 
                                    IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2}')
                                      Dimension Sizes(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {2}')
                                      Initializer: 
                                        IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2}')
                                          Element Values(1):
                                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                      Target: 
                        IAnonymousFunctionOperation (Symbol: Function (x As System.Int32, y As System.Int32) As <anonymous type: Key x As System.Int32, Key y As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                              ReturnedValue: 
                                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                  Initializers(2):
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In New Integer() {1}')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                        Right: 
                                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                        Left: 
                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                            Instance Receiver: 
                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                        Right: 
                                          IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Collections.Generic.IEnumerable(Of System.Int32)), IsImplicit) (Syntax: 'New Integer() {3}')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32>) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                          ReturnedValue: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'z In New Integer() {3}')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3}')
                                  Dimension Sizes(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {3}')
                                  Initializer: 
                                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3}')
                                      Element Values(1):
                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>), IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function ($VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, z As System.Int32) As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                          ReturnedValue: 
                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                              Initializers(3):
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In New Integer() {1}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                    Right: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                    Right: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                        Instance Receiver: 
                                          IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                    Left: 
                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                        Instance Receiver: 
                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                    Right: 
                                      IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'True')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'True')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'True')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'True')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'True')
                      ReturnedValue: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub AggregateClause_WithDifferentClauses_IOperation()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        For Each v In From x In New Integer() {3, 4} Select x + 1'BIND:"From x In New Integer() {3, 4} Select x + 1"
                      Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3}
                          Where True Order By x Distinct Take While True Skip While False Skip 0 Take 100
                          Select x, y, z Let w = x + y + z
                      Into Where(True)
            For Each vv In v
                System.Console.WriteLine(vv)
            Next
        Next

      End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>))) (Syntax: 'From x In N ... Where(True)')
  Expression: 
    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>))(selector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>))) As System.Collections.Generic.IEnumerable(Of System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>))) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)), IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
      Instance Receiver: 
        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of System.Int32)(selector As System.Func(Of System.Int32, System.Int32)) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Select x + 1')
          Instance Receiver: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In New In ... er() {3, 4}')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3, 4}')
                  Dimension Sizes(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: 'New Integer() {3, 4}')
                  Initializer: 
                    IArrayInitializerOperation (2 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3, 4}')
                      Element Values(2):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x + 1')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'x + 1')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x + 1')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x + 1')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + 1')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + 1')
                              Left: 
                                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {1}')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)), IsImplicit) (Syntax: 'New Integer() {1}')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function ($VB$ItAnonymous As System.Int32) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {1}')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {1}')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {1}')
                      ReturnedValue: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>).Where(predicate As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>), IsImplicit) (Syntax: 'Where(True)')
                          Instance Receiver: 
                            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>).Select(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)(selector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>), IsImplicit) (Syntax: 'w = x + y + z')
                              Instance Receiver: 
                                IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).Select(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)(selector As System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>), IsImplicit) (Syntax: 'Select x, y, z')
                                  Instance Receiver: 
                                    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).Take(count As System.Int32) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Take 100')
                                      Instance Receiver: 
                                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).Skip(count As System.Int32) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Skip 0')
                                          Instance Receiver: 
                                            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).SkipWhile(predicate As System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Skip While False')
                                              Instance Receiver: 
                                                IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).TakeWhile(predicate As System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Take While True')
                                                  Instance Receiver: 
                                                    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).Distinct() As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Distinct')
                                                      Instance Receiver: 
                                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Order By x')
                                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                          Operand: 
                                                            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).OrderBy(Of System.Int32)(keySelector As System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Int32)) As System.Linq.IOrderedEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Linq.IOrderedEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'x')
                                                              Instance Receiver: 
                                                                IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>).Where(predicate As System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Where True')
                                                                  Instance Receiver: 
                                                                    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>).SelectMany(Of System.Int32, <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)(collectionSelector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Collections.Generic.IEnumerable(Of System.Int32)), resultSelector As System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32, <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                      Instance Receiver: 
                                                                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).SelectMany(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)(collectionSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), resultSelector As System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                          Instance Receiver: 
                                                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In New Integer() {1}')
                                                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                                              Operand: 
                                                                                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1}')
                                                                                  Dimension Sizes(1):
                                                                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {1}')
                                                                                  Initializer: 
                                                                                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1}')
                                                                                      Element Values(1):
                                                                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                                                                          Arguments(2):
                                                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.Int32)), IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                  Target: 
                                                                                    IAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                          ReturnedValue: 
                                                                                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                                                              Operand: 
                                                                                                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {2}')
                                                                                                  Dimension Sizes(1):
                                                                                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {2}')
                                                                                                  Initializer: 
                                                                                                    IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{2}')
                                                                                                      Element Values(1):
                                                                                                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                                                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                  Target: 
                                                                                    IAnonymousFunctionOperation (Symbol: Function (x As System.Int32, y As System.Int32) As <anonymous type: Key x As System.Int32, Key y As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                          ReturnedValue: 
                                                                                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                                              Initializers(2):
                                                                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In New Integer() {1}')
                                                                                                    Left: 
                                                                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                                                        Instance Receiver: 
                                                                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                                                    Right: 
                                                                                                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                                                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                                                    Left: 
                                                                                                      IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                                                                        Instance Receiver: 
                                                                                                          IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y In New Integer() {2}')
                                                                                                    Right: 
                                                                                                      IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                      Arguments(2):
                                                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                                                                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Collections.Generic.IEnumerable(Of System.Int32)), IsImplicit) (Syntax: 'New Integer() {3}')
                                                                              Target: 
                                                                                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32>) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                                                                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                                                                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'New Integer() {3}')
                                                                                      ReturnedValue: 
                                                                                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                                                          Operand: 
                                                                                            IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {3}')
                                                                                              Dimension Sizes(1):
                                                                                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'New Integer() {3}')
                                                                                              Initializer: 
                                                                                                IArrayInitializerOperation (1 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{3}')
                                                                                                  Element Values(1):
                                                                                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                                                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>, System.Int32, <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>), IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                              Target: 
                                                                                IAnonymousFunctionOperation (Symbol: Function ($VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, z As System.Int32) As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                      ReturnedValue: 
                                                                                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                          Initializers(2):
                                                                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'Aggregate x ... Where(True)')
                                                                                                Left: 
                                                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.$VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                                    Instance Receiver: 
                                                                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                                Right: 
                                                                                                  IParameterReferenceOperation: $VB$It1 (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                                Left: 
                                                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                                                                    Instance Receiver: 
                                                                                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'z In New Integer() {3}')
                                                                                                Right: 
                                                                                                  IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                  Arguments(1):
                                                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'True')
                                                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'True')
                                                                          Target: 
                                                                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'True')
                                                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'True')
                                                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'True')
                                                                                  ReturnedValue: 
                                                                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
                                                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                              Arguments(1):
                                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: keySelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                                                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Int32), IsImplicit) (Syntax: 'x')
                                                                      Target: 
                                                                        IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                                                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                                                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                                                              ReturnedValue: 
                                                                                IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                                                                  Instance Receiver: 
                                                                                    IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.$VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x')
                                                                                      Instance Receiver: 
                                                                                        IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'x')
                                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                      Arguments(0)
                                                  Arguments(1):
                                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'True')
                                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'True')
                                                          Target: 
                                                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'True')
                                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'True')
                                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'True')
                                                                  ReturnedValue: 
                                                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
                                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Arguments(1):
                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'False')
                                                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'False')
                                                      Target: 
                                                        IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'False')
                                                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'False')
                                                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'False')
                                                              ReturnedValue: 
                                                                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'False')
                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                          Arguments(1):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      Arguments(1):
                                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '100')
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100) (Syntax: '100')
                                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>), IsImplicit) (Syntax: 'x')
                                          Target: 
                                            IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>) As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                                                  ReturnedValue: 
                                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'Select x, y, z')
                                                      Initializers(3):
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'Select x, y, z')
                                                            Right: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                                                Instance Receiver: 
                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.$VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x')
                                                                    Instance Receiver: 
                                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'x')
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'Select x, y, z')
                                                            Right: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                                                                Instance Receiver: 
                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.$VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'x')
                                                                    Instance Receiver: 
                                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'x')
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'Select x, y, z')
                                                            Right: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'z')
                                                                Instance Receiver: 
                                                                  IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key $VB$It1 As <anonymous type: Key x As System.Int32, Key y As System.Int32>, Key z As System.Int32>, IsImplicit) (Syntax: 'x')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x + y + z')
                                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>), IsImplicit) (Syntax: 'x + y + z')
                                      Target: 
                                        IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>) As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x + y + z')
                                          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x + y + z')
                                            IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + y + z')
                                              ReturnedValue: 
                                                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, IsImplicit) (Syntax: 'w = x + y + z')
                                                  Initializers(4):
                                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                        Left: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                            Instance Receiver: 
                                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, IsImplicit) (Syntax: 'w = x + y + z')
                                                        Right: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                            Instance Receiver: 
                                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                        Left: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                            Instance Receiver: 
                                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, IsImplicit) (Syntax: 'w = x + y + z')
                                                        Right: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'y')
                                                            Instance Receiver: 
                                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                        Left: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                            Instance Receiver: 
                                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, IsImplicit) (Syntax: 'w = x + y + z')
                                                        Right: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                                                            Instance Receiver: 
                                                              IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'w = x + y + z')
                                                        Left: 
                                                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>.w As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x + y + z')
                                                            Instance Receiver: 
                                                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, IsImplicit) (Syntax: 'w = x + y + z')
                                                        Right: 
                                                          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y + z')
                                                            Left: 
                                                              IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
                                                                Left: 
                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'x')
                                                                    Instance Receiver: 
                                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                                                Right: 
                                                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'y')
                                                                    Instance Receiver: 
                                                                      IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                                            Right: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>.z As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'z')
                                                                Instance Receiver: 
                                                                  IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32>, IsImplicit) (Syntax: 'x + y + z')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: predicate) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'True')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>, System.Boolean), IsImplicit) (Syntax: 'True')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key x As System.Int32, Key y As System.Int32, Key z As System.Int32, Key w As System.Int32>) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'True')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'True')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'True')
                                          ReturnedValue: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Aggregate2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq


Module Module1
    Sub Main()
        Dim q0 As Object

        q0 = Aggregate

        q0 = Aggregate s

        q0 = Aggregate s In

        q0 = Aggregate s In New Integer() {1, 2}

        q0 = Aggregate s In New Integer() {1, 2} Into 

        q0 = Aggregate s In New Integer() {1, 2} Into Group

        q0 = Aggregate s In New Integer() {1, 2} Into [Group]

        q0 = Aggregate s In New Integer() {1, 2} Into s

        q0 = Aggregate s In New Integer() {1, 2} Into n=

        q0 = Aggregate s In New Integer() {1, 2} Into n= ,

        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2

        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2=

        q0 = Aggregate s In New Integer() {1, 2} Into q0 = Count()

        q0 = Aggregate s In New Integer() {1, 2} Into s = Count()

        q0 = Aggregate s In New Integer() {1, 2} Into s = Count(), s = Max()


        q0 = From x In New Integer() {3, 4} Aggregate

        q0 = From x In New Integer() {3, 4} Aggregate s

        q0 = From x In New Integer() {3, 4} Aggregate s In

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2}

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into 

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into Group

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into [Group]

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into s

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into x

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n=

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n= ,

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2=

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into q0 = Count()

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into s = Count()

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into x = Count()

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into s = Count(), s = Max()

        q0 = Aggregate s In New Integer() {1, 2} Into Count() Where True

        q0 = Aggregate s In New Integer() {1, 2} Into Where(DoesntExist)

        q0 = From x In New Integer() {3, 4} Select x + 1 Aggregate s In New Integer() {1, 2} 

        q0 = Aggregate s In New Integer() {1, 2} Into Group()

        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into Group()

        q0 = Aggregate x In "" Into c = Count%
End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'Aggregate' is not declared. It may be inaccessible due to its protection level.
        q0 = Aggregate
             ~~~~~~~~~
BC36607: 'In' expected.
        q0 = Aggregate s
                        ~
BC36615: 'Into' expected.
        q0 = Aggregate s
                        ~
BC30201: Expression expected.
        q0 = Aggregate s In
                           ~
BC36615: 'Into' expected.
        q0 = Aggregate s In
                           ~
BC36615: 'Into' expected.
        q0 = Aggregate s In New Integer() {1, 2}
                                                ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into 
                                                      ~
BC36708: 'Group' not allowed in this context; identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into Group
                                                      ~~~~~
BC36594: Definition of method 'Group' is not accessible in this context.
        q0 = Aggregate s In New Integer() {1, 2} Into [Group]
                                                      ~~~~~~~
BC36594: Definition of method 's' is not accessible in this context.
        q0 = Aggregate s In New Integer() {1, 2} Into s
                                                      ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n=
                                                        ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n= ,
                                                         ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n= ,
                                                          ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2
                                                          ~
BC36594: Definition of method 'n2' is not accessible in this context.
        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2
                                                            ~~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2=
                                                          ~
BC30203: Identifier expected.
        q0 = Aggregate s In New Integer() {1, 2} Into n1= , n2=
                                                               ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        q0 = Aggregate s In New Integer() {1, 2} Into q0 = Count()
                                                      ~~
BC36600: Range variable 's' is already declared.
        q0 = Aggregate s In New Integer() {1, 2} Into s = Count(), s = Max()
                                                                   ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate
                                                     ~
BC36607: 'In' expected.
        q0 = From x In New Integer() {3, 4} Aggregate
                                                     ~
BC36615: 'Into' expected.
        q0 = From x In New Integer() {3, 4} Aggregate
                                                     ~
BC36607: 'In' expected.
        q0 = From x In New Integer() {3, 4} Aggregate s
                                                       ~
BC36615: 'Into' expected.
        q0 = From x In New Integer() {3, 4} Aggregate s
                                                       ~
BC30201: Expression expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In
                                                          ~
BC36615: 'Into' expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In
                                                          ~
BC36615: 'Into' expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2}
                                                                               ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into 
                                                                                     ~
BC36708: 'Group' not allowed in this context; identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into Group
                                                                                     ~~~~~
BC36594: Definition of method 'Group' is not accessible in this context.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into [Group]
                                                                                     ~~~~~~~
BC36594: Definition of method 's' is not accessible in this context.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into s
                                                                                     ~
BC36594: Definition of method 'x' is not accessible in this context.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into x
                                                                                     ~
BC36600: Range variable 'x' is already declared.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into x
                                                                                     ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n=
                                                                                       ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n= ,
                                                                                        ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n= ,
                                                                                         ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2
                                                                                         ~
BC36594: Definition of method 'n2' is not accessible in this context.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2
                                                                                           ~~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2=
                                                                                         ~
BC30203: Identifier expected.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into n1= , n2=
                                                                                              ~
BC30978: Range variable 'q0' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into q0 = Count()
                                                                                     ~~
BC36600: Range variable 'x' is already declared.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into x = Count()
                                                                                     ~
BC36600: Range variable 's' is already declared.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into s = Count(), s = Max()
                                                                                                  ~
BC30205: End of statement expected.
        q0 = Aggregate s In New Integer() {1, 2} Into Count() Where True
                                                              ~~~~~
BC36610: Name 'DoesntExist' is either not declared or not in the current scope.
        q0 = Aggregate s In New Integer() {1, 2} Into Where(DoesntExist)
                                                            ~~~~~~~~~~~
BC36615: 'Into' expected.
        q0 = From x In New Integer() {3, 4} Select x + 1 Aggregate s In New Integer() {1, 2} 
                                                                                             ~
BC30183: Keyword is not valid as an identifier.
        q0 = Aggregate s In New Integer() {1, 2} Into Group()
                                                      ~~~~~
BC36594: Definition of method 'Group' is not accessible in this context.
        q0 = Aggregate s In New Integer() {1, 2} Into Group()
                                                      ~~~~~
BC30183: Keyword is not valid as an identifier.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into Group()
                                                                                     ~~~~~
BC36594: Definition of method 'Group' is not accessible in this context.
        q0 = From x In New Integer() {3, 4} Aggregate s In New Integer() {1, 2} Into Group()
                                                                                     ~~~~~
BC36617: Aggregate function name cannot be used with a type character.
        q0 = Aggregate x In "" Into c = Count%
                                        ~~~~~~
</expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Aggregate2b()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class cls1
    Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Shared Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)'BIND:"Aggregate i In colm Into aggr10(10)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'Aggregate i ...  aggr10(10)')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'aggr10(10)')
      Children(2):
          ILocalReferenceOperation: colm (OperationKind.LocalReference, Type: cls1) (Syntax: 'colm')
          IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '10')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '10')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '10')
                ReturnedValue: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'aggr10' is not accessible in this context.
        Dim q10m = Aggregate i In colm Into aggr10(10)'BIND:"Aggregate i In colm Into aggr10(10)"
                                            ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Aggregate2c()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class cls1
    Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Shared Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
    Shared Function aggr10(ByVal sel As Func(Of Integer, Double)) As Object
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)'BIND:"Aggregate i In colm Into aggr10(10)"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'Aggregate i ...  aggr10(10)')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'aggr10(10)')
      Children(2):
          ILocalReferenceOperation: colm (OperationKind.LocalReference, Type: cls1) (Syntax: 'colm')
          IAnonymousFunctionOperation (Symbol: Function (i As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '10')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '10')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '10')
                ReturnedValue: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36594: Definition of method 'aggr10' is not accessible in this context.
        Dim q10m = Aggregate i In colm Into aggr10(10)'BIND:"Aggregate i In colm Into aggr10(10)"
                                            ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <Fact>
        Public Sub Aggregate2d()
            Dim compilationDef =
<compilation name="Aggregate2d">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class cls1
    Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Shared Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
    Function aggr10(ByVal sel As Func(Of Integer, Double)) As Object
        Return Nothing
    End Function
End Class
 
Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub Aggregate2e()
            Dim compilationDef =
<compilation name="Aggregate2e">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Public Module UserDefinedAggregates

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Aggr10(ByVal values As cls1, ByVal selector As Func(Of Integer, Integer)) As Double
        Return 1
    End Function

End Module

Public Class cls1
    Public Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Public Shared Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub Aggregate2f()
            Dim compilationDef =
<compilation name="Aggregate2f">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Public Module UserDefinedAggregates

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Public Function Aggr10(ByVal values As cls1, ByVal selector As Func(Of Integer, Integer)) As Double
        Return 1
    End Function

End Module

Public Class cls1
    Public Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Private Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub Aggregate2g()
            Dim compilationDef =
<compilation name="Aggregate2g">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class cls1
    Function [Select](Of S)(ByVal sel As Func(Of Integer, S)) As cls1
        Return Nothing
    End Function
    Private Function aggr10(ByVal sel As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class
 
Module Module1
    Sub Main()
        Dim colm As New cls1
        Dim q10m = Aggregate i In colm Into aggr10(10)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'cls1.Private Function aggr10(sel As Func(Of Integer, Integer)) As Object' is not accessible in this context because it is 'Private'.
        Dim q10m = Aggregate i In colm Into aggr10(10)
                                            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Aggregate3()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Class QueryAble(Of T)
    'Inherits Base

    'Public Shadows [Select] As Byte
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
        System.Console.WriteLine("SelectMany {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("SkipWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("OrderBy {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Skip(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Skip {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Take(count As Integer) As QueryAble(Of T)
        System.Console.WriteLine("Take {0}", count)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
        System.Console.WriteLine("Join {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy {0}", item)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupBy ")
        Return New QueryAble(Of R)(v + 1)
    End Function

    Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        System.Console.WriteLine("GroupJoin {0}", x)
        Return New QueryAble(Of R)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim qb As New QueryAble(Of Byte)(0)
        Dim qs As New QueryAble(Of Short)(0)

        Dim q0 As Object

        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Select Where, t, s

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Select Distinct, Where, t, s

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Let t4 = 1

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Let t4 = 1

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             From t4 In qs

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             From t4 In qs

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Join t4 In qs On s Equals t4

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Join t4 In qs On s Equals t4

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group s By t Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group s By t Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group By t Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group By t Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group Join t4 In qs On t Equals t4 Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Group Join t4 In qs On t Equals t4 Into Group

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate t4 In qs Into w = Where(True)

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate t4 In qs Into w = Where(True)

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True)
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate t4 In qs Into w = Where(True), d = Distinct()

        System.Console.WriteLine("------")
        q0 = From s In qi Let t = s + 1
             Aggregate x In qb Into Where(True), Distinct()
             Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
             Aggregate t4 In qs Into w = Where(True), d = Distinct()

        System.Console.WriteLine("------")
        q0 = From i In qi, b In qb
             Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b)

        System.Console.WriteLine("------")
        q0 = From i In qi, b In qb
             Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b), Distinct()

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b)

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i
             Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b), Distinct()

        System.Console.WriteLine("------")
        q0 = From i In qi Select i + 1 From b In qb
        Aggregate s In qs Where s < b Into Where(s < b)

        System.Console.WriteLine("------")
        q0 = From i In qi Select i + 1 From b In qb
        Aggregate s In qs Where s < b Into Where(s < b), Distinct()

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i From ii As Long In qi
             Aggregate s In qs Where s < b Into Where(s < b)
             Select Where, ii, b, i

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb On b Equals i From ii As Long In qi
             Aggregate s In qs Where s < b Into Where(s < b), Distinct()
             Select Distinct, Where, ii, b, i

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb Join ii As Long In qi On b Equals ii On b Equals i
             Aggregate s In qs Where s < b Into Where(s < b)
             Select Where, ii, b, i

        System.Console.WriteLine("------")
        q0 = From i In qi Join b In qb Join ii As Long In qi On b Equals ii On b Equals i
             Aggregate s In qs Where s < b Into Where(s < b), Distinct()
             Select Distinct, Where, ii, b, i
    End Sub
End Module
    ]]></file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_5`3[QueryAble`1[System.Byte],System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_7`4[QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int16]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int16]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int16]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int16]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupBy 
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupBy 
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_11`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_12`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_13`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_14`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_2`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],VB$AnonymousType_15`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_2`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],VB$AnonymousType_16`6[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_17`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_19`4[System.Int32,System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_17`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_19`4[System.Int32,System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_20`2[System.Byte,QueryAble`1[System.Int16]]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_21`2[System.Byte,QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_21`2[System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_22`3[System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_23`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,VB$AnonymousType_24`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_24`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]],VB$AnonymousType_25`4[QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
------
Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_23`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,VB$AnonymousType_26`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_26`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]],VB$AnonymousType_27`4[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_27`4[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16],QueryAble`1[System.Int16]],VB$AnonymousType_28`5[QueryAble`1[System.Int16],QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
------
Select System.Func`2[System.Int32,System.Int64]
Join System.Func`3[System.Byte,System.Int64,VB$AnonymousType_29`2[System.Byte,System.Int64]]
Join System.Func`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],VB$AnonymousType_30`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_30`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]],VB$AnonymousType_25`4[QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
------
Select System.Func`2[System.Int32,System.Int64]
Join System.Func`3[System.Byte,System.Int64,VB$AnonymousType_29`2[System.Byte,System.Int64]]
Join System.Func`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],VB$AnonymousType_31`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_31`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]],VB$AnonymousType_32`4[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
Select System.Func`2[VB$AnonymousType_32`4[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16],QueryAble`1[System.Int16]],VB$AnonymousType_28`5[QueryAble`1[System.Int16],QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
]]>)
        End Sub

        <Fact>
        Public Sub Aggregate4()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](x As Func(Of T, Integer)) As QueryAble(Of Integer)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of Integer)(v + 1)
    End Function

    Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Public Function Distinct() As QueryAble(Of T)
        System.Console.WriteLine("Distinct")
        Return New QueryAble(Of T)(v + 1)
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble(Of Integer)(0)

        Dim q0 As Object 

        q0 = Aggregate s1 In q Into Where(True)
        q0 = Aggregate s1 In q Into Where(True), Distinct

        q0 = From s0 in q Aggregate s1 In q Into Where(True)
        q0 = From s0 in q Aggregate s1 In q Into Where(True), Distinct

        q0 = Aggregate s1 In q Skip 10 Into Where(True)
        q0 = From s0 in q Skip 10 Aggregate s1 In q Into Where(True)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Select' is not accessible in this context.
        q0 = From s0 in q Aggregate s1 In q Into Where(True)
                          ~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        q0 = From s0 in q Aggregate s1 In q Into Where(True)
                                          ~
BC36594: Definition of method 'Select' is not accessible in this context.
        q0 = From s0 in q Aggregate s1 In q Into Where(True), Distinct
                          ~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        q0 = From s0 in q Aggregate s1 In q Into Where(True), Distinct
                                          ~
BC36594: Definition of method 'Skip' is not accessible in this context.
        q0 = Aggregate s1 In q Skip 10 Into Where(True)
                               ~~~~
BC36594: Definition of method 'Skip' is not accessible in this context.
        q0 = From s0 in q Skip 10 Aggregate s1 In q Into Where(True)
                          ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultQueryIndexer1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class DefaultQueryIndexer1
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Function ElementAtOrDefault(x As Integer) As Guid
        Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
    End Function
End Class

Class DefaultQueryIndexer3
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    ReadOnly Property ElementAtOrDefault(x As String) As String
        Get
            Return x
        End Get
    End Property
End Class

Class DefaultQueryIndexer4
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class

Class DefaultQueryIndexer5
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Shared Function ElementAtOrDefault(x As Integer) As Guid
        Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
    End Function
End Class

Class DefaultQueryIndexer6
    Function AsEnumerable() As DefaultQueryIndexer1
        Return New DefaultQueryIndexer1()
    End Function
End Class

Class DefaultQueryIndexer7
    Function AsQueryable() As DefaultQueryIndexer1
        Return New DefaultQueryIndexer1()
    End Function
End Class

Class DefaultQueryIndexer8
    Function Cast(Of T)() As DefaultQueryIndexer1
        Return New DefaultQueryIndexer1()
    End Function
End Class

Module Module1

    &lt;System.Runtime.CompilerServices.Extension()&gt;
    Function ElementAtOrDefault(this As DefaultQueryIndexer4, x As String) As Integer
        Return x
    End Function

    Function TestDefaultQueryIndexer1() As DefaultQueryIndexer1
        Return New DefaultQueryIndexer1()
    End Function

    Function TestDefaultQueryIndexer5() As DefaultQueryIndexer5
        Return New DefaultQueryIndexer5()
    End Function

    Sub Main()
        Dim xx1 As New DefaultQueryIndexer1()

        System.Console.WriteLine(xx1(1))
        System.Console.WriteLine(TestDefaultQueryIndexer1(2))

        Dim xx3 As New DefaultQueryIndexer3()
        System.Console.WriteLine(xx3!aaa)

        Dim xx4 As New DefaultQueryIndexer4()
        System.Console.WriteLine(xx4(4))

        System.Console.WriteLine((New DefaultQueryIndexer5())(6))
        System.Console.WriteLine(TestDefaultQueryIndexer5(7))

        System.Console.WriteLine((New DefaultQueryIndexer6())(8))
        System.Console.WriteLine((New DefaultQueryIndexer7())(9))
        System.Console.WriteLine((New DefaultQueryIndexer8())(10))
    End Sub

End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                                expectedOutput:=
            <![CDATA[
00000001-0000-0000-0000-000000000000
00000002-0000-0000-0000-000000000000
aaa
4
00000006-0000-0000-0000-000000000000
00000007-0000-0000-0000-000000000000
00000008-0000-0000-0000-000000000000
00000009-0000-0000-0000-000000000000
0000000a-0000-0000-0000-000000000000
]]>)
        End Sub

        <Fact>
        Public Sub DefaultQueryIndexer2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class DefaultQueryIndexer1
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Function ElementAtOrDefault(x As Integer) As Guid
        Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
    End Function
End Class

Class DefaultQueryIndexer2
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Function ElementAtOrDefault(x As String) As Integer
        Return x
    End Function
End Class

Class DefaultQueryIndexer4
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function
End Class

Class DefaultQueryIndexer5
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Shared Function ElementAtOrDefault(x As Integer) As Guid
        Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
    End Function
End Class

Class DefaultQueryIndexer9
    Function Cast(Of T)() As DefaultQueryIndexer4
        Return New DefaultQueryIndexer4()
    End Function
End Class

Class DefaultQueryIndexer10

    WriteOnly Property G As DefaultQueryIndexer1
        Set(value As DefaultQueryIndexer1)

        End Set
    End Property
End Class

Class DefaultQueryIndexer11
    Function [Select](x As Func(Of Integer, Integer)) As Object
        Return Nothing
    End Function

    Public ElementAtOrDefault As Guid
End Class

Module Module1
    Function TestDefaultQueryIndexer4() As DefaultQueryIndexer4
        Return New DefaultQueryIndexer4()
    End Function

    Function TestDefaultQueryIndexer5() As DefaultQueryIndexer5
        Return New DefaultQueryIndexer5()
    End Function

    Sub Main()
        Dim xx1 As New DefaultQueryIndexer1()

        System.Console.WriteLine(xx1(0, 1))
        System.Console.WriteLine(xx1!aaa)
        DefaultQueryIndexer1(3)
        System.Console.WriteLine(DefaultQueryIndexer1(3))

        Dim xx2 As New DefaultQueryIndexer2()
        System.Console.WriteLine(xx2!aaa)

        Dim xx4 As New DefaultQueryIndexer4()
        System.Console.WriteLine(xx4(4))
        System.Console.WriteLine(TestDefaultQueryIndexer4(2))

        System.Console.WriteLine((New DefaultQueryIndexer5())(6))
        System.Console.WriteLine(TestDefaultQueryIndexer5(7))

        System.Console.WriteLine((New DefaultQueryIndexer9())(11))

        System.Console.WriteLine((New DefaultQueryIndexer10()).G(11))

        System.Console.WriteLine((New DefaultQueryIndexer11())(12))
        System.Console.WriteLine((New DefaultQueryIndexer11())!bbb)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Function ElementAtOrDefault(x As Integer) As Guid'.
        System.Console.WriteLine(xx1(0, 1))
                                        ~
BC30367: Class 'DefaultQueryIndexer1' cannot be indexed because it has no default property.
        System.Console.WriteLine(xx1!aaa)
                                 ~~~
BC30109: 'DefaultQueryIndexer1' is a class type and cannot be used as an expression.
        DefaultQueryIndexer1(3)
        ~~~~~~~~~~~~~~~~~~~~
BC30109: 'DefaultQueryIndexer1' is a class type and cannot be used as an expression.
        System.Console.WriteLine(DefaultQueryIndexer1(3))
                                 ~~~~~~~~~~~~~~~~~~~~
BC30367: Class 'DefaultQueryIndexer2' cannot be indexed because it has no default property.
        System.Console.WriteLine(xx2!aaa)
                                 ~~~
BC30367: Class 'DefaultQueryIndexer4' cannot be indexed because it has no default property.
        System.Console.WriteLine(xx4(4))
                                 ~~~
BC32016: 'Public Function TestDefaultQueryIndexer4() As DefaultQueryIndexer4' has no parameters and its return type cannot be indexed.
        System.Console.WriteLine(TestDefaultQueryIndexer4(2))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine((New DefaultQueryIndexer5())(6))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(TestDefaultQueryIndexer5(7))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC30367: Class 'DefaultQueryIndexer9' cannot be indexed because it has no default property.
        System.Console.WriteLine((New DefaultQueryIndexer9())(11))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30524: Property 'G' is 'WriteOnly'.
        System.Console.WriteLine((New DefaultQueryIndexer10()).G(11))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30367: Class 'DefaultQueryIndexer11' cannot be indexed because it has no default property.
        System.Console.WriteLine((New DefaultQueryIndexer11())(12))
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30367: Class 'DefaultQueryIndexer11' cannot be indexed because it has no default property.
        System.Console.WriteLine((New DefaultQueryIndexer11())!bbb)
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ''' <summary>
        ''' Breaking change: Native compiler allows ElementAtOrDefault
        ''' to be a field, while Roslyn requires ElementAtOrDefault
        ''' to be a method or property.
        ''' </summary>
        <WorkItem(576814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576814")>
        <Fact()>
        Public Sub DefaultQueryIndexerField()
            Dim source =
                <compilation>
                    <file name="c.vb"><![CDATA[
Option Strict On
Class C
    Public Function [Select](f As System.Func(Of Object, Object)) As C
        Return Nothing
    End Function
    Public ElementAtOrDefault As Object()
End Class
Module M
    Sub M(o As C)
        Dim value As Object
        value = o(1)
        o(2) = value
    End Sub
End Module
]]>
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.AssertTheseDiagnostics(
<expected>
BC30367: Class 'C' cannot be indexed because it has no default property.
        value = o(1)
                ~
BC30367: Class 'C' cannot be indexed because it has no default property.
        o(2) = value
        ~
</expected>)
        End Sub

        <Fact>
        Public Sub QueryLambdas1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class QueryLambdas
    Shared ReadOnly sharedRO As Integer
    ReadOnly instanceRO As Integer

    Shared fld1 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
    Dim fld2 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
    Dim fld3 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(instanceRO)

    Shared fld4 As Action = Sub()
                                PassByRef(sharedRO) '0
                            End Sub

    Dim fld5 As Action = Sub()
                             PassByRef(sharedRO) '1
                             PassByRef(instanceRO) '2
                         End Sub

    Shared Sub New()
        Dim q As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)

        Dim ggg As Action = Sub()
                                PassByRef(sharedRO) '3
                            End Sub
    End Sub

    Sub New()
        Dim q1 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
        Dim q2 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(instanceRO)

        Dim ggg As Action = Sub()
                                PassByRef(sharedRO) '4
                                PassByRef(instanceRO) '5
                            End Sub
    End Sub

    Sub Test()
        Dim q1 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
        Dim q2 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(instanceRO)

        Dim ggg As Action = Sub()
                                PassByRef(sharedRO) '6
                                PassByRef(instanceRO) '7
                            End Sub
    End Sub

    Shared Function PassByRef(ByRef x As Integer) As Integer
        Return x
    End Function
End Class

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Shared fld1 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
                                                                               ~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Dim fld3 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(instanceRO)
                                                                            ~~~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                PassByRef(sharedRO) '0
                                          ~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                             PassByRef(instanceRO) '2
                                       ~~~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
        Dim q As Object = From x In New Integer() {1, 2, 3} Select PassByRef(sharedRO)
                                                                             ~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                PassByRef(sharedRO) '3
                                          ~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
        Dim q2 As Object = From x In New Integer() {1, 2, 3} Select PassByRef(instanceRO)
                                                                              ~~~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                PassByRef(instanceRO) '5
                                          ~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(528731, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528731")>
        <Fact>
        Public Sub BC36598ERR_CannotLiftRestrictedTypeQuery()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotLiftRestrictedTypeQuery">
        <file name="a.vb">
Imports System
Imports System.Linq

        Module m1
            Sub goo(y As ArgIterator)
                Dim col = New Integer() {1, 2}
                Dim x As New ArgIterator
                Dim q1 = From i In col Where x.GetRemainingCount > 0 Select a = 1
                Dim q2 = From i In col Where y.GetRemainingCount > 0 Select a = 2
            End Sub
        End Module
    </file>
    </compilation>, additionalRefs:={Net40.SystemCore})

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC36598: Instance of restricted type 'ArgIterator' cannot be used in a query expression.
                Dim q1 = From i In col Where x.GetRemainingCount > 0 Select a = 1
                                             ~
BC36598: Instance of restricted type 'ArgIterator' cannot be used in a query expression.
                Dim q2 = From i In col Where y.GetRemainingCount > 0 Select a = 2
                                             ~
</expected>)
        End Sub

        <WorkItem(545801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545801")>
        <Fact>
        Public Sub NoPropertyMethodConflictForQueryOperators()
            Dim verifier = CompileAndVerify(
    <compilation name="NoPropertyMethodConflictForQueryOperators">
        <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim q As Object
        Dim _i003 As I003 = New CI003()
        q = Aggregate a In _i003 Into Count()
    End Sub
End Module

Interface I001
    ReadOnly Property Count() As Integer
End Interface

Interface I002
    Function [Select](ByVal selector As Func(Of Integer, Integer)) As I002
    Function Count() As Integer
End Interface

Interface I003
    Inherits I001, I002
End Interface

Class CI003
    Implements I003

    Public ReadOnly Property Count() As Integer Implements I001.Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Function Count1() As Integer Implements I002.Count
        System.Console.WriteLine("CI003.Count") : Return Nothing
    End Function

    Public Function [Select](ByVal selector As System.Func(Of Integer, Integer)) As I002 Implements I002.Select
        Return Me
    End Function
End Class
    </file>
    </compilation>,
            expectedOutput:="CI003.Count", references:={Net451.SystemCore})

            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub RangeVariableNameInference1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb"><![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class Test
    Sub Test02()
        Dim x As System.Xml.Linq.XElement() = New System.Xml.Linq.XElement() { _
                    <Elem1>
                        <Elem2 Attr2="Elem2Attr2Val1" Attr-3="Elem2Attr3Val1">Elem2Val1</Elem2>
                        <Elem2 Attr2="Elem2Attr2Val2" Attr-3="Elem2Attr3Val2">Elem2Val2</Elem2>
                        <Elem2 Attr2="Elem2Attr2Val3" Attr-3="Elem2Attr3Val3">Elem2Val3</Elem2>
                        <Elem3>
                            <Elem2 Attr2="Elem2Attr2Val4" Attr-3="Elem2Attr3Val4">Elem2Val4</Elem2>
                        </Elem3>
                        <Elem-4>Elem4Val1</Elem-4>
                    </Elem1>}

        Dim o As Object

        o = From a In x Select y = 1, x.<Elem-4>
        o = From a In x Select y = 1, x...<Elem-4>
        o = From a In x Select y = 1, x.<Elem-4>.Value
        o = From a In x Select y = 1, x...<Elem-4>.Value
        o = From a In x Select y = 1, x.<Elem-4>.Value()
        o = From a In x Select y = 1, x...<Elem-4>.Value()
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>.Normalize(0)
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>.Normalize(0)
        o = From a In x Select y = 1, x.<Elem-4>(0)
        o = From a In x Select y = 1, x...<Elem-4>(0)
        o = From a In x Select y = 1, x.<Elem2>(0, 1)
        o = From a In x Select y = 1, x...<Elem2>(0, 1)
        o = From a In x Select y = 1, x.<Elem2>()
        o = From a In x Select y = 1, x...<Elem2>()
    End Sub
End Class

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore,
                                                                                                          SystemXmlRef,
                                                                                                          SystemXmlLinqRef})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x.<Elem-4>
                                         ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x...<Elem-4>
                                           ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x.<Elem-4>.Value
                                         ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x...<Elem-4>.Value
                                           ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x.<Elem-4>.Value()
                                         ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x...<Elem-4>.Value()
                                           ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>
                                                  ~~~~~~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>.Normalize(0)
                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        o = From a In x Select y = 1, x.<Elem2>.@<Attr-3>.Normalize(0)
                                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x.<Elem-4>(0)
                                         ~~~~~~
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
        o = From a In x Select y = 1, x...<Elem-4>(0)
                                           ~~~~~~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        o = From a In x Select y = 1, x.<Elem2>(0, 1)
                                      ~~~~~~~~~~~~~~~
BC36582: Too many arguments to extension method 'Public Function ElementAtOrDefault(index As Integer) As XElement' defined in 'Enumerable'.
        o = From a In x Select y = 1, x.<Elem2>(0, 1)
                                                   ~
BC36599: Range variable name can be inferred only from a simple or qualified name with no arguments.
        o = From a In x Select y = 1, x...<Elem2>(0, 1)
                                      ~~~~~~~~~~~~~~~~~
BC36582: Too many arguments to extension method 'Public Function ElementAtOrDefault(index As Integer) As XElement' defined in 'Enumerable'.
        o = From a In x Select y = 1, x...<Elem2>(0, 1)
                                                     ~
BC36586: Argument not specified for parameter 'index' of extension method 'Public Function ElementAtOrDefault(index As Integer) As XElement' defined in 'Enumerable'.
        o = From a In x Select y = 1, x.<Elem2>()
                                        ~~~~~~~
BC36586: Argument not specified for parameter 'index' of extension method 'Public Function ElementAtOrDefault(index As Integer) As XElement' defined in 'Enumerable'.
        o = From a In x Select y = 1, x...<Elem2>()
                                          ~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ERR_RestrictedType1()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb"><![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class Queryable2
    Delegate Function d(Of T)(x As ArgIterator) As T
    Delegate Function d2(Of T)(x As ArgIterator, x As ArgIterator) As T

    Function [Select](Of T)(x As d(Of T)) As Queryable2
        Return Nothing
    End Function

    Function Where(Of T)(x As Func(Of T, Boolean)) As Queryable2
        Return Nothing
    End Function

    Function SelectMany(Of S)(x As d(Of Queryable2), y As d2(Of S)) As Queryable2
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim xx As Object
        xx = From ii In New Queryable2() Select 1
        xx = From ii In New Queryable2()
        xx = From ii In New Queryable2() Select ii
        xx = From ii In New Queryable2() Select (ii)
        xx = From ii In New Queryable2() Select jj = ii
        xx = From ii In New Queryable2() Where True
        xx = From ii In New Queryable2(), jj In New Queryable2()
        xx = From ii In New Queryable2(), jj In New Queryable2() Select 1
    End Sub
End Module

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2()
                  ~~~~~~~~~~~~~~~~~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        xx = From ii In New Queryable2()
                  ~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2() Select ii
                                         ~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        xx = From ii In New Queryable2() Select ii
                                         ~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2() Select (ii)
                                         ~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        xx = From ii In New Queryable2() Select (ii)
                                         ~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2() Select jj = ii
                                         ~~~~~~
BC36594: Definition of method 'Select' is not accessible in this context.
        xx = From ii In New Queryable2() Select jj = ii
                                         ~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2() Where True
                                         ~~~~~
BC36594: Definition of method 'Where' is not accessible in this context.
        xx = From ii In New Queryable2() Where True
                                         ~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2(), jj In New Queryable2()
                  ~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2(), jj In New Queryable2()
                                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_RestrictedType1_2()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb"><![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collections
Imports System.Linq

Class Queryable2
    Delegate Function d(Of T)(x As ArgIterator) As T
    Delegate Function d11(x As ArgIterator) As Integer
    Delegate Function d12(x As ArgIterator) As ArgIterator
    Delegate Function d2(Of T)(x As ArgIterator, x As ArgIterator) As T

    Function [Select](x As d11) As Queryable2
        Return Nothing
    End Function

    Function [Select](x As d12) As Queryable2
        Return Nothing
    End Function

    Function SelectMany(Of S)(x As d(Of Queryable2), y As d2(Of S)) As Queryable2
        Return Nothing
    End Function
End Class

Module Module1
    Sub Main()
        Dim xx As Object
        xx = From ii In New Queryable2() Select 1
        xx = From ii In New Queryable2()
        xx = From ii In New Queryable2() Select ii
        xx = From ii In New Queryable2() Select (ii)
        xx = From ii In New Queryable2() Select jj = ii
        xx = From ii In New Queryable2(), jj In New Queryable2()
        xx = From ii In New Queryable2(), jj In New Queryable2() Select 1
    End Sub
End Module

    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                                                                                         additionalRefs:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2(), jj In New Queryable2()
                  ~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        xx = From ii In New Queryable2(), jj In New Queryable2()
                                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_RestrictedType1_3()
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Linq
Module M
    Sub M()
        Dim c1 As System.ArgIterator()() = Nothing
        Dim c2 As System.TypedReference()() = Nothing
        Dim z = From x In c1, y In c2
    End Sub
End Module
    </file>
    </compilation>, references:={Net40.SystemCore}).AssertTheseDiagnostics(
    <expected>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim c1 As System.ArgIterator()() = Nothing
                  ~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim c2 As System.TypedReference()() = Nothing
                  ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = From x In c1, y In c2
                     ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = From x In c1, y In c2
                     ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = From x In c1, y In c2
                     ~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = From x In c1, y In c2
                              ~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = From x In c1, y In c2
                              ~
</expected>)
        End Sub

        <WorkItem(542724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542724")>
        <Fact>
        Public Sub QueryExprInAttributes()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        <![CDATA[
<MyAttr(From i In Q1 Select 2)>
Class cls1
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30002: Type 'MyAttr' is not defined.
<MyAttr(From i In Q1 Select 2)>
 ~~~~~~
BC30059: Constant expression is required.
<MyAttr(From i In Q1 Select 2)>
        ~~~~~~~~~~~~~~~~~~~~~
BC30451: 'Q1' is not declared. It may be inaccessible due to its protection level.
<MyAttr(From i In Q1 Select 2)>
                  ~~
]]>
</expected>)

        End Sub

        <Fact>
        Public Sub Bug10127()
            Dim compilationDef =
<compilation name="Bug10127">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Linq
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim q As Object 
        q = Aggregate x In New Integer(){1} Into s = Sum(Nothing)
        System.Console.WriteLine(q)
        System.Console.WriteLine(q.GetType())

        System.Console.WriteLine("-------")

        q = From x In New Integer() {1} Order By Nothing
        System.Console.WriteLine(DirectCast(q, IEnumerable(Of Integer))(0))
    End Sub
End Module

    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef, references:={Net40.SystemCore},
                             expectedOutput:=
            <![CDATA[
0
System.Int32
-------
1
]]>)
        End Sub

        <WorkItem(528969, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528969")>
        <Fact>
        Public Sub InaccessibleElementAtOrDefault()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System

Class Q1
    Public Function [Select](selector As Func(Of Integer, Integer)) As Q1
        Return Nothing
    End Function

    Private Function ElementAtOrDefault(x As String) As Integer
        Return 4
    End Function
End Class

Module Test
    Sub Main()
        Dim qs As New Q1()
        Dim zs = From q In qs Select q

        Dim element = zs(2)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30367: Class 'Q1' cannot be indexed because it has no default property.
        Dim element = zs(2)
                      ~~
</expected>)

        End Sub

        <WorkItem(543120, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543120")>
        <Fact()>
        Public Sub ExplicitTypeNameInExprRangeVarDeclInLetClause()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System
Imports System.Linq

Module Module1
    Sub Main(args As String())
        Dim q1 = From i1 In New Integer() {4, 5} Let i2 As Integer = "Hello".Length
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <WorkItem(543138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543138")>
        <Fact()>
        Public Sub FunctionLambdaInConditionOfJoinClause()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main(args As String())
        Dim arr = New Byte() {4, 5}
        Dim q2 = From num In arr Join n1 In arr On num.ToString() Equals (Function() n1).ToString()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
                                expectedOutput:=
            <![CDATA[
]]>)
        End Sub

        <WorkItem(543171, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543171")>
        <Fact()>
        Public Sub FunctionLambdaInOrderByClause()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System
Imports System.Linq

Module Program
    Sub Main()
        Dim arr = New Integer() {4, 5}
        Dim q2 = From i1 In arr Order By Function() 5
        Dim q3 = arr.OrderBy(Function(i1) Function() 5)
        Dim q4 = From i1 In arr Order By ((Function() 5))
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            compilation.AssertNoDiagnostics()
            CompileAndVerify(compilation,
                                expectedOutput:=
            <![CDATA[
]]>)
        End Sub

        <WorkItem(529014, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529014")>
        <Fact>
        Public Sub MissingByInGroupByQueryOperator()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q1 = From i1 In arr, i2 In arr Group i1, i2  
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36605: 'By' expected.
        Dim q1 = From i1 In arr, i2 In arr Group i1, i2  
                                                         ~
BC36615: 'Into' expected.
        Dim q1 = From i1 In arr, i2 In arr Group i1, i2  
                                                         ~
</expected>)
        End Sub

        <Fact()>
        Public Sub InaccessibleQueryMethodOnCollectionType1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q0 = From s1 In qi Take While False'BIND:"Take While False"
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'QueryAble.Private Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble(Of Integer)' is not accessible in this context because it is 'Private'.
        Dim q0 = From s1 In qi Take While False'BIND:"Take While False"
                               ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InaccessibleQueryMethodOnCollectionType2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, String)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function

    Private Function TakeWhile(x As Func(Of T, Integer)) As QueryAble(Of T)
        System.Console.WriteLine("TakeWhile {0}", x)
        Return New QueryAble(Of T)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q0 = From s1 In qi Take While False'BIND:"Take While False"
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'TakeWhile' is not accessible in this context.
        Dim q0 = From s1 In qi Take While False'BIND:"Take While False"
                               ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GroupBy6()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v+1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q As Object = From s In qi Group By key = Nothing Into [Select](s)
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'QueryAble.Private Function GroupBy(Of K, R)(key As Func(Of Integer, K), into As Func(Of K, QueryAble(Of Integer), R)) As QueryAble(Of R)' is not accessible in this context because it is 'Private'.
        Dim q As Object = From s In qi Group By key = Nothing Into [Select](s)
                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GroupBy7()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of Integer), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v+1)
    End Function

    Private Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of String), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v+1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q As Object = From s In qi Group By key = Nothing Into [Select](s)
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'GroupBy' is not accessible in this context.
        Dim q As Object = From s In qi Group By key = Nothing Into [Select](s)
                                       ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GroupJoin6()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q0 As Object = From s1 In qi Group Join t1 In qi On s1 Equals t1 Into [Select](t1)
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'QueryAble.Private Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of Integer, K), innerKey As Func(Of I, K), x As Func(Of Integer, QueryAble(Of I), R)) As QueryAble(Of R)' is not accessible in this context because it is 'Private'.
        Dim q0 As Object = From s1 In qi Group Join t1 In qi On s1 Equals t1 Into [Select](t1)
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub GroupJoin7()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class QueryAble(Of T)
    Public ReadOnly v As Integer

    Sub New(v As Integer)
        Me.v = v
    End Sub

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)(v + 1)
    End Function

    Private Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of Integer), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v + 1)
    End Function

    Private Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of String), R)) As QueryAble(Of R)
        Return New QueryAble(Of R)(v + 1)
    End Function
End Class

Module Module1

    Sub Main()
        Dim qi As New QueryAble(Of Integer)(0)
        Dim q0 As Object = From s1 In qi Group Join t1 In qi On s1 Equals t1 Into [Select](t1)
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'GroupJoin' is not accessible in this context.
        Dim q0 As Object = From s1 In qi Group Join t1 In qi On s1 Equals t1 Into [Select](t1)
                                         ~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(543523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543523")>
        <Fact()>
        Public Sub IncompleteLambdaInsideOrderByClause()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System
Imports System.Linq
Module Program
    Sub Main()
        Dim arr = New Integer() {4, 5}
        Dim q2 = From i1 In arr Order By Function() 
                                             r
                                         End Function
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC36594: Definition of method 'OrderBy' is not accessible in this context.
        Dim q2 = From i1 In arr Order By Function() 
                                ~~~~~~~~
BC36610: Name 'r' is either not declared or not in the current scope.
                                             r
                                             ~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                         End Function
                                         ~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact(), WorkItem(544312, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544312")>
        Public Sub WideningConversionInOverloadResolution()
            Dim compilationDef =
<compilation name="WideningConversionInOverloadResolution">
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Module Program

    Public Order As String = ""

    Class scen1(Of T)
        Public Function [Select](ByVal sel As Func(Of T, Integer)) As scen1(Of Integer)
            Order &= "Sel1"
            sel(Nothing)
            Return New scen1(Of Integer)
        End Function

        Public Function [Select](ByVal sel As Func(Of T, Long)) As scen1(Of Long)
            Order &= "Sel2"
            sel(Nothing)
            Return New scen1(Of Long)
        End Function
        Public Function [Select](ByVal sel As Func(Of T, Short)) As scen1(Of Short)
            Order &= "Sel3"
            sel(Nothing)
            Return New scen1(Of Short)
        End Function
        Public Function GroupJoin(Of L, K2, R)(ByVal inner As scen1(Of L), ByVal key1 As Func(Of T, K2), ByVal key2 As Func(Of L, Object), ByVal res As Func(Of T, scen1(Of L), R)) As scen1(Of R)
            Order &= "Join1"
            key1(Nothing)
            Return New scen1(Of R)
        End Function
    End Class

    Sub Main()
        ' "Need a widening conversion for result")
        Order = ""
        Dim c1 = New scen1(Of Integer)
        Dim q1 = From i In c1 Group Join j In c1 On i Equals j Into G = Group
        Console.WriteLine(order)
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation,
                             expectedOutput:=
            <![CDATA[
Join1
]]>)

        End Sub

        <Fact, WorkItem(530910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530910")>
        Public Sub IQueryableOverStringMax()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq

Module Regress123995
    Sub Call0()
        Dim ints = New System.Collections.Generic.List(Of Integer)
        ints.Add(1)
        Dim source As IQueryable(Of Integer)
        source = ints.AsQueryable
        Dim strings = New System.Collections.Generic.List(Of String)
        strings.Add("1")
        strings.Add("2")
        'Query Use of Max
        'Generically Inferred
        Dim query = _
        From x In source _
        Select strings.Max(Function(s) s)
    End Sub
End Module
    </file>
</compilation>

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseDll).AssertNoDiagnostics()

        End Sub

        <Fact, WorkItem(1042011, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042011")>
        Public Sub LambdaWithClosureInQueryExpressionAndPDB()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System.Linq

Module Module1

    Sub Main()
        Dim x = From y In {1} Select Function() y
    End Sub

End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, options:=TestOptions.DebugExe)
        End Sub

        <Fact, WorkItem(1099, "https://github.com/dotnet/roslyn/issues/1099")>
        Public Sub LambdaWithErrorCrash()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Imports System.Linq

Class C
    Shared Function Id(Of T)(a As T, i As Integer) As T
        Return a
    End Function

    Sub F2()
        Dim result = From a In Id({1}, 1), b In Id({1, 2}, 2)
                     From c In Id({1, 2, 3}, 3)
                     Let d = Id(1, 4), e = Id(2, 5)
                     Distinct
                         Take Whi
                     Aggregate f In Id({1}, 6), g In Id({2}, 7)
                         From j In Id({1}, 9)
                         Let h = Id(1, 4), i = Id(2, 5)
                         Where Id(g &lt; 2, 8)
                     Into Count(), Distinct()

    End Sub
    End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, references:={Net40.SystemCore}, options:=TestOptions.ReleaseDll)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'Whi' is not declared. It may be inaccessible due to its protection level.
                         Take Whi
                              ~~~
</expected>)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub IOperationForQueryClause()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q'BIND:"From s In q"
                           Where s > 0 

    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... Where s > 0')
  Expression: 
    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where s > 0')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                          Left: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub IOperationForCollectionRangeVariable()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Where s > 0 Where 10 > s 'BIND:"From s In q Where s > 0 Where 10 > s"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q ... here 10 > s')
  Expression: 
    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where 10 > s')
      Instance Receiver: 
        IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where s > 0')
          Instance Receiver: 
            ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
                  Target: 
                    IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                          ReturnedValue: 
                            IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                              Left: 
                                IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '10 > s')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: '10 > s')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '10 > s')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '10 > s')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '10 > s')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: '10 > s')
                          Left: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                          Right: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact, WorkItem(17838, "https://github.com/dotnet/roslyn/issues/17838")>
        Public Sub IOperationForRangeVariableReference()
            Dim source = <![CDATA[
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function

    Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
        System.Console.WriteLine("Where")
        Return Me
    End Function

End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q2 As Object = From s In q Where s > 0 'BIND:"From s In q Where s > 0"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: QueryAble) (Syntax: 'From s In q Where s > 0')
  Expression: 
    IInvocationOperation ( Function QueryAble.Where(x As System.Func(Of System.Int32, System.Boolean)) As QueryAble) (OperationKind.Invocation, Type: QueryAble, IsImplicit) (Syntax: 'Where s > 0')
      Instance Receiver: 
        ILocalReferenceOperation: q (OperationKind.LocalReference, Type: QueryAble) (Syntax: 'q')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 's > 0')
            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Boolean), IsImplicit) (Syntax: 's > 0')
              Target: 
                IAnonymousFunctionOperation (Symbol: Function (s As System.Int32) As System.Boolean) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 's > 0')
                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 's > 0')
                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 's > 0')
                      ReturnedValue: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's > 0')
                          Left: 
                            IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 's')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23223, "https://github.com/dotnet/roslyn/issues/23223")>
        Public Sub DuplicateRangeVariableName_IOperation_01()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Imports System.Linq

Module Module1
    Sub Main()
        Dim q As Object = From implicit In New Integer() {1, 2, 3} Let implicit = "1" Select implicit 'BIND:"From implicit In New Integer() {1, 2, 3} Let implicit = "1" Select implicit"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsInvalid) (Syntax: 'From implic ... ct implicit')
	  Expression:
	    IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>).Select(Of System.Int32)(selector As System.Func(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>, System.Int32)) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Select implicit')
	      Instance Receiver:
	        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>)(selector As System.Func(Of System.Int32, <anonymous type: Key implicit As System.Int32, Key $156 As System.String>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>), IsInvalid, IsImplicit) (Syntax: 'implicit = "1"')
	          Instance Receiver:
	            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'implicit In ... ) {1, 2, 3}')
	              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
	              Operand:
	                IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32()) (Syntax: 'New Integer() {1, 2, 3}')
	                  Dimension Sizes(1):
	                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsImplicit) (Syntax: 'New Integer() {1, 2, 3}')
	                  Initializer:
	                    IArrayInitializerOperation (3 elements) (OperationKind.ArrayInitializer, Type: null) (Syntax: '{1, 2, 3}')
	                      Element Values(3):
	                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
	                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
	                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
	          Arguments(1):
	              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '"1"')
	                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key implicit As System.Int32, Key $156 As System.String>), IsImplicit) (Syntax: '"1"')
	                  Target:
	                    IAnonymousFunctionOperation (Symbol: Function (implicit As System.Int32) As <anonymous type: Key implicit As System.Int32, Key $156 As System.String>) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '"1"')
	                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '"1"')
	                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '"1"')
	                          ReturnedValue:
	                            IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key implicit As System.Int32, Key $156 As System.String>, IsInvalid, IsImplicit) (Syntax: 'implicit = "1"')
	                              Initializers(2):
	                                  IParameterReferenceOperation: implicit (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'implicit')
	                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "1") (Syntax: '"1"')
	                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
	                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
	      Arguments(1):
	          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'implicit')
	            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of <anonymous type: Key implicit As System.Int32, Key $156 As System.String>, System.Int32), IsImplicit) (Syntax: 'implicit')
	              Target:
	                IAnonymousFunctionOperation (Symbol: Function ($VB$It As <anonymous type: Key implicit As System.Int32, Key $156 As System.String>) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'implicit')
	                  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'implicit')
	                    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'implicit')
	                      ReturnedValue:
	                        IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key implicit As System.Int32, Key $156 As System.String>.implicit As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'implicit')
	                          Instance Receiver:
	                            IParameterReferenceOperation: $VB$It (OperationKind.ParameterReference, Type: <anonymous type: Key implicit As System.Int32, Key $156 As System.String>, IsImplicit) (Syntax: 'implicit')
	            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
	            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30978: Range variable 'implicit' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim q As Object = From implicit In New Integer() {1, 2, 3} Let implicit = "1" Select implicit 'BIND:"From implicit In New Integer() {1, 2, 3} Let implicit = "1" Select implicit"
                                                                       ~~~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23223, "https://github.com/dotnet/roslyn/issues/23223")>
        Public Sub DuplicateRangeVariableName_IOperation_02()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Imports System.Linq

Module Module1
    Sub Main()
        Dim a = New Integer() {1, 2, 3}
        Dim q As Object = From x In a Join x In a On x Equals 1 'BIND:"From x In a Join x In a On x Equals 1"
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: ?, IsInvalid) (Syntax: 'From x In a ...  x Equals 1')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
      Children(5):
          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
            Children(1):
                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'a')
          ILocalReferenceOperation: a (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'a')
          IAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'x')
                    Children(1):
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          IAnonymousFunctionOperation (Symbol: Function ($168 As System.Int32) As ?) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: '1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: '1')
              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '1')
                ReturnedValue: 
                  IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: '1')
                    Children(1):
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          IAnonymousFunctionOperation (Symbol: Function (x As System.Int32, $168 As System.Int32) As <anonymous type: Key x As System.Int32, Key $168 As System.Int32>) (OperationKind.AnonymousFunction, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
              IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
                ReturnedValue: 
                  IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key $168 As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'Join x In a ...  x Equals 1')
                    Initializers(2):
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                        IParameterReferenceOperation: $168 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'x')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36600: Range variable 'x' is already declared.
        Dim q As Object = From x In a Join x In a On x Equals 1 'BIND:"From x In a Join x In a On x Equals 1"
                                           ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of QueryExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class

End Namespace
