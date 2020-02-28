﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Microsoft.CodeAnalysis.VisualBasic.Symbols

Friend Class GrammarGenerator
    Inherits WriteUtils

    Private ReadOnly _seen As New HashSet(Of String)()

    Private _rules As Dictionary(Of String, List(Of Production))
    Private _lexicalRules As ImmutableArray(Of String)
    Private _majorRules As ImmutableArray(Of String)

    Public Sub New(parseTree As ParseTree)
        MyBase.New(parseTree)
    End Sub

    Friend Function Run() As String
        ' Syntax.xml refers to a special pseudo-element 'Modifier'.  Synthesize that for the grammar.
        Dim modifiers = GetMembers(Of DeclarationModifiers)().
            Select(Function(m) m.ToString() + "Keyword").Where(Function(n) GetSyntaxKind(n) <> SyntaxKind.None).ToList()
        Dim modifiersString = String.Join("|", modifiers.OrderBy(Function(a) a))

        _parseTree.NodeStructures.Add("Modifier", New ParseNodeStructure(
            New XElement("node-structure",
                         New XAttribute("name", "Modifier"),
                         New XAttribute("parent", "VisualBasicSyntaxNode"),
                         New XElement(
                            XName.Get("child", "http://schemas.microsoft.com/VisualStudio/Roslyn/Compiler"),
                            New XAttribute("name", "Keyword"),
                            New XAttribute("kind", modifiersString))), _parseTree))

        _rules = Me._parseTree.NodeStructures.Values.ToDictionary(
            Function(n) n.Name, Function(n) New List(Of Production)())

        Dim nonTokens = _parseTree.NodeStructures.Values.Where(Function(n) Not n.IsToken)
        Dim tokens = _parseTree.NodeStructures.Values.Where(Function(n) n.IsToken)

        ' Initial pass.  Set up base/inheritance rules and break out compound production
        ' rules so they're available for other productions to reference.
        Dim compoundNodes = New List(Of ParseNodeStructure)

        For Each structureNode In nonTokens.Concat(tokens)
            Dim parent = structureNode.ParentStructure
            If parent IsNot Nothing Then
                _rules(parent.Name).Add(RuleReference(structureNode.Name))
            End If

            ' VB syntax has a ton of nodes that are effectively 'compound' nodes.  i.e. there is
            ' EndBlockStatement (which is referenced by tons of other nodes, like EnumDeclaration,
            ' ClassDeclaration, WhileStatement, etc. etc.)  However, all these nodes reference a
            ' specific node-kind specialization of that compound node.  These specializations are
            ' what spell out that while EndBlock is defined as `'End' ('Class' | 'Enum' | 'Structure
            ' | ...)` that there are really N different specializations like `'End' 'Class'` `'End'
            ' 'Enum'` etc.  In these cases to make the generated grammar much nicer, we emit as those
            ' individual productions.
            '
            ' So, we first check if this production actually defines N different sub-kinds
            If structureNode.NodeKinds.Count >= 2 Then
                ' Then we see if there are children that having a matching number of kinds. When
                ' this happens, it's the case that each kind the child could be corresponds 1:1
                ' with all the different sub-kinds of hte production.
                Dim correspondingChildren = structureNode.Children.Where(
                    Function(c)
                        Dim childKinds = TryCast(c.ChildKind, List(Of ParseNodeKind))
                        Return childKinds IsNot Nothing AndAlso childKinds.Count = structureNode.NodeKinds.Count
                    End Function).ToArray()

                If correspondingChildren.Length > 0 Then
                    compoundNodes.Add(structureNode)

                    Dim correspondingChildrenKinds = correspondingChildren.ToDictionary(
                        Function(c) c,
                        Function(c) DirectCast(c.ChildKind, List(Of ParseNodeKind)))

                    ' We then iterate each different kind, pickin out the specific subkinds of of
                    ' each child that correspond to it.
                    For i = 0 To structureNode.NodeKinds.Count - 1
                        Dim nodeKind = structureNode.NodeKinds(i)
                        Dim local_i = i
                        Dim mappedChildren = structureNode.Children.Select(
                        Function(c)
                            If correspondingChildren.Contains(c) Then
                                Return c.WithChildKind(correspondingChildrenKinds(c)(local_i))
                            End If

                            Return c
                        End Function).ToList()

                        ' We then emit a production for the child kind.
                        _rules.Add(nodeKind.Name, New List(Of Production)())
                        _rules(nodeKind.Name).Add(HandleChildren(structureNode, mappedChildren))

                        ' And then point the top level production at each of these child productions.
                        _rules(structureNode.Name).Add(RuleReference(nodeKind.Name))
                    Next
                End If
            End If
        Next

        For Each structureNode In nonTokens.Concat(tokens)
            ' ignore compound nodes.  we handled it in the above loop.
            If compoundNodes.Contains(structureNode) Then
                Continue For
            End If

            If Not structureNode.Abstract Then
                Dim children = GetAllChildrenOfStructure(structureNode)

                ' Convert rules Like `a: (x | y) ...` into:
                ' a: x ...
                '  | y ...
                If children.Count > 0 Then
                    Dim child = children(0)
                    Dim kinds = TryCast(child.ChildKind, List(Of ParseNodeKind))
                    If kinds IsNot Nothing Then
                        Dim childStructure = GetCommonStructure(kinds)
                        If childStructure.IsToken Then
                            For Each kind In kinds
                                _rules(structureNode.Name).Add(HandleChildren(structureNode, children.Select(
                                    Function(c) If(c Is child, child.WithChildKind(kind), c))))
                            Next
                            Continue For
                        End If
                    End If

                    _rules(structureNode.Name).Add(HandleChildren(structureNode, children))
                End If
            End If
        Next

        ' Clear out this production as it's not really helpful to have a rule
        ' that points outward to all other tokens
        _rules("SkippedTokensTriviaSyntax").Clear()
        _rules("SkippedTokensTriviaSyntax").Add(RuleReference("SyntaxToken").Suffix("*"))

        _rules("PunctuationSyntax").Clear()

        ' The grammar will bottom out with certain lexical productions. Create rules for these.
        Dim lexicalRules = _rules.Values.SelectMany(Function(ps) ps).SelectMany(Function(p) p.ReferencedRules).
            Where(Function(r)
                      Dim productions As List(Of Production) = Nothing
                      Return Not _rules.TryGetValue(r, productions) OrElse productions.Count = 0
                  End Function).ToArray()
        For Each name In lexicalRules
            _rules(name) = New List(Of Production) From {New Production("/* see lexical specification */")}
        Next

        ' The grammar will bottom out with certain lexical productions. Create rules for these.
        _lexicalRules = tokens.Select(Function(t) t.Name).ToImmutableArray()

        ' Define a few major sections to help keep the grammar file naturally grouped.
        _majorRules = ImmutableArray.Create(
            "CompilationUnitSyntax", "StatementSyntax", "ExpressionSyntax", "TypeSyntax", "NameSyntax", "XmlNodeSyntax", "StructuredTriviaSyntax",
            "Modifier", "SyntaxToken", "PunctuationSyntax", "EmptyToken")

        Dim result = "// <auto-generated />" + Environment.NewLine + "grammar vb;" + Environment.NewLine

        ' Handle each major section first And then walk any rules Not hit transitively from them.
        For Each rule In _majorRules.Concat(_rules.Keys.OrderBy(Function(a) a))
            ProcessRule(rule, result)
        Next

        Return result
    End Function

    Private Shared Function Join(delim As String, productions As IEnumerable(Of Production)) As Production
        Return New Production(
            String.Join(delim, productions.Where(Function(p) p.Text.Length > 0)),
            productions.SelectMany(Function(p) p.ReferencedRules))
    End Function

    Private Function HandleChildren(structureNode As ParseNodeStructure, children As IEnumerable(Of ParseNodeChild), Optional delim As String = " ") As Production
        Return Join(delim, children.Select(
                    Function(child) HandleField(structureNode, child)))
    End Function

    Private Function HandleField(structureNode As ParseNodeStructure, child As ParseNodeChild) As Production
        If child.IsSeparated Then
            Return HandleSeparatedList(structureNode, child)
        ElseIf child.IsList Then
            Return HandleList(structureNode, child)
        Else
            Return HandleChildKind(structureNode, child, child.ChildKind).Suffix("?", [when]:=child.IsOptional)
        End If
    End Function

    Private Function HandleSeparatedList(structureNode As ParseNodeStructure, child As ParseNodeChild) As Production
        Dim childProduction = HandleChildKind(structureNode, child, child.ChildKind)
        Dim separatorProd = HandleChildKind(structureNode, child, child.SeparatorsKind)

        Return childProduction.Suffix(" (" + separatorProd.Text + " " + childProduction.Text + ")").
            Suffix("*", [when]:=child.MinCount < 2).Suffix("+", [when]:=child.MinCount >= 2).
            Parenthesize([when]:=child.MinCount = 0).Suffix("?", [when]:=child.MinCount = 0)
    End Function

    Private Function HandleList(structureNode As ParseNodeStructure, child As ParseNodeChild) As Production
        Return HandleChildKind(structureNode, child, child.ChildKind).Suffix("*")
    End Function

    Private Function HandleChildKind(structureNode As ParseNodeStructure,
                                     child As ParseNodeChild,
                                     childKind As Object) As Production
        If child.Name = "Modifiers" Then
            Return RuleReference("Modifier")
        End If

        ' VB syntax model is interesting, it will have base types that support certain kinds of
        ' child tokens/nodes.  Then, it will have derived types that then support a subset of those
        ' kinds.  For example, CastExpressionSyntax says that its keyword can be
        ' CType/DirectCast/TryCast.  However, there is information in Syntax.xml that says that if
        ' you have a DirectCastExpressionSyntax that the keyword is DirectCast only.  So when we're
        ' emitting the code for direct_cast_expression we don't want to say that it starts with
        ' 'DirectCast' and not either of the other keywords.
        Dim mappedKinds = GetMappedKinds(structureNode, child)
        Dim nodeKind = If(mappedKinds.nodeKind, TryCast(childKind, ParseNodeKind))
        Dim nodeKindList = If(mappedKinds.nodeKinds, TryCast(childKind, List(Of ParseNodeKind)))

        If nodeKind IsNot Nothing Then
            Return HandleNodeKind(nodeKind)
        ElseIf nodeKindList IsNot Nothing Then
            Dim common = GetCommonStructure(nodeKindList)
            If common.IsRoot Then
                Dim commons = nodeKindList.Select(Function(n) GetNonRootParent(n))
                Return Join(" | ",
                    commons.Select(Function(c) RuleReference(c.Name)).Distinct()).Parenthesize()
            ElseIf common IsNot Nothing AndAlso Not common.IsToken Then
                Return RuleReference(common.Name)
            Else
                Return Join(" | ",
                    nodeKindList.Select(Function(nk) HandleNodeKind(nk)).Distinct()).Parenthesize()
            End If
        End If

        Throw New NotImplementedException()
    End Function

    Private Function GetMappedKinds(structureNode As ParseNodeStructure,
                                    child As ParseNodeChild) As (nodeKind As ParseNodeKind, nodeKinds As List(Of ParseNodeKind))
        ' Ensure certain deferred collections have been computed.
        Dim unused1 = child.ChildKind()
        Dim unused2 = child.ChildKind("")

        If structureNode.NodeKinds IsNot Nothing Then
            If structureNode.NodeKinds.Count = 1 Then
                If child.KindForNodeKind IsNot Nothing Then
                    Dim kind As ParseNodeKind = Nothing
                    If child.KindForNodeKind.TryGetValue(structureNode.NodeKinds(0).Name, kind) Then
                        Return (kind, Nothing)
                    End If
                End If
            ElseIf structureNode.NodeKinds.Count > 1 Then
                Dim kindList = New List(Of ParseNodeKind)()
                For Each structureNodeKind In structureNode.NodeKinds
                    Dim kind As ParseNodeKind = Nothing
                    If child.KindForNodeKind.TryGetValue(structureNodeKind.Name, kind) Then
                        kindList.Add(kind)
                    End If
                Next

                If kindList.Count > 0 Then
                    Return (Nothing, kindList)
                End If
            End If
        End If

        Return Nothing
    End Function

    Private Function GetNonRootParent(n As ParseNodeKind) As ParseNodeStructure
        Dim current = n.NodeStructure
        While Not current.ParentStructure.IsRoot
            current = current.ParentStructure
        End While

        Return current
    End Function

    Private Function HandleNodeKind(nodeKind As ParseNodeKind) As Production
        If Not String.IsNullOrEmpty(nodeKind.TokenText) Then
            If nodeKind.TokenText = "\" Then
                Return New Production("'\\'")
            ElseIf nodeKind.TokenText = "'" Then
                Return New Production("'\''")
            Else
                Return New Production("'" + nodeKind.TokenText + "'")
            End If
        End If

        If nodeKind.Name = "EmptyToken" Then
            Return RuleReference("EmptyToken")
        ElseIf nodeKind.Name = "EndOfFileToken" Then
            Return New Production("")
        End If

        If _rules.ContainsKey(nodeKind.Name) Then
            Return RuleReference(nodeKind.Name)
        End If

        'If nodeKind.NodeStructure.Name = "EndBlockStatementSyntax" Then
        '    Return RuleReference(nodeKind.Name)
        'End If

        Return RuleReference(nodeKind.NodeStructure.Name)
    End Function

    Private Sub ProcessRule(name As String, ByRef result As String)
        If name <> "VisualBasicSyntaxNode" AndAlso _seen.Add(name) Then
            Dim sorted = _rules(name).Distinct().OrderBy(Function(v) v)
            If sorted.Any() Then
                result += Environment.NewLine + RuleReference(name).Text + Environment.NewLine + "  : " +
                          String.Join(Environment.NewLine + "  | ", sorted) + Environment.NewLine + "  ;" + Environment.NewLine

                ' Now proceed in depth-first fashion through the referenced rules to keep related rules
                ' close by. Don't recurse into major-sections to help keep them separated in grammar file.
                Dim references = sorted.SelectMany(Function(t) t.ReferencedRules)
                For Each referencedRule In references.Where(Function(r) Not _majorRules.Concat(_lexicalRules).Contains(r))
                    ProcessRule(referencedRule, result)
                Next
            End If
        End If
    End Sub

    Private Function RuleReference(name As String) As Production
        Dim trimmed = If(name.EndsWith("Syntax"), name.Substring(0, name.Length - "Syntax".Length), name)
        Return New Production(
            s_normalizationRegex.Replace(trimmed, "_").ToLower(),
            ImmutableArray.Create(name))
    End Function

    Private Shared Function GetSyntaxKind(name As String) As SyntaxKind
        Return GetMembers(Of SyntaxKind)().Where(Function(k) k.ToString() = name).SingleOrDefault()
    End Function

    Private Shared Function GetMembers(Of TEnum)() As IEnumerable(Of TEnum)
        Return GetType(TEnum).GetFields(BindingFlags.Public Or BindingFlags.Static).
            Select(Function(f) f.GetValue(Nothing)).OfType(Of TEnum)()
    End Function

    ' Converts a PascalCased name into snake_cased name.
    Private Shared ReadOnly s_normalizationRegex As New Regex(
        "(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])",
        RegexOptions.IgnorePatternWhitespace Or RegexOptions.Compiled)

    Friend Structure Production
        Implements IComparable(Of Production)

        Public ReadOnly Text As String
        Public ReadOnly ReferencedRules As ImmutableArray(Of String)

        Public Sub New(text As String, Optional referencedRules As IEnumerable(Of String) = Nothing)
            Me.Text = text
            Me.ReferencedRules = If(referencedRules Is Nothing, ImmutableArray(Of String).Empty, referencedRules.ToImmutableArray())
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Text = DirectCast(obj, Production).Text
        End Function

        Public Overrides Function ToString() As String
            Return Text
        End Function

        Public Function CompareTo(other As Production) As Integer Implements IComparable(Of Production).CompareTo
            Return StringComparer.Ordinal.Compare(Me.Text, other.Text)
        End Function

        Public Function Prefix(val As String) As Production
            Return New Production(val + Me.Text, ReferencedRules)
        End Function

        Public Function Suffix(val As String, Optional [when] As Boolean = True) As Production
            Return If([when], New Production(Me.Text + val, ReferencedRules), Me)
        End Function

        Public Function Parenthesize(Optional [when] As Boolean = True) As Production
            Return If([when], Prefix("(").Suffix(")"), Me)
        End Function
    End Structure
End Class

Namespace Global.Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class GreenNode
        Public Const ListKind = 1
    End Class
End Namespace
