' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SharedResourceHelpers
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Partial Public Class CommandLineTests
        Inherits BasicTestBase

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_01()
            Dim source =
            <compilation>
                <file name="a.vb">Imports System

Module Program
    Sub Main(args As String())
        Dim x As Integer
        Dim yy As Integer
        Const zzz As Long = 0
    End Sub

    Function goo()
    End Function
End Module
                    </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(5) : warning BC42024: Unused local variable: 'x'.

        Dim x As Integer
            ~           
PATH(6) : warning BC42024: Unused local variable: 'yy'.

        Dim yy As Integer
            ~~           
PATH(7) : warning BC42099: Unused local constant: 'zzz'.

        Const zzz As Long = 0
              ~~~            
PATH(11) : warning BC42105: Function 'goo' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.

    End Function
    ~~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
                Dim actual = output.ToString().Trim()
                Assert.Equal(expected, actual)
            End Using

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_02()
            ' It verifies the case where diagnostic does not have the associated location in it.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Runtime.CompilerServices

Module Module1
    Delegate Sub delegateType()

    Sub main()
        Dim a As ArgIterator = Nothing
        Dim d As delegateType = AddressOf a.Goo
    End Sub

    <Extension()> _
    Public Function Goo(ByVal x As ArgIterator) as Integer
	Return 1
    End Function
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(9) : error BC36640: Instance of restricted type 'ArgIterator' cannot be used in a lambda expression.

        Dim d As delegateType = AddressOf a.Goo
                                          ~    
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en", "-imports:System"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_03()
            ' It verifies the case where the squiggles covers the error span with tabs in it.
            Dim source = "Module Module1" + vbCrLf +
                     "  Sub Main()" + vbCrLf +
                     "      Dim x As Integer = ""a" + vbTab + vbTab + vbTab + "b""c ' There is a tab in the string." + vbCrLf +
                     "  End Sub" + vbCrLf +
                     "End Module" + vbCrLf

            Dim result = <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(3) : error BC30201: Expression expected.

      Dim x As Integer = "a            b"c ' There is a tab in the string.
                         ~                                                 
PATH(3) : error BC30004: Character constant must contain exactly one character.

      Dim x As Integer = "a            b"c ' There is a tab in the string.
                         ~~~~~~~~~~~~~~~~~                       
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Dim expected = ReplacePathAndVersionAndHash(result, file).Trim()
                Dim actual = output.ToString().Trim()
                Assert.Equal(expected, actual)
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_04()
            ' It verifies the case where the squiggles covers multiple lines.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Collections.Generic
Module Module1
    Sub Main()
        Dim i3 = From el In {
                3, 33, 333
                } Select el
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(5) : error BC36593: Expression of type 'Integer()' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.

        Dim i3 = From el In {
                            ~
                3, 33, 333
~~~~~~~~~~~~~~~~~~~~~~~~~~
                } Select el
~~~~~~~~~~~~~~~~~          
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_05()
            ' It verifies the case where the squiggles covers multiple lines.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System.Collections.Generic
Module _
    Module1
    Sub Main()
    End Sub
'End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(3) : error BC30625: 'Module' statement must end with a matching 'End Module'.

Module _
~~~~~~~~
    Module1
~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_06()
            ' It verifies the case where the squiggles covers the very long error span.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System
Imports System.Collections.Generic

Module Program

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Sub Main(args As String())
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(7) : error BC37220: Name 'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeEventHandler' exceeds the maximum length allowed in metadata.

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)
                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using

            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(545214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545214")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_07()
            ' It verifies the case where the error is on the last line.
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
        Console.WriteLine("Hello from VB")
    End Sub
End Class]]>
                </file>
            </compilation>

            Dim result =
                <file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(4) : error BC30625: 'Module' statement must end with a matching 'End Module'.

Module Module1
~~~~~~~~~~~~~~
PATH(8) : error BC30460: 'End Class' must be preceded by a matching 'Class'.

End Class
~~~~~~~~~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub

        <WorkItem(531606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531606")>
        <Fact()>
        Public Sub ErrorMessageWithSquiggles_08()
            Dim source =
            <compilation>
                <file name="a.vb">
                    <![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim i As system.Boolean,
    End Sub
End Module
]]>
                </file>
            </compilation>

            Dim result =
<file name="output">Microsoft (R) Visual Basic Compiler version VERSION (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.

PATH(6) : error BC30203: Identifier expected.

        Dim i As system.Boolean,
                                ~
</file>

            Dim fileName = "a.vb"
            Dim dir = Temp.CreateDirectory()
            Dim file = dir.CreateFile(fileName)
            file.WriteAllText(source.Value)

            Using output As New StringWriter()
                Dim vbc As New MockVisualBasicCompiler(Nothing, dir.Path, {fileName, "/preferreduilang:en"})
                vbc.Run(output, Nothing)

                Assert.Equal(ReplacePathAndVersionAndHash(result, file), output.ToString())
            End Using
            CleanupAllGeneratedFiles(file.Path)
        End Sub


    End Class

End Namespace
