' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PopulateSwitch
    Partial Public Class PopulateSwitchStatementTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        {|FixAllInDocument:|}Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select

        Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        {|FixAllInProject:|}Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.Buzz
                Exit Select
            Case MyEnum2.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
            Case MyEnum3.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.Buzz
                Exit Select
            Case MyEnum2.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
            Case MyEnum3.FizzBuzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        {|FixAllInSolution:|}Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.Buzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
        End Select
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Enum MyEnum1
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum1.Fizz
        Select Case e
            Case MyEnum1.Fizz
                Exit Select
            Case MyEnum1.Buzz
                Exit Select
            Case MyEnum1.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Enum MyEnum2
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum2.Fizz
        Select Case e
            Case MyEnum2.Fizz
                Exit Select
            Case MyEnum2.Buzz
                Exit Select
            Case MyEnum2.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Enum MyEnum3
    Fizz
    Buzz
    FizzBuzz
End Enum
Class Goo
    Sub Bar()
        Dim e = MyEnum3.Fizz
        Select Case e
            Case MyEnum3.Fizz
                Exit Select
            Case MyEnum3.Buzz
                Exit Select
            Case MyEnum3.FizzBuzz
                Exit Select
            Case Else
                Exit Select
        End Select
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function
    End Class
End Namespace
