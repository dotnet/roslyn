' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class InterpolatedStringTests
        Inherits BasicTestBase

        Private ReadOnly _formattableStringSource As Xml.Linq.XElement =
<file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, ParamArray args As Object()) As FormattableString
            Return New ConcreteFormattableString(formatString, args)
        End Function

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
</file>

        <Fact>
        Public Sub SimpleInterpolation()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim number = 8675309
        Write($"Jenny: {number}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Jenny: 8675309")

        End Sub

        <Fact>
        Public Sub InterpolationWithAlignment()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim number = 8675309
        Write($"Jenny: {number,12}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Jenny:      8675309")

        End Sub

        <Fact>
        Public Sub InterpolationWithAlignmentWithNonDecimalBaseAndOrTypeSuffix()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim number = 8675309
        Write($"Jenny: {number,&amp;H1A} {1,1UL}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Jenny: " & "8675309".PadLeft(&H1A) & " 1")

        End Sub

        <Fact>
        Public Sub InterpolationWithFormat()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim number = 8675309
        Write($"Jenny: {number:###-####}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Jenny: 867-5309")

        End Sub

        <Fact>
        Public Sub InterpolationWithFormatAndAlignment()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim number = 8675309
        Write($"Jenny: {number,12:###-####}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Jenny:     867-5309")

        End Sub

        <Fact>
        Public Sub TwoInterpolations()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim Hello = "Goodbye", World = "No one"
        Write($"This is a ""{NameOf(Hello)}, {NameOf(World)}!"" program.")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="This is a ""Hello, World!"" program.")

        End Sub

        <Fact>
        Public Sub EscapeSequences()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Dim arr As Object() = {}
        Write($"Solution: {{ { If(arr.Length > 0, String.Join("", "", arr), "Ø") } }}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Solution: { Ø }")

        End Sub

        <Fact>
        Public Sub EscapeSequencesWitNoInterpolations()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Write($"{{Ø}}")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="{Ø}")

        End Sub

        <Fact, WorkItem(1102783)>
        Public Sub SmartQuotes()

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main()

        Dim arr = {
            $<%= LEFT_DOUBLE_QUOTATION_MARK %>1<%= RIGHT_DOUBLE_QUOTATION_MARK %>,
            $<%= RIGHT_DOUBLE_QUOTATION_MARK %>2<%= LEFT_DOUBLE_QUOTATION_MARK %>,
            $<%= LEFT_DOUBLE_QUOTATION_MARK %>3",
            $"4<%= LEFT_DOUBLE_QUOTATION_MARK %>,
            $"5<%= RIGHT_DOUBLE_QUOTATION_MARK %>,
            $<%= RIGHT_DOUBLE_QUOTATION_MARK %>6",
            $" <%= RIGHT_DOUBLE_QUOTATION_MARK %><%= LEFT_DOUBLE_QUOTATION_MARK %> ",
            $<%= RIGHT_DOUBLE_QUOTATION_MARK %> {1:x<%= RIGHT_DOUBLE_QUOTATION_MARK %><%= LEFT_DOUBLE_QUOTATION_MARK %>y} <%= LEFT_DOUBLE_QUOTATION_MARK %>
        }

        System.Console.WriteLine(String.Join("", arr))

    End Sub
