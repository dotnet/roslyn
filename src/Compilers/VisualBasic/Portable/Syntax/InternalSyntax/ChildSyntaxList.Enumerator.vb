' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Structure ChildSyntaxList
        Friend Structure Enumerator
            Private ReadOnly _node As GreenNode
            Private _childIndex As Integer
            Private _list As GreenNode
            Private _listIndex As Integer
            Private _currentChild As GreenNode

            Friend Sub New(node As VisualBasicSyntaxNode)
                _node = node
                _childIndex = -1
                _listIndex = -1
                _list = Nothing
                _currentChild = Nothing
            End Sub

            Public Function MoveNext() As Boolean
                If _node IsNot Nothing Then
                    If _list IsNot Nothing Then
                        _listIndex = _listIndex + 1

                        If _listIndex < _list.SlotCount Then
                            _currentChild = _list.GetSlot(_listIndex)
                            Return True
                        End If

                        _list = Nothing
                        _listIndex = -1
                    End If

                    While True
                        _childIndex = _childIndex + 1

                        If _childIndex = _node.SlotCount Then
                            Exit While
                        End If

                        Dim child = _node.GetSlot(_childIndex)
                        If child Is Nothing Then
                            Continue While
                        End If

                        If CType(child.RawKind, SyntaxKind) = SyntaxKind.List Then
                            _list = child
                            _listIndex = _listIndex + 1

                            If _listIndex < _list.SlotCount Then
                                _currentChild = _list.GetSlot(_listIndex)
                                Return True
                            Else
                                _list = Nothing
                                _listIndex = -1
                                Continue While
                            End If
                        Else
                            _currentChild = child
                        End If

                        Return True
                    End While
                End If

                _currentChild = Nothing
                Return False
            End Function

            Public ReadOnly Property Current As GreenNode
                Get
                    Return _currentChild
                End Get
            End Property
        End Structure
    End Structure
End Namespace
