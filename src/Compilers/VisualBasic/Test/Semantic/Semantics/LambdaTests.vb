' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class LambdaTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
    Dim y As Object = Function(ParamArray x as Integer()) x
    Dim y1 As Object = Function(Optional x As Integer = 0) x
    Dim y2 As Object = Function(x As Integer) As Integer x
    Dim y3 As Object = Function(x As Integer) As Integer 
                            return x
                       End Function

    Dim y4 As Object = Function(x As Integer)
                            [Function] = Nothing
                            return x
                       End Function

    Dim y5 As Object = Sub(x As Integer) As Integer 
                            return x
                       End Sub

    Dim y6 As Object = Sub(x As Integer)
                            return x
                       End Sub

    Dim y8 As Object = Sub(x As Integer) System.Console.WriteLine(x)

    Dim y7 As Object = Sub(x As Integer) As Integer x
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30026: 'End Sub' expected.
  Sub Main()
  ~~~~~~~~~~
BC33009: 'Lambda' parameters cannot be declared 'ParamArray'.
    Dim y As Object = Function(ParamArray x as Integer()) x
                               ~~~~~~~~~~
BC33010: 'Lambda' parameters cannot be declared 'Optional'.
    Dim y1 As Object = Function(Optional x As Integer = 0) x
                                ~~~~~~~~
BC36674: Multiline lambda expression is missing 'End Function'.
    Dim y2 As Object = Function(x As Integer) As Integer x
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30205: End of statement expected.
    Dim y2 As Object = Function(x As Integer) As Integer x
                                                         ~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y3 As Object = Function(x As Integer) As Integer 
                                ~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y4 As Object = Function(x As Integer)
                                ~
BC30451: 'Function' is not declared. It may be inaccessible due to its protection level.
                            [Function] = Nothing
                            ~~~~~~~~~~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y5 As Object = Sub(x As Integer) As Integer 
                           ~
BC30205: End of statement expected.
    Dim y5 As Object = Sub(x As Integer) As Integer 
                                         ~~~~~~~~~~
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                            return x
                            ~~~~~~~~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y6 As Object = Sub(x As Integer)
                           ~
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                            return x
                            ~~~~~~~~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y8 As Object = Sub(x As Integer) System.Console.WriteLine(x)
                           ~
BC36641: Lambda parameter 'x' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y7 As Object = Sub(x As Integer) As Integer x
                           ~
BC30205: End of statement expected.
    Dim y7 As Object = Sub(x As Integer) As Integer x
                                         ~~~~~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
  End Sub
         ~
]]>
</expected>)
        End Sub

        <Fact>
        Public Sub Test2()

            Dim compilationDef =
<compilation name="LambdaTests2">
    <file name="a.vb">
Module Program
  Sub Main()
    Dim l1 As System.Func(Of Integer, Integer) = Function(x) x
    Dim l2 As System.Func(Of Integer, Integer) = Function(x) 
                                                    Return x
                                                 End Function  

    Dim l3 As System.Action(Of Integer) = Sub(x) System.Console.WriteLine(x)
    TakeAction(l3)

    Dim l4 As System.Action(Of Integer) = Sub(x) 
                                             System.Console.WriteLine(x)
                                             Goto LB1
                                             LB1:
                                          End Sub

    Dim l5 As System.Action(Of Integer) = DirectCast(Sub(x) 
                                                         System.Console.WriteLine(x)
                                                         Exit Sub
                                                     End Sub, System.Action(Of Integer))

    Dim l6 As System.Action(Of Integer) = TryCast(Sub(x) 
                                                     System.Console.WriteLine(x)
                                                     Exit Sub
                                                  End Sub, System.Action(Of Integer))

    Dim y As Integer = 1 

    TakeAction(Sub(x) 
                    System.Console.WriteLine(x)
                    y = y + x

                    TakeAction(Sub(z) 
                        System.Console.WriteLine(z)
                        y = y + x
                        Exit Sub
                    End Sub)

                    Exit Sub
               End Sub)

    System.Console.WriteLine(y)
  End Sub

  Sub TakeAction(x as System.Action(Of Integer))
    x.Invoke(1)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, <![CDATA[
1
1
1
3
]]>)
        End Sub

        <Fact>
        Public Sub Test3()

            Dim compilationDef =
<compilation name="LambdaTests3">
    <file name="a.vb">
Module Program
  Sub Main()
    TakeAction(Sub(x) 
                    System.Console.WriteLine(x)
                    Exit Function ' 1
               End Sub)

    TakeAction(Sub(x) Exit Function) ' 2



    TakeFunction(Function(x) 
                    System.Console.WriteLine(x)
                    Exit Sub ' 3
                    return 34
                 End Function)

    TakeAction(Sub(x) 
                    System.Console.WriteLine(x)
                    LB2: Goto LB1 ' 4

               End Sub)

LB1: Goto LB2
  End Sub

  Sub TakeAction(x as System.Action(Of Integer))
  End Sub

  Sub TakeFunction(x as System.Func(Of Integer, Integer))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30067: 'Exit Function' is not valid in a Sub or Property.
                    Exit Function ' 1
                    ~~~~~~~~~~~~~
BC30067: 'Exit Function' is not valid in a Sub or Property.
    TakeAction(Sub(x) Exit Function) ' 2
                      ~~~~~~~~~~~~~
BC30065: 'Exit Sub' is not valid in a Function or Property.
                    Exit Sub ' 3
                    ~~~~~~~~
BC30132: Label 'LB1' is not defined.
                    LB2: Goto LB1 ' 4
                              ~~~
BC30132: Label 'LB2' is not defined.
LB1: Goto LB2
          ~~~
</expected>)

        End Sub

        <Fact()>
        Public Sub Test4()

            Dim compilationDef =
<compilation name="LambdaTests4">
    <file name="a.vb">
