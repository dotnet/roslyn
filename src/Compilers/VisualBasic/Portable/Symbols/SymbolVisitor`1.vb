' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class VisualBasicSymbolVisitor(Of TResult)

        Public Overridable Function Visit(symbol As Symbol) As TResult
            Return If(symbol Is Nothing, Nothing, symbol.Accept(Me))
        End Function

        Public Overridable Function DefaultVisit(symbol As Symbol) As TResult
            Return Nothing
        End Function

        Public Overridable Function VisitAlias(symbol As AliasSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitArrayType(symbol As ArrayTypeSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitAssembly(symbol As AssemblySymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitEvent(symbol As EventSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitField(symbol As FieldSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitLabel(symbol As LabelSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitLocal(symbol As LocalSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitMethod(symbol As MethodSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitModule(symbol As ModuleSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitNamedType(symbol As NamedTypeSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitNamespace(symbol As NamespaceSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitParameter(symbol As ParameterSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitProperty(symbol As PropertySymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitRangeVariable(symbol As RangeVariableSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function

        Public Overridable Function VisitTypeParameter(symbol As TypeParameterSymbol) As TResult
            Return DefaultVisit(symbol)
        End Function
    End Class
End Namespace
