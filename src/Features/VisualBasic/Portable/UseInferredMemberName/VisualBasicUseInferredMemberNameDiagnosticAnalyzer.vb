' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UseInferredMemberName
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName
    ''' <summary>
    ''' Offers to simplify tuple expressions and anonymous types with redundant names, such as <c>(a:=a, b:=b)</c> or <c>New With {.a = a, .b = b}</c>
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseInferredMemberNameDiagnosticAnalyzer
        Inherits AbstractUseInferredMemberNameDiagnosticAnalyzer

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(c As SyntaxNodeAnalysisContext) AnalyzeSyntax(c),
                SyntaxKind.NameColonEquals, SyntaxKind.NamedFieldInitializer)
        End Sub

        Protected Overrides Sub LanguageSpecificAnalyzeSyntax(context As SyntaxNodeAnalysisContext, syntaxTree As SyntaxTree, optionSet As OptionSet)
            Dim parseOptions = DirectCast(syntaxTree.Options, VisualBasicParseOptions)
            Select Case context.Node.Kind()
                Case SyntaxKind.NameColonEquals
                    ReportDiagnosticsIfNeeded(DirectCast(context.Node, NameColonEqualsSyntax), context, optionSet, syntaxTree, parseOptions)
                    Exit Select
                Case SyntaxKind.NamedFieldInitializer
                    ReportDiagnosticsIfNeeded(DirectCast(context.Node, NamedFieldInitializerSyntax), context, optionSet, syntaxTree)
                    Exit Select
            End Select
        End Sub

        Private Sub ReportDiagnosticsIfNeeded(nameColonEquals As NameColonEqualsSyntax, context As SyntaxNodeAnalysisContext,
                                              optionSet As OptionSet, syntaxTree As SyntaxTree, parseOptions As VisualBasicParseOptions)

            If Not nameColonEquals.IsParentKind(SyntaxKind.SimpleArgument) Then
                Return
            End If

            Dim argument = DirectCast(nameColonEquals.Parent, SimpleArgumentSyntax)
            If Not optionSet.GetOption(CodeStyleOptions.PreferInferredTupleNames, context.Compilation.Language).Value OrElse
                Not VisualBasicInferredMemberNameReducer.CanSimplifyTupleName(argument, parseOptions) Then
                Return
            End If

            ' Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    nameColonEquals.GetLocation(),
                    optionSet.GetOption(CodeStyleOptions.PreferInferredTupleNames, context.Compilation.Language).Notification.Severity,
                    additionalLocations:=Nothing,
                    properties:=Nothing))

            ' Also fade out the part of the name-colon-equals syntax
            Dim fadeSpan = TextSpan.FromBounds(nameColonEquals.Name.SpanStart, nameColonEquals.ColonEqualsToken.Span.End)
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)))
        End Sub

        Private Sub ReportDiagnosticsIfNeeded(fieldInitializer As NamedFieldInitializerSyntax, context As SyntaxNodeAnalysisContext,
                                              optionSet As OptionSet, syntaxTree As SyntaxTree)
            If Not fieldInitializer.Parent.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression) Then
                Return
            End If

            If Not optionSet.GetOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, context.Compilation.Language).Value OrElse
                Not VisualBasicInferredMemberNameReducer.CanSimplifyNamedFieldInitializer(fieldInitializer) Then

                Return
            End If

            Dim fadeSpan = TextSpan.FromBounds(fieldInitializer.Name.SpanStart, fieldInitializer.EqualsToken.Span.End)

            ' Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    syntaxTree.GetLocation(fadeSpan),
                    optionSet.GetOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, context.Compilation.Language).Notification.Severity,
                    additionalLocations:=Nothing,
                    properties:=Nothing))

            ' Also fade out the part of the name-equals syntax
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)))
        End Sub
    End Class
End Namespace
