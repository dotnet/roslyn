' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundMethodOrPropertyGroup

        ''' <summary>
        ''' returns name used to lookup the method/property in the group.
        ''' </summary>
        Friend ReadOnly Property MemberName() As String
            Get
                'NOTE: type characters are not included in method names.
                Select Case Me.Kind
                    Case BoundKind.MethodGroup
                        Dim methods = DirectCast(Me, BoundMethodGroup).Methods
                        Dim name As String = methods(0).Name

                        Debug.Assert(methods.All(Function(m) IdentifierComparison.Comparer.Compare(m.Name, name) = 0))
                        Return name

                    Case BoundKind.PropertyGroup
                        Dim properties = DirectCast(Me, BoundPropertyGroup).Properties
                        Dim name As String = properties(0).Name

                        Debug.Assert(properties.All(Function(m) IdentifierComparison.Comparer.Compare(m.Name, name) = 0))
                        Return name
                End Select

                Throw ExceptionUtilities.UnexpectedValue(Me.Kind)
            End Get
        End Property

        ''' <summary>
        ''' returns the container of the first member in the group.
        ''' </summary>
        Friend ReadOnly Property ContainerOfFirstInGroup() As TypeSymbol
            Get
                Select Case Me.Kind
                    Case BoundKind.MethodGroup
                        Return DirectCast(Me, BoundMethodGroup).Methods(0).ContainingType

                    Case BoundKind.PropertyGroup
                        Return DirectCast(Me, BoundPropertyGroup).Properties(0).ContainingType
                End Select

                Throw ExceptionUtilities.UnexpectedValue(Me.Kind)
            End Get
        End Property
    End Class
End Namespace