Module Program
  Sub Main()
    Dim y1 As System.Func(Of Integer) = Function() As &lt;Out&gt; Integer
                                                        Exit Function
                                                    End Function ' 0

    Dim y2 As System.Func(Of Integer, Integer) = Function(&lt;[In]&gt; a) As Integer
                                                        Exit Function
                                                    End Function ' 8

    Dim y3 As System.Func(Of Integer(), Integer) = Function(x() As Integer)
                                                        Exit Function
                                                    End Function ' 1

    Dim y4 As System.Func(Of Integer()(), Integer) = Function(x() As Integer())
                                                            Exit Function
                                                        End Function ' 2

    Dim y5 As System.Func(Of Integer(), Integer) = Function(x())
                                                        Exit Function
                                                    End Function '3

    Dim y6 As System.Func(Of Integer, Integer) = Function(x?)
                                                        Exit Function
                                                    End Function ' 4

    Dim y7 As System.Func(Of Integer, Integer) = Function(x)
                                                        Dim x As Object = Nothing ' 1
                                                        Exit Function
                                                    End Function ' 5

    Dim local1 As Object = Nothing

    Dim y8 As System.Func(Of Integer, Integer) = Function(x)
                                                        Dim local1 As Integer = 0
                                                    End Function ' 6

    Dim y9 As System.Func(Of Integer, Integer) = Function(x)
                                                        Static local2 As Integer = 0
                                                    End Function ' 7

    Dim y10 As System.Action(Of Integer) = Sub(local1)
                                           End Sub

  End Sub

    Sub Main2(Of T)(param As Integer)
        Dim y11 As System.Action(Of Integer) = Sub(T)
                                               End Sub

        Dim y12 As System.Action(Of Integer) = Sub(param As Integer)
                                               End Sub

        Dim y13 As System.Func(Of Integer, System.Action(Of Integer)) = Function(param2)
                                                                            Return Sub(param2)
                                                                                   End Sub
                                                                        End Function
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36677: Attributes cannot be applied to return types of lambda expressions.
    Dim y1 As System.Func(Of Integer) = Function() As <Out> Integer
                                                      ~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 0
                                                    ~~~~~~~~~~~~
BC36634: Attributes cannot be applied to parameters of lambda expressions.
    Dim y2 As System.Func(Of Integer, Integer) = Function(<[In]> a) As Integer
                                                          ~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 8
                                                    ~~~~~~~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 1
                                                    ~~~~~~~~~~~~
BC31087: Array modifiers cannot be specified on both a variable and its type.
    Dim y4 As System.Func(Of Integer()(), Integer) = Function(x() As Integer())
                                                                     ~~~~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                        End Function ' 2
                                                        ~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer(), Integer)'.
    Dim y5 As System.Func(Of Integer(), Integer) = Function(x())
                                                   ~~~~~~~~~~~~~~
BC36643: Array modifiers cannot be specified on lambda expression parameter name. They must be specified on its type.
    Dim y5 As System.Func(Of Integer(), Integer) = Function(x())
                                                            ~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function '3
                                                    ~~~~~~~~~~~~
BC36632: Nullable parameters must specify a type.
    Dim y6 As System.Func(Of Integer, Integer) = Function(x?)
                                                          ~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 4
                                                    ~~~~~~~~~~~~
BC36667: Variable 'x' is already declared as a parameter of this or an enclosing lambda expression.
                                                        Dim x As Object = Nothing ' 1
                                                            ~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 5
                                                    ~~~~~~~~~~~~
BC30616: Variable 'local1' hides a variable in an enclosing block.
                                                        Dim local1 As Integer = 0
                                                            ~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 6
                                                    ~~~~~~~~~~~~
BC36672: Static local variables cannot be declared inside lambda expressions.
                                                        Static local2 As Integer = 0
                                                        ~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                                                    End Function ' 7
                                                    ~~~~~~~~~~~~
BC36641: Lambda parameter 'local1' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
    Dim y10 As System.Action(Of Integer) = Sub(local1)
                                               ~~~~~~
BC32089: 'T' is already declared as a type parameter of this method.
        Dim y11 As System.Action(Of Integer) = Sub(T)
                                                   ~
BC36641: Lambda parameter 'param' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim y12 As System.Action(Of Integer) = Sub(param As Integer)
                                                   ~~~~~
BC36641: Lambda parameter 'param2' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
                                                                            Return Sub(param2)
                                                                                       ~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub Test5()

            Dim compilationDef =
<compilation name="LambdaTests5">
    <file name="a.vb">

Class TestBase
    Public ReadOnly baseInstance As System.Guid
    Public Shared ReadOnly baseShared As System.Guid
End Class

Structure TestStruct
    Public instance As System.Guid
    Public Shared [shared] As System.Guid
    Public ReadOnly instanceRO As System.Guid
    Public Shared ReadOnly sharedRO As System.Guid
End Structure

Class Test1
    Inherits TestBase

    Public ReadOnly x As System.Guid
    Public ReadOnly x1 As TestStruct

    Sub New()
        x = New System.Guid()
        x1.instance = New System.Guid()
        x1.shared = New System.Guid() ' 21
        x1.instanceRO = New System.Guid() ' 22
        x1.sharedRO = New System.Guid() '23

        Dim z As Test1 = New Test1()
        z.x = New System.Guid() ' 24
        z.x1.instance = New System.Guid() ' 25
        z.x1.shared = New System.Guid() '26
        z.x1.instanceRO = New System.Guid() '27
        z.x1.sharedRO = New System.Guid() '28

        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  x = New System.Guid() ' 1
                                                  PassByRef(x) ' 2
                                                  PassByRef(z.x)
                                                  PassByRef(baseInstance)
                                                  PassByRef(baseShared)

                                                  x1.instance = New System.Guid() '31
                                                  x1.shared = New System.Guid() '32
                                                  PassByRef(x1.instance) '33
                                                  PassByRef(x1.shared) '34
                                                  PassByRef(x1.instanceRO)
                                                  PassByRef(x1.sharedRO) '35

                                                  z.x1.shared = New System.Guid() '36
                                                  PassByRef(z.x1.instance)
                                                  PassByRef(z.x1.shared) '37
                                                  PassByRef(z.x1.instanceRO)
                                                  PassByRef(z.x1.sharedRO) '38
                                              End Sub

    End Sub

    Sub NotAConstructor()
        PassByRef(x)

        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  x = New System.Guid() ' 5
                                                  PassByRef(x)
                                                  Dim z1 As Test1 = New Test1()
                                                  PassByRef(z1.x)
                                                  PassByRef(baseInstance)
                                                  PassByRef(baseShared)

                                                  PassByRef(x1.instance)
                                                  PassByRef(x1.shared) '42
                                                  PassByRef(x1.instanceRO)
                                                  PassByRef(x1.sharedRO) '43

                                                  PassByRef(z1.x1.instance)
                                                  PassByRef(z1.x1.shared) '44
                                                  PassByRef(z1.x1.instanceRO)
                                                  PassByRef(z1.x1.sharedRO) '45
                                              End Sub
    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub
End Class

Class Test2

    ReadOnly x As System.Guid

    Sub New()
        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  PassByRef(x)
                                              End Sub

    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub

    Shared Sub PassByRef(v As System.Object)
    End Sub
End Class

Class Test3
    Inherits TestBase

    Shared ReadOnly x As System.Guid

    Shared Sub New()
        x = New System.Guid()

        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  x = New System.Guid() ' 3
                                                  PassByRef(x) ' 4
                                                  Dim z As Test1 = New Test1()
                                                  PassByRef(z.x)
                                                  PassByRef(baseInstance)
                                                  PassByRef(baseShared)
                                              End Sub

    End Sub

    Sub NotAConstructor()
        PassByRef(x)

        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  x = New System.Guid() ' 6
                                                  PassByRef(x)
                                                  Dim z As Test1 = New Test1()
                                                  PassByRef(z.x)
                                                  PassByRef(baseInstance)
                                                  PassByRef(baseShared)
                                              End Sub
    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub
End Class

Class Test4

    Shared ReadOnly x As System.Guid

    Sub New()
        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  x = New System.Guid() ' 7
                                                  PassByRef(x)
                                              End Sub

    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub
End Class

Class Test5

    ReadOnly x As System.Guid

    Shared Sub New()
        Dim t As New Test5()
        Dim y1 As System.Action(Of Integer) = Sub(v)
                                                  t.x = New System.Guid() ' 8
                                                  PassByRef(t.x)
                                              End Sub

    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub
End Class

Class Test6

    ReadOnly x As System.Guid

    Sub New()
        Dim y1 As System.Func(Of System.Action(Of Integer)) = Function()
                                                                  Return Sub(v)
                                                                             x = New System.Guid() ' 9
                                                                             PassByRef(x) ' 10
                                                                         End Sub
                                                              End Function

    End Sub

    Shared Sub PassByRef(ByRef v As System.Guid)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x1.shared = New System.Guid() ' 21
        ~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        x1.instanceRO = New System.Guid() ' 22
        ~~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        x1.sharedRO = New System.Guid() '23
        ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x1.sharedRO = New System.Guid() '23
        ~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        z.x = New System.Guid() ' 24
        ~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        z.x1.instance = New System.Guid() ' 25
        ~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        z.x1.shared = New System.Guid() '26
        ~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        z.x1.instanceRO = New System.Guid() '27
        ~~~~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
        z.x1.sharedRO = New System.Guid() '28
        ~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        z.x1.sharedRO = New System.Guid() '28
        ~~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x = New System.Guid() ' 1
                                                  ~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                                  PassByRef(x) ' 2
                                                            ~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x1.instance = New System.Guid() '31
                                                  ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  x1.shared = New System.Guid() '32
                                                  ~~~~~~~~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                                  PassByRef(x1.instance) '33
                                                            ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(x1.shared) '34
                                                            ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(x1.sharedRO) '35
                                                            ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  z.x1.shared = New System.Guid() '36
                                                  ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(z.x1.shared) '37
                                                            ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(z.x1.sharedRO) '38
                                                            ~~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x = New System.Guid() ' 5
                                                  ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(x1.shared) '42
                                                            ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(x1.sharedRO) '43
                                                            ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(z1.x1.shared) '44
                                                            ~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                                                  PassByRef(z1.x1.sharedRO) '45
                                                            ~~~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x = New System.Guid() ' 3
                                                  ~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                                  PassByRef(x) ' 4
                                                            ~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                                                  PassByRef(baseInstance)
                                                            ~~~~~~~~~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x = New System.Guid() ' 6
                                                  ~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  x = New System.Guid() ' 7
                                                  ~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                  t.x = New System.Guid() ' 8
                                                  ~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                                                                             x = New System.Guid() ' 9
                                                                             ~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                                                                             PassByRef(x) ' 10
                                                                                       ~
</expected>)
        End Sub

        <Fact>
        Public Sub Test6()

            Dim compilationDef =
<compilation name="LambdaTests6">
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim y1 As System.Func(Of Integer, Integer) = Function(x) As Integer
                                                         Return 1
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36674: Multiline lambda expression is missing 'End Function'.
        Dim y1 As System.Func(Of Integer, Integer) = Function(x) As Integer
                                                     ~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test7()

            Dim compilationDef =
<compilation name="LambdaTests7">
    <file name="a.vb">
Module Module1

    Function Main() As Integer
        Dim y1 As System.Action(Of Integer) = Sub(x)
                                                Return
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36673: Multiline lambda expression is missing 'End Sub'.
        Dim y1 As System.Action(Of Integer) = Sub(x)
                                              ~~~~~~
BC42353: Function 'Main' doesn't return a value on all code paths. Are you missing a 'Return' statement?
    End Function
    ~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test8()

            Dim compilationDef =
