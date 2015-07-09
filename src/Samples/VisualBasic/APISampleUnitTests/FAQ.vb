' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace APISampleUnitTestsVB

    <TestClass>
    Public Class FAQ

        <AttributeUsage(AttributeTargets.Method)>
        Private Class FAQAttribute
            Inherits Attribute

            Private ReadOnly _Id As Integer

            Public ReadOnly Property Id As Integer
                Get
                    Return _Id
                End Get
            End Property

            Public Sub New(id As Integer)
                Me._Id = id
            End Sub
        End Class


        Private _Mscorlib As MetadataReference

        Public ReadOnly Property Mscorlib As MetadataReference
            Get
                If _Mscorlib Is Nothing Then
                    _Mscorlib = MetadataReference.CreateFromFile(GetType(Object).Assembly.Location)
                End If
                Return _Mscorlib
            End Get
        End Property

#Region " Section 1 : Getting Information Questions "

        <FAQ(1)>
        <TestMethod>
        Public Sub GetTypeForTypeName()

            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Public Sub Main()

        Dim i As Integer = 0 
        i += 1

    End Sub
End Module
</text>.Value)
            Dim vbRuntime = MetadataReference.CreateFromFile(GetType(CompilerServices.StandardModuleAttribute).Assembly.Location)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib, vbRuntime})
            Dim model = comp.GetSemanticModel(tree)

            ' Get TypeSyntax corresponding to the keyword 'Integer' above.
            Dim typeName =
                Aggregate
                    node In tree.GetRoot().DescendantNodes.OfType(Of TypeSyntax)()
                Where
                    node.ToString() = "Integer"
                Into [Single]()

            ' Use GetTypeInfo() to get TypeSymbol corresponding to the keyword 'Integer' above.
            Dim type = CType(model.GetTypeInfo(typeName).Type, ITypeSymbol)

            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType)
            Assert.AreEqual("Integer", type.ToDisplayString())

            ' Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to keyword 'Integer' above.
            type = CType(model.GetSymbolInfo(typeName).Symbol, ITypeSymbol)

            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType)
            Assert.AreEqual("Integer", type.ToDisplayString())
        End Sub

        <FAQ(2)>
        <TestMethod>
        Public Sub GetTypeForVariableDeclaration()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Public Sub Main()

        Dim i = 0 : i += 1
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)

            ' Get ModifiedIdentifierSyntax corresponding to the identifier 'i' in the statement 'Dim i = ...' above.
            Dim identifier As ModifiedIdentifierSyntax = tree.GetRoot().
                                                              DescendantNodes.
                                                              OfType(Of VariableDeclaratorSyntax).
                                                              Single.
                                                              Names.
                                                              Single

            ' Get TypeSymbol corresponding to 'Dim i' above.
            Dim type = CType(model.GetDeclaredSymbol(identifier), ILocalSymbol).Type

            Assert.AreEqual(SpecialType.System_Int32, type.SpecialType)
            Assert.AreEqual("Integer", type.ToDisplayString())
        End Sub

        <FAQ(3)>
        <TestMethod>
        Public Sub GetTypeForExpressions()
            Dim source =
<text>
Imports System

Module Program

    Public Sub M(s As Short())
        Dim d = 1.0
        Console.WriteLine(s(0) + d)
    End Sub

    Public Sub Main()
    End Sub
End Module
</text>.Value

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)

            Dim _projectId = ProjectId.CreateNewId()
            Dim _documentId = DocumentId.CreateNewId(_projectId)

            Dim sln = New AdhocWorkspace().CurrentSolution.
                          AddProject(_projectId, "MyProject", "MyProject", LanguageNames.VisualBasic).WithProjectCompilationOptions(_projectId, vbOptions).
                              AddMetadataReference(_projectId, Mscorlib).
                              AddDocument(_documentId, "MyFile.vb", source)
            Dim document = sln.GetDocument(_documentId)
            Dim model = CType(document.GetSemanticModelAsync().Result, SemanticModel)

            ' Get BinaryExpressionSyntax corresponding to the expression 's(0) + d' above.
            Dim addExpression As BinaryExpressionSyntax = document.GetSyntaxRootAsync().Result.
                                                                   DescendantNodes.
                                                                   OfType(Of BinaryExpressionSyntax).
                                                                   Single

            ' Get TypeSymbol corresponding to expression 's(0) + d' above.
            Dim expressionTypeInfo As TypeInfo = model.GetTypeInfo(addExpression)
            Dim expressionType = expressionTypeInfo.Type

            Assert.AreEqual(SpecialType.System_Double, expressionType.SpecialType)
            Assert.AreEqual("Double", expressionType.ToDisplayString())
            Assert.AreEqual(SpecialType.System_Double, expressionTypeInfo.ConvertedType.SpecialType)
            Assert.IsTrue(model.GetConversion(addExpression).IsIdentity)

            ' Get IdentifierNameSyntax corresponding to the variable 'd' in expression 's(0) + d' above.
            Dim identifier = CType(addExpression.Right, IdentifierNameSyntax)

            ' Use GetTypeInfo() to get TypeSymbol corresponding to variable 'd' above.
            Dim variableTypeInfo As TypeInfo = model.GetTypeInfo(identifier)
            Dim variableType = variableTypeInfo.Type

            Assert.AreEqual(SpecialType.System_Double, variableType.SpecialType)
            Assert.AreEqual("Double", variableType.ToDisplayString())
            Assert.AreEqual(SpecialType.System_Double, variableTypeInfo.ConvertedType.SpecialType)
            Assert.IsTrue(model.GetConversion(identifier).IsIdentity)

            ' Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to variable 'd' above.
            variableType = (CType(model.GetSymbolInfo(identifier).Symbol, ILocalSymbol)).Type

            Assert.AreEqual(SpecialType.System_Double, variableType.SpecialType)
            Assert.AreEqual("Double", variableType.ToDisplayString())

            ' Get InvocationExpressionSyntax corresponding to 's(0)' in expression 's(0) + d' above.
            Dim elementAccess = CType(addExpression.Left, InvocationExpressionSyntax)

            ' Use GetTypeInfo() to get TypeSymbol corresponding to 's(0)' above.
            expressionTypeInfo = model.GetTypeInfo(elementAccess)
            expressionType = expressionTypeInfo.Type

            Assert.AreEqual(SpecialType.System_Int16, expressionType.SpecialType)
            Assert.AreEqual("Short", expressionType.ToDisplayString())
            Assert.AreEqual(SpecialType.System_Double, expressionTypeInfo.ConvertedType.SpecialType)

            Dim conv = model.GetConversion(elementAccess)
            Assert.IsTrue(conv.IsWidening AndAlso conv.IsNumeric)

            ' Get IdentifierNameSyntax corresponding to the parameter 's' in expression 's(0) + d' above.
            identifier = CType(elementAccess.Expression, IdentifierNameSyntax)

            ' Use GetTypeInfo() to get TypeSymbol corresponding to parameter 's' above.
            variableTypeInfo = model.GetTypeInfo(identifier)
            variableType = variableTypeInfo.Type

            Assert.AreEqual("Short()", variableType.ToDisplayString())
            Assert.AreEqual("Short()", variableTypeInfo.ConvertedType.ToDisplayString())
            Assert.IsTrue(model.GetConversion(identifier).IsIdentity)

            ' Alternately, use GetSymbolInfo() to get TypeSymbol corresponding to parameter 's' above.
            variableType = (CType(model.GetSymbolInfo(identifier).Symbol, IParameterSymbol)).Type

            Assert.AreEqual("Short()", variableType.ToDisplayString())
            Assert.AreEqual(SpecialType.System_Int16, CType(variableType, IArrayTypeSymbol).ElementType.SpecialType)
        End Sub

        <FAQ(4)>
        <TestMethod>
        Public Sub GetInScopeSymbols()
            Dim source =
<text>
Class C

End Class

Module Program

    Private i As Integer = 0

    Public Sub Main()
        
        Dim j As Integer = 0 : j += i

        ' What symbols are in scope here?
    End Sub
End Module
</text>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)
            Dim model = comp.GetSemanticModel(tree)

            ' Get position of the comment above.
            Dim position = source.IndexOf("' ")

            ' Get 'all' symbols that are in scope at the above position. 
            Dim symbols = model.LookupSymbols(position)

            ' Note: "Windows" only appears as a symbol at this location in Windows 8.1.
            Dim results = String.Join(vbLf, From symbol In symbols
                                            Select result = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                                            Where result <> "Windows"
                                            Order By result)

            Assert.AreEqual(
<text>C
j As Integer
Microsoft
Program
Program.i As Integer
Sub Program.Main()
System</text>.Value, results)

            ' Filter results by looking at Kind of returned symbols (only get locals and fields).
            results = String.Join(vbLf, From symbol In symbols
                                        Where symbol.Kind = SymbolKind.Local OrElse
                                              symbol.Kind = SymbolKind.Field
                                        Select result = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                                        Order By result)
            Assert.AreEqual(
<text>j As Integer
Program.i As Integer</text>.Value, results)

            ' Filter results - get namespaces and types.
            ' Note: "Windows" only appears as a symbol at this location in Windows 8.1.
            symbols = model.LookupNamespacesAndTypes(position)
            results = String.Join(vbLf, From symbol In symbols
                                        Select result = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                                        Where result <> "Windows"
                                        Order By result)

            Assert.AreEqual(
<text>C
Microsoft
Program
System</text>.Value, results)
        End Sub

        <FAQ(5)>
        <TestMethod>
        Public Sub GetSymbolsForAccessibleMembersOfAType()
            Dim source =
<text>
Imports System

Public Class C

    Friend InstanceField As Integer = 0

    Public Property InstanceProperty As Integer

    Friend Sub InstanceMethod()
        Console.WriteLine(InstanceField)
    End Sub

    Protected Sub InaccessibleInstanceMethod()
        Console.WriteLine(InstanceProperty)
    End Sub
End Class

Public Module ExtensionMethods
    &lt;System.Runtime.CompilerServices.Extension&gt;
    Public Sub ExtensionMethod(s As C)
    End Sub
End Module

Module Program

    Sub Main()
        Dim c As C = New C()
        c.ToString()
    End Sub
