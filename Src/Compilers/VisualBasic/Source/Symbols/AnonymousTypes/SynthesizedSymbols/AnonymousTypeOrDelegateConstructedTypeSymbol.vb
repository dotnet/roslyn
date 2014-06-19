Imports System.Collections.Generic
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AnonymousTypeManager

        Friend MustInherit Class AnonymousTypeOrDelegateConstructedTypeSymbol
            Inherits SubstitutedNamedType.ConstructedInstanceType

            Public ReadOnly TypeDescriptor As AnonymousTypeDescriptor

            Public Sub New(substitution As TypeSubstitution, descriptor As AnonymousTypeDescriptor)
                MyBase.New(substitution)

                ' Anonymous Types are never nested, we are taking advantage of this when we implement InternalSubstituteTypeParameters.
                Debug.Assert(substitution.TargetGenericDefinition.ContainingSymbol.Kind = SymbolKind.Namespace)
                Me.TypeDescriptor = descriptor
            End Sub

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property IsAnonymousType As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
                Get
                    Return ReadOnlyArray(Of Location).CreateFrom(Me.TypeDescriptor.Location)
                End Get
            End Property

            ''' <summary>
            ''' Adjust name of template's fields based on smallest location of the type descriptor. 
            ''' </summary>
            Friend Sub AdjustMetadataNamesInTemplate()
                Debug.Assert(Me.TypeDescriptor.Location.InSource)
                DirectCast(Me.OriginalNamedTypeDefinition, AnonymousTypeOrDelegateTemplateSymbol).AdjustMetadataNames(Me.TypeDescriptor)
            End Sub

            Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeSymbol
                If additionalSubstitution Is Nothing Then
                    Return Me
                End If

                Dim oldSubstitution As TypeSubstitution = Me.TypeSubstitution
                Dim newSubstitution As TypeSubstitution = VisualBasic.TypeSubstitution.AdjustForConstruct(Nothing, oldSubstitution, additionalSubstitution)
                Contract.ThrowIfNull(newSubstitution) ' Why would we ever cancel old substitution out?

                If newSubstitution IsNot oldSubstitution Then
                    Return CompleteInternalSubstituteTypeParameters(newSubstitution)
                End If

                ' No effect.
                Return Me
            End Function

            Protected MustOverride Function CompleteInternalSubstituteTypeParameters(newSubstitution As TypeSubstitution) As TypeSymbol
        End Class

    End Class
End Namespace