<compilation name="LambdaTests8">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function() Value()) '2
        M1(Function() '4
               Return Value()
           End Function)
    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Byte))
        System.Console.WriteLine(x.GetType())
    End Sub

    Function Value() As System.ValueType
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Byte]
System.Func`1[System.Byte]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'M1' can be called with these arguments:
    'Public Sub M1(x As Func(Of Short))': Option Strict On disallows implicit conversions from 'ValueType' to 'Short'.
    'Public Sub M1(x As Func(Of Byte))': Option Strict On disallows implicit conversions from 'ValueType' to 'Byte'.
        M1(Function() Value()) '2
        ~~
BC30518: Overload resolution failed because no accessible 'M1' can be called with these arguments:
    'Public Sub M1(x As Func(Of Short))': Option Strict On disallows implicit conversions from 'ValueType' to 'Short'.
    'Public Sub M1(x As Func(Of Byte))': Option Strict On disallows implicit conversions from 'ValueType' to 'Byte'.
        M1(Function() '4
        ~~
</expected>)

            compilationDef =
<compilation name="LambdaTests8">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function() 1) '1
        M1(Function() '3
               Return 1
           End Function)
    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Byte))
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Byte]
System.Func`1[System.Byte]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Byte]
System.Func`1[System.Byte]
]]>)
        End Sub

        <Fact>
        Public Sub Test9()

            Dim compilationDef =
<compilation name="LambdaTests9">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function() 0) '1
        M1(Function() Value()) '2

        M1(Function()
               Return 0
           End Function)

        M1(Function()
               Return Value()
           End Function)
    End Sub

    Sub M1(x As System.Func(Of Integer))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of System.ValueType))
        System.Console.WriteLine(x.GetType())
    End Sub

    Function Value() As System.ValueType
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int32]
System.Func`1[System.ValueType]
System.Func`1[System.Int32]
System.Func`1[System.ValueType]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int32]
System.Func`1[System.ValueType]
System.Func`1[System.Int32]
System.Func`1[System.ValueType]
]]>)

        End Sub

        <Fact>
        Public Sub Test10()

            Dim compilationDef =
<compilation name="LambdaTests10">
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim a As Integer = 1

        M1(Function() a)
        M1(Function()
               Return a
           End Function)

    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Byte))
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Byte]
System.Func`1[System.Byte]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'M1' can be called with these arguments:
    'Public Sub M1(x As Func(Of Short))': Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
    'Public Sub M1(x As Func(Of Byte))': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
        M1(Function() a)
        ~~
BC30518: Overload resolution failed because no accessible 'M1' can be called with these arguments:
    'Public Sub M1(x As Func(Of Short))': Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
    'Public Sub M1(x As Func(Of Byte))': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
        M1(Function()
        ~~
</expected>)

        End Sub

        <Fact>
        Public Sub Test11()

            Dim compilationDef =
<compilation name="LambdaTests11">
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim a As Integer = 1

        M1(Function() )
        M1(Function()
           End Function)

        M2(Sub() )
        M2(Sub()
           End Sub)

    End Sub

    Sub M1(x As System.Func(Of Short))
    End Sub

    Sub M2(x As System.Action)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC30201: Expression expected.
        M1(Function() )
                      ~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
           End Function)
           ~~~~~~~~~~~~
BC36918: Single-line statement lambdas must include exactly one statement.
        M2(Sub() )
           ~~~~~~
]]>
</expected>)

        End Sub

        <Fact>
        Public Sub Test12()

            Dim compilationDef =
<compilation name="LambdaTests12">
    <file name="a.vb">
Module Module1

    Sub Main()

        M2(1.5)
    End Sub

    Sub M2(x As Short)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(x As Single)
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'M2' can be called without a narrowing conversion:
    'Public Sub M2(x As Short)': Argument matching parameter 'x' narrows from 'Double' to 'Short'.
    'Public Sub M2(x As Single)': Argument matching parameter 'x' narrows from 'Double' to 'Single'.
        M2(1.5)
        ~~
</expected>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Single    
]]>)

            compilationDef =
<compilation name="LambdaTests12">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function()
               Return 1.5
           End Function)
    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Single))
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int16]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Single]
]]>)

        End Sub

        <Fact>
        Public Sub Test13()

            Dim compilationDef =
<compilation name="LambdaTests13">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function()
               Return 1
           End Function)

        M2(1)
    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Integer))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(x As Short)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(x As Integer)
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int32]
System.Int32
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int32]
System.Int32
]]>)
        End Sub

        <Fact>
        Public Sub Test14()

            Dim compilationDef =
<compilation name="LambdaTests14">
    <file name="a.vb">
Module Module1

    Sub Main()

        M1(Function()
               Return 1
           End Function)

        M2(1)
    End Sub

    Sub M1(x As System.Func(Of Short))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M1(x As System.Func(Of Long))
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(x As Short)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(x As Long)
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int64]
System.Int64
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int64]
System.Int64
]]>)
        End Sub

        <Fact>
        Public Sub Test15()

            Dim compilationDef =
<compilation name="LambdaTests15">
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim a As Integer = 1

        M1(Function()
               Return a
           End Function,
           Function(x)
               If x Then
                   Return 1
               Else
                   Return Value()
               End If
           End Function)

    End Sub

    Sub M1(x As System.Func(Of Integer), y As System.Func(Of Boolean, Long))
        System.Console.WriteLine(y.GetType())
    End Sub

    Sub M1(x As System.Func(Of Integer), y As System.Func(Of Boolean, Integer))
        System.Console.WriteLine(y.GetType())
    End Sub

    Function Value() As Long
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Boolean,System.Int64]
]]>)

            compilation = compilation.WithOptions(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Boolean,System.Int64]
]]>)
        End Sub

        <Fact()>
        Public Sub Test16()

            Dim compilationDef =
<compilation name="LambdaTests15">
    <file name="a.vb">
Module Module1

    Sub Main()

        Dim z As String

        Dim a As System.Func(Of Boolean, String) = Function(x As Boolean)
                                                       Dim y As String
                                                       If x Then
                                                           Return ""
                                                       ElseIf Not x Then
                                                           Return y
                                                       ElseIf x Then
                                                           Return z

                                                           y = ""
                                                       End If
                                                   End Function


    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
                                                           Return y
                                                                  ~
