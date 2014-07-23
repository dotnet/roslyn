' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Represents a synthesized state machine helper field.
    ''' </summary>
    Friend Class StateMachineFieldSymbol
        Inherits SynthesizedFieldSymbol
        Implements ISynthesizedMethodBodyImplementationSymbol

        Public Sub New(containingType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            MyBase.New(containingType, implicitlyDefinedBy, type, name, accessibility, isReadOnly, isShared, isSpecialNameAndRuntimeSpecial)
        End Sub

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbol Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                Return symbol.Method
            End Get
        End Property
    End Class

End Namespace