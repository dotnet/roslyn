' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend MustInherit Class DisplayClassInstance
        Friend MustOverride ReadOnly Property ContainingSymbol As Symbol
        Friend MustOverride ReadOnly Property Type As NamedTypeSymbol
        Friend MustOverride Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassInstance
        Friend MustOverride Function ToBoundExpression(syntax As SyntaxNode) As BoundExpression
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

        Friend Overrides Function ToBoundExpression(syntax As SyntaxNode) As BoundExpression
            Return New BoundLocal(syntax, Me.Local, Me.Local.Type).MakeCompilerGenerated()
        End Function
    End Class

    Friend NotInheritable Class DisplayClassInstanceFromParameter
        Inherits DisplayClassInstance

        Friend ReadOnly Parameter As ParameterSymbol

        Friend Sub New(parameter As ParameterSymbol)
            Debug.Assert(parameter IsNot Nothing)
            Debug.Assert(parameter.Name.Equals("Me", StringComparison.Ordinal) OrElse
                parameter.Name.IndexOf("$Me", StringComparison.Ordinal) >= 0 OrElse
                parameter.Name.IndexOf("$It", StringComparison.Ordinal) >= 0)
            Me.Parameter = parameter
        End Sub

        Friend Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me.Parameter.ContainingSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property Type As NamedTypeSymbol
            Get
                Return DirectCast(Me.Parameter.Type, NamedTypeSymbol)
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As DisplayClassInstance
            Debug.Assert(method.IsShared)
            Dim otherOrdinal = If(Me.ContainingSymbol.IsShared, Me.Parameter.Ordinal, Me.Parameter.Ordinal + 1)
            Dim otherParameter = method.Parameters(otherOrdinal)
            Return New DisplayClassInstanceFromParameter(otherParameter)
        End Function

        Friend Overrides Function ToBoundExpression(syntax As SyntaxNode) As BoundExpression
            Return New BoundParameter(syntax, Me.Parameter, Me.Parameter.Type).MakeCompilerGenerated()
        End Function
    End Class
End Namespace
