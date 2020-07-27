' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer

        Protected Overrides Sub RegisterAttributeSyntaxAction(context As CompilationStartAnalysisContext, compilationAnalyzer As CompilationAnalyzer)
            context.RegisterSyntaxNodeAction(
                Sub(syntaxContext As SyntaxNodeAnalysisContext)
                    Dim attribute = DirectCast(syntaxContext.Node, AttributeSyntax)
                    Select Case attribute.Target?.AttributeModifier.Kind()
                        Case SyntaxKind.AssemblyKeyword, SyntaxKind.ModuleKeyword
                            compilationAnalyzer.AnalyzeAssemblyOrModuleAttribute(attribute, syntaxContext.SemanticModel, AddressOf syntaxContext.ReportDiagnostic, syntaxContext.CancellationToken)
                    End Select
                End Sub,
                SyntaxKind.Attribute)
        End Sub
    End Class
End Namespace
