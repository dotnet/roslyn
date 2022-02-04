' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
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

        Protected Overrides Sub LanguageSpecificAnalyzeSyntax(context As SyntaxNodeAnalysisContext,
                syntaxTree As SyntaxTree,
                options As AnalyzerOptions,
                cancellationToken As CancellationToken)
            Dim parseOptions = DirectCast(syntaxTree.Options, VisualBasicParseOptions)
            Select Case context.Node.Kind()
                Case SyntaxKind.NameColonEquals
                    ReportDiagnosticsIfNeeded(DirectCast(context.Node, NameColonEqualsSyntax), context, options, syntaxTree, parseOptions, cancellationToken)
                    Exit Select
                Case SyntaxKind.NamedFieldInitializer
                    ReportDiagnosticsIfNeeded(DirectCast(context.Node, NamedFieldInitializerSyntax), context, options, syntaxTree, cancellationToken)
                    Exit Select
            End Select
        End Sub

        Private Sub ReportDiagnosticsIfNeeded(nameColonEquals As NameColonEqualsSyntax,
                context As SyntaxNodeAnalysisContext,
                options As AnalyzerOptions,
                syntaxTree As SyntaxTree,
                parseOptions As VisualBasicParseOptions,
                cancellationToken As CancellationToken)

            If Not nameColonEquals.IsParentKind(SyntaxKind.SimpleArgument) Then
                Return
            End If

            Dim argument = DirectCast(nameColonEquals.Parent, SimpleArgumentSyntax)
            Dim preference = options.GetOption(
                CodeStyleOptions2.PreferInferredTupleNames, context.Compilation.Language, syntaxTree, cancellationToken)
            If Not preference.Value OrElse
                Not CanSimplifyTupleName(argument, parseOptions) Then
                Return
            End If

            ' Create a normal diagnostic
            Dim fadeSpan = TextSpan.FromBounds(nameColonEquals.Name.SpanStart, nameColonEquals.ColonEqualsToken.Span.End)
            context.ReportDiagnostic(
                DiagnosticHelper.CreateWithLocationTags(
                    Descriptor,
                    nameColonEquals.GetLocation(),
                    preference.Notification.Severity,
                    additionalLocations:=ImmutableArray(Of Location).Empty,
                    additionalUnnecessaryLocations:=ImmutableArray.Create(syntaxTree.GetLocation(fadeSpan))))
        End Sub

        Private Sub ReportDiagnosticsIfNeeded(
                fieldInitializer As NamedFieldInitializerSyntax,
                context As SyntaxNodeAnalysisContext,
                options As AnalyzerOptions,
                syntaxTree As SyntaxTree,
                cancellationToken As CancellationToken)
            If Not fieldInitializer.Parent.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression) Then
                Return
            End If

            Dim preference = options.GetOption(
                CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, context.Compilation.Language, syntaxTree, cancellationToken)
            If Not preference.Value OrElse
                Not CanSimplifyNamedFieldInitializer(fieldInitializer) Then

                Return
            End If

            Dim fadeSpan = TextSpan.FromBounds(fieldInitializer.Name.SpanStart, fieldInitializer.EqualsToken.Span.End)

            ' Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.CreateWithLocationTags(
                    Descriptor,
                    syntaxTree.GetLocation(fadeSpan),
                    preference.Notification.Severity,
                    additionalLocations:=ImmutableArray(Of Location).Empty,
                    additionalUnnecessaryLocations:=ImmutableArray.Create(syntaxTree.GetLocation(fadeSpan))))
        End Sub
    End Class
End Namespace
