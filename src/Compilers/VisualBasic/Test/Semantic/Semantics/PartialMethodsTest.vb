' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class PartialMethodsTest
        Inherits BasicTestBase

        <Fact()>
        Public Sub MergePartialMethodAndParameterSymbols()

            Dim text1 = <text>
Imports System

Partial Public Class C1
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Private Sub M(i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.Equal("Private Sub M(i As Integer)", methodImpl.ToDisplayString())

            Assert.Same(methodDecl, methodImpl.PartialDefinitionPart)
            Assert.Null(methodImpl.PartialImplementationPart)
            Assert.Null(methodDecl.PartialDefinitionPart)

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim pType01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim pType02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Assert.NotEqual(pType01, pType02)

            Dim ptSym01 = model1.GetDeclaredSymbol(pType01)
            Dim ptSym02 = model2.GetDeclaredSymbol(pType02)
            Assert.Same(ptSym01, ptSym02)
            Assert.Equal(2, ptSym01.Locations.Length)

            Dim pMethod01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Dim pMethod02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Assert.NotEqual(pMethod01, pMethod02)

            Dim pmSym01 = model1.GetDeclaredSymbol(pMethod01)
            Dim pmSym02 = model2.GetDeclaredSymbol(pMethod02)
            Assert.NotSame(pmSym01, pmSym02)

            Assert.Same(methodDecl, pmSym01)
            Assert.Same(methodImpl, pmSym02)

            Assert.Equal(1, pmSym01.Locations.Length)
            Assert.Equal(1, pmSym02.Locations.Length)

            Dim pParam01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim pParam02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Assert.NotEqual(pParam01, pParam02)

            Dim ppSym01 = model1.GetDeclaredSymbol(pParam01)
            Dim ppSym02 = model2.GetDeclaredSymbol(pParam02)
            Assert.NotSame(ppSym01, ppSym02)
            Assert.Equal(1, ppSym01.Locations.Length)
            Assert.Equal(1, ppSym02.Locations.Length)
        End Sub

        <Fact()>
        Public Sub MergePartialMethodAndTypeParameterSymbols()

            Dim text1 = <text>
Imports System

Partial Public Class C1
    Partial Private Sub M(Of T)(i As T)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Private Sub M(Of T)(i As T)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(Of T)(i As T)", methodDecl.ToDisplayString())

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.Equal("Private Sub M(Of T)(i As T)", methodImpl.ToDisplayString())

            Assert.Same(methodDecl, methodImpl.PartialDefinitionPart)
            Assert.Null(methodImpl.PartialImplementationPart)
            Assert.Null(methodDecl.PartialDefinitionPart)

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim pType01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim pType02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Assert.NotEqual(pType01, pType02)

            Dim ptSym01 = model1.GetDeclaredSymbol(pType01)
            Dim ptSym02 = model2.GetDeclaredSymbol(pType02)
            Assert.Same(ptSym01, ptSym02)
            Assert.Equal(2, ptSym01.Locations.Length)

            Dim pMethod01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Dim pMethod02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Assert.NotEqual(pMethod01, pMethod02)

            Dim pmSym01 = model1.GetDeclaredSymbol(pMethod01)
            Dim pmSym02 = model2.GetDeclaredSymbol(pMethod02)
            Assert.NotSame(pmSym01, pmSym02)

            Assert.Same(methodDecl, pmSym01)
            Assert.Same(methodImpl, pmSym02)

            Assert.Equal(1, pmSym01.Locations.Length)
            Assert.Equal(1, pmSym02.Locations.Length)

            Dim pTypeParam01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()
            Dim pTypeParam02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()
            Assert.NotEqual(pTypeParam01, pTypeParam02)

            Dim ppSym01 = model1.GetDeclaredSymbol(pTypeParam01)
            Dim ppSym02 = model2.GetDeclaredSymbol(pTypeParam02)
            Assert.NotSame(ppSym01, ppSym02)
            Assert.Equal(1, ppSym01.Locations.Length)
            Assert.Equal(1, ppSym02.Locations.Length)
        End Sub

        <Fact()>
        Public Sub ConflictingPartialMethodDeclarations()

            Dim text1 = <text>
Imports System

Partial Public Class C1
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Partial Private Sub M(i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(2, methods.Length)

            Dim methodDecl1 = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl1)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl1.ToDisplayString())
            Assert.Null(methodDecl1.PartialImplementationPart)
            Assert.Null(methodDecl1.PartialDefinitionPart)

            Dim methodDecl2 = TryCast(methods(1), MethodSymbol)
            Assert.NotNull(methodDecl2)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl2.ToDisplayString())
            Assert.Null(methodDecl2.PartialImplementationPart)
            Assert.Null(methodDecl2.PartialDefinitionPart)

            Assert.NotSame(methodDecl1, methodDecl2)

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim pType01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim pType02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Assert.NotEqual(pType01, pType02)

            Dim ptSym01 = model1.GetDeclaredSymbol(pType01)
            Dim ptSym02 = model2.GetDeclaredSymbol(pType02)
            Assert.Same(ptSym01, ptSym02)
            Assert.Equal(2, ptSym01.Locations.Length)

            Dim pMethod01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Dim pMethod02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Assert.NotEqual(pMethod01, pMethod02)

            Dim pmSym01 = model1.GetDeclaredSymbol(pMethod01)
            Dim pmSym02 = model2.GetDeclaredSymbol(pMethod02)
            Assert.NotSame(pmSym01, pmSym02)

            Assert.Same(methodDecl1, pmSym01)
            Assert.Same(methodDecl2, pmSym02)

            Assert.Equal(1, pmSym01.Locations.Length)
            Assert.Equal(1, pmSym02.Locations.Length)

            Dim pParam01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim pParam02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Assert.NotEqual(pParam01, pParam02)

            Dim ppSym01 = model1.GetDeclaredSymbol(pParam01)
            Dim ppSym02 = model2.GetDeclaredSymbol(pParam02)
            Assert.NotSame(ppSym01, ppSym02)
            Assert.Equal(1, ppSym01.Locations.Length)
            Assert.Equal(1, ppSym02.Locations.Length)
        End Sub

        <WorkItem(545469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545469")>
        <Fact()>
        Public Sub MetadataNameOnPartialMethodImplementation()

            Dim text1 = <text>
Imports System

Public Module Module1
    Partial Private Sub GOo(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
    End Sub
    Private Sub goo(Of T As Class, U As T, V As {U, Exception})(aa As T, y As U, z As V)
        Console.WriteLine("goo")
    End Sub
    Sub Main()
    End Sub
End Module
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim comp = CreateCompilationWithMscorlib40({tree1})
            Dim model = comp.GetSemanticModel(tree1)

            Dim id = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodStatementSyntax).
                                                       Where(Function(node) node.Identifier.ValueText = "goo").First()

            Dim method = model.GetDeclaredSymbol(id)
            Dim unused = method.MetadataName
        End Sub

        <Fact()>
        Public Sub ConflictingPartialMethodImplementations()

            Dim text1 = <text>
Imports System

Partial Public Class C1
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Private Sub M(i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim text3 = <text>
Imports System

Partial Public Class C1
    Private Sub M(i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim tree3 = ParseAndVerify(text3)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2, tree3})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())
            Assert.Equal(3, pTypeSym.Locations.Length)

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(2, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.Equal("Private Sub M(i As Integer)", methodImpl.ToDisplayString())

            Assert.Same(methodDecl, methodImpl.PartialDefinitionPart)
            Assert.Null(methodImpl.PartialImplementationPart)
            Assert.Null(methodDecl.PartialDefinitionPart)

            Dim methodImplBad = TryCast(methods(1), MethodSymbol)
            Assert.NotNull(methodImplBad)
            Assert.Equal("Private Sub M(i As Integer)", methodImplBad.ToDisplayString())

            Assert.NotSame(methodDecl, methodImplBad)
            Assert.NotSame(methodImpl, methodImplBad)
            Assert.Null(methodImplBad.PartialImplementationPart)
            Assert.Null(methodImplBad.PartialDefinitionPart)

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim pType01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim pType02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Assert.NotEqual(pType01, pType02)
            Dim model3 = comp.GetSemanticModel(tree3)
            Dim pType03 = tree3.GetCompilationUnitRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().First()
            Assert.NotEqual(pType01, pType03)
            Assert.NotEqual(pType02, pType03)

            Dim ptSym01 = model1.GetDeclaredSymbol(pType01)
            Dim ptSym02 = model2.GetDeclaredSymbol(pType02)
            Dim ptSym03 = model2.GetDeclaredSymbol(pType02)
            Assert.Same(pTypeSym, ptSym01)
            Assert.Same(pTypeSym, ptSym02)
            Assert.Same(pTypeSym, ptSym03)

            Dim pMethod01 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Dim pMethod02 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Dim pMethod03 = tree3.GetCompilationUnitRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
            Assert.NotEqual(pMethod01, pMethod02)
            Assert.NotEqual(pMethod01, pMethod03)
            Assert.NotEqual(pMethod03, pMethod02)

            Dim pmSym01 = model1.GetDeclaredSymbol(pMethod01)
            Dim pmSym02 = model2.GetDeclaredSymbol(pMethod02)
            Dim pmSym03 = model3.GetDeclaredSymbol(pMethod03)
            Assert.NotSame(pmSym01, pmSym02)
            Assert.NotSame(pmSym01, pmSym03)
            Assert.NotSame(pmSym03, pmSym02)

            Assert.Same(methodDecl, pmSym01)
            Assert.Same(methodImpl, pmSym02)
            Assert.Same(methodImplBad, pmSym03)

            Assert.Equal(1, pmSym01.Locations.Length)
            Assert.Equal(1, pmSym02.Locations.Length)
        End Sub

        <Fact()>
        Public Sub MergeMethodAttributes()

            Dim text1 = <text>
Imports System
Imports System.Reflection

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class A1
    Inherits Attribute
End Class

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=True)&gt;
Class A2
    Inherits Attribute
