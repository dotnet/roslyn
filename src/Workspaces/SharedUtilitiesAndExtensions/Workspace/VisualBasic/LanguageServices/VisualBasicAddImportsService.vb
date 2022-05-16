' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

#If CODE_STYLE Then
Imports OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
#Else
Imports OptionSet = Microsoft.CodeAnalysis.Options.OptionSet
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImports
    <ExportLanguageService(GetType(IAddImportsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddImportsService
        Inherits AbstractAddImportsService(Of
            CompilationUnitSyntax,
            NamespaceBlockSyntax,
            ImportsStatementSyntax,
            ImportsStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Private Shared ReadOnly ImportsStatementComparer As ImportsStatementComparer = New ImportsStatementComparer(New CaseInsensitiveTokenComparer())

        Protected Overrides Function IsEquivalentImport(a As SyntaxNode, b As SyntaxNode) As Boolean
            Dim importsA = TryCast(a, ImportsStatementSyntax)
            Dim importsB = TryCast(b, ImportsStatementSyntax)
            If importsA Is Nothing OrElse importsB Is Nothing Then
                Return False
            End If

            Return ImportsStatementComparer.Compare(importsA, importsB) = 0

        End Function

        Protected Overrides Function GetGlobalImports(compilation As Compilation, generator As SyntaxGenerator) As ImmutableArray(Of SyntaxNode)
            Dim result = ArrayBuilder(Of SyntaxNode).GetInstance()

            For Each import In compilation.MemberImports()
                If TypeOf import Is INamespaceSymbol Then
                    result.Add(generator.NamespaceImportDeclaration(import.ToDisplayString()))
                End If
            Next

            Return result.ToImmutableAndFree()
        End Function

        Protected Overrides Function GetAlias(usingOrAlias As ImportsStatementSyntax) As SyntaxNode
            Return usingOrAlias.ImportsClauses.OfType(Of SimpleImportsClauseSyntax).
                                               Where(Function(c) c.Alias IsNot Nothing).
                                               FirstOrDefault()?.Alias
        End Function

        Public Overrides Function PlaceImportsInsideNamespaces(configOptions As AnalyzerConfigOptions, fallbackValue As Boolean) As Boolean
            ' Visual Basic doesn't support imports inside namespaces
            Return False
        End Function
        Protected Overrides Function IsStaticUsing(usingOrAlias As ImportsStatementSyntax) As Boolean
            ' Visual Basic doesn't support static imports
            Return False
        End Function

        Protected Overrides Function GetExterns(node As SyntaxNode) As SyntaxList(Of ImportsStatementSyntax)
            Return Nothing
        End Function

        Protected Overrides Function GetUsingsAndAliases(node As SyntaxNode) As SyntaxList(Of ImportsStatementSyntax)
            If node.Kind() = SyntaxKind.CompilationUnit Then
                Return DirectCast(node, CompilationUnitSyntax).Imports
            End If

            Return Nothing
        End Function

        Protected Overrides Function Rewrite(
                externAliases() As ImportsStatementSyntax,
                usingDirectives() As ImportsStatementSyntax,
                staticUsingDirectives() As ImportsStatementSyntax,
                aliasDirectives() As ImportsStatementSyntax,
                externContainer As SyntaxNode,
                usingContainer As SyntaxNode,
                staticUsingContainer As SyntaxNode,
                aliasContainer As SyntaxNode,
                options As AddImportPlacementOptions,
                root As SyntaxNode,
                cancellationToken As CancellationToken) As SyntaxNode

            Dim compilationUnit = DirectCast(root, CompilationUnitSyntax)

            If Not compilationUnit.CanAddImportsStatements(options.AllowInHiddenRegions, cancellationToken) Then
                Return compilationUnit
            End If

            Return compilationUnit.AddImportsStatements(
                usingDirectives.Concat(aliasDirectives).ToList(),
                options.PlaceSystemNamespaceFirst,
                Array.Empty(Of SyntaxAnnotation))
        End Function

        Private Class CaseInsensitiveTokenComparer
            Implements IComparer(Of SyntaxToken)
            Public Function Compare(x As SyntaxToken, y As SyntaxToken) As Integer Implements IComparer(Of SyntaxToken).Compare
                ' By using 'ValueText' we get the value that is normalized.  i.e.
                ' [class] will be 'class', and unicode escapes will be converted
                ' to actual unicode.  This allows sorting to work properly across
                ' tokens that have different source representations, but which
                ' mean the same thing.

                ' Don't bother checking the raw kind, since this will only ever be used with Identifier tokens.

                Return CaseInsensitiveComparer.Default.Compare(x.GetIdentifierText(), y.GetIdentifierText())
            End Function
        End Class
    End Class
End Namespace
