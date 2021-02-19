' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Binder used to bind For and ForEach blocks. 
    ''' It hosts the control variable (if one is declared) 
    ''' and inherits ExitableStatementBinder to provide Continue/Exit labels if needed. 
    ''' </summary>
    Friend NotInheritable Class ForOrForEachBlockBinder
        Inherits ExitableStatementBinder

        Private ReadOnly _syntax As ForOrForEachBlockSyntax
        Private _locals As ImmutableArray(Of LocalSymbol) = Nothing

        Public Sub New(enclosing As Binder, syntax As ForOrForEachBlockSyntax)
            MyBase.New(enclosing, SyntaxKind.ContinueForStatement, SyntaxKind.ExitForStatement)

            Debug.Assert(syntax IsNot Nothing)
            _syntax = syntax
        End Sub

        Friend Overrides ReadOnly Property Locals As ImmutableArray(Of LocalSymbol)
            Get
                If _locals.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_locals, BuildLocals(), Nothing)
                End If

                Return _locals
            End Get
        End Property

        ' Build a read only array of all the local variables declared By the For statement.
        ' There can only be 0 or 1 variable.
        Private Function BuildLocals() As ImmutableArray(Of LocalSymbol)
            Dim localVar As LocalSymbol = Nothing
            Dim controlVariableSyntax As VisualBasicSyntaxNode

            If _syntax.Kind = SyntaxKind.ForBlock Then
                controlVariableSyntax = DirectCast(_syntax.ForOrForEachStatement, ForStatementSyntax).ControlVariable
            Else
                controlVariableSyntax = DirectCast(_syntax.ForOrForEachStatement, ForEachStatementSyntax).ControlVariable
            End If

            Dim declarator = TryCast(controlVariableSyntax, VariableDeclaratorSyntax)
            If declarator IsNot Nothing Then

                ' Note:
                ' if the AsClause is nothing _AND_ a nullable modifier is used _AND_ Option Infer is On 
                ' the control variable will not get an inferred type.
                ' The only difference for Option Infer On and Off is the fact whether the type (Object) is considered
                ' implicit or explicit.

                Debug.Assert(declarator.Names.Count = 1)

                Dim modifiedIdentifier As ModifiedIdentifierSyntax = declarator.Names(0)
                localVar = LocalSymbol.Create(Me.ContainingMember, Me,
                                               modifiedIdentifier.Identifier, modifiedIdentifier, declarator.AsClause,
                                               declarator.Initializer,
                                               If(_syntax.Kind = SyntaxKind.ForEachBlock,
                                                  LocalDeclarationKind.ForEach,
                                                  LocalDeclarationKind.For))

            Else
                Dim identifierName = TryCast(controlVariableSyntax, IdentifierNameSyntax)

                If identifierName IsNot Nothing Then

                    Dim identifier = identifierName.Identifier

                    If OptionInfer Then

                        Dim result = LookupResult.GetInstance()

                        ContainingBinder.Lookup(result, identifier.ValueText, 0, LookupOptions.AllMethodsOfAnyArity, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

                        ' If there was something found we do not create a new local of an inferred type.
                        ' The only exception is if all symbols (should be one only, or the result is not good) are type symbols.
                        ' It's perfectly legal to create a local named with the same name as the enclosing type.
                        If Not (result.IsGoodOrAmbiguous AndAlso
                            result.Symbols(0).Kind <> SymbolKind.NamedType AndAlso
                            result.Symbols(0).Kind <> SymbolKind.TypeParameter) Then

                            localVar = CreateLocalSymbol(identifier)
                        End If

                        result.Free()

                    End If
                End If
            End If

            If localVar IsNot Nothing Then
                Return ImmutableArray.Create(localVar)
            End If

            Return ImmutableArray(Of LocalSymbol).Empty
        End Function

        Private Function CreateLocalSymbol(identifier As SyntaxToken) As LocalSymbol
            If _syntax.Kind = SyntaxKind.ForBlock Then
                Dim forStatementSyntax = DirectCast(_syntax.ForOrForEachStatement, ForStatementSyntax)

                Dim localVar = LocalSymbol.CreateInferredForFromTo(Me.ContainingMember,
                                                     Me,
                                                     identifier,
                                                     forStatementSyntax.FromValue,
                                                     forStatementSyntax.ToValue,
                                                     forStatementSyntax.StepClause)

                Return localVar
            Else
                Dim forEachStatementSyntax = DirectCast(_syntax.ForOrForEachStatement, ForEachStatementSyntax)

                Dim localVar = LocalSymbol.CreateInferredForEach(Me.ContainingMember,
                                                         Me,
                                                         identifier,
                                                         forEachStatementSyntax.Expression)

                Return localVar
            End If
        End Function

    End Class
End Namespace