End Class

Partial Public Class C1
    &lt;A1(), A2()&gt;
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    &lt;A2()&gt;
    Private Sub M(i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())

            Dim attributes = methodDecl.GetAttributes()
            Assert.Equal(3, attributes.Length)

        End Sub

        <Fact()>
        Public Sub MergeMethodParametersAttributes()

            Dim text1 = <text>
Imports System
Imports System.Reflection

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class A1
    Inherits Attribute
End Class

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=True)&gt;
Class A2
    Inherits Attribute
End Class

Partial Public Class C1
    Partial Private Sub M(&lt;A1(), A2()&gt; i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Private Sub M(&lt;A2()&gt; i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())
            Assert.Equal(1, methodDecl.ParameterCount)

            Dim parameter = TryCast(methodDecl.Parameters(0), ParameterSymbol)
            Assert.NotNull(parameter)
            Assert.Equal("i As Integer", parameter.ToDisplayString())

            Dim attributes = parameter.GetAttributes()
            Assert.Equal(3, attributes.Length)

        End Sub

        <Fact()>
        Public Sub MergeMethodParametersAttributes2()

            Dim text1 = <text>
Imports System
Imports System.Reflection

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class A1
    Inherits Attribute
End Class

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=True)&gt;
Class A2
    Inherits Attribute
