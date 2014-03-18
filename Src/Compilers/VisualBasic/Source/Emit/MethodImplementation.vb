' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Imports System
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend Class MethodImplementation
        Implements IMethodImplementation

        Private ReadOnly _implementing As IMethodDefinition
        Private ReadOnly _implemented As IMethodReference

        Public Sub New(implementing As IMethodDefinition, implemented As IMethodReference)
            Me._implementing = implementing
            Me._implemented = implemented
        End Sub

#Region "IMethodImplementation members"
        Public ReadOnly Property ContainingType As ITypeDefinition Implements IMethodImplementation.ContainingType
            Get
                Return _implementing.ContainingTypeDefinition
            End Get
        End Property

        Public Sub Dispatch(visitor As MetadataVisitor) Implements IMethodImplementation.Dispatch
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public ReadOnly Property ImplementingMethod As IMethodReference Implements IMethodImplementation.ImplementingMethod
            Get
                Return _implementing
            End Get
        End Property

        Public ReadOnly Property ImplementedMethod As IMethodReference Implements IMethodImplementation.ImplementedMethod
            Get
                Return _implemented
            End Get
        End Property
#End Region

    End Class

End Namespace

