' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports PreprocessorState = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Scanner.PreprocessorState
Imports Scanner = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Scanner

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicSyntaxTree
        ''' <summary>
        ''' Map containing information about all conditional symbol definitions in the source file corresponding to a parsed syntax tree.
        ''' </summary>
        Private Class ConditionalSymbolsMap

            ''' <summary>
            ''' Conditional symbols map, where each key-value pair indicates:
            '''   Key: Conditional symbol name.
            '''   Value: Stack of all active conditional symbol definitions, i.e. #Const directives, in the source file corresponding to a parsed syntax tree.
            '''          All the defining #Const directives for a conditional symbol are pushed onto this stack in source code order.
            '''          Each stack entry is a tuple {InternalSyntax.CConst, Integer} where:
            '''            InternalSyntax.CConst: Constant value of the symbol.
            '''            Integer: Source position of the defining #Const directive.
            ''' </summary>
            Private ReadOnly _conditionalsMap As ImmutableDictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer)))

            Friend Shared ReadOnly Uninitialized As ConditionalSymbolsMap = New ConditionalSymbolsMap()
            ' Only used by Uninitialized instance
            Private Sub New()
            End Sub

            Private Sub New(conditionalsMap As ImmutableDictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer))))
                Debug.Assert(conditionalsMap IsNot Nothing)
                Debug.Assert(conditionalsMap.Any())
#If DEBUG Then
                For Each kvPair In conditionalsMap
                    Dim conditionalStack As Stack(Of Tuple(Of InternalSyntax.CConst, Integer)) = kvPair.Value
                    Debug.Assert(conditionalStack.Any())

                    ' Ensure that all the defining #Const directives for this conditional symbol are pushed onto the stack in source code order.
                    Dim prevPosition As Integer = Int32.MaxValue
                    For i = 0 To conditionalStack.Count - 1
                        Dim position As Integer = conditionalStack(i).Item2
                        Debug.Assert(prevPosition >= position)
                        prevPosition = position
                    Next
                Next
#End If
                Me._conditionalsMap = conditionalsMap
            End Sub

#Region "Build conditional symbols map"
            Friend Shared Function Create(syntaxRoot As VisualBasicSyntaxNode, options As VisualBasicParseOptions) As ConditionalSymbolsMap
                Dim symbolsMapBuilder = New ConditionalSymbolsMapBuilder()
                Dim conditionalSymbolsMap As ImmutableDictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer))) = symbolsMapBuilder.Build(syntaxRoot, options)
                Debug.Assert(conditionalSymbolsMap Is Nothing OrElse conditionalSymbolsMap.Count > 0)
                Return If(conditionalSymbolsMap IsNot Nothing, New ConditionalSymbolsMap(conditionalSymbolsMap), Nothing)
            End Function

            Private Class ConditionalSymbolsMapBuilder
                Private _conditionalsMap As Dictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer)))
                Private _preprocessorState As PreprocessorState

                Friend Function Build(root As SyntaxNodeOrToken, options As VisualBasicParseOptions) As ImmutableDictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer)))
                    Me._conditionalsMap = New Dictionary(Of String, Stack(Of Tuple(Of InternalSyntax.CConst, Integer)))(IdentifierComparison.Comparer)

                    ' Process command line preprocessor symbol definitions.
                    Dim preprocessorSymbolsMap As ImmutableDictionary(Of String, InternalSyntax.CConst) = Scanner.GetPreprocessorConstants(options)
                    Me.ProcessCommandLinePreprocessorSymbols(preprocessorSymbolsMap)
                    Me._preprocessorState = New PreprocessorState(preprocessorSymbolsMap)

                    ' Get and process source directives.
                    Dim directives As IEnumerable(Of DirectiveTriviaSyntax) = root.GetDirectives(Of DirectiveTriviaSyntax)()
                    Debug.Assert(directives IsNot Nothing)
                    ProcessSourceDirectives(directives)

                    Return If(Me._conditionalsMap.Any(), ImmutableDictionary.CreateRange(IdentifierComparison.Comparer, Me._conditionalsMap), Nothing)
                End Function

                Private Sub ProcessCommandLinePreprocessorSymbols(preprocessorSymbolsMap As ImmutableDictionary(Of String, InternalSyntax.CConst))
                    For Each kvPair In preprocessorSymbolsMap
                        Me.ProcessConditionalSymbolDefinition(kvPair.Key, kvPair.Value, 0)
                    Next
                End Sub

                Private Sub ProcessConditionalSymbolDefinition(name As String, value As InternalSyntax.CConst, position As Integer)
                    Dim values As Stack(Of Tuple(Of InternalSyntax.CConst, Integer)) = Nothing
                    If Not _conditionalsMap.TryGetValue(name, values) Then
                        ' First definition for this conditional symbol in this source file, create a new key-value pair.
                        values = New Stack(Of Tuple(Of InternalSyntax.CConst, Integer))
                        _conditionalsMap.Add(name, values)
                    End If

                    values.Push(Tuple.Create(value, position))
                End Sub

                Private Sub ProcessSourceDirectives(directives As IEnumerable(Of DirectiveTriviaSyntax))
                    For Each directive In directives
                        ProcessDirective(directive)
                    Next
                End Sub

                ' Process all active conditional directives under trivia, in source code order.
                Private Sub ProcessDirective(directive As DirectiveTriviaSyntax)
                    Debug.Assert(_conditionalsMap IsNot Nothing)
                    Debug.Assert(directive IsNot Nothing)

                    Select Case directive.Kind
                        Case SyntaxKind.ConstDirectiveTrivia
                            Dim prevPreprocessorSymbols = _preprocessorState.SymbolsMap
                            _preprocessorState = Scanner.ApplyDirective(_preprocessorState, DirectCast(directive.Green(), InternalSyntax.DirectiveTriviaSyntax))
                            Dim newPreprocessorSymbols = _preprocessorState.SymbolsMap

                            If Not prevPreprocessorSymbols Is newPreprocessorSymbols Then
                                Dim name As String = DirectCast(directive, ConstDirectiveTriviaSyntax).Name.ValueText
