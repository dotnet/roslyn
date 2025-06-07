' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Basic.Reference.Assemblies
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class SemanticModelTests
        Inherits SemanticModelTestBase

#Region "LookupSymbols Function"

        <Fact()>
        Public Sub LookupSymbols1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System   

Class B
    Public f1 as Integer
End Class

Class D
    Inherits B

    Public Sub goo()
        Console.WriteLine() 'BIND:"WriteLine"
    End Sub
End Class

Module M
    Public f1 As Integer
    Public f2 As Integer
End Module

Module M2
    Public f1 As Integer
    Public f2 As Integer
End Module
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim pos As Integer = FindBindingTextPosition(compilation, "a.vb")

            Dim syms = semanticModel.LookupSymbols(pos, Nothing, "f1")
            Assert.Equal(1, syms.Length)
            Assert.Equal("B.f1 As System.Int32", syms(0).ToTestDisplayString())

            ' This one is tricky.. M.f2 and M2.f2 are ambiguous at the same
            ' binding scope, so we get both symbols here.
            syms = semanticModel.LookupSymbols(pos, Nothing, "f2")
            Assert.Equal(2, syms.Length)
            Dim fullNames = From s In syms.AsEnumerable Order By s.ToTestDisplayString() Select s.ToTestDisplayString()
            Assert.Equal("M.f2 As System.Int32", fullNames(0))
            Assert.Equal("M2.f2 As System.Int32", fullNames(1))

            syms = semanticModel.LookupSymbols(pos, Nothing, "int32")
            Assert.Equal(1, syms.Length)
            Assert.Equal("System.Int32", syms(0).ToTestDisplayString())

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub LookupSymbols2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System   

    Class B
    End Class
    Class A
        Class B(Of X)
        End Class
        Class B(Of X, Y)
        End Class
        Sub M()
            Dim B As Integer
            Console.WriteLine() 
        End Sub
    End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim posInside As Integer = CompilationUtils.FindPositionFromText(tree, "WriteLine")
            Dim posOutside As Integer = CompilationUtils.FindPositionFromText(tree, "Sub M")

            ' Inside the method, "B" shadows classes of any arity.
            Dim syms = semanticModel.LookupSymbols(posInside, Nothing, "b", Nothing)
            Assert.Equal(1, syms.Length)
            Assert.Equal("B As System.Int32", syms(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, syms(0).Kind)

            ' Outside the method, all B's are available.
            syms = semanticModel.LookupSymbols(posOutside, Nothing, "b", Nothing)
            Assert.Equal(3, syms.Length)
            Dim fullNames = syms.Select(Function(x) x.ToTestDisplayString()).OrderBy(StringComparer.Ordinal).ToArray()
            Assert.Equal("A.B(Of X)", fullNames(0))
            Assert.Equal("A.B(Of X, Y)", fullNames(1))
            Assert.Equal("B", fullNames(2))

            ' Inside the method, all B's are available if only types/namespace are allowed
            syms = semanticModel.LookupNamespacesAndTypes(posOutside, Nothing, "b")
            Assert.Equal(3, syms.Length)
            fullNames = syms.Select(Function(x) x.ToTestDisplayString()).OrderBy(StringComparer.Ordinal).ToArray()
            Assert.Equal("A.B(Of X)", fullNames(0))
            Assert.Equal("A.B(Of X, Y)", fullNames(1))
            Assert.Equal("B", fullNames(2))

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact()>
        Public Sub LookupSymbols3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports AliasZ = B.Z
    Class A
        Public Class Z(Of X)
        End Class
        Public Class Z(Of X, Y)
        End Class
    End Class
    Class B
        Inherits A
        Public Class Z ' in B
        End Class
        Public Class Z(Of X)
        End Class
    End Class
    Class C
        Inherits B
        Public z As Integer ' in C
    End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim posInsideB As Integer = CompilationUtils.FindPositionFromText(tree, "in B")
            Dim posInsideC As Integer = CompilationUtils.FindPositionFromText(tree, "in C")

            ' Lookup Z, all arities, inside B
            Dim syms = semanticModel.LookupSymbols(posInsideB, name:="z")
            ' Assert.Equal(3, syms.Count)
            Dim fullNames = From s In syms.AsEnumerable Order By s.ToTestDisplayString() Select s.ToTestDisplayString()
            Assert.Equal("A.Z(Of X, Y)", fullNames(0))
            Assert.Equal("B.Z", fullNames(1))
            Assert.Equal("B.Z(Of X)", fullNames(2))

            ' Lookup Z, all arities, inside C. Since fields shadow by name in VB, only the field is found.
            syms = semanticModel.LookupSymbols(posInsideC, name:="z")
            Assert.Equal(1, syms.Length)
            Assert.Equal("C.z As System.Int32", syms(0).ToTestDisplayString())

            ' Lookup AliasZ, all arities, inside C
            syms = semanticModel.LookupSymbols(posInsideC, name:="aliasz")
            Assert.Equal(1, syms.Length)
            Assert.Equal("AliasZ=B.Z", syms(0).ToTestDisplayString())

            ' Lookup AliasZ, all arities, inside C with container
            syms = semanticModel.LookupSymbols(posInsideC, name:="C")
            Assert.Equal(1, syms.Length)
            Assert.Equal("C", syms(0).ToTestDisplayString())

            Dim C = DirectCast(syms.Single, NamespaceOrTypeSymbol)
            syms = semanticModel.LookupSymbols(posInsideC, name:="aliasz", container:=C)
            Assert.Equal(0, syms.Length)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact()>
        Public Sub LookupSymbols4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Namespace N1.N2        
    Class A
        Public X As Integer
    End Class
    Class B
        Inherits A
        Public Y As Integer
    End Class
    Class C
        Inherits B
        Public Z As Integer
        Public Sub M()
            System.Console.WriteLine() ' in M
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim posInsideM As Integer = CompilationUtils.FindPositionFromText(tree, "in M")

            ' Lookup all symbols
            Dim syms = From s In semanticModel.LookupSymbols(posInsideM, name:=Nothing)
            Dim fullNames = From s In syms Order By s.ToTestDisplayString() Select s.ToTestDisplayString()
            Assert.Contains("N1.N2.A", fullNames)
            Assert.Contains("N1.N2.B", fullNames)
            Assert.Contains("N1.N2.C", fullNames)
            Assert.Contains("N1.N2.C.Z As System.Int32", fullNames)
            Assert.Contains("Sub N1.N2.C.M()", fullNames)
            Assert.Contains("N1", fullNames)
            Assert.Contains("N1.N2", fullNames)
            Assert.Contains("System", fullNames)
            Assert.Contains("Microsoft", fullNames)
            Assert.Contains("Function System.Object.ToString() As System.String", fullNames)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact()>
        Public Sub LookupSymbolsMustNotBeInstance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class A
    Public X As Integer
    Public Shared SX As Integer
End Class
Class B
    Inherits A
    Public Function Y() As Integer
    End Function
    Public Shared Function SY() As Integer
    End Function
End Class
Class C
    Inherits B
    Public Z As Integer ' in C
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim classC As NamedTypeSymbol = DirectCast(compilation.GlobalNamespace().GetMembers("C").Single(), NamedTypeSymbol)
            Dim posInsideC As Integer = CompilationUtils.FindPositionFromText(tree, "in C")

            Dim symbols = semanticModel.LookupStaticMembers(position:=posInsideC, container:=classC, name:=Nothing)
            Dim fullNames = From s In symbols.AsEnumerable Order By s.ToTestDisplayString() Select s.ToTestDisplayString()

            Assert.Equal(4, symbols.Length)
            Assert.Equal("A.SX As System.Int32", fullNames(0))
            Assert.Equal("Function B.SY() As System.Int32", fullNames(1))
            Assert.Equal("Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean", fullNames(2))
            Assert.Equal("Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean", fullNames(3))

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub LookupSymbolsInTypeParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Interface IA
    Sub MA()
End Interface
Interface IB
    Sub MB()
End Interface
Interface IC
    Sub M(Of T, U)()
End Interface
Class C
    Implements IA
    Private Sub MA() Implements IA.MA
    End Sub
    Public Sub M(Of T)()
    End Sub
End Class
Class D(Of T As IB)
    Public Sub MD(Of U As {C, T, IC}, V As Structure)(_u As U, _v As V)
    End Sub
End Class
Module E
    <Extension()>
    Friend Sub [ME](c As IC, o As Object)
    End Sub
    <Extension()>
    Friend Sub [ME](v As System.ValueType)
    End Sub
End Module
]]></file>
</compilation>, {Net40.References.SystemCore})

            Dim tree = compilation.SyntaxTrees.Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim method = compilation.GlobalNamespace().GetMember(Of NamedTypeSymbol)("D").GetMember(Of MethodSymbol)("MD")
            Dim parameter = method.Parameters(0)
            Dim position = CompilationUtils.FindPositionFromText(tree, "As U")
            Dim symbols = semanticModel.LookupSymbols(position, container:=parameter.Type, includeReducedExtensionMethods:=True)
            CheckSymbols(symbols,
                         "Sub C.M(Of T)()",
                         "Function Object.ToString() As String",
                         "Function Object.Equals(obj As Object) As Boolean",
                         "Function Object.Equals(objA As Object, objB As Object) As Boolean",
                         "Function Object.ReferenceEquals(objA As Object, objB As Object) As Boolean",
                         "Function Object.GetHashCode() As Integer",
                         "Function Object.GetType() As Type",
                         "Sub IB.MB()",
                         "Sub IC.ME(o As Object)")

            parameter = method.Parameters(1)
            position = CompilationUtils.FindPositionFromText(tree, "As V")
            symbols = semanticModel.LookupSymbols(position, container:=parameter.Type, includeReducedExtensionMethods:=True)
            CheckSymbols(symbols,
                         "Function ValueType.Equals(obj As Object) As Boolean",
                         "Function Object.Equals(obj As Object) As Boolean",
                         "Function Object.Equals(objA As Object, objB As Object) As Boolean",
                         "Function ValueType.GetHashCode() As Integer",
                         "Function Object.GetHashCode() As Integer",
                         "Function ValueType.ToString() As String",
                         "Function Object.ToString() As String",
                         "Function Object.ReferenceEquals(objA As Object, objB As Object) As Boolean",
                         "Function Object.GetType() As Type",
                         "Sub ValueType.ME()")

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        Private Shared Sub CheckSymbols(symbols As ImmutableArray(Of ISymbol), ParamArray descriptions As String())
            CompilationUtils.CheckSymbols(symbols, descriptions)
        End Sub

        <Fact()>
        Public Sub LookupNames1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Namespace N1.N2        
    Class A
        Public X As Integer
    End Class
    Class B
        Inherits A
        Public Y As Integer
    End Class
    Class C
        Inherits B
        Public Z As Integer
        Public Sub M()
            System.Console.WriteLine() ' in M
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim posInsideM As Integer = CompilationUtils.FindPositionFromText(tree, "in M")

            ' Lookup names
            Dim names As IEnumerable(Of String) = From n In semanticModel.LookupNames(posInsideM) Order By n
            Assert.Contains("A", names)
            Assert.Contains("B", names)
            Assert.Contains("C", names)
            Assert.Contains("Z", names)
            Assert.Contains("M", names)
            Assert.Contains("N1", names)
            Assert.Contains("N2", names)
            Assert.Contains("System", names)
            Assert.Contains("Microsoft", names)
            Assert.Contains("ToString", names)
            Assert.Contains("Equals", names)
            Assert.Contains("GetType", names)

            ' Lookup names, namespace and types only
            names = From n In semanticModel.LookupNames(posInsideM, namespacesAndTypesOnly:=True) Order By n
            Assert.Contains("A", names)
            Assert.Contains("B", names)
            Assert.Contains("C", names)
            Assert.DoesNotContain("Z", names)
            Assert.DoesNotContain("M", names)
            Assert.Contains("N1", names)
            Assert.Contains("N2", names)
            Assert.Contains("System", names)
            Assert.Contains("Microsoft", names)
            Assert.DoesNotContain("ToString", names)
            Assert.DoesNotContain("Equals", names)
            Assert.DoesNotContain("GetType", names)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact()>
        Public Sub LookupNames2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Class A
        Inherits B
        Public X As Integer 
    End Class
    Class B
        Inherits A
        Public Y As Integer ' in B
    End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim posInsideB As Integer = CompilationUtils.FindPositionFromText(tree, "in B")

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30257: Class 'A' cannot inherit from itself: 
    'A' inherits from 'B'.
    'B' inherits from 'A'.
        Inherits B
                 ~