End Class

Partial Public Class C1
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Public Class C1
    Private Sub M(&lt;A1(), A2()&gt; i As Integer)
        Console.WriteLine()
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2})

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())
            Assert.Equal(1, methodDecl.ParameterCount)

            Dim parameter = TryCast(methodDecl.Parameters(0), ParameterSymbol)
            Assert.NotNull(parameter)
            Assert.Equal("i As Integer", parameter.ToDisplayString())

            Dim attributes = parameter.GetAttributes()
            Assert.Equal(2, attributes.Length)

        End Sub

        <Fact()>
        Public Sub MergeMethodParametersAttributes_NoDuplication()

            Dim text1 = <text><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class A1
    Inherits Attribute
End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Class A2
    Inherits Attribute
End Class

Partial Public Class C1
    Partial Private Sub M(<A2()> i As Integer)
    End Sub
End Class
]]>
                        </text>.Value.Trim()

            Dim text2 = <text><![CDATA[
Imports System

Partial Public Class C1
    Private Sub M(<A1(), A1(), A2()> i As Integer)
        Console.WriteLine()
    End Sub
End Class
]]>
                        </text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal("Private Sub M(i As Integer)", methodDecl.ToDisplayString())
            Assert.Equal(1, methodDecl.ParameterCount)

            Dim parameter = TryCast(methodDecl.Parameters(0), ParameterSymbol)
            Assert.NotNull(parameter)
            Assert.Equal("i As Integer", parameter.ToDisplayString())

            Dim attributes = parameter.GetAttributes()
            Assert.Equal(4, attributes.Length)

            parameter = TryCast(methodDecl.PartialImplementationPart.Parameters(0), ParameterSymbol)
            Assert.NotNull(parameter)
            Assert.Equal("i As Integer", parameter.ToDisplayString())

            attributes = parameter.GetAttributes()
            Assert.Equal(4, attributes.Length)

            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidMultipleAttributeUsage1, "A1()").WithArguments("A1"))

        End Sub

        <Fact>
        Public Sub MergeMethodParametersAttributesComplexVsSimple()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Reflection