End Module
</text>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)

            ' Get position of 'c.ToString()' above.
            Dim position = source.IndexOf("c.ToString()")

            ' Get IdentifierNameSyntax corresponding to identifier 'c' above.
            Dim identifier = CType(tree.GetRoot().FindToken(position).Parent, IdentifierNameSyntax)

            ' Get TypeSymbol corresponding to variable 'c' above.
            Dim type = model.GetTypeInfo(identifier).Type

            ' Get symbols for 'accessible' members on the above TypeSymbol.
            Dim symbols = model.LookupSymbols(position, container:=type, includeReducedExtensionMethods:=True)

            Dim results = String.Join(vbLf, From symbol In symbols
                                            Select result = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                                            Order By result)
            Assert.AreEqual(
<text>C.InstanceField As Integer
Function Object.Equals(obj As Object) As Boolean
Function Object.Equals(objA As Object, objB As Object) As Boolean
Function Object.GetHashCode() As Integer
Function Object.GetType() As Type
Function Object.ReferenceEquals(objA As Object, objB As Object) As Boolean
Function Object.ToString() As String
Property C.InstanceProperty As Integer
Sub C.ExtensionMethod()
Sub C.InstanceMethod()</text>.Value, results)
        End Sub

        <FAQ(6)>
        <TestMethod>
        Public Sub FindAllInvocationsOfAMethod()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Class C1

    Public Sub M1()
        M2()
    End Sub

    Public Sub M2()
    End Sub
End Class

Class C2

    Public Sub M1()
        M2()
        Call New C1().M2()
    End Sub

    Public Sub M2()
    End Sub
End Class

Module Program

    Sub Main()
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)
            Dim model = comp.GetSemanticModel(tree)

            ' Get MethodBlockSyntax corresponding to method C1.M2() above.
            Dim methodDeclaration As MethodBlockSyntax =
                    Aggregate c In tree.GetRoot().DescendantNodes.OfType(Of ClassBlockSyntax)()
                    Where c.ClassStatement.Identifier.ValueText = "C1"
                    From m In c.Members.OfType(Of MethodBlockSyntax)()
                    Where CType(m.SubOrFunctionStatement, MethodStatementSyntax).Identifier.ValueText = "M2"
                    Select m
                    Into [Single]()

            ' Get MethodSymbol corresponding to method C1.M2() above.
            Dim method = CType(model.GetDeclaredSymbol(methodDeclaration), IMethodSymbol)

            ' Get all InvocationExpressionSyntax in the above code.
            Dim allInvocations = tree.GetRoot().DescendantNodes.OfType(Of InvocationExpressionSyntax)()

            ' Use GetSymbolInfo() to find invocations of method C1.M2() above.
            Dim matchingInvocations = From i In allInvocations Where model.GetSymbolInfo(i).Symbol.Equals(method)

            Assert.AreEqual(2, matchingInvocations.Count)
        End Sub

        <FAQ(7)>
        <TestMethod>
        Public Sub FindAllReferencesToAMethodInASolution()
            Dim source1 = <text>
Namespace NS

    Public Class C

        Public Sub MethodThatWeAreTryingToFind()
        End Sub

        Public Sub AnotherMethod()
            MethodThatWeAreTryingToFind() ' First Reference.
        End Sub
    End Class
End Namespace</text>.Value
            Dim source2 = <text>
Imports NS
Imports AliasedType = NS.C

Module Program

    Sub Main()
        Dim c1 = New C()
        c1.MethodThatWeAreTryingToFind() ' Second Reference.
        c1.AnotherMethod()
        Dim c2 = New AliasedType()
        c2.MethodThatWeAreTryingToFind() ' Third Reference.
    End Sub
End Module</text>.Value
            Dim _project1Id = ProjectId.CreateNewId(),
                _project2Id = ProjectId.CreateNewId()
            Dim _document1Id = DocumentId.CreateNewId(_project1Id),
                _document2Id = DocumentId.CreateNewId(_project2Id)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)

            Dim sln = New AdhocWorkspace().CurrentSolution.
                        AddProject(_project1Id, "Project1", "Project1", LanguageNames.VisualBasic).
                            AddMetadataReference(_project1Id, Mscorlib).
                            AddDocument(_document1Id, "File1.vb", source1).
                            WithProjectCompilationOptions(_project1Id, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithEmbedVbCoreRuntime(True)).
                        AddProject(_project2Id, "Project2", "Project2", LanguageNames.VisualBasic).WithProjectCompilationOptions(_project2Id, vbOptions).
                            AddMetadataReference(_project2Id, Mscorlib).
                            AddProjectReference(_project2Id, New ProjectReference(_project1Id)).
                            AddDocument(_document2Id, "File2.vb", source2)

            ' If you wish to try against a real solution you could use code like
            ' Dim sln = Solution.Load("<Path>")
            ' OR Dim sln = Workspace.LoadSolution("<Path>").CurrentSolution
            Dim project1 = sln.GetProject(_project1Id)
            Dim document1 = project1.GetDocument(_document1Id)

            ' Get MethodBlockSyntax corresponding to the 'MethodThatWeAreTryingToFind'.
            Dim methodBlock As MethodBlockSyntax = document1.GetSyntaxRootAsync().Result.DescendantNodes.
                                                                             OfType(Of MethodBlockSyntax).
                                                                             Single(Function(m) m.SubOrFunctionStatement.Identifier.ValueText = "MethodThatWeAreTryingToFind")

            ' Get MethodSymbol corresponding to the 'MethodThatWeAreTryingToFind'.
            Dim method = document1.GetSemanticModelAsync().Result.GetDeclaredSymbol(methodBlock)

            ' Find all references to the 'MethodThatWeAreTryingToFind' in the solution.
            Dim methodReferences = SymbolFinder.FindReferencesAsync(method, sln).Result

            Assert.AreEqual(1, methodReferences.Count)

            Dim methodReference = methodReferences.Single()

            Assert.AreEqual(3, methodReference.Locations.Count)

            Dim methodDefinition = CType(methodReference.Definition, IMethodSymbol)

            Assert.AreEqual("MethodThatWeAreTryingToFind", methodDefinition.Name)
            Assert.IsTrue(methodReference.Definition.Locations.Single.IsInSource)
            Assert.AreEqual("File1.vb", methodReference.Definition.Locations.Single.SourceTree.FilePath)
            Assert.IsTrue(methodReference.Locations.All(Function(referenceLocation) referenceLocation.Location.IsInSource))
            Assert.AreEqual(1, methodReference.Locations.Count(Function(referenceLocation) referenceLocation.Document.Name = "File1.vb"))
            Assert.AreEqual(2, methodReference.Locations.Count(Function(referenceLocation) referenceLocation.Document.Name = "File2.vb"))
        End Sub

        <FAQ(8)>
        <TestMethod>
        Public Sub FindAllInvocationsToMethodsFromAParticularNamespace()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System
Imports System.Threading.Tasks

Module Program

    Sub Main()
        Dim a As Action = Sub() Return
        Dim t = Task.Factory.StartNew(a)
        t.Wait()
        Console.WriteLine(a.ToString())

        a = Sub()
                t = New Task(a)
                t.Start()
                t.Wait()
            End Sub
        a()
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)

            ' Instantiate MethodInvocationWalker (below) and tell it to find invocations to methods from the System.Threading.Tasks namespace.
            Dim walker = New MethodInvocationWalker With {.SemanticModel = model, .NamespaceName = "System.Threading.Tasks"}
            walker.Visit(tree.GetRoot())

            Assert.AreEqual(
<text>
Line 8: Task.Factory.StartNew(a)
Line 9: t.Wait()
Line 13: New Task(a)
Line 14: t.Start()
Line 15: t.Wait()</text>.Value, walker.Results.ToString())
        End Sub

        ' Below SyntaxWalker checks all nodes of type ObjectCreationExpressionSyntax or InvocationExpressionSyntax
        ' present under the SyntaxNode being visited to detect invocations to methods from the supplied namespace.
        Public Class MethodInvocationWalker
            Inherits VisualBasicSyntaxWalker

            Public Property SemanticModel As SemanticModel

            Public Property NamespaceName As String

            Public Property Results As New StringBuilder()

            Private Function CheckWhetherMethodIsFromNamespace(node As ExpressionSyntax) As Boolean
                Dim isMatch = False
                If SemanticModel IsNot Nothing Then
                    Dim symbolInfo = SemanticModel.GetSymbolInfo(node)

                    Dim ns As String = symbolInfo.Symbol.ContainingNamespace.ToDisplayString()
                    If ns = NamespaceName Then
                        Results.Append(vbLf)
                        Results.Append("Line ")
                        Results.Append(SemanticModel.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line)
                        Results.Append(": ")
                        Results.Append(node.ToString())
                        isMatch = True
                    End If
                End If

                Return isMatch
            End Function

            Public Overrides Sub VisitObjectCreationExpression(node As ObjectCreationExpressionSyntax)
                CheckWhetherMethodIsFromNamespace(node)
                MyBase.VisitObjectCreationExpression(node)
            End Sub

            Public Overrides Sub VisitInvocationExpression(node As InvocationExpressionSyntax)
                CheckWhetherMethodIsFromNamespace(node)
                MyBase.VisitInvocationExpression(node)
            End Sub
        End Class

        <FAQ(9)>
        <TestMethod>
        Public Sub GetAllFieldAndMethodSymbolsInACompilation()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Namespace NS1

    Public Class C

        Dim InstanceField As Integer = 0

        Friend Sub InstanceMethod()
            Console.WriteLine(InstanceField)
        End Sub
    End Class
End Namespace

Namespace NS2

    Module ExtensionMethods
        &lt;System.Runtime.CompilerServices.Extension&gt;
        Public Sub ExtensionMethod(s As NS1.C)
        End Sub
    End Module
End Namespace

Module Program

    Sub Main()
        Dim c As NS1.C = New NS1.C()
        c.ToString()
    End Sub
End Module
</text>.Value)

            Dim vbRuntime = MetadataReference.CreateFromFile(GetType(CompilerServices.StandardModuleAttribute).Assembly.Location)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib, vbRuntime})
            Dim results = New StringBuilder()

            ' Traverse the symbol tree to find all namespaces, types, methods and fields.
            ' For Each ns As NamespaceSymbol In comp.GetReferencedAssemblySymbol(Mscorlib).GlobalNamespace.GetNamespaceMembers()
            For Each ns In comp.Assembly.GlobalNamespace.GetNamespaceMembers()
                results.Append(vbLf)
                results.Append(ns.Kind.ToString())
                results.Append(": ")
                results.Append(ns.Name)
                For Each typeMember In ns.GetTypeMembers()
                    results.Append(vbLf)
                    results.Append("    ")
                    results.Append(typeMember.TypeKind.ToString())
                    results.Append(": ")
                    results.Append(typeMember.Name)
                    For Each member In typeMember.GetMembers()
                        results.Append(vbLf)
                        results.Append("       ")
                        If member.Kind = SymbolKind.Field OrElse member.Kind = SymbolKind.Method Then
                            results.Append(member.Kind.ToString())
                            results.Append(": ")
                            results.Append(member.Name)
                        End If
                    Next

                Next

            Next

            Assert.AreEqual(
<text>
Namespace: NS1
    Class: C
       Method: .ctor
       Field: InstanceField
       Method: InstanceMethod
Namespace: NS2
    Module: ExtensionMethods
       Method: ExtensionMethod</text>.Value, results.ToString())
        End Sub

        <FAQ(10)>
        <TestMethod>
        Public Sub TraverseAllExpressionsInASyntaxTreeUsingAWalker()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Module Program

    Sub Main()
        Dim i = 0.0
        i += 1 + 2L
    End Sub
