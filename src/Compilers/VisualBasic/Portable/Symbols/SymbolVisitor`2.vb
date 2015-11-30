﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Virtual dispatch based on a symbol's particular class. 
    ''' </summary>
    ''' <typeparam name="TResult">Result type</typeparam>
    ''' <typeparam name="TArgument">Additional argument type</typeparam>
    Friend MustInherit Class VisualBasicSymbolVisitor(Of TArgument, TResult)

        ''' <summary>
        ''' Call the correct VisitXXX method in this class based on the particular type of symbol that is passed in.
        ''' </summary>
        Public Overridable Function Visit(symbol As Symbol, Optional arg As TArgument = Nothing) As TResult
            If symbol Is Nothing Then
                Return Nothing
            End If

            Return symbol.Accept(Me, arg)
        End Function

        Public Overridable Function DefaultVisit(symbol As Symbol, arg As TArgument) As TResult
            Return Nothing
        End Function

        Public Overridable Function VisitAlias(symbol As AliasSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitAssembly(symbol As AssemblySymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitModule(symbol As ModuleSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitNamespace(symbol As NamespaceSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitNamedType(symbol As NamedTypeSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitTypeParameter(symbol As TypeParameterSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitArrayType(symbol As ArrayTypeSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitErrorType(symbol As ErrorTypeSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitMethod(symbol As MethodSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitProperty(symbol As PropertySymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitField(symbol As FieldSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitParameter(symbol As ParameterSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitLocal(symbol As LocalSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitRangeVariable(symbol As RangeVariableSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitLabel(symbol As LabelSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

        Public Overridable Function VisitEvent(symbol As EventSymbol, arg As TArgument) As TResult
            Return DefaultVisit(symbol, arg)
        End Function

    End Class
End Namespace