Class A1
    Inherits Attribute
End Class

Class A2
    Inherits Attribute
End Class

Partial Public Class X
    Partial Private Sub SimpleDefSimpleImpl(i As Integer)
    End Sub

    Private Sub SimpleDefSimpleImpl(i As Integer)
    End Sub

    Partial Private Sub SimpleDefComplexImpl(i As Integer)
    End Sub

    Private Sub SimpleDefComplexImpl(<A2> i As Integer)
    End Sub

    Partial Private Sub ComplexDefSimpleImpl(<A1>i As Integer)
    End Sub

    Private Sub ComplexDefSimpleImpl(i As Integer)
    End Sub

    Partial Private Sub ComplexDefComplexImpl(<A1>i As Integer)
    End Sub

    Private Sub ComplexDefComplexImpl(<A2>i As Integer)
    End Sub
End Class
]]>
    </file>
</compilation>
            ' first access def then impl
            CompileAndVerify(source, sourceSymbolValidator:=
                Sub(m)
                    Dim x = DirectCast(m.GlobalNamespace.GetMember("X"), TypeSymbol)
                    Dim SimpleDefSimpleImpl = DirectCast(x.GetMembers("SimpleDefSimpleImpl").Single(), MethodSymbol)
                    Dim ComplexDefSimpleImpl = DirectCast(x.GetMembers("ComplexDefSimpleImpl").Single(), MethodSymbol)
                    Dim SimpleDefComplexImpl = DirectCast(x.GetMembers("SimpleDefComplexImpl").Single(), MethodSymbol)
                    Dim ComplexDefComplexImpl = DirectCast(x.GetMembers("ComplexDefComplexImpl").Single(), MethodSymbol)

                    Assert.Equal(0, SimpleDefSimpleImpl.Parameters(0).GetAttributes().Length)
                    Assert.Equal(0, SimpleDefSimpleImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)

                    Assert.Equal(1, ComplexDefSimpleImpl.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, ComplexDefSimpleImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)

                    Assert.Equal(1, SimpleDefComplexImpl.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, SimpleDefComplexImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)

                    Assert.Equal(2, ComplexDefComplexImpl.Parameters(0).GetAttributes().Length)
                    Assert.Equal(2, ComplexDefComplexImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)
                End Sub)

            ' first access impl then def
            CompileAndVerify(source, sourceSymbolValidator:=
                Sub(m)
                    Dim x = DirectCast(m.GlobalNamespace.GetMember("X"), TypeSymbol)
                    Dim SimpleDefSimpleImpl = DirectCast(x.GetMembers("SimpleDefSimpleImpl").Single(), MethodSymbol)
                    Dim ComplexDefSimpleImpl = DirectCast(x.GetMembers("ComplexDefSimpleImpl").Single(), MethodSymbol)
                    Dim SimpleDefComplexImpl = DirectCast(x.GetMembers("SimpleDefComplexImpl").Single(), MethodSymbol)
                    Dim ComplexDefComplexImpl = DirectCast(x.GetMembers("ComplexDefComplexImpl").Single(), MethodSymbol)

                    Assert.Equal(0, SimpleDefSimpleImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)
                    Assert.Equal(0, SimpleDefSimpleImpl.Parameters(0).GetAttributes().Length)

                    Assert.Equal(1, ComplexDefSimpleImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, ComplexDefSimpleImpl.Parameters(0).GetAttributes().Length)

                    Assert.Equal(1, SimpleDefComplexImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)
                    Assert.Equal(1, SimpleDefComplexImpl.Parameters(0).GetAttributes().Length)

                    Assert.Equal(2, ComplexDefComplexImpl.PartialImplementationPart.Parameters(0).GetAttributes().Length)
                    Assert.Equal(2, ComplexDefComplexImpl.Parameters(0).GetAttributes().Length)
                End Sub)
        End Sub

        <WorkItem(544502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544502")>
        <Fact()>
        Public Sub MergePartialMethodEvents()
            CompileAndVerify(
<compilation name="MergePartialMethodEvents">
    <file name="a.vb">
Imports System

Partial Class C1
    Private Event E1(int As Integer)
    Private Event E2(int As Integer)

    Partial Private Sub M(i As Integer) Handles Me.E1, Me.E2
    End Sub

    Sub Raise()
        RaiseEvent E1(1)
        RaiseEvent E2(2)
        RaiseEvent E3(3)
    End Sub

    Shared Sub Main()
        Call New C1().Raise()
    End Sub
End Class
    </file>
    <file name="b.vb">
Imports System

Partial Class C1
    Private Event E3(int As Integer)
    Private Sub M(i As Integer) Handles Me.E2, Me.E3
        Console.Write(i.ToString())
        Console.Write(";")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="1;2;2;3;")
        End Sub

        <WorkItem(544502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544502")>
        <Fact()>
        Public Sub MergePartialMethodEvents2()
            CompileAndVerify(
<compilation name="MergePartialMethodEvents2">
    <file name="a.vb">
Imports System

Partial Class C1
    Private Sub M(i As Integer) Handles Me.E2, Me.E3
        Console.Write(i.ToString())
        Console.Write(";")
    End Sub

    Private Event E1(int As Integer)
    Private Event E2(int As Integer)

    Sub Raise()
        RaiseEvent E1(1)
        RaiseEvent E2(2)
        RaiseEvent E3(3)
    End Sub

    Shared Sub Main()
        Call New C1().Raise()
    End Sub
End Class
    </file>
    <file name="b.vb">
Imports System

Partial Class C1
    Private Event E3(int As Integer)
    Partial Private Sub M(i As Integer) Handles Me.E1, Me.E2
    End Sub 
End Class
    </file>
</compilation>,
expectedOutput:="1;2;2;3;")
        End Sub

        <WorkItem(544502, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544502")>
        <Fact()>
        Public Sub MergePartialMethodEvents3()
            CompileAndVerify(
<compilation name="MergePartialMethodEvents3">
    <file name="a.vb">
Imports System

Partial Class c1
    Private Handler1 As Action(Of Integer)
    Private Handler2 As Action(Of Integer)
    Private Custom Event E1 As Action(Of Integer)
        AddHandler(value As Action(Of Integer))
            Handler1 = CType([Delegate].Combine(Handler1, value), Action(Of Integer))
        End AddHandler
        RemoveHandler(value As Action(Of Integer))
            Handler1 = CType([Delegate].Remove(Handler1, value), Action(Of Integer))
        End RemoveHandler
        RaiseEvent(obj As Integer)
            If Handler1 IsNot Nothing Then Handler1(1)
        End RaiseEvent
    End Event
    Private Custom Event E2 As Action(Of Integer)
        AddHandler(value As Action(Of Integer))
            Handler2 = CType([Delegate].Combine(Handler2, value), Action(Of Integer))
        End AddHandler
        RemoveHandler(value As Action(Of Integer))
            Handler2 = CType([Delegate].Remove(Handler2, value), Action(Of Integer))
        End RemoveHandler
        RaiseEvent(obj As Integer)
            If Handler2 IsNot Nothing Then Handler2(2)
        End RaiseEvent
    End Event

    Partial Private Sub M(i As Integer) Handles Me.E1, Me.E2
    End Sub

    Sub Raise()
        RaiseEvent E1(1)
        RaiseEvent E2(2)
        RaiseEvent E3(3)
    End Sub

    Shared Sub Main()
        Call New c1().Raise()
    End Sub
End Class

Partial Class C1
    Private Event E3(int As Integer)
    Private Sub M(i As Integer) Handles Me.E2, Me.E3
        Console.Write(i.ToString())
        Console.Write(";")
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="1;2;2;3;")
        End Sub

        <Fact()>
        Public Sub MergePartialMethodWithEvents()
            CompileAndVerify(
<compilation name="MergePartialMethodWithEvents">
    <file name="a.vb">
Imports System
Class C
    Public Event E1(int As Integer)
    Public Event E2(int As Integer)
    Public Event E3(int As Integer)

    Sub Raise()
        RaiseEvent E1(1)
        RaiseEvent E2(2)
        RaiseEvent E3(3)
    End Sub
End Class
Partial Class C1
    WithEvents e As New C
    Private Sub M(i As Integer) Handles e.E2, e.E3
        Console.Write(i.ToString())
        Console.Write(";")
    End Sub
    Sub Test()
        Call e.Raise()
    End Sub
    Shared Sub Main()
        Dim x = New C1
        x.Test()
    End Sub
End Class
    </file>
    <file name="b.vb">
Imports System

Partial Class C1
    Partial Private Sub M(i As Integer) Handles e.E1, e.E2
    End Sub
End Class
    </file>
</compilation>,
expectedOutput:="1;2;2;3;")
        End Sub

        <Fact()>
        Public Sub ExplicitInterfaceDiagnosticsNotDuplicated()

            Dim text1 = <text>
Public Interface A
    Sub B()
End Interface

Partial Public Class C1
    Partial Private Sub M(i As Integer)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Partial Public Class C1
    Private Sub M(i As Integer) Implements A.B
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single()
            Assert.Equal("C1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)
            Assert.Equal(0, methodDecl.ExplicitInterfaceImplementations.Length)

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.Equal(0, methodImpl.ExplicitInterfaceImplementations.Length)

            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_InterfaceNotImplemented1, "A").WithArguments("A"))
        End Sub

        <WorkItem(544432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544432")>
        <Fact()>
        Public Sub TestCaseWithTypeArgumentsAndConstraints()
            Dim text1 = <text>
Imports System
Module Module1
    Partial Private Sub M(Of A As Class, U As A, V As {U, Exception}, W As Structure)(aa As A, uu As U, vv As V(), ww As W?)
    End Sub
    Private Sub M(Of A As Class, U As A, V As {U, Exception}, W As Structure)(aa As A, uu As U, vv As V(), ww As W?)
    End Sub
    Sub Main()
    End Sub
End Module
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim comp = CreateCompilationWithMscorlib40({tree1}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("Module1").Single()
            Assert.Equal("Module1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.NotSame(methodDecl, methodImpl)
            Assert.Same(methodDecl.PartialImplementationPart, methodImpl)
            Assert.Same(methodImpl.PartialDefinitionPart, methodDecl)
        End Sub

        <WorkItem(544445, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544445")>
        <Fact()>
        Public Sub TestCaseWithTypeArgumentsAndConstraints2()
            Dim text1 = <text>
Imports System

Module Module1
    Sub Main()
    End Sub
    Partial Private Sub M(Of T As {Exception,}, U As T, V)()
    End Sub
    Private Sub M(Of T As {Exception}, U As T, V)()
    End Sub
End Module
</text>.Value.Trim()

            Dim tree1 = VisualBasicSyntaxTree.ParseText(text1)
            Dim comp = CreateCompilationWithMscorlib40({tree1}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("Module1").Single()
            Assert.Equal("Module1", pTypeSym.ToDisplayString())

            Dim methods = pTypeSym.GetMembers("M")
            Assert.Equal(1, methods.Length)

            Dim methodDecl = TryCast(methods(0), MethodSymbol)
            Assert.NotNull(methodDecl)

            Dim methodImpl = methodDecl.PartialImplementationPart
            Assert.NotNull(methodImpl)
            Assert.NotSame(methodDecl, methodImpl)
            Assert.Same(methodDecl.PartialImplementationPart, methodImpl)
            Assert.Same(methodImpl.PartialDefinitionPart, methodDecl)
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolsForParamsAndTypeParams()
            'This is to make sure that the issues from C# bugs 12468 and 12819 don't repro in VB.
            Dim text1 = <text>
Imports System

Partial Class C1
    Partial Private Shared Sub M(Of U As {T, Exception}, T)(x As T)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Class C1
    Private Shared Sub M(Of U As {Exception, T}, T)(x As T)
    End Sub
    Shared Function Bar() As Integer
        Return 1
    End Function
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim root1 = tree1.GetCompilationUnitRoot()
            Dim root2 = tree1.GetCompilationUnitRoot()
            Dim para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim typePara1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()
            Dim typePara2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()

            Dim paraSym1 = model1.GetDeclaredSymbol(para1)
            Dim paraSym2 = model2.GetDeclaredSymbol(para2)
            Assert.NotNull(paraSym1)
            Assert.NotNull(paraSym2)
            Assert.Equal("x As T", paraSym1.ToTestDisplayString())
            Assert.Equal("x As T", paraSym2.ToTestDisplayString())
            Assert.NotEqual(paraSym1.Locations(0), paraSym2.Locations(0))

            Dim typeParaSym1 = model1.GetDeclaredSymbol(typePara1)
            Dim typeParaSym2 = model2.GetDeclaredSymbol(typePara2)
            Assert.NotNull(typeParaSym1)
            Assert.NotNull(typeParaSym2)
            Assert.Equal("U", typeParaSym1.ToTestDisplayString)
            Assert.Equal(2, typeParaSym1.ConstraintTypes.Length)
            Assert.Equal("U", typeParaSym2.ToTestDisplayString)
            Assert.Equal(2, typeParaSym2.ConstraintTypes.Length)
            Assert.NotEqual(typeParaSym1.Locations(0), typeParaSym2.Locations(0))
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolsForParamsAndTypeParams2()
            'This is to make sure that the issues from C# bugs 12468 and 12819 don't repro in VB.
            Dim text1 = <text>
Imports System

Partial Class C1
    Partial Private Shared Sub M(Of U As {T, Exception}, T)(x As T)
    End Sub
End Class
</text>.Value.Trim()

            Dim text2 = <text>
Imports System

Partial Class C1
    Shared Function Bar() As Integer
        Return 1
    End Function
    Private Shared Sub M(Of U As {Exception, T}, T)(x As T)
    End Sub
End Class
</text>.Value.Trim()

            Dim tree1 = ParseAndVerify(text1)
            Dim tree2 = ParseAndVerify(text2)
            Dim comp = CreateCompilationWithMscorlib40({tree1, tree2}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim model1 = comp.GetSemanticModel(tree1)
            Dim model2 = comp.GetSemanticModel(tree2)
            Dim root1 = tree1.GetCompilationUnitRoot()
            Dim root2 = tree1.GetCompilationUnitRoot()
            Dim para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of ParameterSyntax)().First()
            Dim typePara1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()
            Dim typePara2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType(Of TypeParameterSyntax)().First()

            Dim paraSym1 = model1.GetDeclaredSymbol(para1)
            Dim paraSym2 = model2.GetDeclaredSymbol(para2)
            Assert.NotNull(paraSym1)
            Assert.NotNull(paraSym2)
            Assert.Equal("x As T", paraSym1.ToTestDisplayString())
            Assert.Equal("x As T", paraSym2.ToTestDisplayString())
            Assert.NotEqual(paraSym1.Locations(0), paraSym2.Locations(0))

            Dim typeParaSym1 = model1.GetDeclaredSymbol(typePara1)
            Dim typeParaSym2 = model2.GetDeclaredSymbol(typePara2)
            Assert.NotNull(typeParaSym1)
            Assert.NotNull(typeParaSym2)
            Assert.Equal("U", typeParaSym1.ToTestDisplayString)
            Assert.Equal(2, typeParaSym1.ConstraintTypes.Length)
            Assert.Equal("U", typeParaSym2.ToTestDisplayString)
            Assert.Equal(2, typeParaSym2.ConstraintTypes.Length)
            Assert.NotEqual(typeParaSym1.Locations(0), typeParaSym2.Locations(0))
        End Sub

        <WorkItem(544499, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544499")>
        <Fact()>
        Public Sub GetMembersForMustoverridePartialMethod()
            'TODO: Please add any additional verification below if necessary.
            Dim text = <text>
Partial Class C1
    Partial Mustoverride Private Sub M()
    End Sub
End Class
</text>.Value.Trim()
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim comp = CreateCompilationWithMscorlib40({tree}, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim typeSym = comp.GetTypeByMetadataName("C1")
            Assert.NotNull(typeSym)

            Dim methodSym = typeSym.GetMembers("M").Single()
            Assert.NotNull(methodSym)
        End Sub

        <WorkItem(921704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/921704")>
        <Fact()>
        Public Sub Bug921704()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Partial Private Sub BeforeInitializeComponent(ByRef isInitialized as Boolean)
    End Sub
    Private Sub BeforeInitializeComponent(ByRef isInitialized as Boolean)
        isInitialized = true
    End Sub
End Module
    </file>
</compilation>)
        End Sub

    End Class
End Namespace
