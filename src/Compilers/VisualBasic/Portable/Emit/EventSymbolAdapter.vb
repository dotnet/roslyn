' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class EventSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class EventSymbol
#End If
        Implements Cci.IEventDefinition

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private Iterator Function IEventDefinitionAccessors(context As EmitContext) As IEnumerable(Of Cci.IMethodReference) Implements Cci.IEventDefinition.GetAccessors
            CheckDefinitionInvariant()

            Dim addMethod = AdaptedEventSymbol.AddMethod.GetCciAdapter()
            Debug.Assert(addMethod IsNot Nothing)
            If addMethod.ShouldInclude(context) Then
                Yield addMethod
            End If

            Dim removeMethod = AdaptedEventSymbol.RemoveMethod.GetCciAdapter()
            Debug.Assert(removeMethod IsNot Nothing)
            If removeMethod.ShouldInclude(context) Then
                Yield removeMethod
            End If

            Dim raiseMethod = AdaptedEventSymbol.RaiseMethod?.GetCciAdapter()
            If raiseMethod IsNot Nothing AndAlso raiseMethod.ShouldInclude(context) Then
                Yield raiseMethod
            End If
        End Function

        Private ReadOnly Property IEventDefinitionAdder As Cci.IMethodReference Implements Cci.IEventDefinition.Adder
            Get
                CheckDefinitionInvariant()
                Dim addMethod = AdaptedEventSymbol.AddMethod.GetCciAdapter()
                Debug.Assert(addMethod IsNot Nothing)
                Return addMethod
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionRemover As Cci.IMethodReference Implements Cci.IEventDefinition.Remover
            Get
                CheckDefinitionInvariant()
                Dim removeMethod = AdaptedEventSymbol.RemoveMethod.GetCciAdapter()
                Debug.Assert(removeMethod IsNot Nothing)
                Return removeMethod
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionIsRuntimeSpecial As Boolean Implements Cci.IEventDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return AdaptedEventSymbol.HasRuntimeSpecialName
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionIsSpecialName As Boolean Implements Cci.IEventDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return AdaptedEventSymbol.HasSpecialName
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionCaller As Cci.IMethodReference Implements Cci.IEventDefinition.Caller
            Get
                CheckDefinitionInvariant()
                Return AdaptedEventSymbol.RaiseMethod?.GetCciAdapter()
            End Get

        End Property

        Private Overloads Function IEventDefinitionGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IEventDefinition.GetType
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(AdaptedEventSymbol.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IEventDefinitionContainingTypeDefinition As Cci.ITypeDefinition Implements Cci.IEventDefinition.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return AdaptedEventSymbol.ContainingType.GetCciAdapter()
            End Get

        End Property

        Private ReadOnly Property IEventDefinitionVisibility As Cci.TypeMemberVisibility Implements Cci.IEventDefinition.Visibility
            Get
                CheckDefinitionInvariant()
                Return AdaptedEventSymbol.MetadataVisibility
            End Get

        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            CheckDefinitionInvariant()
            Return AdaptedEventSymbol.ContainingType.GetCciAdapter()
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
                Return AdaptedEventSymbol.MetadataName
            End Get
        End Property
    End Class

    Partial Friend Class EventSymbol
#If DEBUG Then
        Private _lazyAdapter As EventSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As EventSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New EventSymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedEventSymbol As EventSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As EventSymbol
            Return Me
        End Function
#End If

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property
    End Class

#If DEBUG Then
    Partial Friend NotInheritable Class EventSymbolAdapter
        Friend ReadOnly Property AdaptedEventSymbol As EventSymbol

        Friend Sub New(underlyingEventSymbol As EventSymbol)
            AdaptedEventSymbol = underlyingEventSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedEventSymbol
            End Get
        End Property
    End Class
#End If
End Namespace

