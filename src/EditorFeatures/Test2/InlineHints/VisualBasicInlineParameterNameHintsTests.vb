' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <Trait(Traits.Feature, Traits.Features.InlineHints)>
    Public Class VisualBasicInlineParameterNameHintsTests
        Inherits AbstractInlineHintsTests

        <WpfFact>
        Public Async Function TestNoParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod()
                            End Sub

                            Sub TestMethod()

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestOneParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}5)
                            End Sub

                            Sub TestMethod(x As Integer)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=5)
                            End Sub

                            Sub TestMethod(x As Integer)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestTwoParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}5, {|y:|}2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=5, y:=2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestNegativeNumberParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}-5, {|y:|}2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=-5, y:=2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestCIntCast() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}CInt(5.5), {|y:|}2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=CInt(5.5), y:=2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestCTypeCast() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}CType(5.5, Integer), {|y:|}2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=CType(5.5, Integer), y:=2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestTryCastCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test({|x:|}TryCast(New Object(), String))
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test(x:=TryCast(New Object(), String))
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestDirectCastCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test({|x:|}DirectCast(New Object(), String))
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test(x:=DirectCast(New Object(), String))
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestCastingANegativeSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}CInt(-5.5), {|y:|}2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=CInt(-5.5), y:=2.2)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestObjectCreationParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}CInt(-5.5), {|y:|}2.2, {|obj:|}New Object())
                            End Sub

                            Sub TestMethod(x As Integer, y As Double, obj As Object)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=CInt(-5.5), y:=2.2, obj:=New Object())
                            End Sub

                            Sub TestMethod(x As Integer, y As Double, obj As Object)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestMissingParameterNameSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod()
                            End Sub

                            Sub TestMethod(As Integer)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact>
        Public Async Function TestDelegateParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Delegate Sub TestDelegate(ByVal str As String)

		                    Public Sub TestTheDelegate(ByVal test As TestDelegate)
			                    test({|str:|}"Test")
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Delegate Sub TestDelegate(ByVal str As String)

		                    Public Sub TestTheDelegate(ByVal test As TestDelegate)
			                    test(str:="Test")
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestParamsArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub UseParams(ParamArray args() As Integer)
			                   
		                    End Sub

		                    Public Sub Main()
			                    UseParams({|args:|}1, 2, 3, 4, 5)
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub UseParams(ParamArray args() As Integer)
			                   
		                    End Sub

		                    Public Sub Main()
			                    UseParams(1, 2, 3, 4, 5)
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestAttributesArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        &lt;Obsolete({|message:|}"test")&gt;
                        Public Class Foo
                            Sub TestMethod()

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        &lt;Obsolete(message:="test")&gt;
                        Public Class Foo
                            Sub TestMethod()

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestIncompleteFunctionCall() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}5,)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=5,)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact>
        Public Async Function TestInterpolatedString() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:|}$"")
                            End Sub

                            Sub TestMethod(x As String)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod(x:=$"")
                            End Sub

                            Sub TestMethod(x As String)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestNotOnEnableDisableBoolean1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub EnableLogging(value as boolean)
    end sub

    sub Main() 
        EnableLogging(true)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestNotOnEnableDisableBoolean2() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub DisableLogging(value as boolean)
    end sub

    sub Main() 
        DisableLogging(true)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnEnableDisableNonBoolean1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub EnableLogging(value as string)
    end sub

    sub Main() 
        EnableLogging({|value:|}"IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub EnableLogging(value as string)
    end sub

    sub Main() 
        EnableLogging(value:="IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnEnableDisableNonBoolean2() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub DisableLogging(value as string)
    end sub

    sub Main() 
        DisableLogging({|value:|}"IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub DisableLogging(value as string)
    end sub

    sub Main() 
        DisableLogging(value:="IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnSetMethodWithClearContext() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub SetClassification(classification as string)
    end sub

    sub Main() 
        SetClassification("IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestOnSetMethodWithUnclearContext() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub SetClassification(values as string)
    end sub

    sub Main() 
        SetClassification({|values:|}"IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub SetClassification(values as string)
    end sub

    sub Main() 
        SetClassification(values:="IO")
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithAlphaSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(objA as integer, objB as integer, objC as integer)
    end sub

    sub Main() 
        Goo(1, 2, 3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNonAlphaSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(objA as integer, objB as integer, nonobjC as integer)
    end sub

    sub Main() 
        Goo({|objA:|}1, {|objB:|}2, {|nonobjC:|}3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(objA as integer, objB as integer, nonobjC as integer)
    end sub

    sub Main() 
        Goo(objA:=1, objB:=2, nonobjC:=3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>
            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNumericSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(obj1 as integer, obj2 as integer, obj3 as integer)
    end sub

    sub Main() 
        Goo(1, 2, 3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, input)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47597")>
        Public Async Function TestMethodWithNonNumericSuffix1() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(obj1 as integer, obj2 as integer, nonobj3 as integer)
    end sub

    sub Main() 
        Goo({|obj1:|}1, {|obj2:|}2, {|nonobj3:|}3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
class A
    sub Goo(obj1 as integer, obj2 as integer, nonobj3 as integer)
    end sub

    sub Main() 
        Goo(obj1:=1, obj2:=2, nonobj3:=3)
    end sub
end class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66817")>
        Public Async Function TestParameterNameIsReservedKeyword() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Sub M()
        N({|Integer:|}1)
    End Sub

    Sub N([Integer] As Integer)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Sub M()
        N(Integer:=1)
    End Sub

    Sub N([Integer] As Integer)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66817")>
        Public Async Function TestParameterNameIsContextualKeyword() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Sub M()
        N({|Async:|}True)
    End Sub

    Sub N(Async As Boolean)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Dim output =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Sub M()
        N(Async:=True)
    End Sub

    Sub N(Async As Boolean)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input, output)
        End Function
    End Class
End Namespace
