' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class AnonymousTypesSemanticsTests : Inherits BasicTestBase

        <Fact()>
        Public Sub AnonymousTypeSymbolsTest01()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolsTest01">
        <file name="a.vb">
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)

        Dim v1 As Object = [#0 New With { .aa = 1, 
                                      .BB = "", 
                                      .CCC = new SSS() } 0#]

        Dim v2 As Object = [#1 New With { 
                                      .AA = new SSS(), 
                                      .bb = 123.456, 
                                      .ccc = [#2 New With { .Aa = 123, 
                                                        .Bb = "", 
                                                        .CcC = new SSS() }  2#] } 1#]

    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(3, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal(1, info0.Type.Locations.Length)
            Assert.Equal(info0.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span)

            Dim info1 = model.GetSemanticInfoSummary(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal(1, info1.Type.Locations.Length)
            Assert.Equal(info1.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span)

            Dim info2 = model.GetSemanticInfoSummary(DirectCast(nodes(2), ExpressionSyntax))
            Assert.Equal(1, info2.Type.Locations.Length)
            Assert.Equal(info2.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 5).Span)

            Assert.Equal(info0.Type, info2.Type)
            Assert.NotEqual(info0.Type, info1.Type)

            CheckFieldNameAndLocation(model, info0.Type, tree, 1, "aa")
            CheckFieldNameAndLocation(model, info0.Type, tree, 2, "BB")
            CheckFieldNameAndLocation(model, info0.Type, tree, 3, "CCC")

            CheckFieldNameAndLocation(model, info1.Type, tree, 5, "AA")
            CheckFieldNameAndLocation(model, info1.Type, tree, 7, "bb")
            CheckFieldNameAndLocation(model, info1.Type, tree, 8, "ccc")

            CheckFieldNameAndLocation(model, info2.Type, tree, 9, "Aa")
            CheckFieldNameAndLocation(model, info2.Type, tree, 10, "Bb")
            CheckFieldNameAndLocation(model, info2.Type, tree, 11, "CcC")

        End Sub

        <WorkItem(543829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543829")>
        <Fact()>
        Public Sub AnonymousTypeSymbolWithExplicitNew()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolWithExplicitNew">
        <file name="a.vb">
Module ModuleA
    Sub Test1()
        Dim q = New With { .y = 2 }
        Dim x = New With { .Y = 5 }
        Dim z = x
    End Sub
End Module
        </file>
    </compilation>
            Dim text As String = compilationDef.Value.Replace(vbLf, vbCrLf)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(compilationDef, {SystemRef, SystemCoreRef, MsvbRef})
            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            ' check 'q'
            Dim posQ As Integer = text.IndexOf("q"c)
            Dim declaratorQ = tree.GetRoot().FindToken(posQ).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeExplicit(model,
                                       DirectCast(model.GetDeclaredSymbol(declaratorQ.Names(0)), LocalSymbol),
                                       DirectCast(declaratorQ.Initializer.Value, AnonymousObjectCreationExpressionSyntax))

            ' check 'x'
            Dim posX = text.IndexOf("x"c)
            Dim declaratorX = tree.GetRoot().FindToken(posX).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeExplicit(model,
                                       DirectCast(model.GetDeclaredSymbol(declaratorX.Names(0)), LocalSymbol),
                                       DirectCast(declaratorX.Initializer.Value, AnonymousObjectCreationExpressionSyntax))

            ' check 'z' --> 'x'
            Dim posZ = text.IndexOf("z"c)
            Dim declaratorZ = tree.GetRoot().FindToken(posZ).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeExplicit(model,
                                       DirectCast(model.GetDeclaredSymbol(declaratorZ.Names(0)), LocalSymbol),
                                       DirectCast(declaratorX.Initializer.Value, AnonymousObjectCreationExpressionSyntax))

        End Sub

        Private Sub CheckAnonymousTypeExplicit(model As SemanticModel, local As LocalSymbol, anonObjectCreation As AnonymousObjectCreationExpressionSyntax)
            Dim localType = local.Type
            Assert.True(localType.IsAnonymousType)

            ' IsImplicitlyDeclared: Return false. The new { } clause 
            '                       serves as the declaration.
            Assert.False(localType.IsImplicitlyDeclared)

            ' DeclaringSyntaxNodes: Return the AnonymousObjectCreationExpression from the particular 
            '                       anonymous type definition that flowed to this usage.
            Assert.Equal(1, localType.DeclaringSyntaxReferences.Length)
            Assert.Same(anonObjectCreation, localType.DeclaringSyntaxReferences(0).GetSyntax())

            ' SemanticModel.GetDeclaredSymbol: Return this symbol when applied to the 
            '                                  AnonymousObjectCreationExpression in the new { } declaration.
            Dim symbol = model.GetDeclaredSymbol(anonObjectCreation)
            Assert.NotNull(symbol)
            Assert.Equal(Of ISymbol)(localType, symbol)
            Assert.Same(anonObjectCreation, symbol.DeclaringSyntaxReferences(0).GetSyntax())

            ' Locations: Return the Span of that particular 
            '            AnonymousObjectCreationExpression's NewKeyword.
            Assert.Equal(1, localType.Locations.Length)
            Assert.Equal(localType.Locations(0), anonObjectCreation.NewKeyword.GetLocation())

            ' Members check
            Dim propIndex As Integer = 0
            For Each member In localType.GetMembers()
                If member.Kind = SymbolKind.Property Then

                    ' Equals: Return true when comparing same-named members of 
                    '         structurally-equivalent anonymous type symbols.
                    Dim members = symbol.GetMembers(member.Name)
                    Assert.Equal(1, members.Length)
                    Assert.Equal(member, members(0))

                    ' IsImplicitlyDeclared: Return false. The goo = bar clause in 
                    '                       the new { } clause serves as the declaration.
                    Assert.False(member.IsImplicitlyDeclared)

                    ' DeclaringSyntaxNodes: Return the AnonymousObjectMemberDeclarator from the 
                    '                       particular property definition that flowed to this usage.
                    Dim propertyInitializer = anonObjectCreation.Initializer.Initializers(propIndex)
                    Assert.Equal(1, member.DeclaringSyntaxReferences.Length)
                    Assert.Same(propertyInitializer, member.DeclaringSyntaxReferences(0).GetSyntax())

                    ' SemanticModel.GetDeclaredSymbol: Return this symbol when applied to its new { } 
                    '                                  declaration's AnonymousObjectMemberDeclarator.
                    Dim propSymbol = model.GetDeclaredSymbol(propertyInitializer)
                    Assert.Equal(Of ISymbol)(member, propSymbol)
                    Assert.Same(propertyInitializer, propSymbol.DeclaringSyntaxReferences(0).GetSyntax())

                    ' Locations: Return the Span of that particular 
                    '            AnonymousObjectMemberDeclarator's IdentifierToken.
                    Assert.Equal(1, member.Locations.Length)
                    Assert.Equal(member.Locations(0), DirectCast(propertyInitializer, NamedFieldInitializerSyntax).Name.GetLocation())

                    propIndex += 1
                End If
            Next
        End Sub

        <WorkItem(543829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543829")>
        <Fact()>
        Public Sub AnonymousTypeSymbolImplicit()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolImplicit">
        <file name="a.vb">
Option Infer On

Imports System
Imports System.Linq
Imports System.Collections.Generic

Module ModuleA
    Sub Test1()
        Dim a = {1, 2, 3}
        Dim qf = From x In a
                 Select x, y = x + 2
 
        Dim ql = From x In a
                 Select x, y = x + 2

        Dim zf = qf.First()
        Dim zl = ql.Last()

        Dim w = New With { Key .x = 5, Key .y = 2 }
    End Sub
End Module
        </file>
    </compilation>
            Dim text As String = compilationDef.Value.Replace(vbLf, vbCrLf)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(compilationDef, {SystemRef, SystemCoreRef, MsvbRef})
            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            ' calculate offsets
            Dim x1 = text.IndexOf("x"c)
            Dim x2 = text.IndexOf("x"c, x1 + 1)
            Dim x3 = text.IndexOf("x"c, x2 + 1)
            Dim x4 = text.IndexOf("x"c, x3 + 1)
            Dim x5 = text.IndexOf("x"c, x4 + 1)
            Dim y1 = text.IndexOf("y"c)
            Dim y2 = text.IndexOf("y"c, y1 + 1)
            Dim y3 = text.IndexOf("y"c, y2 + 1)
            Dim y4 = text.IndexOf("y"c, y3 + 1)
            Dim y5 = text.IndexOf("y"c, y4 + 1)
            Dim select1 = text.IndexOf("Select", StringComparison.Ordinal)
            Dim select2 = text.IndexOf("Select", select1 + 1, StringComparison.Ordinal)

            ' get 'other' type
            Dim posW As Integer = text.IndexOf("w"c)
            Dim declaratorW = tree.GetRoot().FindToken(posW).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim typeW = DirectCast(model.GetDeclaredSymbol(declaratorW.Names(0)), LocalSymbol).Type

            ' check 'zf'
            Dim posZF As Integer = text.IndexOf("zf", StringComparison.Ordinal)
            Dim declaratorZF = tree.GetRoot().FindToken(posZF).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorZF.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(select1).GetLocation,
                                       typeW,
                                       True,
                                       tree.GetRoot().FindToken(x2 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y4 - 2).GetLocation())

            ' check 'zk'
            Dim posZL As Integer = text.IndexOf("zl", StringComparison.Ordinal)
            Dim declaratorZL = tree.GetRoot().FindToken(posZL).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorZL.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(select2).GetLocation,
                                       DirectCast(model.GetDeclaredSymbol(declaratorZF.Names(0)), LocalSymbol).Type,
                                       True,
                                       tree.GetRoot().FindToken(x5 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y5 - 2).GetLocation())

        End Sub

        <WorkItem(543829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543829")>
        <Fact()>
        Public Sub AnonymousDelegateSymbolImplicit()
            Dim compilationDef =
    <compilation name="AnonymousDelegateSymbolImplicit">
        <file name="a.vb">
Option Infer On

Imports System
Imports System.Linq
Imports System.Collections.Generic

Module ModuleA
    Sub Test1()
        Dim del_a = Sub(x As Integer, yy As String)
                    End Sub
        Dim del_b = Sub(x As Integer, yy As String)
                    End Sub
        Dim del_t = Sub(x As Integer, yy As String)
                    End Sub
    End Sub
End Module
        </file>
    </compilation>
            Dim text As String = compilationDef.Value.Replace(vbLf, vbCrLf)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(compilationDef, {SystemRef, SystemCoreRef, MsvbRef})
            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            ' calculate offsets
            Dim x1 = text.IndexOf("x"c)
            Dim x2 = text.IndexOf("x"c, x1 + 1)
            Dim x3 = text.IndexOf("x"c, x2 + 1)
            Dim y1 = text.IndexOf("yy", StringComparison.Ordinal)
            Dim y2 = text.IndexOf("yy", y1 + 1, StringComparison.Ordinal)
            Dim y3 = text.IndexOf("yy", y2 + 1, StringComparison.Ordinal)
            Dim sub1 = text.IndexOf("Sub", StringComparison.Ordinal)
            Dim sub2 = text.IndexOf("Sub", sub1 + 1, StringComparison.Ordinal)
            Dim sub3 = text.IndexOf("Sub", sub2 + 1, StringComparison.Ordinal)
            Dim sub4 = text.IndexOf("Sub", sub3 + 1, StringComparison.Ordinal)

            ' get 'model' type
            Dim posT As Integer = text.IndexOf("del_t", StringComparison.Ordinal)
            Dim declaratorT = tree.GetRoot().FindToken(posT).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim typeT = DirectCast(model.GetDeclaredSymbol(declaratorT.Names(0)), LocalSymbol).Type

            ' check 'a'
            Dim posA As Integer = text.IndexOf("del_a", StringComparison.Ordinal)
            Dim declaratorA = tree.GetRoot().FindToken(posA).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorA.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(sub2).Parent.Parent.GetLocation,
                                       typeT,
                                       False,
                                       tree.GetRoot().FindToken(x1 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y1 - 2).GetLocation())

            ' check 'b'
            Dim posB As Integer = text.IndexOf("del_b", StringComparison.Ordinal)
            Dim declaratorB = tree.GetRoot().FindToken(posB).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorB.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(sub4).Parent.Parent.GetLocation,
                                       DirectCast(model.GetDeclaredSymbol(declaratorA.Names(0)), LocalSymbol).Type,
                                       False,
                                       tree.GetRoot().FindToken(x2 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y2 - 2).GetLocation())

        End Sub

        <WorkItem(543829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543829")>
        <Fact()>
        Public Sub AnonymousDelegateSymbolImplicit2()
            Dim compilationDef =
    <compilation name="AnonymousDelegateSymbolImplicit2">
        <file name="a.vb">
Option Infer On

Imports System
Imports System.Linq
Imports System.Collections.Generic

Module ModuleA
    Function Test1() As Integer
        Dim del_a = Sub(x As Integer, yy As String) Console.WriteLine()
        Dim del_b = Sub(x As Integer, yy As String) Console.WriteLine()
        Dim del_t = Sub(x As Integer, yy As String) Console.WriteLine()
        Return 0
    End Function
End Module
        </file>
    </compilation>
            Dim text As String = compilationDef.Value.Replace(vbLf, vbCrLf)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(compilationDef, {SystemRef, SystemCoreRef, MsvbRef})
            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            ' calculate offsets
            Dim x1 = text.IndexOf("x"c)
            Dim x2 = text.IndexOf("x"c, x1 + 1)
            Dim x3 = text.IndexOf("x"c, x2 + 1)
            Dim y1 = text.IndexOf("yy", StringComparison.Ordinal)
            Dim y2 = text.IndexOf("yy", y1 + 1, StringComparison.Ordinal)
            Dim y3 = text.IndexOf("yy", y2 + 1, StringComparison.Ordinal)
            Dim sub1 = text.IndexOf("Sub", StringComparison.Ordinal)
            Dim sub2 = text.IndexOf("Sub", sub1 + 1, StringComparison.Ordinal)

            ' get 'model' type
            Dim posT As Integer = text.IndexOf("del_t", StringComparison.Ordinal)
            Dim declaratorT = tree.GetRoot().FindToken(posT).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim typeT = DirectCast(model.GetDeclaredSymbol(declaratorT.Names(0)), LocalSymbol).Type

            ' check 'a'
            Dim posA As Integer = text.IndexOf("del_a", StringComparison.Ordinal)
            Dim declaratorA = tree.GetRoot().FindToken(posA).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorA.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(sub1).Parent.Parent.GetLocation,
                                       typeT,
                                       False,
                                       tree.GetRoot().FindToken(x1 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y1 - 2).GetLocation())

            ' check 'b'
            Dim posB As Integer = text.IndexOf("del_b", StringComparison.Ordinal)
            Dim declaratorB = tree.GetRoot().FindToken(posB).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim delegateA = DirectCast(model.GetDeclaredSymbol(declaratorA.Names(0)), LocalSymbol).Type
            CheckAnonymousTypeImplicit(DirectCast(model.GetDeclaredSymbol(declaratorB.Names(0)), LocalSymbol),
                                       tree.GetRoot().FindToken(sub2).Parent.Parent.GetLocation,
                                       delegateA,
                                       False,
                                       tree.GetRoot().FindToken(x2 - 2).GetLocation(),
                                       tree.GetRoot().FindToken(y2 - 2).GetLocation())

            Assert.IsType(Of AnonymousTypeManager.AnonymousDelegatePublicSymbol)(delegateA)
            Assert.False(DirectCast(delegateA, INamedTypeSymbol).IsSerializable)
        End Sub

        Private Sub CheckAnonymousTypeImplicit(local As LocalSymbol, location As Location, anotherType As TypeSymbol, isType As Boolean, ParamArray locations() As Location)
            Dim localType = local.Type
            Assert.True(localType.IsAnonymousType)

            ' IsImplicitlyDeclared: Return true. The new { } clause is 
            '                       a reference to an implicit declaration.
            Assert.True(localType.IsImplicitlyDeclared)

            ' DeclaringSyntaxNodes: Return a list with no nodes.
            Assert.Equal(0, localType.DeclaringSyntaxReferences.Length)

            ' Locations: Return the Span of that particular 
            '            AnonymousObjectCreationExpression's NewKeyword.
            Assert.Equal(1, localType.Locations.Length)
            Assert.Equal(location.SourceSpan, localType.Locations(0).SourceSpan)

            ' Members check
            Dim propIndex As Integer = 0
            Dim membersQuery = If(isType,
                                  localType.GetMembers(),
                                  DirectCast(localType.GetMembers()(3), MethodSymbol).Parameters.As(Of Symbol)())

            For Each member In membersQuery
                If member.Kind = If(isType, SymbolKind.Property, SymbolKind.Parameter) Then

                    ' Equals: Return true when comparing same-named members of 
                    '         structurally-equivalent anonymous type symbols.
                    Dim members = If(isType,
                                     anotherType.GetMembers(member.Name),
                                     ImmutableArray.CreateRange(Of Symbol)(
                                         From mem In DirectCast(localType.GetMembers()(3), MethodSymbol).Parameters
                                         Select mem
                                         Where mem.Name = member.Name))

                    Assert.Equal(1, members.Length)
                    Assert.Equal(member, members(0))

                    ' IsImplicitlyDeclared: Return true. The goo = bar clause initializes 
                    '                       a property on the implicitly-declared type.
                    Assert.True(member.IsImplicitlyDeclared)

                    ' DeclaringSyntaxNodes: Return a list with no nodes.
                    Assert.Equal(0, member.DeclaringSyntaxReferences.Length)

                    ' Locations: Return the Span of that particular ExpressionRangeVariable's 
                    '            ModifiedIdentifier (if the property name is specified), or 
                    '            IdentifierName (if the property name is inferred).
                    Assert.Equal(1, member.Locations.Length)
                    Assert.Equal(locations(propIndex).SourceSpan, member.Locations(0).SourceSpan)

                    propIndex += 1
                End If
            Next

            Assert.Equal(locations.Count, propIndex)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeSymbolsTest01a()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolsTest01a">
        <file name="a.vb">
Option Infer On
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)

        Dim CcC As Object = [#0 New With { .aa = 1, 
                                      .BB = "", 
                                      ModuleA.cCc } 0#]

        Dim v2 As Object = [#1 New With { 
                                      .AA = new SSS(), 
                                      .bb = 123.456, 
                                      .ccc = [#2 New With { .Aa = 123, 
                                                        .Bb = "", 
                                                        CcC }  2#] } 1#]

    End Sub
    Public Readonly Property cCc As Object
        Get
            Return Nothing
        End Get
    End Property
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(3, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal(1, info0.Type.Locations.Length)
            Assert.Equal(info0.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span)

            Dim info1 = model.GetSemanticInfoSummary(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal(1, info1.Type.Locations.Length)
            Assert.Equal(info1.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span)

            Dim info2 = model.GetSemanticInfoSummary(DirectCast(nodes(2), ExpressionSyntax))
            Assert.Equal(1, info2.Type.Locations.Length)
            Assert.Equal(info2.Type.Locations(0).SourceSpan, tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 4).Span)

            Assert.Equal(info0.Type, info2.Type)
            Assert.NotEqual(info0.Type, info1.Type)

            CheckFieldNameAndLocation(model, info0.Type, tree, 1, "aa")
            CheckFieldNameAndLocation(model, info0.Type, tree, 2, "BB")
            CheckFieldNameAndLocation(model, info0.Type, tree, 4, "cCc")

            CheckFieldNameAndLocation(model, info1.Type, tree, 5, "AA")
            CheckFieldNameAndLocation(model, info1.Type, tree, 7, "bb")
            CheckFieldNameAndLocation(model, info1.Type, tree, 8, "ccc")

            CheckFieldNameAndLocation(model, info2.Type, tree, 9, "Aa")
            CheckFieldNameAndLocation(model, info2.Type, tree, 10, "Bb")
            CheckFieldNameAndLocation(model, info2.Type, tree, 11, "CcC")

        End Sub

        <Fact()>
        Public Sub AnonymousTypeSymbolsTest02()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolsTest02">
        <file name="a.vb">
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)

        dim tmp = [#0 (New With { Key .aa = 1, .BB = "", .CCC = new SSS() }).Equals(
                            (New With { .AA = new SSS(), .ccc = New With { Key .Aa = 123, .Bb = "", .CcC = new SSS() } }).ccc) 0#]

    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Overloads Function Equals(val As <anonymous type: Key aa As Integer, BB As String, CCC As ModuleA.SSS>) As Boolean", info0.Symbol.ToDisplayString())

        End Sub

        <Fact()>
        Public Sub AnonymousTypeSymbolsTest03()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolsTest03">
        <file name="a.vb">
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        dim tmp = [#0 (New With { Key .aa = 1, .BB = "", .CCC = new SSS() }).Equals(
                            (New With { .AA = new SSS(), .ccc = New With { .Aa = 123, .Bb = "", .CcC = new SSS() } }).ccc) 0#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Overrides Function Equals(obj As Object) As Boolean", info0.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeKeywordAsMemberIdentifier()
            Dim compilationDef =
    <compilation name="AnonymousTypeKeywordAsMemberIdentifier">
        <file name="a.vb">
Imports System

Namespace NS
    Public Class Test

        Shared field As Object = Nothing
        Friend Property P As Double

        Protected Function F() As String
            Return Nothing
        End Function

        Public Sub Test()
            Dim at = [#0 New With { .if = 123.456!, 
                                .try = 111&amp;,
                                .next = [#1  New With {.Dim = F, Key .Key = NS.Test.field, .Imports = P} 1#] } 0#]
        End Sub

    End Class
End Namespace
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(2, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)
            Dim expr2 = DirectCast(nodes(1), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New([if] As Single, [try] As Long, [next] As <anonymous type: Dim As String, Key Key As Object, Imports As Double>)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(0, typeInfo.Type.Interfaces.Length)

            typeInfo = model.GetTypeInfo(expr2)
            Assert.Equal("Public Sub New([Dim] As String, Key As Object, [Imports] As Double)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(1, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeReferenceInExpression()
            Dim compilationDef =
    <compilation name="AnonymousTypeReferenceInExpression">
        <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module M

    <Extension()>
    Function ExtF(ByVal o As Object) As Object
        Return Nothing
    End Function

    Sub Goo(o As String)
        Dim at = [#0 New With {.f1 = o.ExtF, Key .f2 = .f1, .f3 = DirectCast(.f2, Integer)} 0#]
    End Sub

End Module
       ]]></file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New(f1 As Object, f2 As Object, f3 As Integer)", typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(1, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact(), WorkItem(542245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542245")>
        Public Sub AnonymousTypeReferenceLambdas()
            Dim compilationDef =
    <compilation name="AnonymousTypeReferenceLambda">
        <file name="at.vb">
Imports System

delegate Function D1() As Boolean
delegate Function D2() As String
Friend Module AM
    Sub Main()
        Dim at1 As [#0 New With {.module = DirectCast(Function() As Boolean
                                          Return -1
                                      End Function, D1),
                                 key .Main = .Module()
                                } 0#]
        Dim at2 = [#1 New With {.mid = at1, .mod = New With { Key .mod = DirectCast(Function() As String
                                                        Return 123.ToString()
                                                    End Function, D2)}} 1#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal(2, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)
            Dim expr2 = DirectCast(nodes(1), ExpressionSyntax)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New([module] As D1, Main As Boolean)", typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(1, typeInfo.Type.Interfaces.Length)
            Dim mems = typeInfo.Type.GetMembers()

            typeInfo = model.GetTypeInfo(expr2)
            Assert.Equal("Public Sub New(mid As <anonymous type: module As D1, Key Main As Boolean>, [mod] As <anonymous type: Key mod As D2>)", typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(0, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeProjectioninitializer01()
            Dim compilationDef =
    <compilation name="AnonymousTypeProjectioninitializer01">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class Test

    Shared GF As IList(Of Object) = Nothing
    Protected Function F() As String
        Return Nothing
    End Function

    Public Sub Test(p As Object())
        Dim local As Char = "q"c
        Dim at = [#0 New With {local, GF, p, Me.F()} 0#]
    End Sub

End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New(local As Char, GF As System.Collections.Generic.IList(Of Object), p As Object(), F As String)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(0, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeProjectioninitializer01a()
            Dim compilationDef =
    <compilation name="AnonymousTypeProjectioninitializer01a">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Public Class Base
    Protected ReadOnly Property Prop As Single
        Get
            Return 0.0!
        End Get
    End Property
End Class

Public Class Test
    Inherits Base

    Public Sub Test(p As Object())
        Dim at = [#0 New With {MyBase.Prop} 0#]
    End Sub

End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New(Prop As Single)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(0, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeProjectioninitializer02()
            Dim compilationDef =
    <compilation name="AnonymousTypeProjectioninitializer">
        <file name="a.vb">
Imports Sys = System
Imports Opt = System.IO.SearchOption

Structure GS(Of T)
    Public Shared SF As T
End Structure

Public Class Test

    Public Sub Test(p As Object())
        Dim at = [#0 New With {Key SYS.Console.OpenStandardOutput(), Key GS(Of Integer).sf, Key opt.TopDirectoryOnly} 0#]
    End Sub

End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New(OpenStandardOutput As System.IO.Stream, sf As Integer, TopDirectoryOnly As System.IO.SearchOption)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(1, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeAsFieldInitializers()
            Dim compilationDef =
    <compilation name="AnonymousTypeAsFieldInitializers">
        <file name="a1.vb">
Imports Sys = System
Class C
    Dim at As Object = [#0 New With {f1, .f2 = f2 + f1.Length, .Idx = f3(1),
        .nested = [#1 New With {Key f3, C.aprop, Me!Q} 1#] } 0#]
    Dim f1 As String
    Dim f2 As Byte = 127
    Dim f3() As Char = New Char() {"A"c, "B"c, "C"c}

    Public ReadOnly Default Property Idx(p As String) As String
        Get
            Return p &amp; p
        End Get
    End Property
    Friend Shared ReadOnly Property aprop() As String
        Get
            Return "wello horld"
        End Get
    End Property
End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(2, nodes.Count)
            Dim expr1 = DirectCast(nodes(0), ExpressionSyntax)
            Dim expr2 = DirectCast(nodes(1), ExpressionSyntax)

            Dim model = compilation.GetSemanticModel(tree)

            Dim typeInfo = model.GetTypeInfo(expr1)
            Assert.Equal("Public Sub New(f1 As String, f2 As Integer, Idx As Char, nested As <anonymous type: Key f3 As Char(), aprop As String, Q As String>)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(0, typeInfo.Type.Interfaces.Length)

            typeInfo = model.GetTypeInfo(expr2)
            Assert.Equal("Public Sub New(f3 As Char(), aprop As String, Q As String)",
                         typeInfo.Type.TheOnlyConstructor.ToDisplayString())
            Assert.Equal(1, typeInfo.Type.Interfaces.Length)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeTemplateCanNotConstruct()
            Dim compilationDef =
    <compilation name="AnonymousTypeTemplateCanNotConstruct">
        <file name="a.vb">
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        dim tmp = [#0 (New With { Key .aa = 1, .CCC = new SSS() }) 0#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Dim type = info0.Type
            Assert.Equal("<anonymous type: Key aa As Integer, CCC As ModuleA.SSS>", type.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeSymbolRoundtripTest1()
            Dim compilationDef =
    <compilation name="AnonymousTypeSymbolRoundtripTest1">
        <file name="a.vb">
Module ModuleA
    Structure SSS
    End Structure 
    Sub Test1(x As Integer)
        dim tmp = [#0 (New With { Key .aa = 1, .CCC = new SSS() }) 0#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Dim type = info0.Type
            Assert.Equal("<anonymous type: Key aa As Integer, CCC As ModuleA.SSS>",
                         type.ToDisplayString())

            Assert.Equal(1, type.Interfaces.Length)
            Dim iEquatable = type.Interfaces(0)
            Assert.Equal("System.IEquatable(Of <anonymous type: Key aa As Integer, CCC As ModuleA.SSS>)",
                         iEquatable.ToDisplayString())

            Assert.Equal(1, iEquatable.TypeArguments.Length)
            Dim typeArgument = iEquatable.TypeArguments(0)
            Assert.Equal(type, typeArgument)

            Dim equalsMethod = DirectCast(type.GetMembers("Equals").Where(
                                                Function(s) DirectCast(s, MethodSymbol).ExplicitInterfaceImplementations.Length > 0).Single(),
                                          MethodSymbol)
            Assert.Equal("Public Overloads Function Equals(val As <anonymous type: Key aa As Integer, CCC As ModuleA.SSS>) As Boolean",
                         equalsMethod.ToDisplayString())

            Dim equalsMethodParamType = equalsMethod.Parameters(0).Type
            Assert.Equal(type, equalsMethodParamType)

            Dim explicitImpMethod = equalsMethod.ExplicitInterfaceImplementations(0)
            Assert.Equal("Function Equals(other As <anonymous type: Key aa As Integer, CCC As ModuleA.SSS>) As Boolean",
                         explicitImpMethod.ToDisplayString())

            Dim explicitImpMethodType = explicitImpMethod.ContainingType
            Assert.Equal(iEquatable, explicitImpMethodType)

        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldDeclarationIdentifier()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldDeclarationIdentifier">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        dim tmp = New With { Key [#0 .aa 0#] = 1 }
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As Integer", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldDeclarationIdentifier2()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldDeclarationIdentifier2">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        dim tmp = New With { Key [#0 .aa 0#] = 1, [#1 .bb 1#] = 1 + [#2 .aa 2#] }
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As Integer", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal("Public Property bb As Integer", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(2), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As Integer", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldDeclarationIdentifier3()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldDeclarationIdentifier3">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Main(args As String())
        dim tmp = New With { [#0 .a 0#] = 1, 
                             [#1 args 1#], 
                             [#2 .b 2#] = "--" &amp; [#3 .args 3#]([#4 .a 4#]) &amp; "--" }
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Property a As Integer", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(1), InferredFieldInitializerSyntax).Expression)
            Assert.Equal("args As String()", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(2), ExpressionSyntax))
            Assert.Equal("Public Property b As String", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(3), ExpressionSyntax))
            Assert.Equal("Public Property args As String()", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(4), SimpleArgumentSyntax).Expression)
            Assert.Equal("Public Property a As Integer", info.Symbol.ToDisplayString())
        End Sub

        <WorkItem(543723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543723")>
        <Fact()>
        Public Sub AnonymousTypeFieldDeclarationIdentifier4()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldDeclarationIdentifier4">
        <file name="a.vb">
Imports System
Class Program
    Dim b1 = New With {.k2 = [#0 New [#1 Program  1#] () 0#] }
    Sub Main(args As String())
    End Sub
End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Sub New()", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal("Program", info.Symbol.ToDisplayString())
        End Sub

        <WorkItem(543723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543723")>
        <Fact()>
        Public Sub AnonymousTypePropertyDeclarationIdentifier4()
            Dim compilationDef =
    <compilation name="AnonymousTypePropertyDeclarationIdentifier4">
        <file name="a.vb">
Imports System
Class Program
    Public Property P As Object = New With {.k2 = [#0 New [#1 Program  1#] () 0#] }
    Sub Main(args As String())
    End Sub
End Class
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Sub New()", info.Symbol.ToDisplayString())

            info = model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal("Program", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldReferencedFromNestedLambda()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldReferencedFromNestedLambda">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = New With {Key .aa = 1,
                                .bb = DirectCast(Function(p As Integer) As Integer
                                                     Return p + [#0 .aa 0#]
                                                 End Function, Func(Of Integer, Integer))}
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC36549: Anonymous type property 'aa' cannot be used in the definition of a lambda expression within the same initialization list.
                                                     Return p +     .aa    
                                                                    ~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As Integer", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldReferencedFromNestedLambda2()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldReferencedFromNestedLambda2">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = New With {Key .aa = 1,
                                .bb = DirectCast(Function(p As Integer) p + [#0 .aa 0#], Func(Of Integer, Integer))}
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC36549: Anonymous type property 'aa' cannot be used in the definition of a lambda expression within the same initialization list.
                                .bb = DirectCast(Function(p As Integer) p +     .aa    , Func(Of Integer, Integer))}
                                                                                ~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As Integer", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldReferencedFromNestedQuery()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldReferencedFromNestedQuery">
        <file name="a.vb">
Imports System
Imports System.Linq
Module ModuleA
    Sub Test1()
        Dim tmp = New With {Key .aa = "abc", .bb = (From x In [#0 .aa 0#] Select x)}
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As String", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldReferencedFromNestedQuery2()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldReferencedFromNestedQuery2">
        <file name="a.vb">
Imports System
Imports System.Linq
Module ModuleA
    Sub Test1()
        Dim tmp = New With {Key .aa = "abc", .bb = (From x In [#0 .aa 0#] Select .aa)}
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC36549: Anonymous type property 'aa' cannot be used in the definition of a lambda expression within the same initialization list.
        Dim tmp = New With {Key .aa = "abc", .bb = (From x In     .aa     Select .aa)}
                                                                                 ~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public ReadOnly Property aa As String", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeFieldCreatedInQuery()
            Dim compilationDef =
    <compilation name="AnonymousTypeFieldCreatedInQuery">
        <file name="a.vb">
Imports System
Imports System.Linq
Module ModuleA
    Sub Test1()
        Dim tmp = From x In "abc" Select New With {Key [#0 x 0#], .b = [#1 x 1#], .c = [#2 .x 2#]}
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim querySymbol = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol
            Assert.Equal("x", querySymbol.ToDisplayString())
            Assert.Equal(SymbolKind.RangeVariable, querySymbol.Kind)

            querySymbol = model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax)).Symbol
            Assert.Equal("x", querySymbol.ToDisplayString())
            Assert.Equal(SymbolKind.RangeVariable, querySymbol.Kind)

            Assert.Equal("Public ReadOnly Property x As Char",
                         model.GetSymbolInfo(DirectCast(nodes(2), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbol()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbol">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = [#0 New With {Key .aa = 1,
                                    .bb = 1 + .aa} 0#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Dim info = model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)", info.Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_Variable()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_Variable">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp As [#0 New With {Key .aa = 1,
                                     .bb = 1 + .aa} 0#]
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_TypeInference()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_TypeInference">
        <file name="a.vb">
Option Infer On
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp As New With {Key .aa = 1, .bb = 1 + .aa}
        Console.WriteLine( [#0 tmp 0#] .ToString())
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("<anonymous type: Key aa As Integer, bb As Integer>",
                         model.GetTypeInfo(DirectCast(nodes(0), ExpressionSyntax)).Type.ToDisplayString())
        End Sub

        <WorkItem(542268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542268")>
        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle">
        <file name="a.vb">
Option Infer On
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp As New With {Key .aa = tmp.aa}
        Console.WriteLine( [#0 tmp 0#] .ToString())
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<error>
BC30980: Type of 'tmp' cannot be inferred from an expression containing 'tmp'.
        Dim tmp As New With {Key .aa = tmp.aa}
                                       ~~~
BC42104: Variable 'tmp' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim tmp As New With {Key .aa = tmp.aa}
                                       ~~~
</error>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("<anonymous type: Key aa As ?>",
                         model.GetTypeInfo(DirectCast(nodes(0), ExpressionSyntax)).Type.ToDisplayString())
        End Sub

        <WorkItem(542268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542268")>
        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle1()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle1">
        <file name="a.vb">
Option Infer Off
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp As New With {Key .aa = tmp.aa}
        Console.WriteLine( [#0 tmp 0#] .ToString())
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<error>
BC30980: Type of 'tmp' cannot be inferred from an expression containing 'tmp'.
        Dim tmp As New With {Key .aa = tmp.aa}
                                       ~~~
BC42104: Variable 'tmp' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim tmp As New With {Key .aa = tmp.aa}
                                       ~~~
</error>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("<anonymous type: Key aa As ?>",
                         model.GetTypeInfo(DirectCast(nodes(0), ExpressionSyntax)).Type.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle2()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_TypeInference_Cycle2">
        <file name="a.vb">
Option Infer On
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = New With {Key .aa = tmp}
        Console.WriteLine( [#0 tmp 0#] .ToString())
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<error>
BC30980: Type of 'tmp' cannot be inferred from an expression containing 'tmp'.
        Dim tmp = New With {Key .aa = tmp}
                                      ~~~
BC42104: Variable 'tmp' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim tmp = New With {Key .aa = tmp}
                                      ~~~
</error>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("<anonymous type: Key aa As ?>",
                         model.GetTypeInfo(DirectCast(nodes(0), ExpressionSyntax)).Type.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_Variable_Using()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_Variable_Using">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Using tmp As [#0 New With {Key .aa = 1,
                                       .bb = 1 + .aa} 0#]
        End Using
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC36010: 'Using' operand of type '&lt;anonymous type: Key aa As Integer, bb As Integer&gt;' must implement 'System.IDisposable'.
        Using tmp As     New With {Key .aa = 1,
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact(), WorkItem(528745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528745")>
        Public Sub AnonymousTypeCreationSymbolInAsNew_Field()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_Field">
        <file name="a.vb">
Imports System
Module ModuleA
    Public FLD As [#0 New With {Key .aa = 1,
                                    .bb = 1 + .aa} 0#]
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC30180: Keyword does not name a type.
    Public FLD As     New With {Key .aa = 1,
                          ~~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_Const()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_Const">
        <file name="a.vb">
Imports System
Module ModuleA
    Const FLD As [#0 New With {Key .aa = 1,
                                   .bb = 1 + .aa} 0#]
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC30180: Keyword does not name a type.
    Const FLD As     New With {Key .aa = 1,
                         ~~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeCreationSymbolInAsNew_Property()
            Dim compilationDef =
    <compilation name="AnonymousTypeCreationSymbolInAsNew_Property">
        <file name="a.vb">
Imports System
Module ModuleA
    Public Property FLD As [#0 New With {Key .aa = 1,
                                             .bb = 1 + .aa} 0#]
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC30180: Keyword does not name a type.
    Public Property FLD As     New With {Key .aa = 1,
                                   ~~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("Public Sub New(aa As Integer, bb As Integer)",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeInsideLambda1()
            Dim compilationDef =
    <compilation name="AnonymousTypeInsideLambda1">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = DirectCast(Sub(a As Integer)
                                 Dim local = New With {Key [#0 a 0#], [#1 .bb 1#] = 1 + [#2 .a 2#]}
                             End Sub, Action(Of Integer))
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("a As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
            Assert.Equal("Public Property bb As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax)).Symbol.ToDisplayString())
            Assert.Equal("Public ReadOnly Property a As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(2), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub AnonymousTypeInsideLambda2()
            Dim compilationDef =
    <compilation name="AnonymousTypeInsideLambda2">
        <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim tmp = DirectCast(Function(a As Integer) DirectCast(New With {Key [#0 a 0#], [#1 .bb 1#] = 1 + [#2 .a 2#]}, Object), Func(Of Integer, Object))
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Dim model = compilation.GetSemanticModel(tree)

            Assert.Equal("a As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(0), ExpressionSyntax)).Symbol.ToDisplayString())
            Assert.Equal("Public Property bb As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(1), ExpressionSyntax)).Symbol.ToDisplayString())
            Assert.Equal("Public ReadOnly Property a As Integer",
                         model.GetSymbolInfo(DirectCast(nodes(2), ExpressionSyntax)).Symbol.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub LookupSymbols1()
            Dim compilationDef =
<compilation name="LookupSymbols1">
    <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1(x As Integer)
        Dim local = [#1 New With {Key x, .y = [#0 . 0#], .z = .x} 1#] 'end
    End Sub
End Module
    </file>
</compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
  BC30203: Identifier expected.
        Dim local =     New With {Key x, .y =     .    , .z = .x}     'end
                                                       ~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Dim anonymousType = model.GetTypeInfo(DirectCast(nodes(1), ExpressionSyntax))

            Dim pos As Integer = nodes(0).Span.End
            Dim syms1 = model.LookupSymbols(pos, container:=DirectCast(anonymousType.Type, TypeSymbol))

            CompilationUtils.CheckSymbolsUnordered(syms1,
                "Public Overrides Function ToString() As String",
                "Public Overrides Function GetHashCode() As Integer",
                "Public Overloads Function Equals(val As <anonymous type: Key x As Integer, y As ?, z As Integer>) As Boolean",
                "Public Overrides Function Equals(obj As Object) As Boolean",
                "Public Overridable Overloads Function Equals(obj As Object) As Boolean",
                "Public Shared Overloads Function Equals(objA As Object, objB As Object) As Boolean",
                "Public ReadOnly Property x As Integer",
                "Public Property y As ?",
                "Public Property z As Integer",
                "Public Shared Overloads Function ReferenceEquals(objA As Object, objB As Object) As Boolean",
                "Public Overloads Function [GetType]() As System.Type")
        End Sub

        <Fact()>
        Public Sub LookupSymbols2()
            Dim compilationDef =
<compilation name="LookupSymbols2">
    <file name="a.vb">
Imports System
Module ModuleA
    Sub Test1()
        Dim local = [#1 New With {Key .x = 1, .x = [#0 2 0#]} 1#] 'end
    End Sub
End Module
    </file>
</compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC36547: Anonymous type member or property 'x' is already declared.
        Dim local =     New With {Key .x = 1, .x =     2    }     'end
                                              ~~~~~~~~~~
</errors>)
            Dim model = compilation.GetSemanticModel(tree)

            Dim anonymousType = model.GetTypeInfo(DirectCast(nodes(1), ExpressionSyntax))

            Dim pos As Integer = nodes(0).Span.End
            Dim syms1 = model.LookupSymbols(pos, container:=DirectCast(anonymousType.Type, TypeSymbol))

            CompilationUtils.CheckSymbolsUnordered(syms1,
                "Public Overrides Function ToString() As String",
                "Public Overrides Function GetHashCode() As Integer",
                "Public Overloads Function Equals(val As <anonymous type: Key x As Integer, x As Integer>) As Boolean",
                "Public Overrides Function Equals(obj As Object) As Boolean",
                "Public Overridable Overloads Function Equals(obj As Object) As Boolean",
                "Public Shared Overloads Function Equals(objA As Object, objB As Object) As Boolean",
                "Public ReadOnly Property x As Integer",
                "Public Shared Overloads Function ReferenceEquals(objA As Object, objB As Object) As Boolean",
                "Public Overloads Function [GetType]() As System.Type")
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolForAnonymousTypeProperty01()
            Dim source =
<compilation name="AnonymousTypeProperty01">
    <file name="a.vb">
Imports system

Module ModuleA

    ReadOnly Property Prop As Long
        Get
            Dim Local As Short = -1
            Dim anonType = New With {Key .ID = 123, .do = "QC", key local, Prop}  ' WRN BC42104
            Return anonType.id + anonType.Local
        End Get
    End Property
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim anonProps = tree.GetRoot().DescendantNodes().OfType(Of FieldInitializerSyntax)()
            Assert.Equal(4, anonProps.Count())
            Dim symList = From ap In anonProps Let apsym = model.GetDeclaredSymbol(ap) Order By apsym.Name Select apsym.Name
            Dim results = String.Join(", ", symList)
            Assert.Equal("do, ID, local, Prop", results)
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolForAnonymousTypeProperty02()
            Dim source =
<compilation name="AnonymousTypeProperty02">
    <file name="a.vb">
Imports System

Class C
    Private field = 111
    Sub M(p1 As Byte, ByRef p2 As SByte, ParamArray ary() As String)
        Dim local As ULong = 12345
        Dim anonType = New With {.local = locaL, Me.FielD, ary, p1, p2}
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim anonProps = tree.GetRoot().DescendantNodes().OfType(Of FieldInitializerSyntax)()
            Assert.Equal(5, anonProps.Count())
            Dim symList = From ap In anonProps Let apsym = model.GetDeclaredSymbol(ap) Order By apsym.Name Select apsym.Name
            Dim results = String.Join(", ", symList)
            Assert.Equal("ary, FielD, local, p1, p2", results)
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolForAnonymousTypeProperty03()
            Dim source =
<compilation name="AnonymousTypeProperty03">
    <file name="a.vb">
Imports System
Enum E
    a
    b
    c
End Enum

Class Base
    Protected baseField As E = E.b
    Protected Overridable ReadOnly Property BaseProp As Base
        Get
            Return Me
        End Get
    End Property
    Public deleField As Func(Of String, Char)
End Class

Class AnonTypeTest
    Inherits Base

    Protected Overrides ReadOnly Property BaseProp As Base
        Get
            Return Nothing
        End Get
    End Property

    Default ReadOnly Property Item([string] As String) As Char
        Get
            Dim anonType = New With {.id = deleField, MyBase.BaseProp, MyBase.baseField, .ret = [string]}
            Return anonType.id(anonType.ret)
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim anonProps = tree.GetRoot().DescendantNodes().OfType(Of FieldInitializerSyntax)()
            Assert.Equal(4, anonProps.Count())
            Dim symList = From ap In anonProps Let apsym = model.GetDeclaredSymbol(ap) Order By apsym.Name Select apsym.Name
            Dim results = String.Join(", ", symList)
            Assert.Equal("baseField, BaseProp, id, ret", results)
        End Sub

        <Fact()>
        Public Sub GetDeclaredSymbolForAnonymousTypeProperty04()
            Dim source =
<compilation name="AnonymousTypeProperty04">
    <file name="a.vb">
Imports System
Enum E
    a
    b
    c
End Enum

Structure S
    Public Shared sField As E
    Public Interface IGoo
    End Interface

    Public Property GetGoo As IGoo
    Public Function GetGoo2() As Action(Of UShort)
        Return Nothing
    End Function
End Structure

Class AnonTypeTest

    Function F() As Action(Of UShort)
        Dim anonType1 = New With {.a1 = New With {S.sField, .igoo = New With {New S().GetGoo}}}
        Dim anonType2 = New With {.a1 = New With {.a2 = New With {.a2 = S.sField, .a3 = New With {.a3 = New S().GetGoo2()}}}}
        Return anonType2.a1.a2.a3.a3
    End Function
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim anonProps = tree.GetRoot().DescendantNodes().OfType(Of FieldInitializerSyntax)()
            Assert.Equal(9, anonProps.Count())
            Dim symList = From ap In anonProps Let apsym = model.GetDeclaredSymbol(ap) Order By apsym.Name Select apsym.Name
            Dim results = String.Join(", ", symList)
            Assert.Equal("a1, a1, a2, a2, a3, a3, GetGoo, igoo, sField", results)
        End Sub

        <Fact>
        Public Sub SameAnonymousTypeInTwoLocations()
            ' This code declares the same anonymous type twice. Make sure the locations
            ' reflect this.
            Dim source =
<compilation name="AnonymousTypeProperty04">
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Dim a1 = New With {.id = 1, .name = "Q"}
        Dim a2 = New With {.id = 1, .name = "Q"}
        Dim a3 = a1
        Dim a4 = a2
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)

            Dim programType = DirectCast((comp.GlobalNamespace.GetTypeMembers("Module1").Single()), NamedTypeSymbol)
            Dim mainMethod = DirectCast((programType.GetMembers("Main").Single()), MethodSymbol)
            Dim mainSyntax = TryCast(mainMethod.DeclaringSyntaxReferences.Single().GetSyntax(), MethodBaseSyntax)
            Dim mainBlock = DirectCast(mainSyntax.Parent, MethodBlockSyntax)
            Dim statement1 = TryCast(mainBlock.Statements(0), LocalDeclarationStatementSyntax)
            Dim statement2 = TryCast(mainBlock.Statements(1), LocalDeclarationStatementSyntax)
            Dim statement3 = TryCast(mainBlock.Statements(2), LocalDeclarationStatementSyntax)
            Dim statement4 = TryCast(mainBlock.Statements(3), LocalDeclarationStatementSyntax)
            Dim localA3 = TryCast(model.GetDeclaredSymbol(statement3.Declarators(0).Names(0)), LocalSymbol)
            Dim localA4 = TryCast(model.GetDeclaredSymbol(statement4.Declarators(0).Names(0)), LocalSymbol)
            Dim typeA3 = localA3.Type
            Dim typeA4 = localA4.Type

            ' A3 and A4 should have different type objects, that compare equal. They should have 
            ' different locations.
            Assert.Equal(typeA3, typeA4)
            Assert.NotSame(typeA3, typeA4)
            Assert.NotEqual(typeA3.Locations(0), typeA4.Locations(0))

            ' The locations of a3's type should be the type declared in statement 1, the location
            ' of a4's type should be the type declared in statement 2.
            Assert.True(statement1.Span.Contains(typeA3.Locations(0).SourceSpan))
            Assert.True(statement2.Span.Contains(typeA4.Locations(0).SourceSpan))
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub SameAnonymousTypeInTwoLocationsWithStaticLocals()
            ' This code declares the same anonymous type twice. Make sure the locations
            ' reflect this.
            Dim source =
<compilation name="AnonymousTypeProperty04">
    <file name="a.vb">
Imports System

Module Module1
    Sub Main()
        Static a1 = New With {.id = 1, .name = "Q"}
        Static a2 = New With {.id = 1, .name = "Q"}
        Static a3 = a1
        Static a4 = a2
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)
            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)

            Dim programType = DirectCast((comp.GlobalNamespace.GetTypeMembers("Module1").Single()), NamedTypeSymbol)
            Dim mainMethod = DirectCast((programType.GetMembers("Main").Single()), MethodSymbol)
            Dim mainSyntax = TryCast(mainMethod.DeclaringSyntaxReferences.Single().GetSyntax(), MethodBaseSyntax)
            Dim mainBlock = DirectCast(mainSyntax.Parent, MethodBlockSyntax)
            Dim statement1 = TryCast(mainBlock.Statements(0), LocalDeclarationStatementSyntax)
            Dim statement2 = TryCast(mainBlock.Statements(1), LocalDeclarationStatementSyntax)
            Dim statement3 = TryCast(mainBlock.Statements(2), LocalDeclarationStatementSyntax)
            Dim statement4 = TryCast(mainBlock.Statements(3), LocalDeclarationStatementSyntax)
            Dim localA3 = TryCast(model.GetDeclaredSymbol(statement3.Declarators(0).Names(0)), LocalSymbol)
            Dim localA4 = TryCast(model.GetDeclaredSymbol(statement4.Declarators(0).Names(0)), LocalSymbol)
            Dim typeA3 = localA3.Type
            Dim typeA4 = localA4.Type

            ' A3 and A4 should have different type objects, that compare equal. They should have 
            ' different locations.
            Assert.Equal(typeA3, typeA4)
            Assert.Same(typeA3, typeA4)  'As Local type inference does not occur for Static Locals
            Assert.Equal(typeA3.Locations(0), typeA4.Locations(0))
        End Sub

#Region "Utils"

        Private Sub CheckFieldNameAndLocation(model As SemanticModel, type As ITypeSymbol, tree As SyntaxTree, identifierIndex As Integer, fieldName As String, Optional isKey As Boolean = False)
            Dim node = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, identifierIndex).AsNode
            Dim span As TextSpan = node.Span
            Assert.Equal(fieldName, node.ToString())

            ' get declared symbol 
            Dim fieldInitializer As FieldInitializerSyntax = Nothing
            While node IsNot Nothing AndAlso fieldInitializer Is Nothing
                fieldInitializer = TryCast(node, FieldInitializerSyntax)
                node = node.Parent
            End While
            Assert.NotNull(fieldInitializer)

            Dim anonymousType = DirectCast(type, NamedTypeSymbol)

            Dim [property] As PropertySymbol = anonymousType.GetMember(Of PropertySymbol)(fieldName)
            Assert.NotNull([property])
            Assert.Equal(fieldName, [property].Name)
            Assert.Equal(1, [property].Locations.Length)
            Assert.Equal(span, [property].Locations(0).SourceSpan)

            Dim declaredSymbol = model.GetDeclaredSymbol(fieldInitializer)
            Assert.Equal([property], declaredSymbol)

            Dim getter As MethodSymbol = [property].GetMethod
            Assert.NotNull(getter)
            Assert.Equal("get_" & fieldName, getter.Name)

            If Not isKey Then
                Dim setter As MethodSymbol = [property].SetMethod
                Assert.NotNull(setter)
                Assert.Equal("set_" & fieldName, setter.Name)
            Else
                Assert.True([property].IsReadOnly)
            End If

            ' Do we actually need this??
            'Dim field As FieldSymbol = anonymousType.GetMember(Of FieldSymbol)("$" & fieldName)
            'Assert.NotNull(field)
            'Assert.Equal("$" & fieldName, field.Name)     
            'Assert.Equal(isKey, field.IsReadOnly)
        End Sub

        Private Function Compile(text As XElement, ByRef tree As SyntaxTree, nodes As List(Of SyntaxNode), Optional errors As XElement = Nothing) As VisualBasicCompilation
            Dim spans As New List(Of TextSpan)
            ExtractTextIntervals(text, spans)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(text, {Net40.References.System, Net40.References.SystemCore, Net40.References.MicrosoftVisualBasic})
            If errors Is Nothing Then
                CompilationUtils.AssertNoErrors(compilation)
            Else
                CompilationUtils.AssertTheseDiagnostics(compilation, errors)
            End If

            tree = compilation.SyntaxTrees(0)
            For Each span In spans

                Dim stack As New Stack(Of SyntaxNode)
                stack.Push(tree.GetRoot())

                While stack.Count > 0
                    Dim node = stack.Pop()

                    If span.Contains(node.Span) Then
                        nodes.Add(node)
                        Exit While
                    End If

                    For Each child In node.ChildNodes
                        stack.Push(child)
                    Next
                End While
            Next

            Return compilation
        End Function

        Private Shared Sub ExtractTextIntervals(text As XElement, nodes As List(Of TextSpan))
            text.<file>.Value = text.<file>.Value.Trim().Replace(vbLf, vbCrLf)

            Dim index As Integer = 0
            Do
                Dim startMarker = "[#" & index
                Dim endMarker = index & "#]"

                ' opening '[#{0-9}'
                Dim start = text.<file>.Value.IndexOf(startMarker, StringComparison.Ordinal)
                If start < 0 Then
                    Exit Do
                End If

                ' closing '{0-9}#]'
                Dim [end] = text.<file>.Value.IndexOf(endMarker, StringComparison.Ordinal)
                Assert.InRange([end], 0, Int32.MaxValue)

                nodes.Add(New TextSpan(start, [end] - start + 3))

                text.<file>.Value = text.<file>.Value.Replace(startMarker, "   ").Replace(endMarker, "   ")

                index += 1
                Assert.InRange(index, 0, 9)
            Loop
        End Sub

        Private Shared Function GetNamedTypeSymbol(c As VisualBasicCompilation, namedTypeName As String, Optional fromCorLib As Boolean = False) As NamedTypeSymbol
            Dim nameParts = namedTypeName.Split("."c)

            Dim srcAssembly = DirectCast(c.Assembly, SourceAssemblySymbol)
            Dim nsSymbol As NamespaceSymbol = (If(fromCorLib, srcAssembly.CorLibrary, srcAssembly)).GlobalNamespace
            For Each ns In nameParts.Take(nameParts.Length - 1)
                nsSymbol = DirectCast(nsSymbol.GetMember(ns), NamespaceSymbol)
            Next
            Return DirectCast(nsSymbol.GetTypeMember(nameParts(nameParts.Length - 1)), NamedTypeSymbol)
        End Function

#End Region

        <Fact>
        <WorkItem(2928, "https://github.com/dotnet/roslyn/issues/2928")>
        Public Sub ContainingSymbol()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Test
    Sub Main()
        Dim x = New With {.y = 1}
        System.Console.WriteLine(x.GetType())
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.DebugExe.WithRootNamespace("Ns1.Ns2"))

            Dim tree As SyntaxTree = comp.SyntaxTrees.Single()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "x").Single()

            Dim type = semanticModel.GetTypeInfo(x).Type
            Assert.Equal("<anonymous type: y As System.Int32>", type.ToTestDisplayString())
            Assert.True(type.ContainingNamespace.IsGlobalNamespace)

            Dim validator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    Dim anonType = (From sym In m.GlobalNamespace.GetMembers()
                                    Where sym.Name.Contains("AnonymousType")).Single()
                End Sub

            CompileAndVerify(comp, symbolValidator:=validator, expectedOutput:="VB$AnonymousType_0`1[System.Int32]")
        End Sub
    End Class

#Region "Extensions"

    Friend Module TestExtensions

        <Extension()>
        Public Function TheOnlyConstructor(type As ITypeSymbol) As MethodSymbol
            Dim namedType As NamedTypeSymbol = TryCast(type, NamedTypeSymbol)
            If namedType IsNot Nothing Then
                Debug.Assert(namedType.InstanceConstructors.Length = 1)
                Return namedType.InstanceConstructors(0)
            Else
                ' Not implemented yet
                Throw New NotImplementedException()
            End If
        End Function

    End Module

#End Region

End Namespace