End Module
</text>.Value)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)
            Dim walker = New ExpressionWalker() With {.SemanticModel = model}
            walker.Visit(tree.GetRoot())
            Assert.AreEqual(
<text>
LiteralExpressionSyntax 0.0 has type Double
IdentifierNameSyntax i has type Double
BinaryExpressionSyntax 1 + 2L has type Long
LiteralExpressionSyntax 1 has type Integer
LiteralExpressionSyntax 2L has type Long</text>.Value, walker.Results.ToString())
        End Sub

        ' Below SyntaxWalker traverses all expressions under the SyntaxNode being visited and lists the types of these expressions.
        Public Class ExpressionWalker
            Inherits SyntaxWalker
            Public Property SemanticModel As SemanticModel

            Public Property Results As New StringBuilder()

            Public Overrides Sub Visit(node As SyntaxNode)
                If TypeOf node Is ExpressionSyntax Then
                    Dim type = SemanticModel.GetTypeInfo(CType(node, ExpressionSyntax)).Type
                    If type IsNot Nothing Then
                        Results.Append(vbLf)
                        Results.Append(node.GetType().Name)
                        Results.Append(" ")
                        Results.Append(node.ToString())
                        Results.Append(" has type ")
                        Results.Append(type.ToDisplayString())
                    End If
                End If

                MyBase.Visit(node)
            End Sub
        End Class

        <FAQ(11)>
        <TestMethod>
        Public Sub CompareSyntax()
            Dim source =
<text>
Imports System

Module Program

    Sub Main()
        Dim i = 0.0
        i += 1 + 2L
    End Sub
End Module
</text>.Value
            Dim tree1 = SyntaxFactory.ParseSyntaxTree(source)
            Dim tree2 = SyntaxFactory.ParseSyntaxTree(source)
            Dim node1 As SyntaxNode = tree1.GetRoot()
            Dim node2 As SyntaxNode = tree2.GetRoot()

            ' Compare trees and nodes that are identical.
            Assert.IsTrue(tree1.IsEquivalentTo(tree2))
            Assert.IsTrue(node1.IsEquivalentTo(node2))

            ' tree3 is identical to tree1 except for a single comment.
            Dim tree3 = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Module Program

    ' Additional comment.
    Sub Main()
        Dim i = 0.0
        i += 1 + 2L
    End Sub
End Module
</text>.Value)
            Dim node3 As SyntaxNode = tree3.GetRoot()

            ' Compare trees and nodes that are identical except for trivia.
            Assert.IsTrue(tree1.IsEquivalentTo(tree3)) ' Trivia differences are ignored.
            Assert.IsFalse(node1.IsEquivalentTo(node3)) ' Trivia differences are considered.

            ' tree4 is identical to tree1 except for method body contents.
            Dim tree4 = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Module Program

    Sub Main()
    End Sub
End Module
</text>.Value)

            Dim node4 As SyntaxNode = tree4.GetRoot()

            ' Compare trees and nodes that are identical at the top-level.
            Assert.IsTrue(tree1.IsEquivalentTo(tree4, topLevel:=True)) ' Only top-level nodes are considered.
            Assert.IsFalse(node1.IsEquivalentTo(node4)) ' Non-top-level nodes are considered.

            ' Tokens and Trivia can also be compared.
            Dim token1 As SyntaxToken = node1.DescendantTokens.First
            Dim token2 As SyntaxToken = node2.DescendantTokens.First

            Assert.IsTrue(token1.IsEquivalentTo(token2))

            Dim trivia1 As SyntaxTrivia = node1.DescendantTrivia().First(Function(t) t.Kind() = SyntaxKind.WhitespaceTrivia)
            Dim trivia2 As SyntaxTrivia = node2.DescendantTrivia().Last(Function(t) t.Kind() = SyntaxKind.EndOfLineTrivia)

            Assert.IsFalse(trivia1.IsEquivalentTo(trivia2))
        End Sub

        <FAQ(29)>
        <TestMethod>
        Public Sub TraverseAllCommentsInASyntaxTreeUsingAWalker()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

''' &lt;summary&gt;First Comment&lt;/summary&gt;
Module Program

    ' Second Comment
    Sub Main()
        ' Third Comment
    End Sub

End Module    
</text>.Value)

            Dim walker As New CommentWalker()
            walker.Visit(tree.GetRoot())

            Assert.AreEqual(
<text>
''' &lt;summary&gt;First Comment&lt;/summary&gt; (Parent Token: ModuleKeyword) (Structured)
' Second Comment (Parent Token: SubKeyword)
' Third Comment (Parent Token: EndKeyword)</text>.Value, walker.Results.ToString())
        End Sub

        ' Below SyntaxWalker traverses all comments present under the SyntaxNode being visited.
        Public Class CommentWalker
            Inherits VisualBasicSyntaxWalker

            Public Property Results As New StringBuilder()

            Public Sub New()
                MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
            End Sub

            Public Overrides Sub VisitTrivia(trivia As SyntaxTrivia)
                If trivia.Kind() = SyntaxKind.CommentTrivia OrElse trivia.Kind() = SyntaxKind.DocumentationCommentTrivia Then
                    Results.Append(vbLf)
                    Results.Append(trivia.ToFullString().Trim())
                    Results.Append(" (Parent Token: ")
                    Results.Append(trivia.Token.Kind.ToString())
                    Results.Append(")")

                    If trivia.Kind() = SyntaxKind.DocumentationCommentTrivia Then
                        ' Trivia for xml documentation comments have additional 'structure'
                        ' available under a child DocumentationCommentSyntax.
                        Assert.IsTrue(trivia.HasStructure)
                        Dim documentationComment = CType(trivia.GetStructure(), DocumentationCommentTriviaSyntax)
                        Assert.IsTrue(documentationComment.ParentTrivia = trivia)
                        Results.Append(" (Structured)")
                    End If
                End If

                MyBase.VisitTrivia(trivia)
            End Sub
        End Class

        <FAQ(12)>
        <TestMethod>
        Public Sub CompareSymbols()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Class C

End Class

Module Program

    Public Sub Main()
        Dim c = New C()
        Console.WriteLine(c.ToString())
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)
            Dim model = comp.GetSemanticModel(tree)

            ' Get ModifiedIdentifierSyntax corresponding to the identifier 'c' in the statement 'Dim c = ...' above.
            Dim identifier As ModifiedIdentifierSyntax = tree.GetRoot().
                                                              DescendantNodes.
                                                              OfType(Of VariableDeclaratorSyntax).
                                                              Single.
                                                              Names.
                                                              Single

            ' Get TypeSymbol corresponding to 'Dim c' above.
            Dim type As ITypeSymbol = CType(model.GetDeclaredSymbol(identifier), ILocalSymbol).Type
            Dim expectedType As ITypeSymbol = comp.GetTypeByMetadataName("C")

            Assert.IsTrue(type.Equals(expectedType))
        End Sub

        <FAQ(13)>
        <TestMethod>
        Public Sub TestWhetherANodeIsPartOfATreeOrASemanticModel()
            Dim source = <text>
Imports System

Class C

End Class

Module Program

    Public Sub Main()
        Dim c = New C()
        Console.WriteLine(c.ToString())
    End Sub
End Module
</text>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim other = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)
            Dim nodeFromTree As SyntaxNode = tree.GetRoot()
            Dim tokenNotFromTree As SyntaxToken = SyntaxFactory.Token(SyntaxKind.ClassKeyword)
            Dim nodeNotFromTree As SyntaxNode = other.GetRoot()

            Assert.IsTrue(nodeFromTree.SyntaxTree Is tree)
            Assert.IsTrue(nodeFromTree.SyntaxTree Is model.SyntaxTree)
            Assert.IsFalse(tokenNotFromTree.SyntaxTree Is tree)
            Assert.IsFalse(nodeNotFromTree.SyntaxTree Is model.SyntaxTree)
            Assert.IsTrue(nodeNotFromTree.SyntaxTree Is other)
        End Sub

        <FAQ(14)>
        <TestMethod>
        Public Sub ValueVersusValueTextVersusGetTextForTokens()
            Dim source = <text>
Imports System

Module Program

    Public Sub Main()
        Dim [long] = 1L
        Console.WriteLine([long])
    End Sub
End Module
</text>.Value
            Dim tree = SyntaxFactory.ParseSyntaxTree(source)

            ' Get token corresponding to identifier '[long]' above.
            Dim token1 As SyntaxToken = tree.GetRoot().FindToken(source.IndexOf("[long]"))
            ' Get token corresponding to literal '1L' above.
            Dim token2 As SyntaxToken = tree.GetRoot().FindToken(source.IndexOf("1L"))

            Assert.AreEqual("String", token1.Value.GetType().Name)
            Assert.AreEqual("long", token1.Value)
            Assert.AreEqual("long", token1.ValueText)
            Assert.AreEqual("[long]", token1.ToString())

            Assert.AreEqual("Int64", token2.Value.GetType().Name)
            Assert.AreEqual(1L, token2.Value)
            Assert.AreEqual("1", token2.ValueText)
            Assert.AreEqual("1L", token2.ToString())
        End Sub

        <FAQ(16)>
        <TestMethod>
        Public Sub GetLineAndColumnInfo()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Public Sub Main()
    End Sub
