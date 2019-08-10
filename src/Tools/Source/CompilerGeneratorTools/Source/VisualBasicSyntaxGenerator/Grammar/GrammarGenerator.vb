' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Microsoft.CodeAnalysis.VisualBasic.Symbols

Friend Class GrammarGenerator
    Inherits WriteUtils

    Private _nameToProductions As Dictionary(Of String, List(Of Production))
    Private _seen As New HashSet(Of String)()
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

        _nameToProductions = Me._parseTree.NodeStructures.Values.ToDictionary(
            Function(n) n.Name, Function(n) New List(Of Production)())

        Dim nonTokens = _parseTree.NodeStructures.Values.Where(Function(n) Not n.IsToken)
        Dim tokens = _parseTree.NodeStructures.Values.Where(Function(n) n.IsToken)

        For Each structureNode In nonTokens.Concat(tokens)
            Dim parent = structureNode.ParentStructure
            If parent IsNot Nothing Then
                _nameToProductions(parent.Name).Add(RuleReference(structureNode.Name))
            End If

            If Not structureNode.Abstract Then
                Dim children = GetAllChildrenOfStructure(structureNode)
                If children.Count > 0 Then
                    _nameToProductions(structureNode.Name).Add(HandleChildren(structureNode, children))
                End If
            End If

            '        // Convert rules Like `a: (x | y)` into:
            '        // a x
            '        //  | y;
            '        If (Type.Children.Count == 1 && Type.Children[0] Is Field field && field.IsToken)
            '        {
            '            nameToProductions[type.Name].AddRange(field.Kinds.Select(k =>
            '                HandleChildren(New List <TreeTypeChild> {New Field { Type = "SyntaxToken", Kinds = { k } } })));
            '            Continue For;
            '        }
        Next

        For Each token In tokens
            If _nameToProductions.ContainsKey(token.Name) AndAlso
               _nameToProductions(token.Name).Count = 0 Then

                _nameToProductions(token.Name).Add(New Production("/* see lexical specification */"))
            End If
        Next

        ' The grammar will bottom out with certain lexical productions. Create rules for these.
        _lexicalRules = tokens.Select(Function(t) t.Name).ToImmutableArray()

        ' Define a few major sections to help keep the grammar file naturally grouped.
        _majorRules = ImmutableArray.Create(
            "CompilationUnitSyntax", "TypeSyntax", "StatementSyntax", "ExpressionSyntax", "XmlNodeSyntax", "StructuredTriviaSyntax", "Modifier", "SyntaxToken")

        Dim result = "// <auto1-generated/>" + Environment.NewLine + "grammar vb;" + Environment.NewLine

        ' Handle each major section first And then walk any rules Not hit transitively from them.
        For Each rule In _majorRules.Concat(_nameToProductions.Keys.OrderBy(Function(a) a))
            ProcessRule(rule, result)
        Next

        Return result
    End Function

    Private Shared Function Join(delim As String, productions As IEnumerable(Of Production)) As Production
        Return New Production(
            String.Join(delim, productions.Where(Function(p) p.Text.Length > 0)),
            productions.SelectMany(Function(p) p.ReferencedRules))
    End Function

    Private Function HandleChildren(structureNode As ParseNodeStructure, children As List(Of ParseNodeChild), Optional delim As String = " ") As Production
        Return Join(delim, children.Select(
                    Function(child)
                        Return HandleField(structureNode, child)
                    End Function))
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

        Dim result = childProduction.Suffix("(" + separatorProd.Text + " " + childProduction.Text + ")*").
            Parenthesize().Suffix("?")
        Return result
    End Function

    Private Function HandleList(structureNode As ParseNodeStructure, child As ParseNodeChild) As Production
        Dim childProduction = HandleChildKind(structureNode, child, child.ChildKind)
        Return childProduction.Suffix("*")
    End Function

    Private Function HandleChildKind(structureNode As ParseNodeStructure,
                                     child As ParseNodeChild,
                                     childKind As Object) As Production
        If child.Name = "Modifiers" Then
            Return RuleReference("Modifier")
        End If

        Dim x = child.ChildKind()
        Dim y = child.ChildKind("")

        Dim nodeKind = TryCast(childKind, ParseNodeKind)
        Dim nodeKindList = TryCast(childKind, List(Of ParseNodeKind))

        If structureNode.NodeKinds IsNot Nothing Then
            If structureNode.NodeKinds.Count = 1 Then
                If child.KindForNodeKind IsNot Nothing Then
                    Dim kind As ParseNodeKind = Nothing
                    If child.KindForNodeKind.TryGetValue(structureNode.NodeKinds(0).Name, kind) Then
                        nodeKind = kind
                    End If
                End If
            ElseIf structureNode.NodeKinds.Count > 1 Then
                Dim tempList = New List(Of ParseNodeKind)()
                For Each structureNodeKind In structureNode.NodeKinds
                    Dim kind As ParseNodeKind = Nothing
                    If child.KindForNodeKind.TryGetValue(structureNodeKind.Name, kind) Then
                        tempList.Add(kind)
                    End If
                Next

                If tempList.Count > 0 Then
                    nodeKindList = tempList
                End If
            End If
        End If

        If nodeKind IsNot Nothing Then
            Return HandleNodeKind(nodeKind)
        ElseIf nodeKindList IsNot Nothing Then
            Dim common = GetCommonStructure(nodeKindList)
            If common IsNot Nothing AndAlso Not common.IsToken Then
                Return RuleReference(common.Name)
            Else
                Return Join(
                    " | ",
                    nodeKindList.Select(Function(nk) HandleNodeKind(nk)).Distinct()).Parenthesize()
            End If
        Else
            Throw New NotImplementedException()
        End If
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

        Return RuleReference(nodeKind.NodeStructure.Name)
    End Function

    Private Sub ProcessRule(name As String, ByRef result As String)
        If name <> "VisualBasicSyntaxNode" AndAlso _seen.Add(name) Then
            Dim sorted = _nameToProductions(name).OrderBy(Function(v) v)
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
