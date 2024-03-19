' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This is used while computing the values of constant fields.  Since they can depend on each
    ''' other, we need to keep track of which ones we are currently computing in order to avoid (and
    ''' report) cycles.
    ''' </summary>
    Friend NotInheritable Class ConstantFieldsInProgress

        Private ReadOnly _fieldOpt As SourceFieldSymbol
        Private ReadOnly _dependencies As Dependencies

        Friend Shared ReadOnly Empty As New ConstantFieldsInProgress(Nothing, Nothing)

        Friend Sub New(fieldOpt As SourceFieldSymbol, dependencies As Dependencies)
            _fieldOpt = fieldOpt
            _dependencies = dependencies
        End Sub

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return _fieldOpt Is Nothing
            End Get
        End Property

        Public Function AnyDependencies() As Boolean
            Return _dependencies.Any()
        End Function

        Friend Sub AddDependency(field As SourceFieldSymbol)
            _dependencies.Add(field)
        End Sub

#If DEBUG Then
        Friend NotInheritable Class Dependencies
            Private _isFrozen As Boolean
#Else
        Friend Structure Dependencies
#End If
            Private ReadOnly _builder As HashSet(Of SourceFieldSymbol)

            Friend Sub New(builder As HashSet(Of SourceFieldSymbol))
                Debug.Assert(builder IsNot Nothing)
                _builder = builder
            End Sub

            Friend Sub Add(field As SourceFieldSymbol)
#If DEBUG Then
                Debug.Assert(Not _isFrozen)
#End If
                _builder.Add(field)
            End Sub

            Friend Function Any() As Boolean
                Return _builder.Count <> 0
            End Function

            <Conditional("DEBUG")>
            Friend Sub Freeze()
#If DEBUG Then
                _isFrozen = True
#End If
            End Sub

#If DEBUG Then
        End Class
#Else
        End Structure
#End If
    End Class
End Namespace