End Module
</text>.Value, path:="MyCodeFile.vb")

            ' Get MethodBlockSyntax corresponding to the method block for 'Sub Main()' above.
            Dim node As MethodBlockSyntax = tree.GetRoot().DescendantNodes.OfType(Of MethodBlockSyntax).Single

            ' Use GetLocation() and GetLineSpan() to get file, line and column info for above BlockSyntax.
            Dim location As Location = node.GetLocation()
            Dim lineSpan As FileLinePositionSpan = location.GetLineSpan()

            Assert.IsTrue(location.IsInSource)
            Assert.AreEqual("MyCodeFile.vb", lineSpan.Path)
            Assert.AreEqual(3, lineSpan.StartLinePosition.Line)
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character)

            ' Alternate way to get file, line and column info from any span.
            location = tree.GetLocation(node.Span)
            lineSpan = location.GetLineSpan()

            Assert.AreEqual("MyCodeFile.vb", lineSpan.Path)
            Assert.AreEqual(3, lineSpan.StartLinePosition.Line)
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character)

            ' Yet another way to get file, line and column info from any span.
            lineSpan = tree.GetLineSpan(node.Span)

            Assert.AreEqual("MyCodeFile.vb", lineSpan.Path)
            Assert.AreEqual(4, lineSpan.EndLinePosition.Line)
            Assert.AreEqual(11, lineSpan.EndLinePosition.Character)

            ' SyntaxTokens also have GetLocation(). 
            ' Use GetLocation() to get the position of the 'Public' token under the above MethodBlockSyntax.                       
            Dim token As SyntaxToken = node.DescendantTokens().First
            location = token.GetLocation()
            lineSpan = location.GetLineSpan()

            Assert.AreEqual("MyCodeFile.vb", lineSpan.Path)
            Assert.AreEqual(3, lineSpan.StartLinePosition.Line)
            Assert.AreEqual(4, lineSpan.StartLinePosition.Character)

            ' SyntaxTrivia also have GetLocation(). 
            ' Use GetLocation() to get the position of the first EndOfLineTrivia under the above SyntaxToken.      
            Dim trivia As SyntaxTrivia = token.LeadingTrivia.First()
            location = trivia.GetLocation()
            lineSpan = location.GetLineSpan()

            Assert.AreEqual("MyCodeFile.vb", lineSpan.Path)
            Assert.AreEqual(2, lineSpan.StartLinePosition.Line)
            Assert.AreEqual(0, lineSpan.StartLinePosition.Character)
        End Sub

        <FAQ(17)>
        <TestMethod>
        Public Sub GetEmptySourceLinesFromASyntaxTree()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Public Shared Sub Main()
    End Sub
End Module
</text>.Value, path:="MyCodeFile.vb")

            Dim text As SourceText = tree.GetText()

            Assert.AreEqual(7, text.Lines.Count)

            ' Enumerate empty lines.
            Dim results = String.Join(vbLf, From line In text.Lines
                                            Where String.IsNullOrWhiteSpace(line.ToString())
                                            Select String.Format("Line {0} (Span {1}-{2}) is empty", line.LineNumber, line.Start, line.End))

            Assert.AreEqual(
<text>Line 0 (Span 0-0) is empty
Line 2 (Span 16-16) is empty
Line 6 (Span 69-69) is empty</text>.Value, results)
        End Sub

        <FAQ(18)>
        <TestMethod>
        Public Sub UseSyntaxWalker()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Public Sub Main()
#If True Then
#End If
        Dim b = True
        If b Then
        End If

        If Not b Then
        End If
    End Sub
End Module

Structure S

End Structure
</text>.Value)
            Dim walker = New IfStatementIfKeywordAndTypeBlockWalker()
            walker.Visit(tree.GetRoot())
            Assert.AreEqual(
<text>
Visiting ModuleBlockSyntax (Kind = ModuleBlock)
Visiting SyntaxToken (Kind = IfKeyword): #If True Then
Visiting SyntaxToken (Kind = IfKeyword): #End If
Visiting IfStatementSyntax (Kind = IfStatement): If b Then
Visiting SyntaxToken (Kind = IfKeyword): If b Then
Visiting SyntaxToken (Kind = IfKeyword): End If
Visiting IfStatementSyntax (Kind = IfStatement): If Not b Then
Visiting SyntaxToken (Kind = IfKeyword): If Not b Then
Visiting SyntaxToken (Kind = IfKeyword): End If
Visiting StructureBlockSyntax (Kind = StructureBlock)</text>.Value, walker.Results.ToString())
        End Sub

        ' Below SyntaxWalker traverses all IfStatementSyntax, IfKeyworkd and TypeBlockSyntax present under the SyntaxNode being visited.
        Public Class IfStatementIfKeywordAndTypeBlockWalker
            Inherits VisualBasicSyntaxWalker
            Public Property Results As New StringBuilder()

            ' Turn on visiting of nodes, tokens and trivia present under structured trivia.
            Public Sub New()
                MyBase.New(SyntaxWalkerDepth.StructuredTrivia)
            End Sub

            ' If you need to visit all SyntaxNodes of a particular (derived) type that appears directly
            ' in a syntax tree, you can override the Visit* mehtod corresponding to this type.
            ' For example, you can override VisitIfStatement to visit all SyntaxNodes of type IfStatementSyntax.
            Public Overrides Sub VisitIfStatement(node As IfStatementSyntax)
                Results.Append(vbLf)
                Results.Append("Visiting ")
                Results.Append(node.GetType().Name)
                Results.Append(" (Kind = ")
                Results.Append(node.Kind().ToString())
                Results.Append("): ")
                Results.Append(node.ToString())
                MyBase.VisitIfStatement(node)
            End Sub

            ' Visits all SyntaxTokens.
            Public Overrides Sub VisitToken(token As SyntaxToken)
                ' We only care about SyntaxTokens with Kind 'IfKeyword'.
                If token.Kind() = SyntaxKind.IfKeyword Then
                    Results.Append(vbLf)
                    Results.Append("Visiting ")
                    Results.Append(token.GetType().Name)
                    Results.Append(" (Kind = ")
                    Results.Append(token.Kind().ToString())
                    Results.Append("): ")
                    Results.Append(token.Parent.ToString())
                End If

                MyBase.VisitToken(token)
            End Sub

            ' Visits all SyntaxNodes.
            Public Overrides Sub Visit(node As SyntaxNode)
                ' If you need to visit all SyntaxNodes of a particular base type that can never
                ' appear directly in a syntax tree then this would be the place to check for that.
                ' For example, TypeBlockSyntax is a base type for all the type declarations (like 
                ' ModuleBlockSyntax and StructureBlockSyntax) that can appear in a syntax tree.
                If TypeOf node Is TypeBlockSyntax Then
                    Results.Append(vbLf)
                    Results.Append("Visiting ")
                    Results.Append(node.GetType().Name)
                    Results.Append(" (Kind = ")
                    Results.Append(node.Kind().ToString())
                    Results.Append(")")
                End If

                MyBase.Visit(node)
            End Sub
        End Class

        <FAQ(19)>
        <TestMethod>
        Public Sub GetFullyQualifiedName()
            Dim source =
<text>
Imports System
Imports AliasedType = NS.C(Of Integer)

Namespace NS

    Public Class C(Of T)

        Public Structure S(Of U)

        End Structure
    End Class
End Namespace

Module Program

    Public Sub Main()
        Dim s As AliasedType.S(Of Long) = New AliasedType.S(Of Long)()
        Console.WriteLine(s.ToString())
    End Sub
End Module
</text>.Value
            Dim _projectId = ProjectId.CreateNewId()
            Dim _documentId = DocumentId.CreateNewId(_projectId)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)

            Dim sln = New AdhocWorkspace().CurrentSolution.
                          AddProject(_projectId, "MyProject", "MyProject", LanguageNames.VisualBasic).WithProjectCompilationOptions(_projectId, vbOptions).
                              AddMetadataReference(_projectId, Mscorlib).
                              AddDocument(_documentId, "MyFile.vb", source)

            Dim document = sln.GetDocument(_documentId)
            Dim root = document.GetSyntaxRootAsync().Result
            Dim model = CType(document.GetSemanticModelAsync().Result, SemanticModel)

            ' Get StructureBlockSyntax corresponding to 'Structure S' above.
            Dim structBlock As StructureBlockSyntax = root.DescendantNodes.OfType(Of StructureBlockSyntax).Single

            ' Get TypeSymbol corresponding to 'Structure S' above.
            Dim structType = model.GetDeclaredSymbol(structBlock)

            ' Use ToDisplayString() to get fully qualified name.
            Assert.AreEqual("NS.C(Of T).S(Of U)", structType.ToDisplayString())
            Assert.AreEqual("Global.NS.C(Of T).S(Of U)", structType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))

            ' Get ModifiedIdentifierSyntax corresponding to identifier 's' in 'Dim s As AliasedType.S(Of Long) = ...' above.
            Dim modifiedIdentifier As ModifiedIdentifierSyntax = root.DescendantNodes.
                                                                      OfType(Of VariableDeclaratorSyntax).
                                                                      Single.
                                                                      Names.
                                                                      Single

            ' Get TypeSymbol corresponding to above ModifiedIdentifierSyntax.
            Dim variableType = (CType(model.GetDeclaredSymbol(modifiedIdentifier), ILocalSymbol)).Type

            Assert.IsFalse(variableType.Equals(structType)) ' Type of variable is a closed generic type while that of the struct is an open generic type.
            Assert.IsTrue(variableType.OriginalDefinition.Equals(structType)) ' OriginalDefinition for a closed generic type points to corresponding open generic type.
            Assert.AreEqual("NS.C(Of Integer).S(Of Long)", variableType.ToDisplayString())
            Assert.AreEqual("Global.NS.C(Of Integer).S(Of Long)", variableType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
        End Sub

        <FAQ(20)>
        <TestMethod>
        Public Sub OverloadBindingDetermination()
            Dim source = <source>
Imports System

Public Class Program
	Private Function Identity (a As Integer)
		Return a
	End Function
	
	Private Function Identity (a As Char)
		Return a
	End Function
	
	Public Sub Main()
		Dim v1 = Identity(3)
		Dim v2 = Identity("a"C)
        Dim v3 = Identity("arg1")
	End Sub
End Class</source>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)
            Dim model = comp.GetSemanticModel(tree)

            Dim allInvocations = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)

            ' Below, we expect to find that the method taking an Integer was selected.
            ' We can confidently index into the invocations because we are following the source line-by-line. This is not always a safe practice.
            Dim intInvocation = allInvocations.ElementAt(0)
            Dim info = model.GetSymbolInfo(intInvocation)
            Assert.IsNotNull(info.Symbol)
            Assert.AreEqual("Private Function Identity(a As Integer) As Object", info.Symbol.ToDisplayString)

            ' Below, we expect to find that the method taking a Char was selected.
            Dim charInvocation = allInvocations.ElementAt(1)
            info = model.GetSymbolInfo(charInvocation)
            Assert.IsNotNull(info.Symbol)
            Assert.AreEqual("Private Function Identity(a As Char) As Object", info.Symbol.ToDisplayString)

            ' Below, we expect to find that no suitable Method was found, and therefore none were selected.
            Dim stringInvocation = allInvocations.ElementAt(2)
            info = model.GetSymbolInfo(stringInvocation)
            Assert.IsNull(info.Symbol)
            Assert.AreEqual(2, info.CandidateSymbols.Length)
            Assert.AreEqual(CandidateReason.OverloadResolutionFailure, info.CandidateReason)
        End Sub

        <FAQ(21)>
        <TestMethod>
        Public Sub ClassifyConversionFromAnExpressionToATypeSymbol()
            Dim source =
