' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Partial Class VisualBasicDefinitionMap
        Friend MustInherit Class LocalVariableDeclaratorsVisitor
            Inherits VisualBasicSyntaxWalker

            Protected MustOverride Sub VisitForStatementDeclarations(node As ForStatementSyntax)

            Protected MustOverride Sub VisitForEachStatementDeclarations(node As ForEachStatementSyntax)

            Protected MustOverride Sub VisitSyncLockStatementDeclarations(node As SyncLockStatementSyntax)

            Protected MustOverride Sub VisitUsingStatementDeclarations(node As UsingStatementSyntax)

            Protected MustOverride Sub VisitWithStatementDeclarations(node As WithStatementSyntax)

            Protected MustOverride Sub VisitVariableDeclaratorDeclarations(node As VariableDeclaratorSyntax)

            Protected MustOverride Sub VisitIdentifierNameDeclarations(node As IdentifierNameSyntax)

            Public NotOverridable Overrides Sub VisitForEachStatement(node As ForEachStatementSyntax)
                Me.VisitForEachStatementDeclarations(node)
                MyBase.VisitForEachStatement(node)
            End Sub

            Public NotOverridable Overrides Sub VisitForStatement(node As ForStatementSyntax)
                Me.VisitForStatementDeclarations(node)
                MyBase.VisitForStatement(node)
            End Sub

            Public NotOverridable Overrides Sub VisitSyncLockStatement(node As SyncLockStatementSyntax)
                Me.VisitSyncLockStatementDeclarations(node)
                MyBase.VisitSyncLockStatement(node)
            End Sub

            Public NotOverridable Overrides Sub VisitUsingStatement(node As UsingStatementSyntax)
                Me.VisitUsingStatementDeclarations(node)
                MyBase.VisitUsingStatement(node)
            End Sub

            Public NotOverridable Overrides Sub VisitWithStatement(node As WithStatementSyntax)
                Me.VisitWithStatementDeclarations(node)
                MyBase.VisitWithStatement(node)
            End Sub

            Public NotOverridable Overrides Sub VisitVariableDeclarator(node As VariableDeclaratorSyntax)
                Me.VisitVariableDeclaratorDeclarations(node)
                MyBase.VisitVariableDeclarator(node)
            End Sub

            Public NotOverridable Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                Me.VisitIdentifierNameDeclarations(node)
            End Sub

            Public Overrides Sub VisitGoToStatement(node As GoToStatementSyntax)
                ' goto syntax does not declare locals
                Return
            End Sub

            Public Overrides Sub VisitLabelStatement(node As LabelStatementSyntax)
                ' labels do not declare locals
                Return
            End Sub

            Public Overrides Sub VisitLabel(node As LabelSyntax)
                ' labels do not declare locals
                Return
            End Sub

            Public Overrides Sub VisitGetXmlNamespaceExpression(node As GetXmlNamespaceExpressionSyntax)
                ' GetXmlNamespace does not declare locals
                Return
            End Sub

            Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
                Me.Visit(node.Expression)

                ' right side of the . does not declare locals
                Return
            End Sub

            Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
                Me.Visit(node.Left)

                ' right side of the . does not declare locals
                Return
            End Sub

            Public Overrides Sub VisitNamedArgument(node As NamedArgumentSyntax)
                Me.Visit(node.Expression)

                ' argument name in "foo(argName := expr)" does not declare locals
                Return
            End Sub

            Protected Sub New()
                MyBase.New(SyntaxWalkerDepth.Node)
            End Sub
        End Class

        Private NotInheritable Class LocalSlotMapBuilder
            Inherits LocalVariableDeclaratorsVisitor

            <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
            Private Structure LocalName
                Public ReadOnly Name As String
                Public ReadOnly Kind As SynthesizedLocalKind
                Public ReadOnly UniqueId As Integer

                Public Sub New(name As String, kind As SynthesizedLocalKind, uniqueId As Integer)
                    Me.Name = name
                    Me.Kind = kind
                    Me.UniqueId = uniqueId
                End Sub

                Private Function GetDebuggerDisplay() As String
                    Return String.Format("[{0}, {1}, {2}]", Me.Kind, Me.UniqueId, Me.Name)
                End Function
            End Structure

            Private ReadOnly localNames As ImmutableArray(Of LocalName)
            Private ReadOnly localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo)
            Private ReadOnly locals As Dictionary(Of EncLocalInfo, Integer)
            Private ReadOnly knownDeclaredLocals As HashSet(Of String) = New HashSet(Of String)

            Private slotIndex As Integer

            Private offset As Integer

            Private Sub New(localNames As ImmutableArray(Of String), localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo), locals As Dictionary(Of EncLocalInfo, Integer))
                Me.localNames = localNames.SelectAsArray(AddressOf ParseName)
                Me.localInfo = localInfo
                Me.locals = locals
                Me.slotIndex = 0
            End Sub

            Public Shared Function CreateMap(block As SyntaxNode,
                                             localNames As ImmutableArray(Of String),
                                             localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo)) As Dictionary(Of EncLocalInfo, Integer)

                Dim map = New Dictionary(Of EncLocalInfo, Integer)()
                Dim visitor = New LocalSlotMapBuilder(localNames, localInfo, map)
                Call (New ExplicitDeclarationCollector(visitor.knownDeclaredLocals)).Visit(block)
                visitor.Visit(block)
                Return map
            End Function

            Private NotInheritable Class ExplicitDeclarationCollector
                Inherits VisualBasicSyntaxWalker

                Private ReadOnly explicitlyDeclared As HashSet(Of String)

                Public Sub New(explicitlyDeclared As HashSet(Of String))
                    MyBase.New(SyntaxWalkerDepth.Node)
                    Me.explicitlyDeclared = explicitlyDeclared
                End Sub

                Public Overrides Sub VisitVariableDeclarator(node As VariableDeclaratorSyntax)
                    MyBase.VisitVariableDeclarator(node)

                    For Each name In node.Names
                        explicitlyDeclared.Add(name.Identifier.ValueText)
                    Next
                End Sub
            End Class

            Protected Overrides Sub VisitForEachStatementDeclarations(node As ForEachStatementSyntax)
                Dim kindOpt As SynthesizedLocalKind? = Me.TryGetSlotIndex(SynthesizedLocalKind.ForEachEnumerator, SynthesizedLocalKind.ForEachArray)
                If kindOpt.HasValue Then
                    ' Enumerator.
                    If kindOpt.Value = SynthesizedLocalKind.ForEachArray Then
                        ' Index (VB only specialcases ForEach in single-dimensional case).
                        Dim kind = SynthesizedLocalKind.ForEachArrayIndex
                        If Me.IsSlotIndex(kind) Then
                            Me.AddLocal(kind)
                        End If
                    End If

                    'Loop variable
                    Dim ident = TryCast(node.ControlVariable, IdentifierNameSyntax)
                    If ident IsNot Nothing Then
                        Dim name As String = ident.Identifier.ValueText
                        If Me.IsSlotIndex(name) Then
                            Me.AddLocal(name)
                        End If
                    End If
                End If

                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitForStatementDeclarations(node As ForStatementSyntax)
                Dim kindOpt As SynthesizedLocalKind? = Me.TryGetSlotIndex(SynthesizedLocalKind.ForLoopObject)

                If kindOpt.HasValue Then
                    Dim kind = SynthesizedLocalKind.ForLimit
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                    End If

                    kind = SynthesizedLocalKind.ForStep
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                    End If

                    kind = SynthesizedLocalKind.ForDirection
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                    End If

                    'Loop variable
                    Dim ident = TryCast(node.ControlVariable, IdentifierNameSyntax)
                    If ident IsNot Nothing Then
                        Dim name As String = ident.Identifier.ValueText
                        If Me.IsSlotIndex(name) Then
                            Me.AddLocal(name)
                        End If
                    End If
                End If

                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitSyncLockStatementDeclarations(node As SyncLockStatementSyntax)
                ' Expecting one or two locals depending on which overload of Monitor.Enter is used.
                Dim expr As ExpressionSyntax = node.Expression
                Debug.Assert(expr IsNot Nothing)
                If Me.TryGetSlotIndex(SynthesizedLocalKind.Lock).HasValue Then
                    ' if the next local Is LockTaken, then the lock was emitted with the two argument
                    ' overload for Monitor.Enter(). Otherwise, the single argument overload was used.
                    If IsSlotIndex(SynthesizedLocalKind.LockTaken) Then
                        AddLocal(SynthesizedLocalKind.LockTaken)
                    End If
                End If
                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitUsingStatementDeclarations(node As UsingStatementSyntax)
                Dim expr = node.Expression
                If expr IsNot Nothing Then
                    Me.TryGetSlotIndex(SynthesizedLocalKind.Using)
                End If
                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitWithStatementDeclarations(node As WithStatementSyntax)
                Me.TryGetSlotIndex(SynthesizedLocalKind.With)
                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitVariableDeclaratorDeclarations(node As VariableDeclaratorSyntax)
                For Each name In node.Names
                    Me.TryGetSlotIndex(name.Identifier.ValueText)
                    Me.offset += 1
                Next
            End Sub

            Protected Overrides Sub VisitIdentifierNameDeclarations(node As IdentifierNameSyntax)
                Dim name = node.Identifier.ValueText

                If Not Me.knownDeclaredLocals.Contains(name) Then
                    ' this name does not match any so far known locals
                    ' if there is a local by this name in the local file up until this slotIndex, it must be declared implicitly and this is it's declaration
                    ' NOTE: For/ForEach may "implicitly" define locals that are not really implicit (VB is like that), but that is not ambiguous
                    '       those locals are scoped to the corresponding blocks and would not be declared before we go through
                    '       corresponding For/ForEach syntax which would have claimed them and add to knownDeclared.
                    '       implicit locals on the other hand all declared at the method scope, so will always be declared before For/ForEach locals

                    Me.TryGetSlotIndexAtCurrentOffsetOrBefore(name)
                End If
                Me.offset += 1
            End Sub

            Private Shared Function ParseName(name As String) As LocalName
                If name Is Nothing Then
                    Return New LocalName(name, SynthesizedLocalKind.None, 0)
                End If
                Dim kind As SynthesizedLocalKind
                Dim uniqueId As Integer
                GeneratedNames.TryParseLocalName(name, kind, uniqueId)
                Return New LocalName(name, kind, uniqueId)
            End Function

            Private Function IsSlotIndex(name As String) As Boolean
                Dim slot = Me.slotIndex
                Return IsSlotIndex(name, slot)
            End Function

            Private Function IsSlotIndex(name As String, slot As Integer) As Boolean
                Return slot < Me.localNames.Length AndAlso Me.localNames(slot).Kind = SynthesizedLocalKind.None AndAlso name = Me.localNames(slot).Name
            End Function

            Private Function IsSlotIndex(kind As SynthesizedLocalKind) As Boolean
                Return Me.slotIndex < Me.localNames.Length AndAlso Me.localNames(Me.slotIndex).Kind = kind
            End Function

            Private Function IsSlotIndex(ParamArray kinds As SynthesizedLocalKind()) As Boolean
                Return Me.slotIndex < Me.localNames.Length AndAlso Array.IndexOf(Of SynthesizedLocalKind)(kinds, Me.localNames(Me.slotIndex).Kind) >= 0
            End Function

            Private Function TryGetSlotIndex(name As String) As Boolean
                While Me.slotIndex < Me.localNames.Length
                    If Me.IsSlotIndex(name) Then
                        Me.AddLocal(name)
                        Return True
                    End If
                    Me.slotIndex += 1
                End While
                Return False
            End Function

            Private Function TryGetSlotIndex(kind As SynthesizedLocalKind) As SynthesizedLocalKind?
                While Me.slotIndex < Me.localNames.Length
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                        Return New SynthesizedLocalKind?(kind)
                    End If
                    Me.slotIndex += 1
                End While
                Return Nothing
            End Function

            Private Function TryGetSlotIndex(ParamArray kinds As SynthesizedLocalKind()) As SynthesizedLocalKind?
                While Me.slotIndex < Me.localNames.Length
                    If Me.IsSlotIndex(kinds) Then
                        Dim kind As SynthesizedLocalKind = Me.localNames(Me.slotIndex).Kind
                        Me.AddLocal(kind)
                        Return New SynthesizedLocalKind?(kind)
                    End If
                    Me.slotIndex += 1
                End While
                Return Nothing
            End Function

            Private Function TryGetSlotIndexAtCurrentOffsetOrBefore(name As String) As Boolean
                Dim slot = 0
                While slot <= Me.slotIndex
                    If Me.IsSlotIndex(name, slot) Then
                        Me.AddLocal(SynthesizedLocalKind.None, slot, name)
                        If (slot = Me.slotIndex) Then
                            Me.slotIndex += 1
                        End If
                        Return True
                    End If
                    slot += 1
                End While
                Return False
            End Function

            Private Sub AddLocal(kind As SynthesizedLocalKind)
                Debug.Assert(kind <> SynthesizedLocalKind.None, "None must have name")
                Dim slot = Me.slotIndex
                AddLocal(kind, slot, Nothing)
                Me.slotIndex += 1
            End Sub

            Private Sub AddLocal(name As String)
                Dim slot = Me.slotIndex
                AddLocal(SynthesizedLocalKind.None, slot, name)
                Me.slotIndex += 1
            End Sub

            Private Sub AddLocal(kind As SynthesizedLocalKind, slot As Integer, name As String)
                Dim info As MetadataDecoder.LocalInfo = Me.localInfo(slot)

                ' We do not emit custom modifiers on locals so ignore the
                ' previous version of the local if it had custom modifiers.
                If info.CustomModifiers.IsDefaultOrEmpty Then
                    Dim local As EncLocalInfo = New EncLocalInfo(Me.offset, CType(info.Type, Cci.ITypeReference), info.Constraints, CType(kind, CommonSynthesizedLocalKind), info.SignatureOpt)
                    Me.locals.Add(local, slot)
                    If name IsNot Nothing Then
                        Me.knownDeclaredLocals.Add(name)
                    End If
                End If
            End Sub
        End Class

        Friend NotInheritable Class LocalVariableDeclaratorsCollector
            Inherits LocalVariableDeclaratorsVisitor

            Private ReadOnly builder As ArrayBuilder(Of SyntaxNode)

            Friend Shared Function GetDeclarators(method As IMethodSymbol) As ImmutableArray(Of SyntaxNode)
                Dim syntaxRefs = method.DeclaringSyntaxReferences
                If syntaxRefs.Length = 0 Then
                    Return ImmutableArray(Of SyntaxNode).Empty
                End If
                Dim syntax As SyntaxNode = syntaxRefs(0).GetSyntax(Nothing)
                Dim block = syntax.Parent
                Debug.Assert(TypeOf block Is MethodBlockBaseSyntax)

                Dim builder = ArrayBuilder(Of SyntaxNode).GetInstance()
                Call New LocalVariableDeclaratorsCollector(builder).Visit(block)
                Return builder.ToImmutableAndFree()
            End Function

            Public Sub New(builder As ArrayBuilder(Of SyntaxNode))
                Me.builder = builder
            End Sub

            Protected Overrides Sub VisitForEachStatementDeclarations(node As ForEachStatementSyntax)
                Me.builder.Add(node)
            End Sub

            Protected Overrides Sub VisitForStatementDeclarations(node As ForStatementSyntax)
                Me.builder.Add(node)
            End Sub

            Protected Overrides Sub VisitSyncLockStatementDeclarations(node As SyncLockStatementSyntax)
                Me.builder.Add(node)
            End Sub

            Protected Overrides Sub VisitWithStatementDeclarations(node As WithStatementSyntax)
                Me.builder.Add(node)
            End Sub

            Protected Overrides Sub VisitUsingStatementDeclarations(node As UsingStatementSyntax)
                Me.builder.Add(node)
            End Sub

            Protected Overrides Sub VisitVariableDeclaratorDeclarations(node As VariableDeclaratorSyntax)
                For Each name In node.Names
                    Me.builder.Add(name)
                Next
            End Sub

            Protected Overrides Sub VisitIdentifierNameDeclarations(node As IdentifierNameSyntax)
                Debug.Assert(Not Me.builder.Contains(node))
                Me.builder.Add(node)
            End Sub
        End Class
    End Class

End Namespace