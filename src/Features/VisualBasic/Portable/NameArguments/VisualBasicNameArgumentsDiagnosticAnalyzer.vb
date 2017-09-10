' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.NameArguments
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.NameArguments
    ''' <summary>
    '''
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicNameArgumentsDiagnosticAnalyzer
        Inherits AbstractNameArgumentsDiagnosticAnalyzer

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(c As SyntaxNodeAnalysisContext) AnalyzeSyntax(c),
                SyntaxKind.InvocationExpression)
            '=> context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression,
            '    SyntaxKind.ObjectCreationExpression, SyntaxKind.BaseConstructorInitializer,
            '    SyntaxKind.ThisConstructorInitializer, SyntaxKind.ElementAccessExpression,
            '    SyntaxKind.Attribute);
        End Sub

        Friend Overrides Sub ReportDiagnosticIfNeeded(context As SyntaxNodeAnalysisContext, optionSet As OptionSet, parameters As ImmutableArray(Of IParameterSymbol))
            Throw New NotImplementedException()
        End Sub

        Friend Overrides Function LanguageSupportsNonTrailingNamedArguments(options As ParseOptions) As Boolean
            Return DirectCast(options, VisualBasicParseOptions).LanguageVersion >= LanguageVersion.VisualBasic15_5
        End Function
    End Class
End Namespace