<text>
Imports System

Module Program

    Sub M()
    End Sub

    Sub M(l As Long)
    End Sub

    Sub M(s As Short)
    End Sub

    Sub M(i As Integer)
    End Sub

    Sub Main()
        Dim ii As Integer = 0
        Console.WriteLine(ii)
        Dim jj As Short = 1
        Console.WriteLine(jj)
        Dim ss As String = String.Empty
        Console.WriteLine(ss)

        ' Perform conversion classification here.
    End Sub
End Module
</text>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)

            ' Get ModifiedIdentifierSyntax corresponding to variable 'ii' in 'Dim ii ...' above.
            Dim modifiedIdentifier As ModifiedIdentifierSyntax = CType(tree.GetRoot().FindToken(source.IndexOf("ii")).Parent.Parent, VariableDeclaratorSyntax).Names.Single

            ' Get TypeSymbol corresponding to above ModifiedIdentifierSyntax.
            Dim targetType = CType(model.GetDeclaredSymbol(modifiedIdentifier), ILocalSymbol).Type

            ' Perform ClassifyConversion for expressions from within the above SyntaxTree.
            Dim sourceExpression1 = CType(tree.GetRoot().FindToken(source.IndexOf("jj)")).Parent, ExpressionSyntax)
            Dim conversion As Conversion = model.ClassifyConversion(sourceExpression1, targetType)

            Assert.IsTrue(conversion.IsWidening AndAlso conversion.IsNumeric)

            Dim sourceExpression2 = CType(tree.GetRoot().FindToken(source.IndexOf("ss)")).Parent, ExpressionSyntax)
            conversion = model.ClassifyConversion(sourceExpression2, targetType)

            Assert.IsTrue(conversion.IsNarrowing AndAlso conversion.IsString)

            ' Perform ClassifyConversion for constructed expressions
            ' at the position identified by the comment "' Perform ..." above.
            Dim sourceExpression3 As ExpressionSyntax = SyntaxFactory.IdentifierName("jj")
            Dim position = source.IndexOf("' ")
            conversion = model.ClassifyConversion(position, sourceExpression3, targetType)

            Assert.IsTrue(conversion.IsWidening AndAlso conversion.IsNumeric)

            Dim sourceExpression4 As ExpressionSyntax = SyntaxFactory.IdentifierName("ss")
            conversion = model.ClassifyConversion(position, sourceExpression4, targetType)

            Assert.IsTrue(conversion.IsNarrowing AndAlso conversion.IsString)

            Dim sourceExpression5 As ExpressionSyntax = SyntaxFactory.ParseExpression("100L")
            conversion = model.ClassifyConversion(position, sourceExpression5, targetType)

            ' This is Widening because the numeric literal constant 100L can be converted to Integer
            ' without any data loss. Note: This is special for literal constants.
            Assert.IsTrue(conversion.IsWidening AndAlso conversion.IsNumeric)
        End Sub

        <FAQ(22)>
        <TestMethod>
        Public Sub ClassifyConversionFromOneTypeSymbolToAnother()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
    End Sub
End Module
</text>.Value)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim int32Type = comp.GetSpecialType(SpecialType.System_Int32)
            Dim int16Type = comp.GetSpecialType(SpecialType.System_Int16)
            Dim stringType = comp.GetSpecialType(SpecialType.System_String)
            Dim int64Type = comp.GetSpecialType(SpecialType.System_Int64)

            Assert.IsTrue(comp.ClassifyConversion(int32Type, int32Type).IsIdentity)

            Dim conversion1 = comp.ClassifyConversion(int16Type, int32Type)

            Assert.IsTrue(conversion1.IsWidening AndAlso conversion1.IsNumeric)

            Dim conversion2 = comp.ClassifyConversion(stringType, int32Type)

            Assert.IsTrue(conversion2.IsNarrowing AndAlso conversion2.IsString)

            Dim conversion3 = comp.ClassifyConversion(int64Type, int32Type)

            Assert.IsTrue(conversion3.IsNarrowing AndAlso conversion3.IsNumeric)
        End Sub

        <FAQ(23)>
        <TestMethod>
        Public Sub GetTargetFrameworkVersionForCompilation()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim version As Version = comp.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity.Version
            Assert.AreEqual(4, version.Major)
        End Sub

        <FAQ(24)>
        <TestMethod>
        Public Sub GetAssemblySymbolsAndSyntaxTreesFromAProject()
            Dim source = <text>
Module Program

    Sub Main()
    End Sub
End Module
</text>.Value

            Dim _projectId = ProjectId.CreateNewId()
            Dim _documentId = DocumentId.CreateNewId(_projectId)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)

            Dim sln = New AdhocWorkspace().CurrentSolution.
                          AddProject(_projectId, "MyProject", "MyProject", LanguageNames.VisualBasic).WithProjectCompilationOptions(_projectId, vbOptions).
                              AddMetadataReference(_projectId, Mscorlib).
                              AddDocument(_documentId, "MyFile.vb", source)

            ' If you wish to try against a real project you could use code like
            ' Dim project = Solution.LoadStandaloneProject("<Path>")
            ' OR Dim project = Workspace.LoadStandaloneProject("<Path>").CurrentSolution.Projects.First
            Dim project = sln.Projects.Single
            Dim compilation = project.GetCompilationAsync().Result

            ' Get AssemblySymbols for above compilation and the first assembly (Mscorlib) referenced by it.
            Dim compilationAssembly As IAssemblySymbol = compilation.Assembly
            Dim referencedAssembly As IAssemblySymbol = DirectCast(compilation.GetAssemblyOrModuleSymbol(project.MetadataReferences.First), IAssemblySymbol)

            Assert.IsTrue(compilation.GetTypeByMetadataName("Program").ContainingAssembly.Equals(compilationAssembly))
            Assert.IsTrue(compilation.GetTypeByMetadataName("System.Object").ContainingAssembly.Equals(referencedAssembly))

            Dim tree As SyntaxTree = project.Documents.Single.GetSyntaxTreeAsync().Result

            Assert.AreEqual("MyFile.vb", tree.FilePath)

        End Sub

        <FAQ(25)>
        <TestMethod>
        Public Sub UseSyntaxAnnotations()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Module Program

    Sub Main()
        Dim i As Integer = 0
        Console.WriteLine(i)
    End Sub
End Module
</text>.Value)

            ' Tag all tokens that contain the letter 'i'.
            Dim rewriter = New MyAnnotator()
            Dim oldRoot As SyntaxNode = tree.GetRoot()
            Dim newRoot As SyntaxNode = rewriter.Visit(oldRoot)

            Assert.IsFalse(oldRoot.ContainsAnnotations)
            Assert.IsTrue(newRoot.ContainsAnnotations)

            ' Find all tokens that were tagged with annotations of type MyAnnotation.
            Dim annotatedTokens As IEnumerable(Of SyntaxNodeOrToken) = newRoot.GetAnnotatedNodesAndTokens(MyAnnotation.Kind)
            Dim results = String.Join(vbLf, annotatedTokens.Select(Function(nodeOrToken)
                                                                       Assert.IsTrue(nodeOrToken.IsToken)
                                                                       Dim annotation = nodeOrToken.GetAnnotations(MyAnnotation.Kind).Single
                                                                       Return String.Format("{0} (position {1})", nodeOrToken.ToString(), MyAnnotation.GetPosition(annotation))
                                                                   End Function))

            Assert.AreEqual(
<text>Main (position 2)
Dim (position 1)
i (position 0)
WriteLine (position 2)
i (position 0)</text>.Value, results)
        End Sub

        ' Below VisualBasicSyntaxRewriter tags all SyntaxTokens that contain the lowercase letter 'i' under the SyntaxNode being visited.
        Public Class MyAnnotator
            Inherits VisualBasicSyntaxRewriter

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim newToken = MyBase.VisitToken(token)
                Dim position = token.ToString().IndexOf("i"c)
                If position >= 0 Then
                    newToken = newToken.WithAdditionalAnnotations(MyAnnotation.Create(position))
                End If

                Return newToken
            End Function
        End Class

        Public Class MyAnnotation
            Public Const Kind As String = "MyAnnotation"

            Public Shared Function Create(position As Integer) As SyntaxAnnotation
                Return New SyntaxAnnotation(Kind, position.ToString())
            End Function

            Public Shared Function GetPosition(annotation As SyntaxAnnotation) As Integer
                Return Int32.Parse(annotation.Data)
            End Function
        End Class

        <FAQ(37)>
        <TestMethod>
        Public Sub GetBaseTypesAndOverridingRelationships()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System
MustInherit Class C1
    Public Overridable Function F1(s As Short) As Integer
        Return 0
    End Function

    Public MustOverride Property P1 As Integer
End Class

MustInherit Class C2
    Inherits C1

    Public Shadows Overridable Function F1(s As Short) As Integer
        Return 1
    End Function
End Class

Class C3 
    Inherits C2

    Public Overrides NotOverridable Function F1(s As Short) As Integer
        Return 2
    End Function

    Public Overrides Property P1 As Integer
End Class
</text>.Value)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            ' Get TypeSymbols for types C1, C2 and C3 above.
            Dim typeC1 = comp.GetTypeByMetadataName("C1")
            Dim typeC2 = comp.GetTypeByMetadataName("C2")
            Dim typeC3 = comp.GetTypeByMetadataName("C3")
            Dim typeObject = comp.GetSpecialType(SpecialType.System_Object)

            ' Get TypeSymbols for base types of C1, C2 and C3 above.
            Assert.IsTrue(typeC1.BaseType.Equals(typeObject))
            Assert.IsTrue(typeC2.BaseType.Equals(typeC1))
            Assert.IsTrue(typeC3.BaseType.Equals(typeC2))

            ' Get MethodSymbols for methods named F1 in types C1, C2 and C3 above.
            Dim methodC1F1 = CType(typeC1.GetMembers("F1").Single(), IMethodSymbol)
            Dim methodC2F1 = CType(typeC2.GetMembers("F1").Single(), IMethodSymbol)
            Dim methodC3F1 = CType(typeC3.GetMembers("F1").Single(), IMethodSymbol)

            ' Get overriding relationships between above MethodSymbols.
            Assert.IsTrue(methodC1F1.IsOverridable)
            Assert.IsTrue(methodC2F1.IsOverridable)
            Assert.IsFalse(methodC2F1.IsOverrides)
            Assert.IsTrue(methodC3F1.IsOverrides)
            Assert.IsTrue(methodC3F1.IsNotOverridable)
            Assert.IsTrue(methodC3F1.OverriddenMethod.Equals(methodC2F1))
            Assert.IsFalse(methodC3F1.OverriddenMethod.Equals(methodC1F1))

            ' Get PropertySymbols for properties named P1 in types C1 and C3 above.
            Dim propertyC1P1 = CType(typeC1.GetMembers("P1").Single(), IPropertySymbol)
            Dim propertyC3P1 = CType(typeC3.GetMembers("P1").Single(), IPropertySymbol)

            ' Get overriding relationships between above PropertySymbols.
            Assert.IsTrue(propertyC1P1.IsMustOverride)
            Assert.IsFalse(propertyC1P1.IsOverridable)
            Assert.IsTrue(propertyC3P1.IsOverrides)
            Assert.IsTrue(propertyC3P1.OverriddenProperty.Equals(propertyC1P1))
        End Sub

        <FAQ(38)>
        <TestMethod>
        Public Sub GetInterfacesAndImplementationRelationships()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System
