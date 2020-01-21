' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Composition
Imports System.Globalization
Imports Microsoft.CodeAnalysis.CodeLens
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeLens
    <ExportLanguageService(GetType(ICodeLensDisplayInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicDisplayInfoService
        Implements ICodeLensDisplayInfoService

        Private Shared ReadOnly Format As SymbolDisplayFormat = New SymbolDisplayFormat(
                SymbolDisplayGlobalNamespaceStyle.Omitted,                                  ' Don't prepend VB namespaces with "Global."
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,    ' Show fully qualified names
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
                SymbolDisplayMemberOptions.IncludeContainingType Or SymbolDisplayMemberOptions.IncludeParameters,
                SymbolDisplayDelegateStyle.NameOnly,
                SymbolDisplayExtensionMethodStyle.StaticMethod,
                SymbolDisplayParameterOptions.IncludeType,
                SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                SymbolDisplayLocalOptions.IncludeType,
                SymbolDisplayKindOptions.None,
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Returns the node that should be displayed
        ''' </summary>
        Public Function GetDisplayNode(node As SyntaxNode) As SyntaxNode Implements ICodeLensDisplayInfoService.GetDisplayNode
            Select Case node.Kind()
                ' A variable declarator can contain multiple symbols, for example "Private field2, field3 As Integer"
                ' In that case default to the first field name.
                Case SyntaxKind.VariableDeclarator
                    Dim variableNode = CType(node, VariableDeclaratorSyntax)
                    Return GetDisplayNode(variableNode.Names.First())

                ' A field declaration (global variable) can contain multiple symbols, for example "Private field2, field3 As Integer"
                ' In that case default to the first field name.
                Case SyntaxKind.FieldDeclaration
                    Dim fieldNode = CType(node, FieldDeclarationSyntax)
                    Return GetDisplayNode(fieldNode.Declarators.First())

                Case SyntaxKind.PredefinedType
                    Return GetDisplayNode(node.Parent)

                Case SyntaxKind.DocumentationCommentTrivia
                    If node.IsStructuredTrivia Then
                        Dim structuredTriviaSyntax = CType(node, StructuredTriviaSyntax)
                        Return GetDisplayNode(structuredTriviaSyntax.ParentTrivia.Token.Parent)
                    Else
                        Return node
                    End If
            End Select

            Return node
        End Function

        Private Shared Function SymbolToDisplayString(symbol As ISymbol) As String
            Return If(symbol Is Nothing, FeaturesResources.paren_Unknown_paren, symbol.ToDisplayString(Format))
        End Function

        Private Shared Function FormatPropertyAccessor(node As SyntaxNode, symbolName As String) As String
            Dim symbolNameWithNoParams As String = RemoveParameters(symbolName)
            If node.IsKind(SyntaxKind.GetAccessorBlock) Then
                symbolName = String.Format(CultureInfo.CurrentCulture, VBFeaturesResources.Property_getter_name, symbolNameWithNoParams)
            Else
                Debug.Assert(node.IsKind(SyntaxKind.SetAccessorBlock))

                symbolName = String.Format(CultureInfo.CurrentCulture, VBFeaturesResources.Property_setter_name, symbolNameWithNoParams)
            End If

            Return symbolName
        End Function

        Private Shared Function FormatEventHandler(node As SyntaxNode, symbolName As String) As String
            ' symbol name looks Like this at this point : Namespace.Class.Event(EventHandler)
            Dim symbolNameWithNoParams As String = RemoveParameters(symbolName)
            If node.IsKind(SyntaxKind.AddHandlerAccessorBlock) Then
                symbolName = String.Format(CultureInfo.CurrentCulture, VBFeaturesResources.Event_add_handler_name, symbolNameWithNoParams)
            Else
                Debug.Assert(node.IsKind(SyntaxKind.RemoveHandlerAccessorBlock))

                symbolName = String.Format(CultureInfo.CurrentCulture, VBFeaturesResources.Event_remove_handler_name, symbolNameWithNoParams)
            End If

            Return symbolName
        End Function

        Private Shared Function IsAccessorForDefaultProperty(symbol As ISymbol) As Boolean
            Dim methodSymbol = TryCast(symbol, IMethodSymbol) ' its really a SourcePropertyAccessorSymbol but it Is Not accessible 
            If methodSymbol IsNot Nothing Then
                Dim propertySymbol = TryCast(methodSymbol.AssociatedSymbol, IPropertySymbol)
                If propertySymbol IsNot Nothing Then
                    ' Applying the default modifier to a property allows it to be used Like a C# indexer
                    Return propertySymbol.IsDefault()
                End If
            End If

            Return False
        End Function

        Private Shared Function RemoveParameters(symbolName As String) As String
            Dim openParenIndex As Integer = symbolName.IndexOf("("c)
            Dim symbolNameWithNoParams As String = symbolName.Substring(0, openParenIndex)
            Return symbolNameWithNoParams
        End Function

        ''' <summary>
        ''' Gets the DisplayName for the given node.
        ''' </summary>
        Public Function GetDisplayName(semanticModel As SemanticModel, node As SyntaxNode) As String Implements ICodeLensDisplayInfoService.GetDisplayName
            If VisualBasicSyntaxFactsService.Instance.IsGlobalAttribute(node) Then
                Return node.ToString()
            End If

            Dim symbol As ISymbol = semanticModel.GetDeclaredSymbol(node)
            Dim symbolName As String = Nothing

            Select Case node.Kind()
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock
                    ' Indexer properties should Not include get And set
                    symbol = semanticModel.GetDeclaredSymbol(node)
                    If IsAccessorForDefaultProperty(symbol) AndAlso node.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                        Return GetDisplayName(semanticModel, node.Parent)
                    Else
                        ' Append "get" Or "set" to property accessors
                        symbolName = SymbolToDisplayString(symbol)
                        symbolName = FormatPropertyAccessor(node, symbolName)
                    End If

                Case SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock
                    ' Append "add" Or "remove" to event handlers
                    symbolName = SymbolToDisplayString(symbol)
                    symbolName = FormatEventHandler(node, symbolName)

                Case SyntaxKind.ImportsStatement
                    symbolName = "Imports"

                Case Else
                    symbolName = SymbolToDisplayString(symbol)
            End Select

            Return symbolName
        End Function
    End Class
End Namespace