BC42104: Variable 'z' is used before it has been assigned a value. A null reference exception could result at runtime.
                                                           Return z
                                                                  ~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                                   End Function
                                                   ~~~~~~~~~~~~
]]>
</expected>)
        End Sub

        <WorkItem(539777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539777")>
        <Fact>
        Public Sub TestOverloadResolutionWithStrictOff()

            Dim compilationDef =
<compilation name="TestOverloadResolutionWithStrictOff">
    <file name="M.vb">
Option Strict Off

Imports System

Module M
    Function Goo(ByVal x As Func(Of Object), ByVal y As Integer) As String
      Return "ABC"
    End Function

    Sub Goo(ByVal x As Func(Of String), ByVal y As Long)
    End Sub

    Sub Main()
        Dim x As Object = 1
        Dim y As Long = 1
        Console.WriteLine(Goo(Function() x, y).ToLower())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, expectedOutput:="abc")
        End Sub

        <WorkItem(539608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539608")>
        <Fact>
        Public Sub InvokeOffOfLambda()

            Dim compilationDef =
<compilation name="InvokeOffOfLambda">
    <file name="M.vb">
Option Strict Off

Module Module1
    Sub Main()
        Dim x = Function() As String
                    Return 1
                End Function.Invoke &amp; Function() As String
                                              Return 2
                                          End Function.Invoke 

        System.Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="12")
        End Sub

        <WorkItem(539519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539519")>
        <Fact>
        Public Sub ParseIncompleteMultiLineLambdaWithExpressionAfterAsClause()
            ' This looks like a single line lambda with an as clause but it is in fact a badly formed multi-line lambda
            Dim compilationDef =
                <compilation name="IncompleteMultiLineLambdaWithExpressionAfterAsClause">
                    <file name="M.vb">
                        <![CDATA[
                        Module Program
                          Sub Main()
                            Dim l1 As System.Func(Of Integer, Integer) = Function(x) As Integer x
                          End Sub
                        End Module
                        ]]>
                    </file>
                </compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
                                                                     <![CDATA[
BC36674: Multiline lambda expression is missing 'End Function'.
                            Dim l1 As System.Func(Of Integer, Integer) = Function(x) As Integer x
                                                                         ~~~~~~~~~~~~~~~~~~~~~~
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                            Dim l1 As System.Func(Of Integer, Integer) = Function(x) As Integer x
                                                                                                ~
BC30205: End of statement expected.
                            Dim l1 As System.Func(Of Integer, Integer) = Function(x) As Integer x
                                                                                                ~
]]>
                                                                 </errors>)
        End Sub

        <Fact>
        Public Sub Error_BC36532()

            Dim compilationDef =
<compilation name="BC36532">
    <file name="M.vb">
imports System        

Module Module1
    Sub Main()
        Dim x5 As Func(Of Integer) = Function() As Guid
                                         Return New Guid()
                                     End Function
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer)'.
        Dim x5 As Func(Of Integer) = Function() As Guid
                                     ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_BC36670()

            Dim compilationDef =
<compilation name="BC36670">
    <file name="M.vb">
imports System        

Module Module1
    Sub Main()
        Dim x6 As Func(Of Integer) = Sub(y As Guid)
                                     End Sub
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Integer)'.
        Dim x6 As Func(Of Integer) = Sub(y As Guid)
                                     ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Error_BC36625()

            Dim compilationDef =
<compilation name="BC36625">
    <file name="M.vb">
imports System        