Interface I1
    Sub M1()
    Property P1 As Integer
End Interface

Interface I2 
    Inherits I1
    Sub M2()
End Interface

Class C1
    Implements I1

    Public Sub M1() Implements I1.M1
    End Sub

    Public Overridable Property P1 As Integer Implements I1.P1
End Class

Class C2
    Inherits C1
    Implements I2

    Shadows Public Sub M1() Implements I1.M1
    End Sub

    Public Sub M2() Implements I2.M2
    End Sub
End Class

Class C3 
    Inherits C2
    Implements I1

    Public Overrides Property P1 As Integer Implements I1.P1
End Class
</text>.Value)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            ' Get TypeSymbols for types I1, I2, C1, C2 and C3 above.
            Dim typeI1 = comp.GetTypeByMetadataName("I1")
            Dim typeI2 = comp.GetTypeByMetadataName("I2")
            Dim typeC1 = comp.GetTypeByMetadataName("C1")
            Dim typeC2 = comp.GetTypeByMetadataName("C2")
            Dim typeC3 = comp.GetTypeByMetadataName("C3")

            Assert.IsNull(typeI1.BaseType)
            Assert.IsNull(typeI2.BaseType)
            Assert.AreEqual(0, typeI1.Interfaces.Count)
            Assert.IsTrue(typeI2.Interfaces.Single().Equals(typeI1))

            ' Get TypeSymbol for interface implemented by C1 above.
            Assert.IsTrue(typeC1.Interfaces.Single().Equals(typeI1))

            ' Get TypeSymbols for interfaces implemented by C2 above.
            Assert.IsTrue(typeC2.Interfaces.Single().Equals(typeI2))
            Assert.AreEqual(2, typeC2.AllInterfaces.Count)
            Assert.IsNotNull(Aggregate type In typeC2.AllInterfaces
                             Where type.Equals(typeI1)
                             Into [Single]())
            Assert.IsNotNull(Aggregate type In typeC2.AllInterfaces
                             Where type.Equals(typeI2)
                             Into [Single]())

            ' Get TypeSymbols for interfaces implemented by C3 above.
            Assert.IsTrue(typeC3.Interfaces.Single().Equals(typeI1))
            Assert.AreEqual(2, typeC3.AllInterfaces.Count)
            Assert.IsNotNull(Aggregate type In typeC3.AllInterfaces
                             Where type.Equals(typeI1)
                             Into [Single]())
            Assert.IsNotNull(Aggregate type In typeC3.AllInterfaces
                             Where type.Equals(typeI2)
                             Into [Single]())

            ' Get MethodSymbols for methods named M1 and M2 in types I1, I2, C1 and C2 above.
            Dim methodI1M1 = CType(typeI1.GetMembers("M1").Single(), IMethodSymbol)
            Dim methodI2M2 = CType(typeI2.GetMembers("M2").Single(), IMethodSymbol)
            Dim methodC1M1 = CType(typeC1.GetMembers("M1").Single(), IMethodSymbol)
            Dim methodC2M1 = CType(typeC2.GetMembers("M1").Single(), IMethodSymbol)
            Dim methodC2M2 = CType(typeC2.GetMembers("M2").Single(), IMethodSymbol)

            ' Get interface implementation relationships between above MethodSymbols.
            Assert.IsTrue(typeC1.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC1M1))
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC2M1))
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(methodI2M2).Equals(methodC2M2))
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(methodI1M1).Equals(methodC2M1))
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(methodI2M2).Equals(methodC2M2))

            Assert.IsTrue(methodC1M1.ExplicitInterfaceImplementations.Single().Equals(methodI1M1))
            Assert.IsTrue(methodC2M1.ExplicitInterfaceImplementations.Single().Equals(methodI1M1))
            Assert.IsTrue(methodC2M2.ExplicitInterfaceImplementations.Single().Equals(methodI2M2))

            ' Get PropertySymbols for properties named P1 in types I1, C1 and C3 above.
            Dim propertyI1P1 = CType(typeI1.GetMembers("P1").Single(), IPropertySymbol)
            Dim propertyC1P1 = CType(typeC1.GetMembers("P1").Single(), IPropertySymbol)
            Dim propertyC3P1 = CType(typeC3.GetMembers("P1").Single(), IPropertySymbol)

            ' Get interface implementation relationships between above PropertySymbols.
            Assert.IsTrue(typeC1.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC1P1))
            Assert.IsTrue(typeC2.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC1P1))
            Assert.IsTrue(typeC3.FindImplementationForInterfaceMember(propertyI1P1).Equals(propertyC3P1))

            Assert.IsTrue(propertyC1P1.ExplicitInterfaceImplementations.Single.Equals(propertyI1P1))
            Assert.IsTrue(propertyC3P1.ExplicitInterfaceImplementations.Single.Equals(propertyI1P1))
        End Sub

        <FAQ(39)>
        <TestMethod>
        Public Sub GetAppliedAttributes()
            Dim source = <source>
Imports System
Module Module1
        &lt;AttributeUsage(AttributeTargets.Method)&gt;
        Private Class ExampleAttribute
            Inherits Attribute

            Private ReadOnly _Id As Integer

            Public ReadOnly Property Id As Integer
                Get
                    Return _Id
                End Get
            End Property

            Public Sub New(id As Integer)
                Me._Id = id
            End Sub
        End Class

        Sub Method1()
            ' Intentionally left blank
        End Sub

        &lt;ExampleAttribute(1)&gt;
        Sub Method2()
            ' Intentionally left blank
        End Sub
        
        &lt;ExampleAttribute(2)&gt;
        Sub Method3()
            ' Intentionally left blank
        End Sub
End Module
                         </source>.Value
            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithEmbedVbCoreRuntime(True)
            Dim compilation = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = compilation.GetSemanticModel(tree)

            Dim getMethod As Func(Of String, IMethodSymbol) = Function(name) Aggregate declaration In tree.GetRoot().DescendantNodes().OfType(Of MethodStatementSyntax)
                                                                             Where 0 = String.Compare(name, declaration.Identifier.Text, True)
                                                                             Select model.GetDeclaredSymbol(declaration)
                                                                             Into [Single]

            Dim methodSymbol As IMethodSymbol
            Dim attributeSymbol As INamedTypeSymbol
            Dim appliedAttribute As AttributeData

            attributeSymbol = Aggregate declaration In tree.GetRoot().DescendantNodes().OfType(Of ClassStatementSyntax)
                              Where 0 = String.Compare(declaration.Identifier.Text, "ExampleAttribute", True)
                              Select model.GetDeclaredSymbol(declaration) Into [Single]

            ' Verify that a method has no attributes
            methodSymbol = getMethod("Method1")
            Assert.AreEqual(0, methodSymbol.GetAttributes().Count)

            ' Inspect the attributes that have been given to methods 2 and 3
            methodSymbol = getMethod("Method2")
            appliedAttribute = methodSymbol.GetAttributes().Single
            Assert.AreEqual(attributeSymbol, appliedAttribute.AttributeClass)
            Assert.AreEqual(TypedConstantKind.Primitive, appliedAttribute.ConstructorArguments(0).Kind)
            Assert.AreEqual(1, CType(appliedAttribute.ConstructorArguments(0).Value, Integer))

            methodSymbol = getMethod("Method3")
            appliedAttribute = methodSymbol.GetAttributes().Single
            Assert.AreEqual(attributeSymbol, appliedAttribute.AttributeClass)
            Assert.AreEqual(TypedConstantKind.Primitive, appliedAttribute.ConstructorArguments(0).Kind)
            Assert.AreEqual(2, CType(appliedAttribute.ConstructorArguments(0).Value, Integer))
        End Sub
#End Region

#Region " Section 2 : Constructing & Updating Tree Questions "

        <FAQ(26)>
        <TestMethod>
        Public Sub AddMethodToClass()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Class C

End Class
</text>.Value)

            Dim compilationUnit = CType(tree.GetRoot(), CompilationUnitSyntax)

            ' Get ClassBlockSyntax corresponding to 'Class C' above.
            Dim classDeclaration As ClassBlockSyntax = compilationUnit.ChildNodes.OfType(Of ClassBlockSyntax).Single

            ' Construct a new MethodBlockSyntax.
            Dim newMethodDeclaration As MethodBlockSyntax = SyntaxFactory.SubBlock(SyntaxFactory.SubStatement("M"))

            ' Add this new MethodBlockSyntax to the above ClassBlockSyntax.
            Dim newClassDeclaration As ClassBlockSyntax = classDeclaration.AddMembers(newMethodDeclaration)

            ' Update the CompilationUnitSyntax with the new ClassBlockSyntax.
            Dim newCompilationUnit As CompilationUnitSyntax = compilationUnit.ReplaceNode(classDeclaration, newClassDeclaration)

            ' Format the new CompilationUnitSyntax.
            newCompilationUnit = newCompilationUnit.NormalizeWhitespace("    ")

            Dim expected =
<text>Class C

    Sub M
    End Sub
End Class
</text>.Value

            Assert.AreEqual(expected, newCompilationUnit.ToFullString().Replace(vbCrLf, vbLf))
        End Sub

        <FAQ(27)>
        <TestMethod>
        Public Sub ReplaceSubExpression()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
        Dim i As Integer = 0, j As Integer = 0
        Console.WriteLine((i + j) - (i + j))
    End Sub