</expected>)
            ' Lookup names
            Dim names As IEnumerable(Of String) = From n In semanticModel.LookupNames(posInsideB) Order By n
            Assert.Contains("A", names)
            Assert.Contains("B", names)
            Assert.Contains("X", names)
            Assert.Contains("Y", names)
        End Sub

        <Fact()>
        Public Sub ObjectMembersOnInterfaces1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ObjectMembersOnInterfaces">
    <file name="a.vb">

Module Module1

    Sub Main()
        System.Console.WriteLine(System.IDisposable.Equals(1, 1))

        Dim x As I1 = New C1()
        System.Console.WriteLine(x.GetHashCode()) 'BIND:"GetHashCode"
    End Sub

End Module

Interface I1
End Interface

Class C1
    Implements I1

    Public Overrides Function GetHashCode() As Integer
        Return 1234
    End Function
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
True
1234
]]>)

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("I1"), name:="GetHashCode")

            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Function System.Object.GetHashCode() As System.Int32", actual_lookupSymbols(0).ToTestDisplayString())

            Dim getHashCode = DirectCast(actual_lookupSymbols(0), MethodSymbol)

            Assert.Contains(getHashCode, GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("I1")))
        End Sub

        <Fact()>
        Public Sub ObjectMembersOnInterfaces2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ObjectMembersOnInterfaces">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As I1 = Nothing
        x.GetHashCode()
        x.GetHashCode(1)
        x.ToString()
        x.ToString(1)
        x.ToString(1, 2)

        Dim y As I5 = Nothing
        y.GetHashCode()
    End Sub

    &lt;Extension()&gt;
    Function ToString(this As I1, x As Integer, y As Integer) As String
        Return Nothing
    End Function
