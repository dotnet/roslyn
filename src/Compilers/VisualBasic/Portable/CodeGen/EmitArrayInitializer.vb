' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    Friend Partial Class CodeGenerator

        Private Enum ArrayInitializerStyle
            ' Initialize every element
            Element

            ' Initialize all elements at once from a metadata blob
            Block

            ' Mixed case where there are some initializers that are constants and
            ' there is enough of them so that it makes sense to use block initialization
            ' followed by individual initialization of non-constant elements
            Mixed
        End Enum

        ''' <summary>
        ''' Entry point to the array initialization.
        ''' Assumes that we have newly created array on the stack.
        ''' 
        ''' inits could be an array of values for a single dimensional array
        ''' or an   array (of array)+  of values for a multidimensional case
        ''' 
        ''' in either case it is expected that number of leaf values will match number 
        ''' of elements in the array and nesting level should match the rank of the array.
        ''' </summary>
        Private Sub EmitArrayInitializers(arrayType As ArrayTypeSymbol, inits As BoundArrayInitialization)
            Dim initExprs = inits.Initializers
            Dim initializationStyle = ShouldEmitBlockInitializer(arrayType.ElementType, initExprs)

            If initializationStyle = ArrayInitializerStyle.Element Then
                Me.EmitElementInitializers(arrayType, initExprs, True)
            Else
                _builder.EmitArrayBlockInitializer(Me.GetRawData(initExprs), inits.Syntax, _diagnostics)

                If initializationStyle = ArrayInitializerStyle.Mixed Then
                    EmitElementInitializers(arrayType, initExprs, False)
                End If
            End If
        End Sub

        Private Sub EmitElementInitializers(arrayType As ArrayTypeSymbol,
                                            inits As ImmutableArray(Of BoundExpression),
                                            includeConstants As Boolean)

            If Not IsMultidimensionalInitializer(inits) Then
                EmitOnedimensionalElementInitializers(arrayType, inits, includeConstants)
            Else
                EmitMultidimensionalElementInitializers(arrayType, inits, includeConstants)
            End If
        End Sub

        Private Sub EmitOnedimensionalElementInitializers(arrayType As ArrayTypeSymbol,
                                    inits As ImmutableArray(Of BoundExpression),
                                    includeConstants As Boolean)

            For i As Integer = 0 To inits.Length - 1
                Dim init = inits(i)
                If ShouldEmitInitExpression(includeConstants, init) Then
                    _builder.EmitOpCode(ILOpCode.Dup)
                    _builder.EmitIntConstant(i)
                    EmitExpression(init, True)
                    EmitArrayElementStore(arrayType, init.Syntax)
                End If
            Next i
        End Sub

        ' if element init is not a constant we have no choice - we need to emit it
        ' if element is a nontrivial constant we do what includeConstants flag says
        ' if element is a null or Zero constant - no need to emit initializer, arrays are created zero inited.
        Private Shared Function ShouldEmitInitExpression(includeConstants As Boolean, init As BoundExpression) As Boolean
            Return init.ConstantValueOpt Is Nothing OrElse
                   (includeConstants AndAlso Not init.ConstantValueOpt.IsDefaultValue)
        End Function


        ''' <summary>
        ''' To handle array initialization of arbitrary rank it is convenient to 
        ''' approach multidimensional initialization as a recursively nested.
        ''' 
        ''' ForAll{i, j, k} Init(i, j, k) ===> 
        ''' ForAll{i} ForAll{j, k} Init(i, j, k) ===>
        ''' ForAll{i} ForAll{j} ForAll{k} Init(i, j, k)
        ''' 
        ''' This structure is used for capturing initializers of a given index and 
        ''' the index value itself.
        ''' </summary>
        Private Structure IndexDesc
            Public Sub New(Index As Integer, Initializers As ImmutableArray(Of BoundExpression))
                Me.Index = Index
                Me.Initializers = Initializers
            End Sub

            Public ReadOnly Index As Integer
            Public ReadOnly Initializers As ImmutableArray(Of BoundExpression)
        End Structure

        Private Sub EmitMultidimensionalElementInitializers(arrayType As ArrayTypeSymbol,
                                                            inits As ImmutableArray(Of BoundExpression),
                                                            includeConstants As Boolean)
            ' Using a List for the stack instead of the framework Stack because IEnumerable from Stack is top to bottom.
            ' This algorithm requires the IEnumerable to be from bottom to top. See extensions for List in CollectionExtensions.vb.

            Dim indices As New ArrayBuilder(Of IndexDesc)

            ' emit initializers for all values of the leftmost index.
            For i As Integer = 0 To inits.Length - 1
                indices.Push(New IndexDesc(i, DirectCast(inits(i), BoundArrayInitialization).Initializers))
                EmitAllElementInitializersRecursive(arrayType, indices, includeConstants)
            Next

            Debug.Assert(Not indices.Any)
        End Sub

        ''' <summary>
        ''' Emits all initializers that match indices on the stack recursively.
        ''' 
        ''' Example: 
        '''  if array has [0..2, 0..3, 0..2] shape
        '''  and we have {1, 2} indices on the stack
        '''  initializers for 
        '''              [1, 2, 0]
        '''              [1, 2, 1]
        '''              [1, 2, 2]
        ''' 
        '''  will be emitted and the top index will be pushed off the stack 
        '''  as at that point we would be completely done with emitting initializers 
        '''  corresponding to that index.
        ''' </summary>
        Private Sub EmitAllElementInitializersRecursive(arrayType As ArrayTypeSymbol,
                                                        indices As ArrayBuilder(Of IndexDesc),
                                                        includeConstants As Boolean)
            Dim top = indices.Peek
            Dim inits = top.Initializers

            If IsMultidimensionalInitializer(inits) Then
                ' emit initializers for the less significant indices recursively
                For i As Integer = 0 To inits.Length - 1
                    indices.Push(New IndexDesc(i, DirectCast(inits(i), BoundArrayInitialization).Initializers))
                    EmitAllElementInitializersRecursive(arrayType, indices, includeConstants)
                Next
            Else
                ' leaf case
                For i As Integer = 0 To inits.Length - 1
                    Dim init = inits(i)
                    If ShouldEmitInitExpression(includeConstants, init) Then
                        ' emit array ref
                        _builder.EmitOpCode(ILOpCode.Dup)

                        Debug.Assert(indices.Count = arrayType.Rank - 1)

                        ' emit values of all indices that are in progress
                        For Each row In indices
                            _builder.EmitIntConstant(row.Index)
                        Next

                        ' emit the leaf index
                        _builder.EmitIntConstant(i)

                        Dim initExpr = inits(i)
                        EmitExpression(initExpr, True)
                        EmitArrayElementStore(arrayType, init.Syntax)
                    End If
                Next
            End If

            indices.Pop()
        End Sub

        Private Function AsConstOrDefault(init As BoundExpression) As ConstantValue
            Dim initConstantValueOpt As ConstantValue = init.ConstantValueOpt

            If initConstantValueOpt IsNot Nothing Then
                Return initConstantValueOpt
            End If

            Dim type As TypeSymbol = init.Type.GetEnumUnderlyingTypeOrSelf
            Return ConstantValue.Default(type.SpecialType)
        End Function

        Private Function ShouldEmitBlockInitializer(elementType As TypeSymbol, inits As ImmutableArray(Of BoundExpression)) As ArrayInitializerStyle
            If Not _module.SupportsPrivateImplClass Then
                Return ArrayInitializerStyle.Element
            End If

            If elementType.IsEnumType() Then
                If Not _module.Compilation.EnableEnumArrayBlockInitialization Then
                    Return ArrayInitializerStyle.Element
                End If
                elementType = DirectCast(elementType, NamedTypeSymbol).EnumUnderlyingType
            End If

            If elementType.SpecialType.IsBlittable() Then
                If (_module.GetInitArrayHelper Is Nothing) Then
                    Return ArrayInitializerStyle.Element
                End If

                Dim initCount As Integer = 0
                Dim constCount As Integer = 0
                InitializerCountRecursive(inits, initCount, constCount)

                If initCount > 2 Then
                    If initCount = constCount Then
                        Return ArrayInitializerStyle.Block
                    End If

                    Dim thresholdCnt As Integer = Math.Max(3, (initCount \ 3))

                    If constCount >= thresholdCnt Then
                        Return ArrayInitializerStyle.Mixed
                    End If
                End If
            End If

            Return ArrayInitializerStyle.Element
        End Function

        ''' <summary>
        ''' Count of all initializers.
        ''' </summary>
        Private Sub InitializerCountRecursive(inits As ImmutableArray(Of BoundExpression), ByRef initCount As Integer, ByRef constInits As Integer)
            If inits.Length = 0 Then
                Return
            End If

            For Each init In inits
                Dim asArrayInit = TryCast(init, BoundArrayInitialization)

                If asArrayInit IsNot Nothing Then
                    InitializerCountRecursive(asArrayInit.Initializers, initCount, constInits)
                Else
                    ' NOTE Default values Do Not need To be initialized. 
                    '       .NET arrays are always zero-inited.
                    If Not init.IsDefaultValue() Then
                        initCount += 1
                        If init.ConstantValueOpt IsNot Nothing Then
                            constInits += 1
                        End If
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Produces a serialized blob of all constant initializers.
        ''' Non-constant initializers are matched with a zero of corresponding size.
        ''' </summary>
        Private Function GetRawData(initializers As ImmutableArray(Of BoundExpression)) As ImmutableArray(Of Byte)
            ' the initial size is a guess.
            ' there is no point to be precise here as MemoryStream always has N + 1 storage 
            ' and will need to be trimmed regardless
            Dim writer = Cci.PooledBlobBuilder.GetInstance(initializers.Length * 4)

            SerializeArrayRecursive(writer, initializers)

            Dim result = writer.ToImmutableArray()
            writer.Free()
            Return result
        End Function

        Private Sub SerializeArrayRecursive(bw As BlobBuilder, inits As ImmutableArray(Of BoundExpression))
            If inits.Length <> 0 Then
                If inits(0).Kind = BoundKind.ArrayInitialization Then
                    For Each init In inits
                        SerializeArrayRecursive(bw, DirectCast(init, BoundArrayInitialization).Initializers)
                    Next
                Else
                    For Each init In inits
                        Me.AsConstOrDefault(init).Serialize(bw)
                    Next
                End If
            End If
        End Sub

        ''' <summary>
        ''' Check if it is a regular collection of expressions or there are nested initializers.
        ''' </summary>
        Private Function IsMultidimensionalInitializer(inits As ImmutableArray(Of BoundExpression)) As Boolean
            Debug.Assert(inits.All(Function(init) init.Kind <> BoundKind.ArrayInitialization) OrElse
                         inits.All(Function(init) init.Kind = BoundKind.ArrayInitialization),
                         "all or none should be nested")

            Return inits.Length <> 0 AndAlso inits(0).Kind = BoundKind.ArrayInitialization
        End Function
    End Class

End Namespace
