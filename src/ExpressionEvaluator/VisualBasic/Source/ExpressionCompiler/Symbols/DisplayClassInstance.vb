' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend MustInherit Class DisplayClassInstance
        Friend MustOverride ReadOnly Property ContainingSymbol As Symbol
        Friend MustOverride ReadOnly Property Type As NamedTypeSymbol
        Friend MustOverride Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassInstance
        Friend MustOverride Function ToBoundExpression(syntax As VisualBasicSyntaxNode) As BoundExpression
    End Class

    Friend NotInheritable Class DisplayClassInstanceFromLocal
        Inherits DisplayClassInstance

        Friend ReadOnly Local As EELocalSymbol

        Friend Sub New(local As EELocalSymbol)
            Debug.Assert(Not local.IsByRef)
            Debug.Assert(local.DeclarationKind = LocalDeclarationKind.Variable)

            Me.Local = local
        End Sub

        Friend Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me.Local.ContainingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property Type As NamedTypeSymbol
            Get
                Return DirectCast(Me.Local.Type, NamedTypeSymbol)
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassInstance
            Dim otherInstance = DirectCast(Me.Local.ToOtherMethod(method, typeMap), EELocalSymbol)
            Return New DisplayClassInstanceFromLocal(otherInstance)
        End Function

        Friend Overrides Function ToBoundExpression(syntax As VisualBasicSyntaxNode) As BoundExpression
            Return New BoundLocal(syntax, Me.Local, Me.Local.Type).MakeCompilerGenerated()
        End Function
    End Class

    Friend NotInheritable Class DisplayClassInstanceFromMe
        Inherits DisplayClassInstance

        Friend ReadOnly MeParameter As ParameterSymbol

        Friend Sub New(meParameter As ParameterSymbol)
            Debug.Assert(meParameter IsNot Nothing)
            Me.MeParameter = meParameter
        End Sub

        Friend Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me.MeParameter.ContainingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property Type As NamedTypeSymbol
            Get
                Return DirectCast(Me.MeParameter.Type, NamedTypeSymbol)
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassInstance
            Debug.Assert(method.IsShared)
            Dim otherParameter = method.Parameters(0)
            Return New DisplayClassInstanceFromMe(otherParameter)
        End Function

        Friend Overrides Function ToBoundExpression(syntax As VisualBasicSyntaxNode) As BoundExpression
            Return New BoundParameter(syntax, Me.MeParameter, Me.MeParameter.Type).MakeCompilerGenerated()
        End Function
    End Class
End Namespace
