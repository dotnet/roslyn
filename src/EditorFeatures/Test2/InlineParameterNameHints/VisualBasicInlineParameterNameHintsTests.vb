' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParameterNameHints
    Public Class VisualBasicInlineParameterNameHintsTests
        Inherits AbstractInlineParameterNameHintsTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
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

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestOneParameterSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:5|})
                            End Sub

                            Sub TestMethod(x As Integer)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestTwoParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:5|}, {|y:2.2|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestNegativeNumberParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:-5|}, {|y:2.2|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestCIntCast() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:CInt(5.5)|}, {|y:2.2|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestCTypeCast() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:CType(5.5, Integer)|}, {|y:2.2|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestTryCastCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test({|x:TryCast(New Object(), String)|})
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestDirectCastCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub test(x As String)

		                    End Sub

		                    Public Sub Main()
			                    test({|x:DirectCast(New Object(), String)|})
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestCastingANegativeSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:CInt(-5.5)|}, {|y:2.2|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestObjectCreationParametersSimpleCase() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:CInt(-5.5)|}, {|y:2.2|}, {|obj:New Object()|})
                            End Sub

                            Sub TestMethod(x As Integer, y As Double, obj As Object)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
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

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestDelegateParameter() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Delegate Sub TestDelegate(ByVal str As String)

		                    Public Sub TestTheDelegate(ByVal test As TestDelegate)
			                    test({|str:"Test"|})
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestParamsArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Public Class Test
		                    Public Sub UseParams(ParamArray args() As Integer)
			                   
		                    End Sub

		                    Public Sub Main()
			                    UseParams({|args:1|}, 2, 3, 4, 5)
		                    End Sub
	                    End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestAttributesArgument() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        &lt;Obsolete({|message:"test"|})&gt;
                        Public Class Foo
                            Sub TestMethod()

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.InlineParameterNameHints)>
        Public Async Function TestIncompleteFunctionCall() As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
                        Class Foo
                            Sub Main(args As String())
                                TestMethod({|x:5|},)
                            End Sub

                            Sub TestMethod(x As Integer, y As Double)

                            End Sub
                        End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyParamHints(input)
        End Function
    End Class
End Namespace
