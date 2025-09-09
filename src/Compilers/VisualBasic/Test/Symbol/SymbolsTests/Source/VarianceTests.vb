' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class VarianceTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Disallowed1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
    End Sub
End Module

Class C124(Of In T, Out S)
End Class

Structure S124(Of In T, Out S)
End Structure

Interface I125(Of In T, Out S)
End Interface

Delegate Sub D126(Of In T, Out S)()

Class C125
    Sub Goo(Of In T, Out S)()
    End Sub

    Function Bar(Of In T, Out S)() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
Class C124(Of In T, Out S)
              ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
Class C124(Of In T, Out S)
                    ~~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
Structure S124(Of In T, Out S)
                  ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
Structure S124(Of In T, Out S)
                        ~~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Sub Goo(Of In T, Out S)()
               ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Sub Goo(Of In T, Out S)()
                     ~~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Function Bar(Of In T, Out S)() As Integer
                    ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Function Bar(Of In T, Out S)() As Integer
                          ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub NestingInAnInterface1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">


Interface I1(Of In T, Out S)

    Interface I2(Of In T1, Out S1)
    End Interface

    Delegate Sub D1(Of In T1, Out S1)()

    Class C1(Of In T1)
        Class C2
        End Class

        Structure S1
        End Structure

        Enum E1
            x
        End Enum

        Interface I3(Of In T2, Out S2)
        End Interface

        Delegate Sub D1(Of In T2, Out S2)()
    End Class

    Class C3
        Class C4
        End Class

        Structure S2
        End Structure

        Enum E2
            x
        End Enum

        Interface I4(Of In T1, Out S1)
        End Interface

        Delegate Sub D2(Of In T2, Out S2)()
    End Class

    Structure S3(Of In T1)
        Class C5
        End Class

        Structure S4
        End Structure

        Enum E3
            x
        End Enum

        Interface I5(Of In T2, Out S2)
        End Interface

        Delegate Sub D3(Of In T2, Out S2)()
    End Structure

    Structure S5
        Class C6
        End Class

        Structure S5
        End Structure

        Enum E4
            x
        End Enum

        Interface I6(Of In T2, Out S2)
        End Interface

        Delegate Sub D4(Of In T2, Out S2)()
    End Structure

    Enum E5
        y
    End Enum

    Interface I7
        Class C7
        End Class

        Structure S6
        End Structure

        Enum E6
            x
        End Enum

        Interface I8(Of In T2, Out S2)
        End Interface

        Delegate Sub D5(Of In T2, Out S2)()
    End Interface
End Interface

Interface I9
    Class C8
    End Class

    Structure S7
    End Structure

    Enum E7
        x
    End Enum

    Interface I10(Of In T2, Out S2)
    End Interface

    Delegate Sub D6(Of In T2, Out S2)()
End Interface
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            Dim expected =
<expected>
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Class C1(Of In T1)
          ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Class C1(Of In T1)
                ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Class C3
          ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Structure S3(Of In T1)
              ~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Structure S3(Of In T1)
                    ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Structure S5
              ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Enum E5
         ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
        Class C7
              ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
        Structure S6
                  ~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
        Enum E6
             ~~
</expected>

            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

            ' Second time is intentional!
            CompilationUtils.AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub SyntheticEventDelegate1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Interface I1(Of In T)
    Event x1(y As Integer)
End Interface

Interface I2(Of Out T)
    Event x2(y As Integer)
End Interface

Interface I3(Of T)
    Event x3(y As Integer)
End Interface

Interface I4(Of In T)
    Interface I5
        Event x4(y As Integer)
    End Interface

    Class C1
        Event x5(y As Integer)
    End Class
End Interface
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            Dim expected =
<expected>
BC36738: Event definitions with parameters are not allowed in an interface such as 'I1(Of T)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I1(Of T)'. For example, 'Event x1 As Action(Of ...)'.
    Event x1(y As Integer)
    ~~~~~~~~~~~~~~~~~~~~~~
BC36738: Event definitions with parameters are not allowed in an interface such as 'I2(Of T)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I2(Of T)'. For example, 'Event x2 As Action(Of ...)'.
    Event x2(y As Integer)
    ~~~~~~~~~~~~~~~~~~~~~~
