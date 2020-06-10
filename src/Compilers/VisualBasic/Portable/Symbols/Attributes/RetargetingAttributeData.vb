' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
    ''' <summary>
    ''' Represents a retargeting custom attribute
    ''' </summary>
    Friend Class RetargetingAttributeData
        Inherits SourceAttributeData
        Friend Sub New(ByVal applicationNode As SyntaxReference,
                       ByVal attributeClass As NamedTypeSymbol,
                       ByVal attributeConstructor As MethodSymbol,
                       ByVal constructorArguments As ImmutableArray(Of TypedConstant),
                       ByVal namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)),
                       ByVal isConditionallyOmitted As Boolean,
                       ByVal hasErrors As Boolean)
            MyBase.New(applicationNode, attributeClass, attributeConstructor, constructorArguments, namedArguments, isConditionallyOmitted, hasErrors)
        End Sub

        ''' <summary>
        ''' Gets the retargeted System.Type type symbol.
        ''' </summary>
        ''' <param name="targetSymbol">Target symbol on which this attribute is applied.</param>
        ''' <returns>Retargeted System.Type type symbol.</returns>
        Friend Overrides Function GetSystemType(ByVal targetSymbol As Symbol) As TypeSymbol
            Dim retargetingAssembly = DirectCast(If(targetSymbol.Kind = SymbolKind.Assembly, targetSymbol, targetSymbol.ContainingAssembly), RetargetingAssemblySymbol)
            Dim underlyingAssembly = DirectCast(retargetingAssembly.UnderlyingAssembly, SourceAssemblySymbol)

            ' Get the System.Type from the underlying assembly's Compilation
            Dim systemType As TypeSymbol = underlyingAssembly.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type)

            ' Retarget the type
            Dim retargetingModule = DirectCast(retargetingAssembly.Modules(0), RetargetingModuleSymbol)
            Return retargetingModule.RetargetingTranslator.Retarget(systemType, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
        End Function
    End Class
End Namespace
