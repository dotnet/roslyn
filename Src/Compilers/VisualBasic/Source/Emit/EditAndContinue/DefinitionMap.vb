' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit
    Friend Structure MethodDefinitionEntry
        Public Sub New(previousMethod As MethodSymbol, preserveLocalVariables As Boolean, syntaxMap As Func(Of SyntaxNode, SyntaxNode))
            Me.PreviousMethod = previousMethod
            Me.PreserveLocalVariables = preserveLocalVariables
            Me.SyntaxMap = syntaxMap
        End Sub

        Public ReadOnly PreviousMethod As MethodSymbol
        Public ReadOnly PreserveLocalVariables As Boolean
        Public ReadOnly SyntaxMap As Func(Of SyntaxNode, SyntaxNode)
    End Structure

    ''' <summary>
    ''' Matches symbols from an assembly in one compilation to
    ''' the corresponding assembly in another. Assumes that only
    ''' one assembly has changed between the two compilations.
    ''' </summary>
    Friend NotInheritable Class DefinitionMap
        Inherits Microsoft.CodeAnalysis.Emit.DefinitionMap

        Private ReadOnly [module] As PEModule
        Private ReadOnly metadataDecoder As MetadataDecoder
        Private ReadOnly mapToMetadata As SymbolMatcher
        Private ReadOnly mapToPrevious As SymbolMatcher
        Private ReadOnly methodMap As IReadOnlyDictionary(Of MethodSymbol, MethodDefinitionEntry)

        Public Sub New(
                [module] As PEModule,
                 metadataDecoder As MetadataDecoder,
                 mapToMetadata As SymbolMatcher,
                 mapToPrevious As SymbolMatcher,
                 methodMap As IReadOnlyDictionary(Of MethodSymbol, MethodDefinitionEntry))

            Debug.Assert([module] IsNot Nothing)
            Debug.Assert(metadataDecoder IsNot Nothing)
            Debug.Assert(mapToMetadata IsNot Nothing)
            Debug.Assert(methodMap IsNot Nothing)

            Me.module = [module]
            Me.metadataDecoder = metadataDecoder
            Me.mapToMetadata = mapToMetadata
            Me.mapToPrevious = If(mapToPrevious, mapToMetadata)
            Me.methodMap = methodMap
        End Sub

        Friend Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
            Return Me.mapToPrevious.TryGetAnonymousTypeName(template, name, index)
        End Function

        Friend Overrides Function TryGetTypeHandle(def As ITypeDefinition, ByRef handle As TypeHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PENamedTypeSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetEventHandle(def As IEventDefinition, ByRef handle As EventHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEEventSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetFieldHandle(def As IFieldDefinition, ByRef handle As FieldHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEFieldSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetMethodHandle(def As IMethodDefinition, ByRef handle As MethodHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEMethodSymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function TryGetPropertyHandle(def As IPropertyDefinition, ByRef handle As PropertyHandle) As Boolean
            Dim other = TryCast(Me.mapToMetadata.MapDefinition(def), PEPropertySymbol)
            If other IsNot Nothing Then
                handle = other.Handle
                Return True
            Else
                handle = Nothing
                Return False
            End If
        End Function

        Friend Overrides Function DefinitionExists(def As IDefinition) As Boolean
            Dim previous = Me.mapToPrevious.MapDefinition(def)
            Return previous IsNot Nothing
        End Function

        Friend Overrides Function TryGetPreviousLocals(
                             baseline As EmitBaseline,
                             method As IMethodSymbol,
                             <Out()> ByRef previousLocals As ImmutableArray(Of EncLocalInfo),
                             <Out()> ByRef getPreviousLocalSlot As GetPreviousLocalSlot) As Boolean

            previousLocals = Nothing
            getPreviousLocalSlot = DefinitionMap.NoPreviousLocalSlot

            Dim handle As MethodHandle = Nothing
            If Not Me.TryGetMethodHandle(baseline, CType(method, IMethodDefinition), handle) Then
                ' Unrecognized method. Must have been added in the current compilation.
                Return False
            End If

            Dim methodEntry As MethodDefinitionEntry = Nothing
            If Not Me.methodMap.TryGetValue(DirectCast(method, MethodSymbol), methodEntry) Then
                ' Not part of changeset. No need to preserve locals.
                Return False
            End If

            If Not methodEntry.PreserveLocalVariables Then
                ' Not necessary to preserve locals.
                Return False
            End If

            Dim previousMethod As MethodSymbol = CType(methodEntry.PreviousMethod, MethodSymbol)
            Dim methodIndex As UInteger = CUInt(MetadataTokens.GetRowNumber(handle))
            Dim map As SymbolMatcher

            ' Check if method has changed previously. If so, we already have a map.
            If baseline.LocalsForMethodsAddedOrChanged.TryGetValue(methodIndex, previousLocals) Then
                map = Me.mapToPrevious
            Else
                ' Method has not changed since initial generation. Generate a map
                ' using the local names provided with the initial metadata.
                Dim localNames As ImmutableArray(Of String) = baseline.LocalNames.Invoke(methodIndex)
                Debug.Assert(Not localNames.IsDefault)

                Dim localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo) = Nothing
                Try
                    Debug.Assert(Me.module.HasIL)
                    Dim methodIL As MethodBodyBlock = Me.module.GetMethodILOrThrow(handle)
                    If Not methodIL.LocalSignature.IsNil Then
                        Dim signature = Me.module.MetadataReader.GetLocalSignature(methodIL.LocalSignature)
                        localInfo = Me.metadataDecoder.DecodeLocalSignatureOrThrow(signature)
                    Else
                        localInfo = ImmutableArray(Of MetadataDecoder.LocalInfo).Empty
                    End If
                Catch ex As UnsupportedSignatureContent
                Catch ex As BadImageFormatException
                End Try

                If localInfo.IsDefault Then
                    ' TODO: Report error that metadata is not supported.
                    Return False
                End If

                ' The signature may have more locals than names if trailing locals are unnamed.
                ' (Locals in the middle of the signature may be unnamed too but since localNames
                ' Is indexed by slot, unnamed locals before the last named local will be represented
                ' as null values in the array.)
                Debug.Assert(localInfo.Length >= localNames.Length)
                previousLocals = GetLocalSlots(previousMethod, localNames, localInfo)
                Debug.Assert(previousLocals.Length = localInfo.Length)

                map = Me.mapToMetadata
            End If

            ' Find declarators in previous method syntax.
            ' The locals are indices into this list.
            Dim previousDeclarators As ImmutableArray(Of VisualBasicSyntaxNode) = GetLocalVariableDeclaratorsVisitor.GetDeclarators(previousMethod)

            ' Create a map from declarator to declarator offset.
            Dim previousDeclaratorToOffset = New Dictionary(Of VisualBasicSyntaxNode, Integer)()
            For offset As Integer = 0 To previousDeclarators.Length - 1
                previousDeclaratorToOffset.Add(previousDeclarators(offset), offset)
            Next

            ' Create a map from local info to slot.
            Dim previousLocalInfoToSlot As Dictionary(Of EncLocalInfo, Integer) = New Dictionary(Of EncLocalInfo, Integer)()
            For slot As Integer = 0 To previousLocals.Length - 1
                Dim localInfo As EncLocalInfo = previousLocals(slot)
                Debug.Assert(Not localInfo.IsDefault)
                If localInfo.IsInvalid Then
                    ' Unrecognized or deleted local.
                    Continue For
                End If
                previousLocalInfoToSlot.Add(localInfo, slot)
            Next

            Dim syntaxMap As Func(Of SyntaxNode, SyntaxNode) = methodEntry.SyntaxMap
            If syntaxMap Is Nothing Then
                ' If there was no syntax map, the syntax structure has not changed,
                ' so we can map from current to previous syntax by declarator index.
                Debug.Assert(methodEntry.PreserveLocalVariables)
                ' Create a map from declarator to declarator index.

                Dim currentDeclarators As ImmutableArray(Of VisualBasicSyntaxNode) = GetLocalVariableDeclaratorsVisitor.GetDeclarators(DirectCast(method, MethodSymbol))
                Dim currentDeclaratorToIndex = CreateDeclaratorToIndexMap(currentDeclarators)
                syntaxMap = Function(currentSyntax As SyntaxNode)
                                Dim currentIndex As Integer = currentDeclaratorToIndex(DirectCast(currentSyntax, VisualBasicSyntaxNode))
                                Return previousDeclarators(currentIndex)
                            End Function
            End If

            getPreviousLocalSlot = Function(identity As Object, typeRef As ITypeReference, constraints As LocalSlotConstraints)
                                       Dim local = DirectCast(identity, LocalSymbol)
                                       Dim syntaxRefs = local.DeclaringSyntaxReferences
                                       Debug.Assert(Not syntaxRefs.IsDefault)

                                       If Not syntaxRefs.IsDefaultOrEmpty Then
                                           Dim currentSyntax As SyntaxNode = syntaxRefs(0).GetSyntax(Nothing)
                                           Dim previousSyntax = DirectCast(syntaxMap(currentSyntax), VisualBasicSyntaxNode)

                                           Dim offset As Integer = Nothing
                                           If previousSyntax IsNot Nothing AndAlso previousDeclaratorToOffset.TryGetValue(previousSyntax, offset) Then
                                               Dim previousType = map.MapReference(typeRef)
                                               If previousType IsNot Nothing Then
                                                   Dim localKey = New EncLocalInfo(offset, previousType, constraints, CInt(local.TempKind))
                                                   Dim slot As Integer
                                                   If previousLocalInfoToSlot.TryGetValue(localKey, slot) Then
                                                       Return slot
                                                   End If
                                               End If
                                           End If
                                       End If

                                       Return -1
                                   End Function
            Return True
        End Function

        Private Overloads Function TryGetMethodHandle(baseline As EmitBaseline, def As IMethodDefinition, <Out()> ByRef handle As MethodHandle) As Boolean
            If Me.TryGetMethodHandle(def, handle) Then
                Return True
            End If

            def = DirectCast(Me.mapToPrevious.MapDefinition(def), IMethodDefinition)
            If def IsNot Nothing Then
                Dim methodIndex As UInteger = 0
                If baseline.MethodsAdded.TryGetValue(def, methodIndex) Then
                    handle = MetadataTokens.MethodHandle(CInt(methodIndex))
                    Return True
                End If
            End If

            handle = Nothing
            Return False
        End Function

        Friend Overrides Function GetLocalInfo(methodDef As IMethodDefinition, localDefs As ImmutableArray(Of LocalDefinition)) As ImmutableArray(Of EncLocalInfo)
            If localDefs.IsEmpty Then
                Return ImmutableArray(Of EncLocalInfo).Empty
            End If

            ' Find declarators in current method syntax.
            Dim declarators As ImmutableArray(Of VisualBasicSyntaxNode) = GetLocalVariableDeclaratorsVisitor.GetDeclarators(DirectCast(methodDef, MethodSymbol))

            ' Create a map from declarator to declarator index.
            Dim declaratorToIndex As IReadOnlyDictionary(Of SyntaxNode, Integer) = CreateDeclaratorToIndexMap(declarators)

            Return localDefs.SelectAsArray(Function(localDef) GetLocalInfo(declaratorToIndex, localDef))
        End Function

        Private Overloads Shared Function GetLocalInfo(declaratorToIndex As IReadOnlyDictionary(Of SyntaxNode, Integer), localDef As LocalDefinition) As EncLocalInfo
            ' Local symbol will be null for short-lived temporaries.
            Dim local = DirectCast(localDef.Identity, LocalSymbol)
            If local IsNot Nothing Then
                Dim syntaxRefs = local.DeclaringSyntaxReferences
                Debug.Assert(Not syntaxRefs.IsDefault)

                If Not syntaxRefs.IsDefaultOrEmpty Then
                    Dim syntax As SyntaxNode = syntaxRefs(0).GetSyntax()
                    Return New EncLocalInfo(declaratorToIndex(syntax), localDef.Type, localDef.Constraints, CInt(local.TempKind))
                End If
            End If
            Return New EncLocalInfo(localDef.Type, localDef.Constraints)
        End Function

        Private Shared Function CreateDeclaratorToIndexMap(declarators As ImmutableArray(Of VisualBasicSyntaxNode)) As IReadOnlyDictionary(Of SyntaxNode, Integer)
            Dim declaratorToIndex As Dictionary(Of SyntaxNode, Integer) = New Dictionary(Of SyntaxNode, Integer)()
            For i As Integer = 0 To declarators.Length - 1
                declaratorToIndex.Add(declarators(i), i)
            Next
            Return declaratorToIndex
        End Function

        ''' <summary>
        ''' Match local declarations to names to generate a map from
        ''' declaration to local slot. The names are indexed by slot and the
        ''' assumption is that declarations are in the same order as slots.
        ''' </summary>
        Private Shared Function GetLocalSlots(method As MethodSymbol,
                                              localNames As ImmutableArray(Of String),
                                              localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo)) As ImmutableArray(Of EncLocalInfo)

            Dim syntaxRefs = method.DeclaringSyntaxReferences

            ' No syntax refs for synthesized methods.
            If syntaxRefs.Length = 0 Then
                Return ImmutableArray(Of EncLocalInfo).Empty
            End If

            Dim syntax = syntaxRefs(0).GetSyntax()
            Dim block = syntax.Parent
            Debug.Assert(TypeOf block Is MethodBlockBaseSyntax)

            Dim map As New Dictionary(Of EncLocalInfo, Integer)()
            Dim visitor = GetLocalsVisitor.GetLocals(block, localNames, localInfo, map)
            Dim locals(localInfo.Length - 1) As EncLocalInfo
            For Each pair In map
                locals(pair.Value) = pair.Key
            Next

            ' Populate any remaining locals that were not matched to source.
            For i = 0 To locals.Length - 1
                If locals(i).IsDefault Then
                    Dim info = localInfo(i)
                    Dim constraints = GetConstraints(info)
                    locals(i) = New EncLocalInfo(DirectCast(info.Type, ITypeReference), constraints)
                End If
            Next

            Return ImmutableArray.Create(locals)
        End Function

        Private Shared Function GetConstraints(info As MetadataDecoder.LocalInfo) As LocalSlotConstraints
            Return If(info.IsPinned, LocalSlotConstraints.Pinned, LocalSlotConstraints.None) Or
                If(info.IsByRef, LocalSlotConstraints.ByRef, LocalSlotConstraints.None)
        End Function

        Private NotInheritable Class GetLocalsVisitor
            Inherits LocalVariableDeclaratorsVisitor

            <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
            Private Structure LocalName
                Public ReadOnly Name As String
                Public ReadOnly Kind As TempKind
                Public ReadOnly UniqueId As Integer

                Public Sub New(name As String, kind As TempKind, uniqueId As Integer)
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

            Public Shared Function GetLocals(block As SyntaxNode, localNames As ImmutableArray(Of String), localInfo As ImmutableArray(Of MetadataDecoder.LocalInfo), locals As Dictionary(Of EncLocalInfo, Integer)) As GetLocalsVisitor
                Dim visitor = New GetLocalsVisitor(localNames, localInfo, locals)
                Call (New ExplicitDeclarationCollector(visitor.knownDeclaredLocals)).Visit(block)
                visitor.Visit(block)
                Return visitor
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
                Dim kindOpt As TempKind? = Me.TryGetSlotIndex(TempKind.ForEachEnumerator, TempKind.ForEachArray)
                If kindOpt.HasValue Then
                    ' Enumerator.
                    If kindOpt.Value = TempKind.ForEachArray Then
                        ' Index (VB only specialcases ForEach in single-dimensional case).
                        Dim kind = TempKind.ForEachArrayIndex
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
                Dim kindOpt As TempKind? = Me.TryGetSlotIndex(TempKind.ForLoopObject)

                If kindOpt.HasValue Then
                    Dim kind = TempKind.ForLimit
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                    End If

                    kind = TempKind.ForStep
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                    End If

                    kind = TempKind.ForDirection
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
                If Me.TryGetSlotIndex(TempKind.Lock).HasValue Then
                    ' if the next local Is LockTaken, then the lock was emitted with the two argument
                    ' overload for Monitor.Enter(). Otherwise, the single argument overload was used.
                    If IsSlotIndex(TempKind.LockTaken) Then
                        AddLocal(TempKind.LockTaken)
                    End If
                End If
                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitUsingStatementDeclarations(node As UsingStatementSyntax)
                Dim expr = node.Expression
                If expr IsNot Nothing Then
                    Me.TryGetSlotIndex(TempKind.Using)
                End If
                Me.offset += 1
            End Sub

            Protected Overrides Sub VisitWithStatementDeclarations(node As WithStatementSyntax)
                Me.TryGetSlotIndex(TempKind.With)
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
                    Return New LocalName(name, TempKind.None, 0)
                End If
                Dim kind As TempKind
                Dim uniqueId As Integer
                GeneratedNames.TryParseTemporaryName(name, kind, uniqueId)
                Return New LocalName(name, kind, uniqueId)
            End Function

            Private Function IsSlotIndex(name As String) As Boolean
                Dim slot = Me.slotIndex
                Return IsSlotIndex(name, slot)
            End Function

            Private Function IsSlotIndex(name As String, slot As Integer) As Boolean
                Return slot < Me.localNames.Length AndAlso Me.localNames(slot).Kind = TempKind.None AndAlso name = Me.localNames(slot).Name
            End Function

            Private Function IsSlotIndex(kind As TempKind) As Boolean
                Return Me.slotIndex < Me.localNames.Length AndAlso Me.localNames(Me.slotIndex).Kind = kind
            End Function

            Private Function IsSlotIndex(ParamArray kinds As TempKind()) As Boolean
                Return Me.slotIndex < Me.localNames.Length AndAlso Array.IndexOf(Of TempKind)(kinds, Me.localNames(Me.slotIndex).Kind) >= 0
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

            Private Function TryGetSlotIndex(kind As TempKind) As TempKind?
                While Me.slotIndex < Me.localNames.Length
                    If Me.IsSlotIndex(kind) Then
                        Me.AddLocal(kind)
                        Return New TempKind?(kind)
                    End If
                    Me.slotIndex += 1
                End While
                Return Nothing
            End Function

            Private Function TryGetSlotIndex(ParamArray kinds As TempKind()) As TempKind?
                While Me.slotIndex < Me.localNames.Length
                    If Me.IsSlotIndex(kinds) Then
                        Dim kind As TempKind = Me.localNames(Me.slotIndex).Kind
                        Me.AddLocal(kind)
                        Return New TempKind?(kind)
                    End If
                    Me.slotIndex += 1
                End While
                Return Nothing
            End Function

            Private Function TryGetSlotIndexAtCurrentOffsetOrBefore(name As String) As Boolean
                Dim slot = 0
                While slot <= Me.slotIndex
                    If Me.IsSlotIndex(name, slot) Then
                        Me.AddLocal(TempKind.None, slot, name)
                        If (slot = Me.slotIndex) Then
                            Me.slotIndex += 1
                        End If
                        Return True
                    End If
                    slot += 1
                End While
                Return False
            End Function

            Private Sub AddLocal(tempKind As TempKind)
                Debug.Assert(tempKind <> TempKind.None, "None must have name")
                Dim slot = Me.slotIndex
                AddLocal(tempKind, slot, Nothing)
                Me.slotIndex += 1
            End Sub

            Private Sub AddLocal(name As String)
                Dim slot = Me.slotIndex
                AddLocal(TempKind.None, slot, name)
                Me.slotIndex += 1
            End Sub

            Private Sub AddLocal(tempKind As TempKind, slot As Integer, name As String)
                Dim info As MetadataDecoder.LocalInfo = Me.localInfo(slot)

                ' We do not emit custom modifiers on locals so ignore the
                ' previous version of the local if it had custom modifiers.
                If info.CustomModifiers.IsDefaultOrEmpty Then
                    Dim constraints = GetConstraints(info)
                    Dim local As EncLocalInfo = New EncLocalInfo(Me.offset, CType(info.Type, ITypeReference), constraints, CInt(tempKind))
                    Me.locals.Add(local, slot)
                    If name IsNot Nothing Then
                        Me.knownDeclaredLocals.Add(name)
                    End If
                End If
            End Sub
        End Class

    End Class

    Friend NotInheritable Class GetLocalVariableDeclaratorsVisitor
        Inherits LocalVariableDeclaratorsVisitor

        Private ReadOnly builder As ArrayBuilder(Of VisualBasicSyntaxNode)

        Friend Shared Function GetDeclarators(method As MethodSymbol) As ImmutableArray(Of VisualBasicSyntaxNode)
            Dim syntaxRefs = method.DeclaringSyntaxReferences
            If syntaxRefs.Length = 0 Then
                Return ImmutableArray(Of VisualBasicSyntaxNode).Empty
            End If
            Dim syntax As SyntaxNode = syntaxRefs(0).GetSyntax(Nothing)
            Dim block = syntax.Parent
            Debug.Assert(TypeOf block Is MethodBlockBaseSyntax)

            Dim builder = ArrayBuilder(Of VisualBasicSyntaxNode).GetInstance()
            Call New GetLocalVariableDeclaratorsVisitor(builder).Visit(block)
            Return builder.ToImmutableAndFree()
        End Function

        Public Sub New(builder As ArrayBuilder(Of VisualBasicSyntaxNode))
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
End Namespace