BC36738: Event definitions with parameters are not allowed in an interface such as 'I4(Of T)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I4(Of T)'. For example, 'Event x4 As Action(Of ...)'.
        Event x4(y As Integer)
        ~~~~~~~~~~~~~~~~~~~~~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Class C1
          ~~
</expected>

            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

            ' Second time is intentional!
            CompilationUtils.AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Consistency1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On

Imports System

Interface RW(Of Out T1, In T2) : End Interface
Interface R(Of Out T) : End Interface
Interface W(Of In T) : End Interface

Class Animal : End Class
Class Fish : Inherits Animal : End Class
Class Mammal : Inherits Animal : End Class
Class ChimeraFM : Implements R(Of Fish) : Implements R(Of Mammal) : End Class ' BC42333 "R(Of Mammal)", BC42333 "R(Of Fish)"
Delegate Sub D(Of In T1)(ByVal x As T1)

Class C(Of In T)  'BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
    Sub Test()
        Dim r As R(Of Animal) = New ChimeraFM  ' BC36737: Option Strict On does not allow implicit conversions from 'ChimeraFM' to 'R(Of Animal)' because the conversion is ambiguous.
    End Sub
End Class

Interface I(Of Out Tout, In Tin, Out TSout As Structure, In TSin As Structure)
    Inherits RW(Of Tout, Tin)  ' okay
    Inherits W(Of Tout)  ' BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
    Inherits R(Of Tin)  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.

    Sub f(ByVal x As Tin)  ' okay
    Function f() As Tout  ' okay
    Sub f(ByVal x As RW(Of Tin, Tout))  ' okay
    Sub f(ByVal x As RW(Of RW(Of Tin, Tout), RW(Of Tout, Tin)))  ' okay
    Interface J : End Interface  ' okay

    Sub f36742(ByVal x As Tout)  ' BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an Out type parameter.
    Sub f36742arr(ByVal x As Tout())  ' BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an Out type parameter.
    Function f36743() As Tin  ' BC36743: Type 'Tin' cannot be used as a return type because 'Tin' is an In type parameter.
    Sub f36749(ByRef x As Tout)  ' BC36749: Type 'Tout' cannot be used in this context because In/Out type parameters cannot be used for ByRef parameter types, and 'Tout' is an Out type parameter.
    Sub f36750(ByRef x As Tin)  ' BC36750: Type 'Tin' cannot be used in this context because In/Out type parameters cannot be used for ByRef parameter types, and 'Tin' is an In type parameter.
    Sub f36744(Of T As Tout)()  ' BC36744: Type 'Tout' cannot be used as a generic type constraint because 'Tout' is an Out type parameter.
    ReadOnly Property p36745() As Tin  ' BC36745: Type 'Tin' cannot be used as a ReadOnly property type because 'Tin' is an In type parameter.
    WriteOnly Property p36746() As Tout  ' BC36746: Type 'Tout' cannot be used as a WriteOnly property type because 'Tout' is an Out type parameter.
    Property p36747() As Tout  ' BC36747: Type 'Tout' cannot be used as a property type in this context because 'Tout' is an Out type parameter and the property is not marked ReadOnly.
    Property p36748() As Tin  ' BC36748: Type 'Tin' cannot be used as a property type in this context because 'Tin' is an In type parameter and the property is not marked WriteOnly.

    Function f36740() As TSout?  ' BC36740: Type 'TSout' cannot be used in 'TSout?' because In/Out type parameters cannot be made nullable, and 'TSout' is an Out type parameter.
    Function f36740b() As Nullable(Of TSout)  ' BC36740: Type 'TSout' cannot be used in 'TSout?' because In/Out type parameters cannot be made nullable, and 'TSout' is an Out type parameter.
    Sub f36741(ByVal x As TSin?)  ' BC36741: Type 'TSin' cannot be used in 'TSin?' because In/Out type parameters cannot be made nullable, and 'TSin is an In type parameter.
    Sub f36741b(ByVal x As Nullable(Of TSin))  ' BC36741: Type 'TSin' cannot be used in 'TSin?' because In/Out type parameters cannot be made nullable, and 'TSin is an In type parameter.

    Sub f36724(ByVal x As R(Of Tout)) ' BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
    Sub f36725(ByVal x As W(Of Tin))  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
    Sub f36728(ByVal x As R(Of R(Of Tout))) ' BC36728: Type 'Tout' cannot be used in 'R(Of Tout)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36729(ByVal x As W(Of R(Of Tin)))  ' BC36729: Type 'Tin' cannot be used in 'R(Of Tin)' in this context because 'Tin' is an 'In' type parameter.
    Sub f36726(ByVal x As RW(Of Tout, Tout))  ' BC36726: Type 'Tout' cannot be used for the 'T1' in 'RW(Of T1,T2)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36727(ByVal x As RW(Of Tin, Tin))  ' BC36727: Type 'Tin' cannot be used for the 'T2' in 'RW(Of T1,T2)' in this context because 'Tin' is an 'In' type parameter.
    Sub f36730(ByVal x As RW(Of RW(Of Tout, Tout), RW(Of Tout, Tin)))  ' BC36730: Type 'Tout' cannot be used for the 'T1' of 'RW(Of T1,T2)' in 'RW(Of Tout,Tout)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36731(ByVal x As RW(Of RW(Of Tin, Tin), RW(Of Tout, Tin)))  ' BC36731: Type 'Tin' cannot be used for the 'T2' of 'RW(Of T1,T2)' in 'RW(Of Tin,Tin)' in this context because 'Tin' is an 'In' type parameter.

    Event e36738()  ' BC36738: Event definitions with parameters are not allowed in an interface such as 'I(Of Tout,Tin,TSout,TSin)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I(Of Tout,Tin,TSout,TSin)'. For example, 'Event e36738 As Action(Of ...)'.
    Class C : End Class ' BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Event e As D(Of Tin)  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter

    Sub f36732(ByVal x As J)  ' BC36732: Type 'J' cannot be used in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
    Sub f36733(ByVal x As RW(Of J, Tout))  ' BC36733: Type 'J' cannot be used for the 'T1' in 'RW(Of T1,T2)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
    Sub f36735(ByVal x As R(Of R(Of J))) ' BC36735: Type 'J' cannot be used in 'R(Of J)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
    Sub f36736(ByVal x As RW(Of RW(Of J, Tout), RW(Of Tout, Tin)))  ' BC36736: Type 'J' cannot be used for the 'T1' of 'RW(Of T1,T2)' in 'RW(Of J,Tout)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.

    Sub f36732_1(ByVal x As R(Of J))