Module Module1
    Sub Main()
        Dim x4 As System.Guid = Sub(x0 As Integer) x0 += 1
        Dim x41 As Object 
        x41 = CType(Sub(x0 As Integer) x0 += 1, Guid)
        x41 = DirectCast(Sub(x0 As Integer) x0 += 1, Guid)
        x41 = TryCast(Sub(x0 As Integer) x0 += 1, Guid)
        x41 = TryCast(Sub(x0 As Integer) x0 += 1, String)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36625: Lambda expression cannot be converted to 'Guid' because 'Guid' is not a delegate type.
        Dim x4 As System.Guid = Sub(x0 As Integer) x0 += 1
                                ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'Guid' because 'Guid' is not a delegate type.
        x41 = CType(Sub(x0 As Integer) x0 += 1, Guid)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'Guid' because 'Guid' is not a delegate type.
        x41 = DirectCast(Sub(x0 As Integer) x0 += 1, Guid)
                         ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'Guid' because 'Guid' is not a delegate type.
        x41 = TryCast(Sub(x0 As Integer) x0 += 1, Guid)
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'String' because 'String' is not a delegate type.
        x41 = TryCast(Sub(x0 As Integer) x0 += 1, String)
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(540867, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540867")>
        <Fact>
        Public Sub LambdaForSub2()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System
Module M1
    Sub Bar(Of T)(ByVal x As T) 
        Console.WriteLine(x)
    End Sub

    Sub Main()
        Dim x As Func(Of Action(Of String)) = Function() AddressOf Bar
        x.Invoke().Invoke("Hello World.")
    End Sub
End Module
        </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:="Hello World.")
        End Sub

        <WorkItem(528344, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528344")>
        <Fact()>
        Public Sub DelegateParametersCanBeOmittedInLambda()
            Dim source =
      <compilation>
          <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim x As Action(Of Integer) = Sub() Return
    End Sub
End Module
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(528346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528346")>
        <Fact()>
        Public Sub ParameterTypesCanBeRelaxedInLambda()
            Dim source =
      <compilation>
          <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim a As Action(Of String) = Sub(x As Object) Return
    End Sub
End Module
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(528347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528347")>
        <Fact()>
        Public Sub ReturnTypeCanBeRelaxedInLambda()
            Dim source =
      <compilation>
          <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim a As Func(Of Object) = Function() As String
                                       Return Nothing
                                   End Function
    End Sub
End Module

        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(528348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528348")>
        <Fact()>
        Public Sub ReturnValueOfLambdaCanBeIgnored()
            Dim source =
      <compilation>
          <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim x As Action = Function() 1
    End Sub
End Module
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(528355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528355")>
        <Fact()>
        Public Sub OverloadResolutionWithNestedLambdas()
            Dim source =
      <compilation>
          <file name="a.vb">
Imports System

Class C
    Shared Sub Main()
        Goo(Function() Function() Nothing)
    End Sub

    Shared Sub Goo(x As Func(Of Func(Of String)))
    End Sub

    Sub Goo(x As Func(Of Func(Of Object)))
    End Sub
End Class
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(541008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541008")>
        <Fact>
        Public Sub LambdaInFieldInitializer()
            Dim source =
      <compilation>
          <file name="C.vb">
Imports System

Class C
    Dim A As Action = Sub() Console.Write(ToString)
    Shared Sub Main()
        Dim c As New C()
        c.A()
    End Sub
End Class
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompileAndVerify(comp1, expectedOutput:="C")
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompileAndVerify(comp2, expectedOutput:="C")
        End Sub

        <WorkItem(541894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541894")>
        <Fact()>
        Public Sub InvokeLambda01()
            Dim source =
      <compilation>
          <file name="InvokeLambda01.vb">
Imports System

Module MMM
   Sub Main()
        Dim local = (Function(ap As Byte) As String
                         Return ap.ToString()
                     End Function)(123)
        Console.Writeline(local)
    End Sub
End Module
        </file>
      </compilation>
            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))
            CompileAndVerify(comp1, expectedOutput:="123")
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompileAndVerify(comp2, expectedOutput:="123")
        End Sub

        <WorkItem(528678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528678")>
        <Fact>
        Public Sub LambdaInsideParens()
            Dim source =
      <compilation>
          <file name="LambdaInsideParens.vb">
Imports System
Module Module1
    Public Sub Main()
        Dim func As Func(Of Long, Long) = (Function(x) x)
    End Sub
End Module
        </file>
      </compilation>
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics()
        End Sub

        <WorkItem(904998, "DevDiv/Personal")>
        <Fact>
        Public Sub BC36919ERR_DimInSingleLineSubLambda()
            Dim source = <compilation>
                             <file name="BC36919ERR_DimInSingleLineSubLambda"><![CDATA[
Module Module1
    Public Sub Main()
    Dim x = Sub() Dim y = 2
    End Sub
End Module
    ]]></file></compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_SubDisallowsStatement, "Dim y = 2"))

        End Sub

        <WorkItem(542665, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542665")>
        <Fact>
        Public Sub DimInSingleLineIfInSingleLineLambda()

            Dim compilationDef =
<compilation name="DimInSingleLineIfInSingleLineLambda">
    <file name="a.vb">
Imports System

Module M
    Sub Main
        Dim c = True
        Dim z As Action = Sub() If c Then Dim x = 2 : Console.WriteLine(x)
        z()
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="2").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub FunctionValueOfLambdaDoesNotHaveAName()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()

        Dim x = Function() as String
                    dim [Function] as Integer = 23
                    Return [Function].ToString()
                End Function

        dim y = Sub()
                    dim [Sub] as Integer = 23                    
                End Sub

    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef).VerifyDiagnostics()
        End Sub

        <WorkItem(546167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546167")>
        <Fact()>
        Public Sub ImplicitlyDeclaredVariableInsideLambdaReused()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off
Module Test
    Sub Sub1()
        Dim a = Function(implicit1) implicit1
        Dim b = Function() implicit1 'Creates a new Implicit variable

        Dim c = Sub()
                    Dim a1 = Function(implicit2) implicit2
                    Dim b2 = Function() implicit2 'Creates a new Implicit variable
                End Sub

    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36641: Lambda parameter 'implicit1' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
        Dim a = Function(implicit1) implicit1
                         ~~~~~~~~~
BC42104: Variable 'implicit1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim b = Function() implicit1 'Creates a new Implicit variable
                           ~~~~~~~~~
BC36641: Lambda parameter 'implicit2' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
                    Dim a1 = Function(implicit2) implicit2
                                      ~~~~~~~~~
BC42104: Variable 'implicit2' is used before it has been assigned a value. A null reference exception could result at runtime.
                    Dim b2 = Function() implicit2 'Creates a new Implicit variable
                                        ~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(760094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760094")>
        <Fact()>
        Public Sub Bug760094_OneTopLevelLambda()

            Dim compilationDef =
<compilation>
    <file name="a.vb">

Interface IRealBinding
End Interface

Interface IBinderConverter
End Interface

Class EqualityWeakReference
    Public IsAlive As Boolean
End Class

Class PropertyInfo
End Class

Friend Delegate Function OnChangeDelegateFactoryDelegate(ByVal currentIndex As Integer, ByVal weakSource As EqualityWeakReference) As System.Action(Of Object)
Friend Delegate Sub OnBindLastItem(ByVal index As Integer, ByVal pi As PropertyInfo, ByVal source As Object)

Class OnePropertyPathBinding
    Public Sub Bind(ByVal factory As OnChangeDelegateFactoryDelegate, ByVal OnfinalBind As OnBindLastItem)
    	factory(1, Nothing)(Nothing)
    End Sub

    Public Sub RemoveNotify(ByVal currentIndex As Integer)
    End Sub
End Class

Friend Class PropertyPathBindingItem

    Shared Sub Main()
    	Dim x = new PropertyPathBindingItem()
        System.Console.WriteLine("Done.")
    End Sub

    Private _DestinationBinding As OnePropertyPathBinding

    Friend Sub New()
        _DestinationBinding = New OnePropertyPathBinding()

        _DestinationBinding.Bind(Function(currentIndex As Integer, weakSource As EqualityWeakReference)
                                                                          Return Sub(value As Object)
                                                                                         _DestinationBinding.RemoveNotify(currentIndex)
                                                                                 End Sub
                                                                      End Function, Nothing)
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
Done.
]]>)

            verifier.VerifyIL("PropertyPathBindingItem..ctor",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  newobj     "Sub OnePropertyPathBinding..ctor()"
  IL_000c:  stfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_0017:  ldarg.0
  IL_0018:  ldftn      "Function PropertyPathBindingItem._Lambda$__2-0(Integer, EqualityWeakReference) As System.Action(Of Object)"
  IL_001e:  newobj     "Sub OnChangeDelegateFactoryDelegate..ctor(Object, System.IntPtr)"
  IL_0023:  ldnull
  IL_0024:  callvirt   "Sub OnePropertyPathBinding.Bind(OnChangeDelegateFactoryDelegate, OnBindLastItem)"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Lambda$__2-0",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  newobj     "Sub PropertyPathBindingItem._Closure$__2-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "PropertyPathBindingItem._Closure$__2-0.$VB$Me As PropertyPathBindingItem"
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      "PropertyPathBindingItem._Closure$__2-0.$VB$Local_currentIndex As Integer"
  IL_0013:  ldftn      "Sub PropertyPathBindingItem._Closure$__2-0._Lambda$__1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Closure$__2-0._Lambda$__1",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "PropertyPathBindingItem._Closure$__2-0.$VB$Me As PropertyPathBindingItem"
  IL_0006:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "PropertyPathBindingItem._Closure$__2-0.$VB$Local_currentIndex As Integer"
  IL_0011:  callvirt   "Sub OnePropertyPathBinding.RemoveNotify(Integer)"
  IL_0016:  ret
}
]]>)
        End Sub

        <WorkItem(760094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760094")>
        <Fact()>
        Public Sub Bug760094_TwoTopLevelLambdas()

            Dim compilationDef =
<compilation>
    <file name="a.vb">

Interface IRealBinding
End Interface

Interface IBinderConverter
End Interface

Class EqualityWeakReference
    Public IsAlive As Boolean
End Class

Class PropertyInfo
End Class

Friend Delegate Function OnChangeDelegateFactoryDelegate(ByVal currentIndex As Integer, ByVal weakSource As EqualityWeakReference) As System.Action(Of Object)
Friend Delegate Sub OnBindLastItem(ByVal index As Integer, ByVal pi As PropertyInfo, ByVal source As Object)

Class OnePropertyPathBinding
    Public Sub Bind(ByVal factory As OnChangeDelegateFactoryDelegate, ByVal OnfinalBind As OnBindLastItem)
    End Sub

    Public Sub RemoveNotify(ByVal currentIndex As Integer)
    End Sub
End Class

Friend Class PropertyPathBindingItem

    Private _DestinationBinding As OnePropertyPathBinding


    Friend Sub New()
        _DestinationBinding = New OnePropertyPathBinding()

        _DestinationBinding.Bind(Function(currentIndex As Integer, weakSource As EqualityWeakReference)
                                                                          Return Sub(value As Object)
                                                                                         _DestinationBinding.RemoveNotify(currentIndex)
                                                                                 End Sub
                                                                      End Function, Nothing)

        _DestinationBinding.Bind(Function(currentIndex As Integer, weakSource As EqualityWeakReference)
                                                            Return Sub(value As Object)
                                                                                         _DestinationBinding.RemoveNotify(currentIndex)
                                                                   End Sub
                                                        End Function, Nothing)
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseDll)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("PropertyPathBindingItem..ctor",
            <![CDATA[
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  newobj     "Sub OnePropertyPathBinding..ctor()"
  IL_000c:  stfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_0017:  ldarg.0
  IL_0018:  ldftn      "Function PropertyPathBindingItem._Lambda$__1-0(Integer, EqualityWeakReference) As System.Action(Of Object)"
  IL_001e:  newobj     "Sub OnChangeDelegateFactoryDelegate..ctor(Object, System.IntPtr)"
  IL_0023:  ldnull
  IL_0024:  callvirt   "Sub OnePropertyPathBinding.Bind(OnChangeDelegateFactoryDelegate, OnBindLastItem)"
  IL_0029:  ldarg.0
  IL_002a:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_002f:  ldarg.0
  IL_0030:  ldftn      "Function PropertyPathBindingItem._Lambda$__1-2(Integer, EqualityWeakReference) As System.Action(Of Object)"
  IL_0036:  newobj     "Sub OnChangeDelegateFactoryDelegate..ctor(Object, System.IntPtr)"
  IL_003b:  ldnull
  IL_003c:  callvirt   "Sub OnePropertyPathBinding.Bind(OnChangeDelegateFactoryDelegate, OnBindLastItem)"
  IL_0041:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Lambda$__1-0",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  newobj     "Sub PropertyPathBindingItem._Closure$__1-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "PropertyPathBindingItem._Closure$__1-0.$VB$Me As PropertyPathBindingItem"
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      "PropertyPathBindingItem._Closure$__1-0.$VB$Local_currentIndex As Integer"
  IL_0013:  ldftn      "Sub PropertyPathBindingItem._Closure$__1-0._Lambda$__1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Lambda$__1-2",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  newobj     "Sub PropertyPathBindingItem._Closure$__1-1..ctor()"
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      "PropertyPathBindingItem._Closure$__1-1.$VB$Me As PropertyPathBindingItem"
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      "PropertyPathBindingItem._Closure$__1-1.$VB$Local_currentIndex As Integer"
  IL_0013:  ldftn      "Sub PropertyPathBindingItem._Closure$__1-1._Lambda$__3(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Closure$__1-0._Lambda$__1",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "PropertyPathBindingItem._Closure$__1-0.$VB$Me As PropertyPathBindingItem"
  IL_0006:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "PropertyPathBindingItem._Closure$__1-0.$VB$Local_currentIndex As Integer"
  IL_0011:  callvirt   "Sub OnePropertyPathBinding.RemoveNotify(Integer)"
  IL_0016:  ret
}
]]>)

            verifier.VerifyIL("PropertyPathBindingItem._Closure$__1-1._Lambda$__3",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "PropertyPathBindingItem._Closure$__1-1.$VB$Me As PropertyPathBindingItem"
  IL_0006:  ldfld      "PropertyPathBindingItem._DestinationBinding As OnePropertyPathBinding"
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "PropertyPathBindingItem._Closure$__1-1.$VB$Local_currentIndex As Integer"
  IL_0011:  callvirt   "Sub OnePropertyPathBinding.RemoveNotify(Integer)"
  IL_0016:  ret
}
]]>)
        End Sub

        <WorkItem(1207506, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1207506"), WorkItem(4899, "https://github.com/dotnet/roslyn/issues/4899")>
        <Fact()>
        Public Sub InitClosureInsideABlockInAConstructor()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim f As New FpB(100, 100)
        System.Console.WriteLine(f.FPixels.Length)
    End Sub

End Module

Public Class FpB
    Public Property FPixels() As FloatPointF(,)
        Get
            System.Console.WriteLine("In getter")
            Return m_FPixels
        End Get
        Set(value As FloatPointF(,))
            m_FPixels = value
        End Set
    End Property
    Private m_FPixels As FloatPointF(,)


    Public Sub New(width As Integer, height As Integer)
        Try
            Dim w As Integer = width
            Dim h As Integer = height
            Me.FPixels = New FloatPointF(w - 1, h - 1) {}
            CallDelegate(Sub(y)
                             Dim x = Math.Min(0, w - 1)
                         End Sub)
        Catch ex As Exception
            System.Console.WriteLine(ex.Message)
        End Try
    End Sub

    Sub CallDelegate(d As Action(Of Integer))
        d(1)
    End Sub
End Class

Public Structure FloatPointF
    Public X As Single
    Public Y As Single
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation,
            <![CDATA[
In getter
10000
]]>)
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Public a, b As New C1((Function(n) n + 1)(1))

    Shared Sub Main()
        Dim x as New C0()
        System.Console.Write(x.a.F1)
        System.Console.Write(x.b.F1)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Shared Sub Main()
        Dim a, b As New C1((Function(n) n + 1)(1))
        System.Console.Write(a.F1)
        System.Console.Write(b.F1)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Public a, b As New C1((Function(n) n + 1)(1))

    Shared Sub Main()
        Dim x as New C0()
        System.Console.Write(x.a.F1)
        System.Console.Write(x.b.F1)

        x = New C0(True)
        System.Console.Write(x.a.F1)
        System.Console.Write(x.b.F1)
    End Sub

    Sub New()
    End Sub

    Sub New(b as Boolean)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2222")
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_4()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Public a, b As New C1((Function(n1) (Function(n2) n2 + 1)(n1))(1))

    Shared Sub Main()
        Dim x as New C0()
        System.Console.Write(x.a.F1)
        System.Console.Write(x.b.F1)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_5()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Shared Sub Main()
        Test(Of Object)()
    End Sub

    Shared Sub Test(Of T)()
        Dim a, b As New C1((Function(n)
                                Dim x as T
                                x = Nothing
                                Return CObj(x) + n + 1
                            End Function)(1))
        System.Console.Write(a.F1)
        System.Console.Write(b.F1)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <WorkItem(53593, "https://github.com/dotnet/roslyn/issues/53593")>
        <Fact()>
        Public Sub Issue53593_6()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Public Class C0

    Public WithEvents a, b As New C1((Function(n) n + 1)(1))

    Shared Sub Main()
        Dim x as New C0()
        System.Console.Write(x.a.F1)
        System.Console.Write(x.b.F1)
    End Sub
