' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName
    ''' <summary>
    ''' Offers to simplify tuple expressions and anonymous types with redundant names, such as `(a:=a, b:=b)` or `New With {.a = a, .b = b}`
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseInferredMemberNameDiagnosticAnalyzer
        Inherits AbstractCodeStyleDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId,
                  New LocalizableResourceString(NameOf(FeaturesResources.Use_inferred_member_name), FeaturesResources.ResourceManager, GetType(FeaturesResources)),
                  New LocalizableResourceString(NameOf(FeaturesResources.Member_name_can_be_simplified), FeaturesResources.ResourceManager, GetType(FeaturesResources)))
        End Sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function

        Public Overrides Function OpenFileOnly(workspace As Workspace) As Boolean
            Return False
        End Function

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(c As SyntaxNodeAnalysisContext) AnalyzeSyntax(c),
                SyntaxKind.NameColonEquals, SyntaxKind.NamedFieldInitializer)
        End Sub

        Private Sub AnalyzeSyntax(context As SyntaxNodeAnalysisContext)
            Dim cancellationToken = context.CancellationToken

            Dim syntaxTree = context.Node.SyntaxTree
            Dim optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult()
            If optionSet Is Nothing Then
                Return
            End If

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
            If Not optionSet.GetOption(VisualBasicCodeStyleOptions.PreferInferredTupleNames).Value OrElse
                Not VisualBasicInferredMemberNameReducer.CanSimplifyTupleName(argument, parseOptions) Then
                Return
            End If

            ' Create a normal diagnostic
            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(VisualBasicCodeStyleOptions.PreferInferredTupleNames).Notification.Value),
                    nameColonEquals.GetLocation()))

            ' Also fade out the part of the name-colon-equals syntax
            Dim fadeSpan = TextSpan.FromBounds(nameColonEquals.Name.SpanStart, nameColonEquals.ColonEqualsToken.Span.End)
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)))
        End Sub

        Private Sub ReportDiagnosticsIfNeeded(fieldInitializer As NamedFieldInitializerSyntax, context As SyntaxNodeAnalysisContext,
                                              optionSet As OptionSet, syntaxTree As SyntaxTree)

            If Not optionSet.GetOption(VisualBasicCodeStyleOptions.PreferInferredAnonymousTypeMemberNames).Value OrElse
                Not VisualBasicInferredMemberNameReducer.CanSimplifyNamedFieldInitializer(fieldInitializer) Then

                Return
            End If

            Dim fadeSpan = TextSpan.FromBounds(fieldInitializer.Name.SpanStart, fieldInitializer.EqualsToken.Span.End)

            ' Create a normal diagnostic
            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(VisualBasicCodeStyleOptions.PreferInferredAnonymousTypeMemberNames).Notification.Value),
                    syntaxTree.GetLocation(fadeSpan)))

            ' Also fade out the part of the name-equals syntax
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)))
        End Sub
    End Class
End Namespace
