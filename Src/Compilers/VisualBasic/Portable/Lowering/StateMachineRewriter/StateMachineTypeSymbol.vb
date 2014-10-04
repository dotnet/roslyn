' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class StateMachineTypeSymbol
        Inherits SynthesizedContainer
        Implements ISynthesizedMethodBodyImplementationSymbol

        Public ReadOnly KickoffMethod As MethodSymbol

        Public Sub New(kickoffMethod As MethodSymbol,
                       name As String,
                       baseType As NamedTypeSymbol,
                       originalInterfaces As ImmutableArray(Of NamedTypeSymbol))
            MyBase.New(kickoffMethod, name, baseType, originalInterfaces)

            Debug.Assert(kickoffMethod IsNot Nothing)

            ' If the async method is partial the kickoff method should be the method with body (implementation)
            Debug.Assert(kickoffMethod.IsDefinition)
            Debug.Assert(Not kickoffMethod.IsPartial OrElse kickoffMethod.PartialImplementationPart Is Nothing)

            Me.KickoffMethod = kickoffMethod
        End Sub

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                ' This method contains user code from the async method
                Return True
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return KickoffMethod
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