End Interface
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim expected =
<expected>
BC42333: Interface 'R(Of Mammal)' is ambiguous with another implemented interface 'R(Of Fish)' due to the 'In' and 'Out' parameters in 'Interface R(Of Out T)'.
Class ChimeraFM : Implements R(Of Fish) : Implements R(Of Mammal) : End Class ' BC42333 "R(Of Mammal)", BC42333 "R(Of Fish)"
                                                     ~~~~~~~~~~~~
BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
Class C(Of In T)  'BC36722: Keywords 'Out' and 'In' can only be used in interface and delegate declarations.
           ~~
BC36737: Option Strict On does not allow implicit conversions from 'ChimeraFM' to 'R(Of Animal)' because the conversion is ambiguous.
        Dim r As R(Of Animal) = New ChimeraFM  ' BC36737: Option Strict On does not allow implicit conversions from 'ChimeraFM' to 'R(Of Animal)' because the conversion is ambiguous.
                                ~~~~~~~~~~~~~
BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
    Inherits W(Of Tout)  ' BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
             ~~~~~~~~~~
BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
    Inherits R(Of Tin)  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
             ~~~~~~~~~
BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an 'Out' type parameter.
    Sub f36742(ByVal x As Tout)  ' BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an Out type parameter.
                          ~~~~
BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an 'Out' type parameter.
    Sub f36742arr(ByVal x As Tout())  ' BC36742: Type 'Tout' cannot be used as a ByVal parameter type because 'Tout' is an Out type parameter.
                             ~~~~~~
