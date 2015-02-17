﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class LocalDeclarationRewriter

        Friend Shared Function Rewrite(compilation As VisualBasicCompilation, container As EENamedTypeSymbol, block As BoundBlock) As BoundBlock
            Dim locals = PooledHashSet(Of LocalSymbol).GetInstance()
            Dim walker As New LocalDeclarationWalker(locals)
            walker.Visit(block)

            If locals.Count > 0 Then
                Dim syntax = block.Syntax
                Dim builder = ArrayBuilder(Of BoundStatement).GetInstance()
                For Each local In locals
                    builder.Add(GenerateCreateVariableStatement(compilation, container, syntax, local))
                Next
                builder.AddRange(block.Statements)
                block = New BoundBlock(syntax, block.StatementListSyntax, block.Locals, builder.ToImmutableAndFree())
            End If

            locals.Free()
            Return block
        End Function

        ' Find all implicitly declared locals.
        Private NotInheritable Class LocalDeclarationWalker
            Inherits BoundTreeWalker

            Private ReadOnly _locals As HashSet(Of LocalSymbol)

            Friend Sub New(locals As HashSet(Of LocalSymbol))
                _locals = locals
            End Sub

            Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
                Dim local = node.LocalSymbol
                If local.DeclarationKind = LocalDeclarationKind.ImplicitVariable Then
                    _locals.Add(local)
                End If
                Return node
            End Function

        End Class

        Private Shared Function GenerateCreateVariableStatement(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            local As LocalSymbol) As BoundStatement

            Dim voidType = compilation.GetSpecialType(SpecialType.System_Void)
            Dim typeType = compilation.GetWellKnownType(WellKnownType.System_Type)
            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)

            ' <>CreateVariable(type As Type, name As String)
            Dim method = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.CreateVariableMethodName,
                Function(c, n, s) New PlaceholderMethodSymbol(
                    c,
                    s,
                    n,
                    voidType,
                    Function(m) ImmutableArray.Create(Of ParameterSymbol)(
                        New SynthesizedParameterSymbol(m, typeType, ordinal:=0, isByRef:=False),
                        New SynthesizedParameterSymbol(m, stringType, ordinal:=1, isByRef:=False))))
            Dim type = New BoundGetType(syntax, New BoundTypeExpression(syntax, local.Type), typeType)
            Dim name = New BoundLiteral(syntax, ConstantValue.Create(local.Name), stringType)
            Dim expr = New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray.Create(Of BoundExpression)(type, name),
                constantValueOpt:=Nothing,
                suppressObjectClone:=False,
                type:=method.ReturnType)
            Return New BoundExpressionStatement(syntax, expr).MakeCompilerGenerated()
        End Function

    End Class

End Namespace
