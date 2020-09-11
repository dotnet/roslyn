' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend Class EventSymbol
        Implements Cci.IEventDefinition

        Private Iterator Function IEventDefinitionAccessors(context As EmitContext) As IEnumerable(Of Cci.IMethodReference) Implements Cci.IEventDefinition.GetAccessors
            CheckDefinitionInvariant()

            Dim addMethod = Me.AddMethod
            Debug.Assert(addMethod IsNot Nothing)
            If addMethod.ShouldInclude(context) Then
                Yield addMethod
            End If

            Dim removeMethod = Me.RemoveMethod
            Debug.Assert(removeMethod IsNot Nothing)
            If removeMethod.ShouldInclude(context) Then
                Yield removeMethod
            End If

            Dim raiseMethod = Me.RaiseMethod
            If raiseMethod IsNot Nothing AndAlso raiseMethod.ShouldInclude(context) Then
                Yield raiseMethod
            End If
        End Function

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

