' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

<CLSCompliant(False)>
Public Class ParseMethods
    Inherits BasicTestBase

    <WorkItem(917272, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseSub()
        ParseAndVerify(<![CDATA[
            class c1
                public Sub Goo()
                end sub
            end class
            Module Module1
                Sub Goo()
                end sub
                Sub Goo (i as integer)
                end sub
                Sub Goo (byval i as Integer, byval s as string)
                end sub
                Sub Goo (byref i as long, optional j as integer = 0)
                end sub
                Sub Goo (s as string, paramarray t as integer())
                end sub
                Sub Goo(of T1, T2, T3)(a as T1, b as T2, c as T3)
                end sub
            End Module
        ]]>).
        TraverseAllNodes()
    End Sub

    <WorkItem(917272, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseFunction()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Function Goo() as integer
                    end function
                    Function Goo (i as integer) as integer
                    end function
                    Function Goo (byval i as Integer, byval s as string) as integer
                    end function
                    Function Goo (byref i as long, optional j as integer = 0) as integer
                    end function
                    Function Goo (s as string, paramarray t as integer()) as integer
                    end function
                    Function Goo(of T1, T2, T3)(a as T1, b as T2, c as T3) as integer
                    end function
                End Module
        ]]>).
        TraverseAllNodes()
    End Sub

    <Fact>
    Public Sub ParseProperty()
        ParseAndVerify(<![CDATA[
            Module Module1
                Property Goo As Integer
                    Get 
                    End Get
                    Set(ByVal value As Integer)
                    End Set
                End Property
                Property Goo (i as integer) as integer
                End Property
                Property Goo(ByVal i As Integer, ByVal s As String) As Integer
                End Property
                Property Goo(ByRef i As Long, Optional ByVal j As Integer = 0) As Integer
                End Property
                Property Goo(ByVal s As String, ByVal ParamArray t As Integer()) As Integer
                End Property
            End Module
        ]]>)

        'Property Goo (i as integer) as integer
        'End Property
        'Property Goo(ByVal i As Integer, ByVal s As String) As Integer
        'End Property
        'Property Goo(ByRef i As Long, Optional ByVal j As Integer = 0) As Integer
        'End Property
        'Property Goo(ByVal s As String, ByVal ParamArray t As Integer()) As Integer
        'End Property
    End Sub

    <Fact>
    Public Sub BC32065ERR_GenericParamsOnInvalidMember_Property()
        ParseAndVerify(<![CDATA[
            Module Module1
                Property Goo(of T1, T2, T3)(a as T1, b as T2, c as T3) as integer
                end Property
            End Module
        ]]>,
        <errors>
            <error id="32065"/>
        </errors>)
        'ERRID_GenericParamsOnInvalidMember
    End Sub

    <Fact>
    Public Sub BC30198_ParseFunctionWithErrors()
        ParseAndVerify(<![CDATA[
                Module Module1
                    Sub MySub(p2 as A(B,C))
                    End Sub
                    Function Goo(
                    end function
                End Module
            ]]>,
            <errors>
                <error id="30198"/>
                <error id="30203"/>
                <error id="30638"/>
            </errors>)
        '                        Sub MySub(p2 as A(B,C))
        'End Sub
    End Sub

    <Fact>
    Public Sub ParseOperator()
        ParseAndVerify(<![CDATA[
            Module Module1
                Class c1
                    Operator +(ByVal a As Integer, ByVal b As Integer) As Integer
                    End Operator
                    Operator +(ByVal i As Integer, ByVal s As String) As Integer
                    End Operator
                    Operator +(ByRef i As Long, Optional ByVal j As Integer = 0) As Integer
                    End Operator
                    Operator +(ByVal s As String, ByVal ParamArray t As Integer()) As Integer
                    End Operator
                End Class
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30253ERR_MissingEndInterface_ParseOperatorInInterface()
        ParseAndVerify(<![CDATA[
            interface m1
            shared operator +(i as integer, j as integer) as integer
            return 0
            end operator
            end interface
        ]]>,
            Diagnostic(ERRID.ERR_InvInsideInterface, "shared operator +(i as integer, j as integer) as integer"),
            Diagnostic(ERRID.ERR_InvInsideInterface, "return 0"),
            Diagnostic(ERRID.ERR_InvInsideInterface, "end operator"))
    End Sub

    <WorkItem(890961, "DevDiv/Personal")>
    <Fact>
    Public Sub BC33005ERR_EndOperatorExpected_ParsePartialOperator()
        ParseAndVerify(<![CDATA[
            Class goo
                sub main
                    Public Operator
                End sub
            End Class
        ]]>,
        <errors>
            <error id="30429"/>
            <error id="30289"/>
            <error id="33005"/>
            <error id="30198"/>
            <error id="30199"/>
            <error id="33000"/>
            <error id="30026"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC33018ERR_OperatorDeclaredInModule()
        ParseAndVerify(<![CDATA[
            module m1
            shared operator +(i as integer, j as integer) as integer
            return 0
            end operator
            end module
        ]]>,
        <errors>
            <error id="33018"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub BC31112ERR_InvInsideEndsEvent()
        ParseAndVerify(<![CDATA[
            Module Module1
                Interface i1
                    Delegate Sub d1(ByVal i As Integer)
                    Event e1 As d1
                End Interface

               public Custom Event myevent1 As Action(Of Integer)
                '    'AddHandler(ByVal value As Action(Of Integer))
                '    'End AddHandler

                '    'RemoveHandler(ByVal value As Action(Of Integer))
                '    'End RemoveHandler

                '    'RaiseEvent(ByVal obj As Integer)
                '    'End RaiseEvent
                'End Event

               Custom Event myevent2 As Action(Of Integer)
                    'AddHandler(ByVal value As Action(Of Integer))
                    'End AddHandler

                    'RemoveHandler(ByVal value As Action(Of Integer))
                    'End RemoveHandler

                    'RaiseEvent(ByVal obj As Integer)
                    'End RaiseEvent
                End Event

                Class c1
                    Implements i1

                    Event e1(ByVal i As Integer)

                    Event e2 As i1.d1 Implements i1.e1


            end class
            End Module
        ]]>,
        <errors>
            <error id="31112"/>
            <error id="31114"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub ParseDelegateDeclaration()
        ParseAndVerify(<![CDATA[
            Module Module1
                Delegate Sub ds1 (i as integer)
                delegate Sub ds2 (of T1, T2)(p1 as T, p2 as T2)
                Delegate Function df1 (i as integer) as integer
                Delegate Function fd2 (of T1, T2) (p1 as T1) as T2
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30493ERR_ConstructorFunction_ParseDelegateDeclarationFail()
        ParseAndVerify(<![CDATA[
            Module Module1
                'Interface i1
                '    Sub s()
                '    Function f() As Integer
                'End Interface

                Delegate Sub New (i as integer)
                Delegate Function new () As integer
                Delegate d1()
                'Delegate Function d1() Handles i1.s
            End Module
        ]]>,
        <errors>
            <error id="30183"/>
            <error id="30493"/>
            <error id="30278"/>
        </errors>)
        ' ERRID_InvalidUseOfKeyword = 30183
        ' ERRID_ConstructorFunction = 30493
        ' ERRID_ExpectedSubOrFunction = 30278
    End Sub

    <Fact>
    Public Sub Bug862436()
        ParseAndVerify(<![CDATA[
            Module Module1
                Sub Method1(Optional ByVal x As Object = Nothing)
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862458()
        ParseAndVerify(<![CDATA[
            Imports System.Runtime.InteropServices
            Class Class1
                Declare Function ValidateAnsiBStr Lib "DllMarshalAsStr.Dll" (<MarshalAs(UnmanagedType.AnsiBStr)> ByVal s As String) As Integer
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862470()
        ParseAndVerify(<![CDATA[
            Class Class1
                Custom Event Event1 As Action
                    AddHandler(ByVal value As Action)
                    End AddHandler
                    RemoveHandler(ByVal value As Action)
                    End RemoveHandler
                    RaiseEvent()
                    End RaiseEvent
                End Event
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862481()
        ParseAndVerify(<![CDATA[
            Interface I1
            Event E1()
            End Interface
            Class C1
                Public WithEvents x As I1
                Private Sub x_E1() Handles x.E1 
                End Sub
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862502()
        ParseAndVerify(<![CDATA[
            Class C1
                Protected Event E1()
            End Class
            Class c2
                Inherits c1
                Sub Goo() Handles MyBase.E1 
                End Sub
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug862505()
        ParseAndVerify(<![CDATA[
            Class C1
                Function f1(Optional ByVal c1 As New Object())
                End Function
            End Class
        ]]>,
        <errors>
            <error id="30201"/>
            <error id="30812"/>
            <error id="30180"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub Bug863029()
        ParseAndVerify(<![CDATA[
            Module Helpers
                Public Property Trace As String 
                Sub AppendTrace(ByVal actual As String)
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug863032()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Shared Function Goo() As ULong? 
                End Function
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug863456()
        ParseAndVerify(<![CDATA[
            Class Class1
                Property scen5() As Integer
                    Get
                    End Get
                   <myattr5()> Set(ByVal value As Integer)
                   End Set
                End Property
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug863481()
        ParseAndVerify(<![CDATA[
          Class Class1
             Custom Event e1 As Action(Of Integer)
                    <Obsolete("qqq", True)>
                    AddHandler(ByVal value As Action(Of Integer))
                    End AddHandler
                    <Obsolete("qqq", True)>
                    RemoveHandler(ByVal value As Action(Of Integer))
                    End RemoveHandler
                    <Obsolete _
                    ("qqq", True)> _
                    RaiseEvent(ByVal obj As Integer)
                    End RaiseEvent
                End Event
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug863543()
        ParseAndVerify(<![CDATA[
            Interface I1
                Sub Goo()
            End Interface
            Interface I2
                Sub Goo()
            End Interface
            Public Class Class1
                Implements I1
                Implements I2
         
                Public Sub Goo() Implements I1.Goo, I2.Goo
                End Sub
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug866500()
        ParseAndVerify(<![CDATA[
            Class Class1
                Property X As Integer
                Property Y As Integer
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug866503()
        ParseAndVerify(<![CDATA[
            Class Class2
            End Class
            MustInherit Class Class1
                MustOverride Shared Widening Operator CType(ByVal x As Class1) As Class2
                    Return Nothing
                End Operator
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug866547()
        ParseAndVerify(<![CDATA[
            Class Class1(Of T)
                Implements IEnumerable(Of T)

                Public Function GetEnumerator() As IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
                End Function
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug866551()
        ParseAndVerify(<![CDATA[
            Module Module1
                Public Sub Ascending(ByVal b As Boolean
                                               ) 
                End Sub
            End Module
        ]]>)
    End Sub

    <Fact>
    Public Sub BC30808ERR_ObsoletePropertyGetLetSet_Bug866572()
        ParseAndVerify(<![CDATA[
            Module Module1
                 Property Let Goo()
                 End Property
            End Module

        ]]>,
        <errors>
            <error id="30808"/>
        </errors>)
    End Sub

    <Fact>
    Public Sub Bug867046()
        ParseAndVerify(<![CDATA[
            Class Class1
                Public Property PropXyz = 1
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub Bug867053()
        ParseAndVerify(<![CDATA[
            Class HasProps
                Public Property Scen4() As New <CLSCompliant(True)> System.Collections.ArrayList
            End Class
        ]]>)
    End Sub

    <Fact>
    Public Sub BC31121ERR_CustomEventInvInInterface_Bug868467()
        'Tree loses text when attempting to declare a custom event on an interface
        ParseAndVerify(<![CDATA[
            Public Interface ITest
                Custom Event Event1()
            End Interface
        ]]>,
        <errors>
            <error id="31121"/>
        </errors>)
    End Sub

    <WorkItem(891486, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30198_ParsePartialAutoProperty01()
        ParseAndVerify(<![CDATA[
            Class C
                Property x =
                sub s
                End sub
            End Class
        ]]>,
        <errors>
            <error id="30198"/>
            <error id="30199"/>
            <error id="30429"/>
        </errors>)
    End Sub

    <WorkItem(890768, "DevDiv/Personal")>
    <Fact>
    Public Sub BC36759ERR_AutoPropertyCantHaveParams_ParsePartialAutoProperty02()
        ParseAndVerify(<![CDATA[
            Module bar
                public property P(
            End Module
        ]]>,
        <errors>
            <error id="36759"/>
            <error id="30198"/>
            <error id="30203"/>
        </errors>)
    End Sub

    <WorkItem(883858, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30631ERR_MissingEndGet_PropertyGetTerminatedBySet()
        ParseAndVerify(<![CDATA[
            Class c1
            Property goo As Integer
                Get

                Set(value As Integer)
                End Set
            End Property
            End Class
        ]]>,
        <errors>
            <error id="30289"/>
            <error id="30631"/>
        </errors>)
    End Sub

    <WorkItem(893957, "DevDiv/Personal")>
    <Fact>
    Public Sub BC36674_ParseIncompleteEndConstructForStatementLambda()
        ParseAndVerify(<![CDATA[
                Friend Module Basic001mod
                    Sub Basic001()
                        Dim scen1_v1 As Goo(Of Vertebrates) = Function()
                            loc = "lambda"
                            Return New Vertebrates()
                            End Functio
            ]]>, <errors>
                     <error id="30625"/>
                     <error id="30026"/>
                     <error id="36674"/>
                     <error id="30678"/>
                 </errors>)
        ParseAndVerify(<![CDATA[
               Dim g2 = Function()
Dim a1 = 1
Dim a2
a2 = a1 + 2
Console.WriteLine("Function Lambda" & a2.ToString)

End 
            ]]>,
            <errors>
                <error id="36674"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Dim g2 = Function()
Dim a1 = 1
Dim a2
a2 = a1 + 2
            ]]>,
            <errors>
                <error id="36674"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                        Dim g2 = Function()
                     Dim a1 = 1
                     Dim a2
            ]]>,
            <errors>
                <error id="36674"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Dim g2 = Function()
Return
            ]]>,
            <errors>
                <error id="36674"/>
            </errors>)
    End Sub

    <WorkItem(893960, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseIncompleteCollectionInitializerInLambda()
        ParseAndVerify(<![CDATA[
                Namespace AutoPropInitializationLambda
                    Class HasAutoProps
                    Public Sub Goo()
                        Dim a = Function() If(True, New List(Of Integer) From 
            ]]>, <errors>
                     <error id="30626" message="'Namespace' statement must end with a matching 'End Namespace'." start="17" end="55"/>
                     <error id="30481" message="'Class' statement must end with a matching 'End Class'." start="76" end="94"/>
                     <error id="30026" message="'End Sub' expected." start="115" end="131"/>
                     <error id="30987" message="'{' expected." start="210" end="210"/>
                     <error id="30198" message="')' expected." start="210" end="210"/>
                 </errors>)
        ParseAndVerify(<![CDATA[
               Dim q2 = Function() If(4 > 3, Function() 1U
            ]]>,
            <errors>
                <error id="30198"/>
                <error id="32017"/>
            </errors>)
    End Sub

    <WorkItem(893976, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30215ERR_ExpectedSubFunction_ParseDeclareKeyword()
        ParseAndVerify(<![CDATA[
                Declare
            ]]>,
            <errors>
                <error id="30218"/>
                <error id="30203"/>
                <error id="30215"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Declare F
            ]]>,
            <errors>
                <error id="30218"/>
                <error id="30215"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Declare Fu
            ]]>,
            <errors>
                <error id="30218"/>
                <error id="30215"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Declare Fun
            ]]>,
            <errors>
                <error id="30218"/>
                <error id="30215"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Declare Func
            ]]>,
            <errors>
                <error id="30218"/>
                <error id="30215"/>
            </errors>)
        ParseAndVerify(<![CDATA[
                Declare Function A
            ]]>,
            <errors>
                <error id="30218"/>
            </errors>)
    End Sub

    <WorkItem(893977, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30218ERR_MissingLibInDeclare_ParseDeclareMissingLib()
        ParseAndVerify(<![CDATA[
                 Declare Function GetCORSystemDirectory L
            ]]>,
            <errors>
                <error id="30218"/>
            </errors>)
    End Sub

    <WorkItem(893603, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30198_ParseIncompleteLambda()
        ParseAndVerify(<![CDATA[
                Class SomeClass
    Private Class PrivateClass
        Public Sub Goo()
            dim y = Function
            ]]>,
            <errors>
                <error id="36674"/>
                <error id="30198"/>
                <error id="30199"/>
                <error id="30026"/>
                <error id="30481"/>
                <error id="30481"/>
            </errors>)
    End Sub

    <WorkItem(893959, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30198_ParseConditionalBinaryOperator()
        ParseAndVerify(<![CDATA[
Namespace AutoPropInitializationLambda
Class HasAutoProps
Public Property Scen4() As Object = Function(y As Object, z As Object) If(y Is Nothing, z I
]]>, <errors>
         <error id="30198"/>
         <error id="32017"/>
         <error id="30481"/>
         <error id="30626"/>
     </errors>)
    End Sub

    <WorkItem(894097, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30384_ParseLambdaCatch()
        ParseAndVerify(<![CDATA[
Sub LambdaSyntax02()
Try
Dim s6 = Function ((x) (x + 1))
Catch
]]>,
<errors>
    <error id="30384"/>
    <error id="36674"/>
    <error id="32014"/>
    <error id="30638"/>
    <error id="30203"/>
    <error id="30026"/>
</errors>)
    End Sub

    <WorkItem(2909, "DevDiv_Projects/Roslyn")>
    <WorkItem(894099, "DevDiv/Personal")>
    <Fact>
    Public Sub BC32065_ParseGenericFunction()
        ParseAndVerify(<![CDATA[
 Dim x = Function(Of
]]>,
<errors>
    <error id="36674"/>
    <error id="30198"/>
    <error id="30199"/>
    <error id="32065"/>
</errors>)
    End Sub

    <WorkItem(894452, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30384ERR_ExpectedEndTry_ParseErroneousAddressOf()
        ParseAndVerify(<![CDATA[
Sub DELError002()
Try
d1=addressof Sub
Catch
]]>,
<errors>
    <error id="30384"/>
    <error id="36673"/>
    <error id="30198"/>
    <error id="30199"/>
    <error id="30026"/>
</errors>)
    End Sub

    <WorkItem(896836, "DevDiv/Personal")>
    <Fact>
    Public Sub BC36918ERR_SubRequiresSingleStatement_ParseIncompleteLambdas()
        ParseAndVerify(<![CDATA[
Dim x = Sub]]>,
<errors>
    <error id="36918"/>
    <error id="30198"/>
    <error id="30199"/>
</errors>)
    End Sub

    <WorkItem(897818, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30081ERR_ExpectedEndIf_ParseElseIfInSubLambda()
        ParseAndVerify(<![CDATA[
    module m
    sub main()
        Dim d = Sub() If True Then P(arg) Else If
    end sub
    end module
]]>, <errors>
         <error id="30081" message="'If' must end with a matching 'End If'." start="76" end="78"/>
         <error id="30201" message="Expression expected." start="78" end="78"/>
     </errors>)
    End Sub

    <WorkItem(897848, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30084ERR_ExpectedNext_ParseColonInLambdaReturn()
        ParseAndVerify(<![CDATA[
Dim ddd = Function(m3)
Return Sub() If True Then For Each i In list2 : 
]]>,
<errors>
    <error id="30084"/>
    <error id="36674"/>
</errors>)
    End Sub

    <WorkItem(899947, "DevDiv/Personal")>
    <Fact>
    Public Sub BC32088ERR_TypeArgsUnexpected_ParseImplementsGenerics()
        ParseAndVerify(<![CDATA[
Sub M() Implements NS.I(Of
]]>,
<errors>
    <error id="30026"/>
    <error id="30182"/>
    <error id="30198"/>
</errors>)
        ParseAndVerify(<![CDATA[
Sub M() Implements NS.I(Of Object)
]]>,
<errors>
    <error id="30026"/>
    <error id="32088"/>
</errors>)
    End Sub

    <WorkItem(901336, "DevDiv/Personal")>
    <Fact>
    Public Sub BC36674ERR_MultilineLambdaMissingFunction_ParseColonInsideEmbeddedLambda()
        ParseAndVerify(<![CDATA[
Class Class5
    Dim w = Function()
                If True Then
                    Dim x = Sub()
                                Dim x = True : End If
]]>,
<errors>
    <error id="36673"/>
    <error id="36674"/>
    <error id="30481"/>
</errors>)
    End Sub

    <WorkItem(903444, "DevDiv/Personal")>
    <Fact>
    Public Sub BC36673ERR_MultilineLambdaMissingSub_ParseEventAfterLambda()
        ParseAndVerify(<![CDATA[Dim i = Sub(a as Integer, b as Long)
Event]]>,
<errors>
    <error id="30203"/>
    <error id="36673"/>
</errors>)
    End Sub

    <WorkItem(904759, "DevDiv/Personal")>
    <Fact>
    Public Sub BC30429ERR_InvalidEndSub_ParseEmbeddedSubLambda()
        ParseAndVerify(<![CDATA[Dim x1 = Sub()
Dim x2 = Sub(y) End Sub
End Sub]]>,
<errors>
    <error id="30429"/>
</errors>)
    End Sub

    <WorkItem(904937, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseErrorsInSubLambda()
        ' The errors are semantic errors not parse errors so no errors are expected.
        ParseAndVerify(<![CDATA[Namespace n1
    Module m1
        public sub bar()
            Dim mm = Sub(ByRef x As String, y As Integer) Console.WriteL
                                                                                    ine(x)
        End sub
        
        End Module
End Namespace
]]>)
    End Sub

    <WorkItem(539519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539519")>
    <Fact>
    Public Sub ParseIncompleteMultiLineLambdaWithExpressionAfterAsClause()
        ' This looks like a single line lambda with an as clause but it is in fact a badly formed multi-line lambda
        Dim compilationDef =
        <![CDATA[
Module Program
  Sub Main()
    Dim l1 As System.Func(Of Integer, Integer) = Function(x) As Integer x
  End Sub
End Module
]]>
        ParseAndVerify(compilationDef, <errors>
                                           <error id="36674" message="Multiline lambda expression is missing 'End Function'." start="78" end="100"/>
                                           <error id="30205" message="End of statement expected." start="101" end="102"/>
                                       </errors>)
    End Sub

    <WorkItem(537167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537167")>
    <Fact>
    Public Sub ParseShadowsAfterIncompleteLambda()
        ParseAndVerify(<![CDATA[
Structure Scen31

Shared Dim i = Function(a as Integer)
Shadows Private Function Goo()
End Structure
]]>,
<errors>
    <error id="36674"/>
    <error id="30027"/>
</errors>
        )
    End Sub

    <WorkItem(538494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538494")>
    <Fact>
    Public Sub ParseDefaultPropAfterIncompleteLambda()
        ParseAndVerify(<![CDATA[
Module m
Dim i = Sub(a as Integer)
Default Property
end module
]]>,
<errors>
    <error id="36673"/>
    <error id="30025"/>
    <error id="30203"/>
</errors>
        )
    End Sub

    <WorkItem(541286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541286")>
    <WorkItem(2257, "DevDiv_Projects/Roslyn")>
    <Fact>
    Public Sub BC33002ERR_OperatorNotOverloadable_ParseNotOverloadableOperators()
        ParseAndVerify(<![CDATA[
 Friend Module OLSpErr01mod
        Class A
            'COMPILEERROR: BC33002, "AndAlso"
            shared operator AndAlso(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "OrElse"
            shared operator OrElse(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "New"
            shared operator New(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "TypeOf"
            shared operator TypeOf(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "Is"
            shared operator Is(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "IsNot"
            shared operator IsNot(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33000, "AddressOf"
            shared operator AddressOf(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "GetType"
            shared operator GetType(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33000, "VarType"
            shared operator VarType(x as c1, y as c1) as Integer
            End Operator
            'COMPILEERROR: BC33002, "."
            shared operator .(x as c1, y as c1) as Integer
            End Operator
        End Class

        Friend Structure S
            'COMPILEERROR: BC33002,"+="
            Public Operator +=(ByVal x As cls1) As Integer
            End Operator
            'COMPILEERROR: BC33002, "IsNot"
            shared operator IsNot(x as c1) as Boolean
            End Operator
        End Structure
End Module
            ]]>,
            <errors>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33000"/>
                <error id="33002"/>
                <error id="33000"/>
                <error id="33002"/>
                <error id="33002"/>
                <error id="33002"/>
            </errors>)
    End Sub

    <Fact(), WorkItem(544074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544074")>
    Public Sub ParseSubLambdaWithReturnTypeInsideTryCatch()
        ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        Try
            'COMPILEERROR : BC30205, "As Object" 
            Dim x6 = Sub() As Object 
                     End Sub
        Catch
        Finally
        End Try
    End Sub     
End Module
        ]]>,
        <errors>
            <error id="30205"/>
        </errors>)
    End Sub

    <Fact(), WorkItem(545543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545543")>
    Public Sub ParseSingleLineSubLambdaWithForNext()
        ParseAndVerify(<![CDATA[
Module Program
    Sub Main()
        For i = 1 To 10
            Dim x = Sub() For j = 1 To 10
        Next j, i
    End Sub
End Module
        ]]>,
        Diagnostic(ERRID.ERR_ExpectedNext, "For j = 1 To 10"),
        Diagnostic(ERRID.ERR_ExtraNextVariable, "i"))
    End Sub

End Class