BC36743: Type 'Tin' cannot be used as a return type because 'Tin' is an 'In' type parameter.
    Function f36743() As Tin  ' BC36743: Type 'Tin' cannot be used as a return type because 'Tin' is an In type parameter.
                         ~~~
BC36749: Type 'Tout' cannot be used in this context because 'In' and 'Out' type parameters cannot be used for ByRef parameter types, and 'Tout' is an 'Out' type parameter.
    Sub f36749(ByRef x As Tout)  ' BC36749: Type 'Tout' cannot be used in this context because In/Out type parameters cannot be used for ByRef parameter types, and 'Tout' is an Out type parameter.
                          ~~~~
BC36750: Type 'Tin' cannot be used in this context because 'In' and 'Out' type parameters cannot be used for ByRef parameter types, and 'Tin' is an 'In' type parameter.
    Sub f36750(ByRef x As Tin)  ' BC36750: Type 'Tin' cannot be used in this context because In/Out type parameters cannot be used for ByRef parameter types, and 'Tin' is an In type parameter.
                          ~~~
BC36744: Type 'Tout' cannot be used as a generic type constraint because 'Tout' is an 'Out' type parameter.
    Sub f36744(Of T As Tout)()  ' BC36744: Type 'Tout' cannot be used as a generic type constraint because 'Tout' is an Out type parameter.
                       ~~~~
BC36745: Type 'Tin' cannot be used as a ReadOnly property type because 'Tin' is an 'In' type parameter.
    ReadOnly Property p36745() As Tin  ' BC36745: Type 'Tin' cannot be used as a ReadOnly property type because 'Tin' is an In type parameter.
                                  ~~~
BC36746: Type 'Tout' cannot be used as a WriteOnly property type because 'Tout' is an 'Out' type parameter.
    WriteOnly Property p36746() As Tout  ' BC36746: Type 'Tout' cannot be used as a WriteOnly property type because 'Tout' is an Out type parameter.
                                   ~~~~
BC36747: Type 'Tout' cannot be used as a property type in this context because 'Tout' is an 'Out' type parameter and the property is not marked ReadOnly.
    Property p36747() As Tout  ' BC36747: Type 'Tout' cannot be used as a property type in this context because 'Tout' is an Out type parameter and the property is not marked ReadOnly.
                         ~~~~
BC36748: Type 'Tin' cannot be used as a property type in this context because 'Tin' is an 'In' type parameter and the property is not marked WriteOnly.
    Property p36748() As Tin  ' BC36748: Type 'Tin' cannot be used as a property type in this context because 'Tin' is an In type parameter and the property is not marked WriteOnly.
                         ~~~
BC36740: Type 'TSout' cannot be used in 'TSout?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSout' is an 'Out' type parameter.
    Function f36740() As TSout?  ' BC36740: Type 'TSout' cannot be used in 'TSout?' because In/Out type parameters cannot be made nullable, and 'TSout' is an Out type parameter.
                         ~~~~~~
BC36740: Type 'TSout' cannot be used in 'TSout?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSout' is an 'Out' type parameter.
    Function f36740b() As Nullable(Of TSout)  ' BC36740: Type 'TSout' cannot be used in 'TSout?' because In/Out type parameters cannot be made nullable, and 'TSout' is an Out type parameter.
                          ~~~~~~~~~~~~~~~~~~
BC36741: Type 'TSin' cannot be used in 'TSin?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSin' is an 'In' type parameter.
    Sub f36741(ByVal x As TSin?)  ' BC36741: Type 'TSin' cannot be used in 'TSin?' because In/Out type parameters cannot be made nullable, and 'TSin is an In type parameter.
                          ~~~~~
BC36741: Type 'TSin' cannot be used in 'TSin?' because 'In' and 'Out' type parameters cannot be made nullable, and 'TSin' is an 'In' type parameter.
    Sub f36741b(ByVal x As Nullable(Of TSin))  ' BC36741: Type 'TSin' cannot be used in 'TSin?' because In/Out type parameters cannot be made nullable, and 'TSin is an In type parameter.
                           ~~~~~~~~~~~~~~~~~
BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
    Sub f36724(ByVal x As R(Of Tout)) ' BC36724: Type 'Tout' cannot be used in this context because 'Tout' is an 'Out' type parameter.
                          ~~~~~~~~~~
BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
    Sub f36725(ByVal x As W(Of Tin))  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
                          ~~~~~~~~~
BC36728: Type 'Tout' cannot be used in 'R(Of Tout)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36728(ByVal x As R(Of R(Of Tout))) ' BC36728: Type 'Tout' cannot be used in 'R(Of Tout)' in this context because 'Tout' is an 'Out' type parameter.
                          ~~~~~~~~~~~~~~~~
BC36729: Type 'Tin' cannot be used in 'R(Of Tin)' in this context because 'Tin' is an 'In' type parameter.
    Sub f36729(ByVal x As W(Of R(Of Tin)))  ' BC36729: Type 'Tin' cannot be used in 'R(Of Tin)' in this context because 'Tin' is an 'In' type parameter.
                          ~~~~~~~~~~~~~~~
BC36726: Type 'Tout' cannot be used for the 'T1' in 'RW(Of T1, T2)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36726(ByVal x As RW(Of Tout, Tout))  ' BC36726: Type 'Tout' cannot be used for the 'T1' in 'RW(Of T1,T2)' in this context because 'Tout' is an 'Out' type parameter.
                          ~~~~~~~~~~~~~~~~~
BC36727: Type 'Tin' cannot be used for the 'T2' in 'RW(Of T1, T2)' in this context because 'Tin' is an 'In' type parameter.
    Sub f36727(ByVal x As RW(Of Tin, Tin))  ' BC36727: Type 'Tin' cannot be used for the 'T2' in 'RW(Of T1,T2)' in this context because 'Tin' is an 'In' type parameter.
                          ~~~~~~~~~~~~~~~
BC36730: Type 'Tout' cannot be used for the 'T1' of 'RW(Of T1, T2)' in 'RW(Of Tout, Tout)' in this context because 'Tout' is an 'Out' type parameter.
    Sub f36730(ByVal x As RW(Of RW(Of Tout, Tout), RW(Of Tout, Tin)))  ' BC36730: Type 'Tout' cannot be used for the 'T1' of 'RW(Of T1,T2)' in 'RW(Of Tout,Tout)' in this context because 'Tout' is an 'Out' type parameter.
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36731: Type 'Tin' cannot be used for the 'T2' of 'RW(Of T1, T2)' in 'RW(Of Tin, Tin)' in this context because 'Tin' is an 'In' type parameter.
    Sub f36731(ByVal x As RW(Of RW(Of Tin, Tin), RW(Of Tout, Tin)))  ' BC36731: Type 'Tin' cannot be used for the 'T2' of 'RW(Of T1,T2)' in 'RW(Of Tin,Tin)' in this context because 'Tin' is an 'In' type parameter.
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36738: Event definitions with parameters are not allowed in an interface such as 'I(Of Tout, Tin, TSout, TSin)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I(Of Tout, Tin, TSout, TSin)'. For example, 'Event e36738 As Action(Of ...)'.
    Event e36738()  ' BC36738: Event definitions with parameters are not allowed in an interface such as 'I(Of Tout,Tin,TSout,TSin)' that has 'In' or 'Out' type parameters. Consider declaring the event by using a delegate type which is not defined within 'I(Of Tout,Tin,TSout,TSin)'. For example, 'Event e36738 As Action(Of ...)'.
    ~~~~~~~~~~~~~~
BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
    Class C : End Class ' BC36723: Enumerations, classes, and structures cannot be declared in an interface that has an 'In' or 'Out' type parameter.
          ~
BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter.
    Event e As D(Of Tin)  ' BC36725: Type 'Tin' cannot be used in this context because 'Tin' is an 'In' type parameter
               ~~~~~~~~~
BC36732: Type 'J' cannot be used in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
    Sub f36732(ByVal x As J)  ' BC36732: Type 'J' cannot be used in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
                          ~
BC36733: Type 'J' cannot be used for the 'T1' in 'RW(Of T1, T2)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
    Sub f36733(ByVal x As RW(Of J, Tout))  ' BC36733: Type 'J' cannot be used for the 'T1' in 'RW(Of T1,T2)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
                          ~~~~~~~~~~~~~~
