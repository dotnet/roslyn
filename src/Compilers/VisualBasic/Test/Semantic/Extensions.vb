' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.Feature
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax.FeatureExtensions

' This is deliberately declared in the global namespace so that it will always be discoverable (regardless of Imports).
Friend Module Extensions

    ''' <summary>
    ''' This method is provided as a convenience for testing the SemanticModel.GetDeclaredSymbol implementation.
    ''' </summary>
    ''' <param name="node">This parameter will be type checked, and a NotSupportedException will be thrown if the type is not currently supported by an overload of GetDeclaredSymbol.</param>
    <Extension()>
    Friend Function GetDeclaredSymbolFromSyntaxNode(model As SemanticModel, node As SyntaxNode, Optional cancellationToken As CancellationToken = Nothing) As Symbol
        If Not (
            TypeOf node Is AggregationRangeVariableSyntax OrElse
            TypeOf node Is AnonymousObjectCreationExpressionSyntax OrElse
            TypeOf node Is SimpleImportsClauseSyntax OrElse
            TypeOf node Is CatchStatementSyntax OrElse
            TypeOf node Is CollectionRangeVariableSyntax OrElse
            TypeOf node Is EnumBlockSyntax OrElse
            TypeOf node Is EnumMemberDeclarationSyntax OrElse
            TypeOf node Is EnumStatementSyntax OrElse
            TypeOf node Is EventBlockSyntax OrElse
            TypeOf node Is ExpressionRangeVariableSyntax OrElse
            TypeOf node Is ForEachStatementSyntax OrElse
            TypeOf node Is FieldInitializerSyntax OrElse
            TypeOf node Is ForStatementSyntax OrElse
            TypeOf node Is LabelStatementSyntax OrElse
            TypeOf node Is MethodBaseSyntax OrElse
            TypeOf node Is MethodBlockSyntax OrElse
            TypeOf node Is ModifiedIdentifierSyntax OrElse
            TypeOf node Is NamespaceBlockSyntax OrElse
            TypeOf node Is NamespaceStatementSyntax OrElse
            TypeOf node Is ParameterSyntax OrElse
            TypeOf node Is PropertyBlockSyntax OrElse
            TypeOf node Is PropertyStatementSyntax OrElse
            TypeOf node Is TypeBlockSyntax OrElse
            TypeOf node Is TypeParameterSyntax OrElse
            TypeOf node Is TypeStatementSyntax) _
        Then
            Throw New NotSupportedException("This node type is not supported.")
        End If

        Return DirectCast(model.GetDeclaredSymbol(node, cancellationToken), Symbol)
    End Function

End Module

Namespace Global.Roslyn.Test.Utilities.VisualBasic

    Namespace Requires
        Namespace Language
            Public Class Feature
                Inherits Global.Xunit.FactAttribute

                Private ReadOnly Property theFeature As InternalSyntax.Feature

                Public Sub New(theFeature As InternalSyntax.Feature)
                    Me.theFeature = theFeature
                    If theFeature.IsUnavailable Then Skip = "Language Feature " & theFeature.ToString & " is unavailable."
                End Sub

            End Class

            Public Class Version
                Inherits Global.Xunit.FactAttribute

                Public Enum Comparision
                    EQ
                    LE
                    LT
                    GT
                    GE
                    NE
                End Enum

                Private ReadOnly Property theVersion As LanguageVersion

                Public Sub New(theVersion As LanguageVersion, Optional condition As Comparision = Comparision.LT)
                    Me.theVersion = theVersion
                    Dim defaultVersion = Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions.Default.LanguageVersion
                    Select Case condition
                        Case Comparision.EQ : If Not (defaultVersion = theVersion) Then Skip = "Requires Specific Language Version " & theVersion.ToString
                        Case Comparision.GE : If Not (defaultVersion >= theVersion) Then Skip = "Requires Language Version greater than or equal to" & theVersion.ToString
                        Case Comparision.GT : If Not (defaultVersion > theVersion) Then Skip = "Requires Language Version greater than " & theVersion.ToString
                        Case Comparision.LE : If Not (defaultVersion <= theVersion) Then Skip = "Requires Language Version less than or equal to" & theVersion.ToString
                        Case Comparision.LT : If Not (defaultVersion < theVersion) Then Skip = "Requires Language Version less than " & theVersion.ToString
                        Case Comparision.NE : If Not (defaultVersion <> theVersion) Then Skip = "Requires Langauge to be not be " & theVersion.ToString
                        Case Else
                            Skip = "Unsupported Condition := " & condition.ToString
                    End Select
                End Sub

            End Class
        End Namespace
    End Namespace
End Namespace
