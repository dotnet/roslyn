' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Composition
Imports System.Globalization
Imports Microsoft.CodeAnalysis.CodeLens
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeLens
    <ExportLanguageService(GetType(IDisplayInfoLanguageServices), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class DisplayInfoVisualBasicServices
        Implements IDisplayInfoLanguageServices

        Private Shared ReadOnly DefaultDisplayFormatVB As SymbolDisplayFormat = New SymbolDisplayFormat(
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
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly ShortDisplayFormatVB As SymbolDisplayFormat = New SymbolDisplayFormat(
                SymbolDisplayGlobalNamespaceStyle.Omitted,
                SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                SymbolDisplayGenericsOptions.IncludeTypeParameters,
                SymbolDisplayMemberOptions.IncludeContainingType Or SymbolDisplayMemberOptions.IncludeParameters,
                SymbolDisplayDelegateStyle.NameOnly,
                SymbolDisplayExtensionMethodStyle.StaticMethod,
                SymbolDisplayParameterOptions.IncludeType,
                SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                SymbolDisplayLocalOptions.IncludeType,
                SymbolDisplayKindOptions.None,
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        ''' <summary>
        ''' Indicates if the given node Is a declaration of some kind of symbol. 
        ''' For example a class for a sub declaration.
        ''' </summary>
        Public Function IsDeclaration(node As SyntaxNode) As Boolean Implements IDisplayInfoLanguageServices.IsDeclaration
            ' From the Visual Basic language spec:
            ' NamespaceMemberDeclaration  :=
            '    NamespaceDeclaration  |
            '    TypeDeclaration
            ' TypeDeclaration  ::=
            '    ModuleDeclaration  |
            '    NonModuleDeclaration
            ' NonModuleDeclaration  ::=
            '    EnumDeclaration  |
            '    StructureDeclaration  |
            '    InterfaceDeclaration  |
            '    ClassDeclaration  |
            '    DelegateDeclaration
            ' ClassMemberDeclaration  ::=
            '    NonModuleDeclaration  |
            '    EventMemberDeclaration  |
            '    VariableMemberDeclaration  |
            '    ConstantMemberDeclaration  |
            '    MethodMemberDeclaration  |
            '    PropertyMemberDeclaration  |
            '    ConstructorMemberDeclaration  |
            '    OperatorDeclaration
            Select Case node.Kind()
                ' Because fields declarations can define multiple symbols "Public a, b As Integer" 
                ' We want to get the VariableDeclarator node inside the field declaration to print out the symbol for the name.
                Case SyntaxKind.VariableDeclarator
                    If (node.Parent.IsKind(SyntaxKind.FieldDeclaration)) Then
                        Return True
                    End If
                    Return False

                Case SyntaxKind.NamespaceStatement
                Case SyntaxKind.NamespaceBlock
                Case SyntaxKind.ModuleStatement
                Case SyntaxKind.ModuleBlock
                Case SyntaxKind.EnumStatement
                Case SyntaxKind.EnumBlock
                Case SyntaxKind.StructureStatement
                Case SyntaxKind.StructureBlock
                Case SyntaxKind.InterfaceStatement
                Case SyntaxKind.InterfaceBlock
                Case SyntaxKind.ClassStatement
                Case SyntaxKind.ClassBlock
                Case SyntaxKind.DelegateFunctionStatement
                Case SyntaxKind.DelegateSubStatement
                Case SyntaxKind.EventStatement
                Case SyntaxKind.EventBlock
                Case SyntaxKind.AddHandlerAccessorBlock
                Case SyntaxKind.RemoveHandlerAccessorBlock
                Case SyntaxKind.FieldDeclaration
                Case SyntaxKind.SubStatement
                Case SyntaxKind.SubBlock
                Case SyntaxKind.FunctionStatement
                Case SyntaxKind.FunctionBlock
                Case SyntaxKind.PropertyStatement
                Case SyntaxKind.PropertyBlock
                Case SyntaxKind.GetAccessorBlock
                Case SyntaxKind.SetAccessorBlock
                Case SyntaxKind.SubNewStatement
                Case SyntaxKind.ConstructorBlock
                Case SyntaxKind.OperatorStatement
                Case SyntaxKind.OperatorBlock
                    Return True
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Indicates if the given node Is a namespace import.
        ''' </summary>
        Public Function IsDirectiveOrImport(node As SyntaxNode) As Boolean Implements IDisplayInfoLanguageServices.IsDirectiveOrImport
            Return node.IsKind(SyntaxKind.ImportsStatement)
        End Function

        ''' <summary>
        ''' Indicates if the given node Is an assembly level attribute "[assembly: MyAttribute]"
        ''' </summary>
        Public Function IsGlobalAttribute(node As SyntaxNode) As Boolean Implements IDisplayInfoLanguageServices.IsGlobalAttribute
            If node.IsKind(SyntaxKind.Attribute) Then
                Dim attributeNode = CType(node, AttributeSyntax)
                If attributeNode.Target IsNot Nothing Then
                    Return attributeNode.Target.AttributeModifier.IsKind(SyntaxKind.AssemblyKeyword)
                End If
            End If

            Return False
        End Function

        ''' <summary>
        ''' Indicates if given node Is DocumentationCommentTriviaSyntax
        ''' </summary>
        Public Function IsDocumentationComment(node As SyntaxNode) As Boolean Implements IDisplayInfoLanguageServices.IsDocumentationComment
            Return node.IsKind(SyntaxKind.DocumentationCommentTrivia)
        End Function

        ''' <summary>
        ''' Returns the node that should be displayed
        ''' </summary>
        Public Function GetDisplayNode(node As SyntaxNode) As SyntaxNode Implements IDisplayInfoLanguageServices.GetDisplayNode
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

        Private Shared Function SymbolToDisplayString(symbolDisplayFormat As SymbolDisplayFormat, symbol As ISymbol) As String
            If symbol Is Nothing Then
                Return VBFeaturesResources.Unknown_value
            End If

            Dim symbolName As String = symbol.ToDisplayString(symbolDisplayFormat)

            ' surounding a idenitfier in square brackets allows you to use a keyword as an identifer
            symbolName = symbolName.Replace("[", String.Empty).Replace("]", String.Empty)
            Return symbolName
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
        Public Function GetDisplayName(semanticModel As SemanticModel, node As SyntaxNode, displayFormat As DisplayFormat) As String Implements IDisplayInfoLanguageServices.GetDisplayName
            Dim symbolDisplayFormat As SymbolDisplayFormat = DefaultDisplayFormatVB
            If displayFormat = DisplayFormat.Short Then
                symbolDisplayFormat = ShortDisplayFormatVB
            End If

            If IsGlobalAttribute(node) Then
                Return node.ToString()
            End If

            Dim symbol As ISymbol = semanticModel.GetDeclaredSymbol(node)
            Dim symbolName As String = Nothing

            Select Case node.Kind()
                Case SyntaxKind.GetAccessorBlock
                Case SyntaxKind.SetAccessorBlock
                    ' Indexer properties should Not include get And set
                    symbol = semanticModel.GetDeclaredSymbol(node)
                    If IsAccessorForDefaultProperty(symbol) AndAlso node.Parent.IsKind(SyntaxKind.PropertyBlock) Then
                        Return GetDisplayName(semanticModel, node.Parent, displayFormat)
                    Else
                        ' Append "get" Or "set" to property accessors
                        symbolName = SymbolToDisplayString(symbolDisplayFormat, symbol)
                        symbolName = FormatPropertyAccessor(node, symbolName)
                    End If

                Case SyntaxKind.AddHandlerAccessorBlock
                Case SyntaxKind.RemoveHandlerAccessorBlock
                    ' Append "add" Or "remove" to event handlers
                    symbolName = SymbolToDisplayString(symbolDisplayFormat, symbol)
                    symbolName = FormatEventHandler(node, symbolName)

                Case SyntaxKind.ImportsStatement
                    symbolName = "Imports"

                Case Else
                    symbolName = SymbolToDisplayString(symbolDisplayFormat, symbol)
            End Select

            Return symbolName
        End Function
    End Class
End Namespace
