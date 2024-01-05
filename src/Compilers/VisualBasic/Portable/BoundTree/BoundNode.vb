' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend MustInherit Class BoundNode
        Private ReadOnly _kind As BoundKind
        Private _attributes As BoundNodeAttributes
        Private ReadOnly _syntax As SyntaxNode

        <Flags()>
        Private Enum BoundNodeAttributes As Byte
            HasErrors = 1 'NOTE: the bit means "NOT OK". So that default is OK state.
            WasCompilerGenerated = 1 << 1
#If DEBUG Then
            ''' <summary>
            ''' Captures the fact that consumers of the node already checked the state of the WasCompilerGenerated bit.
            ''' Allows to assert on attempts to set WasCompilerGenerated bit after that.
            ''' </summary>
            WasCompilerGeneratedIsChecked = 1 << 2
#End If
        End Enum

        Public Sub New(kind As BoundKind, syntax As SyntaxNode)
            ValidateLocationInformation(kind, syntax)

            _kind = kind
            _syntax = syntax
        End Sub

        Public Sub New(kind As BoundKind, syntax As SyntaxNode, hasErrors As Boolean)
            MyClass.New(kind, syntax)

            If hasErrors Then
                _attributes = BoundNodeAttributes.HasErrors
            End If
        End Sub

        Protected Sub CopyAttributes(node As BoundNode)
            If node.WasCompilerGenerated Then
                Me.SetWasCompilerGenerated()
            End If
        End Sub

        <Conditional("DEBUG")>
        Private Shared Sub ValidateLocationInformation(kind As BoundKind, syntax As SyntaxNode)
            ' We should always have a syntax node and a syntax tree as well, unless it is a hidden sequence point.
            ' If it's a sequence point, it must have a syntax tree to retrieve the file name.
            Debug.Assert(kind = BoundKind.SequencePoint OrElse kind = BoundKind.SequencePointExpression OrElse syntax IsNot Nothing)
        End Sub

        Public ReadOnly Property HasErrors As Boolean
            Get
                Return (_attributes And BoundNodeAttributes.HasErrors) <> 0
            End Get
        End Property

        ''' <summary>
        ''' The node should not be treated as a direct semantical representation of the syntax it is associated with. 
        ''' Some examples: 
        ''' - implicit call for base constructor is associated with the constructor syntax.
        ''' - code in compiler generated constructor is associated with the type declaration.
        ''' 
        ''' Nodes marked this way are likely to be skipped by SemanticModel, Sequence Point rewriter, etc.
        ''' </summary>
        Public ReadOnly Property WasCompilerGenerated As Boolean
            Get
#If DEBUG Then
                _attributes = _attributes Or BoundNodeAttributes.WasCompilerGeneratedIsChecked
#End If
                Return (_attributes And BoundNodeAttributes.WasCompilerGenerated) <> 0
            End Get
        End Property

        Public Sub SetWasCompilerGenerated()
#If DEBUG Then
            Debug.Assert((_attributes And BoundNodeAttributes.WasCompilerGeneratedIsChecked) = 0)
#End If
            _attributes = _attributes Or BoundNodeAttributes.WasCompilerGenerated
        End Sub

        Public ReadOnly Property Kind As BoundKind
            Get
                Return _kind
            End Get
        End Property

        Public ReadOnly Property Syntax As SyntaxNode
            Get
                Return _syntax
            End Get
        End Property

        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return DirectCast(_syntax.SyntaxTree, VisualBasicSyntaxTree)
            End Get
        End Property

        Public Overridable Overloads Function Accept(visitor As BoundTreeVisitor) As BoundNode
            Throw ExceptionUtilities.Unreachable
        End Function

#If DEBUG Then
#Disable Warning IDE0051 ' Remove unused private members
        Private Function Dump() As String
#Enable Warning IDE0051 ' Remove unused private members
            Return TreeDumper.DumpCompact(BoundTreeDumperNodeProducer.MakeTree(Me))
        End Function

        Public Overloads Function MemberwiseClone(Of T As BoundNode)() As T
            Return DirectCast(Me.MemberwiseClone(), T)
        End Function
#End If
    End Class

    ' Indicates how a particular method group or property group was qualified, so that the right
    ' errors/warnings can be generated after overload resolution.
    Friend Enum QualificationKind
        Unqualified             ' Unqualified -- a simple name
        QualifiedViaValue       ' Qualified through an expression that produces a variable or value
        QualifiedViaTypeName    ' Qualified through an expression that was a type name.
        QualifiedViaNamespace   ' Qualified through an expression that was a namespace name.
    End Enum
End Namespace
