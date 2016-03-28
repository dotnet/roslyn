Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.PopulateSwitch
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.PopulateSwitch

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicPopulateSwitchDiagnosticAnalyzer
        Inherits PopulateSwitchDiagnosticAnalyzerBase(Of SyntaxKind)

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.SelectBlock)
        Public Overrides ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind)
            Get
                Return s_kindsOfInterest
            End Get
        End Property

        Protected Overrides Function GetDiagnosticSpan(node As SyntaxNode) As TextSpan
            Return DirectCast(node, SelectBlockSyntax).Span
        End Function

        Protected Overrides Function SwitchIsFullyPopulated(model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean

            Dim selectBlock = DirectCast(node, SelectBlockSyntax)

            Dim enumType = TryCast(model.GetTypeInfo(selectBlock.SelectStatement.Expression).Type, INamedTypeSymbol)

            If enumType Is Nothing OrElse Not enumType.TypeKind = TypeKind.Enum
                Return True
            End If

            ' ignore enums marked with Flags per spec: https://github.com/dotnet/roslyn/issues/6766#issuecomment-156878851
            For Each attribute In enumType.GetAttributes()

                Dim containingClass = attribute.AttributeClass.ToDisplayString()
                If containingClass = GetType(FlagsAttribute).FullName
                    Return True
                End If
            Next

            Dim caseLabels As New List(Of ExpressionSyntax)
            Dim hasElseCase = False

            For Each block In selectBlock.CaseBlocks
                For Each caseClause In block.CaseStatement.Cases
                    If caseClause.IsKind(SyntaxKind.SimpleCaseClause)
                        caseLabels.Add(DirectCast(caseClause, SimpleCaseClauseSyntax).Value)
                    End If

                    If caseClause.IsKind(SyntaxKind.ElseCaseClause)
                        hasElseCase = True
                    End If
                Next
            Next

            If Not hasElseCase
                Return False
            End If

            Dim labelSymbols As New List(Of ISymbol)
            For Each label In caseLabels
                labelSymbols.Add(model.GetSymbolInfo(label).Symbol)
            Next

            For Each member In enumType.GetMembers()
                ' skip `.ctor` and `value__`
                ' member.IsImplicitlyDeclared doesn't work because `FileMode.value__` is apparently explicitly declared
                Dim field = TryCast(member, IFieldSymbol)
                If field Is Nothing OrElse (Not field.Type.SpecialType = SpecialType.None)
                    Continue For
                End If

                Dim switchHasSymbol = False
                For Each symbol In labelSymbols
                    If symbol Is member
                        switchHasSymbol = True
                        Exit For
                    End If
                Next

                If Not switchHasSymbol
                    Return False
                End If
            Next

            Return True
        End Function
    End Class
End Namespace