#If DEBUG Then
                                Dim values As Stack(Of Tuple(Of InternalSyntax.CConst, Integer)) = Nothing
                                If Not _conditionalsMap.TryGetValue(name, values) Then
                                    ' First definition for this conditional symbol in this source file, create a new key-value pair.
                                    Debug.Assert(Not prevPreprocessorSymbols.ContainsKey(name))
                                Else
                                    ' Not the first definition for this conditional symbol in this source file.
                                    ' We must have an existing entry for this conditional symbol in prevPreprocessorSymbols map.
                                    Debug.Assert(values IsNot Nothing)
                                    Debug.Assert(prevPreprocessorSymbols.ContainsKey(name))
                                    Debug.Assert(Object.Equals(prevPreprocessorSymbols(name).ValueAsObject, values.Peek().Item1.ValueAsObject))
                                End If
#End If
                                ProcessConditionalSymbolDefinition(name, newPreprocessorSymbols(name), directive.SpanStart)
                            End If
                        Case Else
                            _preprocessorState = Scanner.ApplyDirective(_preprocessorState, DirectCast(directive.Green(), InternalSyntax.DirectiveTriviaSyntax))
                    End Select
                End Sub
            End Class

#End Region

            Friend Function GetPreprocessingSymbolInfo(conditionalSymbolName As String, node As IdentifierNameSyntax) As VisualBasicPreprocessingSymbolInfo
                Dim constValue As InternalSyntax.CConst = GetPreprocessorSymbolValue(conditionalSymbolName, node)
                If constValue Is Nothing Then
                    Return VisualBasicPreprocessingSymbolInfo.None
                End If

                ' Get symbol name at preprocessor definition, i.e. #Const directive.
                ' NOTE: symbolName and conditionalSymbolName might have different case, we want the definition name.
                Dim symbolName = _conditionalsMap.Keys.First(Function(key) IdentifierComparison.Equals(key, conditionalSymbolName))
                Return New VisualBasicPreprocessingSymbolInfo(New PreprocessingSymbol(name:=symbolName), constantValueOpt:=constValue.ValueAsObject, isDefined:=True)
            End Function

            Private Function GetPreprocessorSymbolValue(conditionalSymbolName As String, node As SyntaxNodeOrToken) As InternalSyntax.CConst
                Dim values As Stack(Of Tuple(Of InternalSyntax.CConst, Integer)) = Nothing
                If _conditionalsMap.TryGetValue(conditionalSymbolName, values) Then
                    ' All the defining #Const directives for a conditional symbol are pushed onto the stack in source code order.
                    ' Get the first entry from the top end of the stack with source position less then the source position of 'node'.
                    ' If there is none, then the given conditional symbol is undefined at 'node'
                    Dim position As Integer = node.SpanStart
                    For Each valueTuple In values
                        If valueTuple.Item2 < position Then
                            Return valueTuple.Item1
                        End If
                    Next
                End If

                Return Nothing
            End Function

            ' Returns a flag indicating whether the given conditional symbol is defined prior to the given node in source code order in this parsed syntax tree and
            ' it has a non-zero integral value or non-null string value.
            ' NOTE: These criteria are used by the native VB compiler.
            Friend Function IsConditionalSymbolDefined(conditionalSymbolName As String, node As SyntaxNodeOrToken) As Boolean
                If conditionalSymbolName IsNot Nothing Then
                    Dim constValue As InternalSyntax.CConst = GetPreprocessorSymbolValue(conditionalSymbolName, node)
                    If constValue IsNot Nothing AndAlso Not constValue.IsBad Then
                        Select Case constValue.SpecialType
                            Case SpecialType.System_Boolean
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of Boolean))
                                Return value.Value
                            Case SpecialType.System_Byte
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of Byte))
                                Return value.Value <> 0
                            Case SpecialType.System_Int16
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of Int16))
                                Return value.Value <> 0
                            Case SpecialType.System_Int32
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of Int32))
                                Return value.Value <> 0
                            Case SpecialType.System_Int64
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of Int64))
                                Return value.Value <> 0
                            Case SpecialType.System_SByte
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of SByte))
                                Return value.Value <> 0
                            Case SpecialType.System_UInt16
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of UInt16))
                                Return value.Value <> 0
                            Case SpecialType.System_UInt32
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of UInt32))
                                Return value.Value <> 0
                            Case SpecialType.System_UInt64
                                Dim value = DirectCast(constValue, InternalSyntax.CConst(Of UInt64))
                                Return value.Value <> 0
                            Case SpecialType.System_String
                                Debug.Assert(DirectCast(constValue, InternalSyntax.CConst(Of String)).Value IsNot Nothing)
                                Return True
                        End Select
                    End If
                End If

                Return False
            End Function

        End Class
    End Class
End Namespace