End Module
</text>.Value)

            Dim compilationUnit = CType(tree.GetRoot(), CompilationUnitSyntax)

            ' Get BinaryExpressionSyntax corresponding to the two addition expressions 'i + j' above.
            Dim addExpression1 As BinaryExpressionSyntax = compilationUnit.DescendantNodes.
                                                                           OfType(Of BinaryExpressionSyntax).
                                                                           First(Function(b) b.Kind() = SyntaxKind.AddExpression)

            Dim addExpression2 As BinaryExpressionSyntax = compilationUnit.DescendantNodes.
                                                                           OfType(Of BinaryExpressionSyntax).
                                                                           Last(Function(b) b.Kind() = SyntaxKind.AddExpression)

            ' Replace addition expressions 'i + j' with multiplication expressions 'i * j'.
            Dim multipyExpression1 As BinaryExpressionSyntax =
                    SyntaxFactory.MultiplyExpression(addExpression1.Left,
                                              SyntaxFactory.Token(SyntaxKind.AsteriskToken).WithLeadingTrivia(addExpression1.OperatorToken.LeadingTrivia).
                                                                                     WithTrailingTrivia(addExpression1.OperatorToken.TrailingTrivia),
                                              addExpression1.Right)

            Dim multipyExpression2 As BinaryExpressionSyntax =
                    SyntaxFactory.MultiplyExpression(addExpression2.Left,
                                              SyntaxFactory.Token(SyntaxKind.AsteriskToken).WithLeadingTrivia(addExpression2.OperatorToken.LeadingTrivia).
                                                                                     WithTrailingTrivia(addExpression2.OperatorToken.TrailingTrivia),
                                              addExpression2.Right)

            Dim newCompilationUnit As CompilationUnitSyntax =
                compilationUnit.ReplaceNodes(nodes:={addExpression1, addExpression2},
                                             computeReplacementNode:=Function(originalNode, originalNodeWithReplacedDescendants)
                                                                         Dim newNode As SyntaxNode = Nothing
                                                                         If originalNode Is addExpression1 Then
                                                                             newNode = multipyExpression1
                                                                         ElseIf originalNode Is addExpression2 Then
                                                                             newNode = multipyExpression2
                                                                         End If

                                                                         Return newNode
                                                                     End Function)
            Assert.AreEqual(
<text>
Module Program

    Sub Main()
        Dim i As Integer = 0, j As Integer = 0
        Console.WriteLine((i * j) - (i * j))
    End Sub
End Module
</text>.Value, newCompilationUnit.ToFullString())
        End Sub

        <FAQ(28)>
        <TestMethod>
        Public Sub UseSymbolicInformationPlusRewriterToMakeCodeChanges()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

Module Program

    Sub Main()
        Dim x As New C()
        C.ReferenceEquals(x, x)
    End Sub
End Module

Class C

    Dim y As C = Nothing

    Public Sub New()
        y = New C()
    End Sub
End Class
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)

            Dim model = comp.GetSemanticModel(tree)

            ' Get the ClassBlockSyntax corresponding to 'Class C' above.
            Dim classDeclaration As ClassBlockSyntax = tree.GetRoot().DescendantNodes.
                                                                      OfType(Of ClassBlockSyntax).
                                                                      Single(Function(c) c.ClassStatement.Identifier.ToString() = "C")

            ' Get Symbol corresponding to class C above.
            Dim searchSymbol = model.GetDeclaredSymbol(classDeclaration)
            Dim oldRoot As SyntaxNode = tree.GetRoot()
            Dim rewriter = New ClassRenamer() With {.SearchSymbol = searchSymbol, .SemanticModel = model, .NewName = "C1"}
            Dim newRoot As SyntaxNode = rewriter.Visit(oldRoot)

            Assert.AreEqual(
<text>
Imports System

Module Program

    Sub Main()
        Dim x As New C1()
        C1.ReferenceEquals(x, x)
    End Sub
End Module

Class C1

    Dim y As C1 = Nothing

    Public Sub New()
        y = New C1()
    End Sub
End Class
</text>.Value, newRoot.ToFullString())
        End Sub

        ' Below VisualBasicSyntaxRewriter renames multiple occurances of a particular class name under the SyntaxNode being visited.
        ' Note that the below rewriter is not a full / correct implementation of symbolic rename. For example, it doesn't
        ' handle aliases etc. A full implementation for symbolic rename would be more complicated and is
        ' beyond the scope of this sample. The intent of this sample is mainly to demonstrate how symbolic info can be used
        ' in conjunction a rewriter to make syntactic changes.
        Public Class ClassRenamer
            Inherits VisualBasicSyntaxRewriter
            Public Property SearchSymbol As ITypeSymbol

            Public Property SemanticModel As SemanticModel

            Public Property NewName As String

            ' Replace old ClassStatementSyntax with new one.
            Public Overrides Function VisitClassStatement(node As ClassStatementSyntax) As SyntaxNode

                Dim updatedClassStatement = CType(MyBase.VisitClassStatement(node), ClassStatementSyntax)

                ' Get TypeSymbol corresponding to the ClassBlockSyntax and check whether
                ' it is the same as the TypeSymbol we are searching for.
                Dim classSymbol = SemanticModel.GetDeclaredSymbol(node)
                If classSymbol.Equals(SearchSymbol) Then

                    ' Replace the identifier token containing the name of the class.
                    Dim updatedIdentifierToken As SyntaxToken =
                            SyntaxFactory.Identifier(updatedClassStatement.Identifier.LeadingTrivia, NewName, updatedClassStatement.Identifier.TrailingTrivia)

                    updatedClassStatement = updatedClassStatement.WithIdentifier(updatedIdentifierToken)
                End If

                Return updatedClassStatement
            End Function

            ' Replace all occurances of old class name with new one.
            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                Dim updatedIdentifierName = CType(MyBase.VisitIdentifierName(node), IdentifierNameSyntax)

                ' Get TypeSymbol corresponding to the IdentifierNameSyntax and check whether
                ' it is the same as the TypeSymbol we are searching for.
                Dim identifierSymbol = SemanticModel.GetSymbolInfo(node).Symbol

                ' Handle Dim x As |C| = New C().
                Dim isMatchingTypeName = identifierSymbol.Equals(SearchSymbol)

                ' Handle Dim x As C = New |C|().
                Dim isMatchingConstructor = TypeOf identifierSymbol Is IMethodSymbol AndAlso
                                            CType(identifierSymbol, IMethodSymbol).MethodKind = MethodKind.Constructor AndAlso
                                            identifierSymbol.ContainingSymbol.Equals(SearchSymbol)

                If isMatchingTypeName OrElse isMatchingConstructor Then

                    ' Replace the identifier token containing the name of the class.
                    Dim updatedIdentifierToken As SyntaxToken = SyntaxFactory.Identifier(updatedIdentifierName.Identifier.LeadingTrivia, NewName, updatedIdentifierName.Identifier.TrailingTrivia)

                    updatedIdentifierName = updatedIdentifierName.WithIdentifier(updatedIdentifierToken)
                End If

                Return updatedIdentifierName
            End Function
        End Class

        <FAQ(30)>
        <TestMethod>
        Public Sub DeleteAssignmentStatementsFromASyntaxTree()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
        Dim x As Integer = 1
        x = 2
        If True Then
            x = 3
        Else
            x = 4
        End If
    End Sub
End Module
</text>.Value)

            Dim oldRoot As SyntaxNode = tree.GetRoot()
            ' If the assignment statement has a parent block, it is ok to remove the assignment statement completely.
            ' However, if the parent context is some statement like a single-line if statement without a block,
            ' removing the assignment statement would result in the parent statement becoming incomplete and
            ' would produce code that doesn't compile - so we leave this case unhandled.
            Dim nodesToRemove = From node In oldRoot.DescendantNodes().OfType(Of AssignmentStatementSyntax)()
                                Where IsBlock(node.Parent)
            Dim newRoot As SyntaxNode = oldRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)
            Assert.AreEqual(<text>
Module Program

    Sub Main()
        Dim x As Integer = 1
        If True Then
        Else
        End If
    End Sub
End Module
</text>.Value, newRoot.ToFullString())
        End Sub

        Private Shared Function IsBlock(node As SyntaxNode) As Boolean
            If node IsNot Nothing Then
                If TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is DoLoopBlockSyntax OrElse
                   TypeOf node Is ForOrForEachBlockSyntax OrElse
                   TypeOf node Is MultiLineLambdaExpressionSyntax Then

                    Return True
                End If

                Select Case node.Kind
                    Case SyntaxKind.WhileBlock,
                         SyntaxKind.UsingBlock,
                         SyntaxKind.SyncLockBlock,
                         SyntaxKind.WithBlock,
                         SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.ElseBlock,
                         SyntaxKind.TryBlock,
                         SyntaxKind.CatchBlock,
                         SyntaxKind.FinallyBlock,
                         SyntaxKind.CaseBlock

                        Return True
                End Select
            End If

            Return False
        End Function

        <FAQ(31)>
        <TestMethod>
        Public Sub ConstructArrayType()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
    End Sub
End Module
</text>.Value)

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={tree}, references:={Mscorlib}, options:=vbOptions)
            Dim elementType = comp.GetSpecialType(SpecialType.System_Int32)

            Dim arrayType = comp.CreateArrayTypeSymbol(elementType, rank:=3)
            Assert.AreEqual("Integer(*,*,*)", arrayType.ToDisplayString())
        End Sub

        <FAQ(32)>
        <TestMethod>
        Public Sub DeleteRegionsUsingRewriter()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

#Region " Program "
Module Program

    Sub Main()
    End Sub
End Module
#End Region

#Region " Other "
Class C

End Class
#End Region</text>.Value)

            Dim oldRoot As SyntaxNode = tree.GetRoot()
            Dim expected =
<text>
Imports System

Module Program

    Sub Main()
    End Sub
End Module

Class C