End Module    </file>
</compilation>, expectedOutput:="123456 ""  xy")

        End Sub

        <Fact, WorkItem(1102800)>
        Public Sub FullwidthDelimiters()

            ' Any combination of fullwidth and ASCII curly braces of the same direction is an escaping sequence for the corresponding ASCII curly brace.
            ' We insert that curly brace doubled and because this is the escaping sequence understood by String.Format, that will be replaced by a single brace.
            ' This is deliberate design and it aligns with existing rules for double quote escaping in strings.
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console
Module Program
    Sub Main()
        WriteLine($"{0<%= FULLWIDTH_RIGHT_CURLY_BRACKET %>" = "0")
        WriteLine($"<%= FULLWIDTH_LEFT_CURLY_BRACKET %>10<%= FULLWIDTH_COLON %>X}" = "A")

        WriteLine($"}}" = "}")
        WriteLine($"}<%= FULLWIDTH_RIGHT_CURLY_BRACKET %>" = "}")
        WriteLine($"<%= FULLWIDTH_RIGHT_CURLY_BRACKET %>}" = "}")
        WriteLine($"<%= FULLWIDTH_RIGHT_CURLY_BRACKET %><%= FULLWIDTH_RIGHT_CURLY_BRACKET %>" = "}")

        WriteLine($"{{" = "{")
        WriteLine($"{<%= FULLWIDTH_LEFT_CURLY_BRACKET %>" = "{")
        WriteLine($"<%= FULLWIDTH_LEFT_CURLY_BRACKET %>{" = "{")
        WriteLine($"<%= FULLWIDTH_LEFT_CURLY_BRACKET %><%= FULLWIDTH_LEFT_CURLY_BRACKET %>" = "{")

        WriteLine(<%= FULLWIDTH_DOLLAR_SIGN %><%= FULLWIDTH_QUOTATION_MARK %><%= LEFT_DOUBLE_QUOTATION_MARK %><%= LEFT_DOUBLE_QUOTATION_MARK %>" = """")
    End Sub
End Module</file>
</compilation>, expectedOutput:="True
True
True
True
True
True
True
True
True
True
True")

        End Sub

        <Fact>
        Public Sub NestedInterpolations()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Console

Module Program
    Sub Main()
        Write($"The date/time is {$"{#2014-12-18 09:00:00#:yyyy-MM-dd HH:mm:ss}"}.")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="The date/time is 2014-12-18 09:00:00.")

        End Sub

        <Fact>
        Public Sub StringToCharArrayConversion()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As Char() = $"{1} + {1} = {2}"
        Write(s.Length)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="9")

        End Sub

        <Fact>
        Public Sub UserDefinedConversions()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As C = "{1} + {1} = {2}"
    End Sub
End Module

Class C
    Shared Widening Operator CType(obj As String) As C
        Write("CType")
        Return New C()
    End Operator
End Class
    </file>
</compilation>, expectedOutput:="CType")

        End Sub

        <Fact>
        Public Sub TargetTyping()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As IFormattable = $"{1} + {1} = {2}"
        Write(CObj(s).ToString())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="1 + 1 = 2")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim iFormattableType = compilation.GetTypeByMetadataName("System.IFormattable")

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(iFormattableType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub TargetTypingThroughArrayLiteralsAndLambdas()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        M(Of IFormattable)({$"", Nothing})

        N(Of IFormattable)(Function()
                               If True Then
                                   Return $""
                               Else
                                   Return Nothing
                               End If
                           End Function)
    End Sub

    Sub M(Of T)(obj As T())
        Write(obj(0).GetType().Name)
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(f().GetType().Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="ConcreteFormattableStringConcreteFormattableString")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim iFormattableType = compilation.GetTypeByMetadataName("System.IFormattable")

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(iFormattableType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub TargetTypingExplicitConversions()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim ctf = CType($"{1} + {1} = {2}", IFormattable)
        Dim ctfs = CType($"{1} + {1} = {2}", FormattableString)
        Dim dcf = DirectCast($"{1} + {1} = {2}", IFormattable)
        Dim dcfs = DirectCast($"{1} + {1} = {2}", FormattableString)
        Dim tcf = TryCast($"{1} + {1} = {2}", IFormattable)
        Dim tcfs = TryCast($"{1} + {1} = {2}", FormattableString)
        Write(CObj(ctf).ToString())
        Write(CObj(ctfs).ToString())
        Write(CObj(dcf).ToString())
        Write(CObj(dcfs).ToString())
        Write(CObj(tcf).ToString())
        Write(CObj(tcfs).ToString())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="1 + 1 = 21 + 1 = 21 + 1 = 21 + 1 = 21 + 1 = 21 + 1 = 2")

        End Sub

        <Fact>
        Public Sub TargetTypingThroughArrayLiteralsAndLambdasWithNarrowingConversionFromString()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        M(Of Integer)({$"1", Nothing})

        N(Of Integer)(Function()
                               If True Then
                                   Return $"1"
                               Else
                                   Return Nothing
                               End If
                           End Function)
    End Sub

    Sub M(Of T)(obj As T())
        Write(obj(0).GetType().Name)
        Write(obj(0))
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(f().GetType().Name)
        Write(f())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Int321Int321")

        End Sub

        <Fact>
        Public Sub InterpolatedStringConversionIsWidening()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As IFormattable = $"{1} + {1} = {2}"
        Write(CObj(s).ToString())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="1 + 1 = 2")

        End Sub

        <Fact>
        Public Sub ParenthesizationPreventsTargetTyping()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As IFormattable = ($"{1} + {1} = {2}")
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC42322: Runtime errors might occur when converting 'String' to 'IFormattable'.
        Dim s As IFormattable = ($"{1} + {1} = {2}")
                                ~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub InvariantCulture()

            Dim previousCulture = Threading.Thread.CurrentThread.CurrentCulture

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console
Imports System.Threading
Imports System.Globalization
Imports System.FormattableString

Module Program
    Sub Main()
        Dim previousCulture = Thread.CurrentThread.CurrentCulture
        Try
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE")
            Write($"{1.5}")
            Write(CObj(CType($"{1.5}", IFormattable)).ToString())
            Write(CObj(CType($"{1.5}", FormattableString)).ToString())
            Write(Invariant($"{1.5}"))
        Finally
            Thread.CurrentThread.CurrentCulture = previousCulture
        End Try
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="1,51,51,51.5")

            Assert.Equal(previousCulture, Threading.Thread.CurrentThread.CurrentCulture)
        End Sub

        <Fact>
        Public Sub OverloadResolutionWithStringAndIFormattablePrefersString()

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main(args As String())
        M($"")
    End Sub

    Sub M(s As String)
        Write("String")
    End Sub

    Sub M(s As IFormattable)
        Write("IFormattable")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="String")

        End Sub

        <Fact>
        Public Sub OverloadResolutionWithStringAndFormattableStringPrefersString()

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main(args As String())
        M($"")
    End Sub

    Sub M(s As String)
        Write("String")
    End Sub

    Sub M(s As FormattableString)
        Write("FormattableString")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="String")

        End Sub

        <Fact>
        Public Sub OverloadResolutionWithIFormattableAndFormattableStringPrefersFormattableString()

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main(args As String())
        M($"")
    End Sub

    Sub M(s As IFormattable)
        Write("IFormattable")
    End Sub

    Sub M(s As FormattableString)
        Write("FormattableString")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="FormattableString")

        End Sub

        <Fact>
        Public Sub OverloadResolutionWithStringIFormattableAndFormattableStringPrefersString()

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main(args As String())
        M($"")
    End Sub

    Sub M(s As String)
        Write("String")
    End Sub

    Sub M(s As IFormattable)
        Write("IFormattable")
    End Sub

    Sub M(s As FormattableString)
        Write("FormattableString")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="String")

        End Sub

        <Fact>
        Public Sub OverloadResolutionWithFuncOfStringAndFuncOfFormattableStringPrefersFuncOfString()

            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main(args As String())
        M(Function() $"")
    End Sub

    Sub M(f As Func(Of String))
        Write("String")
    End Sub

    Sub M(f As Func(Of FormattableString))
        Write("FormattableString")
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="String")

        End Sub

        <Fact>
        Public Sub TypeInferredAsString()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s = $"{1} + {1} = {2}"
        M(s)

        Dim arr1 = {$""}
        M(arr1)

        Dim arr2 = {$"", $""}
        M(arr2)

        M($"")
    End Sub

    Sub M(Of T)(obj As T)
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="StringString[]String[]String")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(stringType, info.ConvertedType)
            Next
        End Sub

        <Fact>
        Public Sub DominantTypeWithNothingLiteralInferredAsString()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        M({$"", Nothing})

        M(If(True, Nothing, $""))

        N(Function()
              If True Then
                  Return $""
              Else
                  Return Nothing
              End If
          End Function)
    End Sub

    Sub M(Of T)(obj As T)
        Write(GetType(T).Name)
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="String[]StringString")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(stringType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub DominantTypeWithIFormattableCannotBeInferred()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim iFormattableInstance As IFormattable = $""

        M({$"", Nothing, iFormattableInstance})

        M(If(True, iFormattableInstance, $""))

        N(Function()
              If True Then
                  Return $""
              ElseIf True Then
                  Return iFormattableInstance
              Else
                  Return Nothing
              End If
          End Function)
    End Sub

    Sub M(Of T)(obj As T)
        Write(GetType(T).Name)
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Object[]ObjectObject")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim objectType = compilation.GetSpecialType(SpecialType.System_Object)

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax).Skip(1)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(objectType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub DominantTypeWithFormattableStringCannotBeInferred()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim formattableStringInstance As FormattableString = $""

        M({$"", Nothing, formattableStringInstance})

        M(If(True, formattableStringInstance, $""))

        N(Function()
              If True Then
                  Return $""
              ElseIf True Then
                  Return formattableStringInstance
              Else
                  Return Nothing
              End If
          End Function)
    End Sub

    Sub M(Of T)(obj As T)
        Write(GetType(T).Name)
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="Object[]ObjectObject")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim objectType = compilation.GetSpecialType(SpecialType.System_Object)

            For Each e In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax).Skip(1)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                Dim info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(objectType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub DominantTypeWithIFormattableAndFormattableStringCannotBeInferred()
            Dim verifier = CompileAndVerify(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim formattableStringInstance As FormattableString = $""
        Dim iFormattableInstance As IFormattable = $""

        M({$"", Nothing, formattableStringInstance, iFormattableInstance})

        N(Function()
              If True Then
                  Return $""
              ElseIf True Then
                  Return formattableStringInstance
              ElseIf True Then
                  Return iFormattableInstance
              Else
                  Return Nothing
              End If
          End Function)
    End Sub

    Sub M(Of T)(obj As T)
        Write(GetType(T).Name)
    End Sub

    Sub N(Of T)(f As Func(Of T))
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="IFormattable[]IFormattable")

            Dim compilation = verifier.Compilation
            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim iFormattableType = compilation.GetTypeByMetadataName("System.IFormattable")

            Dim interpolatedStrings = root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax).ToArray()
            Dim formattableStringType = compilation.GetTypeByMetadataName("System.FormattableString")

            Assert.True(sm.GetSymbolInfo(interpolatedStrings(0)).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

            Dim info = sm.GetTypeInfo(interpolatedStrings(0))
            Assert.Equal(stringType, info.Type)
            Assert.Equal(formattableStringType, info.ConvertedType)

            For Each e In interpolatedStrings.Skip(1)
                Assert.True(sm.GetSymbolInfo(e).IsEmpty, "Interpolated String expressions shouldn't bind to symbols.")

                info = sm.GetTypeInfo(e)
                Assert.Equal(stringType, info.Type)
                Assert.Equal(iFormattableType, info.ConvertedType)
            Next

        End Sub

        <Fact>
        Public Sub ERR_InterpolationAlignmentOutOfRange()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Write($"This alignment is just small enough {New Object,32767}.") ' Short.MaxValue
        Write($"This alignment is too big {New Object,32768}.") ' Short.MaxValue + 1
        Write($"This alignment is too small {New Object,-32768}.") ' -Short.MaxValue - 1
        Write($"This alignment is just big enough {New Object,-32767}.") ' -Short.MaxValue
        Write($"This alignment is way too big {New Object,9223372036854775808}.") ' Long.MaxValue + 1
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC37250: Alignment value is outside of the supported range.
        Write($"This alignment is too big {New Object,32768}.") ' Short.MaxValue + 1
                                                      ~~~~~
BC37250: Alignment value is outside of the supported range.
        Write($"This alignment is too small {New Object,-32768}.") ' -Short.MaxValue - 1
                                                        ~~~~~~
BC30036: Overflow.
        Write($"This alignment is way too big {New Object,9223372036854775808}.") ' Long.MaxValue + 1
                                                          ~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Error_AmbiguousTypeArgumentInferenceWithIFormattable()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim iFormattableInstance As IFormattable = $""

        M($"", iFormattableInstance)
    End Sub

    Sub M(Of T)(a As T, b As T)
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC36651: Data type(s) of the type parameter(s) in method 'Public Sub M(Of T)(a As T, b As T)' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        M($"", iFormattableInstance)
        ~
</expected>)

        End Sub

        <Fact>
        Public Sub Error_AmbiguousTypeArgumentInferenceWithFormattableString()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim formattableStringInstance As FormattableString = $""

        M($"", formattableStringInstance)
    End Sub

    Sub M(Of T)(a As T, b As T)
        Write(GetType(T).Name)
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub M(Of T)(a As T, b As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        M($"", formattableStringInstance)
        ~
</expected>)

        End Sub

        <Fact>
        Public Sub Error_InterpolationExpressionNotAValue()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Write($"Expression {AddressOf Main} is not a value.")
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        Write($"Expression {AddressOf Main} is not a value.")
                            ~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub FlowAnalysis_Warning_InterpolatedVariableUsedBeforeBeingAssigned()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim v As Object

        Write($"{v}")
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseCompileDiagnostics(compilation,
<expected>
BC42104: Variable 'v' is used before it has been assigned a value. A null reference exception could result at runtime.
        Write($"{v}")
                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub FlowAnalysis_InterpolatedLocalConstNotConsideredUnused()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Const v As Object = Nothing

        Write($"{v}")
    End Sub
End Module
    </file>
</compilation>)

            AssertNoDiagnostics(compilation)

        End Sub

        <Fact>
        Public Sub FlowAnalysis_AnalyzeDataFlowReportsCorrectResultsForVariablesUsedInInterpolations()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim v As Object = Nothing

        WriteLine($"{v}")

        WriteLine(v)
    End Sub
End Module
    </file>
</compilation>)

            AssertNoDiagnostics(compilation)

            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim vSymbol = CType(sm.GetDeclaredSymbol(root.DescendantNodes().OfType(Of ModifiedIdentifierSyntax).Single()), ILocalSymbol)

            Dim writeLineCall = root.DescendantNodes().OfType(Of ExpressionStatementSyntax).First()
            Assert.Equal("WriteLine($""{v}"")", writeLineCall.ToString())

            Dim analysis = sm.AnalyzeDataFlow(writeLineCall)

            Assert.True(analysis.Succeeded)
            Assert.DoesNotContain(vSymbol, analysis.AlwaysAssigned)
            Assert.DoesNotContain(vSymbol, analysis.Captured)
            Assert.Contains(vSymbol, analysis.DataFlowsIn)
            Assert.DoesNotContain(vSymbol, analysis.DataFlowsOut)
            Assert.Contains(vSymbol, analysis.ReadInside)
            Assert.Contains(vSymbol, analysis.ReadOutside)
            Assert.DoesNotContain(vSymbol, analysis.UnsafeAddressTaken)
            Assert.DoesNotContain(vSymbol, analysis.VariablesDeclared)
            Assert.DoesNotContain(vSymbol, analysis.WrittenInside)
            Assert.Contains(vSymbol, analysis.WrittenOutside)

        End Sub

        <Fact>
        Public Sub FlowAnalysis_AnalyzeDataFlowReportsCorrectResultsForVariablesCapturedInInterpolations()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <%= _formattableStringSource %>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim v As Object = Nothing

        WriteLine($"{(Function() v)()}")

        WriteLine(v)
    End Sub
End Module
    </file>
</compilation>)

            AssertNoDiagnostics(compilation)

            Dim mainTree = Aggregate t In compilation.SyntaxTrees Where t.FilePath = "a.vb" Into [Single]()
            Dim root = mainTree.GetRoot()
            Dim sm = compilation.GetSemanticModel(mainTree)

            Dim vSymbol = CType(sm.GetDeclaredSymbol(root.DescendantNodes().OfType(Of ModifiedIdentifierSyntax).Single()), ILocalSymbol)

            Dim writeLineCall = root.DescendantNodes().OfType(Of ExpressionStatementSyntax).First()
            Assert.Equal("WriteLine($""{(Function() v)()}"")", writeLineCall.ToString())

            Dim analysis = sm.AnalyzeDataFlow(writeLineCall)

            Assert.True(analysis.Succeeded)
            Assert.DoesNotContain(vSymbol, analysis.AlwaysAssigned)
            Assert.Contains(vSymbol, analysis.Captured)
            Assert.Contains(vSymbol, analysis.DataFlowsIn)
            Assert.DoesNotContain(vSymbol, analysis.DataFlowsOut)
            Assert.Contains(vSymbol, analysis.ReadInside)
            Assert.Contains(vSymbol, analysis.ReadOutside)
            Assert.DoesNotContain(vSymbol, analysis.UnsafeAddressTaken)
            Assert.DoesNotContain(vSymbol, analysis.VariablesDeclared)
            Assert.DoesNotContain(vSymbol, analysis.WrittenInside)
            Assert.Contains(vSymbol, analysis.WrittenOutside)

        End Sub

        <Fact>
        Public Sub Lowering_MissingFormattableStringDoesntProduceErrorIfFactoryMethodReturnsTypeConvertableToIFormattable()

            Dim verifier = CompileAndVerify(
<compilation>
    <file name="FormattableString.vb">
Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, ParamArray args As Object()) As IFormattable
            Return Nothing
        End Function

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Write(CObj(CType($"{Nothing}.", IFormattable)))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="")

        End Sub

        <Fact>
        Public Sub Lowering_CallsMostOptimalStringFormatOverload()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, arg As Object) As FormattableString
            Console.Write("1 arg")
            Return Nothing
        End Function

        Public Function Create(formatString As String, arg1 As Object, arg2 as Object) As FormattableString
            Console.Write("2 arg")
            Return Nothing
        End Function

        Public Function Create(formatString As String, arg1 As Object, arg2 as Object, arg3 As Object) As FormattableString
            Console.Write("3 arg")
            Return Nothing
        End Function

        Public Function Create(formatString As String, ParamArray args As Object()) As FormattableString
            Console.Write("ParamArray")
            Return Nothing
        End Function

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim empty As IFormattable = $""
        Dim literal As IFormattable = $"Text"
        Dim one As IFormattable = $"One interpolation {Date.Now.Hour}"
        Dim two As IFormattable = $"Two interpolations {Date.Now.Hour}:{Date.Now.Minute}"
        Dim three As IFormattable = $"Three interpolations {Date.Now.Hour}:{Date.Now.Minute}:{Date.Now.Second}"
        Dim four As IFormattable = $"Four interpolations {Date.Now.Hour}:{Date.Now.Minute}:{Date.Now.Second}.{Date.Now.Millisecond}"
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="ParamArrayParamArray1 arg2 arg3 argParamArray")

        End Sub

        <Fact>
        Public Sub Lowering_DoesNotCallFactoryMethodWithParamArrayInNormalForm()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, ParamArray args As Object()) As FormattableString
            Console.Write(If(args Is Nothing, "Null", args.Length))
            Return Nothing
        End Function

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim one As IFormattable = $"One interpolation {Nothing}"
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="1")

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateIsMissing()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Write(CType($"{1}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{1}.", IFormattable))
                    ~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateIsSub()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Sub Create(formatString As String, ParamArray args As Object())
        End Sub

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim obj As Object = Nothing
        Write(CType($"{obj}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateIsNotAMethod()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public ReadOnly Property Create(formatString As String, ParamArray args As Object()) As FormattableString
            Get
                Return New ConcreteFormattableString(formatString, args)
            End Get
        End Property
        
        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim obj As Object = Nothing
        Write(CType($"{obj}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateMethodIsShadowedByField()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Class FormattableStringFactoryBase

        Public Shared Function Create(formatString As String, ParamArray args As Object()) As FormattableString
            Return Nothing
        End Function
        
    End Class

    Public Class FormattableStringFactory
        Inherits FormattableStringFactoryBase
        
        Public Shadows Shared Create As Func(Of String, Object(), FormattableString) = Function(s, args) New ConcreteFormattableString(s, args)

        Protected NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Class

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim obj As Object = Nothing
        Write(CType($"{obj}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateIsInAccessible()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Private Function Create(formatString As String, ParamArray args As Object()) As FormattableString
            Return New ConcreteFormattableString(formatString, args)
        End Function
        
        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim obj As Object = Nothing
        Write(CType($"{obj}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC30390: 'FormattableStringFactory.Private Function Create(formatString As String, ParamArray args As Object()) As FormattableString' is not accessible in this context because it is 'Private'.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_CreateReturnIsNotConvertible()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, ParamArray args As Object()) As Object()
            Return {formatString, args}
        End Function

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim obj As Object = Nothing
        Write(CType($"{obj}.", IFormattable))
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC30311: Value of type 'Object()' cannot be converted to 'IFormattable'.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Write(CType($"{obj}.", IFormattable))
                    ~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub Lowering_ERR_InterpolatedStringFactoryError_ArgArrayIsNotConvertible()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="FormattableString.vb">
Namespace System

    Public MustInherit Class FormattableString
        Implements IFormattable

        Public NotOverridable Overrides Function ToString() As String
            Return ToString(Globalization.CultureInfo.CurrentCulture)
        End Function

        Public MustOverride Overloads Function ToString(formatProvider As IFormatProvider) As String

        Public Overloads Function ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString(formatProvider)
        End Function

        Public Shared Function Invariant(formattable As FormattableString) As String
            Return formattable.ToString(Globalization.CultureInfo.InvariantCulture)
        End Function
    End Class

End Namespace

Namespace System.Runtime.CompilerServices

    Public Module FormattableStringFactory

        Public Function Create(formatString As String, ParamArray args As Integer()) As FormattableString
            Return Nothing
        End Function

        Private NotInheritable Class ConcreteFormattableString
            Inherits FormattableString

            Private ReadOnly FormatString As String
            Private ReadOnly Arguments As Object()

            Public Sub New(formatString As String, arguments As Object())
                Me.FormatString = formatString
                Me.Arguments = arguments
            End Sub

            Public Overrides Function ToString(provider As IFormatProvider) As String
                Return String.Format(provider, FormatString, Arguments)
            End Function

        End Class

    End Module

End Namespace
    </file>
    <file name="a.vb">
Imports System
Imports System.Console

Module Program
    Sub Main()
        Dim s As IFormattable = $"{New String() {}}"
    End Sub
End Module
    </file>
</compilation>)

            AssertTheseEmitDiagnostics(compilation,
<expected>
BC37251: There were one or more errors emitting a call to FormattableStringFactory.Create. Method or its return type may be missing or malformed.
        Dim s As IFormattable = $"{New String() {}}"
                                ~~~~~~~~~~~~~~~~~~~~
BC30311: Value of type 'String()' cannot be converted to 'Integer'.
        Dim s As IFormattable = $"{New String() {}}"
                                   ~~~~~~~~~~~~~~~
</expected>)

        End Sub

    End Class

End Namespace