End Class

Public Class C1

    Public F1 as Integer

    Sub New(a As Integer)
        F1 = a
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="22")
        End Sub

        <WorkItem(64392, "https://github.com/dotnet/roslyn/issues/64392")>
        <Fact()>
        Public Sub ReferToFieldWithinLambdaInTypeAttribute_01()

            Dim compilationDef =
"
<Display(Function() $""{Name}"")>
public class Test
    <Display(Name:=""Name"")>
    public readonly property Name As String
end class

public class DisplayAttribute
    Inherits System.Attribute

    public Sub New()
    end Sub
End Class
"
            Dim compilation = CompilationUtils.CreateCompilation(compilationDef)
            compilation.AssertTheseEmitDiagnostics(
<expected><![CDATA[
BC30057: Too many arguments to 'Public Sub New()'.
<Display(Function() $"{Name}")>
         ~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
<Display(Function() $"{Name}")>
         ~~~~~~~~~~~~~~~~~~~~
BC30661: Field or property 'Name' is not found.
    <Display(Name:="Name")>
             ~~~~
]]></expected>
            )
        End Sub

        <WorkItem(64392, "https://github.com/dotnet/roslyn/issues/64392")>
        <Fact()>
        Public Sub ReferToFieldWithinLambdaInTypeAttribute_02()

            Dim compilationDef =
