' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
    Friend Module NamedTypeSymbolExtensions
        ''' <summary>
        ''' Determines if the default constructor emitted by the compiler would contain an InitializeComponent() call.
        ''' </summary>
        <Extension>
        Public Function IsDesignerGeneratedTypeWithInitializeComponent(type As INamedTypeSymbol, compilation As Compilation) As Boolean
            Dim designerGeneratedAttribute = compilation.DesignerGeneratedAttributeType()

            If designerGeneratedAttribute Is Nothing Then
                Return False
            End If

            If Not type.GetAttributes().Where(Function(a) Equals(a.AttributeClass, designerGeneratedAttribute)).Any() Then
                Return False
            End If

            ' We now need to see if we have an InitializeComponent that matches the pattern. This is 
            ' the same check as in Semantics::IsInitializeComponent in the old compiler.
            For Each baseType In type.GetBaseTypesAndThis()
                Dim possibleInitializeComponent = baseType.GetMembers("InitializeComponent").OfType(Of IMethodSymbol).FirstOrDefault()

                If possibleInitializeComponent IsNot Nothing AndAlso
                   possibleInitializeComponent.IsAccessibleWithin(type) AndAlso
                   Not possibleInitializeComponent.Parameters.Any() AndAlso
                   possibleInitializeComponent.ReturnsVoid AndAlso
                   Not possibleInitializeComponent.IsStatic Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Module
End Namespace