End Module

Interface I1
    Function GetHashCode(x As Integer) As Integer
    Function ToString(x As Integer) As Integer
End Interface

Interface I3
    Function GetHashCode(x As Integer) As Integer
End Interface

Interface I4
    Function GetHashCode() As Integer
End Interface

Interface I5
    Inherits I3, I4
End Interface

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<result>
BC30455: Argument not specified for parameter 'x' of 'Function GetHashCode(x As Integer) As Integer'.
        x.GetHashCode()
          ~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'ToString' accepts this number of arguments.
        x.ToString()
          ~~~~~~~~
</result>
            )

        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMe()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMember">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      Me.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub System.Object.Finalize()",
  "Function System.Object.MemberwiseClone() As System.Object",
  "Sub C1..ctor(x As System.Int32, y As System.Int32)",
  "Sub C1..ctor(x As System.String, y As System.Int32)"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMyClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMyClass">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      MyClass.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub System.Object.Finalize()",
  "Function System.Object.MemberwiseClone() As System.Object",
  "Sub C1..ctor(x As System.Int32, y As System.Int32)",
  "Sub C1..ctor(x As System.String, y As System.Int32)"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMeNotInConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMeNotInConstructor">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
    Public Sub q()
      Me.New() 'BIND:"New"
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub System.Object.Finalize()",
  "Function System.Object.MemberwiseClone() As System.Object",
  "Sub C1.q()"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMyClassNotInConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMyClassNotInConstructor">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
    Public Sub q()
      MyClass.New() 'BIND:"New"
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub System.Object.Finalize()",
  "Function System.Object.MemberwiseClone() As System.Object",
  "Sub C1.q()"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMeNoInstance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMember">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      Me.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing, mustBeStatic:=True)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMyClassNoInstance()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMyClassNoInstance">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1  
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      MyClass.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("C1"), name:=Nothing, mustBeStatic:=True)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfConstructorMemberOnMyBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMyBase">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      MyBase.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
    Public Sub C()
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub B1..ctor(x As System.Int32)"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=compilation.GetTypeByMetadataName("B1"), name:=Nothing)
            Dim actual_lookupSymbols_strings = actual_lookupSymbols.Select(Function(s) s.ToTestDisplayString()).ToList()
            For Each s In expected_in_lookupSymbols
                Assert.Contains(s, actual_lookupSymbols_strings)
            Next
            Assert.Equal(expected_in_lookupSymbols.Count, actual_lookupSymbols_strings.Count)
        End Sub

        <Fact()>
        Public Sub LookupOfInaccessibleOnMyBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LookupOfConstructorMemberOnMyBase">
    <file name="a.vb">

