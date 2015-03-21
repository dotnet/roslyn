' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class EventSymbol
        Implements Cci.IEventDefinition

        Private ReadOnly Property IEventDefinitionAccessors As IEnumerable(Of Cci.IMethodReference) Implements Cci.IEventDefinition.Accessors
            Get
                CheckDefinitionInvariant()
                Dim addMethod = Me.AddMethod
                Debug.Assert(addMethod IsNot Nothing)

                Dim removeMethod = Me.RemoveMethod
                Debug.Assert(removeMethod IsNot Nothing)

                Dim raiseMethod = Me.RaiseMethod
                If raiseMethod IsNot Nothing Then
                    Return {addMethod, removeMethod, raiseMethod}
                Else
                    Return {addMethod, removeMethod}
                End If
            End Get
        End Property

        Private ReadOnly Property IEventDefinitionAdder As Cci.IMethodReference Implements Cci.IEventDefinition.Adder
            Get
                CheckDefinitionInvariant()
                Dim addMethod As MethodSymbol = Me.AddMethod
                Debug.Assert(addMethod IsNot Nothing)
                Return addMethod
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionRemover As Cci.IMethodReference Implements Cci.IEventDefinition.Remover
            Get
                CheckDefinitionInvariant()
                Dim removeMethod As MethodSymbol = Me.RemoveMethod
                Debug.Assert(removeMethod IsNot Nothing)
                Return removeMethod
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionIsRuntimeSpecial As Boolean Implements Cci.IEventDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return HasRuntimeSpecialName
            End Get

        End Property

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private ReadOnly Property IEventDefinitionIsSpecialName As Boolean Implements Cci.IEventDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return Me.HasSpecialName
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionCaller As Cci.IMethodReference Implements Cci.IEventDefinition.Caller
            Get
                CheckDefinitionInvariant()
                Return Me.RaiseMethod
            End Get

        End Property

        Private Overloads Function IEventDefinitionGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IEventDefinition.GetType
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(Me.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IEventDefinitionContainingTypeDefinition As Cci.ITypeDefinition Implements Cci.IEventDefinition.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return Me.ContainingType
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionVisibility As Cci.TypeMemberVisibility Implements Cci.IEventDefinition.Visibility
            Get
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me)
            End Get

        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            CheckDefinitionInvariant()
            Return Me.ContainingType
        End Function

        Friend Overrides Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) ' Implements Cci.IReference.Dispatch
            CheckDefinitionInvariant()
            visitor.Visit(DirectCast(Me, Cci.IEventDefinition))
        End Sub

        Friend Overrides Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition 'Implements Cci.IReference.AsDefinition
            CheckDefinitionInvariant()
            Return Me
        End Function

        Private ReadOnly Property IEventDefinitionName As String Implements Cci.IEventDefinition.Name
            Get
                CheckDefinitionInvariant()
                Return Me.MetadataName
            End Get
        End Property

    End Class

End Namespace