End Class
</text>.Value

            Dim rewriter As VisualBasicSyntaxRewriter = New RegionRemover1()
            Dim newRoot As SyntaxNode = rewriter.Visit(oldRoot)

            Assert.AreEqual(expected, newRoot.ToFullString())

            rewriter = New RegionRemover2()
            newRoot = rewriter.Visit(oldRoot)

            Assert.AreEqual(expected, newRoot.ToFullString())
        End Sub

        ' Below VisualBasicSyntaxRewriter removes all #Regions and #End Regions from under the SyntaxNode being visited.
        Public Class RegionRemover1
            Inherits VisualBasicSyntaxRewriter

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim updatedTrivia As SyntaxTrivia = MyBase.VisitTrivia(trivia)
                Dim directiveTrivia = TryCast(trivia.GetStructure(), DirectiveTriviaSyntax)
                If directiveTrivia IsNot Nothing Then
                    If directiveTrivia.Kind() = SyntaxKind.RegionDirectiveTrivia OrElse directiveTrivia.Kind() = SyntaxKind.EndRegionDirectiveTrivia Then
                        updatedTrivia = Nothing
                    End If
                End If

                Return updatedTrivia
            End Function
        End Class

        ' Below VisualBasicSyntaxRewriter removes all #Regions and #End Regions from under the SyntaxNode being visited.
        Public Class RegionRemover2
            Inherits VisualBasicSyntaxRewriter

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                ' Remove all #Regions and #End Regions from underneath the token.
                Return token.WithLeadingTrivia(RemoveRegions(token.LeadingTrivia)).
                             WithTrailingTrivia(RemoveRegions(token.TrailingTrivia))
            End Function

            Private Function RemoveRegions(oldTriviaList As SyntaxTriviaList) As SyntaxTriviaList
                Return SyntaxFactory.TriviaList(From trivia In oldTriviaList
                                                Where trivia.Kind() <> SyntaxKind.RegionDirectiveTrivia AndAlso trivia.Kind() <> SyntaxKind.EndRegionDirectiveTrivia)
            End Function
        End Class

        <FAQ(33)>
        <TestMethod>
        Public Sub DeleteRegions()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Imports System

#Region " Program "
Module Program

    Sub Main()
    End Sub
End Module
#End Region

#Region " Other "
Class C

End Class
#End Region</text>.Value)

            Dim oldRoot As SyntaxNode = tree.GetRoot()

            ' Get all RegionDirective and EndRegionDirective trivia.
            Dim trivia As IEnumerable(Of SyntaxTrivia) =
                    From t In oldRoot.DescendantTrivia
                    Where t.Kind() = SyntaxKind.RegionDirectiveTrivia OrElse
                          t.Kind() = SyntaxKind.EndRegionDirectiveTrivia

            Dim newRoot As SyntaxNode =
                    oldRoot.ReplaceTrivia(trivia:=trivia,
                                          computeReplacementTrivia:=Function(originalTrivia, originalTriviaWithReplacedDescendants) Nothing)
            Assert.AreEqual(
<text>
Imports System

Module Program

    Sub Main()
    End Sub
End Module

Class C

End Class
</text>.Value, newRoot.ToFullString())
        End Sub

        <FAQ(34)>
        <TestMethod>
        Public Sub InsertLoggingStatements()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
<text>
Module Program

    Sub Main()
        System.Console.WriteLine()
        Dim total As Integer = 0
        For i As Integer = 0 To 4
            total += i
        Next

        If True Then 
            total += 5
        End If
    End Sub
End Module
</text>.Value)

            Dim oldRoot As CompilationUnitSyntax = tree.GetCompilationUnitRoot()
            Dim rewriter = New ConsoleWriteLineInserter()
            Dim newRoot = CType(rewriter.Visit(oldRoot), CompilationUnitSyntax)
            newRoot = newRoot.NormalizeWhitespace() ' normalize all the whitespace to make it legible
            Dim newTree = tree.WithRootAndOptions(newRoot, tree.Options)
            Dim vbRuntime = MetadataReference.CreateFromFile(GetType(CompilerServices.StandardModuleAttribute).Assembly.Location)
            Dim comp = VisualBasicCompilation.Create("MyCompilation", syntaxTrees:={newTree}, references:={Mscorlib, vbRuntime})
            Dim output As String = Execute(comp)

            Assert.AreEqual(
<text>
0
1
3
6
10
15
</text>.Value, output.Replace(vbCrLf, vbLf))
        End Sub

        ' Below VisualBasicSyntaxRewriter inserts a Console.WriteLine() statement to print the value of the
        ' LHS variable for compound assignement statements encountered in the input tree.
        Public Class ConsoleWriteLineInserter
            Inherits VisualBasicSyntaxRewriter

            Public Overrides Function VisitAssignmentStatement(node As AssignmentStatementSyntax) As SyntaxNode
                Dim updatedNode As SyntaxNode = MyBase.VisitAssignmentStatement(node)

                If IsBlock(node.Parent) AndAlso
                    (node.Kind() = SyntaxKind.AddAssignmentStatement OrElse
                     node.Kind() = SyntaxKind.SubtractAssignmentStatement OrElse
                     node.Kind() = SyntaxKind.MultiplyAssignmentStatement OrElse
                     node.Kind() = SyntaxKind.DivideAssignmentStatement) Then

                    ' Print value of the variable on the 'Left' side of
                    ' compound assignement statements encountered.
                    Dim consoleWriteLineStatement As StatementSyntax =
                            SyntaxFactory.ParseExecutableStatement(String.Format("System.Console.WriteLine({0})", node.Left.ToString()))
#If True Then
                    Dim statementPair =
                        SyntaxFactory.List(Of StatementSyntax)({
                            node.WithLeadingTrivia().WithTrailingTrivia(),
                            consoleWriteLineStatement})
#Else
                    Dim statementPair =
                        SyntaxFactory.SeparatedList(Of StatementSyntax)(
                            node.WithLeadingTrivia().WithTrailingTrivia(),
                            SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken, vbLf),
                            consoleWriteLineStatement,
                            SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken, vbLf))
#End If

                    updatedNode =
                        SyntaxFactory.MultiLineIfBlock(
                            SyntaxFactory.IfStatement(
                                SyntaxFactory.Token(SyntaxKind.IfKeyword),
                                SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword)),
                                SyntaxFactory.Token(SyntaxKind.ThenKeyword)
                            ),
                            statementPair,
                            Nothing,
                            Nothing).
                        WithLeadingTrivia(node.GetLeadingTrivia()).
                        WithTrailingTrivia(node.GetTrailingTrivia()) ' Attach leading and trailing trivia (that we removed from the original node above) to the updated node.
                End If

                Return updatedNode
            End Function
        End Class

        ' A simple helper to execute the code present inside a compilation.
        Public Function Execute(comp As Compilation) As String
            Dim output = New StringBuilder()
            Dim exeFilename As String = "OutputVB.exe", pdbFilename As String = "OutputVB.pdb", xmlCommentsFilename As String = "OutputVB.xml"
            Dim emitResult As Microsoft.CodeAnalysis.Emit.EmitResult = Nothing

            Using ilStream = New FileStream(exeFilename, FileMode.OpenOrCreate),
                  pdbStream = New FileStream(pdbFilename, FileMode.OpenOrCreate),
                  xmlCommentsStream = New FileStream(xmlCommentsFilename, FileMode.OpenOrCreate)
                ' Emit IL, PDB and xml documentation comments for the compilation to disk.
                emitResult = comp.Emit(ilStream, pdbStream, xmlCommentsStream)
            End Using

            If emitResult.Success Then
                Dim p = Process.Start(New ProcessStartInfo() With {.FileName = exeFilename, .UseShellExecute = False, .RedirectStandardOutput = True})
                output.Append(p.StandardOutput.ReadToEnd())
                p.WaitForExit()
            Else
                output.AppendLine("Errors:")
                For Each diag In emitResult.Diagnostics
                    output.AppendLine(diag.ToString())
                Next

            End If

            Return output.ToString()
        End Function

        Private Class SimplifyAnnotationRewriter
            Inherits VisualBasicSyntaxRewriter

            Private Function AnnotateNodeWithSimplifyAnnotation(node As SyntaxNode) As SyntaxNode
                Return node.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation)
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                Return AnnotateNodeWithSimplifyAnnotation(node)
            End Function

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Return AnnotateNodeWithSimplifyAnnotation(node)
            End Function

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                Return AnnotateNodeWithSimplifyAnnotation(node)
            End Function

            Public Overrides Function VisitGenericName(node As GenericNameSyntax) As SyntaxNode
                Return AnnotateNodeWithSimplifyAnnotation(node)
            End Function
        End Class

        <FAQ(35)>
        <TestMethod>
        Public Sub UseServices()
            Dim source =
<text>Imports System.Diagnostics
Imports System
Imports System.IO

Namespace NS

Public Class C

End Class
End Namespace

Module Program

    Public Sub Main()
        Dim i As System.Int32 = 0
                System.Console.WriteLine(i.ToString())
        Dim p As Process = Process.GetCurrentProcess()
            Console.WriteLine(p.Id)
    End Sub
End Module
</text>.Value

            Dim _projectId = ProjectId.CreateNewId()
            Dim _documentId = DocumentId.CreateNewId(_projectId)

            Dim systemReference = AppDomain.CurrentDomain.GetAssemblies().Where(Function(x) String.Equals(x.GetName().Name, "System", StringComparison.OrdinalIgnoreCase)).
                Select(Function(a) MetadataReference.CreateFromFile(a.Location)).Single()

            Dim vbOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithEmbedVbCoreRuntime(True)

            Dim sln = New AdhocWorkspace().CurrentSolution.
                          AddProject(_projectId, "MyProject", "MyProject", LanguageNames.VisualBasic).WithProjectCompilationOptions(_projectId, vbOptions).
                              AddMetadataReference(_projectId, Mscorlib).
                              AddMetadataReference(_projectId, systemReference).
                              AddDocument(_documentId, "MyFile.vb", source)

            ' Format the document.
            Dim document = sln.GetDocument(_documentId)
            document = Formatter.FormatAsync(document).Result

            Assert.AreEqual(
<text>Imports System.Diagnostics
Imports System
Imports System.IO

Namespace NS

    Public Class C

    End Class
End Namespace

Module Program

    Public Sub Main()
        Dim i As System.Int32 = 0
        System.Console.WriteLine(i.ToString())
        Dim p As Process = Process.GetCurrentProcess()
        Console.WriteLine(p.Id)
    End Sub
End Module
</text>.Value, document.GetSyntaxRootAsync().Result.ToString().Replace(vbCrLf, vbLf))

            ' Simplify names used in the document i.e. remove unnecessary namespace qualifiers.
            Dim newRoot = New SimplifyAnnotationRewriter().Visit(DirectCast(document.GetSyntaxRootAsync().Result, SyntaxNode))
            document = document.WithSyntaxRoot(newRoot)
            document = Simplifier.ReduceAsync(document).Result

            Assert.AreEqual(
<text>Imports System.Diagnostics
Imports System
Imports System.IO

Namespace NS

    Public Class C

    End Class
End Namespace

Module Program

    Public Sub Main()
        Dim i As Integer = 0
        Console.WriteLine(i.ToString())
        Dim p As Process = Process.GetCurrentProcess()
        Console.WriteLine(p.Id)
    End Sub
End Module
</text>.Value, document.GetSyntaxRootAsync().Result.ToString().Replace(vbCrLf, vbLf))
        End Sub

#End Region

    End Class
End Namespace