Class B1
    Public Sub New(x As Integer)
    End Sub
    Private Sub New(x as String)
    End Sub
End Class
Class C1
    Inherits B1
    Public Sub New(x As Integer, y as Integer)
      MyBase.New() 'BIND:"New"
    End Sub
    Private Sub New(x as String, y as Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupSymbols = {
  "Function System.Object.ToString() As System.String",
  "Function System.Object.Equals(obj As System.Object) As System.Boolean",
  "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
  "Function System.Object.GetHashCode() As System.Int32",
  "Function System.Object.GetType() As System.Type",
  "Sub System.Object.Finalize()",
  "Function System.Object.MemberwiseClone() As System.Object",
  "Sub B1..ctor(x As System.Int32)",
  "Sub B1..ctor(x As System.String)"
                }

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim position = tree.ToString().IndexOf("MyBase", StringComparison.Ordinal)
            Dim binder = DirectCast(model, VBSemanticModel).GetEnclosingBinder(position)

            Dim baseType = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B1")

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            Dim result = LookupResult.GetInstance()
            binder.LookupMember(result, baseType, "Finalize", 0, LookupOptions.IgnoreAccessibility, useSiteDiagnostics)
            Assert.Null(useSiteDiagnostics)
            Assert.True(result.IsGood)
            Assert.Equal("Sub System.Object.Finalize()", result.SingleSymbol.ToTestDisplayString())

            result.Clear()
            binder.LookupMember(result, baseType, "MemberwiseClone", 0, LookupOptions.IgnoreAccessibility, useSiteDiagnostics)
            Assert.Null(useSiteDiagnostics)
            Assert.True(result.IsGood)
            Assert.Equal("Function System.Object.MemberwiseClone() As System.Object", result.SingleSymbol.ToTestDisplayString())

            result.Free()
        End Sub

#End Region

#Region "Regression"

        <WorkItem(539107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539107")>
        <Fact()>
        Public Sub LookupAtLocationEndSubNode()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test
    Public Sub Method1(ByVal param1 As Integer)
        Dim count As Integer = 45
    End Sub 'BIND:"End Sub"
End Class
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "count",
                    "param1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "count As System.Int32",
                    "param1 As System.Int32"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539114")>
        <Fact()>
        Public Sub LookupAtLocationSubBlockNode()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test
    Public Sub Method1(ByVal param1 As Integer) 'BIND:"Public Sub Method1(ByVal param1 As Integer)"
        Dim count As Integer = 45
    End Sub 
End Class
    </file>
</compilation>)

            Dim not_expected_in_lookupNames =
                {
                    "count",
                    "param1"
                }

            Dim not_expected_in_lookupSymbols =
                {
                    "count As System.Int32",
                    "param1 As System.Int32"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.DoesNotContain(not_expected_in_lookupNames(0), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(1), actual_lookupNames)

            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539119")>
        <Fact()>
        Public Sub LookupSymbolsByNameIncorrectArity()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test
    Public Sub Method1(ByVal param1 As Integer) 
        Dim count As Integer = 45 'BIND:"45"
    End Sub 
End Class
    </file>
</compilation>)

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="count", arity:=1)

            Assert.Empty(actual_lookupSymbols)
        End Sub

        <WorkItem(539130, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539130")>
        <Fact()>
        Public Sub LookupWithNameZeroArity()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Module1
    Sub Method1(Of T)(i As T)

    End Sub

    Sub Method1(Of T, U)(i As T, j As U)

    End Sub

    Sub Method1(i As Integer)

    End Sub

    Sub Method1(i As Integer, j As Integer)

    End Sub

    Public Sub Main()
        Dim x As Integer = 45 'BIND:"45"
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupSymbols =
                {
                    "Sub Module1.Method1(i As System.Int32)",
                    "Sub Module1.Method1(i As System.Int32, j As System.Int32)"
                }

            Dim not_expected_in_lookupSymbols =
                {
                    "Sub Module1.Method1(Of T)(i As T)",
                    "Sub Module1.Method1(Of T, U)(i As T, j As U)"
                }

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Method1", arity:=0)
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)

            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(5004, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub LookupExcludeInAppropriateNS()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Module1
End Module
    </file>
</compilation>, {SystemDataRef})

            Dim not_expected_in_lookup =
                {
                    "<CrtImplementationDetails>",
                    "<CppImplementationDetails>"
                }

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Const position = 0
            Dim binder = DirectCast(model, VBSemanticModel).GetEnclosingBinder(position)

            Dim actual_lookupSymbols = model.LookupSymbols(position)
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.DoesNotContain(not_expected_in_lookup(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookup(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539166")>
        <Fact()>
        Public Sub LookupLocationInsideFunctionIgnoreReturnVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test
    Function Func1() As Integer
        Dim x As Integer = "10"
        Return 10
    End Function
End Class        
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim position = tree.ToString().IndexOf("Dim", StringComparison.Ordinal)
            Dim binder = DirectCast(model, VBSemanticModel).GetEnclosingBinder(position)

            Dim result = LookupResult.GetInstance()
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            binder.Lookup(result, "Func1", arity:=0, options:=LookupOptions.MustNotBeReturnValueVariable, useSiteDiagnostics:=useSiteDiagnostics)
            Assert.Null(useSiteDiagnostics)
            Assert.True(result.IsGood)
            Assert.Equal("Function Test.Func1() As System.Int32", result.SingleSymbol.ToTestDisplayString())

            result.Clear()
            binder.Lookup(result, "x", arity:=0, options:=LookupOptions.MustNotBeReturnValueVariable, useSiteDiagnostics:=useSiteDiagnostics)
            Assert.Null(useSiteDiagnostics)
            Assert.True(result.IsGood)
            Assert.Equal("x As System.Int32", result.SingleSymbol.ToTestDisplayString())

            result.Free()
        End Sub

        <WorkItem(539166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539166")>
        <Fact()>
        Public Sub LookupLocationInsideFunction()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test
    Function Func1() As Integer
        Dim x As Integer = "10" 'BIND:"10"
        Return 10
    End Function
End Class        
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "Func1",
                    "x"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Func1 As System.Int32",
                    "x As System.Int32"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527759")>
        <Fact()>
        Public Sub LookupAtLocationClassTypeBlockSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test 'BIND:"Class Test"
    Public Sub PublicMethod()
    End Sub

    Protected Sub ProtectedMethod()
    End Sub

    Private Sub PrivateMethod()
    End Sub
End Class      
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "PublicMethod",
                    "ProtectedMethod",
                    "PrivateMethod"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Sub Test.PublicMethod()",
                    "Sub Test.ProtectedMethod()",
                    "Sub Test.PrivateMethod()"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(2), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527760")>
        <Fact()>
        Public Sub LookupAtLocationClassTypeStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test 'BIND:"Class Test"
    Public Sub PublicMethod()
    End Sub

    Protected Sub ProtectedMethod()
    End Sub

    Private Sub PrivateMethod()
    End Sub
End Class      
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "PublicMethod",
                    "ProtectedMethod",
                    "PrivateMethod"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Sub Test.PublicMethod()",
                    "Sub Test.ProtectedMethod()",
                    "Sub Test.PrivateMethod()"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(2), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527761")>
        <Fact()>
        Public Sub LookupAtLocationEndClassStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Class Test 
    Public Sub PublicMethod()
    End Sub

    Protected Sub ProtectedMethod()
    End Sub

    Private Sub PrivateMethod()
    End Sub
End Class      'BIND:"End Class"
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "PublicMethod",
                    "ProtectedMethod",
                    "PrivateMethod"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Sub Test.PublicMethod()",
                    "Sub Test.ProtectedMethod()",
                    "Sub Test.PrivateMethod()"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(2), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539175")>
        <Fact()>
        Public Sub LookupAtLocationNamespaceBlockSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Namespace NS1 'BIND:"Namespace NS1"
    Namespace NS2
        Class T1
        End Class
    End Namespace

    Class T2
    End Class

    Friend Class T3
    End Class
End Namespace

Namespace NS3
End Namespace
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "NS1",
                    "NS3"
                }
            Dim not_expected_in_lookupNames =
                {
                    "NS2",
                    "T1",
                    "T2",
                    "T3"
                }

            Dim expected_in_lookupSymbols =
                {
                    "NS1",
                    "NS3"
                }
            Dim not_expected_in_lookupSymbols =
                {
                    "NS1.NS2",
                    "NS1.NS2.T1",
                    "NS1.T2",
                    "NS1.T3"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(0), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(1), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(2), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(3), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(3), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539177, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539177")>
        <Fact()>
        Public Sub LookupAtLocationEndNamespaceStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Namespace NS1 
    Namespace NS2
        Class T1
        End Class
    End Namespace

    Class T2
    End Class

    Friend Class T3
    End Class
End Namespace 'BIND:"End Namespace"

Namespace NS3
End Namespace
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "NS1",
                    "NS2",
                    "NS3",
                    "T2",
                    "T3"
                }
            Dim not_expected_in_lookupNames =
                {
                    "T1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "NS1",
                    "NS3",
                    "NS1.NS2",
                    "NS1.T2",
                    "NS1.T3"
                }
            Dim not_expected_in_lookupSymbols =
                {
                    "NS1.NS2.T1"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            For Each expectedName In expected_in_lookupNames
                Assert.Contains(expectedName, actual_lookupNames)
            Next

            For Each notExpectedName In not_expected_in_lookupNames
                Assert.DoesNotContain(notExpectedName, actual_lookupNames)
            Next

            For Each expectedName In expected_in_lookupSymbols
                Assert.Contains(expectedName, actual_lookupSymbols_as_string)
            Next

            For Each notExpectedName In not_expected_in_lookupSymbols
                Assert.DoesNotContain(notExpectedName, actual_lookupSymbols_as_string)
            Next
        End Sub

        <WorkItem(539185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539185")>
        <Fact()>
        Public Sub LookupAtLocationInterfaceBlockSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
    Interface Interface1 'BIND:"Interface Interface1"
        Sub sub1(ByVal i As Integer)
    End Interface
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "Interface1",
                    "sub1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Interface1",
                    "Sub Interface1.sub1(i As System.Int32)"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539185")>
        <Fact>
        Public Sub LookupAtLocationInterfaceStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
    Interface Interface1 'BIND:"Interface Interface1"
        Sub sub1(ByVal i As Integer)
    End Interface
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "Interface1",
                    "sub1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Interface1",
                    "Sub Interface1.sub1(i As System.Int32)"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539185")>
        <Fact>
        Public Sub LookupAtLocationEndInterfaceStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
    Interface Interface1
        Sub sub1(ByVal i As Integer)
    End Interface  'BIND:"End Interface"
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "Interface1",
                    "sub1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Interface1",
                    "Sub Interface1.sub1(i As System.Int32)"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527774")>
        <Fact()>
        Public Sub LookupAtLocationCompilationUnitSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
        Imports A1 = System 'BIND:"Imports A1 = System"
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "System",
                    "Microsoft"
                }

            Dim expected_in_lookupSymbols =
                {
                    "System",
                    "Microsoft"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Equal(2, actual_lookupNames.Count)
            Assert.Equal(2, actual_lookupSymbols_as_string.Count)

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527779")>
        <Fact()>
        Public Sub LookupAtLocationInheritsStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Public Class C1
    Public Sub C1Sub1()
    End Sub
End Class

Public Class C2
    Inherits C1 'BIND:"Inherits C1"
    Public Sub C2Sub1() 
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "C1",
                    "C2",
                    "C2Sub1"
                }
            Dim not_expected_in_lookupNames =
                {
                    "C1Sub1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "C1",
                    "C2",
                    "Sub C2.C2Sub1()"
                }
            Dim not_expected_in_lookupSymbols =
                {
                    "Sub C1.C1Sub1()"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(2), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(527780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527780")>
        <Fact()>
        Public Sub LookupAtLocationImplementsStatementSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Interface Interface1
    Sub Sub1()
End Interface

Public Class ImplementationClass1
    Implements Interface1 'BIND:"Implements Interface1"
    Sub Sub1() Implements Interface1.Sub1
    End Sub

    Sub Sub2()
    End Sub
End Class
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "Interface1",
                    "ImplementationClass1",
                    "Sub1",
                    "Sub2"
                }

            Dim expected_in_lookupSymbols =
                {
                    "Interface1",
                    "ImplementationClass1",
                    "Sub ImplementationClass1.Sub1()",
                    "Sub ImplementationClass1.Sub2()"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(1), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(2), actual_lookupNames)
            Assert.Contains(expected_in_lookupNames(3), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(1), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(2), actual_lookupSymbols_as_string)
            Assert.Contains(expected_in_lookupSymbols(3), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539232, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539232")>
        <Fact()>
        Public Sub LookupAtLocationInsideIfPartOfSingleLineIfStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main()
        If True Then Dim me1 As Integer = 10 Else Dim me2 As Integer = 20 'BIND:"Dim me1 As Integer = 10"
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "me1"
                }
            Dim not_expected_in_lookupNames =
                {
                    "me2"
                }

            Dim expected_in_lookupSymbols =
                {
                    "me1 As System.Int32"
                }
            Dim not_expected_in_lookupSymbols =
                {
                    "me2 As System.Int32"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(539234, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539234")>
        <Fact()>
        Public Sub LookupAtLocationInsideElsePartOfSingleLineElseStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main()
        If True Then Dim me1 As Integer = 10 Else Dim me2 As Integer = 20 'BIND:"Dim me2 As Integer = 20"
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "me2"
                }
            Dim not_expected_in_lookupNames =
                {
                    "me1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "me2 As System.Int32"
                }
            Dim not_expected_in_lookupSymbols =
                {
                    "me1 As System.Int32"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)
            Assert.DoesNotContain(not_expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
            Assert.DoesNotContain(not_expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <WorkItem(542856, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542856")>
        <Fact()>
        Public Sub LookupParamSingleLineLambdaExpr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1)  x4  'BIND:" x4"
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "x1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "x1 As System.Object"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <Fact()>
        Public Sub Bug10272_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x5 = Sub(x1)  x4  'BIND:" x4"
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "x1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "x1 As System.Object"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <Fact(), WorkItem(546400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546400")>
        Public Sub Bug10272_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x5 = Function(x1)  
                    x4  'BIND:" x4"
                 End Function
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "x1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "x1 As System.Object"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <Fact(), WorkItem(546400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546400")>
        Public Sub Bug10272_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x5 = Sub(x1)  
                    x4  'BIND:" x4"
                 End Sub
    End Sub
End Module
    </file>
</compilation>)

            Dim expected_in_lookupNames =
                {
                    "x1"
                }

            Dim expected_in_lookupSymbols =
                {
                    "x1 As System.Object"
                }

            Dim actual_lookupNames = GetLookupNames(compilation, "a.vb")

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")
            Dim actual_lookupSymbols_as_string = actual_lookupSymbols.Select(Function(e) e.ToTestDisplayString())

            Assert.Contains(expected_in_lookupNames(0), actual_lookupNames)

            Assert.Contains(expected_in_lookupSymbols(0), actual_lookupSymbols_as_string)
        End Sub

        <Fact()>
        Public Sub LookupSymbolsAtEOF()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
End Class
    </file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim eof = tree.GetCompilationUnitRoot().FullSpan.End
            Assert.NotEqual(eof, 0)
            Dim symbols = model.LookupSymbols(eof)
            CompilationUtils.CheckSymbols(symbols, "Microsoft", "C", "System")
        End Sub

        <Fact(), WorkItem(939844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939844")>
        Public Sub Bug939844_01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x = 1
        If True Then
            Dim y = 1
            x=y 'BIND:"y"
                                       
        End If
    End Sub
End Module
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim pos As Integer = FindBindingTextPosition(compilation, "a.vb") + 20

            Dim symsX = semanticModel.LookupSymbols(pos, Nothing, "x")
            Assert.Equal(1, symsX.Length)
            Assert.Equal("x As System.Int32", symsX(0).ToTestDisplayString())

            Dim symsY = semanticModel.LookupSymbols(pos, Nothing, "y")
            Assert.Equal(1, symsY.Length)
            Assert.Equal("y As System.Int32", symsY(0).ToTestDisplayString())
        End Sub

        <Fact(), WorkItem(939844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939844")>
        Public Sub Bug939844_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x = 1
        Select Case 1
            Case 1
            Dim y = 1
            x=y 'BIND:"y"
                                       
        End Select
    End Sub
End Module
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim pos As Integer = FindBindingTextPosition(compilation, "a.vb") + 20

            Dim symsX = semanticModel.LookupSymbols(pos, Nothing, "x")
            Assert.Equal(1, symsX.Length)
            Assert.Equal("x As System.Int32", symsX(0).ToTestDisplayString())

            Dim symsY = semanticModel.LookupSymbols(pos, Nothing, "y")
            Assert.Equal(1, symsY.Length)
            Assert.Equal("y As System.Int32", symsY(0).ToTestDisplayString())
        End Sub

#End Region

    End Class

End Namespace