"
<Display(Function() Name)>
public class Test
    <Display(Name:=""Name"")>
    public readonly property Name As String
end class

public class DisplayAttribute
    Inherits System.Attribute

    public Sub New()
    end Sub
End Class
"
            Dim compilation = CompilationUtils.CreateCompilation(compilationDef)
            compilation.AssertTheseEmitDiagnostics(
<expected><![CDATA[
BC30057: Too many arguments to 'Public Sub New()'.
<Display(Function() Name)>
         ~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
<Display(Function() Name)>
         ~~~~~~~~~~~~~~~
BC30661: Field or property 'Name' is not found.
    <Display(Name:="Name")>
             ~~~~
]]></expected>
            )
        End Sub

        <Fact()>
        Public Sub CompilerLoweringPreserveAttribute_01()
            Dim source1 = "
Imports System
Imports System.Runtime.CompilerServices

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter)>
Public Class Preserve1Attribute
    Inherits Attribute
End Class

<CompilerLoweringPreserve>
<AttributeUsage(AttributeTargets.Parameter)>
Public Class Preserve2Attribute
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.Field Or AttributeTargets.Parameter)>
Public Class Preserve3Attribute
    Inherits Attribute
End Class
"
            Dim source2 = "
Class Test1
    Function M2(<Preserve1,Preserve2,Preserve3> x As Integer) As System.Func(Of Integer)
        Return Function() x
    End Function
End Class
"

            Dim validate = Sub(m As ModuleSymbol)
                               AssertEx.SequenceEqual(
                                   {"Preserve1Attribute"},
                                   m.GlobalNamespace.GetMember("Test1._Closure$__1-0.$VB$Local_x").GetAttributes().Select(Function(a) a.ToString()))
                           End Sub

            Dim comp1 = CreateCompilation(
                {source1, source2, CompilerLoweringPreserveAttributeDefinition},
                options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            CompileAndVerify(comp1, symbolValidator:=validate).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub IteratorLambda()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic

Class C
    Sub M()
        Dim lambda = Iterator Function() As IEnumerable(Of Integer)
                         Yield 1
                     End Function
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()

            Dim syntaxTree = compilation.SyntaxTrees.Single()
            Dim semanticModel = compilation.GetSemanticModel(syntaxTree)
            Dim lambdaSyntax = syntaxTree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()
            Dim lambdaSymbolInfo = semanticModel.GetSymbolInfo(lambdaSyntax)
            Dim lambdaMethod As IMethodSymbol = Assert.IsAssignableFrom(Of IMethodSymbol)(lambdaSymbolInfo.Symbol)
            Assert.True(lambdaMethod.IsIterator)
        End Sub

        <Fact>
        Public Sub NotIteratorLambda()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic

Class C
    Sub M()
        Dim lambda = Function() As IEnumerable(Of Integer)
                         Return Nothing
                     End Function
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()

            Dim syntaxTree = compilation.SyntaxTrees.Single()
            Dim semanticModel = compilation.GetSemanticModel(syntaxTree)
            Dim lambdaSyntax = syntaxTree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()
            Dim lambdaSymbolInfo = semanticModel.GetSymbolInfo(lambdaSyntax)
            Dim lambdaMethod As IMethodSymbol = Assert.IsAssignableFrom(Of IMethodSymbol)(lambdaSymbolInfo.Symbol)
            Assert.False(lambdaMethod.IsIterator)
        End Sub
    End Class
End Namespace
