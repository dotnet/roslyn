' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class VisualBasicSymbolVisitor

        Public Overridable Sub Visit(symbol As Symbol)
            If symbol IsNot Nothing Then
                symbol.Accept(Me)
            End If
        End Sub

        Public Overridable Sub DefaultVisit(symbol As Symbol)
        End Sub

        Public Overridable Sub VisitAlias(symbol As AliasSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitArrayType(symbol As ArrayTypeSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitAssembly(symbol As AssemblySymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitEvent(symbol As EventSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitField(symbol As FieldSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitLabel(symbol As LabelSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitLocal(symbol As LocalSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitMethod(symbol As MethodSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitModule(symbol As ModuleSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitNamedType(symbol As NamedTypeSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitNamespace(symbol As NamespaceSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitParameter(symbol As ParameterSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitProperty(symbol As PropertySymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitRangeVariable(symbol As RangeVariableSymbol)
            DefaultVisit(symbol)
        End Sub

        Public Overridable Sub VisitTypeParameter(symbol As TypeParameterSymbol)
            DefaultVisit(symbol)
        End Sub
    End Class
End Namespace