BC36735: Type 'J' cannot be used in 'R(Of I(Of Tout, Tin, TSout, TSin).J)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
    Sub f36735(ByVal x As R(Of R(Of J))) ' BC36735: Type 'J' cannot be used in 'R(Of J)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
                          ~~~~~~~~~~~~~
BC36736: Type 'J' cannot be used for the 'T1' of 'RW(Of T1, T2)' in 'RW(Of I(Of Tout, Tin, TSout, TSin).J, Tout)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
    Sub f36736(ByVal x As RW(Of RW(Of J, Tout), RW(Of Tout, Tin)))  ' BC36736: Type 'J' cannot be used for the 'T1' of 'RW(Of T1,T2)' in 'RW(Of J,Tout)' in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout,Tin,TSout,TSin)', and 'I(Of Tout,Tin,TSout,TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout,Tin,TSout,TSin)'.
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36732: Type 'J' cannot be used in this context because both the context and the definition of 'J' are nested within interface 'I(Of Tout, Tin, TSout, TSin)', and 'I(Of Tout, Tin, TSout, TSin)' has 'In' or 'Out' type parameters. Consider moving the definition of 'J' outside of 'I(Of Tout, Tin, TSout, TSin)'.
    Sub f36732_1(ByVal x As R(Of J))
                            ~~~~~~~
</expected>

            CompilationUtils.AssertTheseDiagnostics(compilation, expected)
        End Sub

        <Fact>
        Public Sub Consistency2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Delegate Function Test(Of In T, Out S)(x As S) As T
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))

            Dim expected =
<expected>
BC36742: Type 'S' cannot be used as a ByVal parameter type because 'S' is an 'Out' type parameter.
Delegate Function Test(Of In T, Out S)(x As S) As T
                                            ~
BC36743: Type 'T' cannot be used as a return type because 'T' is an 'In' type parameter.
Delegate Function Test(Of In T, Out S)(x As S) As T
                                                  ~
</expected>

            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

        End Sub

        <Fact>
        Public Sub TypeInferenceInVariance()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb">
Option Strict On
Class Animal : End Class
Class Mammal : Inherits Animal : End Class
Interface IReadOnly(Of Out T) : End Interface
Class Zoo(Of T) : Implements IReadOnly(Of T) : End Class
Delegate Function D(Of Out T)() As T

Module Module1
    Sub f(Of T)(ByVal d1 As D(Of T), ByVal d2 As D(Of T))
        System.Console.WriteLine(GetType(T))
    End Sub
    Sub Main()
        Dim a As New Animal, m As New Mammal
        Dim za As New Zoo(Of Animal), zm As New Zoo(Of Mammal)
        Dim ra As IReadOnly(Of Animal) = zm, rm As IReadOnly(Of Mammal) = zm
        f(Function() 1, Function() 2)    ' Int
        f(Function() 1, Function() 1.1)  ' Double
        f(Function() a, Function() m)    ' Animal
        f(Function() za, Function() za)  ' Zoo(Of Animal)
        f(Function() za, Function() ra)  ' IReadOnly(Of Animal)
        f(Function() zm, Function() zm)  ' Zoo(Of Mammal)
        f(Function() zm, Function() ra)  ' IReadOnly(Of Animal)
        f(Function() zm, Function() rm)  ' IReadOnly(Of Mammal)
        f(Function() ra, Function() ra)  ' IReadOnly(Of Animal)
        f(Function() ra, Function() rm)  ' IReadOnly(Of Animal)
        f(Function() rm, Function() rm)  ' IReadOnly(Of Mammal)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Dim expected = <![CDATA[
System.Int32
System.Double
Animal
Zoo`1[Animal]
IReadOnly`1[Animal]
Zoo`1[Mammal]
IReadOnly`1[Animal]
IReadOnly`1[Mammal]
IReadOnly`1[Animal]
IReadOnly`1[Animal]
IReadOnly`1[Mammal]                                                  
]]>

            CompileAndVerify(compilation, expected)
        End Sub
    End Class

End Namespace
