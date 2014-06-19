'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Threading

Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Utilities for dealing with types in the compiler.
    ''' </summary>
    Friend Class TypeUtils
        ''' <summary>
        ''' Apply generic type arguments, returning the constructed type. Produces errors for constraints
        ''' that aren't validated. If the wrong number of type arguments are supplied, the set of types
        ''' is silently truncated or extended with the type parameters.
        ''' </summary>
        ''' <param name="genericType">The type to construct from</param>
        ''' <param name="typeArguments">The types to apply</param>
        ''' <param name="syntaxWhole">The place to report errors for the generic type as a whole</param>
        ''' <param name="syntaxArguments">The place to report errors for each generic type argument.</param>
        ''' <param name="binder">The binder to use.</param>
        ''' <returns>The constructed generic type.</returns>
        Public Shared Function ConstructAndValidateConstraints(ByVal genericType As NamedTypeSymbol,
                                                               ByVal typeArguments As TypeSymbol(),
                                                               ByVal syntaxWhole As SyntaxNode,
                                                               ByVal syntaxArguments As IEnumerable(Of SyntaxNode),
                                                               ByVal binder As Binder) As NamedTypeSymbol
            Contract.Requires(genericType IsNot Nothing)
            Contract.Requires(typeArguments IsNot Nothing)
            Contract.Requires(syntaxWhole IsNot Nothing)
            Contract.Requires(syntaxArguments IsNot Nothing)
            Contract.Requires(binder IsNot Nothing)
            Contract.Requires(typeArguments.Count = syntaxArguments.Count())

            If Not genericType.CanConstruct Then ' this also tests for Arity=0
                Return genericType ' nothing to construct.
            End If

            If genericType.Arity <> typeArguments.Count Then
                ' Fix type arguments to be of the right length.
                Dim newTypeArguments(0 To genericType.Arity - 1) As TypeSymbol
                For i = 0 To genericType.Arity - 1
                    If i < typeArguments.Count Then
                        newTypeArguments(i) = typeArguments(i)
                    Else
                        newTypeArguments(i) = genericType.TypeParameters(i)
                    End If
                Next
                typeArguments = newTypeArguments
            End If

            ' TODO: validate constraints.

            Return genericType.Construct(typeArguments)
        End Function

    End Class

End Namespace

