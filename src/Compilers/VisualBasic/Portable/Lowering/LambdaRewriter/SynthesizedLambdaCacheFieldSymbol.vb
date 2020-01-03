' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class SynthesizedLambdaCacheFieldSymbol
        Inherits SynthesizedFieldSymbol
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private ReadOnly _topLevelMethod As MethodSymbol

        Public Sub New(containingType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      topLevelMethod As MethodSymbol,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            MyBase.New(containingType, implicitlyDefinedBy, type, name, accessibility, isReadOnly, isShared, isSpecialNameAndRuntimeSpecial)

            Debug.Assert(topLevelMethod IsNot Nothing)
            _topLevelMethod = topLevelMethod
        End Sub

        ' When the containing top-level method body is updated we don't need to attempt to update the cache field
        ' since a field update is a no-op.
        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Return _topLevelMethod
            End Get
        End Property
    End Class
End Namespace
